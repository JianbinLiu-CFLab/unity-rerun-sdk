// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Transport/Grpc
// Purpose: Streams encoded Rerun messages to a live Rerun Viewer over Grpc.

using System;
using System.Net.Sockets;

namespace Unity.RerunSDK.Transport.Grpc
{
    /// <summary>
    /// Provides Rerun Grpc Viewer Probe support for Unity2Rerun.
    /// </summary>
    internal static class RerunGrpcViewerProbe
    {
        /// TCP-level probe only - confirms port is open, does not verify Grpc.
        /// The actual Grpc handshake is deferred to RerunGrpcClient's background reconnect.
        public static bool IsViewerListening(string grpcAddress, int timeoutMs = 500)
        {
            return IsPortOpen(grpcAddress, Math.Max(1, timeoutMs));
        }
        /// <summary>
        /// Handles the IsPortOpenStatic workflow for this component.
        /// </summary>
        internal static bool IsPortOpenStatic(string grpcAddress, int timeoutMs)
        {
            return IsPortOpen(grpcAddress, timeoutMs);
        }

        private static bool IsPortOpen(string grpcAddress, int timeoutMs)
        {
            if (!Uri.TryCreate(grpcAddress, UriKind.Absolute, out var uri))
                return false;

            var port = uri.Port;
            if (port < 0)
                port = uri.Scheme == "https" ? 443 : 80;

            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(uri.Host, port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs)))
                    return false;

                client.EndConnect(result);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
