using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class CreateTenantFlowTests : BunitContext
{
    [Fact]
    public void Create_flow_renders_stable_selectors_and_fail_closed_reason()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.IsCommandSurfaceAvailable, false));

        cut.Find("[data-testid='tenants-create-flow']");
        cut.Find("[data-testid='tenants-create-tenant-id']");
        cut.Find("[data-testid='tenants-create-name']");
        cut.Find("[data-testid='tenants-create-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-create-unavailable-reason']").TextContent.ShouldContain("unavailable");
        cut.Find("[data-testid='tenants-create-lifecycle']");
        cut.Find("[data-testid='tenants-create-state']");
        cut.Find("[data-testid='tenants-create-refresh']");
    }

    [Fact]
    public void Submit_preserves_literal_tenant_id_and_does_not_confirm_without_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantSummary?>(null)));

        cut.Find("[data-testid='tenants-create-tenant-id']").Input("Tenant.Mixed-01");
        cut.Find("[data-testid='tenants-create-name']").Input("Mixed Tenant");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.LastRequest.ShouldNotBeNull().TenantId.ShouldBe("Tenant.Mixed-01"));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Find("[data-testid='tenants-create-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Projection_evidence_confirms_without_exposing_internal_correlation_id()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<TenantSummary?>(new TenantSummary(tenantId, "Mixed Tenant", TenantStatus.Active))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Input("Tenant.Mixed-01");
        cut.Find("[data-testid='tenants-create-name']").Input("Mixed Tenant");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-create-audit']").TextContent.ShouldContain("Audit evidence pending");
        cut.Markup.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public void Rejection_uses_assertive_live_region_and_safe_text()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Rejected(
                "A tenant with this id already exists. Refresh the list or open the existing tenant if it is visible.",
                "TenantAlreadyExists"),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        cut.Find("[data-testid='tenants-create-tenant-id']").Input("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Input("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldContain("already exists");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
    }

    [Fact]
    public void Submit_without_required_fields_shows_validation_and_does_not_call_gateway()
    {
        StubTenantCommandGateway gateway = new();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-create-validation']").TextContent.ShouldContain("Tenant id is required");
        cut.Find("[data-testid='tenants-create-validation']").GetAttribute("role").ShouldBe("alert");
        gateway.CreateTenantCallCount.ShouldBe(0);
        gateway.LastRequest.ShouldBeNull();
    }

    [Fact]
    public void Rejected_status_remains_non_success_even_when_projection_contains_tenant()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(
                CommandStatus.Rejected,
                "A tenant with this id already exists. Refresh the list or open the existing tenant if it is visible.",
                "TenantAlreadyExists"),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<TenantSummary?>(new TenantSummary(tenantId, "Alpha", TenantStatus.Active))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Input("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Input("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldContain("already exists");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Publish_failed_status_renders_degraded_without_success_styling()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.PublishFailed, "The command was accepted, but publication could not be verified."),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        cut.Find("[data-testid='tenants-create-tenant-id']").Input("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Input("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Degraded));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-create-state']").TextContent.ShouldContain("degraded");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Unavailable_status_lookup_renders_unable_to_verify_with_recovery_action()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = TenantCommandStatusResult.Unknown("Command status could not be verified."),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        cut.Find("[data-testid='tenants-create-tenant-id']").Input("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Input("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-create-refresh']").GetAttribute("disabled").ShouldBeNull();
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Lifecycle_region_is_focusable_so_fail_closed_focus_stays_recoverable()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.IsCommandSurfaceAvailable, false));

        cut.Find("[data-testid='tenants-create-lifecycle']").GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Validation_describedby_only_references_an_existing_validation_element()
    {
        StubTenantCommandGateway gateway = new();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        // Before any validation message there is no validation element, so nothing may point at it.
        cut.Find("[data-testid='tenants-create-name']").GetAttribute("aria-describedby").ShouldBeNull();
        cut.Find("[data-testid='tenants-create-tenant-id']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-create-tenant-id-help");

        cut.Find("form").Submit();

        // After a validation failure the element exists and the describedby references resolve.
        cut.Find("[data-testid='tenants-create-validation']");
        cut.Find("[data-testid='tenants-create-name']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-create-validation");
        cut.Find("[data-testid='tenants-create-tenant-id']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-create-tenant-id-help tenants-create-validation");
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public CreateTenantCommandRequest? LastRequest { get; private set; }

        public int CreateTenantCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenantCommandRequest request, CancellationToken cancellationToken = default)
        {
            CreateTenantCallCount++;
            LastRequest = request;
            return Task.FromResult(Submission);
        }

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Create.Title"] = "Create tenant",
            ["Tenants.Create.Description"] = "Submit a tenant creation command and wait for projection confirmation.",
            ["Tenants.Create.TenantId.Label"] = "Tenant id",
            ["Tenants.Create.TenantId.Help"] = "Use the exact caller-supplied tenant id.",
            ["Tenants.Create.Name.Label"] = "Name",
            ["Tenants.Create.Description.Label"] = "Description",
            ["Tenants.Create.Submit"] = "Create tenant",
            ["Tenants.Create.Refresh"] = "Refresh status",
            ["Tenants.Create.Lifecycle.Title"] = "Command lifecycle",
            ["Tenants.Create.Validation.TenantIdRequired"] = "Tenant id is required.",
            ["Tenants.Create.Validation.NameRequired"] = "Name is required.",
            ["Tenants.Create.Unavailable.Authorization"] = "You are not authorized to create tenants.",
            ["Tenants.Create.Unavailable.Freshness"] = "Refresh tenant data before submitting a command.",
            ["Tenants.Create.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Create.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Create.State.Idle"] = "No command submitted.",
            ["Tenants.Create.State.RequestSent"] = "Request sent.",
            ["Tenants.Create.State.Accepted"] = "Accepted by EventStore; waiting for processing.",
            ["Tenants.Create.State.ProjectionPending"] = "Projection pending; tenant is not confirmed visible yet.",
            ["Tenants.Create.State.Confirmed"] = "Projection confirmed the tenant exists.",
            ["Tenants.Create.State.Rejected"] = "Command rejected.",
            ["Tenants.Create.State.Failed"] = "Command submission failed.",
            ["Tenants.Create.State.Degraded"] = "Command result is degraded and needs review.",
            ["Tenants.Create.State.UnableToVerify"] = "Unable to verify command result.",
            ["Tenants.Create.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Create.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Create.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Create.Audit.MissingSupport"] = "Audit support is missing for this flow.",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
