using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        if (transition == "removed")
        {
            TenantDetail updated = detail with { Members = detail.Members.Where(member => member.UserId != "reader-user").ToArray() };
            cut.Render(p => p.Add(x => x.Detail, updated).Add(x => x.Members, MemberSnapshot(updated)));
        }
        else if (transition == "denied")
        {
            cut.Render(p => p.Add(x => x.IsRemoveMemberAuthorized, false).Add(x => x.IsChangeRoleAuthorized, false));
        }
        string cancel = action == "remove" ? "tenants-remove-member-cancel" : "tenants-change-role-cancel";
        await cut.Find($"[data-testid='{cancel}']").ClickAsync(new MouseEventArgs());
        string expected = transition == "unchanged" ? launcherReference : CapturedElementReferenceId(cut.Instance, "_headingElement");
        cut.WaitForAssertion(() => FocusedElementIds().Last().ShouldBe(expected,
            $"action={action}; transition={transition}; {MemberFocusCallFacts(launcherReference, CapturedElementReferenceId(cut.Instance, "_headingElement"))}"));
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
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", invocation =>
            invocation.Arguments[0] is ElementReference reference && reference.Id == launcher.Element.Id)
            .SetException(new JSException("Detached element"));
        await cut.Find("[data-testid='tenants-remove-member-cancel']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => FocusedElementIds().Last().ShouldBe(CapturedElementReferenceId(cut.Instance, "_headingElement"),
            MemberFocusCallFacts(launcher.Element.Id, CapturedElementReferenceId(cut.Instance, "_headingElement"))));
    }

    private string MemberFocusCallFacts(string launcher, string heading)
        => string.Join("; ", JSInterop.Invocations
            .Where(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .Select(invocation => invocation.Identifier + ":" +
                (invocation.Arguments.Count > 0 && invocation.Arguments[0] is ElementReference reference
                    ? reference.Id == launcher ? "launcher" : reference.Id == heading ? "heading" : "other-element"
                    : "non-element/" + (invocation.Arguments.Count == 0 ? "no-argument" : invocation.Arguments[0]?.GetType().Name ?? "null"))));

    private IRenderedComponent<MemberAccessReview> RenderMemberFocusReview(
        TenantDetail detail,
        bool auditPending = false,
        bool auditAvailable = true)
        => Render<MemberAccessReview>(p => p
            .Add(x => x.IsRemoveMemberAuthorized, true).Add(x => x.IsChangeRoleAuthorized, true)
            .Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(x => x.AuditProofCapabilityAvailable, auditAvailable)
            .Add(x => x.AuditReadPending, auditPending)
            .Add(x => x.Detail, detail)
            .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
            .Add(x => x.Lifecycle, ProjectionLifecycleState.Current).Add(x => x.ProjectionVersion, "tenant-sequence:1")
            .Add(x => x.Members, MemberSnapshot(detail)));
}
