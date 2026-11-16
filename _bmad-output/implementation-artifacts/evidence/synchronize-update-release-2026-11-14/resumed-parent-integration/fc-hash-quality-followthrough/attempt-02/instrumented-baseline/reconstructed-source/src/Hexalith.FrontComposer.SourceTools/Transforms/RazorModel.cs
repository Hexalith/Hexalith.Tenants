using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Output model for Razor DataGrid component generation.
/// </summary>
public sealed class RazorModel : IEquatable<RazorModel> {
    public RazorModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        EquatableArray<ColumnModel> columns,
        ProjectionRenderStrategy strategy = ProjectionRenderStrategy.Default,
        EquatableArray<string> whenStates = default,
        string? entityLabel = null,
        string? entityPluralLabel = null,
        string? emptyStateCtaCommandName = null) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        Columns = columns;
        Strategy = strategy;
        WhenStates = whenStates;
        EntityLabel = entityLabel;
        EntityPluralLabel = entityPluralLabel;
        EmptyStateCtaCommandName = emptyStateCtaCommandName;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public EquatableArray<ColumnModel> Columns { get; }

    /// <summary>
    /// Gets the render strategy selected by <see cref="RazorModelTransform"/> (Story 4-1 D4).
    /// </summary>
    public ProjectionRenderStrategy Strategy { get; }

    /// <summary>
    /// Gets the trimmed, split, non-empty state-enum member names extracted from
    /// <c>[ProjectionRole(..., WhenState = "A,B")]</c> (Story 4-1 D3 / AC9). Empty array
    /// when the attribute specified no filter.
    /// </summary>
    public EquatableArray<string> WhenStates { get; }

    public string? EntityLabel { get; }

    public string? EntityPluralLabel { get; }

    public string? EmptyStateCtaCommandName { get; }

    public bool Equals(RazorModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && Strategy == other.Strategy
            && WhenStates == other.WhenStates
            && EntityLabel == other.EntityLabel
            && EntityPluralLabel == other.EntityPluralLabel
            && EmptyStateCtaCommandName == other.EmptyStateCtaCommandName
            && Columns == other.Columns;
    }

    public override bool Equals(object? obj) => Equals(obj as RazorModel);

    public override int GetHashCode() {
        PrivateHashActivity.Record(17);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + Strategy.GetHashCode();
            hash = (hash * 31) + WhenStates.GetHashCode();
            hash = (hash * 31) + (EntityLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (EntityPluralLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (EmptyStateCtaCommandName?.GetHashCode() ?? 0);
            hash = (hash * 31) + Columns.GetHashCode();
            return hash;
        }
    }
}
