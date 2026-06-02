using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Serialization;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Base class for the tenant <see cref="IDomainQueryHandler"/> implementations. Holds the read-model
/// access, RBAC-scoped visibility, pagination, and protected-cursor logic that used to live in the
/// retired <c>TenantsProjectionActor</c>, dispatched in-process by <c>TenantsQueryController</c> via the
/// platform <c>DomainQueryDispatcher</c>. Reads go through the platform <see cref="IReadModelStore"/>
/// (Epic A8) and cursors through the platform <see cref="IQueryCursorCodec"/> (Epic A9); no DAPR actor
/// host is involved.
/// </summary>
public abstract partial class TenantQueryHandlerBase : IDomainQueryHandler {
    internal const string GlobalAdminProjectionKey = "projection:global-administrators:singleton";
    internal const string StateStoreName = "statestore";
    internal const string TenantAuditProjectionKeyPrefix = "audit:";
    internal const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    internal const string TenantProjectionKeyPrefix = "projection:tenants:";
    private const string FailureOutcome = "failure";
    private const string ForbiddenOutcome = "forbidden";
    private const string ProjectionQueryStage = "projection-query";
    private const string RejectionOutcome = "rejection";
    private const string SuccessOutcome = "success";
    private const string TenantQueryEnvelopeAuthorizationStage = "TenantQueryEnvelopeAuthorization";

    private static readonly JsonSerializerOptions s_queryJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TenantStatusJsonConverter(), new JsonStringEnumConverter() },
    };

    private readonly IReadModelStore _store;
    private readonly TenantTelemetry _telemetry;

    // Per-handler-instance dedup so a persistent orphan does not re-emit a Warning for every visible
    // membership within a single query. Handlers are scoped (one instance per request).
    private readonly HashSet<(string TargetUserId, string OrphanTenantId)> _loggedOrphanMemberships = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantQueryHandlerBase"/> class.
    /// </summary>
    /// <param name="store">The persisted read-model store.</param>
    /// <param name="cursorCodec">The protected pagination cursor codec.</param>
    /// <param name="telemetry">The domain telemetry instruments.</param>
    /// <param name="logger">The logger.</param>
    protected TenantQueryHandlerBase(IReadModelStore store, IQueryCursorCodec cursorCodec, TenantTelemetry telemetry, ILogger logger) {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(cursorCodec);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(logger);
        _store = store;
        CursorCodec = cursorCodec;
        _telemetry = telemetry;
        Logger = logger;
    }

    /// <inheritdoc/>
    public string Domain => GetTenantQuery.Domain;

    /// <inheritdoc/>
    public abstract string QueryType { get; }

    /// <summary>Gets the protected pagination cursor codec.</summary>
    protected IQueryCursorCodec CursorCodec { get; }

    /// <summary>Gets the logger.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope envelope, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(envelope);

        // Pre-cancellation wins over the cheap missing-user validation and emits no span/metric, matching
        // the precedence the retired CachingProjectionActor.QueryAsync gave a pre-cancelled token.
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = _telemetry.StartActivity(TenantTelemetry.QueryExecute);
        var stopwatch = Stopwatch.StartNew();
        string outcome = FailureOutcome;

        _ = (activity?.SetTag(TenantTelemetry.TagQueryType, envelope.QueryType));
        _ = (activity?.SetTag(TenantTelemetry.TagTenantId, envelope.TenantId));
        _ = (activity?.SetTag(TenantTelemetry.TagDomain, envelope.Domain));
        _ = (activity?.SetTag(TenantTelemetry.TagAggregateId, envelope.AggregateId));
        _ = (activity?.SetTag(TenantTelemetry.TagCorrelationId, envelope.CorrelationId));
        _ = (activity?.SetTag(TenantTelemetry.TagStage, ProjectionQueryStage));

        try {
            // Every tenant query is role-sensitive: reject an envelope without an authenticated user
            // before any state access so a missing identity can never reach the read model.
            if (string.IsNullOrWhiteSpace(envelope.UserId)) {
                Log.MissingAuthenticatedUserRejected(
                    Logger,
                    envelope.CorrelationId,
                    envelope.QueryType,
                    QueryAdapterFailureReason.Forbidden,
                    TenantQueryEnvelopeAuthorizationStage);
                outcome = ForbiddenOutcome;
                return new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden);
            }

            cancellationToken.ThrowIfCancellationRequested();
            QueryResult result = await ExecuteCoreAsync(envelope, cancellationToken).ConfigureAwait(false);
            outcome = GetQueryOutcome(result);
            return result;
        }
        catch (Exception ex) {
            _ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
            outcome = FailureOutcome;
            throw;
        }
        finally {
            stopwatch.Stop();
            _ = (activity?.SetTag(TenantTelemetry.TagOutcome, outcome));
            _telemetry.RecordQueryDuration(stopwatch.Elapsed.TotalMilliseconds, envelope.QueryType, outcome);
        }
    }

    /// <summary>
    /// Executes the query after the shared authenticated-user gate has passed.
    /// </summary>
    /// <param name="envelope">The authenticated query envelope.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The query result.</returns>
    protected abstract Task<QueryResult> ExecuteCoreAsync(QueryEnvelope envelope, CancellationToken cancellationToken);

    private protected static QueryResult CreateSuccessResult(JsonElement payload, string? projectionType)
        => new(true, JsonSerializer.SerializeToUtf8Bytes(payload), ProjectionType: projectionType);

    private protected static JsonElement SerializeToElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, s_queryJsonOptions);

    private static string GetQueryOutcome(QueryResult result) {
        if (result.Success) {
            return SuccessOutcome;
        }

        return string.Equals(result.ErrorMessage, QueryAdapterFailureReason.Forbidden, StringComparison.Ordinal)
            || string.Equals(result.ErrorMessage, "Forbidden", StringComparison.Ordinal)
            ? ForbiddenOutcome
            : RejectionOutcome;
    }

    private protected static (string? Cursor, int PageSize) DeserializePaginationPayload(byte[]? payload) {
        TenantQueryPaginationPayload pagination = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(payload);
        return (pagination.Cursor, pagination.PageSize);
    }

    private protected static bool IsConcreteTenantRole(TenantRole role)
        => role is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader;

    private protected static IEnumerable<KeyValuePair<string, TenantRole>> GetConcreteMembers(TenantReadModel model)
        => model.Members.Where(kvp => IsConcreteTenantRole(kvp.Value));

    private protected static HashSet<string> GetUserTenantIds(TenantIndexReadModel indexModel, string userId) {
        if (indexModel.UserTenants.TryGetValue(userId, out Dictionary<string, TenantRole>? tenants)) {
            return tenants
                .Where(kvp => IsConcreteTenantRole(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToHashSet(StringComparer.Ordinal);
        }

        return [];
    }

    private protected static IEnumerable<KeyValuePair<string, TenantRole>> GetVisibleUserTenants(
        TenantIndexReadModel indexModel,
        string requesterUserId,
        Dictionary<string, TenantRole> targetUserTenants,
        bool canViewAllTargetTenants) {
        if (canViewAllTargetTenants) {
            return targetUserTenants.Where(kvp => IsConcreteTenantRole(kvp.Value));
        }

        if (!indexModel.UserTenants.TryGetValue(requesterUserId, out Dictionary<string, TenantRole>? requesterTenants)) {
            return [];
        }

        HashSet<string> requesterOwnedTenantIds = requesterTenants
            .Where(kvp => kvp.Value == TenantRole.TenantOwner)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.Ordinal);

        return targetUserTenants.Where(kvp => IsConcreteTenantRole(kvp.Value) && requesterOwnedTenantIds.Contains(kvp.Key));
    }

    private protected static PaginatedResult<TResult> Paginate<TSource, TResult>(
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

    private protected static PaginatedResult<TenantAuditEntry> PaginateAuditEntries(
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

    private protected static string GetAuditCursor(TenantAuditEntry entry) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{entry.Timestamp.UtcDateTime.Ticks:D20}:{entry.EventId}");

    private protected static TenantAuditQueryPayload DeserializeAuditPayload(byte[]? payload) {
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

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement root, string propertyName) {
        if (!root.TryGetProperty(propertyName, out JsonElement element)
            || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) {
            return null;
        }

        return element.TryGetDateTimeOffset(out DateTimeOffset value) ? value : null;
    }

    private protected PaginatedResult<TResult> ProtectCursor<TResult>(
        PaginatedResult<TResult> result,
        string queryType,
        string scope)
        => string.IsNullOrWhiteSpace(result.Cursor)
            ? result
            : result with { Cursor = CursorCodec.Encode(queryType, scope, result.Cursor) };

    private protected QueryResult InvalidCursorResult(
        string queryType,
        string endpoint,
        string tenantId,
        string userId,
        string? failureReason) {
        Log.InvalidCursorRejected(
            Logger,
            queryType,
            endpoint,
            tenantId,
            userId,
            failureReason ?? "unknown");
        return new(false, default, ErrorMessage: "Invalid cursor.");
    }

    private protected void LogOrphanUserTenantMembershipFiltered(
        string? correlationId,
        string requesterUserId,
        string targetUserId,
        string orphanTenantId) {
        if (_loggedOrphanMemberships.Add((targetUserId, orphanTenantId))) {
            Log.OrphanUserTenantMembershipFiltered(
                Logger,
                correlationId ?? string.Empty,
                GetUserTenantsQuery.QueryType,
                requesterUserId,
                targetUserId,
                orphanTenantId);
        }
    }

    private protected async Task<TValue?> GetStateAsync<TValue>(string key, CancellationToken cancellationToken)
        where TValue : class {
        ReadModelEntry<TValue>? entry = await _store
            .GetAsync<TValue>(StateStoreName, key, cancellationToken)
            .ConfigureAwait(false);
        return entry?.Value;
    }

    private protected async Task<bool> IsAuthorizedForTenantAsync(string userId, TenantReadModel model, CancellationToken cancellationToken) {
        if (model.Members.TryGetValue(userId, out TenantRole role) && IsConcreteTenantRole(role)) {
            return true;
        }

        return await IsGlobalAdminAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    private protected async Task<bool> IsGlobalAdminAsync(string userId, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        GlobalAdministratorReadModel? adminModel = await GetStateAsync<GlobalAdministratorReadModel>(
            GlobalAdminProjectionKey, cancellationToken).ConfigureAwait(false);

        return adminModel is not null && adminModel.Administrators.Contains(userId);
    }

    /// <summary>Common audit query payload fields parsed from the request body.</summary>
    private protected sealed record TenantAuditQueryPayload(
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
            Message = "Invalid tenant query cursor rejected at handler: QueryType={QueryType}, Endpoint={Endpoint}, TenantId={TenantId}, UserId={UserId}, FailureReason={FailureReason}, Stage=TenantQueryHandler")]
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
            Message = "Filtered orphan tenant membership from query result: CorrelationId={CorrelationId}, QueryType={QueryType}, RequesterUserId={RequesterUserId}, TargetUserId={TargetUserId}, OrphanTenantId={OrphanTenantId}, Stage=TenantQueryHandler")]
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
