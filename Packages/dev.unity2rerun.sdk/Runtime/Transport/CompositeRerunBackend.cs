// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Transport
// Purpose: Coordinates Rerun live/file transport state and backend fan-out.

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.Transport
{
    /// Fan-out backend: writes to file first, then live.
    /// File errors are fatal; live errors are swallowed after logging.
    internal class CompositeRerunBackend : IRerunBackend
    {
        private readonly IRerunBackend _file;
        private readonly IRerunBackend _live;

        public CompositeRerunBackend(IRerunBackend file, IRerunBackend live)
        {
            _file = file;
            _live = live;
        }
        /// <summary>
        /// Initializes the backend before messages are written.
        /// </summary>
        public void Initialize(RerunRuntime runtime)
        {
            _file.Initialize(runtime);
            try { _live.Initialize(runtime); }
            catch (System.Exception ex) { LogLiveError("Initialize", ex); }
        }
        /// <summary>
        /// Writes one encoded Rerun message to the backend.
        /// </summary>
        public void Write(EncodedRerunMessage message)
        {
            _file.Write(message);
            try { _live.Write(message); }
            catch (System.Exception ex) { LogLiveError("Write", ex); }
        }
        /// <summary>
        /// Flushes buffered output without changing ownership or finalization state.
        /// </summary>
        public void Flush()
        {
            _file.Flush();
            try { _live.Flush(); }
            catch (System.Exception ex) { LogLiveError("Flush", ex); }
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Shutdown()
        {
            _file.Shutdown();
            try { _live.Shutdown(); }
            catch (System.Exception ex) { LogLiveError("Shutdown", ex); }
        }

        private static void LogLiveError(string op, System.Exception ex)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarning($"[Rerun] Live backend {op} failed: {ex.Message}");
#else
            System.Diagnostics.Debug.WriteLine($"[Rerun] Live backend {op} failed: {ex.Message}");
#endif
        }
    }
}
