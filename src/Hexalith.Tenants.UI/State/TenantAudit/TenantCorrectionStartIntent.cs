using System.Globalization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.SupportSafety;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Safe non-submitting intent handed to the separate consequence preview.</summary>
public sealed record TenantCorrectionStartIntent(
    string OriginalAuditReference,
    string TenantScope,
    string TargetUserId,
    string OutcomeType,
    string CurrentProjectionSnapshotReference,
    TenantCorrectionCommandDomain? IntendedCommandDomain,
    TenantCorrectionCommandType? IntendedCommandType,
    TenantRole? IntendedRole,
    IReadOnlyList<TenantCorrectionUnavailableReason> UnavailableReasons,
    IReadOnlyDictionary<string, string> RequiredPreviewInputs,
    TenantCorrectionProjection? CurrentProjection = null)
{
    /// <summary>Gets whether all start gates allow the separate preview handoff.</summary>
    public bool IsAvailable => UnavailableReasons.Count == 0
        && IntendedCommandDomain is not null && IntendedCommandType is not null;

    /// <summary>Gets whether the selected command restores access.</summary>
    public bool IsRestoreAccessAction => IntendedCommandType is TenantCorrectionCommandType.AddUserToTenant
        or TenantCorrectionCommandType.SetGlobalAdministrator;

    /// <summary>Evaluates complete receipt, current projection, authority, selection and viewport facts.</summary>
    public static TenantCorrectionStartIntent Evaluate(TenantCorrectionStartContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Receipt);
        ArgumentNullException.ThrowIfNull(context.Row);
        TenantAuditRow row = context.Row;
        List<TenantCorrectionUnavailableReason> reasons = [];
        Dictionary<string, string> inputs = new(StringComparer.Ordinal);
        TenantAuditReceipt verifiedReceipt = TenantAuditReceipt.FromRow(row);
        if (context.Receipt.State is not TenantAuditReceiptState.Ready
            || verifiedReceipt.State is not TenantAuditReceiptState.Ready
            || context.Receipt != verifiedReceipt)
        {
            reasons.Add(TenantCorrectionUnavailableReason.AuditEvidenceUnavailable);
        }

        string reference = TenantAuditSupportSafety.SafeApprovedReference(context.Receipt.AuditReference) ?? string.Empty;
        string target = TenantAuditSupportSafety.SafeIdentifier(row.Narrative?.UserId, SupportSafeCopyValueKind.UserId);
        string scope = TenantAuditSupportSafety.SafeIdentifier(row.TenantId, SupportSafeCopyValueKind.TenantId);
        if (reference.Length == 0 || target.Length == 0 || scope.Length == 0
            || !string.Equals(target, row.Target, StringComparison.Ordinal))
        {
            reasons.Add(TenantCorrectionUnavailableReason.AuditEvidenceUnavailable);
        }

        inputs["originalAuditReference"] = reference;
        if (verifiedReceipt.Timestamp is { } timestamp)
        {
            inputs["originalTimestamp"] = timestamp.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!context.IsAuthorized)
        {
            reasons.Add(TenantCorrectionUnavailableReason.AuthorizationIndeterminate);
        }
        if (row.Freshness is not ReadModelFreshnessState.Current
            || !ProjectionLifecyclePolicy.IsProjectionConfirmed(row.Provenance, row.Lifecycle))
        {
            reasons.Add(TenantCorrectionUnavailableReason.FreshnessIndeterminate);
        }
        if (!context.IsNarrowViewportSafe)
        {
            reasons.Add(TenantCorrectionUnavailableReason.NarrowViewportUnavailable);
        }

        TenantCorrectionCommandDomain? domain = null;
        TenantCorrectionCommandType? command = null;
        string projectionReference = string.Empty;
        switch (row.EventType)
        {
            case "UserRemovedFromTenant":
            case "UserRoleChanged":
                domain = TenantCorrectionCommandDomain.Tenants;
                TenantCorrectionProjection? projection = context.Projection;
                bool matchingProjection = projection is not null
                    && string.Equals(projection.TenantId, scope, StringComparison.Ordinal)
                    && string.Equals(projection.TargetUserId, target, StringComparison.Ordinal)
                    && string.Equals(row.Scope, scope, StringComparison.Ordinal);
                bool verifiedCurrentEvidence = matchingProjection && projection!.IsCurrent && projection.IsAuthorized
                    && context.HasCurrentProjectionSnapshot;
                if (!string.Equals(row.Scope, scope, StringComparison.Ordinal)
                    || projection is not null && (!string.Equals(projection.TenantId, scope, StringComparison.Ordinal)
                        || !string.Equals(projection.TargetUserId, target, StringComparison.Ordinal)))
                {
                    reasons.Add(TenantCorrectionUnavailableReason.ScopeConflict);
                }
                if (projection?.IsCurrent is not true || !context.HasCurrentProjectionSnapshot)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.CurrentProjectionUnavailable);
                }
                if (projection?.IsAuthorized is not true)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.AuthorizationIndeterminate);
                }
                if (!context.HasTenantCommandSupport)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.CommandSupportUnavailable);
                }
                TenantStatus status = projection?.TenantStatus ?? context.TenantStatus;
                if (status is TenantStatus.Disabled)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.TenantDisabled);
                }
                else if (status is not TenantStatus.Active)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.TenantLifecycleUnknown);
                }
                bool validRole = context.IntendedRole is TenantRole.TenantOwner
                    or TenantRole.TenantContributor or TenantRole.TenantReader;
                if (!validRole)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.ExplicitRoleRequired);
                }
                TenantRole? currentRole = verifiedCurrentEvidence ? projection!.CurrentRole : null;
                if (row.EventType is "UserRoleChanged" && verifiedCurrentEvidence && currentRole is null)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.CurrentStateIndeterminate);
                }
                if (projection?.HasVerifiedMembership is not true
                    || currentRole is not null && currentRole is not (TenantRole.TenantOwner
                        or TenantRole.TenantContributor or TenantRole.TenantReader))
                {
                    reasons.Add(TenantCorrectionUnavailableReason.CurrentStateIndeterminate);
                }
                if (verifiedCurrentEvidence && projection!.IsMembershipEmpty)
                {
                    if (context.IntendedRole is not TenantRole.TenantOwner)
                    {
                        reasons.Add(TenantCorrectionUnavailableReason.EmptyMembershipRequiresOwner);
                    }
                    if (!projection.IsGlobalAdministrator)
                    {
                        reasons.Add(TenantCorrectionUnavailableReason.AuthorizationIndeterminate);
                    }
                    inputs["emptyMembership"] = "true";
                }
                command = !verifiedCurrentEvidence ? null
                    : currentRole is null ? TenantCorrectionCommandType.AddUserToTenant : TenantCorrectionCommandType.ChangeUserRole;
                if (verifiedCurrentEvidence && validRole && currentRole == context.IntendedRole)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.AlreadyApplied);
                    command = null;
                }
                inputs["tenantId"] = scope;
                inputs["userId"] = target;
                if (validRole)
                {
                    inputs["intendedRole"] = context.IntendedRole!.Value.ToString();
                }
                if (currentRole is not null)
                {
                    inputs["currentRole"] = currentRole.Value.ToString();
                }
                projectionReference = verifiedCurrentEvidence ? "tenant-projection-current" : string.Empty;
                inputs["currentProjectionSnapshot"] = projectionReference;
                break;
            case "GlobalAdministratorRemoved":
            case "GlobalAdministratorSet":
                // Compatibility for Story 5.7. The tenant page always passes support=false.
                domain = TenantCorrectionCommandDomain.GlobalAdministrators;
                command = row.EventType is "GlobalAdministratorRemoved"
                    ? TenantCorrectionCommandType.SetGlobalAdministrator : TenantCorrectionCommandType.RemoveGlobalAdministrator;
                scope = "global-administrators";
                if (!context.HasGlobalAdministratorCommandSupport)
                {
                    reasons.Add(TenantCorrectionUnavailableReason.GlobalAdministratorCommandSupportUnavailable);
                }
                if (!context.HasCurrentProjectionSnapshot || string.IsNullOrWhiteSpace(context.CurrentProjectionSnapshotReference))
                {
                    reasons.Add(TenantCorrectionUnavailableReason.CurrentProjectionUnavailable);
                }
                projectionReference = context.CurrentProjectionSnapshotReference;
                inputs["currentProjectionSnapshot"] = projectionReference;
                inputs["tenantId"] = "system";
                inputs["domain"] = "global-administrators";
                inputs["aggregateId"] = "global-administrators";
                inputs["userId"] = target;
                break;
            default:
                reasons.Add(TenantCorrectionUnavailableReason.UnsupportedOutcome);
                break;
        }

        // The BFF redacts an unavailable authority capture. Do not turn that redaction into a
        // stack of guesses about lifecycle and membership on the operator surface; blockers that
        // do not come from the capture stay visible.
        IReadOnlyList<TenantCorrectionUnavailableReason> safeReasons = IsHiddenRead(context.Projection)
            && !context.IsAuthorized && domain is TenantCorrectionCommandDomain.Tenants
                ? WithoutHiddenReadGuesses(reasons)
                : reasons.Distinct().ToArray();
        return new(reference, scope, target, row.EventType, projectionReference, domain, command,
            context.IntendedRole, safeReasons, inputs,
            domain is TenantCorrectionCommandDomain.Tenants ? context.Projection : null);
    }

    /// <summary>Gets whether the BFF withheld current access, membership and lifecycle from a capture.</summary>
    /// <param name="projection">The redacted capture, if any.</param>
    /// <returns><see langword="true"/> when the capture discloses neither authority nor membership.</returns>
    internal static bool IsHiddenRead(TenantCorrectionProjection? projection)
        => projection is { IsAuthorized: false, HasVerifiedMembership: false };

    /// <summary>Removes the projection, lifecycle and membership guesses a hidden read produces.</summary>
    /// <param name="reasons">The evaluated reasons, including the access reason.</param>
    /// <returns>The distinct reasons an operator can act on.</returns>
    internal static IReadOnlyList<TenantCorrectionUnavailableReason> WithoutHiddenReadGuesses(
        IEnumerable<TenantCorrectionUnavailableReason> reasons)
        => reasons.Where(static reason => reason is not (TenantCorrectionUnavailableReason.CurrentProjectionUnavailable
                or TenantCorrectionUnavailableReason.TenantLifecycleUnknown
                or TenantCorrectionUnavailableReason.CurrentStateIndeterminate))
            .Distinct()
            .ToArray();

    /// <summary>Creates an unarmed receipt intent until current authority and projection are captured.</summary>
    public static TenantCorrectionStartIntent FromReceipt(TenantAuditReceipt receipt, TenantAuditRow row)
        => Evaluate(new(receipt, row, IsAuthorized: false, HasCurrentProjectionSnapshot: false,
            CurrentProjectionSnapshotReference: string.Empty, HasTenantCommandSupport: false));
}
