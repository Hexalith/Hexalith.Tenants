using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Commands;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Adds the status-key message identifier to legacy records supplied by focused HTTP test doubles.
/// </summary>
/// <remarks>
/// Production status stores persist command identity fields. These tests configure lifecycle fields
/// only, so this adapter adds the message identifier required by the command pipeline without
/// fabricating a correlation identifier that may legitimately differ from the message identifier.
/// </remarks>
internal sealed class MessageIdentifyingCommandStatusStore(ICommandStatusStore inner) : ICommandStatusStore {
    /// <inheritdoc/>
    public Task WriteStatusAsync(
        string tenantId,
        string messageId,
        CommandStatusRecord status,
        CancellationToken cancellationToken = default)
        => inner.WriteStatusAsync(tenantId, messageId, status, cancellationToken);

    /// <inheritdoc/>
    public async Task<CommandStatusRecord?> ReadStatusAsync(
        string tenantId,
        string messageId,
        CancellationToken cancellationToken = default) {
        CommandStatusRecord? status = await inner
            .ReadStatusAsync(tenantId, messageId, cancellationToken)
            .ConfigureAwait(false);

        return status is null
            ? null
            : status with { MessageId = status.MessageId ?? messageId };
    }
}
