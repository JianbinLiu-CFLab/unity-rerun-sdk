// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.RerunSDK.Editor
{
    public sealed class RerunLogBuildPreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder => -90;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[RerunLogBuildPreprocess] Generating RerunLog Player fallback sources...");

            try
            {
                var byType = RerunLogCodeGenerator.CollectRerunLogTypes();
                var files = RerunLogCodeGenerator.GenerateSourceFiles(byType, out _);
                var types = byType.Keys.ToList();
                var linkPath = Path.Combine(Application.dataPath, "RerunLog_link.xml");

                if (types.Count == 0)
                {
                    DeleteIfExists(linkPath);
                    Debug.Log("[RerunLogBuildPreprocess] No [RerunLog] sources found.");
                }
                else
                {
                    var linkXml = RerunLogCodeGenerator.EmitLinkXml(types);
                    File.WriteAllText(linkPath, linkXml);
                    foreach (var type in types)
                    {
                        var fullName = type.FullName ?? type.Name;
                        if (!linkXml.Contains($"fullname=\"{fullName}\""))
                            throw new InvalidOperationException($"RerunLog_link.xml validation missed {fullName}");
                    }

                    Debug.Log($"[RerunLogBuildPreprocess] Generated {files.Count} .g.cs file(s).");
                    Debug.Log($"[RerunLogBuildPreprocess] Wrote RerunLog_link.xml for {types.Count} type(s).");
                }
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    "[RerunLog] Player fallback generation failed.\n" +
                    "The build was stopped because RerunLog generated sources or IL2CPP preservation\n" +
                    "could not be prepared before the Player build.\n\n" +
                    "Details:\n" +
                    $"  - Reason: {ex.GetType().Name}: {ex.Message}\n");
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (!File.Exists(path))
                return;
            File.Delete(path);
            var meta = path + ".meta";
            if (File.Exists(meta))
                File.Delete(meta);
            Debug.Log("[RerunLogBuildPreprocess] Removed stale RerunLog_link.xml.");
        }
    }
}
