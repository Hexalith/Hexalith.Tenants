using System.Reflection;
using System.Text.RegularExpressions;

using Hexalith.EventStore.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests.Queries;

public class QueryContractNamingTests {
    private static readonly Assembly ContractsAssembly = typeof(Contracts.Queries.GetTenantQuery).Assembly;

    private static readonly Regex KebabCaseRegex = new(@"^[a-z][a-z0-9-]*$", RegexOptions.Compiled);

    private static List<Type> GetQueryContractTypes() => ContractsAssembly
            .GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.GetInterfaces().Any(i => i == typeof(IQueryContract)))
            .ToList();

    private static string GetStaticProperty(Type type, string propertyName) {
        PropertyInfo? prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        _ = prop.ShouldNotBeNull($"Type '{type.Name}' is missing static property '{propertyName}'");
        object? value = prop.GetValue(null);
        _ = value.ShouldNotBeNull($"Type '{type.Name}'.{propertyName} returned null");
        return (string)value;
    }

    [Fact]
    public void All_IQueryContract_implementations_have_kebab_case_QueryType() {
        List<Type> types = GetQueryContractTypes();
        types.ShouldNotBeEmpty("No IQueryContract implementations found");

        foreach (Type type in types) {
            string queryType = GetStaticProperty(type, "QueryType");
            KebabCaseRegex.IsMatch(queryType)
                .ShouldBeTrue($"QueryType '{queryType}' on '{type.Name}' is not kebab-case");
        }
    }

    [Fact]
    public void All_IQueryContract_implementations_have_non_empty_Domain() {
        List<Type> types = GetQueryContractTypes();
        types.ShouldNotBeEmpty();

        foreach (Type type in types) {
            string domain = GetStaticProperty(type, "Domain");
            domain.ShouldNotBeNullOrWhiteSpace($"Domain on '{type.Name}' is empty");
            KebabCaseRegex.IsMatch(domain)
                .ShouldBeTrue($"Domain '{domain}' on '{type.Name}' is not kebab-case");
        }
    }

    [Fact]
    public void All_IQueryContract_implementations_have_valid_ProjectionType() {
        List<Type> types = GetQueryContractTypes();
        types.ShouldNotBeEmpty();

        foreach (Type type in types) {
            string projectionType = GetStaticProperty(type, "ProjectionType");
            projectionType.ShouldNotBeNullOrWhiteSpace($"ProjectionType on '{type.Name}' is empty");
            projectionType.Contains(':').ShouldBeFalse($"ProjectionType '{projectionType}' on '{type.Name}' contains colon");
            projectionType.Length.ShouldBeLessThanOrEqualTo(100, $"ProjectionType on '{type.Name}' exceeds 100 chars");
        }
    }

    [Fact]
    public void QueryType_values_are_unique() {
        List<Type> types = GetQueryContractTypes();
        types.ShouldNotBeEmpty();

        var queryTypes = types.Select(t => GetStaticProperty(t, "QueryType")).ToList();
        queryTypes.Count.ShouldBe(queryTypes.Distinct().Count(), "Duplicate QueryType values found");
    }

    [Fact]
    public void Exactly_5_IQueryContract_implementations_exist() {
        List<Type> types = GetQueryContractTypes();
        types.Count.ShouldBe(5, $"Expected 5 IQueryContract implementations, found {types.Count}: {string.Join(", ", types.Select(t => t.Name))}");
    }
}
