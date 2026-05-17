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
    /// Provides Unity Editor support for Rerun Scalar Publisher Editor.
    /// </summary>
    [CustomEditor(typeof(RerunScalarPublisher))]
    public class RerunScalarPublisherEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Draws the custom Unity Inspector for the selected component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var sourceProp = serializedObject.FindProperty("_source");
            EditorGUILayout.PropertyField(sourceProp);

            var source = (RerunScalarSource)sourceProp.enumValueIndex;

            // Conditionally show _constantValue
            if (source == RerunScalarSource.Constant)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_constantValue"));
            }

            // Conditionally show _target for transform-based sources
            if (RerunScalarSourceEvaluator.IsTransformSource(source))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_target"));
            }

            // Shared fields from base
            DrawPropertiesExcluding(serializedObject,
                "_source", "_constantValue", "_target", "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
