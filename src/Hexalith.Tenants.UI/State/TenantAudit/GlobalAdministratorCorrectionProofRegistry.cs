namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Retains completed correction provenance for receipt navigation within one interactive circuit.</summary>
public sealed class GlobalAdministratorCorrectionProofRegistry
{
    private readonly object _sync = new();
    private readonly Queue<GlobalAdministratorCorrectionSnapshot> _retainedPairs = new();

    internal void Remember(GlobalAdministratorCorrectionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ProofLink is null
            || snapshot.LifecycleState is not Hexalith.Tenants.UI.State.TenantCommands.TenantCommandLifecycleState.Confirmed
            || snapshot.AuditState is not Hexalith.Tenants.UI.State.TenantCommands.TenantCommandAuditState.AuditAvailable
            || snapshot.SubmittedAtUtc is null
            || snapshot.CommittedEventSequence is not > 0 || string.IsNullOrWhiteSpace(snapshot.CorrelationId))
        {
            return;
        }

        lock (_sync)
        {
            if (_retainedPairs.Any(existing => Equals(existing.ProofLink, snapshot.ProofLink)))
            {
                return;
            }
            _retainedPairs.Enqueue(snapshot);
        }
    }

    internal GlobalAdministratorCorrectionSnapshot? Find(string reference)
    {
        lock (_sync)
        {
            return _retainedPairs.FirstOrDefault(snapshot => IsEndpoint(snapshot, reference));
        }
    }

    internal IReadOnlyList<GlobalAdministratorCorrectionSnapshot> FindAll(string reference)
    {
        lock (_sync)
        {
            return _retainedPairs.Where(snapshot => IsEndpoint(snapshot, reference)).ToArray();
        }
    }

    private static bool IsEndpoint(GlobalAdministratorCorrectionSnapshot snapshot, string reference)
        => string.Equals(snapshot.ProofLink?.OriginalAuditReference, reference, StringComparison.Ordinal)
            || string.Equals(snapshot.ProofLink?.CorrectiveAuditReference, reference, StringComparison.Ordinal);
}
