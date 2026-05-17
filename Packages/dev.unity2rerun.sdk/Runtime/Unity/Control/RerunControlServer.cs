// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity/Control
// Purpose: Implements local loopback sidecar control for interactive Unity samples.

#nullable disable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Unity.RerunSDK.Unity.Control
{
    /// <summary>
    /// Provides Rerun Control Server support for Unity2Rerun.
    /// </summary>
    public sealed class RerunControlServer : IDisposable
    {
        private readonly Func<RerunControlState> _stateProvider;
        private readonly Func<RerunControlCommand, RerunControlCommandResult> _commandHandler;
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public RerunControlServer(
            Func<RerunControlState> stateProvider,
            Func<RerunControlCommand, RerunControlCommandResult> commandHandler)
        {
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public int Port { get; private set; }
        public string ControlUrl { get; private set; } = "";
        public bool IsRunning => _running;

        public event Action<string> Warning;
        /// <summary>
        /// Starts the component or service and prepares its runtime resources.
        /// </summary>
        public void Start(int preferredPort = 18765)
        {
            if (_running)
                return;

            try
            {
                _listener = StartListener(preferredPort);
            }
            catch (SocketException) when (preferredPort != 0)
            {
                _listener = StartListener(0);
                Warning?.Invoke($"Port {preferredPort} is unavailable; using {GetBoundPort(_listener)}.");
            }

            Port = GetBoundPort(_listener);
            ControlUrl = $"http://127.0.0.1:{Port}/";
            _running = true;
            _thread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "RerunControlServer"
            };
            _thread.Start();
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Stop()
        {
            if (!_running)
                return;

            _running = false;
            try
            {
                _listener?.Stop();
            }
            catch
            {
                // Stop is best-effort during Unity shutdown.
            }

            if (_thread != null && _thread.IsAlive)
                _thread.Join(500);

            _thread = null;
            _listener = null;
            Port = 0;
            ControlUrl = "";
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        private static TcpListener StartListener(int port)
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return listener;
        }

        private static int GetBoundPort(TcpListener listener)
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        private void ListenLoop()
        {
            while (_running)
            {
                TcpClient client = null;
                try
                {
                    client = _listener.AcceptTcpClient();
                    HandleClient(client);
                }
                catch (Exception ex) when (IsBenignClientDisconnect(ex))
                {
                    // Browser refreshes/canceled fetches can close the socket while we write.
                    // The server remains healthy, so don't surface this as a Unity warning.
                }
                catch (SocketException)
                {
                    if (_running)
                        Warning?.Invoke("Control server socket error.");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                        Warning?.Invoke($"Control request failed: {ex.Message}");
                }
                finally
                {
                    client?.Dispose();
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, false, 1024, leaveOpen: true);

            var requestLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                WriteResponse(stream, 400, "Bad Request", "text/plain", "Missing request line.");
                return;
            }

            var parts = requestLine.Split(' ');
            if (parts.Length < 2)
            {
                WriteResponse(stream, 400, "Bad Request", "text/plain", "Malformed request line.");
                return;
            }

            var method = parts[0];
            var path = parts[1].Split('?')[0];
            var contentLength = ReadContentLength(reader);
            var body = ReadBody(reader, contentLength);

            if (method == "GET" && (path == "/" || path == ""))
            {
                WriteResponse(stream, 200, "OK", "text/html; charset=utf-8", HtmlPage);
                return;
            }

            if (method == "GET" && path == "/state")
            {
                WriteResponse(stream, 200, "OK", "application/json", _stateProvider().ToJson());
                return;
            }

            if (method == "POST" && path == "/command")
            {
                HandleCommand(stream, body);
                return;
            }

            WriteResponse(stream, 404, "Not Found", "text/plain", "Not found.");
        }

        private void HandleCommand(Stream stream, string body)
        {
            if (!RerunControlCommand.TryParseJson(body, out var command, out var parseError))
            {
                WriteResponse(stream, 400, "Bad Request", "application/json",
                    "{\"ok\":false,\"error\":\"" + Escape(parseError) + "\"}");
                return;
            }

            RerunControlCommandResult result;
            try
            {
                result = _commandHandler(command);
            }
            catch (Exception ex)
            {
                WriteResponse(stream, 500, "Internal Server Error", "application/json",
                    "{\"ok\":false,\"error\":\"" + Escape(ex.Message) + "\"}");
                return;
            }

            if (!result.IsSuccess)
            {
                WriteResponse(stream, 400, "Bad Request", "application/json",
                    "{\"ok\":false,\"error\":\"" + Escape(result.Message) + "\"}");
                return;
            }

            WriteResponse(stream, 200, "OK", "application/json", _stateProvider().ToJson());
        }

        private static int ReadContentLength(StreamReader reader)
        {
            var contentLength = 0;
            string line;
            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {
                var colon = line.IndexOf(':');
                if (colon <= 0) continue;
                var name = line.Substring(0, colon).Trim();
                if (!string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                int.TryParse(line.Substring(colon + 1).Trim(), out contentLength);
            }

            return contentLength;
        }

        private static string ReadBody(TextReader reader, int contentLength)
        {
            if (contentLength <= 0)
                return string.Empty;

            var buffer = new char[contentLength];
            var read = 0;
            while (read < contentLength)
            {
                var n = reader.Read(buffer, read, contentLength - read);
                if (n <= 0) break;
                read += n;
            }

            return new string(buffer, 0, read);
        }

        private static void WriteResponse(Stream stream, int status, string reason, string contentType, string body)
        {
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body ?? "");
            var header =
                $"HTTP/1.1 {status} {reason}\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Connection: close\r\n\r\n";
            var headerBytes = System.Text.Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes, 0, bodyBytes.Length);
        }
        /// <summary>
        /// Handles the IsBenignClientDisconnect workflow for this component.
        /// </summary>
        internal static bool IsBenignClientDisconnect(Exception ex)
        {
            if (ex == null)
                return false;

            if (ex is IOException)
                return true;

            if (ex is SocketException socketException)
            {
                return socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                       socketException.SocketErrorCode == SocketError.ConnectionReset ||
                       socketException.SocketErrorCode == SocketError.Disconnecting ||
                       socketException.SocketErrorCode == SocketError.OperationAborted ||
                       socketException.SocketErrorCode == SocketError.Shutdown;
            }

            return IsBenignClientDisconnect(ex.InnerException);
        }

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>Embedded local control page served by the loopback sidecar endpoint.</summary>
        private const string HtmlPage = @"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>Unity2Rerun Control</title>
  <style>
    body{font-family:system-ui,sans-serif;margin:24px;max-width:760px;line-height:1.4}
    button,input{font:inherit;margin:4px 6px 4px 0}
    section{margin:18px 0}
    .row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
    .param{padding:8px 0;border-top:1px solid #ddd}
    pre{background:#111;color:#eee;padding:12px;overflow:auto}
  </style>
</head>
<body>
  <h1>Unity2Rerun Control</h1>
  <section>
    <h2>Actions</h2>
    <div id=""actions"" class=""row""></div>
  </section>
  <section>
    <h2>Parameters</h2>
    <div id=""parameters""></div>
  </section>
  <button onclick=""refresh()"">Refresh</button>
  <pre id=""state""></pre>
  <script>
    const knownActionIds = ['reset_pose','set_color_green','set_color_red','set_color_blue','scale_down','scale_up','scale_reset'];
    let latestState = null;

    async function refresh(){
      latestState = await (await fetch('/state')).json();
      state.textContent = JSON.stringify(latestState, null, 2);
      renderActions(latestState);
      renderParameters(latestState);
    }

    function renderActions(state){
      actions.textContent = '';
      for (const action of state.actions || []) {
        const button = document.createElement('button');
        button.textContent = action.label || action.id;
        button.disabled = !action.command;
        button.onclick = () => cmd(action.command);
        actions.appendChild(button);
      }
    }

    function renderParameters(state){
      parameters.textContent = '';
      for (const parameter of state.parameters || []) {
        const wrapper = document.createElement('div');
        wrapper.className = 'param';
        const label = document.createElement('label');
        label.textContent = parameter.label || parameter.name;
        wrapper.appendChild(label);

        if (parameter.type === 'float') {
          const input = document.createElement('input');
          input.type = 'number';
          input.step = '0.05';
          input.min = '0.001';
          input.value = parameter.value;
          input.disabled = !parameter.writable;
          input.onchange = () => cmd({type:'set_scale', scale:Number(input.value)});
          wrapper.appendChild(input);
        } else if (parameter.type === 'color') {
          const input = document.createElement('input');
          input.type = 'color';
          input.value = rgbaToHex(parameter.value || [0,1,0,1]);
          input.disabled = !parameter.writable;
          input.onchange = () => cmd({type:'set_color', color:hexToRgba(input.value)});
          wrapper.appendChild(input);
        }
        parameters.appendChild(wrapper);
      }
    }

    async function cmd(body){
      await fetch('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
      refresh();
    }

    function rgbaToHex(rgba){
      return '#' + rgba.slice(0, 3).map(v => Math.round(Math.max(0, Math.min(1, v)) * 255).toString(16).padStart(2, '0')).join('');
    }

    function hexToRgba(hex){
      return [
        parseInt(hex.slice(1, 3), 16) / 255,
        parseInt(hex.slice(3, 5), 16) / 255,
        parseInt(hex.slice(5, 7), 16) / 255,
        1
      ];
    }

    refresh();
  </script>
</body>
</html>";
    }
}
