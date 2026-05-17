// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: UnityProject/Assets/Editor
// Purpose: Adds Unity Editor build helpers for local validation builds.

// Batchmode entrypoint for Unity2Rerun IL2CPP Player builds.

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
/// <summary>
/// Provides Unity Editor support for Rerun Build.
/// </summary>
public static class RerunBuild
{
    /// <summary>Sample scene used by the command-line build entry point.</summary>
    private const string DefaultScene = "Assets/Scenes/SampleScene.unity";
    /// <summary>Default Windows IL2CPP player output path for local validation builds.</summary>
    private const string DefaultOutputPath = "build/Unity/WindowsIL2CPP/Unity2RerunDemo.exe";
    /// <summary>
    /// Builds the WindowsIl2Cpp result from the current inputs.
    /// </summary>
    [MenuItem("Rerun/Build Windows IL2CPP")]
    public static void BuildWindowsIl2Cpp()
    {
        BuildIl2Cpp(DefaultOutputPath, developmentBuild: false);
    }
    /// <summary>
    /// Builds the Il2CppFromCommandLine result from the current inputs.
    /// </summary>
    public static void BuildIl2CppFromCommandLine()
    {
        var outputPath = GetCommandLineValue("-rerunBuildOutput") ?? DefaultOutputPath;
        var developmentBuild = HasCommandLineFlag("-development") || HasCommandLineFlag("-rerunDevelopmentBuild");
        BuildIl2Cpp(outputPath, developmentBuild);
    }

    private static void BuildIl2Cpp(string outputPath, bool developmentBuild)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Build output path is empty.", nameof(outputPath));

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        var scenes = ResolveScenes();
        var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
        PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Medium);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = developmentBuild ? BuildOptions.Development : BuildOptions.None
        };

        Debug.Log($"[RerunBuild] Starting Windows IL2CPP Player build: {outputPath}");
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.totalErrors == 0)
        {
            Debug.Log($"[RerunBuild] Build succeeded: {outputPath}");
            return;
        }

        throw new Exception($"[RerunBuild] Build failed with {report.summary.totalErrors} error(s): {report.summary}");
    }

    private static string[] ResolveScenes()
    {
        var enabledScenes = EditorBuildSettings.scenes;
        if (enabledScenes != null && enabledScenes.Length > 0)
        {
            var scenes = Array.FindAll(enabledScenes, scene => scene.enabled && !string.IsNullOrEmpty(scene.path));
            if (scenes.Length > 0)
            {
                var result = new string[scenes.Length];
                for (var i = 0; i < scenes.Length; i++)
                    result[i] = scenes[i].path;
                return result;
            }
        }

        if (File.Exists(DefaultScene))
            return new[] { DefaultScene };

        throw new FileNotFoundException($"No enabled build scenes found and fallback scene is missing: {DefaultScene}");
    }

    private static string GetCommandLineValue(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }

    private static bool HasCommandLineFlag(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == name)
                return true;
        }
        return false;
    }
}
