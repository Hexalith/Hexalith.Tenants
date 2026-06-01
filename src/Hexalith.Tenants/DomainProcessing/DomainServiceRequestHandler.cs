using System.Diagnostics;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Telemetry;

namespace Hexalith.Tenants.DomainProcessing;

internal static class DomainProcessorMismatchMessages {
    public const string MissingHandleMethod = "No Handle method found for command type";
}

internal sealed partial class DomainServiceRequestHandler(
    IEnumerable<IDomainProcessor> processors,
    ILogger<DomainServiceRequestHandler> logger) {
    private const string DomainProcessingStage = "domain-processing";
    private const string FailureOutcome = "failure";
    private const string NoOpOutcome = "noop";
    private const string RejectionOutcome = "rejection";
    private const string SuccessOutcome = "success";

    public async Task<DomainServiceWireResult> ProcessAsync(DomainServiceRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string commandType = request.Command.CommandType;
        Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        string outcome = FailureOutcome;

        _ = (activity?.SetTag(TenantActivitySource.TagCommandType, commandType));
        _ = (activity?.SetTag(TenantActivitySource.TagTenantId, request.Command.TenantId));
        _ = (activity?.SetTag(TenantActivitySource.TagDomain, request.Command.Domain));
        _ = (activity?.SetTag(TenantActivitySource.TagAggregateId, request.Command.AggregateId));
        _ = (activity?.SetTag(TenantActivitySource.TagCorrelationId, request.Command.CorrelationId));
        if (!string.IsNullOrWhiteSpace(request.Command.CausationId)) {
            _ = (activity?.SetTag(TenantActivitySource.TagCausationId, request.Command.CausationId));
        }

        _ = (activity?.SetTag(TenantActivitySource.TagStage, DomainProcessingStage));

        MissingApplyMethodException? firstMissingApply = null;

        try {
            foreach (IDomainProcessor processor in processors) {
                try {
                    DomainResult result = await processor.ProcessAsync(request.Command, request.CurrentState).ConfigureAwait(false);
                    success = true;
                    outcome = GetOutcome(result);
                    CommandProcessed(
                        logger,
                        request.Command.CorrelationId,
                        request.Command.CausationId ?? string.Empty,
                        request.Command.TenantId,
                        request.Command.Domain,
                        request.Command.AggregateId,
                        commandType,
                        DomainProcessingStage,
                        outcome,
                        success);
                    return DomainServiceWireResult.FromDomainResult(result);
                }
                catch (MissingApplyMethodException ex) {
                    firstMissingApply ??= ex;
                    logger.LogDebug(
                        "Skipping processor {ProcessorType} for command type {CommandType}: state cannot apply event in stream",
                        processor.GetType().Name,
                        request.Command.CommandType);
                }
                catch (InvalidOperationException ex) when (IsProcessorMismatch(ex)) {
                    logger.LogDebug(
                        "Skipping processor {ProcessorType} for command type {CommandType}",
                        processor.GetType().Name,
                        request.Command.CommandType);
                }
            }

            if (firstMissingApply is not null) {
                throw firstMissingApply;
            }

            throw new InvalidOperationException($"No domain processor found for command type '{request.Command.CommandType}'.");
        }
        catch (Exception ex) {
            _ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
            CommandProcessingFailed(
                logger,
                ex,
                request.Command.CorrelationId,
                request.Command.CausationId ?? string.Empty,
                request.Command.TenantId,
                request.Command.Domain,
                request.Command.AggregateId,
                commandType,
                DomainProcessingStage,
                FailureOutcome,
                ex.GetType().Name);
            throw;
        }
        finally {
            stopwatch.Stop();
            _ = (activity?.SetTag(TenantActivitySource.TagSuccess, success));
            _ = (activity?.SetTag(TenantActivitySource.TagOutcome, outcome));
            TenantMetrics.RecordCommandDuration(stopwatch.Elapsed.TotalMilliseconds, commandType, success, outcome);
            activity?.Dispose();
        }
    }

    private static string GetOutcome(DomainResult result) =>
        result switch {
            { IsSuccess: true } => SuccessOutcome,
            { IsRejection: true } => RejectionOutcome,
            { IsNoOp: true } => NoOpOutcome,
            _ => FailureOutcome,
        };

    private static bool IsProcessorMismatch(InvalidOperationException ex)
        => ex.Message.Contains(DomainProcessorMismatchMessages.MissingHandleMethod, StringComparison.Ordinal);

    [LoggerMessage(
        EventId = 100201,
        Level = LogLevel.Information,
        Message = "Tenant command processed: CorrelationId={CorrelationId}, CausationId={CausationId}, TenantId={TenantId}, Domain={Domain}, AggregateId={AggregateId}, CommandType={CommandType}, Stage={Stage}, Outcome={Outcome}, Success={Success}")]
    private static partial void CommandProcessed(
        ILogger logger,
        string correlationId,
        string causationId,
        string tenantId,
        string domain,
        string aggregateId,
        string commandType,
        string stage,
        string outcome,
        bool success);

    [LoggerMessage(
        EventId = 100202,
        Level = LogLevel.Error,
        Message = "Tenant command processing failed: CorrelationId={CorrelationId}, CausationId={CausationId}, TenantId={TenantId}, Domain={Domain}, AggregateId={AggregateId}, CommandType={CommandType}, Stage={Stage}, Outcome={Outcome}, ExceptionType={ExceptionType}")]
    private static partial void CommandProcessingFailed(
        ILogger logger,
        Exception exception,
        string correlationId,
        string causationId,
        string tenantId,
        string domain,
        string aggregateId,
        string commandType,
        string stage,
        string outcome,
        string exceptionType);
}
