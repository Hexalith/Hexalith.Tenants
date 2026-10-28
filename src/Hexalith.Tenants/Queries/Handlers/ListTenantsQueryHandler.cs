using Hexalith.Tenants.Authorization;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>list-tenants</c> query: the paginated set of tenants visible to the authenticated user
/// (all tenants for a global administrator, otherwise the user's own tenants).
/// </summary>
public sealed class ListTenantsQueryHandler(
    IReadModelStore store,
    ITenantsGlobalAdministratorAuthority administratorAuthority,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<ListTenantsQueryHandler> logger,
    IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
    TimeProvider? timeProvider = null)
    : TenantQueryHandlerBase(store, administratorAuthority, cursorCodec, telemetry, logger, freshnessOptions, timeProvider) {
    /// <inheritdoc/>
    /// <summary>Gets the offline route declaration from the same owner metadata as runtime dispatch.</summary>
    public static Hexalith.EventStore.Contracts.Routing.RouteContractDeclaration CatalogDeclaration =>
        Hexalith.Tenants.Server.Routing.TenantCatalogCapabilities.HandlerQueries.Single(route => route.MessageType == ListTenantsQuery.QueryType);

    public override string QueryType => CatalogDeclaration.MessageType!;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        TenantQueryPaginationPayload pagination = ResolveStandardPagination(envelope);
        if (pagination.ErrorMessage is not null)
        {
            return new QueryResult(false, default, ErrorMessage: pagination.ErrorMessage);
        }

        string scope = TenantQueryCursorScopes.ListTenants(envelope.UserId);
        if (!CursorCodec.TryDecode(pagination.Cursor, ListTenantsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(ListTenantsQuery.QueryType, "list-tenants", envelope.AggregateId, envelope.UserId, failureReason);
        }

        ReadModelEntry<TenantIndexReadModel>? indexEntry = await GetStateEntryAsync<TenantIndexReadModel>(
            TenantIndexProjectionKey, cancellationToken).ConfigureAwait(false);
        TenantIndexReadModel? indexModel = indexEntry?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        if (indexModel is null) {
            cancellationToken.ThrowIfCancellationRequested();
            PaginatedResult<TenantSummary> empty = new([], null, false);
            return CreateSuccessResult(
                SerializeToElement(empty),
                "tenant-index",
                indexModel,
                indexEntry?.ETag,
                TenantQueryEnvelopePaging.CreateMetadata(pagination, empty));
        }

        bool isGlobalAdmin = await IsGlobalAdminAsync(envelope, cancellationToken).ConfigureAwait(false);
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
                pagination.PageSize,
                kvp => kvp.Key,
                kvp => new TenantSummary(kvp.Key, kvp.Value.Name, kvp.Value.Status),
                cancellationToken),
            ListTenantsQuery.QueryType,
            scope);

        cancellationToken.ThrowIfCancellationRequested();
        return CreateSuccessResult(
            SerializeToElement(result),
            "tenant-index",
            indexModel,
            indexEntry?.ETag,
            TenantQueryEnvelopePaging.CreateMetadata(pagination, result));
    }
}
