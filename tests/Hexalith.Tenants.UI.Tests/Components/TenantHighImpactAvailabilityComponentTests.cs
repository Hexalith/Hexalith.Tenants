using Bunit;

using System.Globalization;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;

using Fluxor;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;
using NSubstitute;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantHighImpactAvailabilityComponentTests : FluentBunitContext
{
    [Fact]
    public void Configuration_results_keep_accessible_facts_visible_while_unsafe_viewport_is_read_only()
    {
        Services.AddLocalization();
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            isGlobalAdministrator: false,
            ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);
        TenantHighImpactActionEvidence set = Evidence(TenantHighImpactAction.SetConfiguration);
        TenantHighImpactActionEvidence remove = Evidence(TenantHighImpactAction.RemoveConfiguration);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.SetEvidence, set)
            .Add(component => component.RemoveEvidence, remove));

        cut.Find("[data-testid='tenants-config-set-availability']")
            .GetAttribute("data-action").ShouldBe("SetConfiguration");
        cut.Find("[data-testid='tenants-config-remove-availability']")
            .GetAttribute("data-action").ShouldBe("RemoveConfiguration");
        cut.Find("[data-testid='tenants-config-set-identity']").TextContent.ShouldBe("tenant.alpha");
        cut.Find("[data-testid='tenants-config-set-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-config-set-freshness']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-config-set-reason']").TextContent
            .ShouldContain("read-only", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-set-recovery']").TextContent
            .ShouldContain("viewport", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-management-remove-open']")
            .GetAttribute("disabled").ShouldNotBeNull();
    }

    [Fact]
    public void Configuration_launchers_are_associated_with_stable_reason_and_recovery_ids()
    {
        Services.AddLocalization();
        Services.AddSingleton(Substitute.For<ITenantCommandGateway>());
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            isGlobalAdministrator: false,
            ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);
        TenantHighImpactActionEvidence set = Evidence(TenantHighImpactAction.SetConfiguration) with
        {
            Viewport = TenantHighImpactViewportState.Safe,
        };
        TenantHighImpactActionEvidence remove = Evidence(TenantHighImpactAction.RemoveConfiguration) with
        {
            Viewport = TenantHighImpactViewportState.Safe,
        };

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SetEvidence, set)
            .Add(component => component.RemoveEvidence, remove));

        cut.Find("#tenants-config-set-reason");
        cut.Find("#tenants-config-set-recovery");
        cut.Find("#tenants-config-remove-reason");
        cut.Find("#tenants-config-remove-recovery");
        cut.Find("[data-testid='tenants-config-set-open']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-config-set-reason tenants-config-set-recovery");
        cut.FindAll("[data-testid='tenants-config-management-remove-open']")
            .ShouldAllBe(static button => button.GetAttribute("aria-describedby")
                == "tenants-config-remove-reason tenants-config-remove-recovery");
    }

    [Fact]
    public void Null_context_and_omitted_evidence_render_fail_closed_without_exceptions()
    {
        Services.AddLocalization();

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>();

        cut.Find("[data-testid='tenants-config-management-unavailable']");
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-config-management-remove-open']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-set-identity']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public void Omitted_typed_evidence_does_not_synthesize_ready_support_preview_admission_or_viewport()
    {
        Services.AddLocalization();
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            isGlobalAdministrator: false,
            ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current));

        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-management-remove-open']")
            .GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-config-set-reason']").TextContent
            .ShouldContain("read-only", Case.Insensitive);
    }

    [Fact]
    public async Task Fluxor_dispatch_updates_scoped_viewport_observation_and_action_eligibility_in_both_directions()
    {
        Services.AddLocalization();
        Services.AddSingleton(Substitute.For<ITenantCommandGateway>());
        Services.AddScoped<TenantHighImpactViewportObservation>();
        Services.AddFluxor(options => options.ScanAssemblies(typeof(TenantHighImpactViewportEffects).Assembly));
        IStore store = Services.GetRequiredService<IStore>();
        await store.InitializeAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        TenantHighImpactViewportObservation observation = Services.GetRequiredService<TenantHighImpactViewportObservation>();
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            isGlobalAdministrator: false,
            ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);

        IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SetEvidence, Evidence(TenantHighImpactAction.SetConfiguration) with { Viewport = observation.State })
            .Add(component => component.RemoveEvidence, Evidence(TenantHighImpactAction.RemoveConfiguration) with { Viewport = observation.State }));
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();

        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Tablet));
        SpinWait.SpinUntil(() => observation.State is TenantHighImpactViewportState.Safe, TimeSpan.FromSeconds(1))
            .ShouldBeTrue();
        cut.Render(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SetEvidence, Evidence(TenantHighImpactAction.SetConfiguration) with { Viewport = observation.State })
            .Add(component => component.RemoveEvidence, Evidence(TenantHighImpactAction.RemoveConfiguration) with { Viewport = observation.State }));
        cut.Find("[data-testid='tenants-config-set-open']");

        dispatcher.Dispatch(new ViewportTierChangedAction((ViewportTier)255));
        SpinWait.SpinUntil(() => observation.State is TenantHighImpactViewportState.Unknown, TimeSpan.FromSeconds(1))
            .ShouldBeTrue();
        cut.Render(parameters => parameters
            .Add(component => component.Context, context)
            .Add(component => component.SetEvidence, Evidence(TenantHighImpactAction.SetConfiguration) with { Viewport = observation.State })
            .Add(component => component.RemoveEvidence, Evidence(TenantHighImpactAction.RemoveConfiguration) with { Viewport = observation.State }));
        cut.FindAll("[data-testid='tenants-config-set-open']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("en", "Active", "Aging", "Set configuration", "aging or refreshing", "No recovery")]
    [InlineData("fr", "Actif", "Vieillissante", "Definir la configuration", "vieillit ou est en cours", "Aucune recuperation")]
    public void English_and_french_configuration_facts_keep_whole_associated_strings(
        string cultureName,
        string expectedStatus,
        string expectedFreshness,
        string expectedAction,
        string expectedReason,
        string expectedRecovery)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Services.AddLocalization();
            Services.AddSingleton(Substitute.For<ITenantCommandGateway>());
            TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
                "tenant.alpha",
                TenantStatus.Active,
                isGlobalAdministrator: false,
                ["billing"],
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]);
            TenantHighImpactActionEvidence set = Evidence(TenantHighImpactAction.SetConfiguration) with
            {
                Freshness = TenantHighImpactFreshnessState.Aging,
                Viewport = TenantHighImpactViewportState.Safe,
            };
            TenantHighImpactActionEvidence remove = Evidence(TenantHighImpactAction.RemoveConfiguration) with
            {
                Freshness = TenantHighImpactFreshnessState.Aging,
                Viewport = TenantHighImpactViewportState.Safe,
            };

            IRenderedComponent<TenantConfigurationManagement> cut = Render<TenantConfigurationManagement>(parameters => parameters
                .Add(component => component.Context, context)
                .Add(component => component.SetEvidence, set)
                .Add(component => component.RemoveEvidence, remove));

            cut.Find("[data-testid='tenants-config-set-identity']").TextContent.ShouldBe("tenant.alpha");
            cut.Find("[data-testid='tenants-config-set-status']").TextContent.ShouldContain(expectedStatus);
            cut.Find("[data-testid='tenants-config-set-freshness']").TextContent.ShouldContain(expectedFreshness);
            cut.Find("[data-testid='tenants-config-set-action']").TextContent.ShouldContain(expectedAction);
            cut.Find("[data-testid='tenants-config-set-reason']").TextContent.ShouldContain(expectedReason);
            cut.Find("[data-testid='tenants-config-set-recovery']").TextContent.ShouldContain(expectedRecovery);
            cut.Find("[data-testid='tenants-config-set-open']");
            cut.Find("[data-testid='tenants-config-management-remove-open']")
                .GetAttribute("disabled").ShouldBeNull();
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    private static TenantHighImpactActionEvidence Evidence(TenantHighImpactAction action)
        => new(
            "tenant.alpha",
            action,
            TenantHighImpactEvaluationStage.PreviewEntry,
            TenantStatus.Active,
            TenantHighImpactFreshnessState.Current,
            HasCurrentBaseline: true,
            TenantDetailSurfaceKind.Ready,
            ProjectionLifecycleState.Current,
            TenantHighImpactAuthorityEvidence.Authorized,
            TenantHighImpactNamespaceScopeEvidence.Authorized,
            TenantHighImpactSupportEvidence.Ready,
            TenantHighImpactAdmissionEvidence.Available,
            TenantHighImpactPreviewEvidence.Ready,
            TenantHighImpactProofEvidence.NotRequired,
            TenantHighImpactViewportState.Unsafe,
            IsInputComplete: false,
            action is TenantHighImpactAction.RemoveConfiguration
                ? TenantHighImpactTargetState.Present
                : TenantHighImpactTargetState.Unknown);

}
