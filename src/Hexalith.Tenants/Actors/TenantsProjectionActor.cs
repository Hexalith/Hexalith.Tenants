using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Telemetry;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Actors;

/// <summary>
/// Projection actor that serves tenant query endpoints with authorization checks.
/// Inherits ETag-based caching from <see cref="CachingProjectionActor"/>.
/// </summary>
[Actor(TypeName = "ProjectionActor")]
public sealed partial class TenantsProjectionActor : CachingProjectionActor {
    internal const string GlobalAdminProjectionKey = "projection:global-administrators:singleton";
    internal const string StateStoreName = "statestore";
    internal const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    internal const string TenantProjectionKeyPrefix = "projection:tenants:";

    private static readonly JsonSerializerOptions s_queryJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly DaprClient _daprClient;

    public TenantsProjectionActor(
        ActorHost host,
        IETagService eTagService,
        DaprClient daprClient,
        ILogger<TenantsProjectionActor> logger)
        : base(host, eTagService, logger) => _daprClient = daprClient;

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

            int pageSize = root.TryGetProperty("pageSize", out JsonElement pageSizeEl) && pageSizeEl.ValueKind == JsonValueKind.Number
                ? pageSizeEl.GetInt32()
                : 20;

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

    private static HashSet<string> GetUserTenantIds(TenantIndexReadModel indexModel, string userId) {
        if (indexModel.UserTenants.TryGetValue(userId, out Dictionary<string, TenantRole>? tenants)) {
            return new HashSet<string>(tenants.Keys, StringComparer.Ordinal);
        }

        return [];
    }

    private static PaginatedResult<TResult> Paginate<TSource, TResult>(
        IEnumerable<KeyValuePair<string, TSource>> items,
        string? cursor,
        int pageSize,
        Func<KeyValuePair<string, TSource>, string> keySelector,
        Func<KeyValuePair<string, TSource>, TResult> resultSelector) {
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

        return new QueryResult(
            false,
            default,
            ErrorMessage: "Audit queries are not yet implemented (FR29). Planned for a future release.");
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

        (string? cursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);

        PaginatedResult<TenantMember> result = Paginate(
            model.Members,
            cursor,
            pageSize,
            kvp => kvp.Key,
            kvp => new TenantMember(kvp.Key, kvp.Value));

        JsonElement payload = JsonSerializer.SerializeToElement(result, s_queryJsonOptions);
        return CreateSuccessResult(payload, "tenants");
    }

    private async Task<QueryResult> HandleGetUserTenantsAsync(QueryEnvelope envelope) {
        string targetUserId = string.IsNullOrWhiteSpace(envelope.EntityId) ? envelope.UserId : envelope.EntityId;

        // Non-admin can only query own tenants
        if (!string.Equals(targetUserId, envelope.UserId, StringComparison.Ordinal)
            && !await IsGlobalAdminAsync(envelope.UserId).ConfigureAwait(false)) {
            return new QueryResult(false, default, ErrorMessage: "Forbidden");
        }

        TenantIndexReadModel? indexModel = await _daprClient
            .GetStateAsync<TenantIndexReadModel>(StateStoreName, TenantIndexProjectionKey)
            .ConfigureAwait(false);

        if (indexModel is null
            || !indexModel.UserTenants.TryGetValue(targetUserId, out Dictionary<string, TenantRole>? userTenants)) {
            PaginatedResult<UserTenantMembership> empty = new([], null, false);
            JsonElement emptyPayload = JsonSerializer.SerializeToElement(empty, s_queryJsonOptions);
            return CreateSuccessResult(emptyPayload, "tenant-index");
        }

        (string? cursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);

        PaginatedResult<UserTenantMembership> result = Paginate(
            userTenants,
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
            });

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

        (string? cursor, int pageSize) = DeserializePaginationPayload(envelope.Payload);

        PaginatedResult<TenantSummary> result = Paginate(
            tenants,
            cursor,
            pageSize,
            kvp => kvp.Key,
            kvp => new TenantSummary(kvp.Key, kvp.Value.Name, kvp.Value.Status));

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
}
