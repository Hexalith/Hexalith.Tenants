using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Serves the <c>get-user-tenants</c> query: the paginated tenants a target user belongs to. A user can
/// always see their own memberships; a global administrator sees any user's; a tenant owner sees a target
/// user's memberships only for tenants the requester owns. Generic gateway callers name the target user in the
/// payload's CLR <c>UserId</c>; REST callers route it as the envelope entity identifier.
/// </summary>
public sealed class GetUserTenantsQueryHandler(
    IReadModelStore store,
    IQueryCursorCodec cursorCodec,
    TenantTelemetry telemetry,
    ILogger<GetUserTenantsQueryHandler> logger,
    IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
    TimeProvider? timeProvider = null)
    : TenantQueryHandlerBase(store, cursorCodec, telemetry, logger, freshnessOptions, timeProvider) {
    /// <inheritdoc/>
    public override string QueryType => GetUserTenantsQuery.QueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        if (!TryResolveTargetUser(envelope, out string targetUserId)) {
            return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.InvalidEnvelope);
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReadModelEntry<TenantIndexReadModel>? indexEntry = await GetStateEntryAsync<TenantIndexReadModel>(
            TenantIndexProjectionKey, cancellationToken).ConfigureAwait(false);
        TenantIndexReadModel? indexModel = indexEntry?.Value;
        cancellationToken.ThrowIfCancellationRequested();

        // Run the admin check before any early return so cross-user lookups have comparable
        // response timing whether the target user is missing from the index or present-but-filtered-out.
        // This complements D11 response-body uniformity by closing a timing-based user-enumeration oracle.
        bool isSelfLookup = string.Equals(targetUserId, envelope.UserId, StringComparison.Ordinal);
        bool canViewAllTargetTenants = isSelfLookup
            || await IsGlobalAdminAsync(envelope, cancellationToken).ConfigureAwait(false);

        TenantQueryPaginationPayload pagination = ResolveStandardPagination(envelope);
        if (pagination.ErrorMessage is not null)
        {
            return new QueryResult(false, default, ErrorMessage: pagination.ErrorMessage);
        }

        string scope = TenantQueryCursorScopes.GetUserTenants(envelope.UserId, targetUserId);
        if (!CursorCodec.TryDecode(pagination.Cursor, GetUserTenantsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetUserTenantsQuery.QueryType, "get-user-tenants", envelope.AggregateId, envelope.UserId, failureReason);
        }

        if (indexModel is null
            || !indexModel.UserTenants.TryGetValue(targetUserId, out Dictionary<string, TenantRole>? userTenants)) {
            cancellationToken.ThrowIfCancellationRequested();
            PaginatedResult<UserTenantMembership> empty = new([], null, false);
            return CreateSuccessResult(
                SerializeToElement(empty),
                "tenant-index",
                indexModel,
                indexEntry?.ETag,
                TenantQueryEnvelopePaging.CreateMetadata(pagination, empty));
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
                pagination.PageSize,
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
        return CreateSuccessResult(
            SerializeToElement(result),
            "tenant-index",
            indexModel,
            indexEntry?.ETag,
            TenantQueryEnvelopePaging.CreateMetadata(pagination, result));
    }

    /// <summary>
    /// Resolves the user whose memberships are listed: the payload's CLR <c>UserId</c> when set, otherwise the
    /// routed entity identifier, otherwise the caller. Authorization is evaluated afterwards for the resolved user.
    /// </summary>
    /// <param name="envelope">The authenticated query envelope.</param>
    /// <param name="targetUserId">The resolved target user.</param>
    /// <returns><see langword="false"/> when the payload user is malformed or disagrees with an explicit entity identifier.</returns>
    private static bool TryResolveTargetUser(QueryEnvelope envelope, out string targetUserId) {
        targetUserId = string.IsNullOrWhiteSpace(envelope.EntityId) ? envelope.UserId : envelope.EntityId;
        if (!TryReadPayloadUserId(envelope.Payload, out string? payloadUserId)) {
            return false;
        }

        if (payloadUserId is null) {
            return true;
        }

        // The gateway routes a request without an entity identifier to its aggregate identifier, so only a
        // distinct entity identifier was chosen by the caller, and it must name the same user as the payload.
        bool explicitEntityId = !string.IsNullOrWhiteSpace(envelope.EntityId)
            && !string.Equals(envelope.EntityId, envelope.AggregateId, StringComparison.Ordinal);
        if (explicitEntityId && !string.Equals(envelope.EntityId, payloadUserId, StringComparison.Ordinal)) {
            return false;
        }

        targetUserId = payloadUserId;
        return true;
    }

    /// <summary>Reads the optional CLR <c>UserId</c> payload member; a blank value counts as absent.</summary>
    /// <param name="payload">The serialized query payload.</param>
    /// <param name="userId">The non-blank payload user, if any.</param>
    /// <returns><see langword="false"/> when the member is not a string or cannot be decoded.</returns>
    private static bool TryReadPayloadUserId(byte[]? payload, out string? userId) {
        userId = null;
        if (payload is null || payload.Length == 0) {
            return true;
        }

        try {
            using var document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(nameof(GetUserTenantsQuery.UserId), out JsonElement element)
                || element.ValueKind == JsonValueKind.Null) {
                return true;
            }

            if (element.ValueKind != JsonValueKind.String) {
                return false;
            }

            string? value = element.GetString();
            userId = string.IsNullOrWhiteSpace(value) ? null : value;
            return true;
        }
        catch (JsonException) {
            // Unparseable payloads keep the established lenient handling of the legacy paging fields.
            return true;
        }
        catch (InvalidOperationException) {
            // JSON strings can contain escaped unpaired surrogates that fail only when decoded.
            return false;
        }
    }
}
