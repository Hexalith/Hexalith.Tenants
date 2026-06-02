using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>list-tenants</c> query: the paginated set of tenants visible to the authenticated user
/// (all tenants for a global administrator, otherwise the user's own tenants).
/// </summary>
public sealed class ListTenantsQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    ILogger<ListTenantsQueryHandler> logger)
    : TenantQueryHandlerBase(store, cursorCodec, logger) {
    /// <inheritdoc/>
    public override string QueryType => ListTenantsQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.ListTenants(envelope.UserId);
        if (!CursorCodec.TryDecode(protectedCursor, ListTenantsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(ListTenantsQuery.QueryType, "list-tenants", envelope.AggregateId, envelope.UserId, failureReason);
        }

        TenantIndexReadModel? indexModel = await GetStateAsync<TenantIndexReadModel>(
            TenantIndexProjectionKey, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (indexModel is null) {
            cancellationToken.ThrowIfCancellationRequested();
            PaginatedResult<TenantSummary> empty = new([], null, false);
            return CreateSuccessResult(SerializeToElement(empty), "tenant-index");
        }

        bool isGlobalAdmin = await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<KeyValuePair<string, TenantIndexEntry>> tenants;
        if (isGlobalAdmin) {
            tenants = indexModel.Tenants;
        }
        else {
            HashSet<string> userTenantIds = GetUserTenantIds(indexModel, envelope.UserId);
            tenants = indexModel.Tenants.Where(t => userTenantIds.Contains(t.Key));
        }

        PaginatedResult<TenantSummary> result = ProtectCursor(
            Paginate(
                tenants,
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => new TenantSummary(kvp.Key, kvp.Value.Name, kvp.Value.Status),
                cancellationToken),
            ListTenantsQuery.QueryType,
            scope);

        cancellationToken.ThrowIfCancellationRequested();
        return CreateSuccessResult(SerializeToElement(result), "tenant-index");
    }
}
