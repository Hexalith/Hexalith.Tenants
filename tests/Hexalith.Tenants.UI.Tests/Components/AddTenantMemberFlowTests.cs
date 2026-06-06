using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AddTenantMemberFlowTests : BunitContext
{
    [Fact]
    public void Add_member_flow_renders_stable_selectors_and_assignable_roles_only()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-flow']");
        cut.Find("[data-testid='tenants-add-member-user-id']");
        cut.Find("[data-testid='tenants-add-member-role']");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-add-member-lifecycle']");
        cut.Find("[data-testid='tenants-add-member-state']");
        cut.Find("[data-testid='tenants-add-member-audit']");
        cut.Find("[data-testid='tenants-add-member-refresh']");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldContain("Tenant contributor");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldNotContain("Unknown");
        cut.Markup.ShouldNotContain("invite", Case.Insensitive);
        cut.Markup.ShouldNotContain("email", Case.Insensitive);
        cut.Markup.ShouldNotContain("Users navigation", Case.Insensitive);
    }

    [Fact]
    public void Submit_preserves_literal_user_id_and_does_not_confirm_without_member_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        RegisterServices(gateway);
        TenantDetail originalDetail = Detail("tenant.alpha");

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, originalDetail)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(originalDetail)));

        cut.Find("[data-testid='tenants-add-member-user-id']").Input("User/CaseSensitive.01");
        cut.Find("[data-testid='tenants-add-member-role']").Change(nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.LastAddMemberRequest.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01"));
        gateway.LastAddMemberRequest.ShouldNotBeNull().Role.ShouldBe(TenantRole.TenantContributor);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Instance.Snapshot.LastConfirmedMemberProjection.ShouldBeNull();
        cut.Find("[data-testid='tenants-add-member-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public void Projection_evidence_confirms_requested_member_role_without_exposing_internal_correlation_id()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(
                request.TenantId,
                [
                    new TenantMember("owner-user", TenantRole.TenantOwner),
                    new TenantMember(request.UserId, request.Role),
                ]))));

        cut.Find("[data-testid='tenants-add-member-user-id']").Input("literal-user");
        cut.Find("[data-testid='tenants-add-member-role']").Change(nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-add-member-audit']").TextContent.ShouldContain("Audit evidence pending");
        cut.Markup.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public void Already_member_rejection_remains_rejected_without_success_copy()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Rejected(
                "This user is already a member of the tenant. Refresh the member table before trying another action.",
                "UserAlreadyInTenant"),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Input("owner-user");
        cut.Find("[data-testid='tenants-add-member-role']").Change(nameof(TenantRole.TenantOwner));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-add-member-safe-message']").TextContent.ShouldContain("already a member");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        gateway.LastAddMemberRequest.ShouldNotBeNull().UserId.ShouldBe("owner-user");
    }

    [Fact]
    public void Validation_requires_user_id_and_explicit_assignable_role_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-add-member-validation']").TextContent.ShouldContain("User id is required");
        gateway.AddMemberCallCount.ShouldBe(0);

        cut.Find("[data-testid='tenants-add-member-user-id']").Input("literal-user");
        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-add-member-validation']").TextContent.ShouldContain("Select TenantOwner");
        gateway.AddMemberCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, TenantStatus.Active, "Tenant command support is unavailable")]
    [InlineData(true, TenantStatus.Disabled, "lifecycle state does not allow")]
    [InlineData(true, TenantStatus.Unknown, "lifecycle state does not allow")]
    public void Add_member_fails_closed_when_command_surface_or_tenant_lifecycle_is_unavailable(
        bool isCommandSurfaceAvailable,
        TenantStatus tenantStatus,
        string expectedReason)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha") with { Status = tenantStatus })
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, isCommandSurfaceAvailable));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.Find("[data-testid='tenants-add-member-user-id']").Input("literal-user");
        cut.Find("[data-testid='tenants-add-member-role']").Change(nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        gateway.AddMemberCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-add-member-state']").TextContent.ShouldContain("Unable to verify");
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
    }

    [Fact]
    public void Duplicate_submit_while_add_member_command_is_in_flight_is_blocked_before_gateway_submission()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new();
        StubTenantCommandGateway gateway = new()
        {
            AddMemberAsync = _ => pendingSubmission.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Input("literal-user");
        cut.Find("[data-testid='tenants-add-member-role']").Change(nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(1));

        cut.Find("form").Submit();

        gateway.AddMemberCallCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent.ShouldContain("already in progress");
        cut.Find("[data-testid='tenants-add-member-state']").TextContent.ShouldContain("Unable to verify");
        gateway.LastAddMemberRequest.ShouldNotBeNull().UserId.ShouldBe("literal-user");
        cut.Instance.Snapshot.LastConfirmedMemberProjection.ShouldBeNull();

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, TenantFreshnessState.Stale, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Ready, TenantFreshnessState.Unknown, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Degraded, TenantFreshnessState.Unknown, "not authorized")]
    public void Add_member_fails_closed_when_truth_or_authorization_is_not_eligible(
        TenantDetailSurfaceKind surfaceKind,
        TenantFreshnessState freshness,
        string expectedReason)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.Find("[data-testid='tenants-add-member-lifecycle']").GetAttribute("tabindex").ShouldBe("-1");
    }

    private void RegisterServices(StubTenantCommandGateway gateway)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
    }

    private static TenantDetail Detail(string tenantId)
        => Detail(
            tenantId,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);

    private static TenantDetail Detail(string tenantId, IReadOnlyList<TenantMember> members)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            members,
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public Func<AddUserToTenantCommandRequest, Task<TenantCommandSubmissionResult>>? AddMemberAsync { get; init; }

        public AddUserToTenantCommandRequest? LastAddMemberRequest { get; private set; }

        public int AddMemberCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenantCommandRequest request, CancellationToken cancellationToken = default)
        {
            AddMemberCallCount++;
            LastAddMemberRequest = request;
            return AddMemberAsync is null ? Task.FromResult(Submission) : AddMemberAsync(request);
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfigurationCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.AddMember.Title"] = "Add tenant member",
            ["Tenants.AddMember.Description"] = "Add a literal user id to tenant {0}. Current visible owner count is {1}.",
            ["Tenants.AddMember.UserId.Label"] = "User id",
            ["Tenants.AddMember.UserId.Help"] = "Use the exact caller-supplied user id.",
            ["Tenants.AddMember.Role.Label"] = "Tenant role",
            ["Tenants.AddMember.Role.Placeholder"] = "Select a role",
            ["Tenants.AddMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.AddMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.AddMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.AddMember.Submit"] = "Add member",
            ["Tenants.AddMember.Refresh"] = "Refresh status",
            ["Tenants.AddMember.Lifecycle.Title"] = "Add member command lifecycle",
            ["Tenants.AddMember.Validation.UserIdRequired"] = "User id is required.",
            ["Tenants.AddMember.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before adding a member.",
            ["Tenants.AddMember.Unavailable.Authorization"] = "You are not authorized to add members to this tenant.",
            ["Tenants.AddMember.Unavailable.Freshness"] = "Refresh current tenant detail before adding a member.",
            ["Tenants.AddMember.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow adding members.",
            ["Tenants.AddMember.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.AddMember.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.AddMember.State.Idle"] = "No add-member command submitted.",
            ["Tenants.AddMember.State.RequestSent"] = "Add-member request sent.",
            ["Tenants.AddMember.State.Accepted"] = "Accepted by EventStore; waiting for member processing.",
            ["Tenants.AddMember.State.ProjectionPending"] = "Projection pending; the member role is not confirmed visible yet.",
            ["Tenants.AddMember.State.Confirmed"] = "Projection confirmed the user is a tenant member with the requested role.",
            ["Tenants.AddMember.State.Rejected"] = "Add-member command rejected.",
            ["Tenants.AddMember.State.Failed"] = "Add-member command submission failed.",
            ["Tenants.AddMember.State.Degraded"] = "Add-member command result is degraded and needs review.",
            ["Tenants.AddMember.State.UnableToVerify"] = "Unable to verify the add-member command result.",
            ["Tenants.AddMember.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.AddMember.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.AddMember.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.AddMember.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope.",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
