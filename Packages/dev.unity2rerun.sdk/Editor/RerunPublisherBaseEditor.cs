// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Editor
// Purpose: Provides Unity Editor Inspector and build-time tooling for Unity2Rerun.

using Unity.RerunSDK.Unity;
using UnityEditor;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    /// <summary>
    /// Provides Unity Editor support for Rerun Publisher Base Editor.
    /// </summary>
    [CustomEditor(typeof(RerunPublisherBase), true)]
    public class RerunPublisherBaseEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Draws the custom Unity Inspector for the selected component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resolved", EditorStyles.boldLabel);

            var mgrProp = serializedObject.FindProperty("_manager");
            var pathProp = serializedObject.FindProperty("_entityPath");

            if (mgrProp.objectReferenceValue != null)
                EditorGUILayout.LabelField("Manager", mgrProp.objectReferenceValue.name);
            else
                EditorGUILayout.LabelField("Manager", "(auto-detect)");

            EditorGUILayout.LabelField("Entity Path",
                string.IsNullOrEmpty(pathProp.stringValue)
                    ? "(from GameObject hierarchy)"
                    : pathProp.stringValue);

            var rateProp = serializedObject.FindProperty("_publishRateHz");
            var rate = rateProp != null ? rateProp.floatValue : 10f;
            EditorGUILayout.LabelField("Publish Rate",
                rate <= 0 ? "Every frame" : $"{(int)rate} Hz");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
