using System.Diagnostics;
using System.Diagnostics.Metrics;

using Hexalith.EventStore.DomainService;

namespace Hexalith.Tenants.Telemetry;

/// <summary>
/// The Tenants domain's bounded OpenTelemetry instruments (query/projection duration histograms with
/// cardinality-sanitized tags), rehomed onto the platform <see cref="EventStoreDomainDiagnostics"/>
/// convention (Epic A5). The domain no longer declares its own <see cref="ActivitySource"/>/<see cref="Meter"/>:
/// the source/meter are owned by the platform (<c>Hexalith.EventStore.Domain.tenants</c>) and registered with
/// OpenTelemetry by <c>AddEventStoreDomainTelemetry("tenants")</c>. This type preserves the domain-specific
/// span names, tag keys, and metric sanitization (the R-007 secret-leak / cardinality gate) that the platform
/// convention does not provide. Registered as a singleton and injected by the query/projection emitters.
/// </summary>
public sealed class TenantTelemetry {
    /// <summary>Span name for projection query execution.</summary>
    public const string QueryExecute = "Tenants.Projection.Query";

    /// <summary>Span name for tenant projection event processing.</summary>
    public const string ProjectionProject = "Tenants.Projection.Project";

    /// <summary>Tag key for aggregate ID (trace spans only, never on metrics).</summary>
    public const string TagAggregateId = "tenants.aggregate_id";

    /// <summary>Tag key for causation availability status.</summary>
    public const string TagCausationIdStatus = "tenants.causation_id_status";

    /// <summary>Tag key for correlation ID (trace spans only, never on metrics).</summary>
    public const string TagCorrelationId = "tenants.correlation_id";

    /// <summary>Tag key for domain.</summary>
    public const string TagDomain = "tenants.domain";

    /// <summary>Tag key for event count.</summary>
    public const string TagEventCount = "tenants.event_count";

    /// <summary>Tag key for event type summary.</summary>
    public const string TagEventTypes = "tenants.event_types";

    /// <summary>Tag key for bounded outcome.</summary>
    public const string TagOutcome = "tenants.outcome";

    /// <summary>Tag key for projection type.</summary>
    public const string TagProjectionType = "tenants.projection_type";

    /// <summary>Tag key for query type.</summary>
    public const string TagQueryType = "tenants.query_type";

    /// <summary>Tag key for processing stage.</summary>
    public const string TagStage = "tenants.stage";

    /// <summary>Tag key for tenant ID (trace spans only, never on metrics).</summary>
    public const string TagTenantId = "tenants.tenant_id";

    private static readonly HashSet<string> _knownQueryTypes = new([
        "get-tenant",
        "list-tenants",
        "get-tenant-users",
        "get-user-tenants",
        "get-tenant-audit",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownProjectionCategories = new([
        "tenant",
        "global-administrators",
        "unknown",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownProjectionStages = new([
        "domain-processing",
        "projection-dispatch",
        "projection-query",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownTelemetryOutcomes = new([
        "success",
        "rejection",
        "noop",
        "failure",
        "forbidden",
        "unknown-query",
        "completed",
        "unsupported-domain",
        "invalid-identity",
        "retry-recovered",
        "retry-exhausted",
    ], StringComparer.Ordinal);

    private readonly ActivitySource _activitySource;
    private readonly Histogram<double> _projectionQueryDuration;
    private readonly Histogram<double> _eventProcessingDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantTelemetry"/> class, sourcing its activity source and
    /// meter from the platform-owned <see cref="EventStoreDomainDiagnostics"/> for the tenants domain.
    /// </summary>
    /// <param name="diagnostics">The platform domain diagnostics (convention-named source/meter).</param>
    public TenantTelemetry(EventStoreDomainDiagnostics diagnostics) {
        ArgumentNullException.ThrowIfNull(diagnostics);
        _activitySource = diagnostics.ActivitySource;
        _projectionQueryDuration = diagnostics.Meter.CreateHistogram<double>(
            "tenants.projection.query.duration", "ms", "Projection query processing duration");
        _eventProcessingDuration = diagnostics.Meter.CreateHistogram<double>(
            "tenants.event.processing.duration", "ms", "Tenant event projection processing duration");
    }

    /// <summary>Starts an internal-kind activity for the given bounded span name.</summary>
    /// <param name="name">The bounded span name (<see cref="QueryExecute"/> or <see cref="ProjectionProject"/>).</param>
    /// <returns>The started activity, or <c>null</c> when no listener is registered.</returns>
    public Activity? StartActivity(string name) => _activitySource.StartActivity(name, ActivityKind.Internal);

    /// <summary>
    /// Records the duration of a projection query execution.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="queryType">The query type identifier.</param>
    /// <param name="outcome">The bounded query outcome.</param>
    public void RecordQueryDuration(double milliseconds, string queryType, string outcome)
        => _projectionQueryDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("query_type", SanitizeQueryType(queryType)),
            new KeyValuePair<string, object?>("outcome", SanitizeOutcome(outcome)));

    /// <summary>
    /// Records tenant event projection processing duration.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="domain">The bounded domain.</param>
    /// <param name="projectionType">The bounded projection category.</param>
    /// <param name="stage">The bounded processing stage.</param>
    /// <param name="outcome">The bounded processing outcome.</param>
    public void RecordEventProcessingDuration(
        double milliseconds,
        string domain,
        string projectionType,
        string stage,
        string outcome)
        => _eventProcessingDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("domain", SanitizeProjectionDomain(domain)),
            new KeyValuePair<string, object?>("projection_type", SanitizeProjectionCategory(projectionType)),
            new KeyValuePair<string, object?>("stage", SanitizeProjectionStage(stage)),
            new KeyValuePair<string, object?>("outcome", SanitizeOutcome(outcome)));

    private static string SanitizeQueryType(string queryType)
        => !string.IsNullOrEmpty(queryType) && _knownQueryTypes.Contains(queryType) ? queryType : "unknown";

    private static string SanitizeProjectionCategory(string projectionType)
        => !string.IsNullOrEmpty(projectionType) && _knownProjectionCategories.Contains(projectionType) ? projectionType : "unknown";

    private static string SanitizeProjectionDomain(string domain)
        => domain is "tenants" or "global-administrators" ? domain : "unknown";

    private static string SanitizeProjectionStage(string stage)
        => !string.IsNullOrEmpty(stage) && _knownProjectionStages.Contains(stage) ? stage : "unknown";

    private static string SanitizeOutcome(string outcome)
        => !string.IsNullOrEmpty(outcome) && _knownTelemetryOutcomes.Contains(outcome) ? outcome : "failure";
}
