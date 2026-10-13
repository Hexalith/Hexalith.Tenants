using System.Globalization;

using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.Localization;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Resolves shipped resources under an explicit culture during renderer event callbacks.</summary>
/// <param name="resources">The real resource localizer created by the localization factory.</param>
/// <param name="culture">The culture for this test context.</param>
internal sealed class CultureBoundTenantsLocalizer(IStringLocalizer resources, CultureInfo culture)
    : IStringLocalizer<TenantsResources>
{
    /// <inheritdoc />
    public LocalizedString this[string name] => InCulture(() => resources[name]);

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
        => InCulture(() => resources[name, arguments]);

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => InCulture(() => resources.GetAllStrings(includeParentCultures).ToArray());

    private T InCulture<T>(Func<T> resolve)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return resolve();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
