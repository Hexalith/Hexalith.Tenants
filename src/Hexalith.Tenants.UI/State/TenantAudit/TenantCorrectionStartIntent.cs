using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public enum TenantCorrectionCommandDomain
{
    Tenants,
    GlobalAdministrators,
}

public enum TenantCorrectionCommandType
{
    AddUserToTenant,
    ChangeUserRole,
    SetGlobalAdministrator,
    RemoveGlobalAdministrator,
}

public enum TenantCorrectionUnavailableReason
{
    AuthorizationIndeterminate,
    FreshnessIndeterminate,
    CurrentProjectionUnavailable,
    AuditEvidenceUnavailable,
    CommandSupportUnavailable,
    ExplicitRoleRequired,
    UnsupportedOutcome,
    GlobalAdministratorCommandSupportUnavailable,
    AlreadyApplied,
    TenantDisabled,
    TenantLifecycleUnknown,
    CurrentStateIndeterminate,
    NarrowViewportUnavailable,
}

public sealed record TenantCorrectionStartContext(
    TenantAuditReceipt Receipt,
    TenantAuditRow Row,
    bool IsAuthorized,
    bool HasCurrentProjectionSnapshot,
    string CurrentProjectionSnapshotReference,
    TenantStatus TenantStatus = TenantStatus.Active,
    TenantRole? CurrentRole = null,
    TenantRole? IntendedRole = null,
    bool HasTenantCommandSupport = true,
    bool HasGlobalAdministratorCommandSupport = false,
    bool IsNarrowViewportSafe = true);

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
    IReadOnlyDictionary<string, string> RequiredPreviewInputs)
{
    public bool IsAvailable
        => UnavailableReasons.Count == 0 && IntendedCommandDomain is not null && IntendedCommandType is not null;

    public bool IsRestoreAccessAction
        => IntendedCommandType is TenantCorrectionCommandType.AddUserToTenant
            or TenantCorrectionCommandType.SetGlobalAdministrator;

    public static TenantCorrectionStartIntent Evaluate(TenantCorrectionStartContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Receipt);
        ArgumentNullException.ThrowIfNull(context.Row);

        List<TenantCorrectionUnavailableReason> reasons = [];
        Dictionary<string, string> previewInputs = new(StringComparer.Ordinal)
        {
            ["originalAuditReference"] = context.Receipt.AuditReference,
            ["currentProjectionSnapshot"] = context.CurrentProjectionSnapshotReference,
        };

        if (!context.IsAuthorized)
        {
            reasons.Add(TenantCorrectionUnavailableReason.AuthorizationIndeterminate);
        }

        if (context.Receipt.State is not TenantAuditReceiptState.Ready)
        {
            reasons.Add(TenantCorrectionUnavailableReason.AuditEvidenceUnavailable);
        }

        if (context.Row.Freshness is not TenantFreshnessState.Current)
        {
            reasons.Add(TenantCorrectionUnavailableReason.FreshnessIndeterminate);
        }

        if (!context.HasCurrentProjectionSnapshot || string.IsNullOrWhiteSpace(context.CurrentProjectionSnapshotReference))
        {
            reasons.Add(TenantCorrectionUnavailableReason.CurrentProjectionUnavailable);
        }

        if (!context.IsNarrowViewportSafe)
        {
            reasons.Add(TenantCorrectionUnavailableReason.NarrowViewportUnavailable);
        }

        TenantCorrectionCommandDomain? domain = null;
        TenantCorrectionCommandType? commandType = null;
        string targetUserId = ReferenceValue(context.Row, "userId") ?? context.Row.Target;
        string tenantScope = context.Row.Scope;

        switch (context.Row.EventType)
        {
            case "UserRemovedFromTenant":
                domain = TenantCorrectionCommandDomain.Tenants;
                commandType = TenantCorrectionCommandType.AddUserToTenant;
                AddTenantMemberRequirements(context, reasons, previewInputs, targetUserId);
                break;
            case "UserRoleChanged":
                domain = TenantCorrectionCommandDomain.Tenants;
                commandType = TenantCorrectionCommandType.ChangeUserRole;
                AddChangeRoleRequirements(context, reasons, previewInputs, targetUserId);
                break;
            case "GlobalAdministratorRemoved":
                domain = TenantCorrectionCommandDomain.GlobalAdministrators;
                commandType = TenantCorrectionCommandType.SetGlobalAdministrator;
                tenantScope = "global-administrators";
                AddGlobalAdministratorRequirements(context, reasons, previewInputs, targetUserId);
                break;
            case "GlobalAdministratorSet":
                domain = TenantCorrectionCommandDomain.GlobalAdministrators;
                commandType = TenantCorrectionCommandType.RemoveGlobalAdministrator;
                tenantScope = "global-administrators";
                AddGlobalAdministratorRequirements(context, reasons, previewInputs, targetUserId);
                break;
            default:
                reasons.Add(TenantCorrectionUnavailableReason.UnsupportedOutcome);
                break;
        }

        return new(
            context.Receipt.AuditReference,
            tenantScope,
            targetUserId,
            context.Row.EventType,
            context.CurrentProjectionSnapshotReference,
            domain,
            commandType,
            context.IntendedRole,
            reasons.Distinct().ToArray(),
            previewInputs);
    }

    public static TenantCorrectionStartIntent FromReceipt(TenantAuditReceipt receipt, TenantAuditRow row)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(row);

        return Evaluate(new(
            receipt,
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: receipt.ProjectionMarker is TenantFreshnessState.Current,
            CurrentProjectionSnapshotReference: string.IsNullOrWhiteSpace(receipt.Scope) ? "current projection" : $"{receipt.Scope}@{receipt.ProjectionMarker}",
            TenantStatus: TenantStatus.Active,
            HasTenantCommandSupport: true,
            HasGlobalAdministratorCommandSupport: false));
    }

    private static void AddTenantMemberRequirements(
        TenantCorrectionStartContext context,
        ICollection<TenantCorrectionUnavailableReason> reasons,
        IDictionary<string, string> previewInputs,
        string targetUserId)
    {
        previewInputs["tenantId"] = context.Row.TenantId;
        previewInputs["userId"] = targetUserId;

        // Fail closed when the audit evidence does not yield the identifiers a tenant-domain
        // correction command requires; an empty tenant or user id must never be treated as a
        // startable correction (AC4). The original evidence stays visible via the unavailable reason.
        if (string.IsNullOrWhiteSpace(context.Row.TenantId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            reasons.Add(TenantCorrectionUnavailableReason.AuditEvidenceUnavailable);
        }

        AddTenantLifecycleReason(context, reasons);

        if (!context.HasTenantCommandSupport)
        {
            reasons.Add(TenantCorrectionUnavailableReason.CommandSupportUnavailable);
        }

        if (context.IntendedRole is null or TenantRole.Unknown)
        {
            reasons.Add(TenantCorrectionUnavailableReason.ExplicitRoleRequired);
            return;
        }

        previewInputs["intendedRole"] = context.IntendedRole.Value.ToString();
    }

    private static void AddChangeRoleRequirements(
        TenantCorrectionStartContext context,
        ICollection<TenantCorrectionUnavailableReason> reasons,
        IDictionary<string, string> previewInputs,
        string targetUserId)
    {
        AddTenantMemberRequirements(context, reasons, previewInputs, targetUserId);

        if (context.CurrentRole is null or TenantRole.Unknown)
        {
            reasons.Add(TenantCorrectionUnavailableReason.CurrentStateIndeterminate);
            return;
        }

        previewInputs["currentRole"] = context.CurrentRole.Value.ToString();

        if (context.IntendedRole == context.CurrentRole)
        {
            reasons.Add(TenantCorrectionUnavailableReason.AlreadyApplied);
        }
    }

    private static void AddGlobalAdministratorRequirements(
        TenantCorrectionStartContext context,
        ICollection<TenantCorrectionUnavailableReason> reasons,
        IDictionary<string, string> previewInputs,
        string targetUserId)
    {
        previewInputs["tenantId"] = "system";
        previewInputs["domain"] = "global-administrators";
        previewInputs["aggregateId"] = "global-administrators";
        previewInputs["userId"] = targetUserId;

        if (!context.HasGlobalAdministratorCommandSupport)
        {
            reasons.Add(TenantCorrectionUnavailableReason.GlobalAdministratorCommandSupportUnavailable);
        }
    }

    private static void AddTenantLifecycleReason(
        TenantCorrectionStartContext context,
        ICollection<TenantCorrectionUnavailableReason> reasons)
    {
        if (context.TenantStatus is TenantStatus.Disabled)
        {
            reasons.Add(TenantCorrectionUnavailableReason.TenantDisabled);
        }
        else if (context.TenantStatus is TenantStatus.Unknown)
        {
            reasons.Add(TenantCorrectionUnavailableReason.TenantLifecycleUnknown);
        }
    }

    private static string? ReferenceValue(TenantAuditRow row, string key)
    {
        string marker = key + ": ";
        string? segment = row.ReferenceContext
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith(marker, StringComparison.Ordinal));

        return segment?.Length > marker.Length ? segment[marker.Length..] : null;
    }
}
