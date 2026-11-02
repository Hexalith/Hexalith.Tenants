using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>Reads only the historical global-administrator audit stream.</summary>
public sealed class GetGlobalAdministratorAuditQueryHandler(
    IReadModelStore store,
    ITenantsGlobalAdministratorAuthority administratorAuthority,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetTenantAuditQueryHandler> logger,
    IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
    TimeProvider? timeProvider = null)
    : GetTenantAuditQueryHandler(store, administratorAuthority, cursorCodec, telemetry, logger, freshnessOptions, timeProvider)
{
    public static new Hexalith.EventStore.Contracts.Routing.RouteContractDeclaration CatalogDeclaration =>
        Hexalith.Tenants.Server.Routing.TenantCatalogCapabilities.HandlerQueries.Single(route => route.MessageType == GetGlobalAdministratorAuditQuery.QueryType);

    public override string QueryType => CatalogDeclaration.MessageType!;

    protected override string? RequiredStream => "global-administrators";
}
