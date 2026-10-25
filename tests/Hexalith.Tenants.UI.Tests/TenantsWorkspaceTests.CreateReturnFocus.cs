using System.Reflection;
using System.Security.Claims;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Theory]
    [InlineData("unchanged", true)]
    [InlineData("view", false)]
    [InlineData("authority", false)]
    [InlineData("disposal", false)]
    public async Task Held_create_return_import_keeps_exact_view_authority_and_lifetime_ownership(string transition, bool applies)
    {
        var authentication = new WorkspaceAcceptanceAuthenticationStateProvider(Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("sub", "operator.alpha"), new Claim("global_admin", "true"),
                new Claim("eventstore:tenant", "system"), new Claim("eventstore:current-tenant", "system")], "test")))));
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        ITenantQueryGateway queries = Substitute.For<ITenantQueryGateway>();
        queries.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current)));
        RegisterPagingFocusServices(queries);
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForAssertion(() => cut.FindComponent<CreateTenantFlow>().Instance.IsAuthorized.ShouldBeTrue());
        IJSRuntime original = Services.GetRequiredService<IJSRuntime>();
        IJSRuntime runtime = Substitute.For<IJSRuntime>();
        IJSObjectReference module = Substitute.For<IJSObjectReference>();
        IJSObjectReference replacementModule = Substitute.For<IJSObjectReference>();
        bool replacementScope = false;
        var held = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int disposals = 0;
        int expectedDisposals = int.MaxValue;
        int ImportCount() => runtime.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "InvokeAsync"
            && call.GetArguments()[0] as string == "import"
            && call.GetArguments()[1] is object?[] arguments && arguments.FirstOrDefault() as string == "./js/tenantsFocus.js");
        async Task<IJSObjectReference> ImportModuleAsync(bool replacement)
        {
            await held.Task.ConfigureAwait(false);
            return replacement ? replacementModule : module;
        }
        runtime.InvokeAsync<IJSObjectReference>(Arg.Any<string>(), Arg.Any<object?[]?>()).Returns(call =>
            call.ArgAt<string>(0) == "import" && call.ArgAt<object?[]?>(1)?.FirstOrDefault() as string == "./js/tenantsFocus.js"
                ? new ValueTask<IJSObjectReference>(ImportModuleAsync(replacementScope))
                : original.InvokeAsync<IJSObjectReference>(call.ArgAt<string>(0), call.ArgAt<object?[]?>(1)));
        foreach (IJSObjectReference importedModule in new[] { module, replacementModule })
        {
            importedModule.InvokeAsync<bool>(Arg.Any<string>(), Arg.Any<object?[]?>()).Returns(new ValueTask<bool>(true));
            importedModule.DisposeAsync().Returns(_ =>
            {
                if (Interlocked.Increment(ref disposals) >= Volatile.Read(ref expectedDisposals)) completed.TrySetResult();
                return ValueTask.CompletedTask;
            });
        }
        typeof(TenantsWorkspace).GetProperty("JS", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cut.Instance, runtime);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        try
        {
            navigation.NavigateTo("/tenants/tenants?sort=name&anchor=tenants-create-lifecycle");
            cut.WaitForAssertion(() => ImportCount().ShouldBeGreaterThan(0));
            int existingImports = ImportCount();
            MethodInfo restore = typeof(TenantsWorkspace).GetMethod("RestoreCreateDetailFocusAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await cut.InvokeAsync(() => (Task)restore.Invoke(cut.Instance, null)!).WaitAsync(TimeSpan.FromSeconds(5));
            ImportCount().ShouldBe(existingImports);
            switch (transition)
            {
                case "view": navigation.NavigateTo("/tenants/workspace-users"); break;
                case "authority":
                    // The still-authorized replacement scope may recover; only the held old owner must retire.
                    replacementScope = true;
                    authentication.Notify(new ClaimsPrincipal());
                    cut.WaitForAssertion(() => ImportCount().ShouldBeGreaterThan(existingImports));
                    break;
                case "disposal": await cut.InvokeAsync(cut.Instance.Dispose); break;
            }
            await cut.InvokeAsync(static () => Task.CompletedTask);
        }
        finally
        {
            Volatile.Write(ref expectedDisposals, ImportCount());
            held.TrySetResult(module);
        }
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        module.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "InvokeAsync"
            && call.GetArguments()[0] as string == "focusElementById").ShouldBe(applies ? 1 : 0);
        module.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "InvokeAsync"
            && call.GetArguments()[0] as string == "consumeReturnMarkers").ShouldBe(applies ? 1 : 0);
        replacementModule.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "InvokeAsync"
            && call.GetArguments()[0] as string == "focusElementById").ShouldBe(transition is "authority" ? 1 : 0);
        replacementModule.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "InvokeAsync"
            && call.GetArguments()[0] as string == "consumeReturnMarkers").ShouldBe(transition is "authority" ? 1 : 0);
    }
}
