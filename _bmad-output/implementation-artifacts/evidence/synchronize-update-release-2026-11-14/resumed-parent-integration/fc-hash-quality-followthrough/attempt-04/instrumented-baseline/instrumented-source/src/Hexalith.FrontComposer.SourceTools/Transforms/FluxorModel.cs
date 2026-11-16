namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Output model for Fluxor feature/actions/reducers generation.
/// </summary>
public sealed class FluxorModel : IEquatable<FluxorModel> {
    public FluxorModel(
        string typeName,
        string @namespace,
        string stateName,
        string featureName) {
        TypeName = typeName;
        Namespace = @namespace;
        StateName = stateName;
        FeatureName = featureName;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string StateName { get; }

    public string FeatureName { get; }

    public bool Equals(FluxorModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && StateName == other.StateName
            && FeatureName == other.FeatureName;
    }

    public override bool Equals(object? obj) => Equals(obj as FluxorModel);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(14);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (StateName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FeatureName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
