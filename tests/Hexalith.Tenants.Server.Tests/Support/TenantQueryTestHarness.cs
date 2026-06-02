using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Tenants.Server.Tests.Support;

/// <summary>
/// Test seam that mirrors the production in-process query dispatch (<c>DomainQueryDispatcher</c> driven
/// by <c>TenantsQueryController</c>): it instantiates the five tenant query handlers over a mocked
/// <see cref="IReadModelStore"/> + <see cref="IQueryCursorCodec"/> and routes an envelope to the handler
/// whose domain and query type match, returning a failure result when none does — exactly as the runtime
/// dispatcher behaves. Replaces the retired direct <c>TenantsProjectionActor</c> instantiation in unit
/// tests.
/// </summary>
internal static class TenantQueryTestHarness {
    public static IReadOnlyList<TenantQueryHandlerBase> CreateHandlers(
        IReadModelStore store,
        IQueryCursorCodec cursorCodec,
        ILoggerFactory? loggerFactory = null,
        TenantTelemetry? telemetry = null) {
        ILoggerFactory factory = loggerFactory ?? NullLoggerFactory.Instance;
        // The convention-named diagnostics produce a source/meter named "Hexalith.EventStore.Domain.tenants"
        // regardless of instance, so a telemetry listener that filters by name observes these handlers'
        // emissions whether or not the caller supplied its own telemetry.
        TenantTelemetry domainTelemetry = telemetry ?? new TenantTelemetry(new EventStoreDomainDiagnostics("tenants"));
        return [
            new GetTenantQueryHandler(store, cursorCodec, domainTelemetry, factory.CreateLogger<GetTenantQueryHandler>()),
            new GetTenantUsersQueryHandler(store, cursorCodec, domainTelemetry, factory.CreateLogger<GetTenantUsersQueryHandler>()),
            new GetUserTenantsQueryHandler(store, cursorCodec, domainTelemetry, factory.CreateLogger<GetUserTenantsQueryHandler>()),
            new ListTenantsQueryHandler(store, cursorCodec, domainTelemetry, factory.CreateLogger<ListTenantsQueryHandler>()),
            new GetTenantAuditQueryHandler(store, cursorCodec, domainTelemetry, factory.CreateLogger<GetTenantAuditQueryHandler>()),
        ];
    }

    public static async Task<QueryResult> ExecuteAsync(
        IReadModelStore store,
        IQueryCursorCodec cursorCodec,
        QueryEnvelope envelope,
        CancellationToken cancellationToken = default,
        ILoggerFactory? loggerFactory = null,
        TenantTelemetry? telemetry = null) {
        TenantQueryHandlerBase? handler = CreateHandlers(store, cursorCodec, loggerFactory, telemetry)
            .FirstOrDefault(h =>
                string.Equals(h.Domain, envelope.Domain, StringComparison.OrdinalIgnoreCase)
                && string.Equals(h.QueryType, envelope.QueryType, StringComparison.OrdinalIgnoreCase));

        return handler is null
            ? QueryResult.Failure($"No query handler is registered for domain '{envelope.Domain}' query type '{envelope.QueryType}'.")
            : await handler.ExecuteAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
