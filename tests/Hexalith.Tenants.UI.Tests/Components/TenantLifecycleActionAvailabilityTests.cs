using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Lifecycle;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantLifecycleActionAvailabilityTests : FluentBunitContext
{
    [Fact]
    public void Lifecycle_availability_renders_stable_selectors_visible_reasons_and_disabled_actions()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Indeterminate)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-actions']");
        cut.Find("[data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-freshness']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-lifecycle-governance-gate']").TextContent.ShouldContain("Unresolved");
        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.FindAll("[data-testid='tenants-lifecycle-unavailable-reason']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-lifecycle-unavailable-reason']")
            .ShouldAllBe(static reason => reason.GetAttribute("tabindex") == "0");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("<form", Case.Insensitive);
        cut.Markup.ShouldNotContain("type=\"submit\"", Case.Insensitive);
        // Assert against visible text: Fluent v5 badge tokens (color="success", --colorStatusSuccessForeground)
        // live in attributes/styles and would false-trigger a raw-markup "Success" guard.
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Governance_blocking_keeps_lifecycle_command_unavailable_with_high_impact_reason()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("high-impact flow not ready");
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("platform gate");
    }

    [Fact]
    public void Disabled_tenant_keeps_disabled_projection_truth_and_blocks_enable_without_optimistic_transition()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.disabled")
            .Add(component => component.CurrentStatus, TenantStatus.Disabled)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Disabled");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Disabled");
        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("#tenants-lifecycle-enable-reason").TextContent.ShouldContain("high-impact flow not ready");
        cut.Find("#tenants-lifecycle-enable-reason").TextContent.ShouldContain("platform gate");
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("TenantLifecycleStateAlreadySet");
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("current projection already shows Disabled");
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldNotContain("current projection already shows Active");
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Same_state_action_names_TenantLifecycleStateAlreadySet_as_safe_domain_outcome()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("TenantLifecycleStateAlreadySet");
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("current projection already shows Active");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Narrow_safety_context_keeps_available_direction_unavailable_with_visible_mobile_reason()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.IsNarrowSafetyContext, true));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("viewport cannot preserve");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-freshness']").TextContent.ShouldContain("Current");
    }

    [Fact]
    public void Lifecycle_component_uses_existing_command_gateway_and_does_not_submit_from_availability_model()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string component = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleActionAvailability.razor"));
        string flow = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleCommandFlow.razor"));
        string commandGateway = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "Gateways",
            "ITenantCommandGateway.cs"));

        component.ShouldNotContain("SubmitCommand");
        flow.ShouldContain("ITenantCommandGateway");
        flow.ShouldContain("EnableTenantAsync");
        flow.ShouldContain("DisableTenantAsync");
        commandGateway.ShouldContain("EnableTenantAsync");
        commandGateway.ShouldContain("DisableTenantAsync");
    }

    [Fact]
    public void Ready_authorized_lifecycle_flow_requires_confirmation_and_confirms_only_after_projection_evidence()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
            Status = new TenantCommandStatusResult(Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed, EventCount: 1),
        };
        List<bool> activity = [];
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(request.TenantId, TenantStatus.Disabled))));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();

        cut.Find("[data-testid='tenants-lifecycle-preview']");
        cut.Find("[data-testid='tenants-lifecycle-focus-start']");
        cut.Find("[data-testid='tenants-lifecycle-focus-end']");
        cut.FindAll("[data-testid='tenants-lifecycle-preview-item']").Count.ShouldBe(10);
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("wrong");
        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-lifecycle-validation']").TextContent.ShouldContain("tenant.alpha");
        gateway.DisableSubmissions.ShouldBe(0);

        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.LastDisableRequest.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        gateway.DisableSubmissions.ShouldBe(1);
        activity.ShouldBe([true, false]);
        cut.Find("[data-testid='tenants-lifecycle-confirmed-status']").TextContent.ShouldContain("Disabled");
        cut.Markup.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Fact]
    public void Cancel_and_escape_close_lifecycle_preview_without_gateway_submission()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
        };
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-preview']");
        cut.Find("[data-testid='tenants-lifecycle-cancel']").Click();

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldBeNull();

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-command-flow']").KeyDown("Escape");

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Markup.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Fact]
    public void Command_surface_unavailable_disables_lifecycle_action_without_gateway_call()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
        };
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, false)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("already in flight or unavailable");
        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();

        cut.FindAll("[data-testid='tenants-lifecycle-command-flow']").ShouldBeEmpty();
        gateway.DisableSubmissions.ShouldBe(0);
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Fact]
    public void Lifecycle_command_activity_lock_is_held_until_projection_confirms()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"),
            Status = new TenantCommandStatusResult(Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed, EventCount: 1),
        };
        TenantStatus projectionStatus = TenantStatus.Active;
        List<bool> activity = [];
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleCommandFlow> cut = Render<TenantLifecycleCommandFlow>(parameters => parameters
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.Availability, AvailableLifecycle(TenantStatus.Active, TenantLifecycleOperation.DisableTenant))
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active))
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(request.TenantId, projectionStatus))));

        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        activity.ShouldBe([true]);
        cut.Instance.Snapshot.LastConfirmedStatus.ShouldBe(TenantStatus.Active);

        projectionStatus = TenantStatus.Disabled;
        cut.Find("[data-testid='tenants-lifecycle-refresh']").Click();

        cut.WaitForAssertion(() =>
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        activity.ShouldBe([true, false]);
        cut.Instance.Snapshot.LastConfirmedStatus.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public void Gateway_unavailable_status_refresh_releases_lifecycle_command_activity_lock()
    {
        RegisterServices();
        List<bool> activity = [];
        TenantDetail detail = Detail("tenant.alpha", TenantStatus.Active);
        TenantLifecycleAvailability availability = AvailableLifecycle(TenantStatus.Active, TenantLifecycleOperation.DisableTenant);

        IRenderedComponent<TenantLifecycleCommandFlow> cut = Render<TenantLifecycleCommandFlow>(parameters => parameters
            .Add(component => component.Detail, detail)
            .Add(component => component.Availability, availability)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active)));

        TenantLifecycleCommandSnapshot tracked = TenantLifecycleCommandSnapshot
            .Idle(detail)
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), detail)
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-life", "correlation-life"));
        SetPrivateField(cut.Instance, "_snapshot", tracked);
        SetPrivateField(cut.Instance, "_hasRaisedCommandActivity", true);
        cut.Render(parameters => parameters
            .Add(component => component.Detail, detail)
            .Add(component => component.Availability, availability)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.OnProjectionRefreshRequested, () => Task.CompletedTask)
            .Add(component => component.OnCommandActivityChanged, active => activity.Add(active)));

        cut.Find("[data-testid='tenants-lifecycle-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        activity.ShouldBe([false]);
    }

    [Fact]
    public void Lifecycle_rejection_displays_safe_non_success_state_without_projection_success_or_raw_details()
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Rejected("The tenant lifecycle already matches the requested state.", "TenantLifecycleStateAlreadySet"),
        };
        RegisterServices(gateway);

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.Detail, Detail("tenant.alpha", TenantStatus.Active))
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.IsCommandSurfaceAvailable, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(request.TenantId, TenantStatus.Disabled))));

        cut.Find("[data-testid='tenants-lifecycle-disable']").Click();
        cut.Find("[data-testid='tenants-lifecycle-confirmation']").Change("tenant.alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.FindComponent<TenantLifecycleCommandFlow>().Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Rejected");
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-safe-message']").TextContent.ShouldContain("already matches");
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-confirmed-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-command-flow'] [data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("token", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
        // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    private void RegisterServices(ITenantCommandGateway? gateway = null)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        if (gateway is not null)
        {
            Services.AddSingleton(gateway);
        }
    }

    private static TenantDetail Detail(string tenantId, TenantStatus status)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            status,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private static TenantLifecycleAvailability AvailableLifecycle(TenantStatus currentStatus, TenantLifecycleOperation operation)
        => new TenantLifecycleAvailabilityInput(
                "tenant.alpha",
                currentStatus,
                ReadModelFreshnessState.Current,
                TenantDetailSurfaceKind.Ready,
                IsCommandSurfaceConnected: true,
                TenantLifecycleGovernanceReadiness.Ready,
                TenantLifecycleAuthorizationReflectionState.Authorized)
            .Evaluate(operation);

    private static void SetPrivateField<TComponent, TValue>(
        TComponent component,
        string fieldName,
        TValue value)
        where TComponent : class
    {
        FieldInfo field = typeof(TComponent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(TComponent).FullName, fieldName);
        field.SetValue(component, value);
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Tenant command status is unavailable.");

        public TenantLifecycleCommandRequest? LastDisableRequest { get; private set; }

        public int DisableSubmissions { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> EnableTenantAsync(TenantLifecycleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> DisableTenantAsync(TenantLifecycleCommandRequest request, CancellationToken cancellationToken = default)
        {
            LastDisableRequest = request;
            DisableSubmissions++;
            return Task.FromResult(Submission);
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
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
            ["Tenants.Lifecycle.Description"] = "Read-only high-impact lifecycle availability for tenant {0}. Commands remain unavailable until authority, freshness, command support, and governance are proven.",
            ["Tenants.Lifecycle.FactsLabel"] = "Tenant lifecycle safety facts",
            ["Tenants.Lifecycle.Freshness.Current"] = "Current",
            ["Tenants.Lifecycle.Freshness.Stale"] = "Stale",
            ["Tenants.Lifecycle.Freshness.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.FreshnessLabel"] = "Freshness",
            ["Tenants.Lifecycle.Governance.Ready"] = "Ready",
            ["Tenants.Lifecycle.Governance.Unresolved"] = "Unresolved",
            ["Tenants.Lifecycle.GovernanceGateLabel"] = "Governance gate",
            ["Tenants.Lifecycle.Operation.DisableTenant"] = "Disable tenant",
            ["Tenants.Lifecycle.Operation.EnableTenant"] = "Enable tenant",
            ["Tenants.Lifecycle.StateLabel"] = "Lifecycle state",
            ["Tenants.Lifecycle.CurrentStatusLabel"] = "Current status",
            ["Tenants.Lifecycle.Status.Active"] = "Active",
            ["Tenants.Lifecycle.Status.Disabled"] = "Disabled",
            ["Tenants.Lifecycle.Status.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.Title"] = "Lifecycle command availability",
            ["Tenants.Lifecycle.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Lifecycle.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Lifecycle.Audit.MissingSupport"] = "Audit support missing.",
            ["Tenants.Lifecycle.Audit.NotStarted"] = "Audit not started.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit delayed; retry status lookup or inspect audit.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Missing implementation support; continue read-only or escalate.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit pending; wait, retry status lookup, or inspect audit.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit unavailable; continue read-only, retry status lookup, or escalate.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "Continue read-only or escalate using support-safe information.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Continue read-only, retry status lookup, or escalate without raw diagnostics.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope.",
            ["Tenants.Lifecycle.Cancel"] = "Cancel",
            ["Tenants.Lifecycle.Confirmation.Help"] = "Type {0} exactly.",
            ["Tenants.Lifecycle.Confirmation.Label"] = "Type the tenant id to confirm",
            ["Tenants.Lifecycle.Confirm"] = "Confirm {0}",
            ["Tenants.Lifecycle.ConfirmedStatus"] = "Last confirmed lifecycle: {0}",
            ["Tenants.Lifecycle.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Lifecycle.Preview.AuditExpectation.Value"] = "Audit pending until evidence is available.",
            ["Tenants.Lifecycle.Preview.AuthorizationGovernance"] = "Authorization and governance",
            ["Tenants.Lifecycle.Preview.AuthorizationGovernance.Value"] = "Server-reflected authority and approved governance are present.",
            ["Tenants.Lifecycle.Preview.CommandSurface"] = "Command surface",
            ["Tenants.Lifecycle.Preview.CommandSurface.Value"] = "The command gateway is connected.",
            ["Tenants.Lifecycle.Preview.CurrentLifecycle"] = "Current lifecycle",
            ["Tenants.Lifecycle.Preview.Description"] = "Review lifecycle consequences for tenant {0}.",
            ["Tenants.Lifecycle.Preview.IntendedLifecycle"] = "Intended lifecycle",
            ["Tenants.Lifecycle.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Lifecycle.Preview.KnownConsequences.DisableTenant"] = "Availability is suspended after projection confirms Disabled.",
            ["Tenants.Lifecycle.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Lifecycle.Preview.KnownUnknowns.Value"] = "Consumers may observe availability asynchronously.",
            ["Tenants.Lifecycle.Preview.ListTitle"] = "Lifecycle consequence preview",
            ["Tenants.Lifecycle.Preview.ProjectionEvidence"] = "Projection evidence",
            ["Tenants.Lifecycle.Preview.ProjectionEvidence.Value"] = "Last confirmed status is {0}.",
            ["Tenants.Lifecycle.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Lifecycle.Preview.RecoveryPath.Value"] = "Submit the opposite lifecycle command after refresh.",
            ["Tenants.Lifecycle.Preview.TenantIdentity"] = "Tenant identity",
            ["Tenants.Lifecycle.Preview.Title"] = "{0} consequence preview",
            ["Tenants.Lifecycle.Recovery.Confirmed"] = "Use refreshed projection truth.",
            ["Tenants.Lifecycle.Recovery.Previewed"] = "Cancel or submit after exact confirmation.",
            ["Tenants.Lifecycle.Refresh"] = "Refresh status",
            ["Tenants.Lifecycle.Result.Title"] = "Lifecycle command result",
            ["Tenants.Lifecycle.State.Confirmed"] = "Projection confirmed the lifecycle change.",
            ["Tenants.Lifecycle.State.Previewed"] = "Preview ready.",
            ["Tenants.Lifecycle.Unavailable.AlreadyActive"] = "{1} is unavailable for tenant {0} because the current projection already shows Active. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Lifecycle.Unavailable.AlreadyDisabled"] = "{1} is unavailable for tenant {0} because the current projection already shows Disabled. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Lifecycle.Unavailable.CommandSurface"] = "{1} is unavailable for tenant {0} because lifecycle command support is not connected. Continue read-only or escalate command-surface readiness.",
            ["Tenants.Lifecycle.Unavailable.Governance"] = "{1} is unavailable for tenant {0} because high-impact lifecycle governance is not ready. Continue read-only until the platform gate is cleared.",
            ["Tenants.Lifecycle.Unavailable.MissingPermission"] = "{1} is unavailable for tenant {0} because server-side global-administrator authority is not proven. Request permission or continue read-only.",
            ["Tenants.Lifecycle.Unavailable.Mobile"] = "{1} is unavailable for tenant {0} because this viewport cannot preserve the full high-impact safety context. Continue read-only on this view.",
            ["Tenants.Lifecycle.Unavailable.InFlightOrCommandSurface"] = "A tenant command is already in flight or unavailable.",
            ["Tenants.Lifecycle.Unavailable.Identity"] = "Tenant identity is incomplete.",
            ["Tenants.Lifecycle.Unavailable.PreviewIncomplete"] = "Preview incomplete.",
            ["Tenants.Lifecycle.Unavailable.SameState"] = "The current projection already shows {0}.",
            ["Tenants.Lifecycle.Unavailable.StaleFreshness"] = "{1} is unavailable for tenant {0} because tenant freshness is stale or unknown. Refresh before considering lifecycle action availability.",
            ["Tenants.Lifecycle.Unavailable.UnknownStatus"] = "{1} is unavailable for tenant {0} because the tenant lifecycle state is unknown. Continue read-only and refresh projection evidence.",
            ["Tenants.Lifecycle.UnavailableReason.HighImpactFlowNotReady"] = "high-impact flow not ready",
            ["Tenants.Lifecycle.UnavailableReason.MissingLifecycleSupport"] = "missing lifecycle support",
            ["Tenants.Lifecycle.UnavailableReason.MissingPermission"] = "missing permission",
            ["Tenants.Lifecycle.UnavailableReason.None"] = "available",
            ["Tenants.Lifecycle.UnavailableReason.StaleData"] = "stale data",
            ["Tenants.Lifecycle.Validation.ConfirmationRequired"] = "Type tenant id {0} exactly.",
        };
    }
}
