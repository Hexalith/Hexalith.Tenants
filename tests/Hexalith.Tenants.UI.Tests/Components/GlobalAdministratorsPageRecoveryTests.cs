using Bunit;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Hexalith.Tenants.UI.Services;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorsPageTests
{
    [Theory]
    [InlineData(true, "confirmed")]
    [InlineData(false, "confirmed")]
    [InlineData(true, "rejected")]
    [InlineData(false, "legacy")]
    [InlineData(false, "caller")]
    [InlineData(true, "permission")]
    [InlineData(true, "no-events")]
    [InlineData(true, "delivery-caller")]
    [InlineData(false, "delivery-caller")]
    [InlineData(true, "missing-caller")]
    [InlineData(false, "missing-caller")]
    [InlineData(true, "missing-delivery-caller")]
    [InlineData(false, "missing-delivery-caller")]
    public async Task RecoveryRetainsOriginalAttemptAfterProjectionChanges(bool grant, string outcome)
    {
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var commands = new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous"),
            new TenantCommandStatusResult(outcome == "rejected" ? CommandStatus.Rejected : CommandStatus.Completed,
                EventCount: outcome == "no-events" ? 0 : 1, HasVerifiedCommandIdentity: true))
        {
            RemoveSubmission = TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous"),
        };
        if (outcome == "legacy") commands.RecoveryResult = TenantCommandStatusResult.UnknownWithKey("Tenants.GlobalAdministrators.Recovery.Unverified");
        var before = ComponentReady("projection-v1", "target-admin", "other-admin");
        var after = grant ? ComponentReady("projection-v2", "target-admin", "other-admin", "candidate")
            : ComponentReady("projection-v2", "other-admin");
        var queries = new StubTenantQueryGateway(before);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(queries);
        Services.AddSingleton<ITenantCommandGateway>(commands);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        if (grant)
        {
            cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("candidate");
            OpenGrantPreview(cut);
            AcknowledgeGrantPreview(cut);
            await cut.Find("[data-testid='tenants-global-admin-grant-confirm']").ClickAsync(new MouseEventArgs());
        }
        else
        {
            OpenRemovePreview(cut);
            AcknowledgeRemovePreview(cut);
            await cut.Find("[data-testid='tenants-global-admin-remove-submit']").ClickAsync(new MouseEventArgs());
        }
        string message = grant ? commands.GrantMessageIds.ShouldHaveSingleItem() : commands.RemoveMessageIds.ShouldHaveSingleItem();
        queries.QueueResponse(Task.FromResult(outcome is "delivery-caller" or "missing-delivery-caller" ? before : after));
        if (outcome is "missing-caller" or "missing-delivery-caller")
            PrivateField<TenantAggregateCommandLease>(cut.Instance, grant ? "_grantAdmissionLease" : "_removeAdmissionLease").RecoveryCallerSubject = null;
        if (outcome is "caller" or "delivery-caller") composition.CallerSubject = "another-authorized-caller";
        if (outcome == "permission") composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        await cut.Find($"[data-testid='tenants-global-admin-{(grant ? "grant" : "remove")}-{(outcome is "delivery-caller" or "missing-delivery-caller" ? "refresh" : "recover")}']").ClickAsync(new MouseEventArgs());
        commands.SetGlobalAdministratorCalls.ShouldBe(grant ? 1 : 0);
        commands.RemoveGlobalAdministratorCalls.ShouldBe(grant ? 0 : 1);
        if (outcome is "caller" or "permission" or "delivery-caller" or "missing-caller" or "missing-delivery-caller") commands.RecoveryMessageIds.ShouldBeEmpty();
        else commands.RecoveryMessageIds.ShouldHaveSingleItem().ShouldBe(message);
        if (outcome is "confirmed" or "rejected")
        {
            cut.WaitForAssertion(() =>
            {
                TenantCommandLifecycleState actual = grant
                    ? PrivateField<GlobalAdministratorGrantCommandSnapshot>(cut.Instance, "_grantSnapshot").State
                    : PrivateField<GlobalAdministratorRemoveCommandSnapshot>(cut.Instance, "_removeSnapshot").State;
                actual.ShouldBe(outcome == "confirmed" ? TenantCommandLifecycleState.Confirmed : TenantCommandLifecycleState.Rejected);
            });
        }
        else
        {
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>()
                .IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task CallerTransitionAfterPrincipalReadBeforeRendererPublicationRejectsRecoveryEvidence()
    {
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var pending = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new StubTenantCommandGateway
        {
            RemoveSubmission = TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous"),
            RecoveryTask = pending.Task,
        };
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(ComponentReady("projection-v1", "target-admin", "other-admin")));
        Services.AddSingleton<ITenantCommandGateway>(commands);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        OpenRemovePreview(cut);
        AcknowledgeRemovePreview(cut);
        await cut.Find("[data-testid='tenants-global-admin-remove-submit']").ClickAsync(new MouseEventArgs());
        TenantAggregateCommandLease lease = PrivateField<TenantAggregateCommandLease>(cut.Instance, "_removeAdmissionLease");
        Task activation = cut.Find("[data-testid='tenants-global-admin-remove-recover']").ClickAsync(new MouseEventArgs());
        await commands.RecoveryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        (Task block, TaskCompletionSource release) = await BlockRendererAsync(cut);
        try
        {
            pending.SetResult(commands.RecoveryResult);
            await composition.RecoveryCallerRechecked.Task.WaitAsync(TimeSpan.FromSeconds(5));
            composition.CallerSubject = "another-authorized-caller";
            typeof(GlobalAdministratorsPage).GetMethod("OnAuthenticationStateChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(cut.Instance, [Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(GlobalAdministratorPrincipal()))]);
        }
        finally
        {
            release.TrySetResult();
            pending.TrySetResult(commands.RecoveryResult);
            await Task.WhenAll(block, activation).WaitAsync(TimeSpan.FromSeconds(5));
        }
        commands.StatusHandles.ShouldBeEmpty();
        lease.Reconciliation.ShouldNotBeNull().CorrelationId.ShouldBeNull();
        lease.Reconciliation.IsSubmissionAmbiguous.ShouldBeTrue();
        commands.RemoveMessageIds.ShouldHaveSingleItem();
    }
    [Theory]
    [InlineData(true, false, "en")]
    [InlineData(false, false, "en")]
    [InlineData(true, true, "en")]
    [InlineData(false, true, "en")]
    [InlineData(true, false, "fr")]
    [InlineData(false, false, "fr")]
    [InlineData(true, true, "fr")]
    [InlineData(false, true, "fr")]
    public async Task InitialDispatchRequiresOriginalCallerAndCanBeReviewedAgain(bool grant, bool throws, string culture)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Services.AddLocalization();
            var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized)
            {
                RecoveryCallerResolver = _ => throws ? throw new InvalidOperationException("caller unavailable") : ValueTask.FromResult<string?>(null),
            };
            var commands = new StubTenantCommandGateway();
            Services.AddSingleton<ITenantsBffComposition>(composition);
            Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(ComponentReady("projection-v1", "target-admin", "other-admin")));
            Services.AddSingleton<ITenantCommandGateway>(commands);
            IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
            async Task ReviewAndSubmitAsync()
            {
                if (grant)
                {
                    cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("candidate");
                    OpenGrantPreview(cut); AcknowledgeGrantPreview(cut);
                    await cut.Find("[data-testid='tenants-global-admin-grant-confirm']").ClickAsync(new MouseEventArgs());
                }
                else
                {
                    OpenRemovePreview(cut); AcknowledgeRemovePreview(cut);
                    await cut.Find("[data-testid='tenants-global-admin-remove-submit']").ClickAsync(new MouseEventArgs());
                }
            }
            await ReviewAndSubmitAsync();
            commands.GrantMessageIds.ShouldBeEmpty(); commands.RemoveMessageIds.ShouldBeEmpty();
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
            cut.Markup.ShouldContain(Services.GetRequiredService<IStringLocalizer<TenantsResources>>()["Tenants.GlobalAdministrators.Recovery.CallerUnavailable"].Value);
            composition.RecoveryCallerResolver = null;
            await ReviewAndSubmitAsync();
            (grant ? commands.GrantMessageIds : commands.RemoveMessageIds).ShouldHaveSingleItem();
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialCallerReadTimesOutOrCancelsOnPageDisposalWithoutDispatch(bool dispose)
    {
        var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized)
        {
            RecoveryCallerResolver = token => { token.Register(() => canceled.TrySetResult()); entered.TrySetResult(); return new(pending.Task); },
        };
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { [TenantUiOperationPolicy.ConfigurationKey] = "00:00:00.100" }).Build());
        var commands = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(ComponentReady("projection-v1", "other-admin")));
        Services.AddSingleton<ITenantCommandGateway>(commands);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("candidate");
        OpenGrantPreview(cut); AcknowledgeGrantPreview(cut);
        Task activation = cut.Find("[data-testid='tenants-global-admin-grant-confirm']").ClickAsync(new MouseEventArgs());
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (dispose) await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
            await activation.WaitAsync(TimeSpan.FromSeconds(5));
            await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            commands.GrantMessageIds.ShouldBeEmpty();
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
        }
        finally { pending.TrySetResult("operator"); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PageLateRecoveryKeepsExactLeaseAcrossRemountAndRejectsAnotherCaller(bool grant, bool callerChanges)
    {
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var pending = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ambiguous = TenantCommandSubmissionResult.Ambiguous("ignored", "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous");
        var commands = new StubTenantCommandGateway(ambiguous, new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true))
        { RemoveSubmission = ambiguous, RecoveryTask = pending.Task };
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(ComponentReady("projection-v1", "target-admin", "other-admin")));
        Services.AddSingleton<ITenantCommandGateway>(commands);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<GlobalAdministratorsPage> first = Render<GlobalAdministratorsPage>();
        if (grant)
        {
            first.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("candidate");
            OpenGrantPreview(first); AcknowledgeGrantPreview(first);
            await first.Find("[data-testid='tenants-global-admin-grant-confirm']").ClickAsync(new MouseEventArgs());
        }
        else
        {
            OpenRemovePreview(first); AcknowledgeRemovePreview(first);
            await first.Find("[data-testid='tenants-global-admin-remove-submit']").ClickAsync(new MouseEventArgs());
        }
        string field = grant ? "_grantAdmissionLease" : "_removeAdmissionLease";
        TenantAggregateCommandLease lease = PrivateField<TenantAggregateCommandLease>(first.Instance, field);
        string message = lease.Reconciliation!.MessageId;
        Task activation = first.Find($"[data-testid='tenants-global-admin-{(grant ? "grant" : "remove")}-recover']").ClickAsync(new MouseEventArgs());
        try
        {
            await commands.RecoveryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await first.InvokeAsync(() => (Task)typeof(GlobalAdministratorsPage).GetMethod(grant ? "RecoverGrantStatusAsync" : "RecoverRemoveStatusAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(first.Instance, null)!);
            commands.RecoveryMessageIds.ShouldHaveSingleItem().ShouldBe(message);
            await first.InvokeAsync(async () => await first.Instance.DisposeAsync());
            if (callerChanges) composition.CallerSubject = "another-authorized-caller";
            IRenderedComponent<GlobalAdministratorsPage> replacement = Render<GlobalAdministratorsPage>();
            pending.SetResult(commands.RecoveryResult);
            await activation.WaitAsync(TimeSpan.FromSeconds(5));
            replacement.WaitForAssertion(() =>
            {
                PrivateField<TenantAggregateCommandLease>(replacement.Instance, field).ShouldBeSameAs(lease);
                lease.Reconciliation!.CorrelationId.ShouldBe(callerChanges ? null : "recovered-correlation");
            });
            lease.Reconciliation!.MessageId.ShouldBe(message);
            lease.RecoveryCallerSubject.ShouldBe("operator");
            (grant ? commands.GrantMessageIds : commands.RemoveMessageIds).ShouldHaveSingleItem().ShouldBe(message);
            if (callerChanges) commands.StatusHandles.ShouldBeEmpty();
        }
        finally { pending.TrySetResult(commands.RecoveryResult); await activation.WaitAsync(TimeSpan.FromSeconds(5)); }
    }

}
