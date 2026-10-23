using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    /// <summary>Dismissal restores the actual launcher, or the stable heading when its row or permission disappeared.</summary>
    /// <param name="action">The member command whose launcher opened the flow.</param>
    /// <param name="transition">The authoritative change before dismissal.</param>
    [Theory]
    [InlineData("remove", "unchanged")]
    [InlineData("remove", "removed")]
    [InlineData("remove", "denied")]
    [InlineData("role", "unchanged")]
    [InlineData("role", "removed")]
    [InlineData("role", "denied")]
    public async Task MemberDismissalFocusUsesCurrentInteractiveLauncherOrHeading(string action, string transition)
    {
        RegisterComponentServices();
        TenantDetail detail = Detail("tenant.alpha");
        IRenderedComponent<MemberAccessReview> cut = RenderMemberFocusReview(detail);
        string selector = action == "remove" ? "tenants-remove-member-open" : "tenants-change-role-open";
        FluentButton launcher = cut.FindComponents<FluentButton>().Last(button =>
            button.Instance.AdditionalAttributes?.GetValueOrDefault("data-testid")?.ToString() == selector).Instance;
        string launcherReference = launcher.Element.Id;
        cut.FindAll($"[data-testid='{selector}']")[1].Click();
        EventCallback close = action == "remove"
            ? cut.FindComponent<RemoveTenantMemberFlow>().Instance.OnCloseRequested
            : cut.FindComponent<ChangeTenantMemberRoleFlow>().Instance.OnCloseRequested;
        if (transition == "removed")
        {
            TenantDetail updated = detail with { Members = detail.Members.Where(member => member.UserId != "reader-user").ToArray() };
            cut.Render(p => p.Add(x => x.Detail, updated).Add(x => x.Members, MemberSnapshot(updated)));
        }
        else if (transition == "denied")
        {
            cut.Render(p => p.Add(x => x.IsRemoveMemberAuthorized, false).Add(x => x.IsChangeRoleAuthorized, false));
        }
        await cut.InvokeAsync(() => close.InvokeAsync());
        string expected = transition == "unchanged" ? launcherReference : CapturedElementReferenceId(cut.Instance, "_headingElement");
        cut.WaitForAssertion(() => FocusedElementIds().Last().ShouldBe(expected));
        cut.Find("#tenants-member-heading").GetAttribute("tabindex").ShouldBe("-1");
        cut.FindAll("span.tenant-members__action-focus").ShouldBeEmpty();

        string? directory = Environment.GetEnvironmentVariable("TENANTS_COMMAND_FORM_FIXTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory) && action == "remove")
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"member-focus-{transition}.html"), cut.Markup);
        }
    }

    /// <summary>A detached browser launcher cannot fault dismissal or suppress heading recovery.</summary>
    [Fact]
    public async Task MemberDismissalFocusRecoversFromDetachedLauncher()
    {
        RegisterComponentServices();
        IRenderedComponent<MemberAccessReview> cut = RenderMemberFocusReview(Detail("tenant.alpha"));
        FluentButton launcher = cut.FindComponents<FluentButton>().Last(button =>
            button.Instance.AdditionalAttributes?.GetValueOrDefault("data-testid")?.ToString() == "tenants-remove-member-open").Instance;
        cut.FindAll("[data-testid='tenants-remove-member-open']")[1].Click();
        EventCallback close = cut.FindComponent<RemoveTenantMemberFlow>().Instance.OnCloseRequested;
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", invocation =>
            invocation.Arguments[0] is ElementReference reference && reference.Id == launcher.Element.Id)
            .SetException(new JSException("Detached element"));
        await cut.InvokeAsync(() => close.InvokeAsync());
        cut.WaitForAssertion(() => FocusedElementIds().Last().ShouldBe(CapturedElementReferenceId(cut.Instance, "_headingElement")));
    }

    private IRenderedComponent<MemberAccessReview> RenderMemberFocusReview(TenantDetail detail)
        => Render<MemberAccessReview>(p => p
            .Add(x => x.IsRemoveMemberAuthorized, true).Add(x => x.IsChangeRoleAuthorized, true)
            .Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(x => x.AuditProofCapabilityAvailable, true).Add(x => x.Detail, detail)
            .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
            .Add(x => x.Lifecycle, ProjectionLifecycleState.Current).Add(x => x.ProjectionVersion, "tenant-sequence:1")
            .Add(x => x.Members, MemberSnapshot(detail)));
}
