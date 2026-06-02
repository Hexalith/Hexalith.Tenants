using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;

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
        ILoggerFactory? loggerFactory = null) {
        ILoggerFactory factory = loggerFactory ?? NullLoggerFactory.Instance;
        return [
            new GetTenantQueryHandler(store, cursorCodec, factory.CreateLogger<GetTenantQueryHandler>()),
            new GetTenantUsersQueryHandler(store, cursorCodec, factory.CreateLogger<GetTenantUsersQueryHandler>()),
            new GetUserTenantsQueryHandler(store, cursorCodec, factory.CreateLogger<GetUserTenantsQueryHandler>()),
            new ListTenantsQueryHandler(store, cursorCodec, factory.CreateLogger<ListTenantsQueryHandler>()),
            new GetTenantAuditQueryHandler(store, cursorCodec, factory.CreateLogger<GetTenantAuditQueryHandler>()),
        ];
    }

    public static async Task<QueryResult> ExecuteAsync(
        IReadModelStore store,
        IQueryCursorCodec cursorCodec,
        QueryEnvelope envelope,
        CancellationToken cancellationToken = default,
        ILoggerFactory? loggerFactory = null) {
        TenantQueryHandlerBase? handler = CreateHandlers(store, cursorCodec, loggerFactory)
            .FirstOrDefault(h =>
                string.Equals(h.Domain, envelope.Domain, StringComparison.OrdinalIgnoreCase)
                && string.Equals(h.QueryType, envelope.QueryType, StringComparison.OrdinalIgnoreCase));

        return handler is null
            ? QueryResult.Failure($"No query handler is registered for domain '{envelope.Domain}' query type '{envelope.QueryType}'.")
            : await handler.ExecuteAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
