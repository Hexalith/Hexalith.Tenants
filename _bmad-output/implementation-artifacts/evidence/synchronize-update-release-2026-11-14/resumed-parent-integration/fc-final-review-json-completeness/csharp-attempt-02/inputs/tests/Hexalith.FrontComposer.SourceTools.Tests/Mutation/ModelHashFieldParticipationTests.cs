using System.Globalization;
using System.Reflection;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects bounded cache-key field participation through real hash collections while allowing collisions.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ModelHashFieldParticipationTests
{
    /// <summary>Exercises supported object collections without pinning any numerical hash or requiring unique hashes.</summary>
    [Fact]
    public void EqualityDistinguishingTextAndCoordinateFieldsDoNotCompletelyCollapseTheCollection()
    {
        // These112 named scalar inputs are independent of the current Equals implementation.
        // Boolean/enum/array/nested-object parameters are outside this scalar sampling boundary.
        // CommandTargetModel.projectionViewKey belongs only to its internal constructor.
        // CommandModel sourceFilePath/sourceLine/sourceColumn are intentionally excluded from equality.
        (Type Type, string[] Fields, string[] ExcludedScalars)[] inventory = [
            (typeof(CommandTargetModel), ["projectionFullyQualifiedName", "viewKey", "expectedStatus"], []),
            (typeof(DomainModel), ["typeName", "namespace", "boundedContext", "boundedContextDisplayLabel", "projectionRole", "projectionRoleWhenState", "displayName", "displayGroupName", "sourceFilePath", "sourceLine", "sourceColumn", "emptyStateCtaCommandTypeName"], []),
            (typeof(CommandModel), ["typeName", "namespace", "boundedContext", "boundedContextDisplayLabel", "displayName", "iconName", "destructiveConfirmTitle", "destructiveConfirmBody", "authorizationPolicyName"], ["sourceFilePath", "sourceLine", "sourceColumn"]),
            (typeof(PropertyModel), ["name", "typeName", "displayName", "enumFullyQualifiedName", "unsupportedTypeFullyQualifiedName", "columnPriority", "fieldGroup", "description", "relativeTimeWindowDays", "sourceTypeName", "fieldGroupDescription"], []),
            (typeof(BadgeMappingEntry), ["enumMemberName", "slot"], []),
            (typeof(DiagnosticInfo), ["id", "message", "severity", "filePath", "line", "column"], []),
            (typeof(ProjectionTemplateMarkerInfo), ["templateTypeFullName", "templateNamespace", "templateTypeName", "projectionTypeFullName", "projectionNamespace", "projectionTypeName", "role", "expectedContractVersion", "filePath", "line", "column"], []),
            (typeof(ColumnModel), ["propertyName", "header", "formatHint", "priority", "fieldGroup", "description", "unsupportedTypeFullyQualifiedName", "relativeTimeWindowDays"], []),
            (typeof(CommandFluxorModel), ["typeName", "namespace", "stateName", "featureName", "actionsWrapperName", "reducersClassName", "commandFullyQualifiedName", "featureQualifiedName"], []),
            (typeof(CommandRendererModel), ["typeName", "namespace", "boundedContext", "iconName", "displayLabel", "fullPageRoute", "commandFullyQualifiedName", "formComponentName", "actionsWrapperName", "stateName", "subscriberTypeName", "destructiveConfirmTitle", "destructiveConfirmBody", "authorizationPolicyName"], []),
            (typeof(FluxorModel), ["typeName", "namespace", "stateName", "featureName"], []),
            (typeof(FormFieldModel), ["propertyName", "typeName", "staticLabel", "enumFullyQualifiedName", "fieldGroup", "description", "fieldGroupDescription"], []),
            (typeof(CommandFormModel), ["typeName", "namespace", "boundedContext", "commandFullyQualifiedName", "buttonLabel", "authorizationPolicyName"], []),
            (typeof(RazorModel), ["typeName", "namespace", "boundedContext", "entityLabel", "entityPluralLabel", "emptyStateCtaCommandName"], []),
            (typeof(RegistrationModel), ["boundedContext", "typeName", "namespace", "displayLabel", "authorizationPolicyName"], []),
        ];
        int exercised = 0;
        foreach ((Type type, string[] fields, string[] excludedScalars) in inventory)
        {
            ConstructorInfo constructor = type.GetConstructors().OrderByDescending(item => item.GetParameters().Length).First();
            ParameterInfo[] parameters = constructor.GetParameters();
            string[] actualScalars = parameters.Where(item => item.ParameterType == typeof(string) || item.ParameterType == typeof(int) || item.ParameterType == typeof(int?))
                .Select(item => item.Name!).Order(StringComparer.Ordinal).ToArray();
            actualScalars.ShouldBe(fields.Concat(excludedScalars).Order(StringComparer.Ordinal).ToArray(), type.Name + " scalar inventory requires an explicit decision for every field");
            object?[] basis = parameters.Select(item => Default(item.ParameterType)).ToArray();
            for (int field = 0; field < parameters.Length; field++)
            {
                Type fieldType = parameters[field].ParameterType;
                if (!fields.Contains(parameters[field].Name, StringComparer.Ordinal))
                {
                    continue;
                }
                object[] values = Enumerable.Range(1, 64).Select(index =>
                {
                    object?[] arguments = (object?[])basis.Clone();
                    arguments[field] = fieldType == typeof(string) ? "Value" + index.ToString(CultureInfo.InvariantCulture) : index;
                    return constructor.Invoke(arguments);
                }).ToArray();
                values.Skip(1).All(value => !object.Equals(values[0], value)).ShouldBeTrue(type.Name + "." + parameters[field].Name + " participates in equality");
                CountingModelComparer comparer = new();
                HashSet<object> collection = new(comparer);
                foreach (object value in values)
                {
                    collection.Add(value).ShouldBeTrue();
                }
                collection.Count.ShouldBe(values.Length);
                long completePairwiseScan = (long)values.Length * (values.Length - 1) / 2;
                comparer.Probes.ShouldBeLessThan(completePairwiseScan, type.Name + "." + parameters[field].Name + " must retain bounded field participation; collisions are allowed");
                exercised++;
            }
        }
        exercised.ShouldBeGreaterThan(0);
        exercised.ShouldBe(112);
    }

    private static object? Default(Type type) => type == typeof(string) ? "Baseline" : type == typeof(FieldDisplayFormat) ? FieldDisplayFormat.RelativeTime : type == typeof(int?) ? 7 : type.IsValueType ? Activator.CreateInstance(type) : null;
}
