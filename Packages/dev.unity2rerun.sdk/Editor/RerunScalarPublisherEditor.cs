// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Unity;
using UnityEditor;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    [CustomEditor(typeof(RerunScalarPublisher))]
    public class RerunScalarPublisherEditor : UnityEditor.Editor
    {
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
