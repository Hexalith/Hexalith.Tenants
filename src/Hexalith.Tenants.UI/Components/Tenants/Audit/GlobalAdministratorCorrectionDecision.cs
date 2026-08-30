namespace Hexalith.Tenants.UI.Components.Tenants.Audit;

/// <summary>Contains one immutable correction-control decision for a single render or I/O boundary.</summary>
/// <param name="IsConfirmDisabled">Whether confirmation is disabled.</param>
/// <param name="CanRefresh">Whether tracked reconciliation may refresh.</param>
/// <param name="Reason">Localized reason associated with disabled controls.</param>
/// <param name="Recovery">Localized recovery associated with disabled controls.</param>
internal sealed record GlobalAdministratorCorrectionDecision(
    bool IsConfirmDisabled,
    bool CanRefresh,
    string? Reason,
    string? Recovery)
{
    /// <summary>Gets the stable reason/recovery association when both strings are usable.</summary>
    internal string? AriaDescribedBy
        => string.IsNullOrWhiteSpace(Reason) || string.IsNullOrWhiteSpace(Recovery)
            ? null
            : "tenants-correction-unavailable tenants-correction-unavailable-recovery";
}
