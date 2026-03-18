using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Tenants.CommandApi.DomainProcessing;

internal static class DomainProcessorMismatchMessages
{
    public const string MissingHandleMethod = "No Handle method found for command type";

    public const string MissingApplyMethodOnState = "No matching Apply method found on state";
}

internal sealed class DomainServiceRequestHandler(
    IEnumerable<IDomainProcessor> processors,
    ILogger<DomainServiceRequestHandler> logger)
{
    public async Task<DomainServiceWireResult> ProcessAsync(DomainServiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (IDomainProcessor processor in processors)
        {
            try
            {
                DomainResult result = await processor.ProcessAsync(request.Command, request.CurrentState).ConfigureAwait(false);
                return DomainServiceWireResult.FromDomainResult(result);
            }
            catch (InvalidOperationException ex) when (IsProcessorMismatch(ex))
            {
                logger.LogDebug(
                    "Skipping processor {ProcessorType} for command type {CommandType}",
                    processor.GetType().Name,
                    request.Command.CommandType);
            }
        }

        throw new InvalidOperationException($"No domain processor found for command type '{request.Command.CommandType}'.");
    }

    private static bool IsProcessorMismatch(InvalidOperationException ex)
        => ex.Message.Contains(DomainProcessorMismatchMessages.MissingHandleMethod, StringComparison.Ordinal)
        || ex.Message.Contains(DomainProcessorMismatchMessages.MissingApplyMethodOnState, StringComparison.Ordinal);
}
