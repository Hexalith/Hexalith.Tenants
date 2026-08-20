using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AddTenantMemberFlowTests : FluentBunitContext
{
    [Fact]
    public void Add_member_flow_renders_stable_selectors_and_assignable_roles_only()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

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
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);
        TenantDetail originalDetail = Detail("tenant.alpha");

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, originalDetail)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(originalDetail)));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("User/CaseSensitive.01");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantContributor));
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
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        string liveProjectionVersion = "v1";
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => liveProjectionVersion)
            .Add(p => p.ProjectionEvidenceProvider, request =>
            {
                liveProjectionVersion = "v2";
                return Task.FromResult<TenantDetail?>(Detail(
                    request.TenantId,
                    [
                        new TenantMember("owner-user", TenantRole.TenantOwner),
                        new TenantMember(request.UserId, request.Role),
                    ]));
            }));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-add-member-audit']").TextContent.ShouldContain("Audit evidence pending");
        cut.Markup.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public void Signalr_nudge_requeries_status_without_confirming_from_notification_alone()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => "v2")
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(Detail(
                "tenant.alpha",
                [
                    new TenantMember("owner-user", TenantRole.TenantOwner),
                    new TenantMember("literal-user", TenantRole.TenantReader),
                ]))));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));
        // Baseline was captured via provider as v2 at submit; keep evidence matching to prove Received cannot confirm.
        int statusCallsBeforeNudge = gateway.StatusCallCount;

        cut.InvokeAsync(() => cut.Instance.HandleAuthoritativeRefreshNudgeAsync());

        cut.WaitForAssertion(() => gateway.StatusCallCount.ShouldBe(statusCallsBeforeNudge + 1));
        // Status remains Received → Accepted; matching evidence alone must not confirm without Completed.
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void In_flight_retry_with_tracking_reuses_status_lookup_and_does_not_dispatch_again()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1"));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(1));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        int statusCallsAfterSubmit = gateway.StatusCallCount;

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(1));
        gateway.StatusCallCount.ShouldBeGreaterThan(statusCallsAfterSubmit);
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [Fact]
    public async Task SignalR_nudge_during_unresolved_submission_keeps_request_and_activity_in_flight()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> submission = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway gateway = new()
        {
            AddMemberAsync = _ => submission.Task,
        };
        RegisterServices(gateway);
        List<bool> activity = [];

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.CommandActivityLease, isActive =>
            {
                activity.Add(isActive);
                return Task.FromResult(true);
            }));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent));

        await cut.InvokeAsync(() => cut.Instance.HandleAuthoritativeRefreshNudgeAsync());

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        gateway.StatusCallCount.ShouldBe(0);
        activity.ShouldBe([true]);

        submission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
        cut.WaitForAssertion(() => activity.ShouldBe([true, false]));
    }

    [Fact]
    public async Task Lost_tracking_refresh_maps_to_unable_to_verify_without_second_dispatch()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1"));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));

        System.Reflection.FieldInfo snapshotField = typeof(AddTenantMemberFlow)
            .GetField("_snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        TenantAddMemberCommandSnapshot snapshot = (TenantAddMemberCommandSnapshot)snapshotField.GetValue(cut.Instance)!;
        snapshotField.SetValue(cut.Instance, snapshot with { MessageId = null, CorrelationId = null });

        await cut.InvokeAsync(async () =>
        {
            await cut.Instance.HandleAuthoritativeRefreshNudgeAsync().ConfigureAwait(false);
        });
        cut.Render();

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        gateway.AddMemberCallCount.ShouldBe(1);
        TenantCommandFlowGuard.RetainsCommandActivity(cut.Instance.Snapshot.State).ShouldBeFalse();
        cut.Find("[data-testid='tenants-add-member-continue-read-only']");
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
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("owner-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantOwner));
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
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-add-member-validation']").TextContent.ShouldContain("User id is required");
        gateway.AddMemberCallCount.ShouldBe(0);

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
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
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, isCommandSurfaceAvailable));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
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
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
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
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    public void Add_member_fails_closed_when_truth_or_authorization_is_not_eligible(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        string expectedReason)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        string reason = cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent;
        reason.ShouldContain(expectedReason, Case.Insensitive);
        reason.ShouldNotContain("not authorized", Case.Insensitive);
        cut.Find("[data-testid='tenants-add-member-lifecycle']").GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Add_member_true_authorization_failure_still_renders_permission_reason()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsAuthorized, false));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent
            .ShouldContain("not authorized", Case.Insensitive);
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

        public Func<AddUserToTenant, Task<TenantCommandSubmissionResult>>? AddMemberAsync { get; init; }

        public AddUserToTenant? LastAddMemberRequest { get; private set; }

        public string? LastAddMemberMessageId { get; private set; }

        public int AddMemberCallCount { get; private set; }

        public int StatusCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
        {
            AddMemberCallCount++;
            LastAddMemberRequest = request;
            LastAddMemberMessageId = messageId;
            return AddMemberAsync is null ? Task.FromResult(Submission) : AddMemberAsync(request);
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            StatusCallCount++;
            return Task.FromResult(Status);
        }
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
            ["Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance"] = "Member projection already matched without provenance that this attempt advanced it. Refresh status or continue read-only.",
            ["Tenants.AddMember.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit evidence is delayed; retry status lookup or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Audit evidence support is missing; continue read-only or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit evidence is pending; wait, refresh status, or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit evidence is unavailable; continue read-only, retry status lookup, or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only or escalate using only the visible support-safe reference.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Audit proof cannot be verified right now. Continue read-only, retry status lookup, or escalate without including raw diagnostics, tokens, payloads, or personal data.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
