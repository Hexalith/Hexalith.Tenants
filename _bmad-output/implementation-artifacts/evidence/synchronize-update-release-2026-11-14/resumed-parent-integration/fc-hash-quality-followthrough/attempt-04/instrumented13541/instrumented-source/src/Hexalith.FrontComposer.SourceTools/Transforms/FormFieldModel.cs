using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Output model describing a single rendered field on an auto-generated command form.
/// </summary>
public sealed class FormFieldModel : IEquatable<FormFieldModel> {
    public FormFieldModel(
        string propertyName,
        string typeName,
        FormFieldTypeCategory typeCategory,
        string staticLabel,
        bool isNullable,
        bool isRequired,
        string? enumFullyQualifiedName,
        bool hasExplicitDisplayName = false,
        string? fieldGroup = null,
        string? description = null,
        string? fieldGroupDescription = null)
        : this(
            propertyName,
            typeName,
            typeCategory,
            staticLabel,
            isNullable,
            isRequired,
            enumFullyQualifiedName,
            hasExplicitDisplayName,
            isProviderCloneAssignable: true,
            fieldGroup,
            description,
            fieldGroupDescription) {
    }

    internal FormFieldModel(
        string propertyName,
        string typeName,
        FormFieldTypeCategory typeCategory,
        string staticLabel,
        bool isNullable,
        bool isRequired,
        string? enumFullyQualifiedName,
        bool hasExplicitDisplayName,
        bool isProviderCloneAssignable,
        string? fieldGroup = null,
        string? description = null,
        string? fieldGroupDescription = null) {
        PropertyName = propertyName;
        TypeName = typeName;
        TypeCategory = typeCategory;
        StaticLabel = staticLabel;
        IsNullable = isNullable;
        IsRequired = isRequired;
        EnumFullyQualifiedName = enumFullyQualifiedName;
        HasExplicitDisplayName = hasExplicitDisplayName;
        IsProviderCloneAssignable = isProviderCloneAssignable;
        FieldGroup = fieldGroup;
        FieldGroupDescription = fieldGroupDescription;
        Description = description;
    }

    /// <summary>Gets the .NET property name (e.g., <c>Amount</c>).</summary>
    public string PropertyName { get; }

    /// <summary>Gets the IR type name produced by the parser (e.g., <c>Int32</c>, <c>Enum</c>).</summary>
    public string TypeName { get; }

    /// <summary>Gets the form-field rendering category.</summary>
    public FormFieldTypeCategory TypeCategory { get; }

    /// <summary>
    /// Gets the compile-time label (from <c>[Display(Name)]</c>, humanized CamelCase, or raw name).
    /// Runtime <c>IStringLocalizer</c> overrides this at render time when available.
    /// </summary>
    public string StaticLabel { get; }

    /// <summary>Gets a value indicating whether the property type is nullable.</summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets a value indicating whether the field is required at the UI layer. Derived from
    /// "not nullable" for reference types plus any additional signals.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the fully qualified enum type name when <see cref="TypeCategory"/> is
    /// <see cref="FormFieldTypeCategory.Select"/>, otherwise <see langword="null"/>.
    /// </summary>
    public string? EnumFullyQualifiedName { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="StaticLabel"/> came from an explicit
    /// <c>[Display(Name = "...")]</c> attribute. When <see langword="true"/>, AC3
    /// mandates that the emitted <c>ResolveLabel</c> helper return the static label
    /// directly, bypassing the runtime <see cref="IStringLocalizer"/> lookup.
    /// Patch 2026-04-16 P-09.
    /// </summary>
    public bool HasExplicitDisplayName { get; }

    /// <summary>Gets the optional declared field-group name.</summary>
    public string? FieldGroup { get; }

    /// <summary>Gets optional operator-facing help text for the declared field group.</summary>
    public string? FieldGroupDescription { get; }

    /// <summary>Gets optional operator-facing help text for the field.</summary>
    public string? Description { get; }

    internal bool IsProviderCloneAssignable { get; }

    public bool Equals(FormFieldModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return PropertyName == other.PropertyName
            && TypeName == other.TypeName
            && TypeCategory == other.TypeCategory
            && StaticLabel == other.StaticLabel
            && IsNullable == other.IsNullable
            && IsRequired == other.IsRequired
            && EnumFullyQualifiedName == other.EnumFullyQualifiedName
            && HasExplicitDisplayName == other.HasExplicitDisplayName
            && IsProviderCloneAssignable == other.IsProviderCloneAssignable
            && FieldGroup == other.FieldGroup
            && FieldGroupDescription == other.FieldGroupDescription
            && Description == other.Description;
    }

    public override bool Equals(object? obj) => Equals(obj as FormFieldModel);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(16);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (PropertyName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + TypeCategory.GetHashCode();
            hash = (hash * 31) + (StaticLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsNullable.GetHashCode();
            hash = (hash * 31) + IsRequired.GetHashCode();
            hash = (hash * 31) + (EnumFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + HasExplicitDisplayName.GetHashCode();
            hash = (hash * 31) + IsProviderCloneAssignable.GetHashCode();
            hash = (hash * 31) + (FieldGroup?.GetHashCode() ?? 0);
            hash = (hash * 31) + (FieldGroupDescription?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Description?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

/// <summary>
/// Output model for command form component generation. Mirrors <see cref="RazorModel"/>
/// for the projection pipeline.
/// </summary>
public sealed class CommandFormModel : IEquatable<CommandFormModel> {
    public CommandFormModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string commandFullyQualifiedName,
        string buttonLabel,
        EquatableArray<FormFieldModel> fields,
        string? authorizationPolicyName = null)
        : this(
            typeName,
            @namespace,
            boundedContext,
            commandFullyQualifiedName,
            buttonLabel,
            fields,
            fields,
            authorizationPolicyName,
            null) {
    }

    internal CommandFormModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string commandFullyQualifiedName,
        string buttonLabel,
        EquatableArray<FormFieldModel> fields,
        string? authorizationPolicyName,
        CommandTargetModel? commandTarget)
        : this(
            typeName,
            @namespace,
            boundedContext,
            commandFullyQualifiedName,
            buttonLabel,
            fields,
            fields,
            authorizationPolicyName,
            commandTarget) {
    }

    internal CommandFormModel(
        string typeName,
        string @namespace,
        string? boundedContext,
        string commandFullyQualifiedName,
        string buttonLabel,
        EquatableArray<FormFieldModel> fields,
        EquatableArray<FormFieldModel> providerCloneFields,
        string? authorizationPolicyName,
        CommandTargetModel? commandTarget = null) {
        TypeName = typeName;
        Namespace = @namespace;
        BoundedContext = boundedContext;
        CommandFullyQualifiedName = commandFullyQualifiedName;
        ButtonLabel = buttonLabel;
        Fields = fields;
        ProviderCloneFields = providerCloneFields;
        AuthorizationPolicyName = authorizationPolicyName;
        CommandTarget = commandTarget;
    }

    public string TypeName { get; }

    public string Namespace { get; }

    public string? BoundedContext { get; }

    public string CommandFullyQualifiedName { get; }

    public string ButtonLabel { get; }

    public EquatableArray<FormFieldModel> Fields { get; }

    internal EquatableArray<FormFieldModel> ProviderCloneFields { get; }

    public string? AuthorizationPolicyName { get; }

    public CommandTargetModel? CommandTarget { get; }

    public bool Equals(CommandFormModel? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return TypeName == other.TypeName
            && Namespace == other.Namespace
            && BoundedContext == other.BoundedContext
            && CommandFullyQualifiedName == other.CommandFullyQualifiedName
            && ButtonLabel == other.ButtonLabel
            && Fields == other.Fields
            && ProviderCloneFields == other.ProviderCloneFields
            && AuthorizationPolicyName == other.AuthorizationPolicyName
            && Equals(CommandTarget, other.CommandTarget);
    }

    public override bool Equals(object? obj) => Equals(obj as CommandFormModel);

    public override int GetHashCode() {
        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(15);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (BoundedContext?.GetHashCode() ?? 0);
            hash = (hash * 31) + (CommandFullyQualifiedName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ButtonLabel?.GetHashCode() ?? 0);
            hash = (hash * 31) + Fields.GetHashCode();
            hash = (hash * 31) + ProviderCloneFields.GetHashCode();
            hash = (hash * 31) + (AuthorizationPolicyName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (CommandTarget?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
