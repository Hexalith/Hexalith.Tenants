using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>get-tenant-users</c> query: the paginated members of a tenant with their roles, gated
/// by tenant membership or global-administrator role.
/// </summary>
public sealed class GetTenantUsersQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetTenantUsersQueryHandler> logger)
    : TenantQueryHandlerBase(store, cursorCodec, telemetry, logger) {
    /// <inheritdoc/>
    public override string QueryType => GetTenantUsersQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetTenantUsers(envelope.AggregateId);
        if (!CursorCodec.TryDecode(protectedCursor, GetTenantUsersQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetTenantUsersQuery.QueryType, "get-tenant-users", envelope.AggregateId, envelope.UserId, failureReason);
        }

        ReadModelEntry<TenantReadModel>? tenantEntry = await GetStateEntryAsync<TenantReadModel>(
            TenantProjectionKeyPrefix + envelope.AggregateId, cancellationToken).ConfigureAwait(false);
        TenantReadModel? model = tenantEntry?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        if (model is null) {
            return await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false)
                ? new QueryResult(false, default, ErrorMessage: "Tenant not found")
                : new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
        }

        if (!await IsAuthorizedForTenantAsync(envelope.UserId, model, cancellationToken).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        PaginatedResult<TenantMember> result = ProtectCursor(
            Paginate(
                GetConcreteMembers(model),
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => new TenantMember(kvp.Key, kvp.Value),
                cancellationToken),
            GetTenantUsersQuery.QueryType,
            scope);

        cancellationToken.ThrowIfCancellationRequested();
        JsonElement payload = SerializeToElement(result);
        return CreateSuccessResult(payload, "tenants", tenantEntry?.ETag);
    }
}
