using Hexalith.FrontComposer.Shell.State.Navigation;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Holds circuit-scoped viewport evidence observed from FrontComposer browser measurements.
/// </summary>
public sealed class TenantHighImpactViewportObservation
{
    /// <summary>
    /// Initializes a new instance with unknown evidence until a browser measurement is observed.
    /// </summary>
    /// <param name="initialState">Optional test seed; production registration uses the unknown default.</param>
    public TenantHighImpactViewportObservation(
        TenantHighImpactViewportState initialState = TenantHighImpactViewportState.Unknown)
    {
        State = initialState;
    }

    /// <summary>Raised after the observed safety state changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Gets the latest measured viewport safety state.</summary>
    public TenantHighImpactViewportState State { get; private set; }

    /// <summary>Observes the authoritative tier carried by the FrontComposer action.</summary>
    /// <param name="tier">Browser-measured viewport tier.</param>
    public void Observe(ViewportTier tier)
    {
        TenantHighImpactViewportState next = tier switch
        {
            ViewportTier.Phone => TenantHighImpactViewportState.Unsafe,
            ViewportTier.Tablet or ViewportTier.CompactDesktop or ViewportTier.Desktop
                => TenantHighImpactViewportState.Safe,
            _ => TenantHighImpactViewportState.Unknown,
        };
        if (next == State)
        {
            return;
        }

        State = next;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
