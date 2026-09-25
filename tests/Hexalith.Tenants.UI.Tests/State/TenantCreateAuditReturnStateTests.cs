using System.Security.Claims;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCreateAuditReturnStateTests
{
    [Fact]
    public void Pending_create_state_is_discarded_when_the_caller_changes()
    {
        SwitchingAuthenticationStateProvider authentication = new();
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<AuthenticationStateProvider>(authentication)
            .BuildServiceProvider();
        using TenantCreateAuditReturnState state = new(services);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot.Idle()
            .RequestSent(new CreateTenant("tenant.alpha", "Alpha", null), null, true);
        state.Remember(snapshot, "tenant.alpha", "Alpha", null);

        authentication.ChangeCaller();

        state.Take().ShouldBeNull();
    }

    private sealed class SwitchingAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        public void ChangeCaller()
            => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
