using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Pins the McpCli decoration of the Tenants Contracts: one "tenants" module with String identifiers and the fixed
/// "system" tenant, exactly the nine approved Commands and five approved Queries, interface-only routing, and a
/// described, schema-valid payload for every exposed operation.
/// </summary>
public class McpCliDecorationTests {
    private static readonly Assembly ContractsAssembly = typeof(CreateTenant).Assembly;

    private static readonly Type[] ExpectedCommands =
    [
        typeof(AddUserToTenant),
        typeof(ChangeUserRole),
        typeof(CreateTenant),
        typeof(DisableTenant),
        typeof(EnableTenant),
        typeof(RemoveTenantConfiguration),
        typeof(RemoveUserFromTenant),
        typeof(SetTenantConfiguration),
        typeof(UpdateTenant),
    ];

    private static readonly Type[] ExpectedQueries =
    [
        typeof(GetTenantAuditQuery),
        typeof(GetTenantQuery),
        typeof(GetTenantUsersQuery),
        typeof(GetUserTenantsQuery),
        typeof(ListTenantsQuery),
    ];

    private static readonly Type[] ExcludedOperations =
    [
        typeof(BootstrapGlobalAdmin),
        typeof(SetGlobalAdministrator),
        typeof(RemoveGlobalAdministrator),
        typeof(GetGlobalAdministratorsQuery),
        typeof(ResolveTenantOwnerAuthority),
    ];

    private static readonly string[] PagingMembers = ["Cursor", "PageSize"];

    // Mirrors the McpCli payload contract: exact CLR names, unknown members rejected, enums by name only.
    private static readonly JsonSerializerOptions McpCliPayloadOptions = new() {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) },
    };

    public static TheoryData<Type> DecoratedOperations => new(ExpectedCommands.Concat(ExpectedQueries));

    [Fact]
    public void Contracts_assembly_declares_the_tenants_module() {
        HexalithModuleAttribute module = ContractsAssembly.GetCustomAttribute<HexalithModuleAttribute>().ShouldNotBeNull();

        module.Name.ShouldBe("tenants");
        module.IdentifierKind.ShouldBe(IdentifierKind.String);
        module.FixedTenant.ShouldBe("system");
        module.FixedTenant.ShouldBe(TenantIdentity.DefaultTenantId);
        module.WireTypeConvention.ShouldBe(WireTypeConvention.Explicit);
        module.SerializerOptionsProvider.ShouldBeNull();
        module.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Exactly_the_approved_commands_and_queries_are_decorated() {
        Type[] commands = ContractsAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<HexalithCommandAttribute>(inherit: false) is not null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        Type[] queries = ContractsAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false) is not null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

        commands.ShouldBe(ExpectedCommands);
        queries.ShouldBe(ExpectedQueries);
    }

    [Fact]
    public void Global_administrator_and_owner_authority_operations_stay_undecorated() {
        foreach (Type type in ExcludedOperations) {
            type.GetCustomAttribute<HexalithCommandAttribute>(inherit: false).ShouldBeNull(type.Name);
            type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false).ShouldBeNull(type.Name);
        }
    }

    [Fact]
    public void Commands_route_only_through_ICommandContract() {
        foreach (Type type in ExpectedCommands) {
            HexalithCommandAttribute command = type.GetCustomAttribute<HexalithCommandAttribute>(inherit: false).ShouldNotBeNull();

            typeof(ICommandContract).IsAssignableFrom(type).ShouldBeTrue(type.Name);
            command.Name.ShouldBeNull(type.Name);
            command.Domain.ShouldBeNull(type.Name);
            command.WireType.ShouldBeNull(type.Name);
            command.AggregateIdProperty.ShouldBeNull(type.Name);
            command.TenantProperty.ShouldBeNull(type.Name);
            command.CorrelationProperty.ShouldBeNull(type.Name);
            command.IdempotencyKeyProperty.ShouldBeNull(type.Name);
            command.ActorProperty.ShouldBeNull(type.Name);

            // The aggregate identifier stays the interface's TenantId projection and remains on the wire.
            PropertyInfo aggregateId = type.GetProperty(nameof(ICommandContract.AggregateId)).ShouldNotBeNull(type.Name);
            aggregateId.GetCustomAttribute<JsonIgnoreAttribute>().ShouldBeNull(type.Name);
        }
    }

    [Fact]
    public void Queries_route_through_IQueryContract_with_tenant_or_index_aggregates() {
        foreach (Type type in ExpectedQueries) {
            HexalithQueryAttribute query = type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false).ShouldNotBeNull();

            typeof(IQueryContract).IsAssignableFrom(type).ShouldBeTrue(type.Name);
            query.Name.ShouldBeNull(type.Name);
            query.Domain.ShouldBeNull(type.Name);
            query.WireType.ShouldBeNull(type.Name);
            query.ProjectionType.ShouldBeNull(type.Name);
            query.ProjectionActorType.ShouldBeNull(type.Name);
            query.TenantProperty.ShouldBeNull(type.Name);
        }

        foreach (Type type in new[] { typeof(GetTenantQuery), typeof(GetTenantUsersQuery), typeof(GetTenantAuditQuery) }) {
            HexalithQueryAttribute query = type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false)!;
            query.AggregateIdProperty.ShouldBe("TenantId", type.Name);
            query.AggregateId.ShouldBeNull(type.Name);
        }

        foreach (Type type in new[] { typeof(ListTenantsQuery), typeof(GetUserTenantsQuery) }) {
            HexalithQueryAttribute query = type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false)!;
            query.AggregateId.ShouldBe("index", type.Name);
            query.AggregateIdProperty.ShouldBeNull(type.Name);
        }
    }

    [Theory]
    [MemberData(nameof(DecoratedOperations))]
    public void Operation_descriptions_explain_more_than_the_type_name(Type type) {
        ArgumentNullException.ThrowIfNull(type);
        string description = GetDescription(type);

        description.ShouldNotBeNullOrWhiteSpace();
        string stem = type.Name.EndsWith("Query", StringComparison.Ordinal) ? type.Name[..^"Query".Length] : type.Name;
        string.Equals(LettersAndDigits(description), LettersAndDigits(stem), StringComparison.OrdinalIgnoreCase)
            .ShouldBeFalse($"{type.Name} description only repeats its name.");
        description.Length.ShouldBeGreaterThan(60, $"{type.Name} needs a description a reader unfamiliar with the source can act on.");
    }

    [Theory]
    [MemberData(nameof(DecoratedOperations))]
    public void Every_payload_member_is_described_and_identifiers_are_marked(Type type) {
        ArgumentNullException.ThrowIfNull(type);
        PropertyInfo[] members = GetPayloadMembers(type);
        members.ShouldNotBeEmpty();

        foreach (PropertyInfo member in members) {
            DescriptionAttribute description = member.GetCustomAttribute<DescriptionAttribute>()
                .ShouldNotBeNull($"{type.Name}.{member.Name} needs a [property: Description] attribute.");
            description.Description.ShouldNotBeNullOrWhiteSpace($"{type.Name}.{member.Name} needs a description.");
            bool marked = member.GetCustomAttribute<HexalithIdentifierAttribute>() is not null;
            marked.ShouldBe(
                member.Name.EndsWith("Id", StringComparison.Ordinal),
                $"{type.Name}.{member.Name}: exactly the identifier members carry HexalithIdentifier.");
            if (marked) {
                member.PropertyType.ShouldBe(typeof(string), $"{type.Name}.{member.Name} is a String-kind identifier.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(DecoratedOperations))]
    public void Every_example_is_a_valid_payload(Type type) {
        ArgumentNullException.ThrowIfNull(type);
        string example = GetExample(type).ShouldNotBeNull($"{type.Name} needs an example.");
        using JsonDocument document = JsonDocument.Parse(example);
        document.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);

        HashSet<string> members = GetPayloadMembers(type).Select(member => member.Name).ToHashSet(StringComparer.Ordinal);
        foreach (JsonProperty property in document.RootElement.EnumerateObject()) {
            members.ShouldContain(property.Name, $"{type.Name} example member {property.Name} is not a payload member.");
            PagingMembers.ShouldNotContain(property.Name, $"{type.Name} example must page through the envelope.");
        }

        if (typeof(ICommandContract).IsAssignableFrom(type)) {
            string[] constructorMembers = type.GetConstructors().Single(constructor => constructor.GetParameters().Length > 0)
                .GetParameters()
                .Select(parameter => parameter.Name!)
                .ToArray();
            foreach (string required in constructorMembers) {
                document.RootElement.TryGetProperty(required, out _).ShouldBeTrue($"{type.Name} example omits required member {required}.");
            }
        }

        object? payload = JsonSerializer.Deserialize(example, type, McpCliPayloadOptions);
        _ = payload.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(ListTenantsQuery))]
    [InlineData(typeof(GetTenantUsersQuery))]
    [InlineData(typeof(GetUserTenantsQuery))]
    [InlineData(typeof(GetTenantAuditQuery))]
    public void Retained_payload_paging_members_are_optional(Type type) {
        ArgumentNullException.ThrowIfNull(type);
        foreach (string name in PagingMembers) {
            PropertyInfo member = type.GetProperty(name).ShouldNotBeNull($"{type.Name}.{name} stays for the REST surface.");
            member.GetCustomAttribute<RequiredMemberAttribute>().ShouldBeNull($"{type.Name}.{name}");
            member.GetCustomAttribute<JsonRequiredAttribute>().ShouldBeNull($"{type.Name}.{name}");
            type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
                .ShouldNotContain(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase), $"{type.Name}.{name}");
        }

        _ = JsonSerializer.Deserialize(GetExample(type)!, type, McpCliPayloadOptions).ShouldNotBeNull();
    }

    private static string GetDescription(Type type)
        => type.GetCustomAttribute<HexalithCommandAttribute>(inherit: false)?.Description
            ?? type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false)!.Description;

    private static string? GetExample(Type type)
        => type.GetCustomAttribute<HexalithCommandAttribute>(inherit: false)?.Example
            ?? type.GetCustomAttribute<HexalithQueryAttribute>(inherit: false)?.Example;

    // Serialized input members: public instance properties with a setter (records expose init setters).
    private static PropertyInfo[] GetPayloadMembers(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is not null && property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Where(property => property.Name != "EqualityContract")
            .ToArray();

    private static string LettersAndDigits(string value) => string.Concat(value.Where(char.IsLetterOrDigit));
}
