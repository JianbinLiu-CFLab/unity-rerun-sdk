using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Unity.RerunSDK.SourceGenerators;
using Xunit;

public class RerunLogSourceGeneratorTests
{
    [Fact]
    public void Generator_emits_source_for_partial_mono_behaviour_with_entries()
    {
        var source = TestStubs + @"
namespace Game.Debug
{
    public class BasePlayer : UnityEngine.MonoBehaviour {}

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
    public class MonoBehaviour { public Transform transform; }
    public class Transform {}
    public class GameObject { public Transform transform; }
    namespace Scripting { public sealed class PreserveAttribute : System.Attribute {} }
}

namespace Unity.RerunSDK.Unity
{
    public sealed class RerunLogAttribute : System.Attribute
    {
        public RerunLogAttribute(string entityPath) { EntityPath = entityPath; }
        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
        public string Level { get; set; } = ""INFO"";
    }

    public sealed class RerunScalarAttribute : System.Attribute
    {
        public RerunScalarAttribute(string entityPath) { EntityPath = entityPath; }
        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
    }

    public sealed class RerunTransformAttribute : System.Attribute
    {
        public RerunTransformAttribute(string entityPath) { EntityPath = entityPath; }
        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
    }

    public enum RerunGeneratedLogKind { TextLog, Scalar, Transform3D }
    public readonly struct RerunGeneratedLogEntry
    {
        public RerunGeneratedLogEntry(string entityPath, RerunGeneratedLogKind kind, float rateHz, string level = ""INFO"") {}
    }
    public interface IRerunGeneratedLogSource
    {
        int RerunLog_EntryCount { get; }
        RerunGeneratedLogEntry RerunLog_GetEntry(int index);
        void RerunLog_Publish(int index, RerunManager manager);
    }
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
