using Fluxor;

using Hexalith.FrontComposer.Shell.State.Navigation;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Observes FrontComposer's runtime viewport actions without persisting or redispatching them.
/// </summary>
/// <param name="observation">Circuit-scoped high-impact viewport observation.</param>
public sealed class TenantHighImpactViewportEffects(TenantHighImpactViewportObservation observation)
{
    /// <summary>Records one authoritative browser measurement.</summary>
    /// <param name="action">FrontComposer viewport action.</param>
    /// <param name="dispatcher">Fluxor dispatcher supplied to effect methods; this observer does not redispatch.</param>
    /// <returns>A completed task.</returns>
    [EffectMethod]
    public Task HandleViewportTierChanged(ViewportTierChangedAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        observation.Observe(action.NewTier);
        return Task.CompletedTask;
    }
}
