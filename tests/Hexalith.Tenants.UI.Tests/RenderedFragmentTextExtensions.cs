using Bunit;

using Microsoft.AspNetCore.Components;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>
/// Test helpers for asserting against the user-visible text of a rendered component.
/// </summary>
internal static class RenderedFragmentTextExtensions
{
    /// <summary>
    /// Returns the concatenated visible text (all descendant text nodes) of the rendered component,
    /// excluding HTML attributes and styles.
    /// </summary>
    /// <remarks>
    /// Fluent UI v5 design tokens surface forbidden substrings inside attribute/style values — e.g.
    /// <c>color="success"</c>, <c>--colorStatusSuccessForeground1</c>, and
    /// <c>--colorNeutralForegroundOnBrand</c> (which contains "undo"). Safety guards that must verify a
    /// word is never *shown to the user* should assert against this visible text rather than the raw
    /// <c>Markup</c>, which would false-trigger on those framework token names.
    /// </remarks>
    public static string VisibleText<TComponent>(this IRenderedComponent<TComponent> rendered)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(rendered);
        return string.Concat(rendered.Nodes.Select(static node => node.TextContent));
    }
}
