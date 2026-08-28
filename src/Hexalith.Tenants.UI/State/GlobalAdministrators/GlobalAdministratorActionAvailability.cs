namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Contains one pure fixed-scope availability result.</summary>
/// <param name="Action">Evaluated fixed-scope action.</param>
/// <param name="IsAvailable">Whether the action may proceed to its next stage.</param>
/// <param name="UnavailableReason">Canonical fail-closed blocker.</param>
/// <param name="ReasonKey">Whole-string localized reason key.</param>
/// <param name="RecoveryKey">Whole-string localized recovery key.</param>
public sealed record GlobalAdministratorActionAvailability(
    GlobalAdministratorActionKind Action,
    bool IsAvailable,
    GlobalAdministratorActionUnavailableReason UnavailableReason,
    string ReasonKey,
    string RecoveryKey)
{
    /// <summary>Returns a support-safe result description.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorActionAvailability)} {{ Action = {Action}, IsAvailable = {IsAvailable}, UnavailableReason = {UnavailableReason} }}";
}
