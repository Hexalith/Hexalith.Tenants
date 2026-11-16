using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// IR consumed by <c>CommandRendererEmitter</c> + <c>CommandPageEmitter</c>. Story 2-2 Task 4.1.
/// Sealed class with manual IEquatable per ADR-009.
/// </summary>
public sealed class CommandRendererModel : IEquatable<CommandRendererModel> {
    public CommandRendererModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        CommandDensity density,
        string? iconName,
        string displayLabel,
        string fullPageRoute,
        string commandFullyQualifiedName,
        EquatableArray<string> nonDerivablePropertyNames,
        EquatableArray<PropertyModel> derivableProperties,
        string formComponentName,
        string actionsWrapperName,
        string stateName,
        string subscriberTypeName,
        bool isDestructive = false,
        string? destructiveConfirmTitle = null,
        string? destructiveConfirmBody = null,
        string? authorizationPolicyName = null) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        Density = density;
        IconName = iconName;
        DisplayLabel = displayLabel;
        FullPageRoute = fullPageRoute;
        CommandFullyQualifiedName = commandFullyQualifiedName;
        NonDerivablePropertyNames = nonDerivablePropertyNames;
        DerivableProperties = derivableProperties;
        FormComponentName = formComponentName;
        ActionsWrapperName = actionsWrapperName;
        StateName = stateName;
        SubscriberTypeName = subscriberTypeName;
        IsDestructive = isDestructive;
        DestructiveConfirmTitle = destructiveConfirmTitle;
        DestructiveConfirmBody = destructiveConfirmBody;
        AuthorizationPolicyName = authorizationPolicyName;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public CommandDensity Density { get; }

    public string? IconName { get; }

    /// <summary>Humanized command name with trailing " Command" stripped (Decision D23).</summary>
    public string DisplayLabel { get; }

    /// <summary><c>/commands/{BC}/{CommandTypeName}</c> route for the FullPage page partial.</summary>
    public string FullPageRoute { get; }

    public string CommandFullyQualifiedName { get; }

    public EquatableArray<string> NonDerivablePropertyNames { get; }

    public EquatableArray<PropertyModel> DerivableProperties { get; }

    /// <summary>Name of the generated <c>{TypeName}Form</c> component the renderer delegates to.</summary>
    public string FormComponentName { get; }

    public string ActionsWrapperName { get; }

    public string StateName { get; }

    /// <summary>Name of the generated <c>{TypeName}LastUsedSubscriber</c> (Decision D28).</summary>
    public string SubscriberTypeName { get; }

    /// <summary>Story 2-5 D1 — true when the command is annotated <c>[Destructive]</c>.</summary>
    public bool IsDestructive { get; }

    /// <summary>Story 2-5 D1 — optional <c>[Destructive(ConfirmationTitle)]</c> override; null → <c>{DisplayLabel}?</c> fallback.</summary>
    public string? DestructiveConfirmTitle { get; }

    /// <summary>Story 2-5 D1 — optional <c>[Destructive(ConfirmationBody)]</c> override; null → localized default body.</summary>
    public string? DestructiveConfirmBody { get; }

    public string? AuthorizationPolicyName { get; }

    public bool Equals(CommandRendererModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && Density == other.Density
            && IconName == other.IconName
            && DisplayLabel == other.DisplayLabel
            && FullPageRoute == other.FullPageRoute
            && CommandFullyQualifiedName == other.CommandFullyQualifiedName
            && NonDerivablePropertyNames == other.NonDerivablePropertyNames
            && DerivableProperties == other.DerivableProperties
            && FormComponentName == other.FormComponentName
            && ActionsWrapperName == other.ActionsWrapperName
            && StateName == other.StateName
            && SubscriberTypeName == other.SubscriberTypeName
            && IsDestructive == other.IsDestructive
            && DestructiveConfirmTitle == other.DestructiveConfirmTitle
            && DestructiveConfirmBody == other.DestructiveConfirmBody
            && AuthorizationPolicyName == other.AuthorizationPolicyName;
    }

    public override bool Equals(object? obj) => Equals(obj as CommandRendererModel);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(13);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + Density.GetHashCode();
            hash = (hash * 31) + (IconName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DisplayLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FullPageRoute?.GetHashCode() ?? 0);
            hash = (hash * 31) + (CommandFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + NonDerivablePropertyNames.GetHashCode();
            hash = (hash * 31) + DerivableProperties.GetHashCode();
            hash = (hash * 31) + (FormComponentName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ActionsWrapperName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (StateName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SubscriberTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsDestructive.GetHashCode();
            hash = (hash * 31) + (DestructiveConfirmTitle?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DestructiveConfirmBody?.GetHashCode() ?? 0);
            hash = (hash * 31) + (AuthorizationPolicyName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
