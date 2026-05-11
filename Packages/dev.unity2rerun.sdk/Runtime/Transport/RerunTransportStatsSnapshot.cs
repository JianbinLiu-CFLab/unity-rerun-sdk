// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Transport
{
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
