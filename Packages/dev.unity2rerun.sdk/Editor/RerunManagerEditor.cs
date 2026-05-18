// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Editor
// Purpose: Provides Unity Editor Inspector and build-time tooling for Unity2Rerun.

using System;
using System.IO;
using Unity.RerunSDK.Unity;
using Unity.RerunSDK.Unity.Control;
using UnityEditor;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    /// <summary>
    /// Provides Unity Editor support for Rerun Manager Editor.
    /// </summary>
    [CustomEditor(typeof(RerunManager))]
    public class RerunManagerEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Requests repainting while Play Mode status values are changing.
        /// </summary>
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
        /// <summary>
        /// Draws the custom Unity Inspector for the selected component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var mgr = (RerunManager)target;

            serializedObject.Update();

            DrawStatusSummary(mgr);
            DrawRecordingSection();
            DrawRrdOutputSection();
            DrawLiveViewerSection();
            DrawAdvancedSection();

            if (Application.isPlaying)
                DrawRuntimeHealth(mgr);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatusSummary(RerunManager mgr)
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Is Recording", mgr.IsRecording ? "Yes" : "No");

            if (Application.isPlaying)
                EditorGUILayout.LabelField("Live State", mgr.LiveState.ToString());

            if (!string.IsNullOrEmpty(mgr.ResolvedOutputPath))
                EditorGUILayout.LabelField("Resolved Output", mgr.ResolvedOutputPath);
        }

        private void DrawRecordingSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recording", EditorStyles.boldLabel);
            DrawProperty("_applicationId");
            DrawProperty("_outputMode");
            DrawProperty("_recordOnStart");
            DrawProperty("_runInBackground");
        }

        private void DrawRrdOutputSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RRD Output", EditorStyles.boldLabel);
            DrawOutputPathPicker();
            DrawProperty("_recordingCompression", new GUIContent(
                "Recording Compression",
                "Compression for .rrd file recording only. Live gRPC output remains uncompressed."));
        }

        private void DrawLiveViewerSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Viewer", EditorStyles.boldLabel);
            DrawProperty("_liveEndpoint");
            DrawProperty("_autoLaunchViewer");
            DrawExecutablePathPicker();
            DrawProperty("_connectTimeoutMs");
            DrawProperty("_reconnectDelayMs");
            DrawProperty("_maxLiveQueueMessages");
        }

        private void DrawAdvancedSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            DrawProperty("_writeViewCoordinates");
        }

        private void DrawRuntimeHealth(RerunManager mgr)
        {
            var stats = mgr.GetTransportStatsSnapshot();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transport Health", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Live State", stats.LiveState.ToString());
            EditorGUILayout.LabelField("Supported", stats.Supported ? "Yes" : "No");
            EditorGUILayout.LabelField("Running", stats.IsRunning ? "Yes" : "No");
            EditorGUILayout.LabelField("Queue Depth", stats.QueueDepth.ToString());
            EditorGUILayout.LabelField("Dropped", stats.DroppedCount.ToString());
            EditorGUILayout.LabelField("Reconnects", stats.ReconnectCount.ToString());
            EditorGUILayout.LabelField("Sent StoreInfo", stats.SentStoreInfoCount.ToString());
            EditorGUILayout.LabelField("Sent Data", stats.SentDataCount.ToString());
            EditorGUILayout.LabelField("Last Error", string.IsNullOrEmpty(stats.LastError) ? "-" : stats.LastError);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sidecar / Control", EditorStyles.boldLabel);
            var bridge = FindControlBridge();
            if (bridge == null)
            {
                EditorGUILayout.LabelField("Bridge", "Not found");
            }
            else
            {
                EditorGUILayout.LabelField("Control URL", string.IsNullOrEmpty(bridge.ControlUrl) ? "-" : bridge.ControlUrl);
                EditorGUILayout.LabelField("Command Count", bridge.CommandCount.ToString());
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(mgr.IsRecording ? "Stop Recording" : "Start Recording"))
            {
                if (mgr.IsRecording)
                    mgr.StopRecording();
                else
                    mgr.StartRecording();
            }
        }

        private void DrawOutputPathPicker()
        {
            var pathProp = serializedObject.FindProperty("_outputPath");
            if (pathProp == null)
            {
                DrawMissing("_outputPath");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(pathProp);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var dir = EditorUtility.OpenFolderPanel("Select Output Directory",
                    Application.dataPath + "/../build/RRD", "");
                if (!string.IsNullOrEmpty(dir))
                {
                    pathProp.stringValue = BuildOutputPathForDirectory(dir);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawExecutablePathPicker()
        {
            var exeProp = serializedObject.FindProperty("_viewerExecutablePath");
            if (exeProp == null)
            {
                DrawMissing("_viewerExecutablePath");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(exeProp);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var exe = EditorUtility.OpenFilePanel("Select Rerun Executable", "", "exe");
                if (!string.IsNullOrEmpty(exe))
                    exeProp.stringValue = exe;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProperty(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null)
            {
                DrawMissing(propertyName);
                return;
            }

            EditorGUILayout.PropertyField(prop);
        }

        private void DrawProperty(string propertyName, GUIContent label)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null)
            {
                DrawMissing(propertyName);
                return;
            }

            EditorGUILayout.PropertyField(prop, label);
        }

        private static void DrawMissing(string propertyName)
        {
            EditorGUILayout.HelpBox($"Missing serialized property: {propertyName}", MessageType.Warning);
        }

        private static string BuildOutputPathForDirectory(string dir)
        {
            var selected = Path.GetFullPath(dir);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedProjectRoot = EnsureTrailingSeparator(projectRoot);

            string basePath;
            if (selected.StartsWith(normalizedProjectRoot, StringComparison.OrdinalIgnoreCase))
            {
                basePath = selected.Substring(normalizedProjectRoot.Length);
            }
            else
            {
                basePath = selected;
            }

            return (basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                    "/unity_recording_{TIMESTAMP}.rrd")
                .Replace('\\', '/');
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;

            return path + Path.DirectorySeparatorChar;
        }

        private static RerunInteractiveControlBridge FindControlBridge()
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<RerunInteractiveControlBridge>();
#else
            return UnityEngine.Object.FindObjectOfType<RerunInteractiveControlBridge>();
#endif
        }
    }
}
