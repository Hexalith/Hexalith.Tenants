using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.Localization;

namespace Hexalith.Tenants.UI.State.TenantCommands;

public static class TenantCommandFlowGuard
{
    /// <summary>Resolves a safe message without exposing a missing resource identifier.</summary>
    /// <param name="localizer">Current UI localization.</param>
    /// <param name="key">Optional localized message key.</param>
    /// <param name="safeMessage">Already safe literal message.</param>
    /// <param name="fallbackKey">Optional flow-specific fallback resource.</param>
    /// <returns>Localized safe copy, the literal message, or no message.</returns>
    public static string? ResolveSafeMessage(IStringLocalizer<TenantsResources> localizer, string? key,
        string? safeMessage, string? fallbackKey = null)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        if (string.IsNullOrWhiteSpace(key))
        {
            return safeMessage;
        }
        LocalizedString localized = localizer[key];
        if (!localized.ResourceNotFound)
        {
            return localized.Value;
        }
        if (fallbackKey is null)
        {
            return null;
        }
        LocalizedString fallback = localizer[fallbackKey];
        return fallback.ResourceNotFound ? null : fallback.Value;
    }

    /// <summary>
    /// Returns whether the parent AggregateIdentity lock must stay raised for the current lifecycle state.
    /// Retention covers submission through projection reconciliation
    /// (<see cref="TenantCommandLifecycleState.RequestSent"/> /
    /// <see cref="TenantCommandLifecycleState.Accepted"/> /
    /// <see cref="TenantCommandLifecycleState.ProjectionPending"/>).
    /// This state-only guard does not release dispatched admission: the retained-attempt tracker
    /// preserves unresolved ownership when an uncertain outcome offers continue-read-only.
    /// </summary>
    /// <param name="state">Current command lifecycle state.</param>
    /// <param name="isSubmitting">Whether a local submit call is still in progress.</param>
    /// <returns><see langword="true"/> when sibling command surfaces for the same aggregate stay locked.</returns>
    public static bool RetainsCommandActivity(TenantCommandLifecycleState state, bool isSubmitting = false)
        => isSubmitting
        || state is TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.ProjectionPending;
}
