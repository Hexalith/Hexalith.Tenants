using System.Diagnostics.Metrics;

namespace Hexalith.Tenants.Telemetry;

/// <summary>
/// Custom metrics for the Tenants service using <see cref="System.Diagnostics.Metrics.Meter"/>.
/// Histograms natively track count, sum, and bucket distribution.
/// </summary>
internal static class TenantMetrics {

    /// <summary>The meter name registered with OpenTelemetry.</summary>
    public const string MeterName = "Hexalith.Tenants";

    private static readonly Meter _meter = new(MeterName);

    private static readonly Histogram<double> _commandDuration =
        _meter.CreateHistogram<double>("tenants.command.duration", "ms", "Tenant command processing duration");

    private static readonly HashSet<string> _knownCommandTypes = new(StringComparer.Ordinal)
    {
        "CreateTenant",
        "UpdateTenantInformation",
        "DisableTenant",
        "EnableTenant",
        "AddUserToTenant",
        "RemoveUserFromTenant",
        "ChangeUserRole",
        "SetTenantConfiguration",
        "RemoveTenantConfiguration",
        "AddGlobalAdministrator",
        "RemoveGlobalAdministrator",
        "RegisterGlobalAdministrator",
    };

    private static readonly HashSet<string> _knownQueryTypes = new(StringComparer.Ordinal)
    {
        "get-tenant",
        "list-tenants",
        "get-tenant-users",
        "get-user-tenants",
        "get-tenant-audit",
    };

    private static readonly Histogram<double> _projectionQueryDuration =
        _meter.CreateHistogram<double>("tenants.projection.query.duration", "ms", "Projection query processing duration");

    /// <summary>
    /// Records the duration of a tenant command processing operation.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="commandType">The command type name (sanitized against known types).</param>
    /// <param name="success">Whether the handler completed without throwing.</param>
    public static void RecordCommandDuration(double milliseconds, string commandType, bool success)
        => _commandDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("command_type", SanitizeCommandType(commandType)),
            new KeyValuePair<string, object?>("success", success));

    /// <summary>
    /// Records the duration of a projection query execution.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="queryType">The query type identifier.</param>
    public static void RecordQueryDuration(double milliseconds, string queryType)
        => _projectionQueryDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("query_type", SanitizeQueryType(queryType)));

    private static string SanitizeCommandType(string commandType)
        => !string.IsNullOrEmpty(commandType) && _knownCommandTypes.Contains(commandType) ? commandType : "unknown";

    private static string SanitizeQueryType(string queryType)
        => !string.IsNullOrEmpty(queryType) && _knownQueryTypes.Contains(queryType) ? queryType : "unknown";
}
