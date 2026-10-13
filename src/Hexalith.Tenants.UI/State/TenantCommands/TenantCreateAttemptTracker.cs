namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Circuit-local retention for complete create attempts, including original intent, baseline, timestamps,
/// delivery uncertainty, tracking and exact aggregate admission across component replacement.
/// </summary>
/// <remarks>
/// Scope is the interactive circuit, matching <see cref="TenantAggregateCommandAdmissionGate"/>. A brand
/// new circuit starts with no retained attempt, so the create flow treats a re-submit as a fresh attempt
/// only when nothing is retained for that tenant id; when a handle is retained the flow refreshes its
/// status and reuses the original message id instead of dispatching the same create twice.
/// </remarks>
public sealed class TenantCreateAttemptTracker
{
    private readonly object _sync = new();
    private readonly Dictionary<string, TenantCommandTrackingHandle> _handleByTenantId = new(StringComparer.Ordinal);

    /// <summary>Creates a tracker using the circuit's aggregate admission service.</summary>
    /// <param name="admissionGate">Circuit-scoped aggregate admission service.</param>
    public TenantCreateAttemptTracker(TenantAggregateCommandAdmissionGate admissionGate)
    {
        Attempts = new(admissionGate);
    }

    /// <summary>Creates the create adapter over the already-registered circuit attempt store.</summary>
    internal TenantCreateAttemptTracker(TenantCommandAttemptTracker attempts)
    {
        Attempts = attempts;
    }

    /// <summary>Gets the complete attempt store shared by tenant command surfaces.</summary>
    internal TenantCommandAttemptTracker Attempts { get; }

    /// <summary>Retains the original create intent, baseline, tracking and admission for replacement rendering.</summary>
    /// <param name="snapshot">Complete original create attempt.</param>
    public void Remember(TenantCreateCommandSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Intent);
        object owner = new();
        if (Attempts.TryBegin(snapshot.Intent.TenantId, owner, snapshot, out TenantRetainedCommandAttempt? attempt))
        {
            _ = Attempts.TryStartDelivery(attempt!, owner);
            _ = Attempts.TryCompleteDelivery(attempt!, snapshot, snapshot);
            Attempts.Detach(attempt, owner);
        }
    }

    /// <summary>
    /// Retains legacy tracking evidence for <paramref name="tenantId"/>. A handle alone cannot establish
    /// the original intent and baseline, so command surfaces fail closed until complete evidence exists.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id the attempt targets.</param>
    /// <param name="handle">Tracking handle returned when the command was accepted.</param>
    public void Remember(string tenantId, TenantCommandTrackingHandle handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(handle);

        lock (_sync)
        {
            _handleByTenantId[tenantId] = handle;
        }
    }

    /// <summary>
    /// Returns the retained attempt handle for <paramref name="tenantId"/>, when one is still tracked.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id to look up.</param>
    /// <returns>The retained handle, or <see langword="null"/> when nothing is tracked.</returns>
    public TenantCommandTrackingHandle? Find(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        lock (_sync)
        {
            if (Attempts.Find<TenantCreateCommandSnapshot>(tenantId) is { } snapshot)
            {
                return !snapshot.IsTerminal && snapshot.MessageId is { } messageId && snapshot.CorrelationId is { } correlationId
                    ? new TenantCommandTrackingHandle(messageId, correlationId, tenantId) { ExpectedDomain = Hexalith.Tenants.Contracts.Identity.TenantIdentity.Domain }
                    : null;
            }

            return _handleByTenantId.TryGetValue(tenantId, out TenantCommandTrackingHandle? handle)
                ? handle
                : null;
        }
    }

    /// <summary>
    /// Clears legacy tracking for <paramref name="tenantId"/>. Complete terminal outcomes remain retained
    /// independently of their already-released aggregate lease.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id to forget.</param>
    public void Forget(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        lock (_sync)
        {
            _ = _handleByTenantId.Remove(tenantId);
        }
    }
}
