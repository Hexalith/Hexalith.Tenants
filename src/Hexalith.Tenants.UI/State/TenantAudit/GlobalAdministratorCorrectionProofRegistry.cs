using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Retains completed correction provenance for receipt navigation within one interactive circuit.</summary>
public sealed class GlobalAdministratorCorrectionProofRegistry
{
    private readonly object _sync = new();
    private readonly Queue<GlobalAdministratorCorrectionSnapshot> _retainedPairs = new();
    private readonly List<GlobalAdministratorReconciliationState> _pendingCompletions = [];

    internal void RememberCompletion(GlobalAdministratorReconciliationState completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if (completion.LifecycleState is not TenantCommandLifecycleState.Confirmed
            || !completion.HasCommandEventEvidence
            || completion.CommittedEventSequence is not > 0
            || completion.SubmittedAtUtc is null
            || completion.ConfirmedProjectionEvidence is not { IsCompleteEvidence: true, HasMore: false } confirmed
            || !GlobalAdministratorCorrectionSnapshot.ProjectionIsReadable(confirmed)
            || string.IsNullOrWhiteSpace(completion.CorrelationId)
            || string.IsNullOrWhiteSpace(completion.SourceAuditReference)
            || completion.SourceAuditTimestampUtc is null
            || (completion.ActionKind is GlobalAdministratorActionKind.Grant
                ? completion.GrantPreview?.IsComplete != true
                : completion.RemovePreview?.IsComplete != true))
        {
            return;
        }

        lock (_sync)
        {
            if (_pendingCompletions.All(existing => !string.Equals(existing.MessageId, completion.MessageId, StringComparison.Ordinal)))
            {
                _pendingCompletions.Add(completion);
            }
        }
    }

    internal void RememberCompletion(GlobalAdministratorCorrectionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.LifecycleState is not TenantCommandLifecycleState.Confirmed
            || snapshot.LastConfirmedProjectionEvidence is not { } confirmed
            || snapshot.MessageId is not { Length: > 0 } messageId
            || !snapshot.Intent.RequiredPreviewInputs.TryGetValue("originalTimestamp", out string? timestamp)
            || !DateTimeOffset.TryParse(timestamp, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTimeOffset sourceTime))
        {
            return;
        }

        RememberCompletion(new GlobalAdministratorReconciliationState(
            snapshot.IsRestoreAccessAction ? GlobalAdministratorActionKind.Grant : GlobalAdministratorActionKind.Remove,
            snapshot.TargetUserId, messageId, snapshot.CorrelationId, snapshot.LifecycleState,
            snapshot.GrantPreview, snapshot.HasCommandEventEvidence, snapshot.IsSubmissionAmbiguous,
            snapshot.SafeMessageKey, snapshot.SafeRecoveryKey, snapshot.RemovePreview, snapshot.RejectionCode)
        {
            CommittedEventSequence = snapshot.CommittedEventSequence,
            SubmittedAtUtc = snapshot.SubmittedAtUtc,
            SourceAuditReference = snapshot.OriginalAuditReference,
            SourceAuditTimestampUtc = sourceTime,
            ConfirmedProjectionEvidence = confirmed,
        });
    }

    internal IReadOnlyList<GlobalAdministratorReconciliationState> PendingCompletions()
    {
        lock (_sync)
        {
            return _pendingCompletions.ToArray();
        }
    }

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
            _pendingCompletions.RemoveAll(existing => string.Equals(existing.MessageId, snapshot.MessageId, StringComparison.Ordinal));
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
