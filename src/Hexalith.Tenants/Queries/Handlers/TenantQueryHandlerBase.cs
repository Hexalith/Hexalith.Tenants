using Hexalith.Tenants.Authorization;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Serialization;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Queries.Handlers;

/// <summary>
/// Base class for the tenant <see cref="IDomainQueryHandler"/> implementations. Holds the read-model
/// access, RBAC-scoped visibility, pagination, and protected-cursor logic that used to live in the
/// retired <c>TenantsProjectionActor</c>. Reads go through the platform <see cref="IReadModelStore"/>
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

    private readonly ReadModelFreshnessThresholds _freshnessThresholds;
    private readonly IReadModelStore _store;
    private readonly ITenantsGlobalAdministratorAuthority _administratorAuthority;
    private readonly TenantTelemetry _telemetry;
    private readonly TimeProvider _timeProvider;

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
    protected TenantQueryHandlerBase(
        IReadModelStore store,
        ITenantsGlobalAdministratorAuthority administratorAuthority,
        IQueryCursorCodec cursorCodec,
        TenantTelemetry telemetry,
        ILogger logger,
        IOptions<ReadModelFreshnessOptions>? freshnessOptions = null,
        TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(cursorCodec);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(logger);
        _store = store;
        _administratorAuthority = administratorAuthority ?? throw new ArgumentNullException(nameof(administratorAuthority));
        CursorCodec = cursorCodec;
        _telemetry = telemetry;
        Logger = logger;
        _freshnessThresholds = (freshnessOptions?.Value ?? new ReadModelFreshnessOptions()).ToThresholds();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public virtual string Domain => GetTenantQuery.Domain;

    /// <inheritdoc/>
    public abstract string QueryType { get; }

    /// <summary>Gets the persisted read-model store for domain-specific read composition.</summary>
    private protected IReadModelStore ReadModelStore => _store;

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

    private protected static QueryResult CreateSuccessResult(JsonElement payload, string? projectionType, string? eTag)
        => TenantQueryResult.FromPayload(payload, projectionType, eTag);

    private protected QueryResult CreateSuccessResult(
        JsonElement payload,
        string? projectionType,
        IReadModelFreshness? readModel,
        string? eTag,
        QueryPagingMetadata? paging = null)
        => TenantQueryResult.FromPayload(
            payload,
            projectionType,
            readModel,
            _freshnessThresholds,
            _timeProvider.GetUtcNow(),
            eTag,
            paging);

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

    /// <summary>
    /// Resolves standard pagination. Present envelope paging alone governs the page; otherwise only the
    /// legacy camelCase payload <c>cursor</c> and <c>pageSize</c> fields written by the REST controllers apply.
    /// </summary>
    /// <param name="envelope">The authenticated query envelope.</param>
    /// <returns>The cursor, bounded page size, optional validation error and paging source.</returns>
    private protected static TenantQueryPaginationPayload ResolveStandardPagination(QueryEnvelope envelope)
        => envelope.Paging is { } paging
            ? TenantQueryEnvelopePaging.Resolve(paging, TenantQueryPaginationPolicy.ClampStandardPageSize)
            : TenantQueryPaginationPayloadParser.DeserializeStandardPayload(envelope.Payload);

    /// <summary>
    /// Resolves audit pagination. Present envelope paging alone governs the page; otherwise the cursor and
    /// page size parsed from the legacy camelCase payload fields apply.
    /// </summary>
    /// <param name="envelope">The authenticated query envelope.</param>
    /// <param name="query">The parsed audit payload.</param>
    /// <returns>The cursor, bounded page size, optional validation error and paging source.</returns>
    private protected static TenantQueryPaginationPayload ResolveAuditPagination(QueryEnvelope envelope, TenantAuditQueryPayload query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return envelope.Paging is { } paging
            ? TenantQueryEnvelopePaging.Resolve(paging, TenantQueryPaginationPolicy.ClampAuditPageSize)
            : new(query.Cursor, query.PageSize);
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

        var requesterOwnedTenantIds = requesterTenants
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
        CancellationToken cancellationToken,
        bool distinguishOwners = false) {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<TenantAuditEntry> ordered = entries
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.EventId, StringComparer.Ordinal)
            .ThenBy(e => distinguishOwners ? TenantAuditOwnership.GetOwner(e, "system") : null, StringComparer.Ordinal);

        if (cursor is not null) {
            ordered = ordered.Where(e => string.Compare(GetAuditCursor(e, distinguishOwners), cursor, StringComparison.Ordinal) > 0);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var page = ordered.Take(pageSize + 1).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        bool hasMore = page.Count > pageSize;
        if (hasMore) {
            page.RemoveAt(page.Count - 1);
        }

        string? nextCursor = hasMore ? GetAuditCursor(page[^1], distinguishOwners) : null;
        return new PaginatedResult<TenantAuditEntry>(page, nextCursor, hasMore);
    }

    private protected static string GetAuditCursor(TenantAuditEntry entry, bool distinguishOwners = false)
    {
        string eventKey = distinguishOwners
            ? string.Concat(entry.EventId.Select(value => ((int)value).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)))
                + "!" + TenantAuditOwnership.GetOwner(entry, "system")
            : entry.EventId;
        return string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{entry.Timestamp.UtcDateTime.Ticks:D20}:{eventKey}");
    }

    /// <summary>
    /// Parses the audit filters, owner selector and legacy paging fields. Filters accept the camelCase names
    /// written by the REST controllers and the CLR names written by generic gateway callers, with the same
    /// validation; both spellings of one filter must agree.
    /// </summary>
    /// <param name="payload">The serialized audit query payload.</param>
    /// <param name="includePaging">Whether the legacy camelCase <c>cursor</c> and <c>pageSize</c> fields are read.</param>
    /// <returns>The parsed audit payload or its validation failure.</returns>
    private protected static TenantAuditQueryPayload DeserializeAuditPayload(byte[]? payload, bool includePaging = true) {
        if (payload is null || payload.Length == 0) {
            return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, null);
        }

        try {
            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) {
                return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, QueryAdapterFailureReason.InvalidEnvelope);
            }

            if (!TryGetDateTimeOffset(root, "from", "From", out DateTimeOffset? from)
                || !TryGetDateTimeOffset(root, "to", "To", out DateTimeOffset? to))
            {
                return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, QueryAdapterFailureReason.InvalidEnvelope);
            }
            string? cursor = null;
            if (includePaging && root.TryGetProperty("cursor", out JsonElement cursorEl) && cursorEl.ValueKind == JsonValueKind.String)
            {
                try
                {
                    cursor = cursorEl.GetString();
                }
                catch (InvalidOperationException)
                {
                    // JSON strings can contain escaped unpaired surrogates that fail only when decoded.
                    return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, QueryAdapterFailureReason.InvalidEnvelope);
                }
            }
            int pageSize = TenantQueryPaginationPolicy.AuditDefaultPageSize;
            if (includePaging && root.TryGetProperty("pageSize", out JsonElement pageSizeEl)
                && pageSizeEl.ValueKind == JsonValueKind.Number
                && pageSizeEl.TryGetInt32(out int parsedPageSize)) {
                pageSize = parsedPageSize;
            }

            pageSize = TenantQueryPaginationPolicy.ClampAuditPageSize(pageSize);

            if (!TryGetCategory(root, "category", out AuditEventCategory? category, out string? errorMessage)
                || !TryGetCategory(root, "Category", out AuditEventCategory? clrCategory, out string? clrErrorMessage))
            {
                return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, QueryAdapterFailureReason.InvalidEnvelope);
            }

            errorMessage ??= clrErrorMessage;
            if (errorMessage is null && category is not null && clrCategory is not null && category != clrCategory)
            {
                // Both spellings of the filter were supplied with different meanings: refuse rather than pick one.
                errorMessage = QueryAdapterFailureReason.InvalidEnvelope;
            }

            category ??= clrCategory;

            if (errorMessage is null && from is not null && to is not null && from > to) {
                errorMessage = $"{QueryAdapterFailureReason.InvalidEnvelope}: Invalid audit query payload: 'from' must not be after 'to'.";
            }

            string? stream = null;
            if (root.TryGetProperty("stream", out JsonElement streamElement) && streamElement.ValueKind != JsonValueKind.Null)
            {
                if (streamElement.ValueKind == JsonValueKind.String)
                {
                    try
                    {
                        stream = streamElement.GetString();
                    }
                    catch (InvalidOperationException)
                    {
                        // JSON strings can contain escaped unpaired surrogates that fail only when decoded.
                        return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, QueryAdapterFailureReason.InvalidEnvelope);
                    }
                }
                else
                {
                    errorMessage = QueryAdapterFailureReason.InvalidEnvelope;
                }
            }

            return new(from, to, category, cursor, pageSize, stream, errorMessage);
        }
        catch (JsonException) {
            return new(null, null, null, null, TenantQueryPaginationPolicy.AuditDefaultPageSize, null, QueryAdapterFailureReason.InvalidEnvelope);
        }
    }

    /// <summary>
    /// Reads one date filter from its camelCase and CLR spellings; both must parse and, when both are set, agree.
    /// </summary>
    private static bool TryGetDateTimeOffset(JsonElement root, string camelCaseName, string clrName, out DateTimeOffset? value)
    {
        value = null;
        if (!TryGetDateTimeOffset(root, camelCaseName, out DateTimeOffset? camelCaseValue)
            || !TryGetDateTimeOffset(root, clrName, out DateTimeOffset? clrValue)
            || camelCaseValue is not null && clrValue is not null && camelCaseValue.Value != clrValue.Value)
        {
            return false;
        }

        value = camelCaseValue ?? clrValue;
        return true;
    }

    /// <summary>
    /// Reads one spelling of the audit category filter with the established validation. A non-string or unknown
    /// category is reported through <paramref name="errorMessage"/>; an undecodable string makes the whole payload invalid.
    /// </summary>
    /// <returns><see langword="false"/> when the category string cannot be decoded.</returns>
    private static bool TryGetCategory(JsonElement root, string propertyName, out AuditEventCategory? category, out string? errorMessage)
    {
        category = null;
        errorMessage = null;
        if (!root.TryGetProperty(propertyName, out JsonElement categoryEl)
            || categoryEl.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (categoryEl.ValueKind != JsonValueKind.String)
        {
            errorMessage = QueryAdapterFailureReason.InvalidEnvelope;
            return true;
        }

        string? categoryValue;
        try
        {
            categoryValue = categoryEl.GetString();
        }
        catch (InvalidOperationException)
        {
            // JSON strings can contain escaped unpaired surrogates that fail only when decoded.
            return false;
        }

        if (!string.IsNullOrWhiteSpace(categoryValue))
        {
            if (Enum.TryParse(categoryValue, ignoreCase: true, out AuditEventCategory parsed))
            {
                category = parsed;
            }
            else
            {
                errorMessage = $"{QueryAdapterFailureReason.InvalidEnvelope}: Invalid audit category: {categoryValue}";
            }
        }

        return true;
    }

    private static bool TryGetDateTimeOffset(JsonElement root, string propertyName, out DateTimeOffset? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out JsonElement element)
            || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        try
        {
            if (element.ValueKind != JsonValueKind.String || !element.TryGetDateTimeOffset(out DateTimeOffset parsed))
            {
                return false;
            }

            value = parsed;
            return true;
        }
        catch (InvalidOperationException)
        {
            // Embedded unpaired surrogates can fail during date decoding instead of returning false.
            return false;
        }
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
        return new(false, default, ErrorMessage: QueryAdapterFailureReason.InvalidCursor);
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
        ReadModelEntry<TValue>? entry = await GetStateEntryAsync<TValue>(key, cancellationToken).ConfigureAwait(false);
        return entry?.Value;
    }

    private protected async Task<ReadModelEntry<TValue>?> GetStateEntryAsync<TValue>(string key, CancellationToken cancellationToken)
        where TValue : class => await _store
            .GetAsync<TValue>(StateStoreName, key, cancellationToken)
            .ConfigureAwait(false);

    private protected async Task<bool> IsAuthorizedForTenantAsync(QueryEnvelope envelope, TenantReadModel model, CancellationToken cancellationToken) {
        if (model.Members.TryGetValue(envelope.UserId, out TenantRole role) && IsConcreteTenantRole(role)) {
            return true;
        }

        return await IsGlobalAdminAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-evaluates global-administrator authority from the final verified native source observation.
    /// </summary>
    /// <remarks>
    /// <see cref="QueryEnvelope.IsGlobalAdmin"/> is untrusted wire data (EventStore Story 5.5, FR28) and never grants
    /// access on its own: the acting user must be a current member of the authenticated native global-administrators source.
    /// </remarks>
    private protected Task<bool> IsGlobalAdminAsync(QueryEnvelope envelope, CancellationToken cancellationToken)
        => IsGlobalAdminAsync(envelope.UserId, cancellationToken);

    private protected Task<bool> IsGlobalAdminAsync(string userId, CancellationToken cancellationToken)
        => _administratorAuthority.IsCurrentGlobalAdministratorAsync(userId, cancellationToken);

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
