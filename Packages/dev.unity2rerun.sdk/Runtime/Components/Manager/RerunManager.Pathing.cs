// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Manager
// Purpose: Resolves recording output paths and migrates legacy path defaults.

using System;
using System.IO;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    public partial class RerunManager
    {
        private string ResolvePath()
        {
            var expandedPath = _outputPath
                .Replace("{PERSISTENT}", Application.persistentDataPath)
                .Replace("{TIMESTAMP}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            if (Path.IsPathRooted(expandedPath))
                return Path.GetFullPath(expandedPath);

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, expandedPath));
        }

        private void MigrateLegacyOutputPath()
        {
            if (string.Equals(_outputPath, LegacyPersistentOutputPath, StringComparison.Ordinal))
                _outputPath = DefaultOutputPath;
        }
    }
}
