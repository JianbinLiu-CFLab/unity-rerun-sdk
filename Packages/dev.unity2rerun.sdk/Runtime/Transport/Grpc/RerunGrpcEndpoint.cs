// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Transport/Grpc
// Purpose: Streams encoded Rerun messages to a live Rerun Viewer over Grpc.

using System;

namespace Unity.RerunSDK.Transport.Grpc
{
    /// Parsed Rerun Grpc endpoint: "rerun+http://host:port/proxy"
    internal readonly struct RerunGrpcEndpoint
    {
        public string Host { get; }
        public int Port { get; }
        public string GrpcAddress { get; }

        public static readonly RerunGrpcEndpoint Default = Parse("rerun+http://127.0.0.1:9876/proxy");

        private RerunGrpcEndpoint(string host, int port, string grpcAddress)
        {
            Host = host;
            Port = port;
            GrpcAddress = grpcAddress;
        }
        /// <summary>
        /// Parses the external representation into the SDK model.
        /// </summary>
        public static RerunGrpcEndpoint Parse(string uri)
        {
            // Expect "rerun+http://host:port/proxy" or "rerun+https://host:port/proxy"
            var prefix = uri.StartsWith("rerun+https://") ? "rerun+https://" : "rerun+http://";
            if (!uri.StartsWith(prefix))
                throw new ArgumentException($"Unsupported Grpc endpoint scheme: {uri}");

            var rest = uri[prefix.Length..];
            var pathIdx = rest.IndexOf('/');
            var hostPortStr = pathIdx >= 0 ? rest[..pathIdx] : rest;

            var colonIdx = hostPortStr.LastIndexOf(':');
            if (colonIdx < 0)
                throw new ArgumentException($"Missing port in endpoint: {uri}");

            var host = hostPortStr[..colonIdx];
            var port = int.Parse(hostPortStr[(colonIdx + 1)..]);
            var grpcAddr = uri[("rerun+".Length)..]; // "http://host:port" for GrpcChannel

            return new RerunGrpcEndpoint(host, port, grpcAddr);
        }
    }
}
