// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    [InitializeOnLoad]
    internal static class RerunLogEditorGenerationBootstrap
    {
        static RerunLogEditorGenerationBootstrap()
        {
            EditorApplication.delayCall += GenerateWhenEditorIsIdle;
        }

        private static void GenerateWhenEditorIsIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            {
                EditorApplication.delayCall += GenerateWhenEditorIsIdle;
                return;
            }

            try
            {
                var byType = RerunLogCodeGenerator.CollectRerunLogTypes();
                RerunLogCodeGenerator.GenerateSourceFiles(byType, out var wroteAnyFiles);
                if (wroteAnyFiles)
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RerunLog] Editor source generation skipped: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
