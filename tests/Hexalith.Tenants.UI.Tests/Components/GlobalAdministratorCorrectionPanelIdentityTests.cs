using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorCorrectionPanelTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void WhitespaceIdentitiesExportActualAnnotatedFluentMarkup(string culture)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Services.AddLocalization();
            Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
            string[] identities = ["ops admin", "ops\u00A0admin", "ops\u202Fadmin", "ops\u2009admin", @"ops\{U+00A0}admin"];
            List<string> rendered = [];
            foreach (string identity in identities)
            {
                var intent = RestoreIntent();
                // The real intent keeps the literal target; only its presentation is encoded.
                intent = intent with { TargetUserId = identity, RequiredPreviewInputs = new Dictionary<string, string>(intent.RequiredPreviewInputs) { ["userId"] = identity } };
                IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
                    .Add(component => component.Intent, intent)
                    .Add(component => component.CurrentProjection, Projection("other-admin")));
                var target = cut.Find("[data-testid='tenants-correction-target-user']");
                target.TextContent.ShouldBe(GlobalAdministratorIdentityDisplay.Encode(identity));
                rendered.Add(cut.Markup);
            }
            rendered.Distinct(StringComparer.Ordinal).Count().ShouldBe(identities.Length);
            string? directory = Environment.GetEnvironmentVariable("TENANTS_CORRECTION_FIXTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, $"global-correction-identities-{culture}.html"), string.Join("\n", rendered));
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
