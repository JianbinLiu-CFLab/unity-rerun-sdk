// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Unity;
using UnityEditor;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    [CustomEditor(typeof(RerunManager))]
    public class RerunManagerEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            var mgr = (RerunManager)target;

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "_outputPath", "_viewerExecutablePath");

            // Browse button for output path
            var pathProp = serializedObject.FindProperty("_outputPath");
            if (pathProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(pathProp);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var dir = EditorUtility.OpenFolderPanel("Select Output Directory",
                        Application.dataPath + "/../build/RRD", "");
                    if (!string.IsNullOrEmpty(dir))
                    {
                        var relative = dir.Replace(Application.dataPath + "/../", "")
                            .Replace('\\', '/');
                        pathProp.stringValue = relative + "/unity_recording_{TIMESTAMP}.rrd";
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // Executable picker for viewer path
            var exeProp = serializedObject.FindProperty("_viewerExecutablePath");
            if (exeProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(exeProp);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var exe = EditorUtility.OpenFilePanel("Select Rerun Executable", "", "exe");
                    if (!string.IsNullOrEmpty(exe))
                    {
                        exeProp.stringValue = exe;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Is Recording", mgr.IsRecording ? "Yes" : "No");

            if (!string.IsNullOrEmpty(mgr.ResolvedOutputPath))
                EditorGUILayout.LabelField("Resolved Output", mgr.ResolvedOutputPath);

            if (Application.isPlaying)
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

                if (GUILayout.Button(mgr.IsRecording ? "Stop Recording" : "Start Recording"))
                {
                    if (mgr.IsRecording)
                        mgr.StopRecording();
                    else
                        mgr.StartRecording();
                }
            }
        }
    }
}
