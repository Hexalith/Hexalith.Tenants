using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Output model for a single DataGrid column, derived from a PropertyModel.
/// </summary>
public sealed class ColumnModel : IEquatable<ColumnModel> {
    public ColumnModel(
        string propertyName,
        string header,
        TypeCategory typeCategory,
        string? formatHint,
        bool isNullable,
        EquatableArray<BadgeMappingEntry> badgeMappings,
        EquatableArray<string> enumMemberNames = default,
        int? priority = null,
        string? fieldGroup = null,
        string? description = null,
        string? unsupportedTypeFullyQualifiedName = null,
        FieldDisplayFormat displayFormat = FieldDisplayFormat.Default,
        int? relativeTimeWindowDays = null) {
        PropertyName = propertyName;
        Header = header;
        TypeCategory = typeCategory;
        FormatHint = formatHint;
        IsNullable = isNullable;
        BadgeMappings = badgeMappings;
        EnumMemberNames = enumMemberNames;
        Priority = priority;
        FieldGroup = fieldGroup;
        Description = description;
        UnsupportedTypeFullyQualifiedName = unsupportedTypeFullyQualifiedName;
        DisplayFormat = displayFormat;
        RelativeTimeWindowDays = displayFormat == FieldDisplayFormat.RelativeTime ? relativeTimeWindowDays ?? 7 : null;
    }

    public string PropertyName { get; }

    public string Header { get; }

    public TypeCategory TypeCategory { get; }

    public string? FormatHint { get; }

    public bool IsNullable { get; }

    public EquatableArray<BadgeMappingEntry> BadgeMappings { get; }

    public EquatableArray<string> EnumMemberNames { get; }

    /// <summary>
    /// Story 4-4 T6.3 / D14 / D17 — declared <c>[ColumnPriority]</c> value carried through from
    /// <see cref="PropertyModel.ColumnPriority"/>. <see langword="null"/> materialises as
    /// <see cref="int.MaxValue"/> at sort time; the Transform stage applies
    /// <c>(Priority ?? int.MaxValue, DeclarationOrder)</c> stable sort.
    /// </summary>
    public int? Priority { get; }

    /// <summary>
    /// Story 4-5 T6.2 / D9 — declared <c>[ProjectionFieldGroup]</c> group name carried through from
    /// <see cref="PropertyModel.FieldGroup"/>. <see langword="null"/> when the property is unannotated;
    /// emitter renders ungrouped properties inside the catch-all "Additional details" accordion.
    /// </summary>
    public string? FieldGroup { get; }

    /// <summary>
    /// Gets the developer-authored field description surfaced as contextual help in
    /// generated projection views (Story 4-6).
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the original fully qualified type name when <see cref="TypeCategory"/> is
    /// <see cref="TypeCategory.Unsupported"/>. Used by placeholder cell emission.
    /// </summary>
    public string? UnsupportedTypeFullyQualifiedName { get; }

    /// <summary>
    /// Gets the Level 1 display format selected during Parse/Transform.
    /// </summary>
    public FieldDisplayFormat DisplayFormat { get; }

    /// <summary>
    /// Gets the relative-time window in days for relative-time columns.
    /// </summary>
    public int? RelativeTimeWindowDays { get; }

    /// <summary>
    /// Story 4-3 D14 — derived gate for column-header filter affordance. True for
    /// Text / Numeric / Enum / DateTime; false for Boolean / Collection / Unsupported.
    /// Derived from <see cref="TypeCategory"/> so IR byte-stability is preserved
    /// (no ctor parameter, no additional equality contribution).
    /// </summary>
    public bool SupportsFilter => TypeCategory switch {
        TypeCategory.Text => true,
        TypeCategory.Numeric => false,
        TypeCategory.Enum => true,
        TypeCategory.DateTime => true,
        _ => false,
    };

    public bool Equals(ColumnModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return PropertyName == other.PropertyName
            && Header == other.Header
            && TypeCategory == other.TypeCategory
            && FormatHint == other.FormatHint
            && IsNullable == other.IsNullable
            && BadgeMappings == other.BadgeMappings
            && EnumMemberNames == other.EnumMemberNames
            && Priority == other.Priority
            && FieldGroup == other.FieldGroup
            && Description == other.Description
            && UnsupportedTypeFullyQualifiedName == other.UnsupportedTypeFullyQualifiedName
            && DisplayFormat == other.DisplayFormat
            && RelativeTimeWindowDays == other.RelativeTimeWindowDays;
    }

    public override bool Equals(object? obj) => Equals(obj as ColumnModel);

    public override int GetHashCode() {
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (PropertyName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Header?.GetHashCode() ?? 0);
            hash = (hash * 31) + TypeCategory.GetHashCode();
            hash = (hash * 31) + (FormatHint?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsNullable.GetHashCode();
            hash = (hash * 31) + BadgeMappings.GetHashCode();
            hash = (hash * 31) + EnumMemberNames.GetHashCode();
            hash = (hash * 31) + (Priority?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FieldGroup?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Description?.GetHashCode() ?? 0);
            hash = (hash * 31) + (UnsupportedTypeFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + DisplayFormat.GetHashCode();
            hash = (hash * 31) + (RelativeTimeWindowDays?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
