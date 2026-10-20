using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants.Lifecycle;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    /// <summary>Same-tenant loads refresh authority even when only return context changes.</summary>
    [Theory]
    [InlineData(false, TenantLifecycleAuthorizationReflectionState.MissingPermission)]
    [InlineData(true, TenantLifecycleAuthorizationReflectionState.MissingPermission)]
    [InlineData(false, TenantLifecycleAuthorizationReflectionState.Indeterminate)]
    [InlineData(true, TenantLifecycleAuthorizationReflectionState.Indeterminate)]
    [InlineData(false, TenantLifecycleAuthorizationReflectionState.Authorized)]
    [InlineData(true, TenantLifecycleAuthorizationReflectionState.Authorized)]
    public async Task SameTenantParameterLoadRefreshesLifecycleAuthority(
        bool changeReturnContext,
        TenantLifecycleAuthorizationReflectionState nextAuthority)
    {
        TenantLifecycleAuthorizationReflectionState initialAuthority
            = nextAuthority == TenantLifecycleAuthorizationReflectionState.Authorized
                ? TenantLifecycleAuthorizationReflectionState.MissingPermission
                : TenantLifecycleAuthorizationReflectionState.Authorized;
        var composition = new StubTenantsBffComposition(initialAuthority);
        int detailReads = 0;
        RegisterServices(_ =>
        {
            detailReads++;
            return Task.FromResult(ReadyWithSafeConfiguration(Detail("tenant.alpha"), projectionVersion: "tenant-sequence:41"));
        }, composition);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants/tenant.alpha");
        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-lifecycle-disable']")
            .GetAttribute("aria-disabled")
            .ShouldBe(initialAuthority == TenantLifecycleAuthorizationReflectionState.Authorized ? "false" : "true"));
        int previousResolutions = composition.AsyncResolutionCount;
        int previousReads = detailReads;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.Reflection = nextAuthority;
        composition.ResolutionGate = completion;

        if (changeReturnContext)
        {
            navigation.NavigateTo(navigation.GetUriWithQueryParameter("returnUrl", "/tenants?search=alpha"));
        }
        else
        {
            cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.alpha"));
        }
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.WaitForAssertion(() =>
        {
            composition.AsyncResolutionCount.ShouldBe(previousResolutions + 1);
            detailReads.ShouldBe(previousReads + 1);
            cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("aria-disabled").ShouldBe("true");
        });
        completion.SetResult();
        cut.WaitForAssertion(() =>
        {
            cut.FindComponent<TenantLifecycleActionAvailability>().Instance.AuthorizationReflection.ShouldBe(nextAuthority);
            cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("aria-disabled")
                .ShouldBe(nextAuthority == TenantLifecycleAuthorizationReflectionState.Authorized ? "false" : "true");
        });

        // A subsequent load can recover from denied/indeterminate authority or consume a new revocation.
        composition.Reflection = nextAuthority == TenantLifecycleAuthorizationReflectionState.Authorized
            ? TenantLifecycleAuthorizationReflectionState.MissingPermission
            : TenantLifecycleAuthorizationReflectionState.Authorized;
        cut.Render(parameters => parameters.Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForAssertion(() =>
        {
            composition.AsyncResolutionCount.ShouldBe(previousResolutions + 2);
            cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("aria-disabled")
                .ShouldBe(nextAuthority == TenantLifecycleAuthorizationReflectionState.Authorized ? "true" : "false");
        });
    }

}
