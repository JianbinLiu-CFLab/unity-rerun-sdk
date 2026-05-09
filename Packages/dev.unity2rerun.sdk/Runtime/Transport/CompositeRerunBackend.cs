// SPDX-License-Identifier: Apache-2.0

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

        public void Initialize(RerunRuntime runtime)
        {
            _file.Initialize(runtime);
            try { _live.Initialize(runtime); }
            catch (System.Exception ex) { LogLiveError("Initialize", ex); }
        }

        public void Write(EncodedRerunMessage message)
        {
            _file.Write(message);
            try { _live.Write(message); }
            catch (System.Exception ex) { LogLiveError("Write", ex); }
        }

        public void Flush()
        {
            _file.Flush();
            try { _live.Flush(); }
            catch (System.Exception ex) { LogLiveError("Flush", ex); }
        }

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
