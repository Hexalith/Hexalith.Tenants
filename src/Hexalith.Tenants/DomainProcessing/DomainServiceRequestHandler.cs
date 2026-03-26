using System.Diagnostics;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Telemetry;

namespace Hexalith.Tenants.DomainProcessing;

internal static class DomainProcessorMismatchMessages {
    public const string MissingHandleMethod = "No Handle method found for command type";

    public const string MissingApplyMethodOnState = "No matching Apply method found on state";
}

internal sealed class DomainServiceRequestHandler(
    IEnumerable<IDomainProcessor> processors,
    ILogger<DomainServiceRequestHandler> logger) {
    public async Task<DomainServiceWireResult> ProcessAsync(DomainServiceRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string commandType = request.Command.CommandType;
        Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();
        bool success = false;

        _ = (activity?.SetTag(TenantActivitySource.TagCommandType, commandType));
        _ = (activity?.SetTag(TenantActivitySource.TagTenantId, request.Command.TenantId));

        try {
            foreach (IDomainProcessor processor in processors) {
                try {
                    DomainResult result = await processor.ProcessAsync(request.Command, request.CurrentState).ConfigureAwait(false);
                    success = true;
                    return DomainServiceWireResult.FromDomainResult(result);
                }
                catch (InvalidOperationException ex) when (IsProcessorMismatch(ex)) {
                    logger.LogDebug(
                        "Skipping processor {ProcessorType} for command type {CommandType}",
                        processor.GetType().Name,
                        request.Command.CommandType);
                }
            }

            throw new InvalidOperationException($"No domain processor found for command type '{request.Command.CommandType}'.");
        }
        catch (Exception ex) {
            _ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
            throw;
        }
        finally {
            stopwatch.Stop();
            _ = (activity?.SetTag(TenantActivitySource.TagSuccess, success));
            TenantMetrics.RecordCommandDuration(stopwatch.Elapsed.TotalMilliseconds, commandType, success);
            activity?.Dispose();
        }
    }

    private static bool IsProcessorMismatch(InvalidOperationException ex)
        => ex.Message.Contains(DomainProcessorMismatchMessages.MissingHandleMethod, StringComparison.Ordinal)
        || ex.Message.Contains(DomainProcessorMismatchMessages.MissingApplyMethodOnState, StringComparison.Ordinal);
}
