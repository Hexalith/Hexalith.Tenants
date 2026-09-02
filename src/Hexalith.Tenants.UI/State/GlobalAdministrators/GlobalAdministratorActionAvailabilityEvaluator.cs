using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Pure evaluator for fixed-scope global-administrator grant and removal availability.</summary>
public static class GlobalAdministratorActionAvailabilityEvaluator
{
    /// <summary>Evaluates grant availability without inferring target existence.</summary>
    /// <param name="evidence">Immutable fixed-scope evidence.</param>
    /// <returns>A deterministic availability result.</returns>
    public static GlobalAdministratorActionAvailability EvaluateGrant(GlobalAdministratorActionEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        GlobalAdministratorActionAvailability? common = EvaluateCommon(
            evidence,
            GlobalAdministratorActionKind.Grant);
        if (common is not null)
        {
            return common;
        }

        if (!evidence.SupportsTrackedGrantDispatch)
        {
            return Blocked(
                GlobalAdministratorActionKind.Grant,
                GlobalAdministratorActionUnavailableReason.MissingLifecycleSupport);
        }

        return evidence.IsGrantPreviewReady
            ? Available(GlobalAdministratorActionKind.Grant)
            : Blocked(
                GlobalAdministratorActionKind.Grant,
                GlobalAdministratorActionUnavailableReason.MissingConsequencePreview);
    }

    /// <summary>Evaluates removal availability for one visible target.</summary>
    /// <param name="evidence">Immutable fixed-scope evidence.</param>
    /// <param name="targetUserId">Visible removal target.</param>
    /// <returns>A deterministic availability result.</returns>
    public static GlobalAdministratorActionAvailability EvaluateRemove(
        GlobalAdministratorActionEvidence evidence,
        string? targetUserId)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        GlobalAdministratorActionAvailability? common = EvaluateCommon(evidence, GlobalAdministratorActionKind.Remove);
        if (common is not null)
        {
            return common;
        }

        if (!evidence.SupportsTrackedRemoveDispatch)
        {
            return Blocked(
                GlobalAdministratorActionKind.Remove,
                GlobalAdministratorActionUnavailableReason.MissingLifecycleSupport);
        }

        if (!HasValidUniqueRows(evidence.CompleteRows)
            || !evidence.HasCompletePopulation
            || evidence.CompleteKind is not GlobalAdministratorsSurfaceKind.Ready
            || evidence.CompleteFreshness is not ReadModelFreshnessState.Current
            || evidence.CompleteLifecycle is not ProjectionLifecycleState.Current
            || string.IsNullOrWhiteSpace(evidence.CompleteProjectionVersion)
            || !string.Equals(
                evidence.VisibleProjectionVersion,
                evidence.CompleteProjectionVersion,
                StringComparison.Ordinal))
        {
            return Blocked(GlobalAdministratorActionKind.Remove, GlobalAdministratorActionUnavailableReason.IncompletePopulation);
        }

        if (string.IsNullOrWhiteSpace(targetUserId)
            || !HasValidUniqueRows(evidence.VisibleRows)
            || !evidence.VisibleRows.Any(row => string.Equals(row.UserId, targetUserId, StringComparison.Ordinal))
            || !evidence.CompleteRows.Any(row => string.Equals(row.UserId, targetUserId, StringComparison.Ordinal)))
        {
            return Blocked(GlobalAdministratorActionKind.Remove, GlobalAdministratorActionUnavailableReason.TargetMissing);
        }

        if (evidence.CompleteRows.Count <= 1)
        {
            return Blocked(GlobalAdministratorActionKind.Remove, GlobalAdministratorActionUnavailableReason.LastAdministrator);
        }

        if (!evidence.IsRemovePreviewReady)
        {
            return Blocked(GlobalAdministratorActionKind.Remove, GlobalAdministratorActionUnavailableReason.MissingConsequencePreview);
        }

        return Available(GlobalAdministratorActionKind.Remove);
    }

    private static GlobalAdministratorActionAvailability? EvaluateCommon(
        GlobalAdministratorActionEvidence evidence,
        GlobalAdministratorActionKind action)
    {
        if (!evidence.IsAuthorized)
        {
            return Blocked(action, GlobalAdministratorActionUnavailableReason.MissingPermission);
        }

        if (!IsQualifiedVisibleRead(evidence))
        {
            return Blocked(action, GlobalAdministratorActionUnavailableReason.StaleData);
        }

        if (!evidence.HasViewportMeasurement
            || evidence.Viewport is not TenantHighImpactViewportState.Safe)
        {
            return Blocked(action, GlobalAdministratorActionUnavailableReason.UnsafeViewport);
        }

        if (!evidence.SupportsDispatch || !evidence.SupportsStatus || !evidence.SupportsRequery)
        {
            return Blocked(action, GlobalAdministratorActionUnavailableReason.MissingLifecycleSupport);
        }

        return !evidence.IsAdmissionAvailable
            ? Blocked(action, GlobalAdministratorActionUnavailableReason.AggregateBusy)
            : null;
    }

    private static bool IsQualifiedVisibleRead(GlobalAdministratorActionEvidence evidence)
    {
        if (evidence.VisibleFreshness is not ReadModelFreshnessState.Current
            || evidence.VisibleLifecycle is not ProjectionLifecycleState.Current
            || string.IsNullOrWhiteSpace(evidence.VisibleProjectionVersion)
            || evidence.VisibleRows is null
            || !HasValidUniqueRows(evidence.VisibleRows))
        {
            return false;
        }

        return evidence.VisibleKind switch
        {
            GlobalAdministratorsSurfaceKind.Ready
                => evidence.VisibleRows.Count > 0 && !evidence.VisibleIsAuthorizationScopedEmpty,
            GlobalAdministratorsSurfaceKind.Empty
                => evidence.VisibleRows.Count == 0 && evidence.VisibleIsAuthorizationScopedEmpty,
            _ => false,
        };
    }

    private static bool HasValidUniqueRows(IReadOnlyList<GlobalAdministratorRow>? rows)
    {
        if (rows is null)
        {
            return false;
        }

        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (GlobalAdministratorRow? row in rows)
        {
            if (row is null
                || string.IsNullOrWhiteSpace(row.UserId)
                || row.UserId.Length > GlobalAdministratorRemovePreview.MaximumUserIdLength
                || row.UserId.Any(char.IsControl)
                || row.Freshness is not ReadModelFreshnessState.Current
                || row.Lifecycle is not ProjectionLifecycleState.Current
                || !identities.Add(row.UserId))
            {
                return false;
            }
        }

        return true;
    }

    private static GlobalAdministratorActionAvailability Available(GlobalAdministratorActionKind action)
        => new(
            action,
            IsAvailable: true,
            GlobalAdministratorActionUnavailableReason.None,
            $"Tenants.GlobalAdministrators.Availability.{action}.Available",
            "Tenants.GlobalAdministrators.Availability.Recovery.None");

    private static GlobalAdministratorActionAvailability Blocked(
        GlobalAdministratorActionKind action,
        GlobalAdministratorActionUnavailableReason reason)
        => new(
            action,
            IsAvailable: false,
            reason,
            $"Tenants.GlobalAdministrators.Availability.{action}.Unavailable.{reason}",
            reason is GlobalAdministratorActionUnavailableReason.MissingConsequencePreview
                ? $"Tenants.GlobalAdministrators.Availability.{action}.Recovery.{reason}"
                : $"Tenants.GlobalAdministrators.Availability.Recovery.{reason}");
}
