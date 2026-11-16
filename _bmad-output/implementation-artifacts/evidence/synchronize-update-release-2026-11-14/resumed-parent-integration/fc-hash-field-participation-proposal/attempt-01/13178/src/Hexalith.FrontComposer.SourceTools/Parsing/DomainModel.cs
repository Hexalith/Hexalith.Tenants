using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hexalith.FrontComposer.SourceTools.Parsing;
/// <summary>
/// Intermediate representation of a [Projection]-annotated domain type,
/// produced by the Parse stage. Pure data -- no Roslyn symbol references.
/// </summary>
public sealed class DomainModel : IEquatable<DomainModel> {
    public DomainModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string? boundedContextDisplayLabel,
        string? projectionRole,
        EquatableArray<PropertyModel> properties,
        string? projectionRoleWhenState = null,
        string? displayName = null,
        string? displayGroupName = null,
        string? sourceFilePath = null,
        int sourceLine = -1,
        int sourceColumn = -1,
        string? emptyStateCtaCommandTypeName = null) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        BoundedContextDisplayLabel = boundedContextDisplayLabel;
        ProjectionRole = projectionRole;
        Properties = properties;
        ProjectionRoleWhenState = projectionRoleWhenState;
        DisplayName = displayName;
        DisplayGroupName = displayGroupName;
        SourceFilePath = sourceFilePath ?? string.Empty;
        SourceLine = sourceLine;
        SourceColumn = sourceColumn;
        EmptyStateCtaCommandTypeName = emptyStateCtaCommandTypeName;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public string? BoundedContextDisplayLabel { get; }

    public string? DisplayName { get; }

    public string? DisplayGroupName { get; }

    internal string SourceFilePath { get; }

    internal int SourceLine { get; }

    internal int SourceColumn { get; }

    public string? ProjectionRole { get; }

    public EquatableArray<PropertyModel> Properties { get; }

    /// <summary>
    /// Gets the optional command type name declared by <c>[ProjectionEmptyStateCta]</c>.
    /// Story 4-6 carries this as a string to keep the Contracts attribute trim-friendly and
    /// avoid Roslyn type-operation coupling in the parse stage.
    /// </summary>
    public string? EmptyStateCtaCommandTypeName { get; }

    /// <summary>
    /// Gets the raw CSV payload of <c>[ProjectionRole(..., WhenState = "A,B")]</c>
    /// (Story 4-1 D2 / D3). Null means no state filter was specified on the attribute;
    /// Transform stage owns the CSV split (single canonical parsing rule).
    /// </summary>
    public string? ProjectionRoleWhenState { get; }

    public bool Equals(DomainModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && BoundedContextDisplayLabel == other.BoundedContextDisplayLabel
            && DisplayName == other.DisplayName
            && DisplayGroupName == other.DisplayGroupName
            && SourceFilePath == other.SourceFilePath
            && SourceLine == other.SourceLine
            && SourceColumn == other.SourceColumn
            && EmptyStateCtaCommandTypeName == other.EmptyStateCtaCommandTypeName
            && ProjectionRole == other.ProjectionRole
            && ProjectionRoleWhenState == other.ProjectionRoleWhenState
            && Properties == other.Properties;
    }

    public override bool Equals(object? obj) => Equals(obj as DomainModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) - (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContextDisplayLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DisplayName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DisplayGroupName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SourceFilePath?.GetHashCode() ?? 0);
            hash = (hash * 31) + SourceLine.GetHashCode();
            hash = (hash * 31) + SourceColumn.GetHashCode();
            hash = (hash * 31) + (EmptyStateCtaCommandTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ProjectionRole?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ProjectionRoleWhenState?.GetHashCode() ?? 0);
            hash = (hash * 31) + Properties.GetHashCode();
            return hash;
        }
    }

}

/// <summary>
/// Classifies a command's rendering density based on its non-derivable property count
/// (Story 2-2 Decision D3). Computed at parse time and carried on <see cref="CommandModel"/>.
/// </summary>
public enum CommandDensity {
    /// <summary>0 or 1 non-derivable fields — renders as a FluentButton (optionally with a single-field FluentPopover).</summary>
    Inline,

    /// <summary>2 to 4 non-derivable fields — renders inside a FluentCard with expand-in-row scroll stabilization.</summary>
    CompactInline,

    /// <summary>5 or more non-derivable fields — renders as a routable FullPage component.</summary>
    FullPage,
}

/// <summary>
/// Intermediate representation of a [Command]-annotated type, produced by the
/// Parse stage. Pure data -- no Roslyn symbol references. The property collection
/// is split into derivable (infrastructure-sourced, hidden from the form) and
/// non-derivable (user-input, rendered as form fields) subsets.
/// </summary>
public sealed class CommandModel : IEquatable<CommandModel> {
    public CommandModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string? boundedContextDisplayLabel,
        string? displayName,
        EquatableArray<PropertyModel> properties,
        EquatableArray<PropertyModel> derivableProperties,
        EquatableArray<PropertyModel> nonDerivableProperties,
        string? iconName = null,
        bool isDestructive = false,
        string? destructiveConfirmTitle = null,
        string? destructiveConfirmBody = null,
        string? authorizationPolicyName = null,
        string? sourceFilePath = null,
        int sourceLine = -1,
        int sourceColumn = -1)
        : this(
            typeName,
            @namespace,
            boundedContext,
            boundedContextDisplayLabel,
            displayName,
            properties,
            derivableProperties,
            nonDerivableProperties,
            iconName,
            isDestructive,
            destructiveConfirmTitle,
            destructiveConfirmBody,
            authorizationPolicyName,
            sourceFilePath,
            sourceLine,
            sourceColumn,
            null) {
    }

    internal CommandModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string? boundedContextDisplayLabel,
        string? displayName,
        EquatableArray<PropertyModel> properties,
        EquatableArray<PropertyModel> derivableProperties,
        EquatableArray<PropertyModel> nonDerivableProperties,
        string? iconName,
        bool isDestructive,
        string? destructiveConfirmTitle,
        string? destructiveConfirmBody,
        string? authorizationPolicyName,
        string? sourceFilePath,
        int sourceLine,
        int sourceColumn,
        CommandTargetModel? commandTarget) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        BoundedContextDisplayLabel = boundedContextDisplayLabel;
        DisplayName = displayName;
        Properties = properties;
        DerivableProperties = derivableProperties;
        NonDerivableProperties = nonDerivableProperties;
        IconName = iconName;
        IsDestructive = isDestructive;
        DestructiveConfirmTitle = destructiveConfirmTitle;
        DestructiveConfirmBody = destructiveConfirmBody;
        AuthorizationPolicyName = authorizationPolicyName;
        SourceFilePath = sourceFilePath ?? string.Empty;
        SourceLine = sourceLine;
        SourceColumn = sourceColumn;
        CommandTarget = commandTarget;
        Density = ComputeDensity(nonDerivableProperties.Count);
    }

    /// <summary>Story 9-1 P22: file path of the command's primary declaration. Used by drift diagnostics for IDE squiggle locations. Empty when unavailable. Not part of equality.</summary>
    internal string SourceFilePath { get; }

    /// <summary>Story 9-1 P22: zero-based line of the command's primary declaration, or -1 when unavailable. Not part of equality.</summary>
    internal int SourceLine { get; }

    /// <summary>Story 9-1 P22: zero-based column of the command's primary declaration, or -1 when unavailable. Not part of equality.</summary>
    internal int SourceColumn { get; }

    /// <summary>
    /// Computes density from non-derivable property count (Story 2-2 AC1, Decision D3).
    /// Boundaries are specification-locked: count ≤ 1 → Inline; 2..4 → CompactInline; ≥ 5 → FullPage.
    /// </summary>
    public static CommandDensity ComputeDensity(int nonDerivableCount) {
        if (nonDerivableCount <= 1) {
            return CommandDensity.Inline;
        }

        if (nonDerivableCount <= 4) {
            return CommandDensity.CompactInline;
        }

        return CommandDensity.FullPage;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public string? BoundedContextDisplayLabel { get; }

    public string? DisplayName { get; }

    public EquatableArray<PropertyModel> Properties { get; }

    public EquatableArray<PropertyModel> DerivableProperties { get; }

    public EquatableArray<PropertyModel> NonDerivableProperties { get; }

    /// <summary>
    /// Gets the Fluent UI icon type-path fragment sourced from <c>[Icon(IconName)]</c>, or <see langword="null"/>
    /// when the attribute is absent. Format validation is deferred to runtime (Story 2-2 Decision D34).
    /// </summary>
    public string? IconName { get; }

    /// <summary>
    /// Gets the density classification derived from <see cref="NonDerivableProperties"/>'s length
    /// (Story 2-2 AC1, Decision D3). Participates in equality (ADR-009).
    /// </summary>
    public CommandDensity Density { get; }

    /// <summary>
    /// Gets a value indicating whether the command is annotated <c>[Destructive]</c>
    /// (Story 2-5 D1 / ADR-026). Drives renderer confirmation gating (D2) and HFC1021 validation.
    /// </summary>
    public bool IsDestructive { get; }

    /// <summary>Gets the optional <c>[Destructive(ConfirmationTitle)]</c> override; null → renderer falls back to <c>{DisplayLabel}?</c>.</summary>
    public string? DestructiveConfirmTitle { get; }

    /// <summary>Gets the optional <c>[Destructive(ConfirmationBody)]</c> override; null → renderer uses localized "This action cannot be undone.".</summary>
    public string? DestructiveConfirmBody { get; }

    /// <summary>
    /// Gets the optional command authorization policy declared by <c>[RequiresPolicy]</c>.
    /// Null means the command is unprotected by Story 7-3 policy metadata.
    /// </summary>
    public string? AuthorizationPolicyName { get; }

    /// <summary>Gets the explicit FC-NIP target descriptor, or null when the command is undeclared or invalid.</summary>
    public CommandTargetModel? CommandTarget { get; }

    public bool Equals(CommandModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && BoundedContextDisplayLabel == other.BoundedContextDisplayLabel
            && DisplayName == other.DisplayName
            && Properties == other.Properties
            && DerivableProperties == other.DerivableProperties
            && NonDerivableProperties == other.NonDerivableProperties
            && IconName == other.IconName
            && Density == other.Density
            && IsDestructive == other.IsDestructive
            && DestructiveConfirmTitle == other.DestructiveConfirmTitle
            && DestructiveConfirmBody == other.DestructiveConfirmBody
            && AuthorizationPolicyName == other.AuthorizationPolicyName
            && Equals(CommandTarget, other.CommandTarget);
    }

    public override bool Equals(object? obj) => Equals(obj as CommandModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContextDisplayLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DisplayName?.GetHashCode() ?? 0);
            hash = (hash * 31) + Properties.GetHashCode();
            hash = (hash * 31) + DerivableProperties.GetHashCode();
            hash = (hash * 31) + NonDerivableProperties.GetHashCode();
            hash = (hash * 31) + (IconName?.GetHashCode() ?? 0);
            hash = (hash * 31) + Density.GetHashCode();
            hash = (hash * 31) + IsDestructive.GetHashCode();
            hash = (hash * 31) + (DestructiveConfirmTitle?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DestructiveConfirmBody?.GetHashCode() ?? 0);
            hash = (hash * 31) + (AuthorizationPolicyName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (CommandTarget?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

/// <summary>
/// Result of parsing a [Command]-annotated type: the IR model (if valid) plus any diagnostics.
/// Mirrors <see cref="ParseResult"/> for the projection pipeline.
/// </summary>
public sealed class CommandParseResult : IEquatable<CommandParseResult> {
    public CommandParseResult(CommandModel? model, EquatableArray<DiagnosticInfo> diagnostics) {
        Model = model;
        Diagnostics = diagnostics;
    }

    public CommandModel? Model { get; }

    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public bool Equals(CommandParseResult? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        bool modelsEqual = (Model is null && other.Model is null)
            || (Model is not null && Model.Equals(other.Model));

        return modelsEqual && Diagnostics == other.Diagnostics;
    }

    public override bool Equals(object? obj) => Equals(obj as CommandParseResult);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Model?.GetHashCode() ?? 0);
            hash = (hash * 31) + Diagnostics.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Compile-time display format selected from Level 1 projection annotations.
/// </summary>
public enum FieldDisplayFormat {
    /// <summary>Use the existing type-derived formatter.</summary>
    Default,

    /// <summary>Render DateTime-like values as compact relative time within the configured window.</summary>
    RelativeTime,

    /// <summary>Render numeric values using the current culture currency format.</summary>
    Currency,
}

/// <summary>
/// IR representation of a single property on a [Projection]-annotated type.
/// </summary>
public sealed class PropertyModel : IEquatable<PropertyModel> {
    public PropertyModel(
        string name,
        string typeName,
        bool isNullable,
        bool isUnsupported,
        string? displayName,
        EquatableArray<BadgeMappingEntry> badgeMappings,
        string? enumFullyQualifiedName = null,
        string? unsupportedTypeFullyQualifiedName = null,
        EquatableArray<string> enumMemberNames = default,
        int? columnPriority = null,
        string? fieldGroup = null,
        string? description = null,
        FieldDisplayFormat displayFormat = FieldDisplayFormat.Default,
        int? relativeTimeWindowDays = null,
        string? sourceTypeName = null,
        EquatableArray<string> requiredExternAliases = default,
        bool supportsStaticAssignment = true,
        string? fieldGroupDescription = null)
        : this(
            name,
            typeName,
            isNullable,
            isUnsupported,
            displayName,
            badgeMappings,
            enumFullyQualifiedName,
            unsupportedTypeFullyQualifiedName,
            enumMemberNames,
            columnPriority,
            fieldGroup,
            description,
            displayFormat,
            relativeTimeWindowDays,
            isWritable: true,
            sourceTypeName: sourceTypeName ?? typeName,
            requiredExternAliases: requiredExternAliases,
            supportsStaticAssignment: supportsStaticAssignment,
            fieldGroupDescription: fieldGroupDescription) {
    }

    internal PropertyModel(
        string name,
        string typeName,
        bool isNullable,
        bool isUnsupported,
        string? displayName,
        EquatableArray<BadgeMappingEntry> badgeMappings,
        string? enumFullyQualifiedName,
        string? unsupportedTypeFullyQualifiedName,
        EquatableArray<string> enumMemberNames,
        int? columnPriority,
        string? fieldGroup,
        string? description,
        FieldDisplayFormat displayFormat,
        int? relativeTimeWindowDays,
        bool isWritable,
        string sourceTypeName,
        EquatableArray<string> requiredExternAliases,
        bool supportsStaticAssignment,
        string? fieldGroupDescription = null) {
        Name = name;
        TypeName = typeName;
        IsNullable = isNullable;
        IsUnsupported = isUnsupported;
        DisplayName = displayName;
        BadgeMappings = badgeMappings;
        EnumFullyQualifiedName = enumFullyQualifiedName;
        UnsupportedTypeFullyQualifiedName = unsupportedTypeFullyQualifiedName;
        EnumMemberNames = enumMemberNames;
        ColumnPriority = columnPriority;
        FieldGroup = fieldGroup;
        FieldGroupDescription = fieldGroupDescription;
        Description = description;
        DisplayFormat = displayFormat;
        RelativeTimeWindowDays = displayFormat == FieldDisplayFormat.RelativeTime ? relativeTimeWindowDays ?? 7 : null;
        IsWritable = isWritable;
        SourceTypeName = sourceTypeName;
        RequiredExternAliases = requiredExternAliases;
        SupportsStaticAssignment = supportsStaticAssignment;
    }

    public string Name { get; }

    public string TypeName { get; }

    public bool IsNullable { get; }

    public bool IsUnsupported { get; }

    public string? DisplayName { get; }

    /// <summary>
    /// Gets the developer-authored property description from <c>[Description]</c> or
    /// <c>[Display(Description=...)]</c>. Story 4-6 propagates this through Transform and
    /// Emit to DataGrid header help and detail captions.
    /// </summary>
    public string? Description { get; }

    public EquatableArray<BadgeMappingEntry> BadgeMappings { get; }

    /// <summary>
    /// Gets the fully qualified enum type name when <see cref="TypeName"/> is <c>"Enum"</c>.
    /// Used by command form emission to populate <c>FluentSelect</c> items.
    /// </summary>
    public string? EnumFullyQualifiedName { get; }

    /// <summary>
    /// Gets the original fully qualified type name when <see cref="IsUnsupported"/> is <see langword="true"/>.
    /// Surfaced to <c>FcFieldPlaceholder</c> so operators see the exact unsupported type.
    /// </summary>
    public string? UnsupportedTypeFullyQualifiedName { get; }

    /// <summary>
    /// Gets the enum members in declaration order when <see cref="EnumFullyQualifiedName"/> is set.
    /// Preserved through Transform so emitters can stabilize status ordering without relying on
    /// runtime numeric enum values.
    /// </summary>
    internal EquatableArray<string> EnumMemberNames { get; }

    /// <summary>
    /// Gets the declared <c>[ColumnPriority]</c> value (Story 4-4 T6.2 / D14). <see langword="null"/>
    /// when the property is unannotated; Transform stage materialises null as <see cref="int.MaxValue"/>
    /// for the stable sort and re-emits the priority on <see cref="Transforms.ColumnModel.Priority"/>.
    /// </summary>
    public int? ColumnPriority { get; }

    /// <summary>
    /// Gets the declared <c>[ProjectionFieldGroup]</c> group name (Story 4-5 T6.1 / D9).
    /// <see langword="null"/> when the property is unannotated; the Transform stage propagates to
    /// <see cref="Transforms.ColumnModel.FieldGroup"/> verbatim. Case-insensitive collision with
    /// the reserved catch-all label <c>"Additional details"</c> emits HFC1030 Information at
    /// parse stage (fail-soft pass-through).
    /// </summary>
    public string? FieldGroup { get; }

    /// <summary>Gets optional help text declared for the generated command-form group.</summary>
    public string? FieldGroupDescription { get; }

    /// <summary>
    /// Gets the Level 1 display format selected by <c>[RelativeTime]</c> or <c>[Currency]</c>.
    /// The value is UI-agnostic metadata; emitters choose the concrete rendering path.
    /// </summary>
    public FieldDisplayFormat DisplayFormat { get; }

    /// <summary>
    /// Gets the relative-time window in days when <see cref="DisplayFormat"/> is
    /// <see cref="FieldDisplayFormat.RelativeTime"/>.
    /// </summary>
    public int? RelativeTimeWindowDays { get; }

    /// <summary>
    /// Gets a value indicating whether generated code may assign this property while cloning a
    /// command for an adopter-provided target resolver. Kept internal because it is generator IR,
    /// not contract metadata.
    /// </summary>
    internal bool IsWritable { get; }

    /// <summary>
    /// Gets the Roslyn-free source syntax for the property's non-nullable assignment type.
    /// </summary>
    public string SourceTypeName { get; }

    /// <summary>
    /// Gets the sorted aliases that generated source must declare before referencing
    /// <see cref="SourceTypeName"/>.
    /// </summary>
    public EquatableArray<string> RequiredExternAliases { get; }

    /// <summary>
    /// Gets a value indicating whether generated safe code may name and assign the property.
    /// </summary>
    public bool SupportsStaticAssignment { get; }

    public bool Equals(PropertyModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Name == other.Name
            && TypeName == other.TypeName
            && IsNullable == other.IsNullable
            && IsUnsupported == other.IsUnsupported
            && DisplayName == other.DisplayName
            && BadgeMappings == other.BadgeMappings
            && EnumFullyQualifiedName == other.EnumFullyQualifiedName
            && UnsupportedTypeFullyQualifiedName == other.UnsupportedTypeFullyQualifiedName
            && EnumMemberNames == other.EnumMemberNames
            && ColumnPriority == other.ColumnPriority
            && FieldGroup == other.FieldGroup
            && FieldGroupDescription == other.FieldGroupDescription
            && Description == other.Description
            && DisplayFormat == other.DisplayFormat
            && RelativeTimeWindowDays == other.RelativeTimeWindowDays
            && IsWritable == other.IsWritable
            && SourceTypeName == other.SourceTypeName
            && RequiredExternAliases == other.RequiredExternAliases
            && SupportsStaticAssignment == other.SupportsStaticAssignment;
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Name?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsNullable.GetHashCode();
            hash = (hash * 31) + IsUnsupported.GetHashCode();
            hash = (hash * 31) + (DisplayName?.GetHashCode() ?? 0);
            hash = (hash * 31) + BadgeMappings.GetHashCode();
            hash = (hash * 31) + (EnumFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (UnsupportedTypeFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + EnumMemberNames.GetHashCode();
            hash = (hash * 31) + (ColumnPriority?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FieldGroup?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FieldGroupDescription?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Description?.GetHashCode() ?? 0);
            hash = (hash * 31) + DisplayFormat.GetHashCode();
            hash = (hash * 31) + (RelativeTimeWindowDays?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsWritable.GetHashCode();
            hash = (hash * 31) + (SourceTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + RequiredExternAliases.GetHashCode();
            hash = (hash * 31) + SupportsStaticAssignment.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Maps an enum member to a badge slot, extracted from [ProjectionBadge] on enum fields.
/// </summary>
public sealed class BadgeMappingEntry : IEquatable<BadgeMappingEntry> {
    public BadgeMappingEntry(string enumMemberName, string slot) {
        EnumMemberName = enumMemberName;
        Slot = slot;
    }

    public string EnumMemberName { get; }

    public string Slot { get; }

    public bool Equals(BadgeMappingEntry? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return EnumMemberName == other.EnumMemberName && Slot == other.Slot;
    }

    public override bool Equals(object? obj) => Equals(obj as BadgeMappingEntry);

    public override int GetHashCode() {
        unchecked {
            return (((17 * 31) + (EnumMemberName?.GetHashCode() ?? 0)) * 31) + (Slot?.GetHashCode() ?? 0);
        }
    }
}

/// <summary>
/// Result of the Parse stage: a domain model (if valid) plus any diagnostics.
/// Diagnostics travel as data because SourceProductionContext is only available in RegisterSourceOutput.
/// </summary>
public sealed class ParseResult : IEquatable<ParseResult> {
    public ParseResult(DomainModel? model, EquatableArray<DiagnosticInfo> diagnostics) {
        Model = model;
        Diagnostics = diagnostics;
    }

    public DomainModel? Model { get; }

    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public bool Equals(ParseResult? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        bool modelsEqual = (Model is null && other.Model is null)
            || (Model is not null && Model.Equals(other.Model));

        return modelsEqual && Diagnostics == other.Diagnostics;
    }

    public override bool Equals(object? obj) => Equals(obj as ParseResult);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Model?.GetHashCode() ?? 0);
            hash = (hash * 31) + Diagnostics.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Serializable diagnostic information -- no Roslyn Location references.
/// </summary>
public sealed class DiagnosticInfo : IEquatable<DiagnosticInfo> {
    public DiagnosticInfo(string id, string message, string severity, string filePath, int line, int column) {
        Id = id;
        Message = message;
        Severity = severity;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    public string Id { get; }

    public string Message { get; }

    public string Severity { get; }

    public string FilePath { get; }

    public int Line { get; }

    public int Column { get; }

    public bool Equals(DiagnosticInfo? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Id == other.Id
            && Message == other.Message
            && Severity == other.Severity
            && FilePath == other.FilePath
            && Line == other.Line
            && Column == other.Column;
    }

    public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Id?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Message?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Severity?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FilePath?.GetHashCode() ?? 0);
            hash = (hash * 31) + Line.GetHashCode();
            hash = (hash * 31) + Column.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Converts the serialized diagnostic coordinates back into a Roslyn location.
    /// </summary>
    /// <returns>The diagnostic location, or <see cref="Location.None"/> when unavailable.</returns>
    public Location ToLocation() {
        if (string.IsNullOrWhiteSpace(FilePath) || Line < 0 || Column < 0) {
            return Location.None;
        }

        LinePosition position = new(Line, Column);
        LinePositionSpan lineSpan = new(position, position);
        return Location.Create(FilePath, new TextSpan(0, 0), lineSpan);
    }
}
