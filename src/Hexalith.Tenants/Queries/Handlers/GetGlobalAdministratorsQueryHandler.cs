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
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetGlobalAdministratorsQueryHandler> logger,
    IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
    TimeProvider? timeProvider = null)
    : TenantQueryHandlerBase(store, cursorCodec, telemetry, logger, freshnessOptions, timeProvider) {
    /// <inheritdoc/>
    public override string Domain => GetGlobalAdministratorsQuery.Domain;

    /// <inheritdoc/>
    public override string QueryType => GetGlobalAdministratorsQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(envelope.TenantId, TenantIdentity.DefaultTenantId, StringComparison.Ordinal)
            || !string.Equals(envelope.AggregateId, TenantIdentity.GlobalAdministratorsAggregateId, StringComparison.Ordinal)) {
            return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetGlobalAdministrators(envelope.UserId);
        if (!CursorCodec.TryDecode(protectedCursor, GetGlobalAdministratorsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
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

        if (model is null || (!envelope.IsGlobalAdmin && !model.Administrators.Contains(envelope.UserId))) {
            return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        PaginatedResult<GlobalAdministratorSummary> result = ProtectCursor(
            Paginate(
                model.Administrators.Select(static userId => KeyValuePair.Create(userId, userId)),
                cursor,
                pageSize,
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
            adminEntry?.ETag);
    }
}
