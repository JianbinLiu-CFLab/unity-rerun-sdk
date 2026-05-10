// SPDX-License-Identifier: Apache-2.0

#nullable disable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Unity.RerunSDK.Unity.Control
{
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

        private static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private const string HtmlPage = @"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>Unity2Rerun Control</title>
  <style>
    body{font-family:system-ui,sans-serif;margin:24px;max-width:720px}
    button,input{font:inherit;margin:4px}
    pre{background:#111;color:#eee;padding:12px;overflow:auto}
  </style>
</head>
<body>
  <h1>Unity2Rerun Control</h1>
  <button onclick=""cmd({type:'reset_pose'})"">Reset</button>
  <button onclick=""cmd({type:'set_color',color:[0,1,0,1]})"">Green</button>
  <button onclick=""cmd({type:'set_color',color:[0.4,0.7,1,1]})"">Blue</button>
  <button onclick=""refresh()"">Refresh</button>
  <pre id=""state""></pre>
  <script>
    async function refresh(){state.textContent=await (await fetch('/state')).text();}
    async function cmd(body){await fetch('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});refresh();}
    refresh();
  </script>
</body>
</html>";
    }
}
