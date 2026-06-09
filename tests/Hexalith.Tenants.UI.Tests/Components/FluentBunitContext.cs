using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>
/// Base bUnit context for tests that render Fluent UI v5 components. Registers the Fluent UI
/// services (so <see cref="LibraryConfiguration"/> and friends resolve) and runs JS interop in
/// loose mode, because Fluent components import JS modules on first render. The Tenants domain UI
/// uses Fluent v5 components only (no raw interactive HTML controls), so component tests render
/// real <c>&lt;fluent-*&gt;</c> elements and need this setup.
/// </summary>
public abstract class FluentBunitContext : BunitContext
{
    protected FluentBunitContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
    }
}

/// <summary>
/// Test helpers for driving Fluent UI v5 components in bUnit.
/// </summary>
public static class FluentSelectInterop
{
    /// <summary>
    /// Drives a Fluent UI v5 <c>FluentSelect&lt;string, string&gt;</c> the way a user selecting an
    /// option does: by invoking the component's <c>ValueChanged</c> callback. bUnit's <c>.Change()</c>
    /// targets the HTML <c>onchange</c> event, but FluentSelect emits its own <c>ondropdownchange</c>
    /// event whose args type is internal, so invoking the component callback is the stable,
    /// version-independent way to change a select's value. The select is located by its preserved
    /// <c>data-testid</c> attribute.
    /// </summary>
    public static void ChangeFluentSelect<TComponent>(IRenderedComponent<TComponent> cut, string testId, string value)
        where TComponent : class, IComponent
    {
        FluentSelect<string, string> select = cut.FindComponents<FluentSelect<string, string>>()
            .Select(rendered => rendered.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, testId, StringComparison.Ordinal));

        // The stub gateways used by these tests complete synchronously, so invoking the value-changed
        // callback (and the render it queues) settles without yielding; blocking here is therefore safe.
        cut.InvokeAsync(() => select.ValueChanged.InvokeAsync(value)).GetAwaiter().GetResult();
    }
}
