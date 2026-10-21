using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Lifecycle;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantLifecycleProjectionCategoryTests : FluentBunitContext
{
    public static IEnumerable<object[]> NonCurrentLifecycleCases()
    {
        foreach (string culture in new[] { "en", "fr" })
        {
            foreach (ProjectionLifecycleState lifecycle in Enum.GetValues<ProjectionLifecycleState>()
                .Where(value => value is not ProjectionLifecycleState.Current)
                .Append((ProjectionLifecycleState)999))
            {
                yield return [culture, lifecycle, false, TenantHighImpactFreshnessState.Current];
                yield return [culture, lifecycle, false, TenantHighImpactFreshnessState.Aging];
                yield return [culture, lifecycle, true, TenantHighImpactFreshnessState.Current];
                yield return [culture, lifecycle, true, TenantHighImpactFreshnessState.Aging];
                yield return [culture, lifecycle, true, TenantHighImpactFreshnessState.Refreshing];
            }
        }
    }

    [Theory]
    [MemberData(nameof(NonCurrentLifecycleCases))]
    public void NonCurrentLifecycleHasDedicatedCategoryAndLocalizedReason(
        string cultureName,
        ProjectionLifecycleState lifecycle,
        bool typedEvidence,
        TenantHighImpactFreshnessState freshness)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            Services.AddLocalization();
            ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
            gateway.SupportsTrackedLifecycleDispatch.Returns(true);
            Services.AddSingleton(gateway);
            TenantHighImpactActionEvidence evidence = Evidence(lifecycle) with { Freshness = freshness };
            TenantLifecycleAvailability availability = TenantLifecycleAvailability.FromEvidence(evidence);

            availability.IsUnavailable.ShouldBeTrue();
            availability.UnavailableReasonCategory.ToString().ShouldBe("ProjectionLifecycle");
            availability.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Unavailable.ProjectionLifecycle");
            availability.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
            TenantHighImpactActionAvailabilityEvaluator.Evaluate(evidence).UnavailableReason
                .ShouldBe(TenantHighImpactUnavailableReason.StaleData);

            IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
            LocalizedString category = localizer["Tenants.Lifecycle.UnavailableReason.ProjectionLifecycle"];
            category.ResourceNotFound.ShouldBeFalse();
            category.Value.ShouldBe(cultureName == "fr" ? "cycle de vie de la projection" : "projection lifecycle");
            IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderAvailability(evidence, typedEvidence);

            foreach (string action in new[] { "enable", "disable" })
            {
                cut.Find($"[data-testid='tenants-lifecycle-{action}']").GetAttribute("disabled").ShouldNotBeNull();
                cut.Find($"[data-testid='tenants-lifecycle-{action}-unavailable-reason']")
                    .GetAttribute("data-reason-category").ShouldBe("ProjectionLifecycle");
                string operation = action == "enable" ? "EnableTenant" : "DisableTenant";
                string expectedReason = localizer["Tenants.Lifecycle.Unavailable.ProjectionLifecycle", "tenant.alpha",
                    localizer[$"Tenants.Lifecycle.Operation.{operation}"].Value].Value;
                cut.Find($"[data-testid='tenants-lifecycle-{action}-reason']").TextContent.ShouldBe(expectedReason);
                cut.Find($"[data-testid='tenants-lifecycle-{action}-recovery']").TextContent
                    .ShouldBe(localizer["Tenants.HighImpact.Recovery.StaleData"].Value);
                cut.Find($"[data-testid='tenants-lifecycle-{action}']").Click();
            }

            cut.FindComponents<TenantLifecycleCommandFlow>().ShouldBeEmpty();
            gateway.ReceivedCalls().ShouldBeEmpty();

            TenantHighImpactActionEvidence recovered = evidence with { ProjectionLifecycle = ProjectionLifecycleState.Current };
            cut.Render(parameters => Configure(parameters, recovered, typedEvidence));
            cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldBeNull();
            cut.Find("[data-testid='tenants-lifecycle-disable-unavailable-reason']")
                .GetAttribute("data-reason-category").ShouldBe("None");
            gateway.ReceivedCalls().ShouldBeEmpty();
            cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
            cut.FindComponents<TenantLifecycleCommandFlow>().Count.ShouldBe(1);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    [Theory]
    [MemberData(nameof(EarlierBlockerCases))]
    public void EarlierBlockersKeepTheirCategory(string cultureName, string scenario, string expectedCategory)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            Services.AddLocalization();
            TenantHighImpactActionEvidence evidence = Evidence(ProjectionLifecycleState.Rebuilding);
            evidence = scenario switch
            {
                "permission" => evidence with { Authority = TenantHighImpactAuthorityEvidence.MissingPermission },
                "unauthorized-read" => evidence with { SurfaceKind = TenantDetailSurfaceKind.Unauthorized },
                "failed-read" => evidence with { SurfaceKind = TenantDetailSurfaceKind.Unavailable },
                "stale-read" => evidence with { SurfaceKind = TenantDetailSurfaceKind.Stale },
                "unknown-freshness" => evidence with { Freshness = TenantHighImpactFreshnessState.Unknown },
                "stale-freshness" => evidence with { Freshness = TenantHighImpactFreshnessState.Stale },
                "refreshing-without-baseline" => evidence with { Freshness = TenantHighImpactFreshnessState.Refreshing, HasCurrentBaseline = false },
                "undefined-freshness" => evidence with { Freshness = (TenantHighImpactFreshnessState)999 },
                _ => evidence with { SurfaceKind = (TenantDetailSurfaceKind)999 },
            };

            TenantLifecycleAvailability availability = TenantLifecycleAvailability.FromEvidence(evidence);
            availability.IsUnavailable.ShouldBeTrue();
            availability.UnavailableReasonCategory.ToString().ShouldBe(expectedCategory);
            availability.SafeMessageKey.ShouldBe(expectedCategory == "MissingPermission"
                ? "Tenants.Lifecycle.Unavailable.MissingPermission"
                : "Tenants.Lifecycle.Unavailable.StaleFreshness");
            IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderAvailability(evidence, true);
            cut.Find("[data-testid='tenants-lifecycle-disable-unavailable-reason']")
                .GetAttribute("data-reason-category").ShouldBe(expectedCategory);
            cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
            IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
            cut.Find("[data-testid='tenants-lifecycle-disable-reason']").TextContent
                .ShouldBe(localizer[$"Tenants.HighImpact.Unavailable.{expectedCategory}"].Value);
            cut.Find("[data-testid='tenants-lifecycle-disable-recovery']").TextContent
                .ShouldBe(localizer[$"Tenants.HighImpact.Recovery.{expectedCategory}"].Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    public static IEnumerable<object[]> EarlierBlockerCases()
    {
        foreach (string culture in new[] { "en", "fr" })
        {
            foreach (string scenario in new[] { "permission", "unauthorized-read", "failed-read", "stale-read",
                "unknown-freshness", "stale-freshness", "refreshing-without-baseline", "undefined-freshness", "undefined-surface" })
            {
                yield return [culture, scenario, scenario is "permission" or "unauthorized-read" ? "MissingPermission" : "StaleData"];
            }
        }
    }

    [Theory]
    [InlineData("en", TenantStatus.Active, false)]
    [InlineData("en", TenantStatus.Active, true)]
    [InlineData("en", TenantStatus.Disabled, false)]
    [InlineData("en", TenantStatus.Disabled, true)]
    [InlineData("fr", TenantStatus.Active, false)]
    [InlineData("fr", TenantStatus.Active, true)]
    [InlineData("fr", TenantStatus.Disabled, false)]
    [InlineData("fr", TenantStatus.Disabled, true)]
    public void OpenConfirmationUsesProjectionLifecycleReasonAndRetainsRefreshRecovery(
        string cultureName, TenantStatus status, bool typedEvidence)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            Services.AddLocalization();
            ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
            gateway.SupportsTrackedLifecycleDispatch.Returns(true);
            Services.AddSingleton(gateway);
            TenantHighImpactActionEvidence evidence = Evidence(ProjectionLifecycleState.Current) with { TenantStatus = status };
            string action = status is TenantStatus.Active ? "disable" : "enable";
            string operation = status is TenantStatus.Active ? "DisableTenant" : "EnableTenant";
            IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderAvailability(evidence, typedEvidence);
            cut.Find($"[data-testid='tenants-lifecycle-{action}']").Click();
            cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldBeNull();
            gateway.ClearReceivedCalls();

            cut.Render(parameters => Configure(parameters, evidence with { ProjectionLifecycle = ProjectionLifecycleState.Rebuilding }, typedEvidence));
            IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
            cut.Find("[data-testid='tenants-lifecycle-flow-unavailable-reason']").TextContent.Trim()
                .ShouldBe(localizer["Tenants.Lifecycle.Unavailable.ProjectionLifecycle", "tenant.alpha",
                    localizer[$"Tenants.Lifecycle.Operation.{operation}"].Value].Value);
            cut.Find("[data-testid='tenants-lifecycle-recovery']").TextContent.Trim()
                .ShouldBe(localizer["Tenants.HighImpact.Recovery.StaleData"].Value);
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldNotBeNull();
            cut.Find("form").Submit();
            gateway.ReceivedCalls().ShouldAllBe(call => call.GetMethodInfo().IsSpecialName);

            cut.Render(parameters => Configure(parameters, evidence, typedEvidence));
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldBeNull();
            cut.FindComponents<TenantLifecycleCommandFlow>().Count.ShouldBe(1);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    public static IEnumerable<object?[]> MissingProjectionVersionCases()
    {
        foreach (string culture in new[] { "en", "fr" })
        {
            foreach (bool typedEvidence in new[] { false, true })
            {
                foreach (string? version in new[] { null, string.Empty, "   " })
                {
                    yield return [culture, typedEvidence, version, TenantStatus.Active];
                    yield return [culture, typedEvidence, version, TenantStatus.Disabled];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(MissingProjectionVersionCases))]
    public void MissingProjectionVersionHasDedicatedLocalizedLauncherAndMountedFlowRecovery(
        string cultureName, bool typedEvidence, string? version, TenantStatus status)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            Services.AddLocalization();
            ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
            gateway.SupportsTrackedLifecycleDispatch.Returns(true);
            gateway.EnableTenantTrackedAsync(Arg.Any<TenantLifecycleCommandRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Ambiguous(
                    call.ArgAt<string>(1), "Tenants.Lifecycle.SubmissionEvidence.Ambiguous")));
            gateway.DisableTenantTrackedAsync(Arg.Any<TenantLifecycleCommandRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Ambiguous(
                    call.ArgAt<string>(1), "Tenants.Lifecycle.SubmissionEvidence.Ambiguous")));
            Services.AddSingleton(gateway);
            Services.AddSingleton<TenantLifecycleAttemptTracker>();
            TenantHighImpactActionEvidence available = Evidence(ProjectionLifecycleState.Current) with { TenantStatus = status };
            TenantHighImpactActionEvidence missingVersion = available with { ProjectionVersion = version };
            TenantLifecycleAvailability availability = TenantLifecycleAvailability.FromEvidence(missingVersion);
            availability.IsUnavailable.ShouldBeTrue();
            availability.UnavailableReasonCategory.ShouldBe(TenantLifecycleUnavailableReasonCategory.StaleData);
            availability.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Unavailable.MissingProjectionVersion");
            TenantHighImpactActionAvailabilityEvaluator.Evaluate(missingVersion).UnavailableReason
                .ShouldBe(TenantHighImpactUnavailableReason.StaleData);

            IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
            LocalizedString reason = localizer["Tenants.Lifecycle.Unavailable.MissingProjectionVersion"];
            LocalizedString recovery = localizer["Tenants.Lifecycle.Recovery.MissingProjectionVersion"];
            reason.ResourceNotFound.ShouldBeFalse();
            recovery.ResourceNotFound.ShouldBeFalse();
            reason.Value.ShouldBe(cultureName == "fr"
                ? "Cette action de cycle de vie est indisponible car la projection du locataire ne fournit aucune preuve de version permettant sa vérification."
                : "This lifecycle action is unavailable because the tenant projection has no version evidence for verification.");
            recovery.Value.ShouldBe(cultureName == "fr"
                ? "Poursuivez en lecture seule et demandez au support de rétablir les preuves de version de la projection avant de réessayer cette action."
                : "Continue read-only and ask support to restore projection-version evidence before trying this action again.");
            IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderAvailability(missingVersion, typedEvidence);
            foreach (string actionName in new[] { "enable", "disable" })
            {
                cut.Find($"[data-testid='tenants-lifecycle-{actionName}']").GetAttribute("disabled").ShouldNotBeNull();
                cut.Find($"[data-testid='tenants-lifecycle-{actionName}-reason']").TextContent.ShouldBe(reason.Value);
                cut.Find($"[data-testid='tenants-lifecycle-{actionName}-recovery']").TextContent.ShouldBe(recovery.Value);
                cut.Find($"[data-testid='tenants-lifecycle-{actionName}']").Click();
            }

            cut.FindComponents<TenantLifecycleCommandFlow>().ShouldBeEmpty();
            gateway.ReceivedCalls().ShouldBeEmpty();

            string action = status is TenantStatus.Active ? "disable" : "enable";
            cut.Render(parameters => Configure(parameters, available, typedEvidence));
            cut.Find($"[data-testid='tenants-lifecycle-{action}']").GetAttribute("disabled").ShouldBeNull();
            cut.Find($"[data-testid='tenants-lifecycle-{action}']").Click();
            cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldBeNull();
            gateway.ClearReceivedCalls();

            cut.Render(parameters => Configure(parameters, missingVersion, typedEvidence));
            cut.Find("[data-testid='tenants-lifecycle-flow-unavailable-reason']").TextContent.Trim().ShouldBe(reason.Value);
            cut.Find("[data-testid='tenants-lifecycle-recovery']").TextContent.Trim().ShouldBe(recovery.Value);
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldNotBeNull();
            cut.Find("form").Submit();
            gateway.ReceivedCalls().ShouldAllBe(call => call.GetMethodInfo().IsSpecialName);

            cut.Render(parameters =>
            {
                Configure(parameters, available, typedEvidence);
                parameters.Add(component => component.AuthorizationReflectionProvider,
                        _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                    .Add(component => component.ProjectionEvidenceProvider,
                        (_, _) => Task.FromResult<TenantDetailSnapshot?>(TenantDetailSnapshot.Ready(
                            new TenantDetail(available.TenantId, "Alpha", "Tenant alpha description", status,
                                [], new Dictionary<string, string>(), DateTimeOffset.UnixEpoch),
                            eTag: null, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current,
                            available.ProjectionVersion)))
                    .Add(component => component.CommandActivityLease, _ => Task.FromResult(true));
            });
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldBeNull();
            cut.Find("form").Submit();
            TenantLifecycleCommandFlow flow = cut.FindComponent<TenantLifecycleCommandFlow>().Instance;
            cut.WaitForAssertion(() => flow.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent));
            flow.Snapshot.BaselineProjectionVersion.ShouldBe(available.ProjectionVersion);
            flow.Snapshot.PreviewProjectionVersion.ShouldBe(available.ProjectionVersion);
            flow.Snapshot.LastConfirmedStatus.ShouldBe(status);
            flow.Snapshot.MessageId.ShouldNotBeNullOrWhiteSpace();
            flow.Snapshot.CorrelationId.ShouldBeNull();
            var dispatch = gateway.ReceivedCalls().Where(call => call.GetMethodInfo().Name
                is nameof(ITenantCommandGateway.EnableTenantTrackedAsync)
                    or nameof(ITenantCommandGateway.DisableTenantTrackedAsync)).ShouldHaveSingleItem();
            dispatch.GetArguments()[0].ShouldBe(new TenantLifecycleCommandRequest(available.TenantId,
                status is TenantStatus.Active ? TenantLifecycleOperation.DisableTenant : TenantLifecycleOperation.EnableTenant));
            dispatch.GetArguments()[1].ShouldBe(flow.Snapshot.MessageId);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    [Theory]
    [InlineData("en", "permission", "MissingPermission")]
    [InlineData("fr", "permission", "MissingPermission")]
    [InlineData("en", "stale", "StaleData")]
    [InlineData("fr", "stale", "StaleData")]
    [InlineData("en", "refreshing-without-baseline", "StaleData")]
    [InlineData("fr", "refreshing-without-baseline", "StaleData")]
    [InlineData("en", "lifecycle", "ProjectionLifecycle")]
    [InlineData("fr", "lifecycle", "ProjectionLifecycle")]
    public void MissingProjectionVersionPreservesEarlierBlockersInLaunchersAndMountedFlow(
        string cultureName, string blocker, string expectedCategory)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            Services.AddLocalization();
            ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
            gateway.SupportsTrackedLifecycleDispatch.Returns(true);
            Services.AddSingleton(gateway);
            TenantHighImpactActionEvidence available = Evidence(ProjectionLifecycleState.Current);
            IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderAvailability(available, typedEvidence: true);
            cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
            cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
            TenantHighImpactActionEvidence blocked = available with { ProjectionVersion = null };
            blocked = blocker switch
            {
                "permission" => blocked with { Authority = TenantHighImpactAuthorityEvidence.MissingPermission },
                "stale" => blocked with { Freshness = TenantHighImpactFreshnessState.Stale },
                "refreshing-without-baseline" => blocked with { Freshness = TenantHighImpactFreshnessState.Refreshing, HasCurrentBaseline = false },
                _ => blocked with { ProjectionLifecycle = ProjectionLifecycleState.Rebuilding },
            };
            TenantLifecycleAvailability availability = TenantLifecycleAvailability.FromEvidence(blocked);
            availability.UnavailableReasonCategory.ToString().ShouldBe(expectedCategory);
            availability.SafeMessageKey.ShouldNotBe("Tenants.Lifecycle.Unavailable.MissingProjectionVersion");
            IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
            string expectedReason = blocker == "lifecycle"
                ? localizer["Tenants.Lifecycle.Unavailable.ProjectionLifecycle", "tenant.alpha",
                    localizer["Tenants.Lifecycle.Operation.DisableTenant"].Value].Value
                : localizer[$"Tenants.HighImpact.Unavailable.{expectedCategory}"].Value;
            string expectedRecovery = localizer[$"Tenants.HighImpact.Recovery.{(blocker == "permission" ? "MissingPermission" : "StaleData")}"].Value;
            gateway.ClearReceivedCalls();

            cut.Render(parameters => Configure(parameters, blocked, typedEvidence: true));
            cut.Find("[data-testid='tenants-lifecycle-disable-reason']").TextContent.ShouldBe(expectedReason);
            cut.Find("[data-testid='tenants-lifecycle-disable-recovery']").TextContent.ShouldBe(expectedRecovery);
            cut.Find("[data-testid='tenants-lifecycle-flow-unavailable-reason']").TextContent.Trim().ShouldBe(expectedReason);
            cut.Find("[data-testid='tenants-lifecycle-recovery']").TextContent.Trim().ShouldBe(expectedRecovery);
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldNotBeNull();
            cut.Find("form").Submit();
            gateway.ReceivedCalls().ShouldAllBe(call => call.GetMethodInfo().IsSpecialName);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    [Theory]
    [InlineData("en", "aging")]
    [InlineData("fr", "aging")]
    [InlineData("en", "refreshing-with-baseline")]
    [InlineData("fr", "refreshing-with-baseline")]
    [InlineData("en", "unsupported")]
    [InlineData("fr", "unsupported")]
    [InlineData("en", "viewport")]
    [InlineData("fr", "viewport")]
    [InlineData("en", "admission")]
    [InlineData("fr", "admission")]
    public void MissingProjectionVersionRemainsTheReasonAfterUsableReadEvidence(
        string cultureName, string scenario)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            Services.AddLocalization();
            ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
            gateway.SupportsTrackedLifecycleDispatch.Returns(true);
            Services.AddSingleton(gateway);
            TenantHighImpactActionEvidence available = Evidence(ProjectionLifecycleState.Current);
            IRenderedComponent<TenantLifecycleActionAvailability> cut = RenderAvailability(available, typedEvidence: true);
            cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
            cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
            TenantHighImpactActionEvidence blocked = available with { ProjectionVersion = null };
            blocked = scenario switch
            {
                "aging" => blocked with { Freshness = TenantHighImpactFreshnessState.Aging },
                "refreshing-with-baseline" => blocked with { Freshness = TenantHighImpactFreshnessState.Refreshing, HasCurrentBaseline = true },
                "unsupported" => blocked with { Support = TenantHighImpactSupportEvidence.Missing },
                "viewport" => blocked with { Viewport = TenantHighImpactViewportState.Unsafe },
                _ => blocked with { Admission = TenantHighImpactAdmissionEvidence.Busy },
            };
            TenantLifecycleAvailability.FromEvidence(blocked).SafeMessageKey
                .ShouldBe("Tenants.Lifecycle.Unavailable.MissingProjectionVersion");
            IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
            string expectedReason = localizer["Tenants.Lifecycle.Unavailable.MissingProjectionVersion"].Value;
            string expectedRecovery = localizer["Tenants.Lifecycle.Recovery.MissingProjectionVersion"].Value;
            gateway.ClearReceivedCalls();

            cut.Render(parameters => Configure(parameters, blocked, typedEvidence: true));
            foreach (string action in new[] { "enable", "disable" })
            {
                cut.Find($"[data-testid='tenants-lifecycle-{action}-reason']").TextContent.ShouldBe(expectedReason);
                cut.Find($"[data-testid='tenants-lifecycle-{action}-recovery']").TextContent.ShouldBe(expectedRecovery);
                cut.Find($"[data-testid='tenants-lifecycle-{action}']").GetAttribute("disabled").ShouldNotBeNull();
            }

            cut.Find("[data-testid='tenants-lifecycle-flow-unavailable-reason']").TextContent.Trim().ShouldBe(expectedReason);
            cut.Find("[data-testid='tenants-lifecycle-recovery']").TextContent.Trim().ShouldBe(expectedRecovery);
            cut.Find("[data-testid='tenants-lifecycle-confirm']").GetAttribute("disabled").ShouldNotBeNull();
            cut.Find("form").Submit();
            gateway.ReceivedCalls().ShouldAllBe(call => call.GetMethodInfo().IsSpecialName);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    private IRenderedComponent<TenantLifecycleActionAvailability> RenderAvailability(
        TenantHighImpactActionEvidence evidence, bool typedEvidence)
        => Render<TenantLifecycleActionAvailability>(parameters => Configure(parameters, evidence, typedEvidence));

    private static void Configure(ComponentParameterCollectionBuilder<TenantLifecycleActionAvailability> parameters,
        TenantHighImpactActionEvidence evidence, bool typedEvidence)
    {
        parameters.Add(component => component.TenantId, evidence.TenantId)
            .Add(component => component.Detail, new TenantDetail(evidence.TenantId, "Alpha", "Tenant alpha description",
                evidence.TenantStatus, [], new Dictionary<string, string>(), DateTimeOffset.UnixEpoch))
            .Add(component => component.CurrentStatus, evidence.TenantStatus)
            .Add(component => component.ProjectionVersion, evidence.ProjectionVersion)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.SurfaceKind, evidence.SurfaceKind)
            .Add(component => component.Freshness, evidence.Freshness is TenantHighImpactFreshnessState.Aging
                ? ReadModelFreshnessState.Aging : ReadModelFreshnessState.Current)
            .Add(component => component.Lifecycle, evidence.ProjectionLifecycle);
        if (typedEvidence)
        {
            parameters.Add(component => component.DisableEvidence, evidence)
                .Add(component => component.EnableEvidence, evidence with { Action = TenantHighImpactAction.EnableTenant });
        }
    }

    private static TenantHighImpactActionEvidence Evidence(ProjectionLifecycleState lifecycle)
        => new TenantLifecycleAvailabilityInput(
            "tenant.alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantDetailSurfaceKind.Ready,
            IsCommandSurfaceConnected: true, "tenant-sequence:41", TenantLifecycleGovernanceReadiness.Ready,
            TenantLifecycleAuthorizationReflectionState.Authorized, Lifecycle: lifecycle)
            .Evaluate(TenantLifecycleOperation.DisableTenant).Evidence!;
}
