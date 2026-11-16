using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Story 6-2 T3 — parses a <c>[ProjectionTemplate]</c> marker into a serializable
/// <see cref="ProjectionTemplateMarkerInfo"/> with attached diagnostics. Pure function:
/// no side effects on the compilation.
/// </summary>
public static class ProjectionTemplateMarkerParser {
    public const string ProjectionTemplateAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionTemplateAttribute";
    public const string ProjectionAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute";
    public const string ProjectionRoleEnumName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole";
    public const string ProjectionTemplateContextTypeName = "Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateContext`1";
    public const string ComponentInterfaceName = "Microsoft.AspNetCore.Components.IComponent";
    public const string ParameterAttributeName = "Microsoft.AspNetCore.Components.ParameterAttribute";

    /// <summary>The current Level 2 contract version, mirrored at compile time so the
    /// generator can validate marker <c>ExpectedContractVersion</c> values without depending
    /// on the runtime constant resolution that may live in the consuming compilation.</summary>
    public const int CurrentContractMajor = 1;
    public const int CurrentContractMinor = 0;
    public const int CurrentContractBuild = 0;
    public const int CurrentContractPacked =
        (CurrentContractMajor * 1_000_000) + (CurrentContractMinor * 1_000) + CurrentContractBuild;

    private static readonly EquatableArray<DiagnosticInfo> EmptyDiagnostics = new(ImmutableArray<DiagnosticInfo>.Empty);
    private static readonly ProjectionTemplateMarkerResult EmptyResult = new(null, EmptyDiagnostics);

    public static ProjectionTemplateMarkerResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct) {
        if (context.TargetSymbol is not INamedTypeSymbol templateSymbol) {
            return EmptyResult;
        }

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        // Use the matched attribute (`context.Attributes` is non-empty by construction of
        // ForAttributeWithMetadataName). Defensive null-check anyway.
        AttributeData? markerAttr = null;
        for (int i = 0; i < context.Attributes.Length; i++) {
            AttributeData attr = context.Attributes[i];
            if (attr.AttributeClass?.ToDisplayString() == ProjectionTemplateAttributeName) {
                markerAttr = attr;
                break;
            }
        }

        if (markerAttr is null) {
            return EmptyResult;
        }

        SyntaxNode targetNode = context.TargetNode;
        string filePath = targetNode.SyntaxTree.FilePath ?? string.Empty;
        Microsoft.CodeAnalysis.Text.LinePosition linePos = targetNode.GetLocation().GetLineSpan().StartLinePosition;

        List<DiagnosticInfo> diagnostics = [];

        // ProjectionType (positional arg #0): typeof(...)
        INamedTypeSymbol? projectionSymbol = null;
        if (markerAttr.ConstructorArguments.Length >= 1
            && markerAttr.ConstructorArguments[0].Value is INamedTypeSymbol resolvedProjection) {
            projectionSymbol = resolvedProjection;
        }

        // ExpectedContractVersion (positional arg #1)
        int expectedContractVersion = 0;
        if (markerAttr.ConstructorArguments.Length >= 2
            && markerAttr.ConstructorArguments[1].Value is int versionValue) {
            expectedContractVersion = versionValue;
        }

        // Role (named arg)
        string? role = null;
        bool invalidRole = false;
        for (int i = 0; i < markerAttr.NamedArguments.Length; i++) {
            KeyValuePair<string, TypedConstant> namedArg = markerAttr.NamedArguments[i];
            if (namedArg.Key != "Role") {
                continue;
            }

            if (namedArg.Value.Value is int roleValue) {
                IAssemblySymbol? attrAssembly = markerAttr.AttributeClass?.ContainingAssembly;
                if (attrAssembly is not null
                    && TryResolveEnumMemberName(attrAssembly, ProjectionRoleEnumName, roleValue, out string roleName)) {
                    role = roleName;
                }
                else {
                    invalidRole = true;
                    diagnostics.Add(new DiagnosticInfo(
                        "HFC1024",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "What: [ProjectionTemplate] on '{0}' specifies an unknown ProjectionRole value.\nExpected: A named value from ProjectionRole.\nGot: numeric role value {1}.\nFix: Remove the unsafe enum cast or use a declared ProjectionRole member.\nFallback: This template is excluded from the generated manifest so it cannot accidentally become an any-role template.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1024",
                            templateSymbol.Name,
                            roleValue),
                        "Warning",
                        filePath,
                        linePos.Line,
                        linePos.Character));
                }
            }
        }

        if (invalidRole) {
            return new ProjectionTemplateMarkerResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        // Validate the projection type
        if (projectionSymbol is null) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1033",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "What: [ProjectionTemplate] on '{0}' is missing its projection type argument.\nExpected: typeof(MyProjection) where MyProjection is a [Projection]-annotated class.\nGot: null.\nFix: Pass typeof(MyProjection) as the first argument.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1033",
                    templateSymbol.Name),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
            return new ProjectionTemplateMarkerResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        if (!IsValidProjectionType(projectionSymbol, out string reason)) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1033",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "What: [ProjectionTemplate] on '{0}' references an invalid projection type '{1}'.\nExpected: A non-abstract, non-generic [Projection]-annotated class.\nGot: {2}.\nFix: Apply [ProjectionTemplate] to a template targeting a [Projection] class.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1033",
                    templateSymbol.Name,
                    projectionSymbol.ToDisplayString(),
                    reason),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
            return new ProjectionTemplateMarkerResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        if (!IsValidTemplateType(
            templateSymbol,
            out string templateReason)) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1034",
                string.Format(
                    CultureInfo.InvariantCulture,
            "What: [ProjectionTemplate] component '{0}' is not a valid Level 2 Razor component.\nExpected: A non-abstract, non-generic Razor component partial type.\nGot: {1}.\nFix: Move the marker to the Razor component partial class.\nFallback: This template is excluded from the generated manifest.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1034",
                    templateSymbol.Name,
                    templateReason),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
            return new ProjectionTemplateMarkerResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        // Validate Context property. Invalid template components are excluded from the manifest.
        if (!ValidateTemplateContextParameter(templateSymbol, projectionSymbol, diagnostics, filePath, linePos)) {
            return new ProjectionTemplateMarkerResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        // Validate contract version (warning-only — major mismatch suppresses runtime
        // selection downstream, but the manifest still records the descriptor so adopters
        // see the warning instead of a silent disappearance).
        ValidateContractVersion(templateSymbol, projectionSymbol, expectedContractVersion, diagnostics, filePath, linePos);

        string templateNamespace = templateSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : templateSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string projectionNamespace = projectionSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : projectionSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string templateFqn = ToSourceTypeName(templateSymbol);
        string projectionFqn = ToSourceTypeName(projectionSymbol);

        return new ProjectionTemplateMarkerResult(
            new ProjectionTemplateMarkerInfo(
                templateFqn,
                templateNamespace,
                templateSymbol.Name,
                projectionFqn,
                projectionNamespace,
                projectionSymbol.Name,
                role,
                expectedContractVersion,
                filePath,
                linePos.Line,
                linePos.Character),
            new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    private static bool IsValidTemplateType(
        INamedTypeSymbol templateSymbol,
        out string reason) {
        if (templateSymbol.TypeKind != TypeKind.Class) {
            reason = "the symbol is not a class";
            return false;
        }

        if (templateSymbol.IsStatic) {
            reason = "the symbol is static";
            return false;
        }

        if (templateSymbol.IsAbstract) {
            reason = "the symbol is abstract";
            return false;
        }

        if (templateSymbol.IsGenericType || templateSymbol.IsUnboundGenericType) {
            reason = "the symbol is generic";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsValidProjectionType(INamedTypeSymbol projectionSymbol, out string reason) {
        if (projectionSymbol.TypeKind == TypeKind.Struct) {
            reason = "the symbol is a struct";
            return false;
        }

        if (projectionSymbol.IsAbstract) {
            reason = "the symbol is abstract";
            return false;
        }

        if (projectionSymbol.IsGenericType) {
            reason = "the symbol is generic (open or constructed)";
            return false;
        }

        if (projectionSymbol.IsUnboundGenericType) {
            reason = "the symbol is an unbound generic";
            return false;
        }

        bool hasProjectionAttribute = false;
        foreach (AttributeData attr in projectionSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == ProjectionAttributeName) {
                hasProjectionAttribute = true;
                break;
            }
        }

        if (!hasProjectionAttribute) {
            reason = "the symbol is not annotated with [Projection]";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool ValidateTemplateContextParameter(
        INamedTypeSymbol templateSymbol,
        INamedTypeSymbol projectionSymbol,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        // Search the template (and its base types) for an instance public property named
        // "Context" whose type is ProjectionTemplateContext<TProjection> with the right T.
        INamedTypeSymbol? current = templateSymbol;
        while (current is not null) {
            foreach (ISymbol member in current.GetMembers("Context")) {
                if (member is not IPropertySymbol property) {
                    continue;
                }

                if (property.IsStatic
                    || property.DeclaredAccessibility != Accessibility.Public) {
                    continue;
                }

                if (property.Type is INamedTypeSymbol propertyType
                    && propertyType.IsGenericType
                    && propertyType.OriginalDefinition.MetadataName == "ProjectionTemplateContext`1"
                    && propertyType.OriginalDefinition.ContainingNamespace?.ToDisplayString()
                        == "Hexalith.FrontComposer.Contracts.Rendering"
                    && propertyType.TypeArguments.Length == 1
                    && SymbolEqualityComparer.Default.Equals(propertyType.TypeArguments[0], projectionSymbol)) {
                    if (HasParameterAttribute(property)) {
                        return true;
                    }

                    diagnostics.Add(new DiagnosticInfo(
                        "HFC1034",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "What: [ProjectionTemplate] component '{0}' declares Context but it is not a Razor parameter.\nExpected: A public `[Parameter]` property `ProjectionTemplateContext<{1}> Context {{ get; set; }}`.\nGot: Matching property without Microsoft.AspNetCore.Components.ParameterAttribute.\nFix: Add `[Parameter]` to the Context property in the companion .razor.cs partial.\nFallback: This template is excluded from the generated manifest.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1034",
                            templateSymbol.Name,
                            projectionSymbol.Name),
                        "Warning",
                        filePath,
                        linePos.Line,
                        linePos.Character));
                    return false;
                }
            }

            current = current.BaseType;
        }

        diagnostics.Add(new DiagnosticInfo(
            "HFC1034",
            string.Format(
                CultureInfo.InvariantCulture,
                "What: [ProjectionTemplate] component '{0}' does not declare the required typed Context parameter.\nExpected: A public `[Parameter]` property `ProjectionTemplateContext<{1}> Context {{ get; set; }}`.\nGot: No matching property on '{0}' or its base types.\nFix: Add the property in the companion .razor.cs partial (mark it `[Parameter, EditorRequired]` for Razor renderers).\nFallback: This template is excluded from the generated manifest.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1034",
                templateSymbol.Name,
                projectionSymbol.Name),
            "Warning",
            filePath,
            linePos.Line,
            linePos.Character));
        return false;
    }

    private static void ValidateContractVersion(
        INamedTypeSymbol templateSymbol,
        INamedTypeSymbol projectionSymbol,
        int expectedContractVersion,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        ContractVersionComparison comparison = CompareContractVersion(expectedContractVersion, CurrentContractPacked);
        if (!comparison.ShouldReportDiagnostic) {
            return;
        }

        if (comparison.Decision == ContractVersionDecision.MajorMismatch) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1035",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "What: [ProjectionTemplate] '{0}' for projection '{1}' declares an incompatible contract version.\nExpected: contract major {2} (installed: {2}.{3}.{4}).\nGot: marker requested major {5} (raw value {6}).\nFix: Rebuild the template against ProjectionTemplateContractVersion.Current and update the [ProjectionTemplate] argument.\nFallback: Runtime selection skips this template until the major versions match.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1035",
                    templateSymbol.Name,
                    projectionSymbol.ToDisplayString(),
                    CurrentContractMajor,
                    CurrentContractMinor,
                    CurrentContractBuild,
                    comparison.Expected.Major,
                    expectedContractVersion),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
            return;
        }

        diagnostics.Add(new DiagnosticInfo(
            "HFC1036",
            string.Format(
                CultureInfo.InvariantCulture,
                "What: [ProjectionTemplate] '{0}' for projection '{1}' uses an out-of-date contract minor version.\nExpected: {2}.{3}.{4} (current).\nGot: {5}.{6}.{7} (raw value {8}).\nFix: Update the marker to ProjectionTemplateContractVersion.Current at the next safe touch.\nDocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1036",
                templateSymbol.Name,
                projectionSymbol.ToDisplayString(),
                CurrentContractMajor,
                CurrentContractMinor,
                CurrentContractBuild,
                comparison.Expected.Major,
                comparison.Expected.Minor,
                comparison.Expected.Build,
                expectedContractVersion),
            "Warning",
            filePath,
            linePos.Line,
            linePos.Character));
    }

    private static bool HasParameterAttribute(IPropertySymbol property) {
        foreach (AttributeData attr in property.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == ParameterAttributeName) {
                return true;
            }
        }

        return false;
    }

    private static string ToSourceTypeName(INamedTypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static ContractVersionComparison CompareContractVersion(int expectedPacked, int actualPacked) {
        ContractVersion expected = Unpack(expectedPacked);
        ContractVersion actual = Unpack(actualPacked);

        if (expected.Major != actual.Major) {
            return new ContractVersionComparison(expected, ContractVersionDecision.MajorMismatch, true);
        }

        if (expected.Minor != actual.Minor) {
            return new ContractVersionComparison(expected, ContractVersionDecision.MinorDrift, true);
        }

        if (expected.Build != actual.Build) {
            return new ContractVersionComparison(expected, ContractVersionDecision.BuildDrift, true);
        }

        return new ContractVersionComparison(expected, ContractVersionDecision.Exact, false);
    }

    private static ContractVersion Unpack(int packed)
        => new(packed / 1_000_000, packed / 1_000 % 1_000, packed % 1_000);

    private static bool TryResolveEnumMemberName(
        IAssemblySymbol assembly,
        string metadataName,
        int value,
        out string memberName) {
        INamedTypeSymbol? enumType = assembly.GetTypeByMetadataName(metadataName);
        if (enumType is not null) {
            foreach (ISymbol member in enumType.GetMembers()) {
                if (member is IFieldSymbol field
                    && field.HasConstantValue
                    && field.ConstantValue is int fieldValue
                    && fieldValue == value) {
                    memberName = field.Name;
                    return true;
                }
            }
        }

        memberName = string.Empty;
        return false;
    }

    private readonly struct ContractVersion {
        public ContractVersion(int major, int minor, int build) {
            Major = major;
            Minor = minor;
            Build = build;
        }

        public int Major { get; }

        public int Minor { get; }

        public int Build { get; }
    }

    private readonly struct ContractVersionComparison {
        public ContractVersionComparison(
            ContractVersion expected,
            ContractVersionDecision decision,
            bool shouldReportDiagnostic) {
            Expected = expected;
            Decision = decision;
            ShouldReportDiagnostic = shouldReportDiagnostic;
        }

        public ContractVersion Expected { get; }

        public ContractVersionDecision Decision { get; }

        public bool ShouldReportDiagnostic { get; }
    }

    private enum ContractVersionDecision {
        Exact,
        MajorMismatch,
        MinorDrift,
        BuildDrift,
    }
}
