namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Output model for command-lifecycle Fluxor feature, actions, and reducers generation.
/// </summary>
/// <remarks>
/// Unlike the projection Fluxor pipeline (Load/Loaded/LoadFailed), command features represent the
/// 5-state lifecycle: Idle -&gt; Submitting -&gt; Acknowledged -&gt; Syncing -&gt; Confirmed / Rejected.
/// </remarks>
public sealed class CommandFluxorModel : IEquatable<CommandFluxorModel> {
    public CommandFluxorModel(
        string typeName,
        string @namespace,
        string stateName,
        string featureName,
        string actionsWrapperName,
        string reducersClassName,
        string commandFullyQualifiedName,
        string featureQualifiedName) {
        TypeName = typeName;
        Namespace = @namespace;
        StateName = stateName;
        FeatureName = featureName;
        ActionsWrapperName = actionsWrapperName;
        ReducersClassName = reducersClassName;
        CommandFullyQualifiedName = commandFullyQualifiedName;
        FeatureQualifiedName = featureQualifiedName;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string StateName { get; }

    public string FeatureName { get; }

    public string ActionsWrapperName { get; }

    public string ReducersClassName { get; }

    public string CommandFullyQualifiedName { get; }

    /// <summary>
    /// Gets the fully qualified feature name used by <see cref="Fluxor.Feature{T}.GetName"/>
    /// (Decision D14: <c>{Namespace}.{TypeName}LifecycleState</c>).
    /// </summary>
    public string FeatureQualifiedName { get; }

    public bool Equals(CommandFluxorModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && StateName == other.StateName
            && FeatureName == other.FeatureName
            && ActionsWrapperName == other.ActionsWrapperName
            && ReducersClassName == other.ReducersClassName
            && CommandFullyQualifiedName == other.CommandFullyQualifiedName
            && FeatureQualifiedName == other.FeatureQualifiedName;
    }

    public override bool Equals(object? obj) => Equals(obj as CommandFluxorModel);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(12);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (StateName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FeatureName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ActionsWrapperName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ReducersClassName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (CommandFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FeatureQualifiedName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
