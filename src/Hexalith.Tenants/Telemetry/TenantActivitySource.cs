using System.Diagnostics;

namespace Hexalith.Tenants.Telemetry;

/// <summary>
/// Provides a single static <see cref="ActivitySource"/> for OpenTelemetry distributed tracing
/// across the Tenants service layer. Follows the EventStoreActivitySource pattern.
/// </summary>
internal static class TenantActivitySource {
    /// <summary>The source name registered with OpenTelemetry.</summary>
    public const string SourceName = "Hexalith.Tenants";

    /// <summary>Span name for tenant command processing.</summary>
    public const string CommandProcess = "Tenants.Command.Process";

    /// <summary>Span name for projection query execution.</summary>
    public const string QueryExecute = "Tenants.Projection.Query";

    /// <summary>Span name for tenant projection event processing.</summary>
    public const string ProjectionProject = "Tenants.Projection.Project";

    /// <summary>Tag key for aggregate ID (trace spans only, never on metrics).</summary>
    public const string TagAggregateId = "tenants.aggregate_id";

    /// <summary>Tag key for causation ID (trace spans only, never on metrics).</summary>
    public const string TagCausationId = "tenants.causation_id";

    /// <summary>Tag key for causation availability status.</summary>
    public const string TagCausationIdStatus = "tenants.causation_id_status";

    /// <summary>Tag key for command type.</summary>
    public const string TagCommandType = "tenants.command_type";

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

    /// <summary>Tag key for tenant ID (trace spans only, never on metrics).</summary>
    public const string TagTenantId = "tenants.tenant_id";

    /// <summary>Tag key for processing stage.</summary>
    public const string TagStage = "tenants.stage";

    /// <summary>Tag key for success/failure status.</summary>
    public const string TagSuccess = "tenants.success";

    /// <summary>Tag key for query type.</summary>
    public const string TagQueryType = "tenants.query_type";

    /// <summary>Gets the singleton <see cref="ActivitySource"/> instance.</summary>
    public static ActivitySource Instance { get; } = new(SourceName);
}
