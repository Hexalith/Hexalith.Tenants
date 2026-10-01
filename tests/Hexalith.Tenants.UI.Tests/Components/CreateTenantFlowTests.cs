using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class CreateTenantFlowTests : FluentBunitContext
{
    [Fact]
    public void Audit_return_restores_create_command_state_once_within_the_circuit()
    {
        TenantCreateAuditReturnState returnState = new(new ServiceCollection().BuildServiceProvider());
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot.Idle()
            .RequestSent(new CreateTenant("tenant.alpha", "Alpha", "Description"), null, true)
            .Accepted(TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"));
        returnState.Remember(snapshot, "tenant.alpha", "Alpha", "Description");
        Services.AddSingleton(returnState);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());

        IRenderedComponent<CreateTenantFlow> returned = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.RestoreAuditReturn, true));

        returned.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        returned.Find("[data-testid='tenants-create-tenant-id']").GetAttribute("value").ShouldBe("tenant.alpha");
        returned.Find("[data-testid='tenants-create-name']").GetAttribute("value").ShouldBe("Alpha");
        returnState.Take().ShouldBeNull();

        returnState.Remember(snapshot, "tenant.alpha", "Alpha", null);
        _ = Render<CreateTenantFlow>();
        returnState.Take().ShouldBeNull();
    }

    [Fact]
    public void Rendered_create_audit_launcher_saves_state_before_navigation()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        TenantCreateAuditReturnState returnState = new(new ServiceCollection().BuildServiceProvider());
        Services.AddSingleton(returnState);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        composition.IsReadSurfaceConnected.Returns(true);
        Services.AddSingleton(composition);

        IRenderedComponent<CascadingValue<bool>> wrapper = Render<CascadingValue<bool>>(parameters => parameters
            .Add(p => p.Name, "AuditReadAvailable")
            .Add(p => p.Value, true)
            .AddChildContent<CreateTenantFlow>(child => child
                .Add(p => p.BaselineTenantAbsent, true)
                .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<(TenantSummary?, string?)>((null, null)))));
        IRenderedComponent<CreateTenantFlow> flow = wrapper.FindComponent<CreateTenantFlow>();
        flow.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        flow.Find("[data-testid='tenants-create-name']").Change("Alpha");
        flow.Find("form").Submit();
        flow.WaitForElement("fluent-anchor-button[data-testid='tenants-audit-entrypoint']");

        var retained = returnState.Take();
        retained.ShouldNotBeNull();
        retained.Value.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        retained.Value.TenantId.ShouldBe("tenant.alpha");
    }

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
    public void Create_flow_renders_visible_heading_by_default()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        cut.Find("#tenants-create-heading").TextContent.ShouldBe("Create tenant");
        cut.Find("[data-testid='tenants-create-flow']")
            .GetAttribute("aria-labelledby").ShouldBe("tenants-create-heading");
    }

    [Fact]
    public void Create_flow_hides_duplicate_heading_when_show_heading_is_false()
    {
        // When the flow is hosted inside an accordion whose header already shows the title, the inner
        // <h2> must not be rendered (no duplicate visible title) while the section keeps an accessible name.
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.ShowHeading, false));

        cut.FindAll("#tenants-create-heading").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-create-flow']").GetAttribute("aria-labelledby").ShouldBeNull();
        cut.Find("[data-testid='tenants-create-flow']").GetAttribute("aria-label").ShouldBe("Create tenant");
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
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<(TenantSummary?, string?)>((null, null))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("Tenant.Mixed-01");
        cut.Find("[data-testid='tenants-create-name']").Change("Mixed Tenant");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.LastRequest.ShouldNotBeNull().TenantId.ShouldBe("Tenant.Mixed-01"));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Find("[data-testid='tenants-create-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Pending_audit_renders_the_shared_control_once_with_its_source_and_shared_label()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.EventsStored),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<(TenantSummary?, string?)>((null, null))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("Tenant.Mixed-01");
        cut.Find("[data-testid='tenants-create-name']").Change("Mixed Tenant");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        AngleSharp.Dom.IElement audit = cut.Find("[data-testid='tenants-create-audit']");
        AngleSharp.Dom.IElement control = audit.QuerySelector("[data-testid='tenants-audit-availability']").ShouldNotBeNull();
        control.GetAttribute("data-state").ShouldBe("pending");
        control.GetAttribute("data-audit-source").ShouldBe("create-tenant");

        // The entry point names the state with the shared label and never repeats it as visible text.
        AngleSharp.Dom.IElement entryPoint = audit.QuerySelector("[data-testid='tenants-audit-entrypoint']").ShouldNotBeNull();
        entryPoint.GetAttribute("aria-label").ShouldBe("Inspect audit for tenant Tenant.Mixed-01 (Audit pending)");
        entryPoint.TextContent.Trim().ShouldBe("Inspect audit");
        // The browser harness measures this inner control inside the recovery shell at 390px.
        entryPoint.ClassList.ShouldContain("tenants-audit-entrypoint");
        entryPoint.ParentElement.ShouldNotBeNull().ClassList.ShouldContain("tenants-audit-availability__action-shell");
        entryPoint.TextContent.ShouldNotContain("Audit pending");
        System.Text.RegularExpressions.Regex.Count(audit.TextContent, "Audit pending").ShouldBe(1);
    }

    [Fact]
    public void Ambiguous_failure_without_a_correlation_id_offers_no_refresh_that_cannot_requery()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Ambiguous(
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                "Tenants.Commands.Unavailable.InvalidTrackingReference"),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable));
        cut.Instance.Snapshot.CorrelationId.ShouldBeNull();
        cut.Find("[data-testid='tenants-create-audit'] [data-testid='tenants-audit-availability']")
            .GetAttribute("data-state").ShouldBe("unavailable");

        // Without a tracking handle the refresh cannot re-query anything; offering it would only exhaust the
        // retry bound behind a misleading limit note.
        cut.FindAll("[data-testid='tenants-create-audit'] [data-recovery-verb='refresh']").ShouldBeEmpty();
        gateway.StatusCallCount.ShouldBe(0);
    }

    [Fact]
    public void Denied_audit_read_renders_no_inspect_audit_recovery()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.EventsStored),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CascadingValue<bool>> wrapper = Render<CascadingValue<bool>>(parameters => parameters
            .Add(p => p.Name, "AuditReadDenied")
            .Add(p => p.Value, true)
            .AddChildContent<CreateTenantFlow>(child => child
                .Add(p => p.BaselineTenantAbsent, true)
                .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<(TenantSummary?, string?)>((null, null)))));
        IRenderedComponent<CreateTenantFlow> cut = wrapper.FindComponent<CreateTenantFlow>();

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending));
        cut.Find("[data-testid='tenants-create-audit'] [data-testid='tenants-audit-availability']");

        // No fragment is passed when audit read is denied, so no empty Inspect-audit shell or test id renders.
        cut.FindAll("[data-recovery-verb='inspectaudit']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-availability-recovery-inspectaudit']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-entrypoint']").ShouldBeEmpty();
        cut.Find("[data-recovery-verb='refresh']");
    }

    [Fact]
    public void Projection_evidence_confirms_without_exposing_internal_correlation_id()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<(TenantSummary?, string?)>(
                (new TenantSummary(tenantId, "Mixed Tenant", TenantStatus.Active), "projection-v2")))
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("Tenant.Mixed-01");
        cut.Find("[data-testid='tenants-create-name']").Change("Mixed Tenant");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        // Projection confirmation is not audit proof and this flow has no in-panel audit verification.
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        cut.Find("[data-testid='tenants-create-audit'] [data-testid='tenants-audit-availability']")
            .GetAttribute("data-state").ShouldBe("missingsupport");
        cut.Find("[data-testid='tenants-create-audit']").TextContent.ShouldContain("Missing implementation support");
        cut.Find("[data-testid='tenants-create-audit']").TextContent.ShouldNotContain("Audit available");
        cut.Markup.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public void Missing_support_without_audit_read_or_escalation_still_offers_continue_read_only()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        TenantCreateAttemptTracker tracker = new();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(tracker);

        IRenderedComponent<CascadingValue<bool>> wrapper = Render<CascadingValue<bool>>(parameters => parameters
            .Add(p => p.Name, "AuditReadDenied")
            .Add(p => p.Value, true)
            .AddChildContent<CreateTenantFlow>(child => child
                .Add(p => p.BaselineTenantAbsent, true)
                .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<(TenantSummary?, string?)>(
                    (new TenantSummary(tenantId, "Alpha", TenantStatus.Active), "projection-v2")))));
        IRenderedComponent<CreateTenantFlow> cut = wrapper.FindComponent<CreateTenantFlow>();
        const string audit = "[data-testid='tenants-create-audit']";

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        // Audit read is denied and no escalation destination is configured, so continue read-only is the one
        // recovery the missing-support state can still offer; the state never renders without a way forward.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport));
        cut.FindAll($"{audit} [data-recovery-verb='inspectaudit']").ShouldBeEmpty();
        cut.FindAll($"{audit} [data-recovery-verb='escalate']").ShouldBeEmpty();

        // The confirming refresh already released the terminal attempt's tracking, before any recovery ran.
        tracker.Find("tenant.alpha").ShouldBeNull();
        cut.Find($"{audit} [data-recovery-verb='continuereadonly']").Click();

        // Continue read-only resets the attempt panel without navigating, and focus lands on the lifecycle section
        // instead of falling back to the document body with the unmounted button.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle));
        cut.FindAll($"{audit} [data-testid='tenants-audit-availability']").ShouldBeEmpty();
        string lifecycleReferenceId = LifecycleReferenceId(cut);
        cut.WaitForAssertion(() => LastFocusedReferenceId().ShouldBe(lifecycleReferenceId));
        gateway.CreateTenantCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Continue_read_only_is_withheld_until_the_submission_projection_refresh_finishes()
    {
        TaskCompletionSource refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            // The default status cannot be read, so the attempt is unverified and offers continue read-only.
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.OnProjectionRefreshRequested, (Func<Task>)(async () =>
            {
                refreshStarted.TrySetResult();
                await releaseRefresh.Task.ConfigureAwait(false);
            })));
        const string audit = "[data-testid='tenants-create-audit']";
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        // The unreadable status is applied, and the submission is still running the host's projection refresh:
        // continue read-only would race the submission it dismisses, so the control does not offer it yet.
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => cut.Find($"{audit} [data-testid='tenants-audit-availability']")
            .GetAttribute("data-state").ShouldBe("unavailable"));
        cut.Find($"{audit} [data-recovery-verb='refresh']");
        cut.FindAll($"{audit} [data-recovery-verb='continuereadonly']").ShouldBeEmpty();

        releaseRefresh.SetResult();

        cut.WaitForAssertion(() => cut.Find($"{audit} [data-recovery-verb='continuereadonly']"), TimeSpan.FromSeconds(5));
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        gateway.CreateTenantCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Continue_read_only_during_a_status_lookup_drops_the_late_result_for_the_dismissed_attempt()
    {
        TaskCompletionSource lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<TenantCommandStatusResult> lateStatus = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int statusCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            // The first lookup cannot be read; the user's Refresh then waits on a slow lookup.
            StatusAsync = _ =>
            {
                if (Interlocked.Increment(ref statusCalls) == 1)
                {
                    return Task.FromResult(TenantCommandStatusResult.Unknown("Command status is unavailable."));
                }

                lookupStarted.TrySetResult();
                return lateStatus.Task;
            },
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));
        const string audit = "[data-testid='tenants-create-audit']";
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Find($"{audit} [data-recovery-verb='continuereadonly']"), TimeSpan.FromSeconds(5));

        Task refresh = cut.Find($"{audit} [data-recovery-verb='refresh']").ClickAsync(new MouseEventArgs());
        await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find($"{audit} [data-recovery-verb='continuereadonly']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle));

        // The late lookup belongs to the dismissed attempt: it is dropped instead of writing that attempt's lifecycle
        // and audit state back onto the reset panel.
        lateStatus.SetResult(new TenantCommandStatusResult(CommandStatus.EventsStored, EventCount: 1, HasVerifiedCommandIdentity: true));
        await refresh.WaitAsync(TimeSpan.FromSeconds(5));

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle));
        cut.Instance.Snapshot.MessageId.ShouldBeNull();
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        cut.FindAll($"{audit} [data-testid='tenants-audit-availability']").ShouldBeEmpty();
        Volatile.Read(ref statusCalls).ShouldBe(2);
        gateway.CreateTenantCallCount.ShouldBe(1);
    }

    [Fact]
    public void Audit_refresh_that_resolves_to_not_started_moves_focus_to_the_lifecycle_section()
    {
        int statusCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            // The first lookup cannot be read; the user's Refresh then finds the command still processing.
            StatusAsync = _ => Task.FromResult(++statusCalls == 1
                ? TenantCommandStatusResult.Unknown("Command status is unavailable.")
                : new TenantCommandStatusResult(CommandStatus.Processing, HasVerifiedCommandIdentity: true)),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable));
        string lifecycleReferenceId = LifecycleReferenceId(cut);
        int focusCallsBeforeRefresh = FocusCallCount();

        cut.Find("[data-testid='tenants-create-audit'] [data-recovery-verb='refresh']").Click();

        // The polite NotStarted state unmounts the control and its focused Refresh button, so focus lands on the
        // lifecycle section instead of falling back to the document body.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted));
        cut.Instance.Snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Polite);
        cut.FindAll("[data-testid='tenants-create-audit'] [data-testid='tenants-audit-availability']").ShouldBeEmpty();
        cut.WaitForAssertion(() => FocusCallCount().ShouldBeGreaterThan(focusCallsBeforeRefresh));
        LastFocusedReferenceId().ShouldBe(lifecycleReferenceId);
    }

    [Fact]
    public void Continue_read_only_from_an_unresolved_attempt_keeps_it_adoptable_instead_of_dispatching_again()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
        };
        TenantCreateAttemptTracker tracker = new();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(tracker);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        // The status lookup cannot be read, so the attempt is unverified and still tracked.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable));
        cut.Instance.Snapshot.IsTerminal.ShouldBeFalse();
        cut.Find("[data-testid='tenants-create-audit'] [data-recovery-verb='continuereadonly']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle));

        // The circuit keeps the unresolved attempt, so resubmitting the same tenant reconciles it.
        tracker.Find("tenant.alpha").ShouldNotBeNull();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.StatusCallCount.ShouldBe(2));
        gateway.CreateTenantCallCount.ShouldBe(1);
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [Fact]
    public void Missing_provenance_renders_localized_unable_to_verify_copy()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.BaselineProjectionVersion, "projection-v1")
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<(TenantSummary?, string?)>(
                (new TenantSummary(tenantId, "Alpha", TenantStatus.Active), "projection-v1"))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldContain("provenance could not be verified");

        // The projection was read but did not prove this create, and no audit read happened: the audit record
        // the Completed status established stays pending.
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void A_failed_projection_read_keeps_the_audit_record_pending()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.BaselineProjectionVersion, "projection-v1")
            .Add(p => p.ProjectionEvidenceProvider, _ => throw new InvalidOperationException("raw projection failure")));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        // A failed projection read is projection truth only: the command is unverified, the audit record is
        // still pending, and neither dimension rewrites the other.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        cut.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Create.Confirm.UnableToVerify.MissingProvenance");
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        cut.Find("[data-testid='tenants-create-audit'] [data-testid='tenants-audit-availability']")
            .GetAttribute("data-state").ShouldBe("pending");
        cut.Markup.ShouldNotContain("raw projection failure", Case.Insensitive);
    }

    [Fact]
    public void Baseline_not_found_allows_detail_provenance_confirmation()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        ITenantQueryGateway queryGateway = Substitute.For<ITenantQueryGateway>();
        queryGateway.GetTenantAsync(
                Arg.Any<TenantDetailRequest>(),
                Arg.Any<TenantDetailSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(TenantDetailSnapshot.NotFound("tenant.alpha")),
                Task.FromResult(TenantDetailSnapshot.Ready(
                    new TenantDetail(
                        "tenant.alpha",
                        "Alpha",
                        null,
                        TenantStatus.Active,
                        [],
                        new Dictionary<string, string>(),
                        DateTimeOffset.UtcNow),
                    "\"etag\"",
                    Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current,
                    projectionVersion: "projection-v2")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(queryGateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineProjectionVersion, "projection-v1"));
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
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

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldContain("already exists");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);

        // A rejection stored nothing: the command dimension shows the outcome and no audit state is implied.
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        cut.FindAll("[data-testid='tenants-audit-availability']").ShouldBeEmpty();
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

    [Theory]
    [InlineData("   ", "Alpha", "Tenant id is required")]
    [InlineData("tenant.alpha", "   ", "Name is required")]
    public void Whitespace_required_fields_show_validation_without_gateway_dispatch(
        string tenantId,
        string name,
        string expectedMessage)
    {
        StubTenantCommandGateway gateway = new();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();
        cut.Find("[data-testid='tenants-create-tenant-id']").Change(tenantId);
        cut.Find("[data-testid='tenants-create-name']").Change(name);
        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-create-validation']").TextContent.ShouldContain(expectedMessage);
        gateway.CreateTenantCallCount.ShouldBe(0);
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
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<(TenantSummary?, string?)>(
                (new TenantSummary(tenantId, "Alpha", TenantStatus.Active), "projection-v2"))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-create-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldContain("already exists");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        cut.FindAll("[data-testid='tenants-audit-availability']").ShouldBeEmpty();
    }

    [Fact]
    public void Retrying_the_same_tracked_attempt_refreshes_status_without_a_second_dispatch()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = TenantCommandStatusResult.Unknown("Command status could not be verified."),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            gateway.CreateTenantCallCount.ShouldBe(1);
            gateway.StatusCallCount.ShouldBe(2);
        });

        // AC4 is about the handle, not just the call count: the same logical attempt must keep its
        // original message id and correlation instead of minting a fresh ULID.
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Instance.Snapshot.CorrelationId.ShouldBe("correlation-123");
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

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
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

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
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

    [Fact]
    public void Changed_intent_while_an_attempt_is_still_tracked_blocks_without_losing_recovery()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.beta");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-create-safe-message']")
            .TextContent.ShouldContain("still being tracked"));
        gateway.CreateTenantCallCount.ShouldBe(1);
        // The block must not discard the tracking it protects, or the refresh recovery its own copy
        // names would be disabled and the next submit would dispatch an untracked duplicate.
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Instance.Snapshot.CorrelationId.ShouldBe("correlation-123");
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Find("[data-testid='tenants-create-refresh']").GetAttribute("disabled").ShouldBeNull();
    }

    [Fact]
    public void A_terminal_attempt_lets_a_different_tenant_start_a_deliberate_new_attempt()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true)
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<(TenantSummary?, string?)>(
                (new TenantSummary(tenantId, "Alpha", TenantStatus.Active), "projection-v2"))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));

        // Creating a second tenant is the normal path and must not cost a spurious blocked submit.
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.beta");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.CreateTenantCallCount.ShouldBe(2));
        cut.Instance.Snapshot.Intent.ShouldNotBeNull().TenantId.ShouldBe("tenant.beta");
    }

    [Fact]
    public void An_indeterminate_baseline_read_blocks_before_dispatch()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
        };
        ITenantQueryGateway queryGateway = Substitute.For<ITenantQueryGateway>();
        queryGateway.GetTenantAsync(
                Arg.Any<TenantDetailRequest>(),
                Arg.Any<TenantDetailSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantDetailSnapshot.Unavailable("Tenant detail is unavailable.")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(queryGateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        // An unreadable baseline proves neither presence nor absence, so it must fail closed before the
        // write rather than be recorded as "tenant was present" and dispatched anyway.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        gateway.CreateTenantCallCount.ShouldBe(0);
        gateway.LastRequest.ShouldBeNull();
    }

    [Fact]
    public void A_tenant_present_at_baseline_is_dispatched_but_can_never_be_confirmed()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        ITenantQueryGateway queryGateway = Substitute.For<ITenantQueryGateway>();
        queryGateway.GetTenantAsync(
                Arg.Any<TenantDetailRequest>(),
                Arg.Any<TenantDetailSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantDetailSnapshot.Ready(
                new TenantDetail(
                    "tenant.alpha",
                    "Alpha",
                    null,
                    TenantStatus.Active,
                    [],
                    new Dictionary<string, string>(),
                    DateTimeOffset.UtcNow),
                "\"etag\"",
                Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current,
                projectionVersion: "projection-v9")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(queryGateway);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.ProjectionEvidenceProvider, tenantId => Task.FromResult<(TenantSummary?, string?)>(
                (new TenantSummary(tenantId, "Alpha", TenantStatus.Active), "projection-v10"))));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        gateway.CreateTenantCallCount.ShouldBe(1);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void A_retained_circuit_attempt_is_adopted_instead_of_dispatched_again()
    {
        // After a component re-mount (workspace tab round trip, accordion collapse) the flow has no
        // tracking of its own, but the circuit still does. Re-submitting the same tenant must reconcile
        // the retained attempt rather than dispatch a second create for it.
        StubTenantCommandGateway gateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        TenantCreateAttemptTracker tracker = new();
        tracker.Remember("tenant.alpha", new TenantCommandTrackingHandle("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(tracker);

        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.BaselineTenantAbsent, true));

        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.StatusCallCount.ShouldBe(1));
        gateway.CreateTenantCallCount.ShouldBe(0);
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Instance.Snapshot.CorrelationId.ShouldBe("correlation-123");
    }

    private static string LifecycleReferenceId(IRenderedComponent<CreateTenantFlow> cut)
        => ((ElementReference)typeof(CreateTenantFlow)
            .GetField("_lifecycleElement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!).Id;

    private int FocusCallCount()
        => JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

    private string LastFocusedReferenceId()
        => JSInterop.Invocations
            .Where(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .Select(invocation => invocation.Arguments.FirstOrDefault())
            .OfType<ElementReference>()
            .LastOrDefault()
            .Id ?? string.Empty;

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; init; }

        public CreateTenant? LastRequest { get; private set; }

        public int CreateTenantCallCount { get; private set; }

        public int StatusCallCount { get; private set; }

        public string? LastMessageId { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
        {
            LastMessageId = messageId;
            CreateTenantCallCount++;
            LastRequest = request;
            return Task.FromResult(Submission);
        }

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            StatusCallCount++;
            return StatusAsync is null ? Task.FromResult(Status) : StatusAsync(handle);
        }
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Create.Title"] = "Create tenant",
            ["Tenants.Create.Description"] = "Submit a tenant creation command and wait for projection confirmation before treating it as visible.",
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
            ["Tenants.Create.Unavailable.Tracking"] = "The previous tenant creation attempt cannot be replaced while its result is still being tracked. Refresh its status or continue read-only.",
            ["Tenants.Create.Availability.FirstTenantUnknown"] = "Creation is available because the authorized tenant list is empty and awaiting its first projection write.",
            ["Tenants.Create.Availability.Stale"] = "Tenant creation is unavailable because the authorized tenant list is stale or cannot prove an empty first-tenant state.",
            ["Tenants.Create.Confirm.UnableToVerify.MissingProvenance"] = "The tenant projection matches the request, but creation provenance could not be verified.",
            ["Tenants.Create.State.Idle"] = "No command submitted.",
            ["Tenants.Create.State.RequestSent"] = "Request sent.",
            ["Tenants.Create.State.Accepted"] = "Accepted by EventStore; waiting for processing.",
            ["Tenants.Create.State.ProjectionPending"] = "Projection pending; tenant is not confirmed visible yet.",
            ["Tenants.Create.State.Confirmed"] = "Projection confirmed the tenant exists.",
            ["Tenants.Create.State.Rejected"] = "Command rejected.",
            ["Tenants.Create.State.Failed"] = "Command submission failed.",
            ["Tenants.Create.State.Degraded"] = "Command result is degraded and needs review.",
            ["Tenants.Create.State.UnableToVerify"] = "Unable to verify command result.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Inspect audit for tenant {1} ({0})",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.InvalidContext"] = "The audit context is invalid. Return to the originating page and select a current tenant.",
            ["Tenants.Audit.EntryPoint.Unavailable.Disconnected"] = "The audit read service is disconnected. Refresh when the connection returns.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "In-panel audit verification is not available for this command, so this panel cannot match an audit record to the attempt. The recorded outcome above is unchanged.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "The audit status could not be read or verified after the command was sent. This does not mean the record does not exist, and no proof is claimed.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.Availability.State.Available"] = "Audit available",
            ["Tenants.Audit.Availability.Reason.Pending"] = "The command's events are stored, but its audit record is not readable yet. It normally appears shortly, and no proof is claimed until it does.",
            ["Tenants.Audit.Availability.Reason.Delayed"] = "The audit record is taking longer than expected to become readable. No proof is claimed until it can be read.",
            ["Tenants.Audit.Availability.RetryLimit"] = "Repeated retries left this state unchanged, so retrying is no longer offered here.",
            ["Tenants.Audit.Receipt.Availability.Unavailable.Reason"] = "This audit read could not verify the requested evidence. This does not mean the record does not exist, and the recorded outcome is unchanged.",
            ["Tenants.Audit.Recovery.Action.Escalate"] = "Escalate without diagnostics",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
