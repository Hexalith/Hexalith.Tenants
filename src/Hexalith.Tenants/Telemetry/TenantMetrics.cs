using System.Diagnostics.Metrics;

namespace Hexalith.Tenants.Telemetry;

/// <summary>
/// Custom metrics for the Tenants service using <see cref="System.Diagnostics.Metrics.Meter"/>.
/// Histograms natively track count, sum, and bucket distribution.
/// </summary>
internal static class TenantMetrics {
    private const string CommandTypeNamespacePrefix = "Hexalith.Tenants.Contracts.Commands.";

    /// <summary>The meter name registered with OpenTelemetry.</summary>
    public const string MeterName = "Hexalith.Tenants";

    private static readonly Meter _meter = new(MeterName);

    private static readonly Histogram<double> _commandDuration =
        _meter.CreateHistogram<double>("tenants.command.duration", "ms", "Tenant command processing duration");

    private static readonly Histogram<double> _projectionQueryDuration =
        _meter.CreateHistogram<double>("tenants.projection.query.duration", "ms", "Projection query processing duration");

    private static readonly Counter<long> _projectionWriteConflicts =
        _meter.CreateCounter<long>("tenants.projection.write.conflicts", "{attempt}", "Projection write optimistic concurrency conflicts");

    private static readonly HashSet<string> _knownCommandTypes = new([
        "CreateTenant",
        "UpdateTenant",
        "DisableTenant",
        "EnableTenant",
        "AddUserToTenant",
        "RemoveUserFromTenant",
        "ChangeUserRole",
        "SetTenantConfiguration",
        "RemoveTenantConfiguration",
        "SetGlobalAdministrator",
        "RemoveGlobalAdministrator",
        "BootstrapGlobalAdmin",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownQueryTypes =
    new([
        "get-tenant",
        "list-tenants",
        "get-tenant-users",
        "get-user-tenants",
        "get-tenant-audit",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownProjectionTypes =
    new([
        "TenantReadModel",
        "TenantAuditReadModel",
        "TenantIndexReadModel",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownProjectionWriteReasons =
    new([
        "guarded-save-conflict",
        "retry-exhausted",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownStateKeyCategories =
    new([
        "tenant read-model",
        "tenant audit",
        "tenant index",
    ], StringComparer.Ordinal);

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

    /// <summary>
    /// Records a projection write optimistic concurrency conflict attempt.
    /// </summary>
    /// <param name="stateKeyCategory">The bounded state key category.</param>
    /// <param name="projectionType">The bounded projection type.</param>
    /// <param name="reason">The bounded conflict reason.</param>
    /// <param name="success">Whether the conflict remained recoverable for the caller.</param>
    public static void RecordProjectionWriteConflict(
        string stateKeyCategory,
        string projectionType,
        string reason,
        bool success)
        => _projectionWriteConflicts.Add(
            1,
            new KeyValuePair<string, object?>("state_key_category", SanitizeStateKeyCategory(stateKeyCategory)),
            new KeyValuePair<string, object?>("projection_type", SanitizeProjectionType(projectionType)),
            new KeyValuePair<string, object?>("reason", SanitizeProjectionWriteReason(reason)),
            new KeyValuePair<string, object?>("success", success));

    private static string SanitizeCommandType(string commandType) {
        if (string.IsNullOrWhiteSpace(commandType)) {
            return "unknown";
        }

        if (_knownCommandTypes.Contains(commandType)) {
            return commandType;
        }

        if (commandType.StartsWith(CommandTypeNamespacePrefix, StringComparison.Ordinal)) {
            string shortName = commandType[CommandTypeNamespacePrefix.Length..];
            return _knownCommandTypes.Contains(shortName) ? shortName : "unknown";
        }

        return "unknown";
    }

    private static string SanitizeQueryType(string queryType)
        => !string.IsNullOrEmpty(queryType) && _knownQueryTypes.Contains(queryType) ? queryType : "unknown";

    private static string SanitizeProjectionType(string projectionType)
        => !string.IsNullOrEmpty(projectionType) && _knownProjectionTypes.Contains(projectionType) ? projectionType : "unknown";

    private static string SanitizeProjectionWriteReason(string reason)
        => !string.IsNullOrEmpty(reason) && _knownProjectionWriteReasons.Contains(reason) ? reason : "unknown";

    private static string SanitizeStateKeyCategory(string stateKeyCategory)
        => !string.IsNullOrEmpty(stateKeyCategory) && _knownStateKeyCategories.Contains(stateKeyCategory) ? stateKeyCategory : "unknown";
}
