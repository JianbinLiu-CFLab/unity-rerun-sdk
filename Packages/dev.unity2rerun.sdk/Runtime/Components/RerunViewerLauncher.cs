// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components
// Purpose: Integrates managed Rerun logging with Unity runtime components.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using Unity.RerunSDK.Transport.Grpc;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Viewer Launcher support for Unity2Rerun.
    /// </summary>
    internal class RerunViewerLauncher
    {
        private Process? _ownedProcess;

        /// Attempt to ensure a Rerun Viewer is listening on the target port.
        /// Returns true if a viewer is (now) listening, false if launch failed.
        public bool EnsureViewerRunning(RerunGrpcEndpoint endpoint,
            string viewerExecutablePath, bool autoLaunch, int launchTimeoutMs = 10000)
        {
            if (RerunGrpcViewerProbe.IsViewerListening(endpoint.GrpcAddress))
            {
                Debug.Log($"[Rerun] Rerun Viewer confirmed on {endpoint.GrpcAddress}");
                return true;
            }

            if (!autoLaunch)
            {
                Debug.LogWarning($"[Rerun] No Viewer on port {endpoint.Port} and auto-launch is disabled");
                return false;
            }

            return LaunchViewer(endpoint, viewerExecutablePath, launchTimeoutMs);
        }

        private bool LaunchViewer(RerunGrpcEndpoint endpoint, string executablePath, int launchTimeoutMs)
        {
            try
            {
                var (cmd, args) = ResolveCommand(executablePath, endpoint);
                if (cmd == null)
                {
                    Debug.LogError("[Rerun] Cannot find rerun executable. Set ViewerExecutablePath or ensure rerun is in PATH.");
                    return false;
                }

                _ownedProcess = new Process
                {
                    StartInfo = new ProcessStartInfo(cmd, args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                };
                _ownedProcess.Start();
                Debug.Log($"[Rerun] Launched Viewer: {cmd} {args} (pid={_ownedProcess.Id})");

                var processExitedCleanly = false;
                var attempts = Math.Max(1, launchTimeoutMs / 100);
                for (int i = 0; i < attempts; i++)
                {
                    if (RerunGrpcViewerProbe.IsViewerListening(endpoint.GrpcAddress))
                    {
                        Debug.Log($"[Rerun] Viewer ready on {endpoint.GrpcAddress}");
                        return true;
                    }
                    if (_ownedProcess.HasExited)
                    {
                        if (_ownedProcess.ExitCode != 0)
                        {
                            Debug.LogWarning($"[Rerun] Viewer process exited before Grpc opened (exit={_ownedProcess.ExitCode})");
                            return false;
                        }

                        if (!processExitedCleanly)
                        {
                            Debug.Log("[Rerun] Viewer launcher process exited cleanly; continuing Grpc probe.");
                            processExitedCleanly = true;
                        }
                    }
                    Thread.Sleep(100);
                }

                Debug.LogWarning($"[Rerun] Viewer process started but Grpc did not open in {launchTimeoutMs}ms");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Rerun] Failed to launch Viewer: {ex.Message}");
                return false;
            }
        }

        private static (string? cmd, string args) ResolveCommand(string executablePath, RerunGrpcEndpoint endpoint)
        {
            var portArg = $"--port {endpoint.Port} --expect-data-soon";

            if (!string.IsNullOrEmpty(executablePath))
                return (executablePath, portArg);

            // Try PATH
            foreach (var name in new[] { "rerun", "rerun.exe" })
            {
                var full = FindInPath(name);
                if (full != null) return (full, portArg);
            }

            // Try Python fallback
            foreach (var pyName in new[] { "python", "python3", "py" })
            {
                var py = FindInPath(pyName);
                if (py != null)
                {
                    foreach (var mod in new[] { "rerun_cli", "rerun" })
                    {
                        try
                        {
                            var p = Process.Start(new ProcessStartInfo(py, $"-m {mod} --help")
                            {
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            });
                            p.WaitForExit(3000);
                            if (p.ExitCode == 0) return (py, $"-m {mod} {portArg}");
                        }
                        catch { }
                    }
                }
            }

            return (null, portArg);
        }

        private static string? FindInPath(string name)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(';'))
            {
                var full = System.IO.Path.Combine(dir.Trim(), name);
                if (System.IO.File.Exists(full)) return full;
            }
            return null;
        }
        /// <summary>
        /// Handles the StopOwnedProcess workflow for this component.
        /// </summary>
        public void StopOwnedProcess()
        {
            if (_ownedProcess != null && !_ownedProcess.HasExited)
            {
                try { _ownedProcess.Kill(); }
                catch { /* already exited */ }
                _ownedProcess.Dispose();
                _ownedProcess = null;
            }
        }
    }
}
