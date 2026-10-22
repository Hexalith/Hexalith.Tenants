using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorsPageTests
{
    /// <summary>Qualifies the final renderer refusal independently of later render-time availability repair.</summary>
    [Fact]
    public async Task InnerGrantRedispatchRefusalSurfacesRecoveryWithoutAnEventRender()
    {
        var commands = new StubTenantCommandGateway(TenantCommandSubmissionResult.Ambiguous(
            "ignored", "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous"));
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            ComponentReady("projection-v1", "admin-a")) { RepeatLastResponse = true });
        Services.AddSingleton<ITenantCommandGateway>(commands);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-admin");
        PreviewAcknowledgeAndConfirmGrant(cut);
        var before = PrivateField<GlobalAdministratorGrantCommandSnapshot>(cut.Instance, "_grantSnapshot");
        var lease = PrivateField<TenantAggregateCommandLease>(cut.Instance, "_grantAdmissionLease");
        before.IsDeliveryRetryWithdrawn.ShouldBeFalse();
        lease.IsDispatchMarked.ShouldBeTrue();
        lease.IsReconciliationDispatchInFlight.ShouldBeFalse();

        int capabilityReads = 0;
        commands.TrackedDispatchSupportProvider = () => ++capabilityReads == 1;
        await cut.InvokeAsync(async () =>
        {
            // Model the successful outer capability read followed by loss at the final renderer boundary.
            // Invoke without an event callback: its automatic rerender would mask a silent inner return
            // by letting OnAfterRenderAsync repair the withdrawal bit.
            typeof(GlobalAdministratorsPage).GetMethod("AreGrantDispatchCapabilitiesCurrent",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cut.Instance, null).ShouldBe(true);
            var dispatch = (Task)typeof(GlobalAdministratorsPage).GetMethod("DispatchGrantAsync",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cut.Instance,
                [true, before, before.PreviewEvidence!, lease, before.MessageId!,
                    PrivateField<long>(cut.Instance, "_grantPreviewGeneration"),
                    PrivateField<long>(cut.Instance, "_grantMutationGeneration"), CancellationToken.None])!;
            await dispatch;
        });

        capabilityReads.ShouldBeGreaterThanOrEqualTo(2);
        var after = PrivateField<GlobalAdministratorGrantCommandSnapshot>(cut.Instance, "_grantSnapshot");
        after.IsDeliveryRetryWithdrawn.ShouldBeTrue();
        after.IsSubmissionAmbiguous.ShouldBeTrue();
        after.MessageId.ShouldBe(before.MessageId);
        after.PreviewEvidence.ShouldBeSameAs(before.PreviewEvidence);
        after.SafeMessageKey.ShouldBe(before.SafeMessageKey);
        after.SafeRecoveryKey.ShouldBe(before.SafeRecoveryKey);
        after.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        PrivateField<TenantAggregateCommandLease>(cut.Instance, "_grantAdmissionLease").ShouldBeSameAs(lease);
        lease.IsDispatchMarked.ShouldBeTrue();
        lease.IsReconciliationDispatchInFlight.ShouldBeFalse();
        commands.GrantMessageIds.ShouldBe([before.MessageId!]);
        commands.StatusHandles.ShouldBeEmpty();
        cut.Find("[data-testid='tenants-global-admin-grant-live-region']")
            .GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-global-admin-grant-delivery-retry-withdrawn-recovery']")
            .TextContent.ShouldContain("same retained command", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admin-grant-refresh']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-global-admin-grant-delivery-retry-withdrawn-reason']")
            .TextContent.ShouldContain("temporarily withdrawn", Case.Insensitive);
    }

    /// <summary>Reconstructs presentation-only withdrawal from live prerequisites after renderer replacement.</summary>
    /// <param name="culture">The shipped culture used for visible recovery copy.</param>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task ReplacementGrantRecomputesWithdrawnRetryAndRearmsTheSameCommand(string culture)
    {
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
        var localizer = new CultureBoundTenantsLocalizer(
            factory.Create(typeof(TenantsResources)), CultureInfo.GetCultureInfo(culture));
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var commands = new StubTenantCommandGateway(TenantCommandSubmissionResult.Ambiguous(
            "ignored", "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous"));
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            ComponentReady("projection-v1", "admin-a")) { RepeatLastResponse = true });
        Services.AddSingleton<ITenantCommandGateway>(commands);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(localizer);
        IRenderedComponent<CascadingValue<bool>> host = Render<CascadingValue<bool>>(parameters => parameters
            .Add(p => p.Name, "GrantPageMounted").Add(p => p.Value, true)
            .AddChildContent<GlobalAdministratorsPage>());
        IRenderedComponent<GlobalAdministratorsPage> first = host.FindComponent<GlobalAdministratorsPage>();
        GlobalAdministratorsPage firstInstance = first.Instance;
        first.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-admin");
        PreviewAcknowledgeAndConfirmGrant(first);
        string messageId = commands.GrantMessageIds.ShouldHaveSingleItem();
        var originalRequest = commands.Requests.ShouldHaveSingleItem();
        commands.StatusHandles.ShouldBeEmpty();
        var lease = PrivateField<TenantAggregateCommandLease>(first.Instance, "_grantAdmissionLease");
        composition.IsGlobalAdministratorGrantPreviewReady = false;
        first.Render();
        first.WaitForAssertion(() => PrivateField<GlobalAdministratorGrantCommandSnapshot>(first.Instance,
            "_grantSnapshot").IsDeliveryRetryWithdrawn.ShouldBeTrue());
        await host.InvokeAsync(() => host.Render(parameters => parameters
            .Add(p => p.Name, "GrantPageMounted").Add(p => p.Value, true)
            .Add(p => p.ChildContent, (RenderFragment)(_ => { }))));
        host.FindComponents<GlobalAdministratorsPage>().ShouldBeEmpty();
        commands.StatusHandles.ShouldBeEmpty();
        host.Render(parameters => parameters.Add(p => p.Name, "GrantPageMounted").Add(p => p.Value, true)
            .AddChildContent<GlobalAdministratorsPage>());
        IRenderedComponent<GlobalAdministratorsPage> replacement = host.FindComponent<GlobalAdministratorsPage>();
        replacement.Instance.ShouldNotBeSameAs(firstInstance);
        replacement.WaitForAssertion(() =>
        {
            var snapshot = PrivateField<GlobalAdministratorGrantCommandSnapshot>(replacement.Instance, "_grantSnapshot");
            snapshot.IsDeliveryRetryWithdrawn.ShouldBeTrue();
            snapshot.IsSubmissionAmbiguous.ShouldBeTrue();
            snapshot.MessageId.ShouldBe(messageId);
            snapshot.Intent!.UserId.ShouldBe("target-admin");
            snapshot.BaselineProjectionVersion.ShouldBe("projection-v1");
            var retry = replacement.Find("[data-testid='tenants-global-admin-grant-refresh']");
            retry.HasAttribute("disabled").ShouldBeTrue();
            retry.TextContent.ShouldContain(localizer["Tenants.GlobalAdministrators.Grant.DeliveryRetry.Withdrawn.Label"].Value);
            foreach (string suffix in new[] { "Reason", "Recovery" })
            {
                string id = $"tenants-global-admin-grant-delivery-retry-withdrawn-{suffix.ToLowerInvariant()}";
                retry.GetAttribute("aria-describedby")!.Split(' ').ShouldContain(id);
                replacement.Find($"#{id}").TextContent.ShouldBe(
                    localizer[$"Tenants.GlobalAdministrators.Grant.DeliveryRetry.Withdrawn.{suffix}"].Value);
            }
        });
        PrivateField<TenantAggregateCommandLease>(replacement.Instance, "_grantAdmissionLease").ShouldBeSameAs(lease);
        commands.GrantMessageIds.ShouldBe([messageId]);
        commands.StatusHandles.ShouldBeEmpty();
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>()
            .IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();

        composition.IsGlobalAdministratorGrantPreviewReady = true;
        replacement.Render();
        replacement.WaitForAssertion(() =>
        {
            replacement.Find("[data-testid='tenants-global-admin-grant-refresh']").HasAttribute("disabled").ShouldBeFalse();
            replacement.FindAll("[data-testid='tenants-global-admin-grant-delivery-retry-withdrawn-reason']").ShouldBeEmpty();
            PrivateField<GlobalAdministratorGrantCommandSnapshot>(replacement.Instance, "_grantSnapshot")
                .IsDeliveryRetryWithdrawn.ShouldBeFalse();
        });
        commands.GrantMessageIds.ShouldBe([messageId]);
        commands.StatusHandles.ShouldBeEmpty();
        await replacement.Find("[data-testid='tenants-global-admin-grant-refresh']").ClickAsync(new MouseEventArgs());
        commands.GrantMessageIds.ShouldBe([messageId, messageId]);
        commands.Requests.ShouldBe([originalRequest, originalRequest]);
        commands.StatusHandles.ShouldBeEmpty();
    }
}
