// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.RerunSDK.Unity.Control;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    [AddComponentMenu("Rerun/Control/Rerun Interactive Control Bridge")]
    public class RerunInteractiveControlBridge : MonoBehaviour
    {
        private const int MaxWarningsPerFrame = 10;

        [SerializeField, Tooltip("Target RerunManager for command logs and metrics.")]
        private RerunManager _manager;

        [SerializeField, Tooltip("Transform controlled by the sidecar panel.")]
        private Transform _target;

        [SerializeField, Tooltip("Preferred loopback port. If unavailable, a random free port is used.")]
        private int _preferredPort = 18765;

        [SerializeField, Tooltip("Start the sidecar control server when this component is enabled.")]
        private bool _startOnEnable = true;

        [SerializeField, Tooltip("Current control URL. Read-only at runtime.")]
        private string _controlUrl = "";

        private readonly Queue<RerunControlCommand> _pendingCommands = new();
        private readonly Queue<string> _pendingWarnings = new();
        private readonly object _gate = new();
        private RerunControlServer _server;
        private RerunControlState _state;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        private int _commandCount;
        private string _lastCommand = "";

        public string ControlUrl => _controlUrl;
        public int CommandCount => _commandCount;

        private void Awake()
        {
            if (_target == null)
                _target = transform;

            _initialPosition = _target.position;
            _initialRotation = _target.rotation;
            _initialScale = _target.localScale;
            RefreshState();
        }

        private void OnEnable()
        {
            if (_startOnEnable)
                StartServer();
        }

        private void OnDisable()
        {
            StopServer();
        }

        private void Update()
        {
            DrainWarnings();
            DrainCommands();
            RefreshState();
        }

        public void StartServer()
        {
            if (_server != null && _server.IsRunning)
                return;

            _server = new RerunControlServer(GetStateSnapshot, EnqueueCommand);
            _server.Warning += EnqueueWarning;
            _server.Start(Mathf.Max(0, _preferredPort));
            _controlUrl = _server.ControlUrl;
            RefreshState();
            LogText($"Control server started at {_controlUrl}", "INFO");
        }

        public void StopServer()
        {
            if (_server == null)
                return;

            LogText("Control server stopped", "INFO");
            _server.Warning -= EnqueueWarning;
            _server.Dispose();
            _server = null;
            _controlUrl = "";
            RefreshState();
        }

        private RerunControlCommandResult EnqueueCommand(RerunControlCommand command)
        {
            lock (_gate)
            {
                _pendingCommands.Enqueue(command);
            }
            return RerunControlCommandResult.Success();
        }

        private void EnqueueWarning(string message)
        {
            lock (_gate)
            {
                _pendingWarnings.Enqueue(message);
            }
        }

        private RerunControlState GetStateSnapshot()
        {
            lock (_gate)
            {
                return _state;
            }
        }

        private void DrainWarnings()
        {
            var logged = 0;
            var suppressed = 0;

            while (true)
            {
                string warning;
                lock (_gate)
                {
                    if (_pendingWarnings.Count == 0)
                        break;
                    warning = _pendingWarnings.Dequeue();
                }

                if (logged < MaxWarningsPerFrame)
                {
                    Debug.LogWarning($"[RerunControl] {warning}");
                    LogText(warning, "WARN");
                    logged++;
                }
                else
                {
                    suppressed++;
                }
            }

            if (suppressed > 0)
            {
                var message = $"{suppressed} sidecar warnings suppressed this frame.";
                Debug.LogWarning($"[RerunControl] {message}");
                LogText(message, "WARN");
            }
        }

        private void DrainCommands()
        {
            while (true)
            {
                RerunControlCommand command;
                lock (_gate)
                {
                    if (_pendingCommands.Count == 0)
                        return;
                    command = _pendingCommands.Dequeue();
                }

                ApplyCommand(command);
            }
        }

        private void ApplyCommand(RerunControlCommand command)
        {
            if (_target == null)
                return;

            switch (command.Type)
            {
                case RerunControlCommandType.SetPose:
                    if (command.HasPosition)
                        _target.position = ToVector3(command.Position);
                    if (command.HasRotationEuler)
                        _target.rotation = Quaternion.Euler(ToVector3(command.RotationEuler));
                    break;
                case RerunControlCommandType.SetScale:
                    if (command.HasScale)
                        _target.localScale = Vector3.one * Mathf.Max(0.001f, command.Scale);
                    break;
                case RerunControlCommandType.SetColor:
                    if (command.HasColor)
                        ApplyColor(ToColor(command.Color));
                    break;
                case RerunControlCommandType.ResetPose:
                    _target.position = _initialPosition;
                    _target.rotation = _initialRotation;
                    _target.localScale = _initialScale;
                    break;
            }

            _commandCount++;
            _lastCommand = RerunControlCommandNames.ToWireName(command.Type);
            RefreshState();
            LogText($"Applied command {_lastCommand}", "INFO");
        }

        private void ApplyColor(Color color)
        {
            var renderer = _target.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }

        private void RefreshState()
        {
            if (_target == null)
                return;

            var renderer = _target.GetComponent<Renderer>();
            var color = renderer != null ? renderer.material.color : Color.white;
            var state = new RerunControlState
            {
                Position = FromVector3(_target.position),
                RotationEuler = FromVector3(_target.rotation.eulerAngles),
                Scale = _target.localScale.x,
                Color = FromColor(color),
                CommandCount = _commandCount,
                LastCommand = _lastCommand,
                ControlUrl = _controlUrl
            };

            lock (_gate)
            {
                _state = state;
            }
        }

        private void LogText(string message, string level)
        {
            var manager = _manager != null ? _manager : FindManager();
            if (manager != null && manager.IsRecording)
                manager.LogText("logs/rerun/control", message, level);
        }

        private static RerunManager FindManager()
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<RerunManager>();
#else
            return FindObjectOfType<RerunManager>();
#endif
        }

        private static Vector3 ToVector3(RerunControlVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static RerunControlVector3 FromVector3(Vector3 value)
        {
            return new RerunControlVector3(value.x, value.y, value.z);
        }

        private static Color ToColor(RerunControlColor value)
        {
            return new Color(value.R, value.G, value.B, value.A);
        }

        private static RerunControlColor FromColor(Color value)
        {
            return new RerunControlColor(value.r, value.g, value.b, value.a);
        }
    }
}
