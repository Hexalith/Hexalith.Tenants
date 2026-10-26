using System.Reflection;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorsPageTests
{
    [Fact]
    public async Task WorkerResetCannotClearCursorHistoryWhileRendererOwnsIt()
    {
        IRenderedComponent<GlobalAdministratorsPage> cut = RenderTeardownPage();
        Stack<string?> history = PrivateField<Stack<string?>>(cut.Instance, "_cursorHistory");
        await cut.InvokeAsync(() => history.Push("owned-cursor"));
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        Task blocker = Task.Run(() => Renderer.Dispatcher.InvokeAsync(() =>
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
        }));
        Task? reset = null;
        Exception? workerFailure = null;
        using ManualResetEventSlim prefixFinished = new();
        Thread worker = new(() =>
        {
            try { reset = cut.Instance.ResetPagingAsync(); }
            catch (Exception exception) { workerFailure = exception; }
            finally { prefixFinished.Set(); }
        });
        try
        {
            entered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            worker.Start();
            prefixFinished.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            workerFailure.ShouldBeNull();
            PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(1);
            history.ShouldHaveSingleItem().ShouldBe("owned-cursor");
        }
        finally
        {
            release.Set();
            await blocker.WaitAsync(TimeSpan.FromSeconds(5));
            if (worker.ThreadState != System.Threading.ThreadState.Unstarted) worker.Join(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            if (reset is not null) await reset.WaitAsync(TimeSpan.FromSeconds(5));
        }
        history.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificationFinalizationContainsDisposalAfterAliveCheckBeforeDispatch()
    {
        IRenderedComponent<GlobalAdministratorsPage> cut = RenderTeardownPage();
        FieldInfo dispatcherField = Renderer.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(Dispatcher));
        object original = dispatcherField.GetValue(Renderer)!;
        Dispatcher failing = Substitute.For<Dispatcher>();
        failing.InvokeAsync(Arg.Any<Action>()).Returns(_ =>
        {
            typeof(GlobalAdministratorsPage).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(cut.Instance, true);
            return Task.FromException(new ObjectDisposedException("renderer"));
        });
        try
        {
            dispatcherField.SetValue(Renderer, failing);
            await Task.Run(() => (Task)typeof(GlobalAdministratorsPage)
                .GetMethod("FinalizeNotificationRefreshAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(cut.Instance, null)!).WaitAsync(TimeSpan.FromSeconds(5));
            failing.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(Dispatcher.InvokeAsync)).ShouldBe(1);
        }
        finally
        {
            dispatcherField.SetValue(Renderer, original);
            typeof(GlobalAdministratorsPage).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(cut.Instance, false);
        }
    }

    private IRenderedComponent<GlobalAdministratorsPage> RenderTeardownPage()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(ComponentReady("projection-v1", "admin")));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        return Render<GlobalAdministratorsPage>();
    }
}
