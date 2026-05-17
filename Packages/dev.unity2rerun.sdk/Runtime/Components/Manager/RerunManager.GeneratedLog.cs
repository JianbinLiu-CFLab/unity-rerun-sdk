// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Manager
// Purpose: Discovers generated log sources and drives their publish cadence.

using System;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    public partial class RerunManager
    {
        /// <summary>
        /// Updates the generated logging source registry.
        /// </summary>
        public static void RegisterGeneratedLogSource(IRerunGeneratedLogSource source)
        {
            if (source == null) return;
            if (!GeneratedLogSources.Contains(source))
                GeneratedLogSources.Add(source);
        }
        /// <summary>
        /// Updates the generated logging source registry.
        /// </summary>
        public static void UnregisterGeneratedLogSource(IRerunGeneratedLogSource source)
        {
            if (source == null) return;
            GeneratedLogSources.Remove(source);
        }

        private void DiscoverGeneratedLogSourcesIfDue()
        {
            if (!IsRecording)
                return;

            _generatedLogDiscoveryTimer -= Time.deltaTime;
            if (_generatedLogDiscoveryTimer > 0f)
                return;

            _generatedLogDiscoveryTimer = GeneratedLogDiscoveryIntervalSeconds;
            DiscoverGeneratedLogSources();
        }

        private static void DiscoverGeneratedLogSources()
        {
#if UNITY_2023_1_OR_NEWER
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var behaviours = FindObjectsOfType<MonoBehaviour>();
#endif
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IRerunGeneratedLogSource source)
                    RegisterGeneratedLogSource(source);
            }
        }

        private void DriveGeneratedLogSources()
        {
            if (!IsRecording || _runtime == null || GeneratedLogSources.Count == 0)
                return;

            _generatedLogSnapshot.Clear();
            _generatedLogSnapshot.AddRange(GeneratedLogSources);
            _generatedLogStale.Clear();

            var dt = Time.deltaTime;
            foreach (var source in _generatedLogSnapshot)
            {
                if (source == null)
                    continue;

                if (source is MonoBehaviour mb)
                {
                    if (mb == null)
                    {
                        _generatedLogStale.Add(source);
                        continue;
                    }

                    if (!mb.isActiveAndEnabled)
                        continue;
                }

                int count;
                try
                {
                    count = source.RerunLog_EntryCount;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RerunLog] Failed to read generated entry count: {ex.Message}");
                    continue;
                }

                if (count <= 0)
                    continue;

                if (!_generatedLogTimers.TryGetValue(source, out var timers) || timers.Length != count)
                {
                    timers = new float[count];
                    _generatedLogTimers[source] = timers;
                }

                for (var i = 0; i < count; i++)
                {
                    timers[i] -= dt;
                    if (timers[i] > 0f)
                        continue;

                    RerunGeneratedLogEntry entry;
                    try
                    {
                        entry = source.RerunLog_GetEntry(i);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RerunLog] Failed to read generated entry {i}: {ex.Message}");
                        timers[i] = 1f;
                        continue;
                    }

                    timers[i] = entry.RateHz > 0f ? 1f / entry.RateHz : 0f;

                    try
                    {
                        source.RerunLog_Publish(i, this);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RerunLog] Generated publish failed for {entry.EntityPath}: {ex.Message}");
                    }
                }
            }

            foreach (var stale in _generatedLogStale)
            {
                GeneratedLogSources.Remove(stale);
                _generatedLogTimers.Remove(stale);
            }
        }
    }
}
