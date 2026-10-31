using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Routing;

/// <summary>Owner-approved offline route and intent declarations. Grants and runtime admission remain unchanged.</summary>
public static class TenantCatalogDeclarations
{
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    /// <summary>Actual owner request/result shapes and explicit surface policy; existing route and intent identities remain unchanged.</summary>
    public static IReadOnlyList<RouteOperationContract> Operations => Routes.Select(Operation).ToArray();
#endif

    /// <summary>Actual owner receiver methods, selected without changing declaration identities.</summary>
    public static IReadOnlyDictionary<string, string> ReceiverMethods => Routes.ToDictionary(route => route.RouteId,
        route => route.Kind switch { "command" => "process", "query" => "query", _ => "project" }, StringComparer.Ordinal);

#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    private static RouteOperationContract Operation(RouteContractDeclaration route)
    {
        var serializer = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new Serialization.TenantStatusJsonConverter(), new System.Text.Json.Serialization.JsonStringEnumConverter() },
        };
        bool internalOperation = route.Kind == "projection" || route.Execution!.RequiresDelegatedContext
            || route.MessageType == BootstrapGlobalAdmin.CommandType;
        string eligibility = route.MessageType switch
        {
            "set-global-administrator" or "remove-global-administrator" => "confirmation-required",
            "get-global-administrators" => "UI-only",
            _ => "agent-eligible",
        };
        Type request = route.Kind == "projection" ? typeof(EventStore.Contracts.Projections.ProjectionRequest)
            : typeof(TenantCatalogDeclarations).Assembly.GetType(route.Execution!.HandlerType, throwOnError: true)!;
        Type result = route.Kind switch
        {
            "command" => typeof(SubmitCommandResponse),
            "projection" => typeof(EventStore.Contracts.Projections.ProjectionResponse),
            _ => route.MessageType switch
            {
                "get-tenant" => typeof(TenantDetail),
                "get-tenant-users" => typeof(PaginatedResult<TenantMember>),
                "get-tenant-audit" => typeof(PaginatedResult<TenantAuditEntry>),
                "list-tenants" => typeof(PaginatedResult<TenantSummary>),
                "get-user-tenants" => typeof(PaginatedResult<UserTenantMembership>),
                "get-global-administrators" => typeof(PaginatedResult<GlobalAdministratorSummary>),
                _ when route.MessageType == ResolveTenantOwnerAuthority.QueryType => typeof(TenantOwnerAuthorityResult),
                _ when route.MessageType == ResolveTenantReadAuthority.QueryType => typeof(TenantReadAuthorityResult),
                _ => throw new InvalidOperationException($"Unknown Tenants query {route.RouteId}."),
            },
        };
        return RouteOperationSchema.Create(route, request, result, internalOperation ? "internal" : "public", eligibility, serializer,
            new System.Text.Json.Schema.JsonSchemaExporterOptions
            {
                TransformSchemaNode = (context, node) => context.TypeInfo.Type == typeof(Enums.TenantStatus)
                    ? new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "string", ["enum"] = new System.Text.Json.Nodes.JsonArray(Enum.GetValues<Enums.TenantStatus>()
                            .Select(status => System.Text.Json.JsonSerializer.SerializeToNode(status, serializer)).ToArray()),
                    } : node,
            });
    }
#endif
    /// <summary>Gets the twelve explicit MessageId v1, Mutation/24-hour command policies.</summary>
    public static IReadOnlyList<CommandIntentDeclaration> Commands { get; } = [
        Command<CreateTenant>("create", false, [new("name", "string"), new("description-is-null", "boolean"), new("description", "string")]),
        Command<UpdateTenant>("update", false, [new("name", "string"), new("description-is-null", "boolean"), new("description", "string")]),
        Command<DisableTenant>("disable", false, []),
        Command<EnableTenant>("enable", false, []),
        Command<AddUserToTenant>("add-user", false, [new("user-id", "string"), new("role", "string")]),
        Command<RemoveUserFromTenant>("remove-user", false, [new("user-id", "string")]),
        Command<ChangeUserRole>("change-user-role", false, [new("user-id", "string"), new("new-role", "string")]),
        Command<SetTenantConfiguration>("set-configuration", false, [new("key", "string"), new("value", "string")]),
        Command<RemoveTenantConfiguration>("remove-configuration", false, [new("key", "string")]),
        Command<BootstrapGlobalAdmin>("bootstrap", true, [new("user-id", "string"), new("acting-subject", "string")]),
        Command<SetGlobalAdministrator>("set", true, [new("user-id", "string"), new("acting-subject", "string")]),
        Command<RemoveGlobalAdministrator>("remove", true, [new("user-id", "string"), new("acting-subject", "string")])];

    /// <summary>Gets all six public queries and both internal computed owner-authority queries.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Queries { get; } = [
        Query<GetTenantQuery>(), Query<GetTenantUsersQuery>(), Query<GetTenantAuditQuery>(),
        Query<ListTenantsQuery>(), Query<GetUserTenantsQuery>(), Query<GetGlobalAdministratorsQuery>(),
        Computed<ResolveTenantOwnerAuthority>("resolve-tenant-owner-authority", ResolveTenantOwnerAuthority.QueryType),
        Computed<ResolveTenantReadAuthority>("resolve-tenant-read-authority", ResolveTenantReadAuthority.QueryType)];

    /// <summary>Gets callable dispatcher routes and their owned outputs, including non-callable index and audit outputs.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Projections { get; } = [
        Projection("tenants", ["tenants", "tenant-index", "tenant-audit"]),
        Projection("global-administrators", ["global-administrators", "global-administrator-audit"])];

    /// <summary>Gets the complete owner route inventory.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Routes { get; } = [.. Commands.Select(c => c.Route), .. Queries, .. Projections];

    private static CommandIntentDeclaration Command<T>(string permission, bool subject, IReadOnlyList<IdempotencyDescriptorField> fields) where T : ICommandContract
    {
        string operation = T.CommandType;
        return new(new(T.Domain + "-" + operation, "command", T.Domain, T.CommandType, null, "v1", 1,
            "workload", EventStoreOperationIdentifiers.DomainServiceProcess, new("platform:" + T.Domain, T.Domain + ":" + permission), [],
            new(typeof(T).FullName!, "clr-short-name-v1", [T.CommandType, typeof(T).Name], "domain-service", null, [])),
            $"hexalith:{T.Domain}:{operation}:intent-v1", $"{T.Domain}:{operation}", $"{T.Domain}:{operation}:policy-v1",
            new($"hexalith:{T.Domain}:{operation}:descriptor-v1", 1, [new("target", "string"), .. fields]),
            IdempotencyReplayRetentionTier.Mutation, subject);
    }

    private static RouteContractDeclaration Query<T>() where T : IQueryContract => new(T.Domain + "-" + T.QueryType, "query", T.Domain,
        T.QueryType, T.ProjectionType, "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
        new(typeof(T).FullName!, "exact", [T.QueryType], "domain-service", "projection", []));

    private static RouteContractDeclaration Computed<T>(string id, string wire) => new("tenants-" + id, "query", "tenants",
        wire, null, "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
        new(typeof(T).FullName!, "exact", [wire], "domain-service", "computed", [], RequiresDelegatedContext: true));

    private static RouteContractDeclaration Projection(string domain, string[] outputs) => new(domain + "-projection", "projection", domain,
        null, domain, "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new("Hexalith.Tenants.Projections.ProjectionDispatcher", "exact", [domain], "domain-service", null, outputs));
}
