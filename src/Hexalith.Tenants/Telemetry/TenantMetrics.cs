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

    private static readonly Histogram<double> _eventProcessingDuration =
        _meter.CreateHistogram<double>("tenants.event.processing.duration", "ms", "Tenant event projection processing duration");

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
        "tenant",
        "global-administrators",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownProjectionCategories =
    new([
        "tenant",
        "global-administrators",
        "unknown",
    ], StringComparer.Ordinal);

    private static readonly HashSet<string> _knownTelemetryOutcomes =
    new([
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

    private static readonly HashSet<string> _knownProjectionStages =
    new([
        "domain-processing",
        "projection-dispatch",
        "projection-query",
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
        => RecordCommandDuration(milliseconds, commandType, success, success ? "success" : "failure");

    /// <summary>
    /// Records the duration of a tenant command processing operation.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="commandType">The command type name (sanitized against known types).</param>
    /// <param name="success">Whether the handler completed without throwing.</param>
    /// <param name="outcome">The bounded domain outcome.</param>
    public static void RecordCommandDuration(double milliseconds, string commandType, bool success, string outcome)
        => _commandDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("command_type", SanitizeCommandType(commandType)),
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("outcome", SanitizeOutcome(outcome)));

    /// <summary>
    /// Records the duration of a projection query execution.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="queryType">The query type identifier.</param>
    public static void RecordQueryDuration(double milliseconds, string queryType)
        => RecordQueryDuration(milliseconds, queryType, "success");

    /// <summary>
    /// Records the duration of a projection query execution.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    /// <param name="queryType">The query type identifier.</param>
    /// <param name="outcome">The bounded query outcome.</param>
    public static void RecordQueryDuration(double milliseconds, string queryType, string outcome)
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
    public static void RecordEventProcessingDuration(
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

    private static string SanitizeProjectionCategory(string projectionType)
        => !string.IsNullOrEmpty(projectionType) && _knownProjectionCategories.Contains(projectionType) ? projectionType : "unknown";

    private static string SanitizeProjectionDomain(string domain)
        => domain is "tenants" or "global-administrators" ? domain : "unknown";

    private static string SanitizeProjectionStage(string stage)
        => !string.IsNullOrEmpty(stage) && _knownProjectionStages.Contains(stage) ? stage : "unknown";

    private static string SanitizeOutcome(string outcome)
        => !string.IsNullOrEmpty(outcome) && _knownTelemetryOutcomes.Contains(outcome) ? outcome : "failure";

    private static string SanitizeProjectionWriteReason(string reason)
        => !string.IsNullOrEmpty(reason) && _knownProjectionWriteReasons.Contains(reason) ? reason : "unknown";

    private static string SanitizeStateKeyCategory(string stateKeyCategory)
        => !string.IsNullOrEmpty(stateKeyCategory) && _knownStateKeyCategories.Contains(stateKeyCategory) ? stateKeyCategory : "unknown";
}
