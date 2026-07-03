using System.Reflection;

using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests.Queries;

public sealed class QueryRestMetadataTests
{
    [Fact]
    public void TenantQueries_DefineRoutesAndBindablePayloadProperties()
    {
        AssertRoute<ListTenantsQuery>("");
        AssertBinding<ListTenantsQuery>(
            RestQueryBindingSource.Constant,
            "index",
            RestQueryBindingSource.None,
            null);
        AssertProperty<ListTenantsQuery>("Cursor", typeof(string));
        AssertProperty<ListTenantsQuery>("PageSize", typeof(int));
        ListTenantsQuery.QueryType.ShouldBe("list-tenants");
        ListTenantsQuery.Domain.ShouldBe("tenants");
        ListTenantsQuery.ProjectionType.ShouldBe("tenant-index");

        AssertRoute<GetTenantQuery>("{tenantId}");
        AssertBinding<GetTenantQuery>(
            RestQueryBindingSource.Route,
            "tenantId",
            RestQueryBindingSource.Route,
            "tenantId");
        AssertProperty<GetTenantQuery>("TenantId", typeof(string));
        GetTenantQuery.QueryType.ShouldBe("get-tenant");
        GetTenantQuery.Domain.ShouldBe("tenants");
        GetTenantQuery.ProjectionType.ShouldBe("tenants");

        AssertRoute<GetTenantUsersQuery>("{tenantId}/users");
        AssertBinding<GetTenantUsersQuery>(
            RestQueryBindingSource.Route,
            "tenantId",
            RestQueryBindingSource.Route,
            "tenantId");
        AssertProperty<GetTenantUsersQuery>("TenantId", typeof(string));
        AssertProperty<GetTenantUsersQuery>("Cursor", typeof(string));
        AssertProperty<GetTenantUsersQuery>("PageSize", typeof(int));
        GetTenantUsersQuery.QueryType.ShouldBe("get-tenant-users");
        GetTenantUsersQuery.Domain.ShouldBe("tenants");
        GetTenantUsersQuery.ProjectionType.ShouldBe("tenants");

        AssertRoute<GetTenantAuditQuery>("{tenantId}/audit");
        AssertBinding<GetTenantAuditQuery>(
            RestQueryBindingSource.Route,
            "tenantId",
            RestQueryBindingSource.Route,
            "tenantId");
        AssertProperty<GetTenantAuditQuery>("TenantId", typeof(string));
        AssertProperty<GetTenantAuditQuery>("From", typeof(DateTimeOffset?));
        AssertProperty<GetTenantAuditQuery>("To", typeof(DateTimeOffset?));
        AssertProperty<GetTenantAuditQuery>("Category", typeof(AuditEventCategory?));
        AssertProperty<GetTenantAuditQuery>("Cursor", typeof(string));
        AssertProperty<GetTenantAuditQuery>("PageSize", typeof(int));
        GetTenantAuditQuery.QueryType.ShouldBe("get-tenant-audit");
        GetTenantAuditQuery.Domain.ShouldBe("tenants");
        GetTenantAuditQuery.ProjectionType.ShouldBe("tenants");
    }

    [Fact]
    public void CrossRouteQueries_DefineAbsoluteRoutesAndBindablePayloadProperties()
    {
        AssertRoute<GetUserTenantsQuery>("~/api/users/{userId}/tenants");
        AssertBinding<GetUserTenantsQuery>(
            RestQueryBindingSource.Constant,
            "index",
            RestQueryBindingSource.Route,
            "userId");
        AssertProperty<GetUserTenantsQuery>("UserId", typeof(string));
        AssertProperty<GetUserTenantsQuery>("Cursor", typeof(string));
        AssertProperty<GetUserTenantsQuery>("PageSize", typeof(int));
        GetUserTenantsQuery.QueryType.ShouldBe("get-user-tenants");
        GetUserTenantsQuery.Domain.ShouldBe("tenants");
        GetUserTenantsQuery.ProjectionType.ShouldBe("tenant-index");

        AssertRoute<GetGlobalAdministratorsQuery>("~/api/global-administrators");
        AssertBinding<GetGlobalAdministratorsQuery>(
            RestQueryBindingSource.Constant,
            "global-administrators",
            RestQueryBindingSource.Constant,
            "global-administrators");
        AssertProperty<GetGlobalAdministratorsQuery>("Cursor", typeof(string));
        AssertProperty<GetGlobalAdministratorsQuery>("PageSize", typeof(int));
        GetGlobalAdministratorsQuery.QueryType.ShouldBe("get-global-administrators");
        GetGlobalAdministratorsQuery.Domain.ShouldBe("global-administrators");
        GetGlobalAdministratorsQuery.ProjectionType.ShouldBe("global-administrators");
    }

    private static void AssertRoute<TQuery>(string expectedTemplate)
    {
        RestRouteAttribute? route = typeof(TQuery).GetCustomAttribute<RestRouteAttribute>();
        route.ShouldNotBeNull();
        route.Verb.ShouldBe(RestVerb.Get);
        route.Template.ShouldBe(expectedTemplate);
    }

    private static void AssertBinding<TQuery>(
        RestQueryBindingSource aggregateSource,
        string aggregateValue,
        RestQueryBindingSource entitySource,
        string? entityValue)
    {
        RestQueryBindingAttribute? binding = typeof(TQuery).GetCustomAttribute<RestQueryBindingAttribute>();
        binding.ShouldNotBeNull();
        binding.AggregateSource.ShouldBe(aggregateSource);
        binding.AggregateValue.ShouldBe(aggregateValue);
        binding.EntitySource.ShouldBe(entitySource);
        binding.EntityValue.ShouldBe(entityValue);
    }

    private static void AssertProperty<TQuery>(string name, Type expectedType)
    {
        PropertyInfo? property = typeof(TQuery).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(expectedType);
        property.GetMethod.ShouldNotBeNull();
    }
}
