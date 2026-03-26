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

    /// <summary>Tag key for command type.</summary>
    public const string TagCommandType = "tenants.command_type";

    /// <summary>Tag key for tenant ID (trace spans only, never on metrics).</summary>
    public const string TagTenantId = "tenants.tenant_id";

    /// <summary>Tag key for success/failure status.</summary>
    public const string TagSuccess = "tenants.success";

    /// <summary>Tag key for query type.</summary>
    public const string TagQueryType = "tenants.query_type";

    /// <summary>Gets the singleton <see cref="ActivitySource"/> instance.</summary>
    public static ActivitySource Instance { get; } = new(SourceName);
}
