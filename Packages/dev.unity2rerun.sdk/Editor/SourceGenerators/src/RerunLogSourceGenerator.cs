// SPDX-License-Identifier: Apache-2.0

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.RerunSDK.Editor;

namespace Unity.RerunSDK.SourceGenerators
{
    [Generator]
    public sealed class RerunLogSourceGenerator : IIncrementalGenerator
    {
        private const string RerunLogAttributeName = "Unity.RerunSDK.Unity.RerunLogAttribute";
        private const string RerunScalarAttributeName = "Unity.RerunSDK.Unity.RerunScalarAttribute";
        private const string RerunTransformAttributeName = "Unity.RerunSDK.Unity.RerunTransformAttribute";
        private const string MonoBehaviourName = "UnityEngine.MonoBehaviour";
        private const string TransformName = "UnityEngine.Transform";
        private const string GameObjectName = "UnityEngine.GameObject";

        private static readonly DiagnosticDescriptor ClassMustBePartial = new(
            "RERUNLOG001",
            "RerunLog source class must be partial",
            "Type '{0}' must be declared partial for RerunLog generation",
            "RerunLog",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor UnsupportedMemberType = new(
            "RERUNLOG002",
            "Unsupported RerunLog member type",
            "Member '{0}' has unsupported type '{1}' for {2}",
            "RerunLog",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor InvalidEntityPath = new(
            "RERUNLOG003",
            "Invalid Rerun entity path",
            "Entity path '{0}' is invalid: {1}",
            "RerunLog",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor MultiVariableField = new(
            "RERUNLOG004",
            "Multi-variable RerunLog field declaration is unsupported",
            "Field declaration with a RerunLog attribute must declare exactly one variable",
            "RerunLog",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor InvalidAttributeTarget = new(
            "RERUNLOG005",
            "Invalid RerunLog attribute target",
            "RerunLog attributes must be on a partial type that inherits UnityEngine.MonoBehaviour",
            "RerunLog",
            DiagnosticSeverity.Error,
            true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => IsCandidate(node),
                    static (ctx, _) => Extract(ctx))
                .Where(static result => result != null);

            context.RegisterSourceOutput(
                candidates.Collect(),
                static (spc, results) => Generate(spc, results));
        }

        private static bool IsCandidate(SyntaxNode node)
        {
            if (node is FieldDeclarationSyntax field && field.AttributeLists.Count > 0)
                return true;
            if (node is PropertyDeclarationSyntax property && property.AttributeLists.Count > 0)
                return true;
            if (node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0)
                return true;
            return false;
        }

        private static CandidateResult Extract(GeneratorSyntaxContext context)
        {
            if (context.Node is FieldDeclarationSyntax field)
                return ExtractField(context, field);
            if (context.Node is PropertyDeclarationSyntax property)
                return ExtractProperty(context, property);
            if (context.Node is ClassDeclarationSyntax cls)
                return ExtractClass(context, cls);
            return null;
        }

        private static CandidateResult ExtractField(GeneratorSyntaxContext context, FieldDeclarationSyntax field)
        {
            if (!HasRerunAttribute(field.AttributeLists))
                return null;

            var classDecl = field.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return DiagnosticOnly(field.GetLocation(), InvalidAttributeTarget);

            var result = CreateResult(context.SemanticModel, classDecl, field.GetLocation());
            if (result.HasBlockingDiagnostics)
                return result;

            if (field.Declaration.Variables.Count != 1)
            {
                result.Diagnostics.Add(Diagnostic.Create(MultiVariableField, field.GetLocation()));
                return result;
            }

            var variable = field.Declaration.Variables[0];
            var symbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (symbol == null)
                return result;

            AddMemberEntries(result, symbol, variable.Identifier.Text, field.GetLocation());
            return result;
        }

        private static CandidateResult ExtractProperty(GeneratorSyntaxContext context, PropertyDeclarationSyntax property)
        {
            if (!HasRerunAttribute(property.AttributeLists))
                return null;

            var classDecl = property.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return DiagnosticOnly(property.GetLocation(), InvalidAttributeTarget);

            var result = CreateResult(context.SemanticModel, classDecl, property.GetLocation());
            if (result.HasBlockingDiagnostics)
                return result;

            var symbol = context.SemanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
            if (symbol == null)
                return result;

            AddMemberEntries(result, symbol, property.Identifier.Text, property.GetLocation());
            return result;
        }

        private static CandidateResult ExtractClass(GeneratorSyntaxContext context, ClassDeclarationSyntax cls)
        {
            if (!HasRerunAttribute(cls.AttributeLists))
                return null;

            var result = CreateResult(context.SemanticModel, cls, cls.Identifier.GetLocation());
            if (result.HasBlockingDiagnostics)
                return result;

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(cls) as INamedTypeSymbol;
            if (classSymbol == null)
                return result;

            foreach (var attr in classSymbol.GetAttributes())
            {
                var attrName = GetFullName(attr.AttributeClass);
                if (attrName == RerunTransformAttributeName)
                {
                    AddEntry(result, attr, RerunSourceEmitter.LogKind.Transform3D,
                        RerunSourceEmitter.MemberKind.ThisTransform, "this", TransformName, cls.Identifier.GetLocation());
                }
                else if (attrName == RerunLogAttributeName || attrName == RerunScalarAttributeName)
                {
                    result.Diagnostics.Add(Diagnostic.Create(InvalidAttributeTarget, cls.Identifier.GetLocation()));
                }
            }

            return result;
        }

        private static CandidateResult CreateResult(SemanticModel model, ClassDeclarationSyntax classDecl, Location location)
        {
            var classSymbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (classSymbol == null)
                return DiagnosticOnly(location, InvalidAttributeTarget);

            var result = new CandidateResult(
                GetNamespace(classSymbol),
                classSymbol.Name,
                ToAccessibility(classSymbol.DeclaredAccessibility),
                classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

            if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                result.Diagnostics.Add(Diagnostic.Create(ClassMustBePartial, classDecl.Identifier.GetLocation(), classSymbol.Name));
                result.HasBlockingDiagnostics = true;
            }

            if (!InheritsFrom(classSymbol, MonoBehaviourName))
            {
                result.Diagnostics.Add(Diagnostic.Create(InvalidAttributeTarget, location));
                result.HasBlockingDiagnostics = true;
            }

            return result;
        }

        private static void AddMemberEntries(
            CandidateResult result,
            ISymbol member,
            string memberName,
            Location location)
        {
            var type = member is IFieldSymbol field ? field.Type :
                member is IPropertySymbol prop ? prop.Type : null;
            if (type == null)
                return;

            foreach (var attr in member.GetAttributes())
            {
                var attrName = GetFullName(attr.AttributeClass);
                if (attrName == RerunLogAttributeName)
                {
                    if (type.SpecialType != SpecialType.System_String)
                    {
                        result.Diagnostics.Add(Diagnostic.Create(
                            UnsupportedMemberType, location, memberName, type.ToDisplayString(), "[RerunLog]"));
                        continue;
                    }

                    AddEntry(result, attr, RerunSourceEmitter.LogKind.TextLog,
                        MemberKindFor(member), memberName, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), location);
                }
                else if (attrName == RerunScalarAttributeName)
                {
                    if (!IsNumeric(type))
                    {
                        result.Diagnostics.Add(Diagnostic.Create(
                            UnsupportedMemberType, location, memberName, type.ToDisplayString(), "[RerunScalar]"));
                        continue;
                    }

                    AddEntry(result, attr, RerunSourceEmitter.LogKind.Scalar,
                        MemberKindFor(member), memberName, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), location);
                }
                else if (attrName == RerunTransformAttributeName)
                {
                    var fullType = GetFullName(type);
                    if (fullType != TransformName && fullType != GameObjectName)
                    {
                        result.Diagnostics.Add(Diagnostic.Create(
                            UnsupportedMemberType, location, memberName, type.ToDisplayString(), "[RerunTransform]"));
                        continue;
                    }

                    AddEntry(result, attr, RerunSourceEmitter.LogKind.Transform3D,
                        MemberKindFor(member), memberName, fullType, location);
                }
            }
        }

        private static void AddEntry(
            CandidateResult result,
            AttributeData attr,
            RerunSourceEmitter.LogKind kind,
            RerunSourceEmitter.MemberKind memberKind,
            string memberName,
            string typeName,
            Location location)
        {
            var entityPath = attr.ConstructorArguments.Length > 0
                ? attr.ConstructorArguments[0].Value as string
                : null;
            var rateHz = GetNamedFloat(attr, "RateHz", 10f);
            var level = kind == RerunSourceEmitter.LogKind.TextLog
                ? GetNamedString(attr, "Level", "INFO")
                : "INFO";

            try
            {
                result.Entries.Add(new RerunSourceEmitter.LogEntry(
                    kind, memberKind, memberName, typeName, entityPath, rateHz, level));
            }
            catch (Exception ex)
            {
                result.Diagnostics.Add(Diagnostic.Create(
                    InvalidEntityPath, location, entityPath ?? "", ex.Message));
            }
        }

        private static void Generate(SourceProductionContext context, ImmutableArray<CandidateResult> results)
        {
            foreach (var diag in results.SelectMany(r => r.Diagnostics))
                context.ReportDiagnostic(diag);

            var grouped = new Dictionary<string, CandidateResult>();
            foreach (var result in results)
            {
                if (result.HasBlockingDiagnostics || result.Entries.Count == 0)
                    continue;

                if (!grouped.TryGetValue(result.FullTypeName, out var existing))
                {
                    grouped[result.FullTypeName] = result;
                    continue;
                }

                existing.Entries.AddRange(result.Entries);
            }

            foreach (var kv in grouped)
            {
                var result = kv.Value;
                try
                {
                    var source = RerunSourceEmitter.EmitClass(
                        result.Namespace,
                        result.ClassName,
                        result.Entries,
                        result.Accessibility);
                    context.AddSource($"{Sanitize(result.FullTypeName)}_RerunLog.g.cs", source);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidAttributeTarget, Location.None, ex.Message));
                }
            }
        }

        private static CandidateResult DiagnosticOnly(Location location, DiagnosticDescriptor descriptor)
        {
            var result = new CandidateResult("", "", "public", Guid.NewGuid().ToString("N"));
            result.Diagnostics.Add(Diagnostic.Create(descriptor, location));
            result.HasBlockingDiagnostics = true;
            return result;
        }

        private static bool HasRerunAttribute(SyntaxList<AttributeListSyntax> lists)
        {
            foreach (var list in lists)
            foreach (var attr in list.Attributes)
            {
                var name = attr.Name.ToString();
                if (name.Contains("RerunLog") || name.Contains("RerunScalar") || name.Contains("RerunTransform"))
                    return true;
            }
            return false;
        }

        private static RerunSourceEmitter.MemberKind MemberKindFor(ISymbol member)
            => member is IPropertySymbol
                ? RerunSourceEmitter.MemberKind.Property
                : RerunSourceEmitter.MemberKind.Field;

        private static bool IsNumeric(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return true;
                default:
                    return false;
            }
        }

        private static bool InheritsFrom(INamedTypeSymbol type, string fullName)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (GetFullName(current) == fullName)
                    return true;
            }
            return false;
        }

        private static string GetFullName(ISymbol symbol)
        {
            if (symbol == null) return "";
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");
        }

        private static string GetNamespace(INamedTypeSymbol symbol)
        {
            var ns = symbol.ContainingNamespace;
            return ns == null || ns.IsGlobalNamespace ? "" : ns.ToDisplayString();
        }

        private static string ToAccessibility(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public: return "public";
                case Accessibility.Internal: return "internal";
                default: return "public";
            }
        }

        private static float GetNamedFloat(AttributeData attr, string name, float fallback)
        {
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == name && kv.Value.Value != null)
                    return Convert.ToSingle(kv.Value.Value);
            }
            return fallback;
        }

        private static string GetNamedString(AttributeData attr, string name, string fallback)
        {
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == name && kv.Value.Value is string value)
                    return value;
            }
            return fallback;
        }

        private static string Sanitize(string value)
        {
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                    chars[i] = '_';
            }
            return new string(chars);
        }

        private sealed class CandidateResult
        {
            public CandidateResult(string ns, string className, string accessibility, string fullTypeName)
            {
                Namespace = ns;
                ClassName = className;
                Accessibility = accessibility;
                FullTypeName = fullTypeName;
            }

            public string Namespace { get; }
            public string ClassName { get; }
            public string Accessibility { get; }
            public string FullTypeName { get; }
            public bool HasBlockingDiagnostics { get; set; }
            public List<RerunSourceEmitter.LogEntry> Entries { get; } = new();
            public List<Diagnostic> Diagnostics { get; } = new();
        }
    }
}
