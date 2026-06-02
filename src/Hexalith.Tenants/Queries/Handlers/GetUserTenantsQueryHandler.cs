using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>get-user-tenants</c> query: the paginated tenants a target user belongs to. A user can
/// always see their own memberships; a global administrator sees any user's; a tenant owner sees a target
/// user's memberships only for tenants the requester owns.
/// </summary>
public sealed class GetUserTenantsQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetUserTenantsQueryHandler> logger)
    : TenantQueryHandlerBase(store, cursorCodec, telemetry, logger) {
    /// <inheritdoc/>
    public override string QueryType => GetUserTenantsQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        string targetUserId = string.IsNullOrWhiteSpace(envelope.EntityId) ? envelope.UserId : envelope.EntityId;

        cancellationToken.ThrowIfCancellationRequested();
        TenantIndexReadModel? indexModel = await GetStateAsync<TenantIndexReadModel>(
            TenantIndexProjectionKey, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // Run the admin check before any early return so cross-user lookups have comparable
        // response timing whether the target user is missing from the index or present-but-filtered-out.
        // This complements D11 response-body uniformity by closing a timing-based user-enumeration oracle.
        bool isSelfLookup = string.Equals(targetUserId, envelope.UserId, StringComparison.Ordinal);
        bool canViewAllTargetTenants = isSelfLookup
            || await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false);

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetUserTenants(envelope.UserId, targetUserId);
        if (!CursorCodec.TryDecode(protectedCursor, GetUserTenantsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetUserTenantsQuery.QueryType, "get-user-tenants", envelope.AggregateId, envelope.UserId, failureReason);
        }

        if (indexModel is null
            || !indexModel.UserTenants.TryGetValue(targetUserId, out Dictionary<string, TenantRole>? userTenants)) {
            cancellationToken.ThrowIfCancellationRequested();
            PaginatedResult<UserTenantMembership> empty = new([], null, false);
            return CreateSuccessResult(SerializeToElement(empty), "tenant-index");
        }

        IEnumerable<KeyValuePair<string, TenantRole>> visibleUserTenants = GetVisibleUserTenants(
            indexModel,
            envelope.UserId,
            userTenants,
            canViewAllTargetTenants);

        // Resolve each visible membership against the tenant index once. Existing tenants carry
        // their entry forward to the pagination selector; missing tenants are collected so they
        // can be logged after cursor validation passes.
        List<KeyValuePair<string, (TenantIndexEntry Entry, TenantRole Role)>> existingVisibleUserTenants = [];
        List<string> orphanTenantIds = [];
        foreach (KeyValuePair<string, TenantRole> visibleUserTenant in visibleUserTenants) {
            cancellationToken.ThrowIfCancellationRequested();
            if (indexModel.Tenants.TryGetValue(visibleUserTenant.Key, out TenantIndexEntry? entry)) {
                existingVisibleUserTenants.Add(new(visibleUserTenant.Key, (entry, visibleUserTenant.Value)));
                continue;
            }

            orphanTenantIds.Add(visibleUserTenant.Key);
        }

        // Emit repair warnings only after the request is otherwise valid, and only once per
        // (target user, orphan tenant) per handler instance so repeated polling does not flood logs.
        foreach (string orphanTenantId in orphanTenantIds) {
            cancellationToken.ThrowIfCancellationRequested();
            LogOrphanUserTenantMembershipFiltered(envelope.CorrelationId, envelope.UserId, targetUserId, orphanTenantId);
        }

        PaginatedResult<UserTenantMembership> result = ProtectCursor(
            Paginate(
                existingVisibleUserTenants,
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => new UserTenantMembership(
                    kvp.Key,
                    kvp.Value.Entry.Name,
                    kvp.Value.Entry.Status,
                    kvp.Value.Role),
                cancellationToken),
            GetUserTenantsQuery.QueryType,
            scope);

        cancellationToken.ThrowIfCancellationRequested();
        return CreateSuccessResult(SerializeToElement(result), "tenant-index");
    }
}
