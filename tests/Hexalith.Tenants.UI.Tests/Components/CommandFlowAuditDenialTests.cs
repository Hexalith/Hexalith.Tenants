using AngleSharp.Dom;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Components.Tenants.Lifecycle;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Qualifies denied audit recovery across the seven existing-tenant command surfaces.</summary>
public sealed class CommandFlowAuditDenialTests : FluentBunitContext
{
    private Func<bool, bool, IComponent>? _updateAuditAuthority;

    /// <summary>Gets every affected flow with denied, allowed and indeterminate audit authority.</summary>
    /// <returns>Denial regressions paired with enabled and disabled audit recovery controls.</returns>
    public static IEnumerable<object[]> Cases()
    {
        foreach (string kind in new[] { "add", "change", "remove-member", "metadata", "lifecycle", "set", "remove-configuration" })
        {
            yield return [kind, true, false];
            yield return [kind, false, true];
            yield return [kind, false, false];
        }
    }

    /// <summary>Audit authority controls recovery presence and availability, including revocation after dispatch.</summary>
    /// <param name="kind">The command surface to submit.</param>
    /// <param name="auditReadDenied">Whether the current caller is denied audit access.</param>
    /// <param name="auditReadAvailable">Whether audit access has been affirmatively confirmed.</param>
    [Theory]
    [MemberData(nameof(Cases))]
    public void AuditRecoveryRespectsDeniedReadWithoutAnEmptyInspectShell(string kind, bool auditReadDenied, bool auditReadAvailable)
    {
        ITenantCommandGateway gateway = RegisterServices();
        (IRenderedComponent<IComponent> cut, Func<TenantCommandLifecycleState> state) = RenderFlow(kind, auditReadDenied, auditReadAvailable);
        PrepareForm(kind, cut);
        cut.Find("form").Submit();

        // A real mutation and status lookup must reach the shared audit state in every authority case.
        // An empty preview or an unavailable command surface cannot satisfy the absence assertions.
        cut.WaitForAssertion(() => state().ShouldBe(TenantCommandLifecycleState.Degraded));
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("delayed");
        if (kind is "lifecycle")
        {
            // A degraded lifecycle command releases retained ownership, so its audit control cannot re-query.
            cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
        }
        else
        {
            cut.FindAll("[data-recovery-verb='refresh']").ShouldHaveSingleItem();
        }
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(1);
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ITenantCommandGateway.GetStatusAsync)).ShouldBe(1);

        if (auditReadDenied)
        {
            AssertNoInspectRecovery(cut);
        }
        else
        {
            IElement shell = cut.FindAll("[data-recovery-verb='inspectaudit']").ShouldHaveSingleItem();
            shell.GetAttribute("data-testid").ShouldBe("tenants-audit-availability-recovery-inspectaudit");
            IElement control = shell.QuerySelector("[data-testid='tenants-audit-entrypoint']").ShouldNotBeNull();
            control.QuerySelector("[data-testid='tenants-command-audit-entrypoint']").ShouldNotBeNull()
                .TextContent.ShouldNotBeNullOrWhiteSpace();
            if (auditReadAvailable)
            {
                control.LocalName.ShouldBe("fluent-anchor-button");
                control.GetAttribute("disabled").ShouldBeNull();
                control.GetAttribute("href").ShouldNotBeNull().ShouldStartWith("/tenants/tenant.alpha/audit?");
                control.GetAttribute("href")!.ShouldContain("source=command-result");

                IComponent retainedComponent = _updateAuditAuthority.ShouldNotBeNull().Invoke(true, false);
                retainedComponent.ShouldBeSameAs(cut.Instance);
                cut.WaitForAssertion(() => AssertNoInspectRecovery(cut));
                state().ShouldBe(TenantCommandLifecycleState.Degraded);
                cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("delayed");
                gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(1);
                gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ITenantCommandGateway.GetStatusAsync)).ShouldBe(1);
            }
            else
            {
                control.LocalName.ShouldBe("fluent-button");
                control.HasAttribute("disabled").ShouldBeTrue();
                control.GetAttribute("href").ShouldBeNull();
            }
        }
    }

    private static void AssertNoInspectRecovery(IRenderedComponent<IComponent> cut)
    {
        cut.FindAll("[data-recovery-verb='inspectaudit']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-availability-recovery-inspectaudit']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-entrypoint']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-command-audit-entrypoint']").ShouldBeEmpty();
    }

    private ITenantCommandGateway RegisterServices()
    {
        Services.AddLocalization();
        Services.AddSingleton(new TenantLifecycleAttemptTracker());
        Services.AddSingleton(new TenantSetConfigurationAttemptTracker());
        Services.AddSingleton(new TenantRemoveConfigurationAttemptTracker());
        var viewport = new TenantHighImpactViewportObservation();
        viewport.Observe(ViewportTier.Desktop);
        Services.AddSingleton(viewport);
        Services.AddSingleton(Substitute.For<ITenantQueryGateway>());
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        composition.IsReadSurfaceConnected.Returns(true);
        composition.IsCommandSurfaceConnected.Returns(true);
        Services.AddSingleton(composition);

        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.SupportsTrackedLifecycleDispatch.Returns(true);
        gateway.SupportsTrackedSetConfigurationDispatch.Returns(true);
        gateway.SupportsTrackedRemoveConfigurationDispatch.Returns(true);
        gateway.SupportsCommandStatusLookup.Returns(true);
        gateway.AddUserToTenantAsync(Arg.Any<AddUserToTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.ChangeUserRoleAsync(Arg.Any<ChangeUserRole>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.RemoveUserFromTenantAsync(Arg.Any<RemoveUserFromTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.UpdateTenantAsync(Arg.Any<UpdateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.DisableTenantTrackedAsync(Arg.Any<TenantLifecycleCommandRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.SetTenantConfigurationTrackedAsync(Arg.Any<SetTenantConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.RemoveTenantConfigurationTrackedAsync(Arg.Any<RemoveTenantConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Accepted(call.ArgAt<string>(1)));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.PublishFailed, HasVerifiedCommandIdentity: true)));
        Services.AddSingleton(gateway);
        return gateway;
    }

    private static Task<TenantCommandSubmissionResult> Accepted(string messageId)
        => Task.FromResult(TenantCommandSubmissionResult.Accepted(messageId, "correlation-audit-denial"));

    private (IRenderedComponent<IComponent> Cut, Func<TenantCommandLifecycleState> State) RenderFlow(string kind, bool auditReadDenied, bool auditReadAvailable)
    {
        TenantDetail detail = new("tenant.alpha", "Alpha", "Original", TenantStatus.Active,
            [new TenantMember("owner", TenantRole.TenantOwner), new TenantMember("reader", TenantRole.TenantReader)],
            new Dictionary<string, string>(), DateTimeOffset.UtcNow);
        const string version = "tenant-sequence:1";
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            detail.TenantId, TenantStatus.Active, false, ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "enabled")], TenantConfigurationAuthorityState.TenantOwner);
        switch (kind)
        {
            case "add":
                IRenderedComponent<AddTenantMemberFlow> add = RenderScoped<AddTenantMemberFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Detail, detail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
                    .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.ProjectionVersion, version)
                    .Add(x => x.IsAuthorized, true).Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized)));
                return (add, () => add.Instance.Snapshot.State);
            case "change":
                IRenderedComponent<ChangeTenantMemberRoleFlow> change = RenderScoped<ChangeTenantMemberRoleFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Detail, detail).Add(x => x.Member, detail.Members[1])
                    .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
                    .Add(x => x.ProjectionVersion, version).Add(x => x.IsAuthorized, true)
                    .Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized)));
                return (change, () => change.Instance.Snapshot.State);
            case "remove-member":
                IRenderedComponent<RemoveTenantMemberFlow> removeMember = RenderScoped<RemoveTenantMemberFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Detail, detail).Add(x => x.Member, detail.Members[1])
                    .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
                    .Add(x => x.ProjectionVersion, version).Add(x => x.IsAuthorized, true)
                    .Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                    .Add(x => x.AuditProofCapabilityAvailable, true).Add(x => x.TargetPlatformStandingResolved, true));
                return (removeMember, () => removeMember.Instance.Snapshot.State);
            case "metadata":
                IRenderedComponent<EditTenantMetadataFlow> metadata = RenderScoped<EditTenantMetadataFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Detail, detail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
                    .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(x => x.ProjectionVersion, version).Add(x => x.IsAuthorized, true)
                    .Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized)));
                return (metadata, () => metadata.Instance.Snapshot.State);
            case "lifecycle":
                TenantLifecycleAvailability availability = new TenantLifecycleAvailabilityInput(
                    detail.TenantId, detail.Status, ReadModelFreshnessState.Current, TenantDetailSurfaceKind.Ready,
                    IsCommandSurfaceConnected: true, ProjectionVersion: version, TenantLifecycleGovernanceReadiness.Ready,
                    TenantLifecycleAuthorizationReflectionState.Authorized, Lifecycle: ProjectionLifecycleState.Current)
                    .Evaluate(TenantLifecycleOperation.DisableTenant);
                IRenderedComponent<TenantLifecycleCommandFlow> lifecycle = RenderScoped<TenantLifecycleCommandFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Detail, detail).Add(x => x.Availability, availability).Add(x => x.ProjectionVersion, version)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                    .Add(x => x.ProjectionEvidenceProvider, (_, _) => Task.FromResult(TenantCommandProofFixtures.Proof(detail, version))));
                return (lifecycle, () => lifecycle.Instance.Snapshot.State);
            case "set":
                IRenderedComponent<SetTenantConfigurationFlow> set = RenderScoped<SetTenantConfigurationFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Context, context).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
                    .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(x => x.PreviewEvidenceProvider, intent => Task.FromResult(TenantSetConfigurationPreview.Create(
                        intent, TenantStatus.Active, TenantSetConfigurationCurrentState.Different,
                        ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, version, isAuthorized: true))));
                return (set, () => set.Instance.Snapshot.State);
            case "remove-configuration":
                IRenderedComponent<RemoveTenantConfigurationFlow> removeConfiguration = RenderScoped<RemoveTenantConfigurationFlow>(auditReadDenied, auditReadAvailable, p => p
                    .Add(x => x.Context, context).Add(x => x.TargetKey, "billing.mode")
                    .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
                    .Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(x => x.PreviewEvidenceProvider, intent => Task.FromResult(TenantRemoveConfigurationPreview.Create(
                        intent, TenantStatus.Active, TenantRemoveConfigurationCurrentState.Present,
                        ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, version, isAuthorized: true))));
                return (removeConfiguration, () => removeConfiguration.Instance.Snapshot.State);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown command flow.");
        }
    }

    private IRenderedComponent<T> RenderScoped<T>(bool auditReadDenied, bool auditReadAvailable, Action<ComponentParameterCollectionBuilder<T>> parameters)
        where T : class, IComponent
    {
        Action<ComponentParameterCollectionBuilder<CascadingValue<string>>> hostParameters = p => p
            .Add(x => x.Name, "CurrentTenantCallerScope").Add(x => x.Value, "caller-audit-denial")
            .AddChildContent<CascadingValue<string>>(returnUrl => returnUrl.Add(x => x.Name, "DetailAuditReturnUrl").Add(x => x.Value, "/tenants/tenant.alpha")
                .AddChildContent<CascadingValue<bool>>(denied => denied.Add(x => x.Name, "AuditReadDenied").Add(x => x.Value, auditReadDenied)
                    .AddChildContent<CascadingValue<bool>>(available => available.Add(x => x.Name, "AuditReadAvailable").Add(x => x.Value, auditReadAvailable)
                        .AddChildContent(parameters))));
        IRenderedComponent<CascadingValue<string>> host = Render<CascadingValue<string>>(hostParameters);
        _updateAuditAuthority = (denied, available) =>
        {
            auditReadDenied = denied;
            auditReadAvailable = available;
            host.Render(hostParameters);
            return host.FindComponent<T>().Instance;
        };
        return host.FindComponent<T>();
    }

    private static void PrepareForm(string kind, IRenderedComponent<IComponent> cut)
    {
        switch (kind)
        {
            case "add":
                cut.Find("[data-testid='tenants-add-member-user-id']").Change("new-reader");
                FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
                break;
            case "change":
                FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
                break;
            case "remove-member":
                cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader");
                break;
            case "metadata":
                cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
                cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Updated");
                break;
            case "lifecycle":
                cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
                break;
            case "set":
                cut.Find("[data-testid='tenants-config-set-open']").Click();
                cut.Find("[data-testid='tenants-config-set-key-suffix']").Change("mode");
                cut.Find("[data-testid='tenants-config-set-value']").Change("Updated");
                break;
            case "remove-configuration":
                cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown command flow.");
        }
    }

    private static string DispatchName(string kind)
        => kind switch
        {
            "add" => nameof(ITenantCommandGateway.AddUserToTenantAsync),
            "change" => nameof(ITenantCommandGateway.ChangeUserRoleAsync),
            "remove-member" => nameof(ITenantCommandGateway.RemoveUserFromTenantAsync),
            "metadata" => nameof(ITenantCommandGateway.UpdateTenantAsync),
            "lifecycle" => nameof(ITenantCommandGateway.DisableTenantTrackedAsync),
            "set" => nameof(ITenantCommandGateway.SetTenantConfigurationTrackedAsync),
            "remove-configuration" => nameof(ITenantCommandGateway.RemoveTenantConfigurationTrackedAsync),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown command flow."),
        };
}
