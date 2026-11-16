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
        Type[] types = [typeof(CommandTargetModel), typeof(DomainModel), typeof(CommandModel), typeof(PropertyModel),
            typeof(BadgeMappingEntry), typeof(DiagnosticInfo), typeof(ProjectionTemplateMarkerInfo), typeof(ColumnModel),
            typeof(CommandFluxorModel), typeof(CommandRendererModel), typeof(FluxorModel), typeof(FormFieldModel),
            typeof(CommandFormModel), typeof(RazorModel), typeof(RegistrationModel)];
        int exercised = 0;
        foreach (Type type in types)
        {
            ConstructorInfo constructor = type.GetConstructors().OrderByDescending(item => item.GetParameters().Length).First();
            ParameterInfo[] parameters = constructor.GetParameters();
            object?[] basis = parameters.Select(item => Default(item.ParameterType)).ToArray();
            for (int field = 0; field < parameters.Length; field++)
            {
                Type fieldType = parameters[field].ParameterType;
                if (fieldType != typeof(string) && fieldType != typeof(int) && fieldType != typeof(int?))
                {
                    continue;
                }
                object[] values = Enumerable.Range(1, 64).Select(index =>
                {
                    object?[] arguments = (object?[])basis.Clone();
                    arguments[field] = fieldType == typeof(string) ? "Value" + index.ToString(CultureInfo.InvariantCulture) : index;
                    return constructor.Invoke(arguments);
                }).ToArray();
                if (values.All(value => object.Equals(values[0], value)))
                {
                    continue;
                }
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
    }

    private static object? Default(Type type) => type == typeof(string) ? "Baseline" : type == typeof(FieldDisplayFormat) ? FieldDisplayFormat.RelativeTime : type == typeof(int?) ? 7 : type.IsValueType ? Activator.CreateInstance(type) : null;
}
