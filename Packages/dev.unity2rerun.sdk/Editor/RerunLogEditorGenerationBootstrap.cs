// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Editor
// Purpose: Provides Unity Editor Inspector and build-time tooling for Unity2Rerun.

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    /// <summary>
    /// Provides Unity Editor support for Rerun Log Editor Generation Bootstrap.
    /// </summary>
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
