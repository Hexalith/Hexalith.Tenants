using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Exercises the actual non-submitting start markup, localization and callback boundary.</summary>
public sealed class TenantCorrectionStartPanelTests : FluentBunitContext
{
    private new IRenderedComponent<TComponent> Render<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));
        return base.Render(parameters);
    }
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void StartAndHandoffPreserveSafeEvidenceWithoutCommandsOrStatusLookups(string culture)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Services.AddLocalization();
            ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
            Services.AddSingleton(commands);
            TenantCorrectionStartIntent intent = Intent();
            TenantCorrectionStartIntent? handedOff = null;
            IRenderedComponent<TenantCorrectionStartPanel> cut = Render<TenantCorrectionStartPanel>(parameters => parameters
                .Add(p => p.Intent, intent).Add(p => p.OnHandoff, value => handedOff = value));
            cut.Find("[data-testid='tenants-correction-start-reference']").TextContent.ShouldBe("event-original");
            cut.Find("[data-testid='tenants-correction-start-timestamp']").TextContent.ShouldBe("2026-06-01 10:00:00 UTC");
            cut.Find("[data-testid='tenants-correction-start-scope']").TextContent.ShouldBe("tenant.alpha");
            cut.Find("[data-testid='tenants-correction-start-target']").TextContent.ShouldBe("target-user");
            cut.VisibleText().ShouldNotContain("Tenants.Correction.");
            cut.VisibleText().ShouldNotContain("TenantReader");
            cut.VisibleText().ShouldNotContain("tenant-projection-current");
            cut.VisibleText().ShouldNotContain("ProjectionBacked");
            cut.FindAll("[data-testid='tenants-correction-confirm']").ShouldBeEmpty();
            cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-correction-start-handoff']").Click();
            handedOff.ShouldBe(intent);
            commands.ReceivedCalls().ShouldBeEmpty();

            // Browser validation consumes this rendered markup, never a handwritten copy of the panel.
            string? fixtureDirectory = Environment.GetEnvironmentVariable("TENANTS_CORRECTION_FIXTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(fixtureDirectory))
            {
                Directory.CreateDirectory(fixtureDirectory);
                File.WriteAllText(Path.Combine(fixtureDirectory, $"tenant-correction-start-{culture}.html"), cut.Markup);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAndEscapeCloseWithoutHandoff(bool escape)
    {
        Services.AddLocalization();
        int closed = 0;
        int handedOff = 0;
        IRenderedComponent<TenantCorrectionStartPanel> cut = Render<TenantCorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, Intent()).Add(p => p.OnClose, () => closed++)
            .Add(p => p.OnHandoff, _ => handedOff++));
        if (escape) cut.Find("[data-testid='tenants-correction-start-panel']").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        else cut.Find("[data-testid='tenants-correction-start-cancel']").Click();
        closed.ShouldBe(1);
        handedOff.ShouldBe(0);
    }

    [Fact]
    public void UnsafeIdentifiersNeverRenderOrReachHandoff()
    {
        Services.AddLocalization();
        int handedOff = 0;
        TenantCorrectionStartIntent unsafeIntent = Intent() with { TargetUserId = "access_token=secret", OriginalAuditReference = "Bearer secret" };
        IRenderedComponent<TenantCorrectionStartPanel> cut = Render<TenantCorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, unsafeIntent).Add(p => p.OnHandoff, _ => handedOff++));
        cut.Markup.ShouldNotContain("secret");
        cut.Find("[data-testid='tenants-correction-start-handoff']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-start-handoff']").Click();
        handedOff.ShouldBe(0);
    }

    [Fact]
    public void EmptyRecoveryDisclosesOnlyCurrentMembershipAndAuthority()
    {
        Services.AddLocalization();
        TenantCorrectionStartIntent intent = Intent(empty: true);
        IRenderedComponent<TenantCorrectionStartPanel> cut = Render<TenantCorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.OnHandoff, _ => { }));
        cut.Find("[data-testid='tenants-correction-start-empty-recovery']").TextContent
            .ShouldContain("does not establish whether earlier membership existed");
        cut.Find("[data-testid='tenants-correction-start-intended-role']").TextContent.ShouldBe("Tenant owner");
    }

    private static TenantCorrectionStartIntent Intent(bool empty = false)
    {
        TenantAuditRow row = new("event-original", "UserRemovedFromTenant", AuditEventCategory.Access, "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture), "tenant.alpha", "target-user",
            "tenant.alpha", "User removed", string.Empty, ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked, new TenantAuditNarrative(UserId: "target-user"));
        TenantCorrectionProjection projection = new("tenant.alpha", "target-user", TenantStatus.Active, null,
            empty, true, empty, true, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked);
        return TenantCorrectionStartIntent.Evaluate(new(TenantAuditReceipt.FromRow(row), row, true, true, string.Empty,
            IntendedRole: empty ? TenantRole.TenantOwner : TenantRole.TenantReader, Projection: projection));
    }
}
