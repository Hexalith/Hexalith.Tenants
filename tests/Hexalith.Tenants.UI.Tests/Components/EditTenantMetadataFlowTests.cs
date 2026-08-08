using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
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

public sealed class EditTenantMetadataFlowTests : FluentBunitContext
{
    [Fact]
    public void Edit_metadata_flow_renders_confirmed_metadata_stable_selectors_and_accessible_fields()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-edit-metadata-flow']");
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldContain("Alpha");
        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();

        cut.Find("label[for='tenants-edit-metadata-name']").TextContent.ShouldContain("Name");
        cut.Find("[data-testid='tenants-edit-metadata-name']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-edit-metadata-name-help");
        cut.Find("[data-testid='tenants-edit-metadata-description']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-edit-metadata-description-help");
        cut.Find("[data-testid='tenants-edit-metadata-submit']");
        cut.Find("[data-testid='tenants-edit-metadata-cancel']");
        cut.Find("[data-testid='tenants-edit-metadata-refresh']");
        cut.Find("[data-testid='tenants-edit-metadata-lifecycle']");
        cut.Find("[data-testid='tenants-edit-metadata-state']");
        cut.Find("[data-testid='tenants-edit-metadata-audit']");
        cut.Find("[data-testid='tenants-edit-metadata-recovery']");
    }

    [Fact]
    public void Permission_reflection_keeps_read_only_surface_with_inline_unavailable_reason()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsAuthorized, false));

        cut.Find("[data-testid='tenants-edit-metadata-unavailable-reason']").TextContent.ShouldContain("not authorized", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-edit-metadata-open']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldContain("Alpha");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, TenantStatus.Active, "Refresh current")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Current, TenantStatus.Disabled, "lifecycle state")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, TenantStatus.Active, "Refresh current")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current, TenantStatus.Active, "Refresh current")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, ReadModelFreshnessState.Current, TenantStatus.Active, "Refresh current")]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Current, TenantStatus.Active, "Refresh current")]
    public void Edit_metadata_fails_closed_for_stale_unknown_or_disabled_projection(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        TenantStatus status,
        string expectedReason)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description") with { Status = status })
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        string reason = cut.Find("[data-testid='tenants-edit-metadata-unavailable-reason']").TextContent;
        reason.ShouldContain(expectedReason, Case.Insensitive);
        if (surfaceKind is not TenantDetailSurfaceKind.Ready || freshness is not ReadModelFreshnessState.Current)
        {
            reason.ShouldNotContain("not authorized", Case.Insensitive);
        }

        cut.FindAll("[data-testid='tenants-edit-metadata-open']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Unknown)]
    [InlineData(ProjectionLifecycleState.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding)]
    [InlineData(ProjectionLifecycleState.Degraded)]
    [InlineData(ProjectionLifecycleState.Unavailable)]
    [InlineData(ProjectionLifecycleState.LocalOnly)]
    public void Edit_metadata_requires_current_projection_lifecycle(ProjectionLifecycleState lifecycle)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.Lifecycle, lifecycle));

        cut.Find("[data-testid='tenants-edit-metadata-unavailable-reason']").TextContent
            .ShouldContain("projection-confirmed lifecycle", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-edit-metadata-open']").ShouldBeEmpty();
    }

    [Fact]
    public void Edit_metadata_fails_closed_when_command_surface_is_unavailable()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, false));

        cut.Find("[data-testid='tenants-edit-metadata-unavailable-reason']").TextContent
            .ShouldContain("command support is unavailable", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-edit-metadata-open']").ShouldBeEmpty();
        gateway.UpdateTenantCallCount.ShouldBe(0);
    }

    [Fact]
    public void Name_validation_blocks_gateway_submission_with_safe_field_message()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("");
        cut.Find("form").Submit();

        gateway.UpdateTenantCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-edit-metadata-validation']").TextContent.ShouldContain("complete tenant name");
        cut.Find("[data-testid='tenants-edit-metadata-name']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-edit-metadata-name-help tenants-edit-metadata-validation");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
    }

    [Fact]
    public void Successful_submit_confirms_only_after_projection_evidence_and_preserves_last_confirmed_metadata_until_then()
    {
        int projectionCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-update"),
            StatusAsync = _ => Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)),
        };
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(++projectionCalls == 1
                ? Detail(request.TenantId, "Alpha", "Tenant alpha description")
                : Detail(request.TenantId, request.Name, request.Description))));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Updated");
        cut.Find("[data-testid='tenants-edit-metadata-description']").Change("");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        gateway.LastUpdateTenantRequest.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        gateway.LastUpdateTenantRequest.ShouldNotBeNull().Name.ShouldBe("Updated");
        gateway.LastUpdateTenantRequest.ShouldNotBeNull().Description.ShouldBeNull();
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldContain("Alpha");
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldNotContain("Updated");
        cut.Find("[data-testid='tenants-edit-metadata-state']").TextContent.ShouldContain("Projection pending");
        cut.Find("[data-testid='tenants-edit-metadata-state']").TextContent.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-update", Case.Insensitive);

        cut.Find("[data-testid='tenants-edit-metadata-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldContain("Updated");
        cut.Find("[data-testid='tenants-edit-metadata-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-edit-metadata-audit']").TextContent.ShouldContain("Audit evidence pending");
    }

    [Fact]
    public void Confirmed_clear_to_null_description_shows_empty_state_and_not_the_ambient_detail_description()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-update"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        // The component Detail parameter keeps a non-empty description; only projection evidence
        // proves the clear-to-null. The confirmed display must reflect the confirmed (empty) value,
        // never the still-populated ambient Detail.Description.
        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Original description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, request.Name, request.Description))));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-description']").Change("");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot.LastConfirmedDescription.ShouldBeNull();
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldContain("No description is confirmed");
        cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent.ShouldNotContain("Original description");
    }

    [Fact]
    public void Same_metadata_submission_is_still_sent_and_waits_for_projection_truth()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-update"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0),
        };
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(null)));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.UpdateTenantCallCount.ShouldBe(1));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, "rejected", "assertive")]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, "degraded", "assertive")]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, "Unable to verify", "assertive")]
    public void Status_refresh_keeps_terminal_lifecycle_states_distinct_and_support_safe(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        string expectedText,
        string expectedLiveRegion)
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-update"),
            Status = new TenantCommandStatusResult(status, "Safe status message.", "SafeCode"),
        };
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(Detail("tenant.alpha", "Updated", null))));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Updated");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(expectedState));
        cut.Find("[data-testid='tenants-edit-metadata-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Find("[data-testid='tenants-edit-metadata-live-region']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Markup.ShouldNotContain("correlation-update", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Gateway_submission_failure_remains_failed_assertive_and_support_safe()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Failed(
                "Metadata command submission failed. Retry from current tenant detail."),
        };
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Updated");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed));
        cut.Find("[data-testid='tenants-edit-metadata-state']").TextContent.ShouldContain("failed", Case.Insensitive);
        cut.Find("[data-testid='tenants-edit-metadata-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-edit-metadata-safe-message']").TextContent
            .ShouldContain("Retry from current tenant detail");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
    }

    [Fact]
    public void Cancel_and_escape_close_editor_without_submitting_and_request_focus_return()
    {
        int closeCount = 0;
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-cancel']").Click();
        cut.FindAll("[data-testid='tenants-edit-metadata-name']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-flow']").KeyDown("Escape");
        cut.FindAll("[data-testid='tenants-edit-metadata-name']").ShouldBeEmpty();

        closeCount.ShouldBe(2);
        gateway.UpdateTenantCallCount.ShouldBe(0);
    }

    [Fact]
    public void Metadata_styles_preserve_forced_colors_focus_and_status_shape_hooks()
    {
        string styles = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Metadata",
            "EditTenantMetadataFlow.razor.css"));

        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");
        styles.ShouldContain("border-inline-start");
        styles.ShouldContain("tenants-edit-metadata__state--confirmed");
    }

    [Fact]
    public void Command_activity_callback_wraps_in_flight_submission_for_parent_locking()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<bool> activity = [];
        StubTenantCommandGateway gateway = new()
        {
            UpdateTenantSubmissionAsync = _ => pending.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<EditTenantMetadataFlow> cut = Render<EditTenantMetadataFlow>(parameters => parameters
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.Detail, Detail("tenant.alpha", "Alpha", "Tenant alpha description"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, active => activity.Add(active)));

        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Updated");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => activity.ShouldContain(true));

        pending.SetResult(TenantCommandSubmissionResult.Failed("Safe failure."));

        cut.WaitForAssertion(() => activity.ShouldContain(false));
    }

    private void RegisterServices(StubTenantCommandGateway gateway)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
    }

    private static TenantDetail Detail(string tenantId, string name, string? description)
        => new(
            tenantId,
            name,
            description,
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private static string ProjectRoot()
    {
        string directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Hexalith.Tenants.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public Func<UpdateTenant, Task<TenantCommandSubmissionResult>>? UpdateTenantSubmissionAsync { get; init; }

        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; init; }

        public UpdateTenant? LastUpdateTenantRequest { get; private set; }

        public int UpdateTenantCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
        {
            UpdateTenantCallCount++;
            LastUpdateTenantRequest = request;
            return UpdateTenantSubmissionAsync is null ? Task.FromResult(Submission) : UpdateTenantSubmissionAsync(request);
        }

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => StatusAsync is null ? Task.FromResult(Status) : StatusAsync(handle);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.EditMetadata.Title"] = "Tenant metadata",
            ["Tenants.EditMetadata.Description"] = "Edit the confirmed metadata for tenant {0} through a command and projection-confirmed refresh.",
            ["Tenants.EditMetadata.Open"] = "Edit metadata",
            ["Tenants.EditMetadata.ConfirmedName.Label"] = "Last confirmed name",
            ["Tenants.EditMetadata.ConfirmedDescription.Label"] = "Last confirmed description",
            ["Tenants.EditMetadata.Name.Label"] = "Name",
            ["Tenants.EditMetadata.Name.Help"] = "Use the tenant display name to submit with this command.",
            ["Tenants.EditMetadata.Description.Label"] = "Description",
            ["Tenants.EditMetadata.Description.Help"] = "Leave empty to clear the tenant description.",
            ["Tenants.EditMetadata.Description.Empty"] = "No description is confirmed.",
            ["Tenants.EditMetadata.Submit"] = "Submit metadata update",
            ["Tenants.EditMetadata.Refresh"] = "Refresh status",
            ["Tenants.EditMetadata.Cancel"] = "Cancel",
            ["Tenants.EditMetadata.Lifecycle.Title"] = "Metadata command lifecycle",
            ["Tenants.EditMetadata.Validation.NameRequired"] = "Enter the complete tenant name before submitting metadata changes.",
            ["Tenants.EditMetadata.Unavailable.Authorization"] = "You are not authorized to edit this tenant's metadata.",
            ["Tenants.EditMetadata.Unavailable.Freshness"] = "Refresh current tenant detail before editing metadata.",
            ["Tenants.EditMetadata.Unavailable.ProjectionLifecycle"] = "Editing metadata requires a current, projection-confirmed lifecycle.",
            ["Tenants.EditMetadata.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow metadata editing.",
            ["Tenants.EditMetadata.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.EditMetadata.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.EditMetadata.Unavailable.Identity"] = "Tenant identity is unavailable, so metadata editing fails closed.",
            ["Tenants.EditMetadata.State.Idle"] = "No metadata command submitted.",
            ["Tenants.EditMetadata.State.RequestSent"] = "Metadata update request sent.",
            ["Tenants.EditMetadata.State.Accepted"] = "Accepted by EventStore; waiting for metadata processing.",
            ["Tenants.EditMetadata.State.ProjectionPending"] = "Projection pending; submitted metadata is not confirmed visible yet.",
            ["Tenants.EditMetadata.State.Confirmed"] = "Projection confirmed the submitted metadata.",
            ["Tenants.EditMetadata.State.Rejected"] = "Metadata update command rejected.",
            ["Tenants.EditMetadata.State.AlreadyApplied"] = "Already applied is not used for metadata updates.",
            ["Tenants.EditMetadata.State.DuplicatePrevented"] = "Duplicate metadata submission prevented.",
            ["Tenants.EditMetadata.State.Failed"] = "Metadata command submission failed.",
            ["Tenants.EditMetadata.State.Degraded"] = "Metadata command result is degraded and needs review.",
            ["Tenants.EditMetadata.State.UnableToVerify"] = "Unable to verify the metadata command result.",
            ["Tenants.EditMetadata.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.EditMetadata.Audit.AuditPending"] = "Audit evidence pending; no receipt is available in this story.",
            ["Tenants.EditMetadata.Audit.AuditDelayed"] = "Audit evidence delayed; wait or inspect audit when Epic 5 evidence is available.",
            ["Tenants.EditMetadata.Audit.AuditUnavailable"] = "Audit evidence unavailable; no proof or receipt is asserted.",
            ["Tenants.EditMetadata.Audit.MissingSupport"] = "Audit evidence support is missing until Epic 5 implements the evidence source.",
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
            ["Tenants.EditMetadata.Recovery.Idle"] = "Open the form when current projection evidence is available.",
            ["Tenants.EditMetadata.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.EditMetadata.Recovery.Accepted"] = "Wait, refresh status, or continue read-only until projection confirms the metadata.",
            ["Tenants.EditMetadata.Recovery.ProjectionPending"] = "Refresh the tenant detail; do not display success until submitted metadata is confirmed.",
            ["Tenants.EditMetadata.Recovery.Confirmed"] = "Continue read-only or inspect audit when evidence becomes available.",
            ["Tenants.EditMetadata.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.EditMetadata.Recovery.Failed"] = "Retry after checking current projection evidence or escalate.",
            ["Tenants.EditMetadata.Recovery.Degraded"] = "Wait, retry status lookup, inspect audit when available, or escalate.",
            ["Tenants.EditMetadata.Recovery.UnableToVerify"] = "Refresh, retry status lookup, continue read-only, or escalate.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
