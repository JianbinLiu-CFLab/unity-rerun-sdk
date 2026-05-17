// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Transport
// Purpose: Coordinates Rerun live/file transport state and backend fan-out.

using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Transport
{
    /// <summary>
    /// Carries Rerun Transport Stats Snapshot data across Unity2Rerun runtime boundaries.
    /// </summary>
    public readonly struct RerunTransportStatsSnapshot
    {
        public bool Supported { get; }
        public bool IsRunning { get; }
        public RerunLiveState LiveState { get; }
        public long QueueDepth { get; }
        public long DroppedCount { get; }
        public long ReconnectCount { get; }
        public long SentStoreInfoCount { get; }
        public long SentDataCount { get; }
        public string LastError { get; }

        public RerunTransportStatsSnapshot(
            bool supported,
            bool isRunning,
            RerunLiveState liveState,
            long queueDepth,
            long droppedCount,
            long reconnectCount,
            long sentStoreInfoCount,
            long sentDataCount,
            string lastError)
        {
            Supported = supported;
            IsRunning = isRunning;
            LiveState = liveState;
            QueueDepth = queueDepth;
            DroppedCount = droppedCount;
            ReconnectCount = reconnectCount;
            SentStoreInfoCount = sentStoreInfoCount;
            SentDataCount = sentDataCount;
            LastError = lastError ?? "";
        }
    }
}
