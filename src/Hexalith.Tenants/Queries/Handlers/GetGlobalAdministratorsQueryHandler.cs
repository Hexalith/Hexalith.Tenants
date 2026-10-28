using Hexalith.Tenants.Authorization;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the fixed-scope <c>get-global-administrators</c> query from the singleton
/// global-administrator projection.
/// </summary>
public sealed class GetGlobalAdministratorsQueryHandler(
    IReadModelStore store,
    ITenantsGlobalAdministratorAuthority administratorAuthority,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetGlobalAdministratorsQueryHandler> logger,
    IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
    TimeProvider? timeProvider = null)
    : TenantQueryHandlerBase(store, administratorAuthority, cursorCodec, telemetry, logger, freshnessOptions, timeProvider) {
    /// <inheritdoc/>
    public override string Domain => GetGlobalAdministratorsQuery.Domain;

    /// <inheritdoc/>
    /// <summary>Gets the offline route declaration from the same owner metadata as runtime dispatch.</summary>
    public static Hexalith.EventStore.Contracts.Routing.RouteContractDeclaration CatalogDeclaration =>
        Hexalith.Tenants.Server.Routing.TenantCatalogCapabilities.HandlerQueries.Single(route => route.MessageType == GetGlobalAdministratorsQuery.QueryType);

    public override string QueryType => CatalogDeclaration.MessageType!;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(envelope.TenantId, TenantIdentity.DefaultTenantId, StringComparison.Ordinal)
            || !string.Equals(envelope.AggregateId, TenantIdentity.GlobalAdministratorsAggregateId, StringComparison.Ordinal)) {
            return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        TenantQueryPaginationPayload pagination = ResolveStandardPagination(envelope);
        if (pagination.ErrorMessage is not null)
        {
            return new QueryResult(false, default, ErrorMessage: pagination.ErrorMessage);
        }

        string scope = TenantQueryCursorScopes.GetGlobalAdministrators(envelope.UserId);
        if (!CursorCodec.TryDecode(pagination.Cursor, GetGlobalAdministratorsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(
                GetGlobalAdministratorsQuery.QueryType,
                "get-global-administrators",
                envelope.AggregateId,
                envelope.UserId,
                failureReason);
        }

        ReadModelEntry<GlobalAdministratorReadModel>? adminEntry = await GetStateEntryAsync<GlobalAdministratorReadModel>(
            GlobalAdminProjectionKey,
            cancellationToken).ConfigureAwait(false);
        GlobalAdministratorReadModel? model = adminEntry?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        // The wire administrator hint is untrusted (EventStore Story 5.5, FR28): only current membership authorizes.
        if (!await IsGlobalAdminAsync(envelope, cancellationToken).ConfigureAwait(false) || model is null) {
            return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        PaginatedResult<GlobalAdministratorSummary> result = ProtectCursor(
            Paginate(
                model.Administrators.Select(static userId => KeyValuePair.Create(userId, userId)),
                cursor,
                pagination.PageSize,
                static kvp => kvp.Key,
                static kvp => new GlobalAdministratorSummary(kvp.Value),
                cancellationToken),
            GetGlobalAdministratorsQuery.QueryType,
            scope);

        cancellationToken.ThrowIfCancellationRequested();
        return CreateSuccessResult(
            SerializeToElement(result),
            GetGlobalAdministratorsQuery.ProjectionType,
            model,
            adminEntry?.ETag,
            TenantQueryEnvelopePaging.CreateMetadata(pagination, result));
    }
}
