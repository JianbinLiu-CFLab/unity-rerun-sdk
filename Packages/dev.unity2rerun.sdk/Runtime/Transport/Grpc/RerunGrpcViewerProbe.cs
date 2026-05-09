// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net.Sockets;

namespace Unity.RerunSDK.Transport.Grpc
{
    internal static class RerunGrpcViewerProbe
    {
        /// TCP-level probe only — confirms port is open, does not verify gRPC.
        /// The actual gRPC handshake is deferred to RerunGrpcClient's background reconnect.
        public static bool IsViewerListening(string grpcAddress, int timeoutMs = 500)
        {
            return IsPortOpen(grpcAddress, Math.Max(1, timeoutMs));
        }

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
