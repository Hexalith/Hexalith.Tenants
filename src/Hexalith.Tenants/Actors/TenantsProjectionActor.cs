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
using Hexalith.Tenants.Contracts.Serialization;
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
    private const string TenantQueryEnvelopeAuthorizationStage = "TenantQueryEnvelopeAuthorization";

    private static readonly JsonSerializerOptions s_queryJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TenantStatusJsonConverter(), new JsonStringEnumConverter() },
    };

    private readonly DaprClient _daprClient;
    private readonly ITenantQueryCursorCodec _cursorCodec;
    private readonly ILogger<TenantsProjectionActor> _logger;

    // Per-actor-lifetime dedup so a persistent orphan does not re-emit a Warning on every poll.
    // Cleared when the actor is deactivated by Dapr.
    private readonly HashSet<(string TargetUserId, string OrphanTenantId)> _loggedOrphanMemberships = [];

    public TenantsProjectionActor(
        ActorHost host,
        IETagService eTagService,
        DaprClient daprClient,
        ITenantQueryCursorCodec cursorCodec,
        ILogger<TenantsProjectionActor> logger)
        : base(host, eTagService, logger) {
        _daprClient = daprClient;
        _cursorCodec = cursorCodec;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope) =>
        ExecuteQueryAsync(envelope, CancellationToken.None);

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(envelope);

        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.QueryExecute, ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();

        _ = (activity?.SetTag(TenantActivitySource.TagQueryType, envelope.QueryType));

        try {
            if (IsRoleSensitiveQuery(envelope.QueryType) && string.IsNullOrWhiteSpace(envelope.UserId)) {
                Log.MissingAuthenticatedUserRejected(
                    _logger,
                    envelope.CorrelationId,
                    envelope.QueryType,
                    QueryAdapterFailureReason.Forbidden,
                    TenantQueryEnvelopeAuthorizationStage);
                return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
            }

            cancellationToken.ThrowIfCancellationRequested();
            QueryResult result = envelope.QueryType switch {
                "get-tenant" => await HandleGetTenantAsync(envelope, cancellationToken).ConfigureAwait(false),
                "list-tenants" => await HandleListTenantsAsync(envelope, cancellationToken).ConfigureAwait(false),
                "get-tenant-users" => await HandleGetTenantUsersAsync(envelope, cancellationToken).ConfigureAwait(false),
                "get-user-tenants" => await HandleGetUserTenantsAsync(envelope, cancellationToken).ConfigureAwait(false),
                "get-tenant-audit" => await HandleGetTenantAuditAsync(envelope, cancellationToken).ConfigureAwait(false),
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

    private static bool IsRoleSensitiveQuery(string? queryType)
        => queryType is
            "get-tenant" or
            "list-tenants" or
            "get-tenant-users" or
            "get-user-tenants" or
            "get-tenant-audit";

    private static (string? Cursor, int PageSize) DeserializePaginationPayload(byte[]? payload) {
        TenantQueryPaginationPayload pagination = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(payload);
        return (pagination.Cursor, pagination.PageSize);
    }

    private static TenantAuditQueryPayload DeserializeAuditPayload(byte[]? payload) {
        if (payload is null || payload.Length == 0) {
            return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null);
        }

        try {
            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) {
                return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, "Invalid audit query payload.");
            }

            DateTimeOffset? from = TryGetDateTimeOffset(root, "from");
            DateTimeOffset? to = TryGetDateTimeOffset(root, "to");
            string? cursor = root.TryGetProperty("cursor", out JsonElement cursorEl) && cursorEl.ValueKind == JsonValueKind.String
                ? cursorEl.GetString()
                : null;
            int pageSize = TenantQueryPaginationPolicy.AuditDefaultPageSize;
            if (root.TryGetProperty("pageSize", out JsonElement pageSizeEl)
                && pageSizeEl.ValueKind == JsonValueKind.Number
                && pageSizeEl.TryGetInt32(out int parsedPageSize)) {
                pageSize = parsedPageSize;
            }

            pageSize = TenantQueryPaginationPolicy.ClampAuditPageSize(pageSize);

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
            return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, "Invalid audit query payload.");
        }
    }

    private static string GetAuditCursor(TenantAuditEntry entry) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{entry.Timestamp.UtcDateTime.Ticks:D20}:{entry.EventId}");

    private static PaginatedResult<TenantAuditEntry> PaginateAuditEntries(
        IEnumerable<TenantAuditEntry> entries,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<TenantAuditEntry> ordered = entries
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.EventId, StringComparer.Ordinal);

        if (cursor is not null) {
            ordered = ordered.Where(e => string.Compare(GetAuditCursor(e), cursor, StringComparison.Ordinal) > 0);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var page = ordered.Take(pageSize + 1).ToList();
        cancellationToken.ThrowIfCancellationRequested();
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

    private QueryResult InvalidCursorResult(
        string queryType,
        string endpoint,
        string tenantId,
        string userId,
        string? failureReason) {
        Log.InvalidCursorRejected(
            _logger,
            queryType,
            endpoint,
            tenantId,
            userId,
            failureReason ?? "unknown");
        return new(false, default, ErrorMessage: "Invalid cursor.");
    }

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
        Func<KeyValuePair<string, TSource>, TResult> resultSelector,
        CancellationToken cancellationToken) {
        // Callers must pass the current authorized/visible set. The cursor is only an ordinal
        // exclusive lower bound, so hidden or missing anchors are never looked up or disclosed.
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<KeyValuePair<string, TSource>> ordered = items.OrderBy(keySelector, StringComparer.Ordinal);

        if (cursor is not null) {
            ordered = ordered.Where(kvp => string.Compare(keySelector(kvp), cursor, StringComparison.Ordinal) > 0);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var page = ordered.Take(pageSize + 1).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        bool hasMore = page.Count > pageSize;
        if (hasMore) {
            page.RemoveAt(page.Count - 1);
        }

        string? nextCursor = hasMore ? keySelector(page[^1]) : null;
        cancellationToken.ThrowIfCancellationRequested();
        var results = page.Select(resultSelector).ToList();

        return new PaginatedResult<TResult>(results, nextCursor, hasMore);
    }

    private PaginatedResult<TResult> ProtectCursor<TResult>(
        PaginatedResult<TResult> result,
        string queryType,
        string scope)
        => string.IsNullOrWhiteSpace(result.Cursor)
            ? result
            : result with { Cursor = _cursorCodec.Encode(queryType, scope, result.Cursor) };

    private async Task<QueryResult> HandleGetTenantAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        TenantReadModel? model = await _daprClient
            .GetStateAsync<TenantReadModel>(
                StateStoreName,
                TenantProjectionKeyPrefix + envelope.AggregateId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (model is null) {
            return new QueryResult(false, default, ErrorMessage: "Tenant not found");
        }

        if (!await IsAuthorizedForTenantAsync(envelope.UserId, model, cancellationToken).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        cancellationToken.ThrowIfCancellationRequested();
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

    private async Task<QueryResult> HandleGetTenantAuditAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        // CRITICAL: Check GlobalAdmin FIRST — non-admins must get 403, not 501
        if (!await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        // QueryEnvelope.ctor enforces non-empty AggregateId, so the "audit:" shared-key vector
        // raised in review is unreachable through any constructor path.

        TenantAuditQueryPayload query = DeserializeAuditPayload(envelope.Payload);
        if (query.ErrorMessage is not null) {
            return new QueryResult(false, default, ErrorMessage: query.ErrorMessage);
        }

        cancellationToken.ThrowIfCancellationRequested();
        TenantAuditReadModel? model = await _daprClient
            .GetStateAsync<TenantAuditReadModel>(
                StateStoreName,
                TenantAuditProjectionKeyPrefix + envelope.AggregateId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

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
        if (!_cursorCodec.TryDecode(query.Cursor, GetTenantAuditQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetTenantAuditQuery.QueryType, "get-tenant-audit", envelope.AggregateId, envelope.UserId, failureReason);
        }

        cancellationToken.ThrowIfCancellationRequested();
        PaginatedResult<TenantAuditEntry> result = ProtectCursor(
            PaginateAuditEntries(entries, cursor, query.PageSize, cancellationToken),
            GetTenantAuditQuery.QueryType,
            scope);
        cancellationToken.ThrowIfCancellationRequested();
        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenants");
    }

    private async Task<QueryResult> HandleGetTenantUsersAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        TenantReadModel? model = await _daprClient
            .GetStateAsync<TenantReadModel>(
                StateStoreName,
                TenantProjectionKeyPrefix + envelope.AggregateId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (model is null) {
            return new QueryResult(false, default, ErrorMessage: "Tenant not found");
        }

        if (!await IsAuthorizedForTenantAsync(envelope.UserId, model, cancellationToken).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetTenantUsers(envelope.AggregateId);
        if (!_cursorCodec.TryDecode(protectedCursor, GetTenantUsersQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetTenantUsersQuery.QueryType, "get-tenant-users", envelope.AggregateId, envelope.UserId, failureReason);
        }

        PaginatedResult<TenantMember> result = ProtectCursor(
            Paginate(
                model.Members,
                cursor,
                pageSize,
                kvp => kvp.Key,
                kvp => new TenantMember(kvp.Key, kvp.Value),
                cancellationToken),
            GetTenantUsersQuery.QueryType,
            scope);

        cancellationToken.ThrowIfCancellationRequested();
        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenants");
    }

    private async Task<QueryResult> HandleGetUserTenantsAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        string targetUserId = string.IsNullOrWhiteSpace(envelope.EntityId) ? envelope.UserId : envelope.EntityId;

        cancellationToken.ThrowIfCancellationRequested();
        TenantIndexReadModel? indexModel = await _daprClient
            .GetStateAsync<TenantIndexReadModel>(
                StateStoreName,
                TenantIndexProjectionKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        // Run the admin check before any early return so cross-user lookups have comparable
        // response timing whether the target user is missing from the index or present-but-filtered-out.
        // This complements D11 response-body uniformity by closing a timing-based user-enumeration oracle.
        bool isSelfLookup = string.Equals(targetUserId, envelope.UserId, StringComparison.Ordinal);
        bool canViewAllTargetTenants = isSelfLookup
            || await IsGlobalAdminAsync(envelope.UserId, cancellationToken).ConfigureAwait(false);

        if (indexModel is null
            || !indexModel.UserTenants.TryGetValue(targetUserId, out Dictionary<string, TenantRole>? userTenants)) {
            cancellationToken.ThrowIfCancellationRequested();
            PaginatedResult<UserTenantMembership> empty = new([], null, false);
            JsonElement emptyPayload = JsonSerializer.SerializeToElement(empty, s_queryJsonOptions);
            return CreateSuccessResult(emptyPayload, "tenant-index");
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

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.GetUserTenants(targetUserId);
        if (!_cursorCodec.TryDecode(protectedCursor, GetUserTenantsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(GetUserTenantsQuery.QueryType, "get-user-tenants", envelope.AggregateId, envelope.UserId, failureReason);
        }

        // Emit repair warnings only after the request is otherwise valid, and only once per
        // (target user, orphan tenant) per actor lifetime so repeated polling does not flood logs.
        foreach (string orphanTenantId in orphanTenantIds) {
            cancellationToken.ThrowIfCancellationRequested();
            if (_loggedOrphanMemberships.Add((targetUserId, orphanTenantId))) {
                Log.OrphanUserTenantMembershipFiltered(
                    _logger,
                    envelope.CorrelationId,
                    GetUserTenantsQuery.QueryType,
                    envelope.UserId,
                    targetUserId,
                    orphanTenantId);
            }
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
        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenant-index");
    }

    private async Task<QueryResult> HandleListTenantsAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        TenantIndexReadModel? indexModel = await _daprClient
            .GetStateAsync<TenantIndexReadModel>(
                StateStoreName,
                TenantIndexProjectionKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (indexModel is null) {
            cancellationToken.ThrowIfCancellationRequested();
            PaginatedResult<TenantSummary> empty = new([], null, false);
            JsonElement emptyPayload = JsonSerializer.SerializeToElement(empty, s_queryJsonOptions);
            return CreateSuccessResult(emptyPayload, "tenant-index");
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

        (string? protectedCursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);
        string scope = TenantQueryCursorScopes.ListTenants(envelope.UserId);
        if (!_cursorCodec.TryDecode(protectedCursor, ListTenantsQuery.QueryType, scope, out string? cursor, out string? failureReason)) {
            return InvalidCursorResult(ListTenantsQuery.QueryType, "list-tenants", envelope.AggregateId, envelope.UserId, failureReason);
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
        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenant-index");
    }

    private async Task<bool> IsAuthorizedForTenantAsync(string userId, TenantReadModel model, CancellationToken cancellationToken) {
        if (model.Members.ContainsKey(userId)) {
            return true;
        }

        return await IsGlobalAdminAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsGlobalAdminAsync(string userId, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        GlobalAdministratorReadModel? adminModel = await _daprClient
            .GetStateAsync<GlobalAdministratorReadModel>(
                StateStoreName,
                GlobalAdminProjectionKey,
                cancellationToken: cancellationToken)
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

    private static partial class Log {
        [LoggerMessage(
            EventId = 1902,
            Level = LogLevel.Warning,
            Message = "Invalid tenant query cursor rejected at actor: QueryType={QueryType}, Endpoint={Endpoint}, TenantId={TenantId}, UserId={UserId}, FailureReason={FailureReason}, Stage=TenantsProjectionActor")]
        public static partial void InvalidCursorRejected(
            ILogger logger,
            string queryType,
            string endpoint,
            string tenantId,
            string userId,
            string failureReason);

        [LoggerMessage(
            EventId = 1903,
            Level = LogLevel.Warning,
            Message = "Filtered orphan tenant membership from query result: CorrelationId={CorrelationId}, QueryType={QueryType}, RequesterUserId={RequesterUserId}, TargetUserId={TargetUserId}, OrphanTenantId={OrphanTenantId}, Stage=TenantsProjectionActor")]
        public static partial void OrphanUserTenantMembershipFiltered(
            ILogger logger,
            string correlationId,
            string queryType,
            string requesterUserId,
            string targetUserId,
            string orphanTenantId);

        [LoggerMessage(
            EventId = 1904,
            Level = LogLevel.Warning,
            Message = "Tenant query envelope rejected before authorization because authenticated user id was missing: CorrelationId={CorrelationId}, QueryType={QueryType}, FailureReason={FailureReason}, Stage={Stage}")]
        public static partial void MissingAuthenticatedUserRejected(
            ILogger logger,
            string? correlationId,
            string? queryType,
            string failureReason,
            string stage);
    }
}
