using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.Tenants.UI.Components.Tenants.Lifecycle;

/// <summary>Restores lifecycle focus when the browser target remains available.</summary>
internal static class TenantLifecycleFocus
{
    /// <summary>Focuses the target while tolerating a disconnected circuit or a detached browser element.</summary>
    /// <param name="element">The existing lifecycle focus target.</param>
    /// <returns>A task representing the focus operation.</returns>
    internal static async Task FocusSafelyAsync(ElementReference element)
    {
        try
        {
            await element.FocusAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
    }
}
