using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Pure, equatable generator descriptor for an explicitly declared command target.
/// </summary>
public sealed class CommandTargetModel : IEquatable<CommandTargetModel> {
    public CommandTargetModel(
        string projectionFullyQualifiedName,
        CommandTargetResolutionMode resolutionMode,
        CommandTargetChangeKind changeKind,
        string? viewKey,
        string? expectedStatus)
        : this(projectionFullyQualifiedName, resolutionMode, changeKind, viewKey, expectedStatus, null) {
    }

    internal CommandTargetModel(
        string projectionFullyQualifiedName,
        CommandTargetResolutionMode resolutionMode,
        CommandTargetChangeKind changeKind,
        string? viewKey,
        string? expectedStatus,
        string? projectionViewKey) {
        ProjectionFullyQualifiedName = projectionFullyQualifiedName;
        ProjectionViewKey = projectionViewKey;
        ResolutionMode = resolutionMode;
        ChangeKind = changeKind;
        ViewKey = viewKey;
        ExpectedStatus = expectedStatus;
    }

    public string ProjectionFullyQualifiedName { get; }

    /// <summary>Gets the canonical generated view key for the declared projection.</summary>
    public string? ProjectionViewKey { get; }

    public CommandTargetResolutionMode ResolutionMode { get; }

    public CommandTargetChangeKind ChangeKind { get; }

    public string? ViewKey { get; }

    public string? ExpectedStatus { get; }

    public bool Equals(CommandTargetModel? other) =>
        other is not null
        && string.Equals(ProjectionFullyQualifiedName, other.ProjectionFullyQualifiedName, StringComparison.Ordinal)
        && string.Equals(ProjectionViewKey, other.ProjectionViewKey, StringComparison.Ordinal)
        && ResolutionMode == other.ResolutionMode
        && ChangeKind == other.ChangeKind
        && string.Equals(ViewKey, other.ViewKey, StringComparison.Ordinal)
        && string.Equals(ExpectedStatus, other.ExpectedStatus, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as CommandTargetModel);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(0);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + ProjectionFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + (ProjectionViewKey?.GetHashCode() ?? 0);
            hash = (hash * 31) + ResolutionMode.GetHashCode();
            hash = (hash * 31) + ChangeKind.GetHashCode();
            hash = (hash * 31) + (ViewKey?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ExpectedStatus?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
