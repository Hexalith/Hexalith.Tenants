using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Parses [Command]-annotated types into <see cref="CommandModel"/> IR.
/// Pure function: no side effects, no Compilation references in output.
/// </summary>
public static class CommandParser {
    /// <summary>
    /// Property names that are classified as derivable by convention (filled by infrastructure).
    /// </summary>
    private static readonly HashSet<string> WellKnownDerivablePropertyNames = new(StringComparer.Ordinal) {
        "MessageId",
        "CommandId",
        "CorrelationId",
        "TenantId",
        "UserId",
        "Timestamp",
        "CreatedAt",
        "ModifiedAt",
    };

    private const string BoundedContextAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.BoundedContextAttribute";
    private const string DerivedFromAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.DerivedFromAttribute";
    private const string DisplayAttributeName = "System.ComponentModel.DataAnnotations.DisplayAttribute";
    private const string IconAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.IconAttribute";
    private const string DefaultValueAttributeName = "System.ComponentModel.DefaultValueAttribute";
    private const string DestructiveAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.DestructiveAttribute";
    private const string RequiresPolicyAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.RequiresPolicyAttribute";
    private const string CommandTargetAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.CommandTargetAttribute";
    private const string ProjectionAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute";

    /// <summary>
    /// Story 2-5 D20 / ADR-026 — commands whose TypeName matches this pattern AND lack
    /// <c>[Destructive]</c> surface <c>HFC1020</c>. Prefixes cover the common destructive
    /// verbs plus the expanded set added after Red Team Attack-2 (<c>Erase</c>/<c>Drop</c>/
    /// <c>Truncate</c>/<c>Wipe</c>) to close name-heuristic false negatives.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex DestructiveNamePattern =
        new(
            @"^(Delete|Remove|Purge|Erase|Drop|Truncate|Wipe)(?:[A-Z]|Command$)",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Warning threshold for non-derivable property count.</summary>
    public const int NonDerivableWarningThreshold = 30;

    /// <summary>Hard error threshold for non-derivable property count (DoS mitigation).</summary>
    public const int NonDerivableErrorThreshold = 100;

    /// <summary>Hard error threshold for TOTAL property count — Story 2-2 HFC1011 (DoS mitigation).</summary>
    public const int TotalPropertyHardLimit = 200;

    private static readonly EquatableArray<DiagnosticInfo> EmptyDiagnostics = new(ImmutableArray<DiagnosticInfo>.Empty);
    private static readonly CommandParseResult EmptyResult = new(null, EmptyDiagnostics);

    /// <summary>
    /// Parses a <see cref="GeneratorAttributeSyntaxContext"/> target symbol into a command IR.
    /// </summary>
    public static CommandParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct) {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) {
            return EmptyResult;
        }

        return Parse(typeSymbol, context.TargetNode, context.SemanticModel.Compilation, ct);
    }

    /// <summary>
    /// Parses a [Command]-annotated type symbol into a command IR plus any diagnostics.
    /// </summary>
    public static CommandParseResult Parse(
        INamedTypeSymbol typeSymbol,
        SyntaxNode targetNode,
        Compilation compilation,
        CancellationToken ct) {
        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        List<DiagnosticInfo> diagnostics = [];
        string filePath = targetNode.SyntaxTree.FilePath ?? string.Empty;
        Microsoft.CodeAnalysis.Text.LinePosition linePos = targetNode.GetLocation().GetLineSpan().StartLinePosition;

        ValidateTypeKind(typeSymbol, targetNode, diagnostics, filePath, linePos);

        // HFC1014: nested [Command] types are unsupported (Story 2-2 Task 1.3b). A command's ContainingSymbol
        // must be a namespace, not a class/struct/record. Emit early and halt parsing — nested generation would
        // break the route-emission invariant {namespace.{CommandTypeName}Page} if the class lived inside another.
        if (typeSymbol.ContainingType is not null) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1014",
                string.Format(
                    "[Command] type '{0}' is nested inside '{1}'. Command types must be top-level within a namespace.",
                    typeSymbol.Name,
                    typeSymbol.ContainingType.ToDisplayString()),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));

            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        // HFC1017 (Story 2-3 Hindsight H9): generic [Command] types are unsupported. The per-command
        // lifecycle bridge emits one hint per command type; a generic command would collide on hint-name
        // across specialisations and cannot be uniformly subscribed via IActionSubscriber.SubscribeToAction<T>.
        if (typeSymbol.IsGenericType && typeSymbol.TypeParameters.Length > 0) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1017",
                string.Format(
                    "[Command] type '{0}' is generic (arity {1}). Command types must not be generic; specialize or remove the type parameters.",
                    typeSymbol.Name,
                    typeSymbol.TypeParameters.Length),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));

            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        // HFC1009: emitted form body uses `_model = new()`; commands without a public parameterless
        // ctor (positional records, classes with only parameterised ctors) would generate uncompilable
        // code. Surface a build error so the adopter fixes the command shape. (Patch P1.)
        if (!HasPublicParameterlessConstructor(typeSymbol)) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1009",
                string.Format(
                    "[Command] type '{0}' has no public parameterless constructor. The generated form requires 'new {0}()' to build the form model. Add a parameterless constructor, or give every positional parameter a default value.",
                    typeSymbol.Name),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));

            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        string typeName = typeSymbol.Name;
        string ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        string? boundedContext = ParseBoundedContext(typeSymbol, out string? boundedContextDisplayLabel);
        string? displayName = ParseDisplayAttribute(typeSymbol);
        string? iconName = ParseIconAttribute(typeSymbol);
        string? authorizationPolicyName = ParseRequiresPolicyAttribute(typeSymbol, diagnostics, filePath, linePos);
        CommandTargetModel? commandTarget = ParseCommandTarget(typeSymbol, diagnostics, filePath, linePos);
        if (diagnostics.Any(d => d.Id is "HFC1056" or "HFC1057")) {
            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        // Story 2-5 Task 2.2 — parse [Destructive] opt-in marker + optional confirmation copy overrides.
        bool isDestructive = ParseDestructiveAttribute(
            typeSymbol,
            out string? destructiveConfirmTitle,
            out string? destructiveConfirmBody);

        // Story 2-5 D20 / HFC1020 (Info) — name-heuristic guardrail: command matches destructive verb pattern
        // but lacks [Destructive]. Info severity in v0.1 to prevent Day-1 adoption blockers (promotion to
        // Warning + [SuppressHFC1020] ship in Story 9-4).
        if (!isDestructive && DestructiveNamePattern.IsMatch(typeSymbol.Name)) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1020",
                string.Format(
                    "Command '{0}' appears destructive by name but is missing [Destructive] attribute. Add [Destructive] or rename the command.",
                    typeSymbol.Name),
                "Info",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (ct.IsCancellationRequested) {
            return EmptyResult;
        }

        // Collect public, non-static, non-indexer properties, including inherited ones (e.g., MessageId on a base record).
        ImmutableArray<PropertyModel>.Builder allBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
        ImmutableArray<PropertyModel>.Builder derivableBuilder = ImmutableArray.CreateBuilder<PropertyModel>();
        ImmutableArray<PropertyModel>.Builder nonDerivableBuilder = ImmutableArray.CreateBuilder<PropertyModel>();

        // Case-insensitive dedup + MessageId lookup (patch 2026-04-16 P-02): a property named
        // `messageId` satisfies the runtime MessageId contract, so HFC1006 must not fire on casing alone.
        HashSet<string> seenNames = new(StringComparer.Ordinal);
        List<IPropertySymbol> assignmentTargetSymbols = [];
        INamedTypeSymbol? currentType = typeSymbol;
        while (currentType is not null && currentType.SpecialType != SpecialType.System_Object) {
            foreach (ISymbol member in currentType.GetMembers()) {
                if (ct.IsCancellationRequested) {
                    return EmptyResult;
                }

                if (member is not IPropertySymbol propertySymbol
                    || propertySymbol.DeclaredAccessibility != Accessibility.Public
                    || propertySymbol.IsStatic
                    || propertySymbol.IsIndexer) {
                    continue;
                }

                // Skip record synthetic EqualityContract property.
                if (propertySymbol.Name == "EqualityContract") {
                    continue;
                }

                // Inherited override: keep the most-derived declaration only.
                if (!seenNames.Add(propertySymbol.Name)) {
                    continue;
                }

                PropertyModel property = AttributeParser.ParsePropertyForCommand(
                    propertySymbol,
                    typeName,
                    diagnostics,
                    filePath,
                    compilation);
                ValidateDefaultValueType(propertySymbol, diagnostics, filePath);
                allBuilder.Add(property);
                assignmentTargetSymbols.Add(propertySymbol);

                bool isDerivable = IsDerivableProperty(propertySymbol);
                if (isDerivable) {
                    derivableBuilder.Add(property);
                }
                else {
                    nonDerivableBuilder.Add(property);
                }
            }

            currentType = currentType.BaseType;
        }

        // Check MessageId presence (HFC1006). Also walk AllInterfaces so an interface-declared
        // `MessageId` (potentially with a default implementation) satisfies the contract (patch 2026-04-16 P-03).
        bool hasMessageId = seenNames.Contains("MessageId");
        if (!hasMessageId) {
            foreach (INamedTypeSymbol iface in typeSymbol.AllInterfaces) {
                foreach (ISymbol interfaceMember in iface.GetMembers("MessageId")) {
                    if (interfaceMember is IPropertySymbol { IsStatic: false, IsIndexer: false }) {
                        hasMessageId = true;
                        break;
                    }
                }

                if (hasMessageId) {
                    break;
                }
            }
        }

        if (!hasMessageId) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1006",
                string.Format("[Command] type '{0}' is missing a 'MessageId' property. Add a string MessageId property (or inherit from a base record that provides it) so commands can be correlated end-to-end.", typeName),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        // HFC1016: every generated assignment target follows one uniform command-shape policy.
        // This includes derivable properties, even when their type requires a type-free soft-fail
        // renderer arm, so a command cannot move between assignment paths with an incompatible setter.
        bool hasInvalidAssignmentTarget = false;
        foreach (IPropertySymbol prop in assignmentTargetSymbols) {
            IMethodSymbol? setter = prop.SetMethod;
            bool isWritable = setter is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };
            if (!isWritable) {
                hasInvalidAssignmentTarget = true;
                string kind = setter is null
                    ? "has no public setter"
                    : setter.IsInitOnly
                        ? "is declared with an 'init' accessor"
                        : "has a non-public setter";
                Location? sourceLocation = prop.Locations.FirstOrDefault(static location => location.IsInSource);
                Microsoft.CodeAnalysis.Text.LinePosition diagnosticLine = sourceLocation is null
                    ? linePos
                    : sourceLocation.GetLineSpan().StartLinePosition;
                string diagnosticFilePath = sourceLocation?.SourceTree?.FilePath ?? filePath;

                diagnostics.Add(new DiagnosticInfo(
                    "HFC1016",
                    string.Format(
                        "[Command] type '{0}' property '{1}' {2}. FrontComposer command generation requires every command property to declare a public non-init setter. Change the property to '{{ get; set; }}'.",
                        typeName,
                        prop.Name,
                        kind),
                    "Error",
                    diagnosticFilePath,
                    diagnosticLine.Line,
                    diagnosticLine.Character));
            }
        }

        // HFC1011: hard error when total (derivable + non-derivable) public property count exceeds 200 — Story 2-2 Task 1.3a, Red-team RT-5.
        int totalCount = allBuilder.Count;
        if (totalCount > TotalPropertyHardLimit) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1011",
                string.Format(
                    "[Command] type '{0}' has {1} total public properties, exceeding the hard limit of {2}. Split the command into smaller aggregates.",
                    typeName,
                    totalCount,
                    TotalPropertyHardLimit),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        int nonDerivableCount = nonDerivableBuilder.Count;

        // Story 2-5 D1 / AC4 / HFC1021 — [Destructive] commands must have at least one non-derivable field
        // so the emitted renderer can open the destructive confirmation dialog (destructive never renders as
        // 0-field Inline button per UX-DR36).
        if (isDestructive && nonDerivableCount == 0) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1021",
                string.Format(
                    "Destructive command '{0}' must have at least one non-derivable property (destructive commands cannot render as inline buttons).",
                    typeName),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));

            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        if (nonDerivableCount > NonDerivableErrorThreshold) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1007",
                string.Format("[Command] type '{0}' has {1} non-derivable properties, exceeding the hard limit of {2}. Split the command into smaller aggregates.", typeName, nonDerivableCount, NonDerivableErrorThreshold),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
        }
        else if (nonDerivableCount > NonDerivableWarningThreshold) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1007",
                string.Format("[Command] type '{0}' has {1} non-derivable properties, above the recommended limit of {2}. Consider splitting the command for better form ergonomics.", typeName, nonDerivableCount, NonDerivableWarningThreshold),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (hasInvalidAssignmentTarget) {
            return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        var model = new CommandModel(
            typeName,
            ns,
            boundedContext,
            boundedContextDisplayLabel,
            displayName,
            new EquatableArray<PropertyModel>(allBuilder.ToImmutable()),
            new EquatableArray<PropertyModel>(derivableBuilder.ToImmutable()),
            new EquatableArray<PropertyModel>(nonDerivableBuilder.ToImmutable()),
            iconName,
            isDestructive,
            destructiveConfirmTitle,
            destructiveConfirmBody,
            authorizationPolicyName,
            filePath,
            linePos.Line,
            linePos.Character,
            commandTarget);

        return new CommandParseResult(
            model,
            new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutableArray()));
    }

    private static CommandTargetModel? ParseCommandTarget(
        INamedTypeSymbol typeSymbol,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        AttributeData? attribute = typeSymbol.GetAttributes().FirstOrDefault(
            candidate => candidate.AttributeClass?.ToDisplayString() == CommandTargetAttributeName);
        if (attribute is null) {
            return null;
        }

        if (attribute.ConstructorArguments.Length != 3
            || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol projectionType
            || attribute.ConstructorArguments[1].Value is not int resolutionValue
            || attribute.ConstructorArguments[2].Value is not int changeValue
            || !Enum.IsDefined(typeof(CommandTargetResolutionMode), resolutionValue)
            || !Enum.IsDefined(typeof(CommandTargetChangeKind), changeValue)
            || !projectionType.GetAttributes().Any(
                candidate => candidate.AttributeClass?.ToDisplayString() == ProjectionAttributeName)) {
            AddInvalidCommandTargetDiagnostic(typeSymbol, diagnostics, filePath, linePos);
            return null;
        }

        var resolutionMode = (CommandTargetResolutionMode)resolutionValue;
        var changeKind = (CommandTargetChangeKind)changeValue;
        if (resolutionMode == CommandTargetResolutionMode.SameAsSource
            && changeKind != CommandTargetChangeKind.Update) {
            AddInvalidCommandTargetDiagnostic(typeSymbol, diagnostics, filePath, linePos);
            return null;
        }

        string? viewKey = NormalizeNamedString(attribute, nameof(CommandTargetAttribute.ViewKey));
        string? expectedStatus = NormalizeNamedString(attribute, nameof(CommandTargetAttribute.ExpectedStatus));
        string projectionViewKey = ResolveProjectionViewKey(projectionType);
        if (viewKey is not null
            && !string.Equals(viewKey, projectionViewKey, StringComparison.Ordinal)) {
            AddInvalidCommandTargetDiagnostic(typeSymbol, diagnostics, filePath, linePos);
            return null;
        }

        return new CommandTargetModel(
            projectionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            resolutionMode,
            changeKind,
            viewKey,
            expectedStatus,
            projectionViewKey);
    }

    private static string ResolveProjectionViewKey(INamedTypeSymbol projectionType) {
        string projectionName = projectionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        const string GlobalPrefix = "global::";
        if (projectionName.StartsWith(GlobalPrefix, StringComparison.Ordinal)) {
            projectionName = projectionName.Substring(GlobalPrefix.Length);
        }

        string? boundedContext = ParseBoundedContext(projectionType, out _);
        string namespaceName = projectionType.ContainingNamespace?.IsGlobalNamespace == false
            ? projectionType.ContainingNamespace.ToDisplayString()
            : string.Empty;
        return (boundedContext ?? namespaceName) + ":" + projectionName;
    }

    private static string? NormalizeNamedString(AttributeData attribute, string name) {
        foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments) {
            if (argument.Key == name && argument.Value.Value is string value) {
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }

        return null;
    }

    private static void AddInvalidCommandTargetDiagnostic(
        INamedTypeSymbol typeSymbol,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) =>
        diagnostics.Add(new DiagnosticInfo(
            "HFC1005",
            string.Format(
                "Command '{0}' has an invalid CommandTarget declaration. The projection must be [Projection], enum values must be known, and SameAsSource is valid only with Update.",
                typeSymbol.Name),
            "Warning",
            filePath,
            linePos.Line,
            linePos.Character));

    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol typeSymbol) {
        foreach (IMethodSymbol ctor in typeSymbol.InstanceConstructors) {
            if (ctor.DeclaredAccessibility != Accessibility.Public) {
                continue;
            }

            if (ctor.Parameters.Length == 0) {
                return true;
            }

            // A ctor whose parameters all have default values (including positional record
            // primary ctors like `record Foo(string A = "", int B = 0)`) is callable as `new Foo()`.
            bool allOptional = true;
            foreach (IParameterSymbol parameter in ctor.Parameters) {
                if (!parameter.HasExplicitDefaultValue) {
                    allOptional = false;
                    break;
                }
            }

            if (allOptional) {
                return true;
            }
        }

        return false;
    }

    private static bool IsDerivableProperty(IPropertySymbol propertySymbol) {
        // Walk the property's inheritance chain (override / shadow / base positional params):
        // attributes placed on a base declaration must still be discoverable when a derived
        // record shadows the property (see code-review 2026-04-15, patch P15).
        IPropertySymbol? current = propertySymbol;
        while (current is not null) {
            foreach (AttributeData attr in current.GetAttributes()) {
                if (attr.AttributeClass?.ToDisplayString() == DerivedFromAttributeName) {
                    return true;
                }
            }

            current = current.OverriddenProperty ?? FindShadowedProperty(current);
        }

        return WellKnownDerivablePropertyNames.Contains(propertySymbol.Name);
    }

    private static IPropertySymbol? FindShadowedProperty(IPropertySymbol propertySymbol) {
        INamedTypeSymbol? baseType = propertySymbol.ContainingType?.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object) {
            foreach (ISymbol member in baseType.GetMembers(propertySymbol.Name)) {
                if (member is IPropertySymbol basePropertySymbol
                    && basePropertySymbol.DeclaredAccessibility == Accessibility.Public
                    && !basePropertySymbol.IsStatic
                    && !basePropertySymbol.IsIndexer) {
                    return basePropertySymbol;
                }
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    private static void ValidateTypeKind(
        INamedTypeSymbol typeSymbol,
        SyntaxNode targetNode,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        if (typeSymbol.TypeKind == TypeKind.Struct) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: structs are not supported. Only non-abstract, non-generic classes and records are supported.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (typeSymbol.IsAbstract) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: abstract types are not supported.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (typeSymbol.IsGenericType) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: generic types are not supported.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }

        if (typeSymbol.ContainingNamespace?.ToDisplayString().StartsWith("System", StringComparison.Ordinal) == true) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1004",
                string.Format("[Command] attribute on '{0}' is not supported: command types must not live in the 'System' namespace.", typeSymbol.Name),
                "Warning",
                filePath,
                linePos.Line,
                linePos.Character));
        }
    }

    private static string? ParseBoundedContext(INamedTypeSymbol typeSymbol, out string? displayLabel) {
        displayLabel = null;
        INamedTypeSymbol? current = typeSymbol;
        while (current is not null) {
            foreach (AttributeData attr in current.GetAttributes()) {
                if (attr.AttributeClass?.ToDisplayString() == BoundedContextAttributeName) {
                    foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                        if (namedArg.Key == "DisplayLabel"
                            && namedArg.Value.Value is string label
                            && !string.IsNullOrWhiteSpace(label)) {
                            displayLabel = label;
                        }
                    }

                    if (attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is string name
                        && !string.IsNullOrWhiteSpace(name)) {
                        return name.Trim();
                    }

                    return null;
                }
            }

            current = current.ContainingType;
        }

        return null;
    }

    private static string? ParseDisplayAttribute(INamedTypeSymbol typeSymbol) {
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == DisplayAttributeName) {
                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                    if (namedArg.Key == "Name" && namedArg.Value.Value is string name) {
                        return name;
                    }
                }
            }
        }

        return null;
    }

    private static string? ParseRequiresPolicyAttribute(
        INamedTypeSymbol typeSymbol,
        List<DiagnosticInfo> diagnostics,
        string filePath,
        Microsoft.CodeAnalysis.Text.LinePosition linePos) {
        List<AttributeData> attributes = [];
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == RequiresPolicyAttributeName) {
                attributes.Add(attr);
            }
        }

        if (attributes.Count == 0) {
            return null;
        }

        if (attributes.Count > 1) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1057",
                string.Format(
                    "Command '{0}' declares [RequiresPolicy] more than once. Expected at most one policy per command in v1.",
                    typeSymbol.Name),
                "Error",
                filePath,
                linePos.Line,
                linePos.Character));
            return null;
        }

        AttributeData policyAttribute = attributes[0];
        string? value = null;
        if (policyAttribute.ConstructorArguments.Length > 0) {
            TypedConstant ctorArg = policyAttribute.ConstructorArguments[0];
            // Defensive: typeof(...) / array constants surface as non-Primitive Kinds. The cast to
            // string then yields null and would silently drop policy metadata. Reject as HFC1056.
            if (ctorArg.Kind == TypedConstantKind.Primitive) {
                value = ctorArg.Value as string;
            }
        }

        if (string.IsNullOrWhiteSpace(value) || !IsPolicyNameWellFormed(value!)) {
            Microsoft.CodeAnalysis.Text.LinePosition attrLine = (policyAttribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)
                ?.GetLocation()
                .GetLineSpan()
                .StartLinePosition ?? linePos;

            // Pass 3 — echo the (escaped, truncated) offending value so authors can see WHICH character
            // failed without having to count chars in the source. Keeps message length bounded.
            string offending = FormatOffendingPolicyValue(value);
            diagnostics.Add(new DiagnosticInfo(
                "HFC1056",
                string.Format(
                    "Command '{0}' has an invalid [RequiresPolicy] value. Expected a non-empty policy name using letters, digits, '.', ':', '_', or '-' with at least one alphanumeric character. Got: {1}",
                    typeSymbol.Name,
                    offending),
                "Error",
                filePath,
                attrLine.Line,
                attrLine.Character));
            return null;
        }

        return value!.Trim();
    }

    private static bool IsPolicyNameWellFormed(string value) {
        string trimmed = value.Trim();
        if (trimmed.Length == 0) {
            return false;
        }

        bool hasAlphanumeric = false;
        foreach (char c in trimmed) {
            if (char.IsLetterOrDigit(c)) {
                hasAlphanumeric = true;
                continue;
            }

            if (c is not ('.' or ':' or '_' or '-')) {
                return false;
            }
        }

        // Pass 3 — punctuation-only names like "-", ".", ":", or "::" pass the per-character check
        // but produce no meaningful policy lookup. Mirror RequiresPolicyAttribute.IsWellFormed.
        return hasAlphanumeric;
    }

    private static string FormatOffendingPolicyValue(string? raw) {
        if (raw is null) {
            return "<null>";
        }

        const int MaxEcho = 64;
        string truncated = raw.Length > MaxEcho ? raw.Substring(0, MaxEcho) + "..." : raw;

        // Escape control characters so log readers see "\\t" / "\\n" rather than embedded whitespace.
        var sb = new System.Text.StringBuilder(truncated.Length + 2);
        _ = sb.Append('\'');
        foreach (char c in truncated) {
            switch (c) {
                case '\t': _ = sb.Append("\\t"); break;
                case '\r': _ = sb.Append("\\r"); break;
                case '\n': _ = sb.Append("\\n"); break;
                case '\\': _ = sb.Append("\\\\"); break;
                default:
                    if (char.IsControl(c)) {
                        _ = sb.Append("\\u").Append(((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else {
                        _ = sb.Append(c);
                    }

                    break;
            }
        }

        _ = sb.Append('\'');
        return sb.ToString();
    }

    /// <summary>
    /// Story 2-5 Task 2.2 — resolves <c>[Destructive]</c> (with optional named args
    /// <c>ConfirmationTitle</c> / <c>ConfirmationBody</c>) when declared on the command type.
    /// </summary>
    private static bool ParseDestructiveAttribute(
        INamedTypeSymbol typeSymbol,
        out string? confirmationTitle,
        out string? confirmationBody) {
        confirmationTitle = null;
        confirmationBody = null;

        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() != DestructiveAttributeName) {
                continue;
            }

            foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments) {
                if (namedArg.Key == "ConfirmationTitle"
                    && namedArg.Value.Value is string title
                    && !string.IsNullOrWhiteSpace(title)) {
                    confirmationTitle = title;
                }
                else if (namedArg.Key == "ConfirmationBody"
                    && namedArg.Value.Value is string body
                    && !string.IsNullOrWhiteSpace(body)) {
                    confirmationBody = body;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the <c>[Icon(IconName)]</c> attribute value when declared on the command type.
    /// Format validation is deferred to runtime (Story 2-2 Decision D34).
    /// </summary>
    private static string? ParseIconAttribute(INamedTypeSymbol typeSymbol) {
        foreach (AttributeData attr in typeSymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() == IconAttributeName
                && attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string iconName
                && !string.IsNullOrWhiteSpace(iconName)) {
                return iconName;
            }
        }

        return null;
    }

    /// <summary>
    /// HFC1012: emits a parse-time error when a property's <c>[DefaultValue(x)]</c> argument's runtime type is not
    /// assignable to the decorated property's declared type (Story 2-2 Task 1.3c, Chaos CM-1).
    /// Uses Roslyn's <see cref="TypedConstant"/> type to compare against the property type; nullable wrappers unwrapped.
    /// </summary>
    private static void ValidateDefaultValueType(IPropertySymbol propertySymbol, List<DiagnosticInfo> diagnostics, string filePath) {
        foreach (AttributeData attr in propertySymbol.GetAttributes()) {
            if (attr.AttributeClass?.ToDisplayString() != DefaultValueAttributeName) {
                continue;
            }

            if (attr.ConstructorArguments.Length == 0) {
                continue;
            }

            ITypeSymbol propertyType = propertySymbol.Type;
            // Unwrap Nullable<T> for comparison.
            if (propertyType is INamedTypeSymbol named && named.IsGenericType && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) {
                propertyType = named.TypeArguments[0];
            }

            bool assignable;
            string actualTypeDisplay;
            if (attr.AttributeConstructor?.Parameters is { Length: 2 } parameters
                && parameters[0].Type.ToDisplayString() == "System.Type"
                && parameters[1].Type.SpecialType == SpecialType.System_String
                && attr.ConstructorArguments[0].Value is ITypeSymbol declaredDefaultType) {
                assignable = IsDefaultValueTypeAssignable(declaredDefaultType, propertyType);
                actualTypeDisplay = declaredDefaultType.ToDisplayString();
            }
            else {
                TypedConstant arg = attr.ConstructorArguments[0];

                // Null is always assignable (reference / nullable types); skip the check.
                if (arg.IsNull) {
                    continue;
                }

                ITypeSymbol? argType = arg.Type;
                if (argType is null) {
                    continue;
                }

                assignable = IsDefaultValueTypeAssignable(argType, propertyType);
                actualTypeDisplay = argType.ToDisplayString();
            }

            if (!assignable) {
                var attrSyntax = attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
                Microsoft.CodeAnalysis.Text.LinePosition linePos = attrSyntax?.GetLocation().GetLineSpan().StartLinePosition ?? default;
                diagnostics.Add(new DiagnosticInfo(
                    "HFC1012",
                    string.Format(
                        "Property '{0}.{1}' has [DefaultValue] of type '{2}' which is not assignable to the property type '{3}'.",
                        propertySymbol.ContainingType?.Name ?? "?",
                        propertySymbol.Name,
                        actualTypeDisplay,
                        propertySymbol.Type.ToDisplayString()),
                    "Error",
                    filePath,
                    linePos.Line,
                    linePos.Character));
            }
        }
    }

    // ECMA-334 §10.2.3 implicit numeric conversion coverage (Story 2-2 code-review P26).
    private static bool IsDefaultValueTypeAssignable(ITypeSymbol candidateType, ITypeSymbol propertyType) {
        if (SymbolEqualityComparer.Default.Equals(candidateType, propertyType)) {
            return true;
        }

        SpecialType from = candidateType.SpecialType;
        SpecialType to = propertyType.SpecialType;
        return (from, to) switch {
            (SpecialType.System_SByte, SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Byte, SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Int16, SpecialType.System_Int32 or SpecialType.System_Int64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_UInt16, SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Int32, SpecialType.System_Int64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_UInt32, SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Int64 or SpecialType.System_UInt64, SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Char, SpecialType.System_UInt16 or SpecialType.System_Int32 and SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64
                or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
            (SpecialType.System_Single, SpecialType.System_Double) => true,
            _ => false,
        };
    }
}
