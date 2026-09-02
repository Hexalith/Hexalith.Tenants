using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Contains the BFF-composed, support-safe facts for one fixed-scope removal preview.</summary>
/// <param name="TargetUserId">Literal target identifier supplied by the operator.</param>
/// <param name="ScopeTenantId">Fixed platform tenant scope.</param>
/// <param name="ScopeDomain">Fixed global-administrator domain.</param>
/// <param name="ScopeAggregateId">Fixed global-administrator aggregate identifier.</param>
/// <param name="CurrentAdministratorCount">Administrator count from complete current evidence.</param>
/// <param name="ResultingAdministratorCount">Expected count after one successful removal.</param>
/// <param name="Freshness">Freshness captured with the complete evidence.</param>
/// <param name="Lifecycle">Projection lifecycle captured with the complete evidence.</param>
/// <param name="ProjectionVersion">Opaque projection version used as the pre-dispatch baseline.</param>
/// <param name="IsAuthorized">Whether current authoritative caller evidence permits the preview.</param>
/// <param name="IsCompletePopulation">Whether the bounded walk proved the complete fixed population.</param>
/// <param name="IsTargetPresent">Whether exact ordinal comparison proved the target present.</param>
/// <param name="IsSelfRemoval">Whether the authoritative caller subject exactly equals the target.</param>
/// <param name="ScopeFactKey">Whole-string fixed-scope fact composed by the BFF.</param>
/// <param name="AuthorityChangeFactKey">Whole-string authority-change fact composed by the BFF.</param>
/// <param name="FreshnessFactKey">Whole-string evidence-freshness fact composed by the BFF.</param>
/// <param name="RecoveryFactKey">Whole-string recovery fact composed by the BFF.</param>
/// <param name="AuditFactKey">Whole-string audit-expectation fact composed by the BFF.</param>
/// <param name="CallerTargetContextFactKey">Whole-string caller/target context fact composed by the BFF.</param>
/// <param name="KnownConsequencesFactKey">Whole-string known-consequences fact composed by the BFF.</param>
/// <param name="KnownUnknownsFactKey">Whole-string known-unknowns fact composed by the BFF.</param>
/// <param name="UnavailableReasonKey">Whole-string localized reason when the preview is incomplete.</param>
/// <param name="RecoveryKey">Whole-string localized recovery when the preview is incomplete.</param>
public sealed record GlobalAdministratorRemovePreview(
    string TargetUserId,
    string ScopeTenantId,
    string ScopeDomain,
    string ScopeAggregateId,
    int CurrentAdministratorCount,
    int ResultingAdministratorCount,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle,
    string? ProjectionVersion,
    bool IsAuthorized,
    bool IsCompletePopulation,
    bool IsTargetPresent,
    bool IsSelfRemoval,
    string? ScopeFactKey,
    string? AuthorityChangeFactKey,
    string? FreshnessFactKey,
    string? RecoveryFactKey,
    string? AuditFactKey,
    string? CallerTargetContextFactKey,
    string? KnownConsequencesFactKey,
    string? KnownUnknownsFactKey,
    string? UnavailableReasonKey,
    string? RecoveryKey)
{
    /// <summary>Gets the fixed tenant scope.</summary>
    public const string FixedTenantId = "system";

    /// <summary>Gets the fixed command domain.</summary>
    public const string FixedDomain = "global-administrators";

    /// <summary>Gets the fixed aggregate identifier.</summary>
    public const string FixedAggregateId = "global-administrators";

    /// <summary>Gets the maximum supported literal user identifier length.</summary>
    public const int MaximumUserIdLength = 256;

    /// <summary>Gets whether every required preview fact is present and internally consistent.</summary>
    public bool IsComplete
        => IsAuthorized
            && IsCompletePopulation
            && IsTargetPresent
            && IsSupportedIdentity(TargetUserId)
            && string.Equals(ScopeTenantId, FixedTenantId, StringComparison.Ordinal)
            && string.Equals(ScopeDomain, FixedDomain, StringComparison.Ordinal)
            && string.Equals(ScopeAggregateId, FixedAggregateId, StringComparison.Ordinal)
            && CurrentAdministratorCount > 1
            && ResultingAdministratorCount == CurrentAdministratorCount - 1
            && Freshness is ReadModelFreshnessState.Current
            && Lifecycle is ProjectionLifecycleState.Current
            && !string.IsNullOrWhiteSpace(ProjectionVersion)
            && HasAllSafeFacts
            && string.IsNullOrWhiteSpace(UnavailableReasonKey)
            && string.IsNullOrWhiteSpace(RecoveryKey);

    /// <summary>Gets whether every BFF-owned whole-string consequence fact is present.</summary>
    public bool HasAllSafeFacts
        => !string.IsNullOrWhiteSpace(ScopeFactKey)
            && !string.IsNullOrWhiteSpace(AuthorityChangeFactKey)
            && !string.IsNullOrWhiteSpace(FreshnessFactKey)
            && !string.IsNullOrWhiteSpace(RecoveryFactKey)
            && !string.IsNullOrWhiteSpace(AuditFactKey)
            && !string.IsNullOrWhiteSpace(CallerTargetContextFactKey)
            && !string.IsNullOrWhiteSpace(KnownConsequencesFactKey)
            && !string.IsNullOrWhiteSpace(KnownUnknownsFactKey);

    /// <summary>Creates a fail-closed preview without disclosing administrator rows.</summary>
    /// <param name="targetUserId">Literal target identifier.</param>
    /// <param name="reasonKey">Whole-string localized failure reason.</param>
    /// <param name="recoveryKey">Whole-string localized recovery.</param>
    /// <returns>An incomplete support-safe preview.</returns>
    public static GlobalAdministratorRemovePreview Unavailable(
        string targetUserId,
        string reasonKey = "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Evidence",
        string recoveryKey = "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh")
        => new(
            targetUserId,
            FixedTenantId,
            FixedDomain,
            FixedAggregateId,
            0,
            0,
            ReadModelFreshnessState.Unknown,
            ProjectionLifecycleState.Unknown,
            null,
            IsAuthorized: false,
            IsCompletePopulation: false,
            IsTargetPresent: false,
            IsSelfRemoval: false,
            ScopeFactKey: null,
            AuthorityChangeFactKey: null,
            FreshnessFactKey: null,
            RecoveryFactKey: null,
            AuditFactKey: null,
            CallerTargetContextFactKey: null,
            KnownConsequencesFactKey: null,
            KnownUnknownsFactKey: null,
            reasonKey,
            recoveryKey);

    /// <summary>Composes preview facts after caller authority has been resolved.</summary>
    /// <param name="targetUserId">Literal target identifier.</param>
    /// <param name="callerSubject">Literal authoritative caller subject.</param>
    /// <param name="snapshot">Complete fixed-scope projection evidence.</param>
    /// <param name="isAuthorized">Whether current authoritative caller evidence permits the preview.</param>
    /// <returns>A complete preview, or a fail-closed preview with localized recovery keys.</returns>
    public static GlobalAdministratorRemovePreview Create(
        string targetUserId,
        string? callerSubject,
        GlobalAdministratorsSnapshot snapshot,
        bool isAuthorized)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!isAuthorized || !IsSupportedIdentity(callerSubject))
        {
            return Unavailable(
                targetUserId,
                "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Authorization",
                "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Authorization");
        }

        if (!IsSupportedIdentity(targetUserId))
        {
            return Unavailable(
                targetUserId,
                "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Target",
                "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Target");
        }

        if (!HasQualifiedCompleteEvidence(snapshot))
        {
            return Unavailable(
                targetUserId,
                "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Evidence",
                "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh") with
            {
                Freshness = snapshot.Freshness,
                Lifecycle = snapshot.Lifecycle,
                ProjectionVersion = snapshot.ProjectionVersion,
                IsAuthorized = true,
            };
        }

        bool targetPresent = snapshot.Rows.Any(row =>
            string.Equals(row.UserId, targetUserId, StringComparison.Ordinal));
        if (!targetPresent)
        {
            return Unavailable(
                targetUserId,
                "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.TargetMissing",
                "Tenants.GlobalAdministrators.Remove.Preview.Recovery.TargetMissing") with
            {
                CurrentAdministratorCount = snapshot.Rows.Count,
                ResultingAdministratorCount = snapshot.Rows.Count,
                Freshness = snapshot.Freshness,
                Lifecycle = snapshot.Lifecycle,
                ProjectionVersion = snapshot.ProjectionVersion,
                IsAuthorized = true,
                IsCompletePopulation = true,
            };
        }

        if (snapshot.Rows.Count <= 1)
        {
            return Unavailable(
                targetUserId,
                "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.LastAdministrator",
                "Tenants.GlobalAdministrators.Remove.Preview.Recovery.LastAdministrator") with
            {
                CurrentAdministratorCount = snapshot.Rows.Count,
                ResultingAdministratorCount = snapshot.Rows.Count,
                Freshness = snapshot.Freshness,
                Lifecycle = snapshot.Lifecycle,
                ProjectionVersion = snapshot.ProjectionVersion,
                IsAuthorized = true,
                IsCompletePopulation = true,
                IsTargetPresent = true,
            };
        }

        bool selfRemoval = string.Equals(callerSubject, targetUserId, StringComparison.Ordinal);
        return new(
            targetUserId,
            FixedTenantId,
            FixedDomain,
            FixedAggregateId,
            snapshot.Rows.Count,
            snapshot.Rows.Count - 1,
            snapshot.Freshness,
            snapshot.Lifecycle,
            snapshot.ProjectionVersion,
            IsAuthorized: true,
            IsCompletePopulation: true,
            IsTargetPresent: true,
            selfRemoval,
            ScopeFactKey: "Tenants.GlobalAdministrators.Remove.Preview.Scope.Value",
            AuthorityChangeFactKey: "Tenants.GlobalAdministrators.Remove.Preview.AuthorityChange.Value",
            FreshnessFactKey: "Tenants.GlobalAdministrators.Remove.Preview.Freshness.Value",
            RecoveryFactKey: "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Value",
            AuditFactKey: "Tenants.GlobalAdministrators.Remove.Preview.Audit.Value",
            CallerTargetContextFactKey: selfRemoval
                ? "Tenants.GlobalAdministrators.Remove.Preview.CallerTargetContext.Self.Value"
                : "Tenants.GlobalAdministrators.Remove.Preview.CallerTargetContext.Other.Value",
            KnownConsequencesFactKey: "Tenants.GlobalAdministrators.Remove.Preview.KnownConsequences.Value",
            KnownUnknownsFactKey: "Tenants.GlobalAdministrators.Remove.Preview.KnownUnknowns.Value",
            UnavailableReasonKey: null,
            RecoveryKey: null);
    }

    /// <summary>Returns whether a rebuilt preview still describes the exact same dispatch basis.</summary>
    /// <param name="current">Freshly rebuilt preview.</param>
    /// <returns><see langword="true"/> when all dispatch-governing facts match.</returns>
    public bool Matches(GlobalAdministratorRemovePreview? current)
        => IsComplete
            && current?.IsComplete == true
            && string.Equals(TargetUserId, current.TargetUserId, StringComparison.Ordinal)
            && string.Equals(ScopeTenantId, current.ScopeTenantId, StringComparison.Ordinal)
            && string.Equals(ScopeDomain, current.ScopeDomain, StringComparison.Ordinal)
            && string.Equals(ScopeAggregateId, current.ScopeAggregateId, StringComparison.Ordinal)
            && CurrentAdministratorCount == current.CurrentAdministratorCount
            && ResultingAdministratorCount == current.ResultingAdministratorCount
            && string.Equals(ProjectionVersion, current.ProjectionVersion, StringComparison.Ordinal)
            && IsSelfRemoval == current.IsSelfRemoval
            && string.Equals(ScopeFactKey, current.ScopeFactKey, StringComparison.Ordinal)
            && string.Equals(AuthorityChangeFactKey, current.AuthorityChangeFactKey, StringComparison.Ordinal)
            && string.Equals(FreshnessFactKey, current.FreshnessFactKey, StringComparison.Ordinal)
            && string.Equals(RecoveryFactKey, current.RecoveryFactKey, StringComparison.Ordinal)
            && string.Equals(AuditFactKey, current.AuditFactKey, StringComparison.Ordinal)
            && string.Equals(CallerTargetContextFactKey, current.CallerTargetContextFactKey, StringComparison.Ordinal)
            && string.Equals(KnownConsequencesFactKey, current.KnownConsequencesFactKey, StringComparison.Ordinal)
            && string.Equals(KnownUnknownsFactKey, current.KnownUnknownsFactKey, StringComparison.Ordinal);

    /// <summary>Returns whether current complete projection evidence still matches this preview.</summary>
    /// <param name="current">Current fixed-scope projection evidence.</param>
    /// <returns><see langword="true"/> only when identity, count, freshness, and version remain exact.</returns>
    public bool Matches(GlobalAdministratorsSnapshot? current)
        => IsComplete
            && current is not null
            && HasQualifiedCompleteEvidence(current)
            && string.Equals(ProjectionVersion, current.ProjectionVersion, StringComparison.Ordinal)
            && CurrentAdministratorCount == current.Rows.Count
            && current.Rows.Any(row => string.Equals(row.UserId, TargetUserId, StringComparison.Ordinal));

    /// <summary>Returns whether the literal identity satisfies the removal boundary.</summary>
    /// <param name="userId">Literal identity.</param>
    /// <returns><see langword="true"/> when supported without normalization.</returns>
    public static bool IsSupportedIdentity(string? userId)
        => !string.IsNullOrWhiteSpace(userId)
            && userId.Length <= MaximumUserIdLength
            && !userId.Any(char.IsControl);

    /// <summary>Returns a support-safe description that omits identities and projection metadata.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorRemovePreview)} {{ IsComplete = {IsComplete}, IsAuthorized = {IsAuthorized}, IsCompletePopulation = {IsCompletePopulation}, IsTargetPresent = {IsTargetPresent}, IsSelfRemoval = {IsSelfRemoval}, Freshness = {Freshness}, Lifecycle = {Lifecycle} }}";

    private static bool HasQualifiedCompleteEvidence(GlobalAdministratorsSnapshot snapshot)
    {
        if (!snapshot.IsCompleteEvidence
            || snapshot.Kind is not GlobalAdministratorsSurfaceKind.Ready
            || snapshot.Freshness is not ReadModelFreshnessState.Current
            || snapshot.Lifecycle is not ProjectionLifecycleState.Current
            || string.IsNullOrWhiteSpace(snapshot.ProjectionVersion)
            || snapshot.IsAuthorizationScopedEmpty)
        {
            return false;
        }

        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (GlobalAdministratorRow? row in snapshot.Rows)
        {
            if (row is null
                || !IsSupportedIdentity(row.UserId)
                || row.Freshness is not ReadModelFreshnessState.Current
                || row.Lifecycle is not ProjectionLifecycleState.Current
                || !identities.Add(row.UserId))
            {
                return false;
            }
        }

        return snapshot.Rows.Count > 0;
    }
}
