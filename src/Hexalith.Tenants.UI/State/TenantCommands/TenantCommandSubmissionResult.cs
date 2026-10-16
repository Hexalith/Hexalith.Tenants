namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record TenantCommandSubmissionResult(
    TenantCommandLifecycleState State,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    string? SafeMessageKey = null,
    bool IsAmbiguousFailure = false)
{
    public static TenantCommandSubmissionResult Accepted(string messageId, string correlationId)
        => new(TenantCommandLifecycleState.Accepted, messageId, correlationId);

    public static TenantCommandSubmissionResult Rejected(string safeMessage, string? rejectionCode = null)
        => new(TenantCommandLifecycleState.Rejected, SafeMessage: safeMessage, RejectionCode: rejectionCode);

    /// <summary>Rejects with a localized Tenants resource key and a stable diagnostic code.</summary>
    /// <param name="safeMessageKey">Tenants resource key describing the rejection.</param>
    /// <param name="rejectionCode">Optional structured diagnostic code.</param>
    /// <returns>A rejected submission result carrying no gateway-authored visible text.</returns>
    public static TenantCommandSubmissionResult RejectedWithKey(
        string safeMessageKey,
        string? rejectionCode = null)
        => new(
            TenantCommandLifecycleState.Rejected,
            RejectionCode: rejectionCode,
            SafeMessageKey: safeMessageKey);

    public static TenantCommandSubmissionResult Failed(string safeMessage)
        => new(TenantCommandLifecycleState.Failed, SafeMessage: safeMessage);

    /// <summary>
    /// Fails with a Tenants resource key instead of literal text, so gateway-detected faults stay
    /// culture-aware. Flows resolve the key through the localizer and keep EN/FR parity.
    /// </summary>
    /// <param name="safeMessageKey">Tenants resource key describing the failure.</param>
    /// <returns>A failed submission result carrying the key.</returns>
    public static TenantCommandSubmissionResult FailedWithKey(string safeMessageKey)
        => new(TenantCommandLifecycleState.Failed, SafeMessageKey: safeMessageKey);

    /// <summary>
    /// Reports a submission outcome whose delivery cannot be proven. Retrying the same message id is safe,
    /// while minting another identity is not.
    /// </summary>
    /// <param name="messageId">Stable message id that may be redispatched.</param>
    /// <param name="safeMessageKey">Localized support-safe explanation.</param>
    /// <returns>An ambiguous submission result that retains dispatch ownership.</returns>
    public static TenantCommandSubmissionResult Ambiguous(string messageId, string safeMessageKey)
        => new(
            TenantCommandLifecycleState.RequestSent,
            MessageId: messageId,
            SafeMessageKey: safeMessageKey,
            IsAmbiguousFailure: true);

}
