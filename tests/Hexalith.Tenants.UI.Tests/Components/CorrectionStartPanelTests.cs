using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class CorrectionStartPanelTests : FluentBunitContext
{
    [Fact]
    public void Panel_renders_original_evidence_current_snapshot_command_and_preview_handoff_without_submission()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        cut.Find("[data-testid='tenants-correction-panel']").GetAttribute("role").ShouldBe("region");
        cut.Find("[data-testid='tenants-correction-original-evidence']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-correction-current-snapshot']").TextContent.ShouldContain("Current tenant projection");
        cut.Find("[data-testid='tenants-correction-command']").TextContent.ShouldContain("Add user to tenant");
        cut.Find("[data-testid='tenants-correction-domain']").TextContent.ShouldContain("Tenants");
        cut.Find("[data-testid='tenants-correction-preview-data']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-correction-preview-handoff']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Markup.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Panel_renders_blocked_global_admin_reason_without_preview_handoff()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(Row("GlobalAdministratorRemoved")));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Global administrator correction commands are not connected");
        cut.Find("[data-testid='tenants-correction-original-evidence']").TextContent.ShouldContain("event-safe-reference");
        cut.FindAll("[data-testid='tenants-correction-preview-handoff']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_close_uses_callback_for_focus_return()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        bool closed = false;

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, TenantCorrectionStartIntent.Evaluate(Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader)))
            .Add(component => component.OnClose, () => closed = true));

        cut.Find("[data-testid='tenants-correction-close']").Click();

        closed.ShouldBeTrue();
    }

    [Fact]
    public void Panel_submits_restore_command_once_and_links_projection_confirmed_corrective_proof()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-corrective", "UserAddedToTenant"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.AddUserRequests.ShouldHaveSingleItem());
        commandGateway.AddUserRequests[0].TenantId.ShouldBe("tenant.alpha");
        commandGateway.AddUserRequests[0].UserId.ShouldBe("target-user");
        commandGateway.AddUserRequests[0].Role.ShouldBe(TenantRole.TenantReader);
        commandGateway.StatusHandles.ShouldHaveSingleItem().ShouldBe(new TenantCommandTrackingHandle("message-safe", "tracking-safe"));
        queryGateway.DetailRequests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        queryGateway.AuditRequests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Find("[data-testid='tenants-correction-proof-link']").GetAttribute("href").ShouldBe("#audit-event-corrective");
        cut.Find("[data-testid='tenants-correction-proof-link']").TextContent.ShouldContain("2026-06-01 10:05:00 UTC");
        cut.Markup.ShouldNotContain("event-original as undone", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
    }

    [Fact]
    public void Panel_change_role_workflow_sends_change_role_command_and_requeries_projection()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-corrective", "UserRoleChanged"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRoleChanged", "userId: target-user; oldRole: TenantContributor; newRole: TenantReader"),
            currentRole: TenantRole.TenantContributor,
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor))));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.ChangeRoleRequests.ShouldHaveSingleItem());
        commandGateway.ChangeRoleRequests[0].TenantId.ShouldBe("tenant.alpha");
        commandGateway.ChangeRoleRequests[0].UserId.ShouldBe("target-user");
        commandGateway.ChangeRoleRequests[0].NewRole.ShouldBe(TenantRole.TenantReader);
        commandGateway.AddUserRequests.ShouldBeEmpty();
        queryGateway.DetailRequests.ShouldHaveSingleItem();
        cut.Find("[data-testid='tenants-correction-proof-link']").TextContent.ShouldContain("Corrective evidence linked");
    }

    [Fact]
    public void Panel_blocks_stale_restore_when_current_projection_has_different_role()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor))));

        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-safe-message']").TextContent.ShouldContain("role-change correction");
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        commandGateway.AddUserRequests.ShouldBeEmpty();
    }

    [Fact]
    public void Panel_prevents_duplicate_submission_while_command_is_in_flight()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway commandGateway = new() { AddUserResultTask = pendingSubmission.Task };
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-corrective", "UserAddedToTenant"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => commandGateway.AddUserRequests.Count.ShouldBe(1));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        commandGateway.AddUserRequests.Count.ShouldBe(1);
        pendingSubmission.SetResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        cut.WaitForAssertion(() => commandGateway.StatusHandles.Count.ShouldBe(1));
        commandGateway.AddUserRequests.Count.ShouldBe(1);
    }

    private static TenantCorrectionStartContext Context(
        TenantAuditRow row,
        TenantRole? intendedRole = null,
        TenantRole? currentRole = null)
        => new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            CurrentRole: currentRole,
            IntendedRole: intendedRole);

    private static TenantDetail Detail(params TenantMember[] members)
        => new(
            "tenant.alpha",
            "Tenant Alpha",
            null,
            TenantStatus.Active,
            members,
            new Dictionary<string, string>(StringComparer.Ordinal),
            DateTimeOffset.Parse("2026-06-01T09:00:00Z", CultureInfo.InvariantCulture));

    private static TenantAuditSnapshot Audit(string eventReference, string eventType)
        => TenantAuditSnapshot.Ready(
            [Row(eventType, eventReference: eventReference)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"audit-etag\"",
            freshness: TenantFreshnessState.Current,
            request: new TenantAuditRequest("tenant.alpha"));

    private static TenantAuditRow Row(
        string eventType,
        string referenceContext = "",
        string eventReference = "event-safe-reference")
        => new(
            eventReference,
            eventType,
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? AuditEventCategory.Administrative : AuditEventCategory.Access,
            "actor-user",
            eventReference == "event-corrective"
                ? DateTimeOffset.Parse("2026-06-01T10:05:00Z", CultureInfo.InvariantCulture)
                : DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "admin-user" : "target-user",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "global-administrators" : "tenant.alpha",
            eventType,
            string.IsNullOrWhiteSpace(referenceContext)
                ? eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "userId: admin-user" : "userId: target-user"
                : referenceContext,
            TenantFreshnessState.Current);

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public List<AddUserToTenantCommandRequest> AddUserRequests { get; } = [];

        public List<ChangeUserRoleCommandRequest> ChangeRoleRequests { get; } = [];

        public List<TenantCommandTrackingHandle> StatusHandles { get; } = [];

        public Task<TenantCommandSubmissionResult>? AddUserResultTask { get; init; }

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenantCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            AddUserRequests.Add(request);
            return AddUserResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRoleCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            ChangeRoleRequests.Add(request);
            return Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
        {
            StatusHandles.Add(handle);
            return Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenantCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfigurationCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubTenantQueryGateway(TenantDetail detail, TenantAuditSnapshot audit) : ITenantQueryGateway
    {
        public List<TenantDetailRequest> DetailRequests { get; } = [];

        public List<TenantAuditRequest> AuditRequests { get; } = [];

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            DetailRequests.Add(request);
            return Task.FromResult(TenantDetailSnapshot.Ready(detail, "\"detail-etag\"", TenantFreshnessState.Current));
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            AuditRequests.Add(request);
            return Task.FromResult(audit);
        }

        public Task<TenantListSnapshot> ListTenantsAsync(
            TenantListRequest request,
            TenantListSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Correction.Action.PreviewHandoff"] = "Continue to correction preview",
            ["Tenants.Correction.Audit.AuditPending"] = "Corrective audit evidence is pending.",
            ["Tenants.Correction.Confirm.Cancel"] = "Cancel",
            ["Tenants.Correction.Confirm.Refresh"] = "Refresh status",
            ["Tenants.Correction.Confirm.Submit"] = "Submit corrective command",
            ["Tenants.Correction.Close"] = "Close correction start",
            ["Tenants.Correction.Command.AddUserToTenant"] = "Add user to tenant",
            ["Tenants.Correction.Command.ChangeUserRole"] = "Change user role",
            ["Tenants.Correction.Command.SetGlobalAdministrator"] = "Set global administrator",
            ["Tenants.Correction.Domain.GlobalAdministrators"] = "Global administrators",
            ["Tenants.Correction.Domain.Tenants"] = "Tenants",
            ["Tenants.Correction.Field.Command"] = "Intended command",
            ["Tenants.Correction.Field.CurrentSnapshot"] = "Current projection snapshot",
            ["Tenants.Correction.Field.Domain"] = "Command domain",
            ["Tenants.Correction.Field.OriginalEvidence"] = "Original evidence",
            ["Tenants.Correction.Field.PreviewData"] = "Required preview data",
            ["Tenants.Correction.PreviewInput.currentProjectionSnapshot"] = "Current projection snapshot",
            ["Tenants.Correction.PreviewInput.currentRole"] = "Current role",
            ["Tenants.Correction.PreviewInput.domain"] = "Domain",
            ["Tenants.Correction.PreviewInput.aggregateId"] = "Aggregate",
            ["Tenants.Correction.PreviewInput.intendedRole"] = "Intended role",
            ["Tenants.Correction.PreviewInput.originalAuditReference"] = "Original audit reference",
            ["Tenants.Correction.PreviewInput.tenantId"] = "Tenant",
            ["Tenants.Correction.PreviewInput.userId"] = "User",
            ["Tenants.Correction.Lifecycle.Title"] = "Correction lifecycle",
            ["Tenants.Correction.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Correction.Preview.AuditExpectation.Text"] = "Audit evidence is expected after projection confirmation.",
            ["Tenants.Correction.Preview.Consequence.Membership"] = "A new membership event may be appended.",
            ["Tenants.Correction.Preview.Consequence.RoleChange"] = "A new role-change event may be appended.",
            ["Tenants.Correction.Preview.Consequence.Unsupported"] = "No corrective command will be submitted without support.",
            ["Tenants.Correction.Preview.Consequences"] = "Known consequences",
            ["Tenants.Correction.Preview.CurrentProjectionReady"] = "Current tenant projection is available for {0}.",
            ["Tenants.Correction.Preview.CurrentProjectionUnavailable"] = "Current tenant projection is unavailable.",
            ["Tenants.Correction.Preview.CurrentRole"] = "Current role",
            ["Tenants.Correction.Preview.IntendedRole"] = "Intended role",
            ["Tenants.Correction.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Correction.Preview.RecoveryPath.Text"] = "Refresh status or inspect audit.",
            ["Tenants.Correction.Preview.Unknown.HistoricalRole"] = "Historical role evidence can be stale.",
            ["Tenants.Correction.Preview.Unknown.SignalR"] = "SignalR nudges do not prove success.",
            ["Tenants.Correction.Preview.Unknown.Unsupported"] = "Global administrator support is unavailable.",
            ["Tenants.Correction.Preview.Unknowns"] = "Known unknowns",
            ["Tenants.Correction.Proof.Link"] = "Corrective evidence linked at {0}.",
            ["Tenants.Correction.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.Correction.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.Correction.Role.TenantReader"] = "Tenant reader",
            ["Tenants.Correction.RoleChoice.Label"] = "Choose intended role",
            ["Tenants.Correction.RoleChoice.Placeholder"] = "Select role",
            ["Tenants.Correction.State.Accepted"] = "Command accepted; projection confirmation is pending.",
            ["Tenants.Correction.State.AlreadyApplied"] = "The intended state is already present.",
            ["Tenants.Correction.State.Confirmed"] = "Projection confirmed the correction.",
            ["Tenants.Correction.State.Degraded"] = "Correction status is degraded.",
            ["Tenants.Correction.State.Failed"] = "Correction command failed.",
            ["Tenants.Correction.State.Previewed"] = "Preview is ready for deliberate confirmation.",
            ["Tenants.Correction.State.ProjectionPending"] = "Projection confirmation is pending.",
            ["Tenants.Correction.State.Rejected"] = "Correction command was rejected.",
            ["Tenants.Correction.State.RequestSent"] = "Correction command was sent.",
            ["Tenants.Correction.State.UnableToVerify"] = "Correction cannot be verified from current evidence.",
            ["Tenants.Correction.Unavailable.CommandSupportUnavailable"] = "Tenant correction command support is unavailable.",
            ["Tenants.Correction.Title"] = "Start correction",
            ["Tenants.Correction.Unavailable.AlreadyApplied"] = "The current projection already shows the intended state.",
            ["Tenants.Correction.Unavailable.CurrentRoleConflict"] = "Current projection shows this user with a different role; start a role-change correction instead.",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
        };
    }
}
