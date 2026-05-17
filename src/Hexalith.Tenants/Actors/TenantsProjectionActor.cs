using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

namespace Hexalith.Tenants.Actors;

/// <summary>
/// Projection actor that serves tenant query endpoints with authorization checks.
/// Inherits ETag-based caching from <see cref="CachingProjectionActor"/>.
/// </summary>
[Actor(TypeName = TenantProjectionRouting.ActorTypeName)]
public sealed partial class TenantsProjectionActor : CachingProjectionActor {
    internal const string GlobalAdminProjectionKey = "projection:global-administrators:singleton";
    internal const string StateStoreName = "statestore";
    internal const string TenantAuditProjectionKeyPrefix = "audit:";
    internal const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    internal const string TenantProjectionKeyPrefix = "projection:tenants:";

    private static readonly JsonSerializerOptions s_queryJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly DaprClient _daprClient;
    private readonly ITenantQueryCursorCodec _cursorCodec;

    public TenantsProjectionActor(
        ActorHost host,
        IETagService eTagService,
        DaprClient daprClient,
        ITenantQueryCursorCodec cursorCodec,
        ILogger<TenantsProjectionActor> logger)
        : base(host, eTagService, logger) {
        _daprClient = daprClient;
        _cursorCodec = cursorCodec;
    }

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope) {
        ArgumentNullException.ThrowIfNull(envelope);

        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.QueryExecute, ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();

        _ = (activity?.SetTag(TenantActivitySource.TagQueryType, envelope.QueryType));

        try {
            QueryResult result = envelope.QueryType switch {
                "get-tenant" => await HandleGetTenantAsync(envelope).ConfigureAwait(false),
                "list-tenants" => await HandleListTenantsAsync(envelope).ConfigureAwait(false),
                "get-tenant-users" => await HandleGetTenantUsersAsync(envelope).ConfigureAwait(false),
                "get-user-tenants" => await HandleGetUserTenantsAsync(envelope).ConfigureAwait(false),
                "get-tenant-audit" => await HandleGetTenantAuditAsync(envelope).ConfigureAwait(false),
                _ => new QueryResult(false, default, ErrorMessage: $"Unknown query type: {envelope.QueryType}"),
            };

            return result;
        }
        catch (Exception ex) {
            _ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
            throw;
        }
        finally {
            stopwatch.Stop();
            TenantMetrics.RecordQueryDuration(stopwatch.Elapsed.TotalMilliseconds, envelope.QueryType);
        }
    }

    private static QueryResult CreateSuccessResult(JsonElement payload, string? projectionType)
        => new(true, JsonSerializer.SerializeToUtf8Bytes(payload), ProjectionType: projectionType);

    private static (string? Cursor, int PageSize) DeserializePaginationPayload(byte[]? payload) {
        if (payload is null || payload.Length == 0) {
            return (null, 20);
        }

        try {
            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            string? cursor = root.TryGetProperty("cursor", out JsonElement cursorEl) && cursorEl.ValueKind == JsonValueKind.String
                ? cursorEl.GetString()
                : null;

            int pageSize = 20;
            if (root.TryGetProperty("pageSize", out JsonElement pageSizeEl)
                && pageSizeEl.ValueKind == JsonValueKind.Number
                && pageSizeEl.TryGetInt32(out int parsedPageSize)) {
                pageSize = parsedPageSize;
            }

            if (pageSize <= 0) {
                pageSize = 20;
            }

            if (pageSize > 100) {
                pageSize = 100;
            }

            return (cursor, pageSize);
        }
        catch (JsonException) {
            return (null, 20);
        }
    }

    private static TenantAuditQueryPayload DeserializeAuditPayload(byte[]? payload) {
        if (payload is null || payload.Length == 0) {
            return new(null, null, null, null, 100, null);
        }

        try {
            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            DateTimeOffset? from = TryGetDateTimeOffset(root, "from");
            DateTimeOffset? to = TryGetDateTimeOffset(root, "to");
            string? cursor = root.TryGetProperty("cursor", out JsonElement cursorEl) && cursorEl.ValueKind == JsonValueKind.String
                ? cursorEl.GetString()
                : null;
            int pageSize = 100;
            if (root.TryGetProperty("pageSize", out JsonElement pageSizeEl)
                && pageSizeEl.ValueKind == JsonValueKind.Number
                && pageSizeEl.TryGetInt32(out int parsedPageSize)) {
                pageSize = parsedPageSize;
            }

            if (pageSize <= 0) {
                pageSize = 100;
            }

            if (pageSize > 1000) {
                pageSize = 1000;
            }

            AuditEventCategory? category = null;
            string? errorMessage = null;
            if (root.TryGetProperty("category", out JsonElement categoryEl)
                && categoryEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(categoryEl.GetString())) {
                string categoryValue = categoryEl.GetString()!;
                if (Enum.TryParse(categoryValue, ignoreCase: true, out AuditEventCategory parsed)) {
                    category = parsed;
                }
                else {
                    errorMessage = $"Invalid audit category: {categoryValue}";
                }
            }

            if (errorMessage is null && from is not null && to is not null && from > to) {
                errorMessage = "Invalid audit query payload: 'from' must not be after 'to'.";
            }

            return new(from, to, category, cursor, pageSize, errorMessage);
        }
        catch (JsonException) {
            return new(null, null, null, null, 100, "Invalid audit query payload.");
        }
    }

    private static string GetAuditCursor(TenantAuditEntry entry) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{entry.Timestamp.UtcDateTime.Ticks:D20}:{entry.EventId}");

    private static PaginatedResult<TenantAuditEntry> PaginateAuditEntries(
        IEnumerable<TenantAuditEntry> entries,
        string? cursor,
        int pageSize) {
        IEnumerable<TenantAuditEntry> ordered = entries
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.EventId, StringComparer.Ordinal);

        if (cursor is not null) {
            ordered = ordered.Where(e => string.Compare(GetAuditCursor(e), cursor, StringComparison.Ordinal) > 0);
        }

        var page = ordered.Take(pageSize + 1).ToList();
        bool hasMore = page.Count > pageSize;
        if (hasMore) {
            page.RemoveAt(page.Count - 1);
        }

        string? nextCursor = hasMore ? GetAuditCursor(page[^1]) : null;
        return new PaginatedResult<TenantAuditEntry>(page, nextCursor, hasMore);
    }

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement root, string propertyName) {
        if (!root.TryGetProperty(propertyName, out JsonElement element)
            || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) {
            return null;
        }

        return element.TryGetDateTimeOffset(out DateTimeOffset value) ? value : null;
    }

    private static QueryResult InvalidCursorResult() => new(false, default, ErrorMessage: "Invalid cursor.");

    private static HashSet<string> GetUserTenantIds(TenantIndexReadModel indexModel, string userId) {
        if (indexModel.UserTenants.TryGetValue(userId, out Dictionary<string, TenantRole>? tenants)) {
            return new HashSet<string>(tenants.Keys, StringComparer.Ordinal);
        }

        return [];
    }

    private static IEnumerable<KeyValuePair<string, TenantRole>> GetVisibleUserTenants(
        TenantIndexReadModel indexModel,
        string requesterUserId,
        Dictionary<string, TenantRole> targetUserTenants,
        bool canViewAllTargetTenants) {
        if (canViewAllTargetTenants) {
            return targetUserTenants;
        }

        if (!indexModel.UserTenants.TryGetValue(requesterUserId, out Dictionary<string, TenantRole>? requesterTenants)) {
            return [];
        }

        HashSet<string> requesterOwnedTenantIds = requesterTenants
            .Where(kvp => kvp.Value == TenantRole.TenantOwner)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.Ordinal);

        return targetUserTenants.Where(kvp => requesterOwnedTenantIds.Contains(kvp.Key));
    }

    private static PaginatedResult<TResult> Paginate<TSource, TResult>(
        IEnumerable<KeyValuePair<string, TSource>> items,
        string? cursor,
        int pageSize,
        Func<KeyValuePair<string, TSource>, string> keySelector,
        Func<KeyValuePair<string, TSource>, TResult> resultSelector) {
        // Callers must pass the current authorized/visible set. The cursor is only an ordinal
        // exclusive lower bound, so hidden or missing anchors are never looked up or disclosed.
        IEnumerable<KeyValuePair<string, TSource>> ordered = items.OrderBy(keySelector, StringComparer.Ordinal);

        if (cursor is not null) {
            ordered = ordered.Where(kvp => string.Compare(keySelector(kvp), cursor, StringComparison.Ordinal) > 0);
        }

        var page = ordered.Take(pageSize + 1).ToList();
        bool hasMore = page.Count > pageSize;
        if (hasMore) {
            page.RemoveAt(page.Count - 1);
        }

        string? nextCursor = hasMore ? keySelector(page[^1]) : null;
        var results = page.Select(resultSelector).ToList();

        return new PaginatedResult<TResult>(results, nextCursor, hasMore);
    }

    private PaginatedResult<TResult> ProtectCursor<TResult>(
        PaginatedResult<TResult> result,
        string queryType,
        string scope)
        => result.Cursor is null ? result : result with { Cursor = _cursorCodec.Encode(queryType, scope, result.Cursor) };

    private async Task<QueryResult> HandleGetTenantAsync(QueryEnvelope envelope) {
        TenantReadModel? model = await _daprClient
            .GetStateAsync<TenantReadModel>(StateStoreName, TenantProjectionKeyPrefix + envelope.AggregateId)
            .ConfigureAwait(false);

        if (model is null) {
            return new QueryResult(false, default, ErrorMessage: "Tenant not found");
        }

        if (!await IsAuthorizedForTenantAsync(envelope.UserId, model).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        TenantDetail detail = new(
            model.TenantId,
            model.Name,
            model.Description,
            model.Status,
            model.Members.Select(m => new TenantMember(m.Key, m.Value)).ToList(),
            model.Configuration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            model.CreatedAt);

        JsonElement payload = JsonSerializer.SerializeToElement(detail, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenants");
    }

    private async Task<QueryResult> HandleGetTenantAuditAsync(QueryEnvelope envelope) {
        // CRITICAL: Check GlobalAdmin FIRST — non-admins must get 403, not 501
        if (!await IsGlobalAdminAsync(envelope.UserId).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        // QueryEnvelope.ctor enforces non-empty AggregateId, so the "audit:" shared-key vector
        // raised in review is unreachable through any constructor path.

        TenantAuditQueryPayload query = DeserializeAuditPayload(envelope.Payload);
        if (query.ErrorMessage is not null) {
            return new QueryResult(false, default, ErrorMessage: query.ErrorMessage);
        }

        TenantAuditReadModel? model = await _daprClient
            .GetStateAsync<TenantAuditReadModel>(StateStoreName, TenantAuditProjectionKeyPrefix + envelope.AggregateId)
            .ConfigureAwait(false);

        // NFR5 defense-in-depth: a projection bug must not leak rows from another tenant.
        IEnumerable<TenantAuditEntry> entries = (model?.Entries ?? [])
            .Where(e => string.Equals(e.TenantId, envelope.AggregateId, StringComparison.Ordinal));
        if (query.From is not null) {
            entries = entries.Where(e => e.Timestamp >= query.From.Value);
        }

        if (query.To is not null) {
            entries = entries.Where(e => e.Timestamp <= query.To.Value);
        }

        if (query.Category is not null) {
            entries = entries.Where(e => e.Category == query.Category.Value);
        }

        string scope = TenantQueryCursorScopes.GetTenantAudit(envelope.AggregateId, query.From, query.To, query.Category);
        if (!_cursorCodec.TryDecode(query.Cursor, GetTenantAuditQuery.QueryType, scope, out string? cursor, out _)) {
            return InvalidCursorResult();
        }

        PaginatedResult<TenantAuditEntry> result = ProtectCursor(
            PaginateAuditEntries(entries, cursor, query.PageSize),
            GetTenantAuditQuery.QueryType,
            scope);
        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenants");
    }

    private async Task<QueryResult> HandleGetTenantUsersAsync(QueryEnvelope envelope) {
        TenantReadModel? model = await _daprClient
            .GetStateAsync<TenantReadModel>(StateStoreName, TenantProjectionKeyPrefix + envelope.AggregateId)
            .ConfigureAwait(false);

        if (model is null) {
            return new QueryResult(false, default, ErrorMessage: "Tenant not found");
        }

        if (!await IsAuthorizedForTenantAsync(envelope.UserId, model).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetTenantUsers(envelope.AggregateId);
        if (!_cursorCodec.TryDecode(protectedCursor, GetTenantUsersQuery.QueryType, scope, out string? cursor, out _)) {
            return InvalidCursorResult();
        }

        PaginatedResult<TenantMember> result = ProtectCursor(
            Paginate(
                model.Members,
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => new TenantMember(kvp.Key, kvp.Value)),
            GetTenantUsersQuery.QueryType,
            scope);

        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenants");
    }

    private async Task<QueryResult> HandleGetUserTenantsAsync(QueryEnvelope envelope) {
        string targetUserId = string.IsNullOrWhiteSpace(envelope.EntityId) ? envelope.UserId : envelope.EntityId;

        TenantIndexReadModel? indexModel = await _daprClient
            .GetStateAsync<TenantIndexReadModel>(StateStoreName, TenantIndexProjectionKey)
            .ConfigureAwait(false);

        // Run the admin check before any early return so cross-user lookups have comparable
        // response timing whether the target user is missing from the index or present-but-filtered-out.
        // This complements D11 response-body uniformity by closing a timing-based user-enumeration oracle.
        bool isSelfLookup = string.Equals(targetUserId, envelope.UserId, StringComparison.Ordinal);
        bool canViewAllTargetTenants = isSelfLookup
            || await IsGlobalAdminAsync(envelope.UserId).ConfigureAwait(false);

        if (indexModel is null
            || !indexModel.UserTenants.TryGetValue(targetUserId, out Dictionary<string, TenantRole>? userTenants)) {
            PaginatedResult<UserTenantMembership> empty = new([], null, false);
            JsonElement emptyPayload = JsonSerializer.SerializeToElement(empty, s_queryJsonOptions);
            return CreateSuccessResult(emptyPayload, "tenant-index");
        }

        IEnumerable<KeyValuePair<string, TenantRole>> visibleUserTenants = GetVisibleUserTenants(
            indexModel,
            envelope.UserId,
            userTenants,
            canViewAllTargetTenants);

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetUserTenants(targetUserId);
        if (!_cursorCodec.TryDecode(protectedCursor, GetUserTenantsQuery.QueryType, scope, out string? cursor, out _)) {
            return InvalidCursorResult();
        }

        PaginatedResult<UserTenantMembership> result = ProtectCursor(
            Paginate(
                visibleUserTenants,
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => {
                    TenantIndexEntry? entry = indexModel.Tenants.GetValueOrDefault(kvp.Key);
                    return new UserTenantMembership(
                        kvp.Key,
                        entry?.Name ?? string.Empty,
                        entry?.Status ?? TenantStatus.Active,
                        kvp.Value);
                }),
            GetUserTenantsQuery.QueryType,
            scope);

        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenant-index");
    }

    private async Task<QueryResult> HandleListTenantsAsync(QueryEnvelope envelope) {
        TenantIndexReadModel? indexModel = await _daprClient
            .GetStateAsync<TenantIndexReadModel>(StateStoreName, TenantIndexProjectionKey)
            .ConfigureAwait(false);

        if (indexModel is null) {
            PaginatedResult<TenantSummary> empty = new([], null, false);
            JsonElement emptyPayload = JsonSerializer.SerializeToElement(empty, s_queryJsonOptions);
            return CreateSuccessResult(emptyPayload, "tenant-index");
        }

        bool isGlobalAdmin = await IsGlobalAdminAsync(envelope.UserId).ConfigureAwait(false);

        IEnumerable<KeyValuePair<string, TenantIndexEntry>> tenants;
        if (isGlobalAdmin) {
            tenants = indexModel.Tenants;
        }
        else {
            HashSet<string> userTenantIds = GetUserTenantIds(indexModel, envelope.UserId);
            tenants = indexModel.Tenants.Where(t => userTenantIds.Contains(t.Key));
        }

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.ListTenants(envelope.UserId);
        if (!_cursorCodec.TryDecode(protectedCursor, ListTenantsQuery.QueryType, scope, out string? cursor, out _)) {
            return InvalidCursorResult();
        }

        PaginatedResult<TenantSummary> result = ProtectCursor(
            Paginate(
                tenants,
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => new TenantSummary(kvp.Key, kvp.Value.Name, kvp.Value.Status)),
            ListTenantsQuery.QueryType,
            scope);

        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenant-index");
    }

    private async Task<bool> IsAuthorizedForTenantAsync(string userId, TenantReadModel model) {
        if (model.Members.ContainsKey(userId)) {
            return true;
        }

        return await IsGlobalAdminAsync(userId).ConfigureAwait(false);
    }

    private async Task<bool> IsGlobalAdminAsync(string userId) {
        GlobalAdministratorReadModel? adminModel = await _daprClient
            .GetStateAsync<GlobalAdministratorReadModel>(StateStoreName, GlobalAdminProjectionKey)
            .ConfigureAwait(false);

        return adminModel is not null && adminModel.Administrators.Contains(userId);
    }

    private sealed record TenantAuditQueryPayload(
        DateTimeOffset? From,
        DateTimeOffset? To,
        AuditEventCategory? Category,
        string? Cursor,
        int PageSize,
        string? ErrorMessage);
}
