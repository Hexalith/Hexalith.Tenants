using Bunit;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorCorrectionPanelTests
{
    [Theory]
    [InlineData(false, "confirmed")]
    [InlineData(true, "confirmed")]
    [InlineData(false, "legacy")]
    [InlineData(true, "no-events")]
    [InlineData(false, "wrong-sequence")]
    [InlineData(true, "rejected")]
    [InlineData(false, "caller")]
    [InlineData(false, "delivery-caller")]
    [InlineData(true, "delivery-caller")]
    [InlineData(false, "missing-caller")]
    [InlineData(true, "missing-caller")]
    [InlineData(false, "missing-delivery-caller")]
    [InlineData(true, "missing-delivery-caller")]
    public async Task RetainedCorrectionRecoversStatusAfterTargetPresenceChanges(bool revoke, string outcome)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var commands = new StubTenantCommandGateway
        {
            SetResultTask = Task.FromResult(TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous")),
            RemoveResultTask = Task.FromResult(TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous")),
            Status = new(outcome == "rejected" ? CommandStatus.Rejected : CommandStatus.Completed,
                EventCount: outcome == "no-events" ? 0 : 1, HasVerifiedCommandIdentity: true)
            { CommittedEventSequence = outcome == "wrong-sequence" ? 1 : 2 },
        };
        if (outcome == "legacy") commands.RecoveryResult = TenantCommandStatusResult.UnknownWithKey("Tenants.GlobalAdministrators.Recovery.Unverified");
        Services.AddSingleton<ITenantCommandGateway>(commands);
        var before = revoke ? Projection("admin-user", "other-admin") : Projection("other-admin");
        var after = (revoke ? Projection("other-admin") : Projection("admin-user", "other-admin")) with { ProjectionVersion = "tenant-sequence:2" };
        bool changed = false;
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, revoke ? RevokeIntent() : RestoreIntent())
            .Add(component => component.CurrentProjection, before)
            .Add(component => component.ProjectionRefreshProvider, () => Task.FromResult<Hexalith.Tenants.UI.State.GlobalAdministrators.GlobalAdministratorsSnapshot?>(changed ? after : before)));
        await cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new MouseEventArgs());
        string message = commands.TrackedMessageIds.ShouldHaveSingleItem();
        changed = outcome is not ("delivery-caller" or "missing-delivery-caller");
        if (outcome is "missing-caller" or "missing-delivery-caller")
            ((TenantAggregateCommandLease)typeof(GlobalAdministratorCorrectionPanel).GetField("_admissionLease", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance)!).RecoveryCallerSubject = null;
        if (outcome is "caller" or "delivery-caller") ((StubTenantsBffComposition)Services.GetRequiredService<ITenantsBffComposition>()).RecoveryCaller = "another-caller";
        await cut.Find($"[data-testid='tenants-correction-{(outcome is "delivery-caller" or "missing-delivery-caller" ? "refresh" : "recover")}']").ClickAsync(new MouseEventArgs());
        commands.TrackedMessageIds.ShouldHaveSingleItem().ShouldBe(message);
        if (outcome is "caller" or "delivery-caller" or "missing-caller" or "missing-delivery-caller") commands.RecoveryMessageIds.ShouldBeEmpty();
        else commands.RecoveryMessageIds.ShouldHaveSingleItem().ShouldBe(message);
        if (outcome is "confirmed" or "rejected")
        {
            cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(
                outcome == "confirmed" ? TenantCommandLifecycleState.Confirmed : TenantCommandLifecycleState.Rejected));
        }
        else cut.Instance.Snapshot!.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateRecoverySurvivesRemountAndDuplicateActivationWithoutRedispatch(bool revoke)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var pending = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new StubTenantCommandGateway
        {
            SetResultTask = Task.FromResult(TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous")),
            RemoveResultTask = Task.FromResult(TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous")),
            RecoveryTask = pending.Task,
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        var before = revoke ? Projection("admin-user", "other-admin") : Projection("other-admin");
        var after = (revoke ? Projection("other-admin") : Projection("admin-user", "other-admin")) with { ProjectionVersion = "tenant-sequence:2" };
        bool changed = false;
        IRenderedComponent<GlobalAdministratorCorrectionPanel> RenderPanel() => Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, revoke ? RevokeIntent() : RestoreIntent())
            .Add(component => component.CurrentProjection, before)
            .Add(component => component.ProjectionRefreshProvider, () => Task.FromResult<Hexalith.Tenants.UI.State.GlobalAdministrators.GlobalAdministratorsSnapshot?>(changed ? after : before)));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> first = RenderPanel();
        await first.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new MouseEventArgs());
        string message = commands.TrackedMessageIds.ShouldHaveSingleItem();
        changed = true;
        Task activation = first.Find("[data-testid='tenants-correction-recover']").ClickAsync(new MouseEventArgs());
        try
        {
            await commands.RecoveryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await first.InvokeAsync(() => (Task)typeof(GlobalAdministratorCorrectionPanel)
                .GetMethod("RecoverStatusAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(first.Instance, null)!);
            commands.RecoveryMessageIds.ShouldHaveSingleItem().ShouldBe(message);
            await first.InvokeAsync(() => first.Instance.Dispose());
            IRenderedComponent<GlobalAdministratorCorrectionPanel> replacement = RenderPanel();
            pending.SetResult(commands.RecoveryResult);
            await activation.WaitAsync(TimeSpan.FromSeconds(5));
            replacement.WaitForAssertion(() => replacement.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
            replacement.Instance.Snapshot!.MessageId.ShouldBe(message);
            commands.TrackedMessageIds.ShouldHaveSingleItem().ShouldBe(message);
            commands.RecoveryMessageIds.ShouldHaveSingleItem();
        }
        finally
        {
            pending.TrySetResult(commands.RecoveryResult);
            await activation.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
    [Theory]
    [InlineData(false, false, "en")]
    [InlineData(true, false, "en")]
    [InlineData(false, true, "en")]
    [InlineData(true, true, "en")]
    [InlineData(false, false, "fr")]
    [InlineData(true, false, "fr")]
    [InlineData(false, true, "fr")]
    [InlineData(true, true, "fr")]
    public async Task InitialCorrectionRequiresCallerAndRetainsResubmittablePreview(bool revoke, bool throws, string culture)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Services.AddLocalization();
            var composition = new StubTenantsBffComposition();
            Services.AddSingleton<ITenantsBffComposition>(composition);
            composition.RecoveryCallerResolver = _ => throws ? throw new InvalidOperationException("unavailable") : ValueTask.FromResult<string?>(null);
            var commands = new StubTenantCommandGateway { Status = new(CommandStatus.Received, HasVerifiedCommandIdentity: true) };
            Services.AddSingleton<ITenantCommandGateway>(commands);
            var before = revoke ? Projection("admin-user", "other-admin") : Projection("other-admin");
            IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
                .Add(component => component.Intent, revoke ? RevokeIntent() : RestoreIntent())
                .Add(component => component.CurrentProjection, before)
                .Add(component => component.ProjectionRefreshProvider, () => Task.FromResult<GlobalAdministratorsSnapshot?>(before)));
            await cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new MouseEventArgs());
            commands.TrackedMessageIds.ShouldBeEmpty();
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
            cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
            cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldBe(
                Services.GetRequiredService<IStringLocalizer<TenantsResources>>()["Tenants.GlobalAdministrators.Recovery.CallerUnavailable"].Value);
            composition.RecoveryCallerResolver = null;
            await cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new MouseEventArgs());
            commands.TrackedMessageIds.ShouldHaveSingleItem();
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialCorrectionCallerReadTimesOutOrCancelsAndReleasesAdmission(bool dispose)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { [TenantUiOperationPolicy.ConfigurationKey] = "00:00:00.100" }).Build());
        var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var composition = new StubTenantsBffComposition();
        Services.AddSingleton<ITenantsBffComposition>(composition);
        composition.RecoveryCallerResolver = token =>
        { token.Register(() => canceled.TrySetResult()); entered.TrySetResult(); return new(pending.Task); };
        var commands = new StubTenantCommandGateway(); Services.AddSingleton<ITenantCommandGateway>(commands);
        var before = Projection("other-admin");
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent()).Add(component => component.CurrentProjection, before)
            .Add(component => component.ProjectionRefreshProvider, () => Task.FromResult<GlobalAdministratorsSnapshot?>(before)));
        Task activation = cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new MouseEventArgs());
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (dispose) await cut.InvokeAsync(() => cut.Instance.Dispose());
            await activation.WaitAsync(TimeSpan.FromSeconds(5)); await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            commands.TrackedMessageIds.ShouldBeEmpty();
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
        }
        finally { pending.TrySetResult("operator"); }
    }

    [Fact]
    public async Task CorrectionCallerTransitionQueuedBeforeRendererPublicationRejectsExactLateResult()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var pending = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new StubTenantCommandGateway
        {
            SetResultTask = Task.FromResult(TenantCommandSubmissionResult.Ambiguous("ignored",
            "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous")),
            RecoveryTask = pending.Task
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        var before = Projection("other-admin");
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent()).Add(component => component.CurrentProjection, before)
            .Add(component => component.ProjectionRefreshProvider, () => Task.FromResult<GlobalAdministratorsSnapshot?>(before)));
        await cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new MouseEventArgs());
        TenantAggregateCommandLease lease = (TenantAggregateCommandLease)typeof(GlobalAdministratorCorrectionPanel)
            .GetField("_admissionLease", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance)!;
        string message = lease.Reconciliation!.MessageId;
        Task activation = cut.Find("[data-testid='tenants-correction-recover']").ClickAsync(new MouseEventArgs());
        await commands.RecoveryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        (Task block, TaskCompletionSource release) = await BlockRendererAsync(cut);
        var composition = (StubTenantsBffComposition)Services.GetRequiredService<ITenantsBffComposition>();
        try
        {
            pending.SetResult(commands.RecoveryResult);
            await composition.RecoveryCallerRechecked.Task.WaitAsync(TimeSpan.FromSeconds(5));
            composition.RecoveryCaller = "another-authorized-caller";
            typeof(GlobalAdministratorCorrectionPanel).GetMethod("OnAuthenticationStateChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(cut.Instance, [Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                    new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())))]);
        }
        finally { release.TrySetResult(); pending.TrySetResult(commands.RecoveryResult); await Task.WhenAll(block, activation).WaitAsync(TimeSpan.FromSeconds(5)); }
        lease.Reconciliation!.MessageId.ShouldBe(message); lease.Reconciliation.CorrelationId.ShouldBeNull();
        lease.RecoveryCallerSubject.ShouldBe("operator"); commands.StatusHandles.ShouldBeEmpty();
        commands.TrackedMessageIds.ShouldHaveSingleItem().ShouldBe(message);
    }

}
