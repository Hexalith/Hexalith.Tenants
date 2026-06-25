using System.Diagnostics;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Telemetry;

using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Routes <see cref="ProjectionRequest"/> to the projection handler responsible for the
/// requested <see cref="ProjectionRequest.Domain"/>. Unknown domains fail closed with
/// <see cref="StatusCodes.Status400BadRequest"/> instead of silently being projected as tenants.
/// </summary>
public sealed partial class ProjectionDispatcher(
    IReadModelStore store,
    TenantTelemetry telemetry,
    ILoggerFactory? loggerFactory = null,
    TimeProvider? timeProvider = null) {
    public const string TenantsDomain = "tenants";
    public const string GlobalAdministratorsDomain = "global-administrators";

    private const int MaxSpanEventTypes = 8;
    private const string CausationIdUnavailable = "unavailable-from-projection-dto";
    private const string CompletedOutcome = "completed";
    private const string FailureOutcome = "failure";
    private const string GlobalAdministratorsProjectionType = "global-administrators";
    private const string InvalidIdentityOutcome = "invalid-identity";
    private const string ProjectionDispatchStage = "projection-dispatch";
    private const string RetryExhaustedOutcome = "retry-exhausted";
    private const string TenantProjectionType = "tenant";
    private const string UnknownProjectionType = "unknown";
    private const string UnsupportedDomainOutcome = "unsupported-domain";

    private static readonly HashSet<string> s_knownEventTypeNames = new([
        nameof(GlobalAdministratorRemoved),
        nameof(GlobalAdministratorSet),
        nameof(TenantConfigurationRemoved),
        nameof(TenantConfigurationSet),
        nameof(TenantCreated),
        nameof(TenantDisabled),
        nameof(TenantEnabled),
        nameof(TenantUpdated),
        nameof(UserAddedToTenant),
        nameof(UserRemovedFromTenant),
        nameof(UserRoleChanged),
    ], StringComparer.Ordinal);

    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<IResult> DispatchAsync(ProjectionRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        ILogger<ProjectionDispatcher> logger = _loggerFactory.CreateLogger<ProjectionDispatcher>();
        using Activity? activity = telemetry.StartActivity(TenantTelemetry.ProjectionProject);
        var stopwatch = Stopwatch.StartNew();
        string outcome = FailureOutcome;
        string projectionType = GetProjectionType(request.Domain);
        string telemetryDomain = GetTelemetryDomain(request.Domain);
        string correlationId = GetCorrelationId(request);

        _ = (activity?.SetTag(TenantTelemetry.TagStage, ProjectionDispatchStage));
        _ = (activity?.SetTag(TenantTelemetry.TagTenantId, request.TenantId));
        _ = (activity?.SetTag(TenantTelemetry.TagDomain, telemetryDomain));
        _ = (activity?.SetTag(TenantTelemetry.TagAggregateId, request.AggregateId));
        _ = (activity?.SetTag(TenantTelemetry.TagProjectionType, projectionType));
        _ = (activity?.SetTag(TenantTelemetry.TagEventCount, request.Events.Length));
        _ = (activity?.SetTag(TenantTelemetry.TagEventTypes, BuildEventTypeSummary(request.Events)));
        _ = (activity?.SetTag(TenantTelemetry.TagCausationIdStatus, CausationIdUnavailable));
        if (!string.IsNullOrWhiteSpace(correlationId)) {
            _ = (activity?.SetTag(TenantTelemetry.TagCorrelationId, correlationId));
        }

        try {
            switch (request.Domain) {
                case TenantsDomain:
                    ProjectionResponse tenantsResponse = await new TenantProjectionHandler(
                        store,
                        _loggerFactory.CreateLogger<TenantProjectionHandler>(),
                        _timeProvider)
                        .ProjectAsync(request, cancellationToken).ConfigureAwait(false);
                    outcome = CompletedOutcome;
                    return Results.Ok(tenantsResponse);

                case GlobalAdministratorsDomain:
                    if (!GlobalAdministratorProjectionHandler.IsValidGlobalAdministratorIdentity(request)) {
                        outcome = InvalidIdentityOutcome;
                        _ = (activity?.SetStatus(ActivityStatusCode.Error, InvalidIdentityOutcome));
                        return Results.Problem(
                            detail: "Global-administrator projections must use tenant 'system' and aggregate 'global-administrators'.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Invalid global administrator projection identity");
                    }

                    ProjectionResponse globalAdminResponse = await new GlobalAdministratorProjectionHandler(store, _timeProvider)
                        .ProjectAsync(request, cancellationToken).ConfigureAwait(false);
                    outcome = CompletedOutcome;
                    return Results.Ok(globalAdminResponse);

                default:
                    outcome = UnsupportedDomainOutcome;
                    _ = (activity?.SetStatus(ActivityStatusCode.Error, UnsupportedDomainOutcome));
                    return Results.Problem(
                        detail: $"Unsupported projection domain '{request.Domain}'.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Unsupported projection domain");
            }
        }
        catch (Exception ex) {
            outcome = IsRetryExhausted(ex) ? RetryExhaustedOutcome : FailureOutcome;
            _ = (activity?.SetStatus(ActivityStatusCode.Error, outcome));
            ProjectionDispatchFailed(
                logger,
                ex,
                correlationId,
                request.TenantId,
                telemetryDomain,
                request.AggregateId,
                projectionType,
                ProjectionDispatchStage,
                outcome,
                ex.GetType().Name);
            throw;
        }
        finally {
            stopwatch.Stop();
            _ = (activity?.SetTag(TenantTelemetry.TagOutcome, outcome));
            telemetry.RecordEventProcessingDuration(
                stopwatch.Elapsed.TotalMilliseconds,
                telemetryDomain,
                projectionType,
                ProjectionDispatchStage,
                outcome);
            ProjectionDispatchCompleted(
                logger,
                correlationId,
                request.TenantId,
                telemetryDomain,
                request.AggregateId,
                projectionType,
                ProjectionDispatchStage,
                outcome,
                request.Events.Length,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static string BuildEventTypeSummary(IReadOnlyCollection<ProjectionEventDto> events) {
        HashSet<string> distinct = new(StringComparer.Ordinal);
        List<string> sample = new(MaxSpanEventTypes);
        int omitted = 0;

        foreach (ProjectionEventDto evt in events) {
            string eventTypeName = SanitizeEventTypeName(evt.EventTypeName);
            if (!distinct.Add(eventTypeName)) {
                continue;
            }

            if (sample.Count < MaxSpanEventTypes) {
                sample.Add(eventTypeName);
            }
            else {
                omitted++;
            }
        }

        string joined = string.Join(",", sample);
        return omitted > 0 ? $"{joined}+{omitted} more" : joined;
    }

    private static string GetCorrelationId(ProjectionRequest request) =>
        request.Events.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.CorrelationId))?.CorrelationId ?? string.Empty;

    private static string GetTelemetryDomain(string domain) =>
        domain is TenantsDomain or GlobalAdministratorsDomain ? domain : UnknownProjectionType;

    private static string GetProjectionType(string domain) =>
        domain switch {
            TenantsDomain => TenantProjectionType,
            GlobalAdministratorsDomain => GlobalAdministratorsProjectionType,
            _ => UnknownProjectionType,
        };

    private static string SanitizeEventTypeName(string? eventTypeName) {
        if (string.IsNullOrWhiteSpace(eventTypeName)) {
            return UnknownProjectionType;
        }

        int lastDotIndex = eventTypeName.LastIndexOf('.');
        string shortName = lastDotIndex >= 0 ? eventTypeName[(lastDotIndex + 1)..] : eventTypeName;
        return s_knownEventTypeNames.Contains(shortName) ? shortName : UnknownProjectionType;
    }

    private static bool IsRetryExhausted(Exception ex) =>
        ex is InvalidOperationException
        && ex.Message.Contains("optimistic-concurrency retry limit", StringComparison.Ordinal);

    [LoggerMessage(
        EventId = 100301,
        Level = LogLevel.Information,
        Message = "Tenant projection dispatch completed: CorrelationId={CorrelationId}, TenantId={TenantId}, Domain={Domain}, AggregateId={AggregateId}, ProjectionType={ProjectionType}, Stage={Stage}, Outcome={Outcome}, EventCount={EventCount}, DurationMs={DurationMs}")]
    private static partial void ProjectionDispatchCompleted(
        ILogger logger,
        string correlationId,
        string tenantId,
        string domain,
        string aggregateId,
        string projectionType,
        string stage,
        string outcome,
        int eventCount,
        double durationMs);

    [LoggerMessage(
        EventId = 100302,
        Level = LogLevel.Error,
        Message = "Tenant projection dispatch failed: CorrelationId={CorrelationId}, TenantId={TenantId}, Domain={Domain}, AggregateId={AggregateId}, ProjectionType={ProjectionType}, Stage={Stage}, Outcome={Outcome}, ExceptionType={ExceptionType}")]
    private static partial void ProjectionDispatchFailed(
        ILogger logger,
        Exception exception,
        string correlationId,
        string tenantId,
        string domain,
        string aggregateId,
        string projectionType,
        string stage,
        string outcome,
        string exceptionType);
}
