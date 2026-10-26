using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record TenantCommandStatusResult(
    CommandStatus? Status,
    string? SafeMessage = null,
    string? RejectionCode = null,
    int? EventCount = null,
    bool IsPending = false,
    bool IsRetryableFailure = false,
    bool HasVerifiedCommandIdentity = false,
    string? SafeMessageKey = null)
{
    /// <summary>Gets server-only command-specific commit proof after identity and aggregate scope validation.</summary>
    internal long? CommittedEventSequence { get; init; }

    /// <summary>Gets correlation recovered only from a complete exact MessageId and fixed-scope match.</summary>
    internal string? RecoveredCorrelationId { get; init; }

    /// <summary>Omits tracking and aggregate proof from copied diagnostics.</summary>
    public override string ToString()
        => $"{nameof(TenantCommandStatusResult)} {{ Status = {Status}, IsPending = {IsPending}, IsRetryableFailure = {IsRetryableFailure} }}";

    public static TenantCommandStatusResult Unknown(string safeMessage)
        => new(null, safeMessage);

    /// <summary>
    /// Represents normal status-store propagation lag without terminating the tracked command attempt.
    /// </summary>
    /// <param name="safeMessage">Support-safe pending status text.</param>
    /// <returns>A pending status lookup result.</returns>
    public static TenantCommandStatusResult Pending(string safeMessage)
        => new(null, safeMessage, IsPending: true);

    /// <summary>
    /// Represents a transient transport or response-parsing fault that may be retried within the attempt deadline.
    /// </summary>
    /// <param name="safeMessage">Support-safe retryable failure text.</param>
    /// <returns>A retryable status lookup result.</returns>
    public static TenantCommandStatusResult RetryableFailure(string safeMessage)
        => new(null, safeMessage, IsRetryableFailure: true);

    /// <summary>Reports unknown status using a whole-string Tenants resource.</summary>
    /// <param name="safeMessageKey">Localized support-safe status key.</param>
    /// <returns>An unknown status result without command proof.</returns>
    public static TenantCommandStatusResult UnknownWithKey(string safeMessageKey)
        => new(null, SafeMessageKey: safeMessageKey);

    /// <summary>Reports status-store propagation lag using a whole-string Tenants resource.</summary>
    /// <param name="safeMessageKey">Localized support-safe pending key.</param>
    /// <returns>A pending result without command proof.</returns>
    public static TenantCommandStatusResult PendingWithKey(string safeMessageKey)
        => new(null, IsPending: true, SafeMessageKey: safeMessageKey);

    /// <summary>Reports a transient status lookup fault using a whole-string Tenants resource.</summary>
    /// <param name="safeMessageKey">Localized support-safe retryable failure key.</param>
    /// <returns>A retryable result without command proof.</returns>
    public static TenantCommandStatusResult RetryableFailureWithKey(string safeMessageKey)
        => new(null, IsRetryableFailure: true, SafeMessageKey: safeMessageKey);
}
