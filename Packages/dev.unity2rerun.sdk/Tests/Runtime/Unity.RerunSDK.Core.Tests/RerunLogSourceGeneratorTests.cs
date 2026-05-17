// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Rerun Log Source Generator Tests behavior for release and regression validation.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Unity.RerunSDK.SourceGenerators;
using Xunit;
/// <summary>
/// Regression tests for Rerun Log Source Generator Tests.
/// </summary>
public class RerunLogSourceGeneratorTests
{
    [Fact]
    public void Generator_emits_source_for_partial_mono_behaviour_with_entries()
    {
        var source = TestStubs + @"
namespace Game.Debug
{
    /// <summary>
    /// Regression tests for Base Player.
    /// </summary>
    public class BasePlayer : UnityEngine.MonoBehaviour {}
    /// <summary>
    /// Regression tests for Player Debug.
    /// </summary>
    public partial class PlayerDebug : BasePlayer
    {
        [Unity.RerunSDK.Unity.RerunLog(""logs/status"", RateHz = 1f)]
        private string _status = ""ready"";

        [Unity.RerunSDK.Unity.RerunScalar(""metrics/speed"", RateHz = 10f)]
        public float Speed { get; set; }

        [Unity.RerunSDK.Unity.RerunTransform(""world/player"", RateHz = 30f)]
        private UnityEngine.Transform _target;
    }
}";

        var result = RunGenerator(source);

        var generated = Assert.Single(result.GeneratedSources);
        Assert.Contains("partial class PlayerDebug : IRerunGeneratedLogSource", generated.SourceText.ToString());
        Assert.Contains("RerunLog_EntryCount => 3", generated.SourceText.ToString());
        Assert.Contains("manager.LogText(\"logs/status\"", generated.SourceText.ToString());
        Assert.Contains("manager.LogScalar(\"metrics/speed\"", generated.SourceText.ToString());
        Assert.Contains("manager.LogTransform(\"world/player\"", generated.SourceText.ToString());
    }

    [Fact]
    public void Generator_reports_clear_diagnostic_for_non_partial_type()
    {
        var source = TestStubs + @"
/// <summary>
/// Regression tests for Bad Debug.
/// </summary>
public class BadDebug : UnityEngine.MonoBehaviour
{
    [Unity.RerunSDK.Unity.RerunLog(""logs/status"")]
    private string _status = ""ready"";
}";

        var result = RunGenerator(source, expectCompilationErrors: true);

        Assert.Contains(result.Diagnostics, d => d.Id == "RERUNLOG001");
    }

    [Fact]
    public void Generator_allows_user_lifecycle_methods()
    {
        var source = TestStubs + @"
/// <summary>
/// Regression tests for Lifecycle Debug.
/// </summary>
public partial class LifecycleDebug : UnityEngine.MonoBehaviour
{
    [Unity.RerunSDK.Unity.RerunLog(""logs/status"")]
    private string _status = ""ready"";

    private void OnEnable() {}
    private void OnDisable() {}
    private void OnDestroy() {}
}";

        var result = RunGenerator(source);

        var generated = Assert.Single(result.GeneratedSources);
        Assert.DoesNotContain("OnEnable", generated.SourceText.ToString());
        Assert.DoesNotContain("OnDisable", generated.SourceText.ToString());
        Assert.DoesNotContain("OnDestroy", generated.SourceText.ToString());
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "RERUNLOG006");
    }

    private static GeneratorRunResult RunGenerator(string source, bool expectCompilationErrors = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            new[] { syntaxTree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new RerunLogSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);
        if (!expectCompilationErrors)
        {
            Assert.DoesNotContain(generatorDiagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.DoesNotContain(outputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        }
        return Assert.Single(driver.GetRunResult().Results);
    }

    private const string TestStubs = @"
namespace UnityEngine
{
    /// <summary>
    /// Regression tests for Mono Behaviour.
    /// </summary>
    public class MonoBehaviour { public Transform transform; }
    /// <summary>
    /// Regression tests for Transform.
    /// </summary>
    public class Transform {}
    /// <summary>
    /// Regression tests for Game Object.
    /// </summary>
    public class GameObject { public Transform transform; }
    namespace Scripting { public sealed class PreserveAttribute : System.Attribute {} }
}

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Regression tests for Rerun Log Attribute.
    /// </summary>
    public sealed class RerunLogAttribute : System.Attribute
    {
        public RerunLogAttribute(string entityPath) { EntityPath = entityPath; }
        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
        public string Level { get; set; } = ""INFO"";
    }
    /// <summary>
    /// Regression tests for Rerun Scalar Attribute.
    /// </summary>
    public sealed class RerunScalarAttribute : System.Attribute
    {
        public RerunScalarAttribute(string entityPath) { EntityPath = entityPath; }
        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
    }
    /// <summary>
    /// Regression tests for Rerun Transform Attribute.
    /// </summary>
    public sealed class RerunTransformAttribute : System.Attribute
    {
        public RerunTransformAttribute(string entityPath) { EntityPath = entityPath; }
        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
    }
    /// <summary>
    /// Regression tests for Rerun Generated Log Kind.
    /// </summary>
    public enum RerunGeneratedLogKind { TextLog, Scalar, Transform3D }
    /// <summary>
    /// Regression tests for Rerun Generated Log Entry.
    /// </summary>
    public readonly struct RerunGeneratedLogEntry
    {
        public RerunGeneratedLogEntry(string entityPath, RerunGeneratedLogKind kind, float rateHz, string level = ""INFO"") {}
    }
    /// <summary>
    /// Regression tests for IRerun Generated Log Source.
    /// </summary>
    public interface IRerunGeneratedLogSource
    {
        int RerunLog_EntryCount { get; }
        RerunGeneratedLogEntry RerunLog_GetEntry(int index);
        void RerunLog_Publish(int index, RerunManager manager);
    }
    /// <summary>
    /// Regression tests for Rerun Manager.
    /// </summary>
    public class RerunManager
    {
        public static void RegisterGeneratedLogSource(IRerunGeneratedLogSource source) {}
        public static void UnregisterGeneratedLogSource(IRerunGeneratedLogSource source) {}
        public void LogText(string path, string text, string level) {}
        public void LogScalar(string path, double value) {}
        public void LogTransform(string path, UnityEngine.Transform transform) {}
    }
}
";
}
