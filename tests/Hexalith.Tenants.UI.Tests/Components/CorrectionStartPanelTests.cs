using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class CorrectionStartPanelTests : FluentBunitContext
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void Preview_exports_localized_rendered_markup_for_browser_validation(string culture)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Services.AddLocalization();
            StubTenantCommandGateway commands = new();
            Services.AddSingleton<ITenantCommandGateway>(commands);
            TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
                Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
            IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
                .Add(component => component.Intent, intent)
                .Add(component => component.StartProjection, intent.CurrentProjection));

            cut.Find("[data-testid='tenants-correction-original-evidence']").TextContent.ShouldBe("event-safe-reference");
            cut.Find("[data-testid='tenants-correction-original-time']").TextContent.ShouldBe("2026-06-01 10:00:00 UTC");
            cut.Find("[data-testid='tenants-correction-freshness']").TextContent.ShouldNotBeNullOrWhiteSpace();
            cut.Find("[data-testid='tenants-correction-provenance']").TextContent.ShouldNotBeNullOrWhiteSpace();
            cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
            cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
            cut.VisibleText().ShouldNotContain("Tenants.Correction.");
            cut.VisibleText().ShouldNotContain("ProjectionBacked");
            commands.AddUserRequests.ShouldBeEmpty();

            string? directory = Environment.GetEnvironmentVariable("TENANTS_CORRECTION_FIXTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, $"tenant-correction-preview-{culture}.html"), cut.Markup);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Panel_renders_original_evidence_current_snapshot_and_single_confirm_without_submission()
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
        cut.Find("[data-testid='tenants-correction-confirm']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.FindAll("[data-testid='tenants-correction-preview-handoff']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
        cut.Find("[data-testid='tenants-correction-consequences']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.Find("[data-testid='tenants-correction-unknowns']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.Find("[data-testid='tenants-correction-audit-expectation']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.Find("[data-testid='tenants-correction-recovery-path']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("fluent-accordion").Count.ShouldBe(1);
        cut.FindAll("fluent-accordion-item").Count.ShouldBe(2);
        cut.FindAll("fluent-accordion-item").ShouldAllBe(item => item.GetAttribute("heading-level") == "4");
        cut.Find("fluent-accordion-item").HasAttribute("expanded").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-role']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-correction-readiness tenants-correction-unavailable");
        cut.Find("#tenants-correction-readiness").ShouldNotBeNull();
        cut.Find("#tenants-correction-unavailable").ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null, TenantRole.TenantReader, false, 1, "Current tenant owner count: 1", true)]
    [InlineData(TenantRole.TenantContributor, TenantRole.TenantOwner, false, 1, "may grant or change", false)]
    [InlineData(TenantRole.TenantOwner, TenantRole.TenantReader, false, 1, "can leave this tenant with no owner", false)]
    [InlineData(TenantRole.TenantOwner, TenantRole.TenantReader, false, 2, "may grant or change", false)]
    [InlineData(null, TenantRole.TenantOwner, true, 0, "empty tenant receives", true)]
    public void Preview_shows_current_role_owner_count_and_impact_values(
        TenantRole? currentRole, TenantRole intendedRole, bool empty, int ownerCount, string impact, bool targetAbsent)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), intendedRole, currentRole);
        TenantCorrectionProjection projection = context.Projection! with {
            IsMembershipEmpty = empty,
            IsGlobalAdministrator = empty,
            OwnerCount = ownerCount,
        };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = projection });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));

        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent.ShouldContain(impact);
        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent
            .ShouldContain($"Current tenant owner count: {ownerCount}");
        if (ownerCount == 2) cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent
            .ShouldNotContain("last owner");
        cut.Find("[data-testid='tenants-correction-readiness']").TextContent.ShouldContain("are ready");
        cut.Find("[data-testid='tenants-correction-current-role']").TextContent.Contains("absent", StringComparison.Ordinal)
            .ShouldBe(targetAbsent);
    }

    [Fact]
    public void Empty_membership_with_nonowner_role_never_promises_owner_bootstrap()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader);
        TenantCorrectionProjection projection = context.Projection! with {
            IsMembershipEmpty = true, IsGlobalAdministrator = true, OwnerCount = 0,
        };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = projection });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));

        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent
            .ShouldContain("does not change tenant owner access");
        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent
            .ShouldNotContain("empty tenant receives");
    }

    [Fact]
    public async Task Failed_confirm_read_keeps_an_unsubmitted_preview_retryable()
    {
        Services.AddLocalization();
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent)
            .Add(p => p.CurrentCaptureProvider, () => ++reads == 1
                ? Task.FromException<TenantCorrectionProjection>(new HttpRequestException("read failed"))
                : Task.FromResult(intent.CurrentProjection!)));
        int focusCount = JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        await cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.HasSubmitted.ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("Confirm again to retry");
        commands.AddUserRequests.ShouldBeEmpty();
        cut.WaitForAssertion(() => AssertReasonFocus(cut, focusCount));

        await cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        commands.AddUserRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public void Regressed_confirm_capture_blocks_dispatch_and_keeps_the_preview()
    {
        Services.AddLocalization();
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader);
        TenantCorrectionProjection baseline = context.Projection! with { ProjectionVersion = "tenant-sequence:2" };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = baseline });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(baseline with {
                ProjectionVersion = "tenant-sequence:1",
            })));
        int focusCount = JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.HasSubmitted.ShouldBeFalse();
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("older than the previewed evidence");
        commands.AddUserRequests.ShouldBeEmpty();
        cut.WaitForAssertion(() => AssertReasonFocus(cut, focusCount));
    }

    [Fact]
    public void French_membership_failure_uses_localized_recovery_instead_of_gateway_english()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            Services.AddLocalization();
            StubTenantCommandGateway commands = new() {
                AddUserResultTask = Task.FromResult(TenantCommandSubmissionResult.Failed("English gateway failure")),
            };
            Services.AddSingleton<ITenantCommandGateway>(commands);
            TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
                Row("UserRemovedFromTenant"), TenantRole.TenantReader));
            IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
                .Add(p => p.Intent, intent)
                .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));

            cut.Find("[data-testid='tenants-correction-confirm']").Click();

            cut.Find("[data-testid='tenants-correction-safe-message']").TextContent
                .ShouldContain("correction du locataire a échoué");
            cut.VisibleText().ShouldNotContain("English gateway failure");
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public async Task Refresh_is_disabled_during_submission_and_one_status_read_at_a_time()
    {
        Services.AddLocalization();
        TaskCompletionSource<TenantCommandSubmissionResult> delivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<TenantCommandStatusResult> status = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway commands = new() { AddUserResultTask = delivery.Task, StatusTask = status.Task };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));

        Task submitting = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => commands.AddUserRequests.ShouldHaveSingleItem());
        cut.FindAll("[data-testid='tenants-correction-aggregate-busy']").ShouldBeEmpty();
        cut.VisibleText().ShouldNotContain("Another tenant command");
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("request was sent");
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
        delivery.SetResult(TenantCommandSubmissionResult.Accepted(commands.LastMessageId!, "tracking-safe"));
        cut.WaitForAssertion(() => commands.StatusHandles.ShouldHaveSingleItem());
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-refresh']").Click();
        commands.StatusHandles.ShouldHaveSingleItem();
        status.SetResult(TenantCommandStatusResult.Pending("English status pending"));
        await submitting.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeFalse();

        TaskCompletionSource<TenantCommandStatusResult> refreshingStatus = new(TaskCreationOptions.RunContinuationsAsynchronously);
        commands.StatusTask = refreshingStatus.Task;
        Task refreshing = cut.Find("[data-testid='tenants-correction-refresh']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => commands.StatusHandles.Count.ShouldBe(2));
        cut.Instance.HasSubmitted.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-refresh']").Click();
        commands.StatusHandles.Count.ShouldBe(2);
        refreshingStatus.SetResult(TenantCommandStatusResult.Pending("English status pending"));
        await refreshing.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Changed_owner_count_or_membership_empty_fact_requires_a_second_confirmation(bool ownerCountChanged)
    {
        Services.AddLocalization();
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"),
            ownerCountChanged ? TenantRole.TenantReader : TenantRole.TenantOwner,
            ownerCountChanged ? TenantRole.TenantOwner : null);
        TenantCorrectionProjection baseline = context.Projection! with {
            OwnerCount = ownerCountChanged ? 2 : 0,
            IsMembershipEmpty = !ownerCountChanged,
            IsGlobalAdministrator = true,
        };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = baseline });
        TenantCorrectionProjection changed = ownerCountChanged
            ? baseline with { OwnerCount = 1 }
            : baseline with { IsMembershipEmpty = false };
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, baseline)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(changed)));
        int focusCount = JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.HasSubmitted.ShouldBeFalse();
        cut.Instance.Snapshot!.SafeMessageKey.ShouldBe("Tenants.Correction.Preview.ChangedAtConfirm");
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("Review this updated preview and confirm again");
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.WaitForAssertion(() => AssertReasonFocus(cut, focusCount));
        commands.AddUserRequests.ShouldBeEmpty();
        commands.ChangeRoleRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Parent_projection_update_during_confirm_read_cannot_replace_the_reviewed_facts()
    {
        Services.AddLocalization();
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(Row("UserRoleChanged"),
            TenantRole.TenantReader, TenantRole.TenantContributor));
        TaskCompletionSource<TenantCorrectionProjection> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, intent.CurrentProjection)
            .Add(p => p.CurrentCaptureProvider, () => pending.Task));
        Task confirming = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        TenantCorrectionProjection changed = intent.CurrentProjection! with {
            CurrentRole = TenantRole.TenantOwner, ProjectionVersion = "tenant-sequence:2",
        };
        cut.Render(parameters => parameters.Add(p => p.StartProjection, changed));
        cut.Instance.Snapshot!.CurrentRole.ShouldBe(TenantRole.TenantContributor);
        pending.SetResult(changed);
        await confirming.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Preview.ChangedAtConfirm");
        cut.Instance.HasSubmitted.ShouldBeFalse();
        commands.ChangeRoleRequests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("UserAlreadyInTenant", false, "déjà membre")]
    [InlineData("UserNotInTenant", false, "n’est plus membre")]
    [InlineData("RoleEscalation", false, "rôle souhaité")]
    [InlineData("InsufficientPermissions", false, "droits actuels")]
    [InlineData("UserAlreadyInTenant", true, "déjà membre")]
    [InlineData("UserNotInTenant", true, "n’est plus membre")]
    [InlineData("RoleEscalation", true, "rôle souhaité")]
    [InlineData("InsufficientPermissions", true, "droits actuels")]
    [InlineData("TenantDisabled", false, "désactivé")]
    [InlineData("TenantDisabled", true, "désactivé")]
    [InlineData("TenantNotFound", false, "introuvable")]
    [InlineData("TenantNotFound", true, "introuvable")]
    public void French_typed_rejections_explain_the_reason_on_submission_and_status(
        string rejectionCode, bool fromStatus, string frenchReason)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            Services.AddLocalization();
            StubTenantCommandGateway commands = new() {
                AddUserResultFactory = (messageId, _) => fromStatus
                    ? TenantCommandSubmissionResult.Accepted(messageId!, "tracking-safe")
                    : TenantCommandSubmissionResult.Rejected("English gateway rejection", rejectionCode),
                Status = new(CommandStatus.Rejected, "English gateway rejection", rejectionCode,
                    HasVerifiedCommandIdentity: true),
            };
            Services.AddSingleton<ITenantCommandGateway>(commands);
            TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
                Row("UserRemovedFromTenant"), TenantRole.TenantReader));
            IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
                .Add(p => p.Intent, intent)
                .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));

            cut.Find("[data-testid='tenants-correction-confirm']").Click();

            string key = $"Tenants.Correction.Rejection.{rejectionCode}";
            cut.Instance.Snapshot!.SafeMessageKey.ShouldBe(key);
            cut.Find("[data-testid='tenants-correction-safe-message']").TextContent.Trim()
                .ShouldBe(Services.GetRequiredService<IStringLocalizer<TenantsResources>>()[key].Value);
            cut.VisibleText().ShouldContain(frenchReason);
            cut.VisibleText().ShouldNotContain("English gateway rejection");
            cut.VisibleText().ShouldNotContain("Tenants.Correction.");
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Theory]
    [InlineData("Pending", "Tenants.Correction.Status.Pending", "toujours en attente")]
    [InlineData("Retryable", "Tenants.Correction.Status.Retryable", "n’a pas pu être lu")]
    [InlineData("Rejected", "Tenants.Correction.Failure.Rejected", "locataire a été rejetée")]
    [InlineData("PublishFailed", "Tenants.Correction.Failure.Degraded", "entièrement vérifiée")]
    [InlineData("TimedOut", "Tenants.Correction.Failure.UnableToVerify", "ne peut pas être vérifié")]
    public void French_status_recovery_uses_resource_copy_without_gateway_english(
        string kind, string expectedKey, string frenchReason)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
            Services.AddLocalization();
            TenantCommandStatusResult status = kind switch {
                "Pending" => TenantCommandStatusResult.Pending("English gateway status"),
                "Retryable" => TenantCommandStatusResult.RetryableFailure("English gateway status"),
                _ => new(Enum.Parse<CommandStatus>(kind), "English gateway status", HasVerifiedCommandIdentity: true),
            };
            StubTenantCommandGateway commands = new() { Status = status };
            Services.AddSingleton<ITenantCommandGateway>(commands);
            TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
                Row("UserRemovedFromTenant"), TenantRole.TenantReader));
            IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
                .Add(p => p.Intent, intent)
                .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));

            cut.Find("[data-testid='tenants-correction-confirm']").Click();

            cut.Instance.Snapshot!.SafeMessageKey.ShouldBe(expectedKey);
            cut.Find("[data-testid='tenants-correction-safe-message']").TextContent.Trim()
                .ShouldBe(Services.GetRequiredService<IStringLocalizer<TenantsResources>>()[expectedKey].Value);
            cut.VisibleText().ShouldContain(frenchReason);
            cut.VisibleText().ShouldNotContain("English gateway status");
            cut.VisibleText().ShouldNotContain("Tenants.Correction.");
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Expired_retained_attempt_shows_only_available_status_recovery(bool hasCorrelation)
    {
        Services.AddLocalization();
        DateTimeOffset now = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        using TenantCorrectionAttemptTracker tracker = new(() => now, TimeSpan.FromMilliseconds(10));
        Services.AddSingleton(tracker);
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() {
            AmbiguousFirstAdd = !hasCorrelation,
            Status = TenantCommandStatusResult.Pending("English pending status"),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        string messageId = cut.Instance.Snapshot!.MessageId!;
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;

        cut.WaitForAssertion(() => gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha"))
            .ShouldBeFalse(), TimeSpan.FromSeconds(2));
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.SafeMessageKey.ShouldBe(hasCorrelation
            ? "Tenants.Correction.Unavailable.AttemptExpired"
            : "Tenants.Correction.Unavailable.AttemptExpiredWithoutCorrelation"));
        cut.Instance.Snapshot.MessageId.ShouldBe(messageId);
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled")
            .ShouldBe(!hasCorrelation);
        if (hasCorrelation)
        {
            cut.Find("[data-testid='tenants-correction-refresh']").Click();
            commands.StatusHandles.Count.ShouldBe(2);
        }
        commands.AddUserRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Late_delivery_of_an_expired_attempt_cannot_display_or_update_its_replacement()
    {
        Services.AddLocalization();
        DateTimeOffset now = new(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);
        using TenantCorrectionAttemptTracker tracker = new(() => now);
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(tracker);
        Services.AddSingleton(gate);
        TaskCompletionSource<TenantCommandSubmissionResult> delivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway commands = new() { AddUserResultTask = delivery.Task };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));
        Task submitting = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => commands.AddUserRequests.ShouldHaveSingleItem());
        string originalId = cut.Instance.Snapshot!.MessageId!;
        now += TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;
        tracker.IsExpired(intent.TenantScope).ShouldBeTrue();
        TenantCorrectionStartIntent replacementIntent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant", eventReference: "replacement-reference"), TenantRole.TenantReader));
        tracker.TryBegin(TenantCorrectionPreviewSnapshot.FromIntent(replacementIntent), gate,
            out TenantCorrectionAttempt? replacement).ShouldBeTrue();

        delivery.SetResult(TenantCommandSubmissionResult.Rejected("English rejection", "InsufficientPermissions"));
        await submitting.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.Snapshot.MessageId.ShouldBe(originalId);
        cut.Instance.Snapshot.OriginalAuditReference.ShouldBe(intent.OriginalAuditReference);
        tracker.Find(intent.TenantScope)!.MessageId.ShouldBe(replacement!.MessageId);
        tracker.Find(intent.TenantScope)!.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant(intent.TenantScope)).ShouldBeTrue();
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacingEvidenceDuringConfirmRequiresANewConfirmation(bool clearIntentFirst)
    {
        Services.AddLocalization();
        TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(tracker);
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader));
        TenantCorrectionStartIntent replacement = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant", eventReference: "replacement-evidence"), TenantRole.TenantReader));
        TaskCompletionSource<TenantCorrectionProjection> pendingCapture = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                readStarted.SetResult();
                return pendingCapture.Task;
            }));

        Task confirming = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (clearIntentFirst) cut.Render(parameters => parameters.Add(component => component.Intent, null));
        cut.Render(parameters => parameters.Add(component => component.Intent, replacement));
        pendingCapture.SetResult(intent.CurrentProjection!);
        await confirming.WaitAsync(TimeSpan.FromSeconds(5));

        commands.AddUserRequests.ShouldBeEmpty();
        commands.ChangeRoleRequests.ShouldBeEmpty();
        commands.StatusHandles.ShouldBeEmpty();
        tracker.Find("tenant.alpha").ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        cut.Instance.Snapshot!.OriginalAuditReference.ShouldBe("replacement-evidence");
        cut.Instance.HasSubmitted.ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Theory]
    [InlineData(true, "tracking-safe")]
    [InlineData(false, null)]
    [InlineData(false, " ")]
    public void IncompleteAcceptanceReceiptRetainsAnUnverifiedAttempt(bool missingMessageId, string? correlationId)
    {
        Services.AddLocalization();
        TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(tracker);
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new()
        {
            AddUserResultFactory = (messageId, _) => new(TenantCommandLifecycleState.Accepted,
                missingMessageId ? null : messageId, correlationId),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.State.UnableToVerify");
        cut.Instance.Snapshot.MessageId.ShouldBe(commands.LastMessageId);
        tracker.Find("tenant.alpha")!.MessageId.ShouldBe(commands.LastMessageId);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        commands.AddUserRequests.ShouldHaveSingleItem();
        commands.StatusHandles.ShouldBeEmpty();
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Cancel_or_Escape_during_pending_current_read_never_starts_an_attempt(
        bool escape,
        bool unmountBeforeReadCompletes)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionAttemptTracker tracker = new();
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(tracker);
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        TaskCompletionSource<TenantCorrectionProjection> pendingCapture = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int closed = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                readStarted.SetResult();
                return pendingCapture.Task;
            })
            .Add(component => component.OnClose, () => closed++));

        Task confirming = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (escape)
        {
            cut.Find("[data-testid='tenants-correction-panel']")
                .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        }
        else
        {
            cut.Find("[data-testid='tenants-correction-cancel']").Click();
        }

        closed.ShouldBe(1);
        if (unmountBeforeReadCompletes) cut.Dispose();
        pendingCapture.SetResult(intent.CurrentProjection!);
        await confirming.WaitAsync(TimeSpan.FromSeconds(5));

        commands.AddUserRequests.ShouldBeEmpty();
        commands.ChangeRoleRequests.ShouldBeEmpty();
        commands.StatusHandles.ShouldBeEmpty();
        tracker.Find("tenant.alpha").ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Fact]
    public void Panel_submits_restore_once_and_refuses_unlinked_audit_proof()
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
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.AddUserRequests.ShouldHaveSingleItem());
        commandGateway.AddUserRequests[0].TenantId.ShouldBe("tenant.alpha");
        commandGateway.AddUserRequests[0].UserId.ShouldBe("target-user");
        commandGateway.AddUserRequests[0].Role.ShouldBe(TenantRole.TenantReader);
        TenantCommandTrackingHandle handle = commandGateway.StatusHandles.ShouldHaveSingleItem();
        handle.ExpectedDomain.ShouldBe("tenants");
        NUlid.Ulid.TryParse(handle.MessageId, out _).ShouldBeTrue();
        handle.CorrelationId.ShouldBe("tracking-safe");
        handle.AggregateId.ShouldBe("tenant.alpha");
        queryGateway.DetailRequests.ShouldBeEmpty();
        queryGateway.AuditRequests.ShouldBeEmpty();
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirms the intended state");
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        cut.Markup.ShouldNotContain("event-original as undone", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
    }

    [Fact]
    public void Confirmed_redacted_projection_survives_cancel_and_panel_remount_without_new_command()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(new TenantCorrectionAttemptTracker());
        Services.AddSingleton(new TenantAggregateCommandAdmissionGate());
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        Func<Task<TenantCorrectionProjection>> captures = CaptureSequence(intent, TenantRole.TenantReader);
        bool closed = false;
        IRenderedComponent<CorrectionStartPanel> first = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, intent.CurrentProjection)
            .Add(component => component.CurrentCaptureProvider, captures)
            .Add(component => component.OnClose, () => closed = true));

        first.Find("[data-testid='tenants-correction-confirm']").Click();
        first.WaitForAssertion(() => first.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        first.Instance.Snapshot!.LastConfirmedCorrectionProjection!.ProjectionVersion.ShouldBe("tenant-sequence:2");
        first.Instance.Snapshot.LastConfirmedCorrectionProjection.CurrentRole.ShouldBe(TenantRole.TenantReader);
        first.Find("[data-testid='tenants-correction-cancel']").Click();
        closed.ShouldBeTrue();
        first.Dispose();

        IRenderedComponent<CorrectionStartPanel> resumed = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, intent.CurrentProjection)
            .Add(component => component.CurrentCaptureProvider, captures));

        resumed.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        resumed.Instance.Snapshot.LastConfirmedCorrectionProjection!.ProjectionVersion.ShouldBe("tenant-sequence:2");
        resumed.Find("[data-testid='tenants-correction-current-role']").TextContent.ShouldBe("Tenant reader");
        resumed.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        resumed.Markup.ShouldNotContain("tenant-sequence:2");
        commands.AddUserRequests.ShouldHaveSingleItem();
        commands.StatusHandles.ShouldHaveSingleItem();
    }

    [Fact]
    public void Panel_uses_one_current_capture_per_refresh_and_keeps_audit_separate()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("unused-user", TenantRole.TenantOwner)),
            Audit("event-corrective", "UserAddedToTenant"));
        int projectionRefreshCount = 0;
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                projectionRefreshCount++;
                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = projectionRefreshCount == 1 ? null : TenantRole.TenantReader,
                    ProjectionVersion = $"tenant-sequence:{projectionRefreshCount}" });
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        projectionRefreshCount.ShouldBe(2);
        queryGateway.DetailRequests.ShouldBeEmpty();
        queryGateway.AuditRequests.ShouldBeEmpty();
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirms the intended state");
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_provider_confirmed_correction_reports_missing_audit_association()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        // The audit surfaces only the original removal, never the corrective UserAddedToTenant row,
        // so proof lookup must run after projection confirmation and honestly report it as delayed
        // rather than linking unrelated evidence or collapsing the confirmed state into failure.
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("unused-user", TenantRole.TenantOwner)),
            Audit("event-original", "UserRemovedFromTenant"));
        int projectionRefreshCount = 0;
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                projectionRefreshCount++;
                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = projectionRefreshCount == 1 ? null : TenantRole.TenantReader,
                    ProjectionVersion = $"tenant-sequence:{projectionRefreshCount}" });
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        projectionRefreshCount.ShouldBe(2);
        queryGateway.DetailRequests.ShouldBeEmpty();
        queryGateway.AuditRequests.ShouldBeEmpty();
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirms the intended state");
    }

    [Fact]
    public void Panel_change_role_workflow_sends_change_role_command_and_rechecks_projection()
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
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor)))
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.ChangeRoleRequests.ShouldHaveSingleItem());
        commandGateway.ChangeRoleRequests[0].TenantId.ShouldBe("tenant.alpha");
        commandGateway.ChangeRoleRequests[0].UserId.ShouldBe("target-user");
        commandGateway.ChangeRoleRequests[0].NewRole.ShouldBe(TenantRole.TenantReader);
        commandGateway.AddUserRequests.ShouldBeEmpty();
        queryGateway.DetailRequests.ShouldBeEmpty();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
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
    public void Panel_blocks_confirm_when_command_status_lookup_is_unavailable()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new() { SupportsCommandStatusLookup = false };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-readiness']").TextContent.ShouldContain("blocked");
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("not connected");
        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        commandGateway.AddUserRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
    }

    [Fact]
    public void Clearing_selected_role_blocks_the_old_handoff_role_until_reselected()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-correction-role", string.Empty);

        cut.Instance.Snapshot!.IntendedRole.ShouldBe(TenantRole.Unknown);
        cut.Instance.Snapshot.Intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.ExplicitRoleRequired);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        commands.AddUserRequests.ShouldBeEmpty();

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-correction-role", TenantRole.TenantReader.ToString());
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Changed_command_at_confirmation_requires_review_and_a_second_click()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commands = new() { Status = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 4 } };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, intent.CurrentProjection)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                reads++;
                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = reads < 3 ? TenantRole.TenantContributor : TenantRole.TenantReader,
                    ProjectionVersion = $"tenant-sequence:{reads + 1}",
                });
            }));
        int focusCount = JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot.Intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.ChangeUserRole);
        cut.Find("[data-testid='tenants-correction-command']").TextContent.ShouldContain("Change user role");
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("Review this updated preview and confirm again");
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.WaitForAssertion(() => AssertReasonFocus(cut, focusCount));
        cut.Find("[data-testid='tenants-correction-role']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-correction-readiness tenants-correction-unavailable");
        commands.AddUserRequests.ShouldBeEmpty();
        commands.ChangeRoleRequests.ShouldBeEmpty();
        cut.Render(parameters => parameters.Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, intent.CurrentProjection)
            .Add(component => component.CurrentCaptureProvider, cut.Instance.CurrentCaptureProvider));
        cut.Instance.Snapshot.Intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.ChangeUserRole);

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        reads.ShouldBe(3);
        commands.AddUserRequests.ShouldBeEmpty();
        commands.ChangeRoleRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public void Preview_shows_safe_freshness_and_provenance_without_raw_metadata()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context);
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, context.Projection));

        cut.Find("[data-testid='tenants-correction-freshness']").TextContent.ShouldBe("Current");
        cut.Find("[data-testid='tenants-correction-provenance']").TextContent.ShouldBe("Verified tenant projection");
        cut.Find("[data-testid='tenants-correction-panel']").TextContent.ShouldNotContain("tenant-sequence:1");
        cut.Find("[data-testid='tenants-correction-panel']").TextContent.ShouldNotContain("ProjectionBacked");

        TenantCorrectionProjection stale = context.Projection! with {
            Freshness = ReadModelFreshnessState.Stale,
            Provenance = QueryResponseProvenance.Unknown,
        };
        cut.Render(parameters => parameters
            .Add(component => component.Intent, TenantCorrectionStartIntent.Evaluate(context with { Projection = stale }))
            .Add(component => component.StartProjection, stale));

        cut.Find("[data-testid='tenants-correction-freshness']").TextContent.ShouldBe("Stale");
        cut.Find("[data-testid='tenants-correction-provenance']").TextContent.ShouldBe("Projection source could not be verified");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Admission_acquired_during_current_read_blocks_then_releases_the_same_preview()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() { Status = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 3 } };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        object competitor = new();
        int captures = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                captures++;
                if (captures == 1)
                {
                    gate.TryAcquire(TenantCommandAggregateLock.ForTenant("tenant.alpha"), competitor).ShouldBeTrue();
                }

                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = captures < 3 ? null : TenantRole.TenantReader,
                    ProjectionVersion = $"tenant-sequence:{captures}",
                });
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Another tenant command");
        commands.AddUserRequests.ShouldBeEmpty();
        gate.Release(TenantCommandAggregateLock.ForTenant("tenant.alpha"), competitor);
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse());
        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        captures.ShouldBe(3);
        commands.AddUserRequests.ShouldHaveSingleItem();
        commands.StatusHandles.ShouldHaveSingleItem();
    }

    [Fact]
    public void Width_narrows_during_current_read_and_blocks_dispatch_until_safe_again()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantHighImpactViewportObservation viewport = new();
        viewport.Observe(ViewportTier.Desktop);
        Services.AddSingleton(viewport);
        StubTenantCommandGateway commands = new() { Status = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 3 } };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int captures = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                captures++;
                if (captures == 1) viewport.Observe(ViewportTier.Phone);
                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = captures < 3 ? null : TenantRole.TenantReader,
                    ProjectionVersion = $"tenant-sequence:{captures}",
                });
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("wider viewport");
        commands.AddUserRequests.ShouldBeEmpty();
        viewport.Observe(ViewportTier.Desktop);
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse());

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        captures.ShouldBe(3);
        commands.AddUserRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public void Panel_pre_submit_already_applied_live_updates_to_submittable_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantReader))));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Panel_pre_submit_role_conflict_live_updates_to_submittable_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor))));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot!.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentRoleConflict");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Panel_tracked_already_applied_status_survives_parent_re_render_without_re_arming_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0, HasVerifiedCommandIdentity: true),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied));
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        commandGateway.AddUserRequests.ShouldHaveSingleItem();
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
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => commandGateway.AddUserRequests.Count.ShouldBe(1));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        commandGateway.AddUserRequests.Count.ShouldBe(1);
        pendingSubmission.SetResult(TenantCommandSubmissionResult.Accepted(commandGateway.LastMessageId!, "tracking-safe"));
        cut.WaitForAssertion(() => commandGateway.StatusHandles.Count.ShouldBe(1));
        commandGateway.AddUserRequests.Count.ShouldBe(1);
    }

    [Fact]
    public void Panel_confirm_time_capture_already_applied_refuses_dispatch()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int captures = 0;

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                captures++;
                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = TenantRole.TenantReader,
                    ProjectionVersion = "tenant-sequence:2",
                });
            }));

        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        captures.ShouldBe(1);
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        commandGateway.AddUserRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_confirm_time_authority_loss_refuses_dispatch()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection! with {
                IsAuthorized = false,
                ProjectionVersion = "tenant-sequence:2",
            })));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeFalse();
        commandGateway.AddUserRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Fact]
    public void Role_change_with_absent_target_cannot_be_rederived_as_an_add()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRoleChanged"), intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, intent.CurrentProjection));

        cut.Instance.Snapshot!.CanSubmit.ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-current-role']").TextContent.ShouldContain("absent");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        commands.AddUserRequests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TenantRole.Unknown)]
    [InlineData((TenantRole)999)]
    public void Unassignable_current_role_blocks_and_never_renders_a_missing_resource_key(TenantRole currentRole)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRoleChanged"),
            TenantRole.TenantReader, currentRole);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context);
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, context.Projection));

        cut.Find("[data-testid='tenants-correction-current-role']").TextContent.ShouldBe("-");
        cut.VisibleText().ShouldNotContain("Tenants.Correction.Role.Unknown");
        cut.VisibleText().ShouldNotContain("Tenants.Correction.Role.999");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Instance.Snapshot!.Intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.CurrentStateIndeterminate);
    }

    [Fact]
    public void Missing_ordered_projection_version_has_a_localized_block_reason()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader);
        TenantCorrectionProjection unversioned = context.Projection! with { ProjectionVersion = null };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = unversioned });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, unversioned));

        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("ordered projection version is unavailable");
    }

    [Fact]
    public void Current_role_change_with_same_command_requires_a_second_review()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commands = new() { Status = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 4 } };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRoleChanged"), TenantRole.TenantReader, TenantRole.TenantContributor));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.StartProjection, intent.CurrentProjection)
            .Add(component => component.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection! with {
                CurrentRole = ++reads < 3 ? TenantRole.TenantOwner : TenantRole.TenantReader,
                ProjectionVersion = $"tenant-sequence:{reads + 1}",
            })));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("Review this updated preview and confirm again");
        commands.ChangeRoleRequests.ShouldBeEmpty();
        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent.ShouldContain("last owner");

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        commands.ChangeRoleRequests.ShouldHaveSingleItem();
        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent
            .ShouldContain("can leave this tenant with no owner");
        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent.ShouldNotContain("does not change tenant owner access");
    }

    [Fact]
    public void Unverified_completed_status_never_reads_projection_or_releases_lease()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() {
            Status = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: false),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () => {
                reads++;
                return Task.FromResult(intent.CurrentProjection!);
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        reads.ShouldBe(1);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
    }

    [Fact]
    public void Mismatched_gateway_message_id_is_announced_assertively_and_retains_admission()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() {
            AddUserResultFactory = (_, _) => TenantCommandSubmissionResult.Accepted(
                "01ARZ3NDEKTSV4RRFFQ69G5FAV", "tracking-safe"),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Fact]
    public void Definitive_failed_delivery_releases_the_aggregate_lease()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() {
            AddUserResultFactory = (messageId, _) => new(TenantCommandLifecycleState.Failed,
                MessageId: messageId, SafeMessage: "Request refused"),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Throwing_dispatch_retains_the_id_and_refresh_retries_only_that_id()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() { ThrowFirstAdd = true };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        string messageId = cut.Instance.Snapshot!.MessageId!;
        cut.Instance.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();

        cut.Find("[data-testid='tenants-correction-refresh']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        commands.AddUserMessageIds.ShouldAllBe(id => id == messageId);
        commands.AddUserMessageIds.Count.ShouldBe(2);
    }

    [Fact]
    public void Throwing_status_lookup_keeps_a_refreshable_attempt()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() { ThrowFirstStatus = true };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        string messageId = cut.Instance.Snapshot!.MessageId!;
        cut.Instance.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Accepted);
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();

        cut.Find("[data-testid='tenants-correction-refresh']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        commands.AddUserMessageIds.ShouldHaveSingleItem().ShouldBe(messageId);
    }

    [Fact]
    public void Remounted_pending_attempt_keeps_the_role_chosen_inside_the_preview()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(new TenantCorrectionAttemptTracker());
        Services.AddSingleton(new TenantAggregateCommandAdmissionGate());
        StubTenantCommandGateway commands = new() {
            Status = TenantCommandStatusResult.Pending("Status is still pending."),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> first = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () => Task.FromResult(intent.CurrentProjection!)));
        FluentSelectInterop.ChangeFluentSelect(first, "tenants-correction-role", TenantRole.TenantOwner.ToString());
        first.Find("[data-testid='tenants-correction-confirm']").Click();
        first.WaitForAssertion(() => first.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue());
        string messageId = first.Instance.Snapshot!.MessageId!;
        first.Dispose();

        IRenderedComponent<CorrectionStartPanel> resumed = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        resumed.Instance.Snapshot!.MessageId.ShouldBe(messageId);
        resumed.Instance.Snapshot.IntendedRole.ShouldBe(TenantRole.TenantOwner);
        resumed.Find("[data-testid='tenants-correction-readiness']").TextContent
            .ShouldContain("was submitted");
        commands.AddUserRequests.ShouldHaveSingleItem().Role.ShouldBe(TenantRole.TenantOwner);
    }

    [Theory]
    [InlineData("en", "Retry correction delivery", "Refresh status")]
    [InlineData("fr", "Réessayer l’envoi de la correction", "Actualiser l'état")]
    public void AmbiguousDeliveryRecoveryNamesRetryAndReusesOnlyTheRetainedMessageId(
        string culture, string retryLabel, string statusLabel)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Services.AddLocalization();
            StubTenantCommandGateway commandGateway = new() { AmbiguousFirstAdd = true };
            Services.AddSingleton<ITenantCommandGateway>(commandGateway);
            TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
                Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
            IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
                .Add(component => component.Intent, intent)
                .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

            cut.Find("[data-testid='tenants-correction-refresh']").TextContent.Trim().ShouldBe(statusLabel);
            cut.Find("[data-testid='tenants-correction-confirm']").Click();
            cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent));
            string messageId = cut.Instance.Snapshot!.MessageId!;
            NUlid.Ulid.TryParse(messageId, out _).ShouldBeTrue();
            commandGateway.AddUserMessageIds.ShouldHaveSingleItem().ShouldBe(messageId);
            commandGateway.StatusHandles.ShouldBeEmpty();
            cut.Find("[data-testid='tenants-correction-refresh']").TextContent.Trim().ShouldBe(retryLabel);

            cut.Find("[data-testid='tenants-correction-refresh']").Click();

            cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
            commandGateway.AddUserMessageIds.Count.ShouldBe(2);
            commandGateway.AddUserMessageIds.ShouldAllBe(id => id == messageId);
            commandGateway.StatusHandles.ShouldHaveSingleItem().MessageId.ShouldBe(messageId);
            cut.Find("[data-testid='tenants-correction-refresh']").TextContent.Trim().ShouldBe(statusLabel);
            cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
            cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Failed_delivery_with_the_retained_id_stays_locked_and_retries_that_id()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() {
            AddUserResultFactory = (messageId, count) => count == 1
                ? new(TenantCommandLifecycleState.Failed, MessageId: messageId, SafeMessage: "Gateway unavailable", IsAmbiguousFailure: true)
                : TenantCommandSubmissionResult.Accepted(messageId!, "tracking-safe"),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        string messageId = cut.Instance.Snapshot.MessageId!;
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeFalse();
        commands.StatusHandles.ShouldBeEmpty();

        cut.Find("[data-testid='tenants-correction-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        commands.AddUserMessageIds.Count.ShouldBe(2);
        commands.AddUserMessageIds.ShouldAllBe(id => id == messageId);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Fact]
    public void Explicit_domain_rejection_with_the_retained_id_releases_the_lease()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() {
            AddUserResultFactory = (messageId, _) => TenantCommandSubmissionResult.Rejected("Role rejected.", "RoleRejected")
                with { MessageId = messageId },
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
        commands.AddUserRequests.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public void Nonpositive_or_missing_event_count_does_not_read_projection_as_confirmation(int? eventCount)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commands = new() {
            Status = new(CommandStatus.Completed, EventCount: eventCount, HasVerifiedCommandIdentity: true),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                reads++;
                return Task.FromResult(intent.CurrentProjection! with {
                    CurrentRole = reads == 1 ? null : TenantRole.TenantReader,
                    ProjectionVersion = $"tenant-sequence:{reads}",
                });
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.EventCountUnavailable");
        reads.ShouldBe(1);
        commands.StatusHandles.ShouldHaveSingleItem();
        cut.Instance.Snapshot.LastConfirmedCorrectionProjection.ShouldBeNull();
    }

    [Fact]
    public void Panel_failed_terminal_state_moves_focus_to_lifecycle_without_refresh_tracking()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            AddUserResultTask = Task.FromResult(TenantCommandSubmissionResult.Failed("Correction command failed before verification.")),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed));
        cut.Instance.HasSubmitted.ShouldBeTrue();
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("failed");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-role']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
        commandGateway.StatusHandles.ShouldBeEmpty();
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
    }

    [Fact]
    public void Failed_correction_survives_a_parent_re_render_without_re_arming_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            AddUserResultTask = Task.FromResult(TenantCommandSubmissionResult.Failed("Correction command failed before verification.")),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed));

        // A parent re-render (for example an audit pager navigation or projection refresh that keeps this
        // panel open) re-passes the same intent with a refreshed projection that still shows the user
        // absent. The terminal failure must not reset to a fresh, re-armed preview (which would re-enable
        // Submit) and must not discard the failure evidence (AC4).
        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("failed");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Panel_rejected_terminal_state_moves_focus_to_lifecycle()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        // Submission is accepted, but the command status comes back Rejected. Before the terminal-focus
        // parity fix the panel moved focus to the lifecycle region only for Confirmed/Failed; every other
        // terminal state (Rejected here) silently left focus where it was. Now all terminal states move it,
        // matching the global-administrator correction panel.
        StubTenantCommandGateway commandGateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Rejected, EventCount: 0, HasVerifiedCommandIdentity: true),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider, CaptureSequence(intent, TenantRole.TenantReader)));
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
    }

    [Fact]
    public void Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);

        // The audit page provider returns null whenever the refreshed tenant projection is not Current
        // (Stale/Degraded/Unknown). A null provider result must fail closed: the correction stays
        // projection-pending, never confirming off stale evidence, focuses Refresh, and links no proof.
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.CurrentCaptureProvider,
                CaptureSequence(intent, TenantRole.TenantReader, postCommandUnavailable: true)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.StatusHandles.ShouldHaveSingleItem());
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Current_capture_fault_is_safe_before_or_after_admission(bool afterAdmission)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCorrectionAttemptTracker tracker = new();
        Services.AddSingleton(gate);
        Services.AddSingleton(tracker);
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, () =>
            {
                reads++;
                if (!afterAdmission || reads > 1) throw new InvalidOperationException("raw provider fault");
                return Task.FromResult(intent.CurrentProjection!);
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(afterAdmission
            ? TenantCommandLifecycleState.UnableToVerify : TenantCommandLifecycleState.Previewed));
        cut.Find("[data-testid='tenants-correction-safe-message']").TextContent.ShouldNotContain("raw provider fault");
        commands.AddUserRequests.Count.ShouldBe(afterAdmission ? 1 : 0);
        commands.StatusHandles.Count.ShouldBe(afterAdmission ? 1 : 0);
        (tracker.Find("tenant.alpha") is not null).ShouldBe(afterAdmission);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBe(afterAdmission);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Remounted_request_sent_panel_uses_advanced_tracker_before_refreshing_status()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionAttemptTracker tracker = new();
        Services.AddSingleton(tracker);
        Services.AddSingleton(new TenantAggregateCommandAdmissionGate());
        TaskCompletionSource<TenantCommandSubmissionResult> delivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway commands = new() {
            AddUserResultTask = delivery.Task,
            Status = TenantCommandStatusResult.Pending("Status pending."),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        Func<Task<TenantCorrectionProjection>> capture = CaptureSequence(intent, TenantRole.TenantReader);
        IRenderedComponent<CorrectionStartPanel> first = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, capture));

        Task submit = first.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        first.WaitForAssertion(() => commands.AddUserRequests.ShouldHaveSingleItem());
        string messageId = commands.LastMessageId!;
        first.Find("[data-testid='tenants-correction-cancel']").Click();
        first.Dispose();
        IRenderedComponent<CorrectionStartPanel> resumed = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentCaptureProvider, capture));
        resumed.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent);

        delivery.SetResult(TenantCommandSubmissionResult.Accepted(messageId, "tracking-safe"));
        await submit.WaitAsync(TimeSpan.FromSeconds(5));
        resumed.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent);
        tracker.Find("tenant.alpha")!.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Accepted);
        commands.StatusHandles.Count.ShouldBe(1);

        resumed.Find("[data-testid='tenants-correction-refresh']").Click();

        resumed.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Accepted);
        commands.StatusHandles.Count.ShouldBe(2);
        commands.AddUserMessageIds.ShouldHaveSingleItem().ShouldBe(messageId);
    }

    [Fact]
    public void Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();

        // The only candidate corrective row shares the original event's timestamp (10:00:00); it is not
        // strictly newer, so it cannot be causal proof of THIS correction. The time-tie-back lookup must
        // reject it and report audit evidence as delayed rather than linking an unrelated historical row.
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-not-newer", "UserAddedToTenant"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();

        // No audit query can prove command association because the authorized row omits it.
        queryGateway.AuditRequests.ShouldBeEmpty();
    }

    [Fact]
    public void Fresh_confirmation_capture_overrides_conflicting_cached_detail_without_a_start_capture()
    {
        Services.AddLocalization();
        StubTenantCommandGateway commands = new();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), TenantRole.TenantReader, TenantRole.TenantContributor));
        TenantCorrectionProjection fresh = intent.CurrentProjection! with {
            CurrentRole = TenantRole.TenantOwner, OwnerCount = 1, ProjectionVersion = "tenant-sequence:2",
        };
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent)
            .Add(p => p.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor)))
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(fresh)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.CurrentRole.ShouldBe(TenantRole.TenantOwner);
        cut.Instance.Snapshot.Intent.CurrentProjection.ShouldBe(fresh);
        cut.Instance.Snapshot.LastConfirmedProjectionEvidence.ShouldBeNull();
        cut.Instance.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Correction.Preview.ChangedAtConfirm");
        cut.Instance.Snapshot.CanSubmit.ShouldBeTrue();
        commands.ChangeRoleRequests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CommandStatus.EventsStored)]
    [InlineData(CommandStatus.EventsPublished)]
    public void Intermediate_event_progress_stays_pending_until_completed_proof_is_available(CommandStatus stage)
    {
        Services.AddLocalization();
        StubTenantCommandGateway commands = new() {
            Status = new(stage, EventCount: 1, HasVerifiedCommandIdentity: true),
        };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"), intendedRole: TenantRole.TenantReader));
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, intent.CurrentProjection)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(++reads == 1 ? intent.CurrentProjection! : intent.CurrentProjection! with {
                CurrentRole = TenantRole.TenantReader, ProjectionVersion = "tenant-sequence:2",
            })));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Instance.Snapshot.CommittedEventSequence.ShouldBeNull();
        cut.Instance.Snapshot.LastConfirmedCorrectionProjection.ShouldBeNull();
        reads.ShouldBe(1);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        string messageId = cut.Instance.Snapshot.MessageId!;
        commands.StatusTask = Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed,
            EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 2 });
        cut.Find("[data-testid='tenants-correction-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot!.MessageId.ShouldBe(messageId);
        reads.ShouldBe(2);
        commands.AddUserRequests.ShouldHaveSingleItem();
        commands.StatusHandles.Count.ShouldBe(2);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Fact]
    public void FreshRedactedHandoffOverridesConflictingCachedDetailWithoutManufacturingProjectionEvidence()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader, TenantRole.TenantContributor);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context);
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, context.Projection)
            .Add(p => p.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantReader))));
        cut.Instance.Snapshot!.CurrentRole.ShouldBe(TenantRole.TenantContributor);
        cut.Instance.Snapshot.IntendedRole.ShouldBe(TenantRole.TenantReader);
        cut.Instance.Snapshot.Intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.ChangeUserRole);
        cut.Instance.Snapshot.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot.LastConfirmedProjectionEvidence.ShouldBeNull();
        cut.Find("[data-testid='tenants-correction-current-role']").TextContent.ShouldBe("Tenant contributor");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RedactedPreviewRecoversFromAnUnsubmittedBlockedCapture(bool authorityLoss)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRoleChanged"), TenantRole.TenantReader, TenantRole.TenantContributor);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context);
        TenantCorrectionProjection projection = context.Projection!;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));
        TenantCorrectionProjection blocked = authorityLoss ? projection with { IsAuthorized = false }
            : projection with { Freshness = ReadModelFreshnessState.Stale };
        cut.Render(parameters => parameters.Add(p => p.Intent, TenantCorrectionStartIntent.Evaluate(context with { Projection = blocked }))
            .Add(p => p.StartProjection, blocked));
        cut.Instance.Snapshot!.CanSubmit.ShouldBeFalse();
        cut.Instance.Snapshot.HasCommandTracking.ShouldBeFalse();
        cut.Render(parameters => parameters.Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot.CanSubmit.ShouldBeTrue();
    }

    [Fact]
    public void MatchingRedactedRoleIsAlreadyAppliedAndNeverAnUnableToVerifyCommand()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRoleChanged"), TenantRole.TenantReader, TenantRole.TenantReader);
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, TenantCorrectionStartIntent.Evaluate(context)).Add(p => p.StartProjection, context.Projection));
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Instance.Snapshot.CanSubmit.ShouldBeFalse();
        cut.Instance.Snapshot.Intent.IntendedCommandType.ShouldBeNull();
        cut.Instance.Snapshot.ProofLink.ShouldBeNull();
    }

    [Fact]
    public void EmptyRecoveryCannotBeChangedToReaderInsidePreview()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantOwner);
        TenantCorrectionProjection projection = context.Projection! with { IsMembershipEmpty = true, IsGlobalAdministrator = true };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = projection });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-correction-role", TenantRole.TenantReader.ToString());
        cut.Instance.Snapshot!.CanSubmit.ShouldBeFalse();
        cut.Instance.Snapshot.Intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.EmptyMembershipRequiresOwner);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StaleOrMismatchedRedactedCaptureCannotShowAlreadyApplied(bool scopeConflict)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRoleChanged"), TenantRole.TenantReader, TenantRole.TenantReader);
        TenantCorrectionProjection projection = scopeConflict ? context.Projection! with { TenantId = "tenant.beta" }
            : context.Projection! with { Freshness = ReadModelFreshnessState.Stale };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = projection });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));
        cut.Instance.Snapshot!.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Instance.Snapshot.Intent.RequiredPreviewInputs.ContainsKey("currentRole").ShouldBeFalse();
        cut.Instance.Snapshot.CanSubmit.ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-current-role']").TextContent.ShouldBe("-");
        cut.Find("[data-testid='tenants-correction-owner-impact']").TextContent.ShouldBe("-");
    }

    [Fact]
    public void HiddenReadPreviewShowsOneAccessRecovery()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader);
        TenantCorrectionProjection hidden = TenantCorrectionProjection.Unavailable("tenant.alpha", "target-user");
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { IsAuthorized = false, Projection = hidden });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, hidden));
        cut.Instance.Snapshot!.CanSubmit.ShouldBeFalse();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.Trim()
            .ShouldBe("Current access could not be verified. Refresh or request permission before starting correction.");
        cut.Find("[data-testid='tenants-correction-current-snapshot']").TextContent.ShouldBe("Current tenant projection is unavailable.");
    }

    [Fact]
    public void EmptyRecoveryPreviewShowsLocalizedValuesWithoutMachineTokens()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantOwner);
        TenantCorrectionProjection projection = context.Projection! with { IsMembershipEmpty = true, IsGlobalAdministrator = true };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = projection });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(p => p.Intent, intent).Add(p => p.StartProjection, projection));
        cut.Instance.Snapshot!.Intent.RequiredPreviewInputs["emptyMembership"].ShouldBe("true");
        string previewData = cut.Find("[data-testid='tenants-correction-preview-data']").TextContent;
        previewData.ShouldContain("Empty membership recovery");
        previewData.ShouldContain("Current membership is empty. Recovery requires current global administrator authority");
        previewData.ShouldContain("Current state from the tenant projection; current authority was checked.");
        string visibleText = cut.Find("[data-testid='tenants-correction-panel']").TextContent;
        visibleText.ShouldNotContain("tenant-projection-current");
        System.Text.RegularExpressions.Regex.IsMatch(visibleText, @"\btrue\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .ShouldBeFalse();
    }

    [Fact]
    public void An_earlier_matching_role_stays_pending_until_the_commands_commit_is_projected()
    {
        Services.AddLocalization();
        TenantAggregateCommandAdmissionGate gate = new();
        Services.AddSingleton(gate);
        StubTenantCommandGateway commands = new() { Status = new(CommandStatus.Completed, EventCount: 1,
            HasVerifiedCommandIdentity: true) { CommittedEventSequence = 987654321 } };
        Services.AddSingleton<ITenantCommandGateway>(commands);
        TenantCorrectionStartContext context = Context(Row("UserRoleChanged"), TenantRole.TenantReader, TenantRole.TenantContributor);
        TenantCorrectionProjection baseline = context.Projection! with { ProjectionVersion = "tenant-sequence:5" };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = baseline });
        int reads = 0;
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters.Add(p => p.Intent, intent)
            .Add(p => p.StartProjection, baseline)
            .Add(p => p.CurrentCaptureProvider, () => Task.FromResult(++reads == 1 ? baseline : baseline with {
                CurrentRole = TenantRole.TenantReader,
                ProjectionVersion = reads == 2 ? "tenant-sequence:6" : "tenant-sequence:987654321",
            })));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        cut.VisibleText().ShouldNotContain("987654321");
        cut.Markup.ShouldNotContain("987654321");
        string messageId = cut.Instance.Snapshot.MessageId!;
        cut.Find("[data-testid='tenants-correction-refresh']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot!.MessageId.ShouldBe(messageId);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        commands.ChangeRoleRequests.ShouldHaveSingleItem();
        cut.VisibleText().ShouldNotContain("987654321");
        cut.Markup.ShouldNotContain("987654321");
    }

    [Fact]
    public void A_fresh_nonempty_capture_removes_the_old_empty_membership_preview_row()
    {
        Services.AddLocalization();
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartContext context = Context(Row("UserRemovedFromTenant"), TenantRole.TenantOwner);
        TenantCorrectionProjection empty = context.Projection! with { IsMembershipEmpty = true, IsGlobalAdministrator = true, OwnerCount = 0 };
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(context with { Projection = empty });
        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters.Add(p => p.Intent, intent)
            .Add(p => p.StartProjection, empty).Add(p => p.CurrentCaptureProvider, () => Task.FromResult(empty with {
                IsMembershipEmpty = false, OwnerCount = 1, ProjectionVersion = "tenant-sequence:2",
            })));
        cut.Instance.Snapshot!.Intent.RequiredPreviewInputs.ShouldContainKey("emptyMembership");
        int focusCount = JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.Instance.Snapshot!.Intent.RequiredPreviewInputs.ShouldNotContainKey("emptyMembership");
        cut.VisibleText().ShouldNotContain("This does not establish whether earlier membership existed");
        cut.Instance.HasSubmitted.ShouldBeFalse();
        cut.WaitForAssertion(() => AssertReasonFocus(cut, focusCount));
    }

    private void AssertReasonFocus(IRenderedComponent<CorrectionStartPanel> cut, int previousFocusCount)
    {
        var reason = (Microsoft.AspNetCore.Components.ElementReference)typeof(CorrectionStartPanel)
            .GetField("_reasonElement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!;
        var focusCalls = JSInterop.Invocations.Where(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ToArray();
        focusCalls.Length.ShouldBeGreaterThan(previousFocusCount);
        focusCalls.Last().Arguments.OfType<Microsoft.AspNetCore.Components.ElementReference>().Single().Id.ShouldBe(reason.Id);
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
            IntendedRole: intendedRole,
            Projection: new TenantCorrectionProjection(row.TenantId, row.Narrative?.UserId ?? string.Empty,
                TenantStatus.Active, currentRole, false, true, false, true, ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked)
                { ProjectionVersion = "tenant-sequence:1", OwnerCount = 1 });

    private static Func<Task<TenantCorrectionProjection>> CaptureSequence(
        TenantCorrectionStartIntent intent,
        TenantRole? postCommandRole,
        bool postCommandUnavailable = false)
    {
        int reads = 0;
        return () =>
        {
            reads++;
            if (postCommandUnavailable && reads > 1)
            {
                return Task.FromResult(TenantCorrectionProjection.Unavailable(intent.TenantScope, intent.TargetUserId));
            }

            TenantCorrectionProjection current = intent.CurrentProjection! with
            {
                CurrentRole = reads == 1 ? intent.CurrentProjection!.CurrentRole : postCommandRole,
                ProjectionVersion = $"tenant-sequence:{reads}",
            };
            return Task.FromResult(current);
        };
    }

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
            freshness: ReadModelFreshnessState.Current,
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
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked,
            new TenantAuditNarrative(UserId: eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal)
                ? "admin-user"
                : "target-user"));

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public bool SupportsCommandStatusLookup { get; init; } = true;

        public List<AddUserToTenant> AddUserRequests { get; } = [];

        public List<string?> AddUserMessageIds { get; } = [];

        public bool AmbiguousFirstAdd { get; init; }

        public bool ThrowFirstAdd { get; init; }

        public bool ThrowFirstStatus { get; init; }

        public List<ChangeUserRole> ChangeRoleRequests { get; } = [];

        public List<TenantCommandTrackingHandle> StatusHandles { get; } = [];

        public string? LastMessageId { get; private set; }

        public Task<TenantCommandSubmissionResult>? AddUserResultTask { get; init; }

        public Func<string?, int, TenantCommandSubmissionResult>? AddUserResultFactory { get; init; }

        public TenantCommandStatusResult Status { get; init; }
            = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 2 };

        public Task<TenantCommandStatusResult>? StatusTask { get; set; }

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
        {
            AddUserRequests.Add(request);
            AddUserMessageIds.Add(messageId);
            LastMessageId = messageId;
            if (ThrowFirstAdd && AddUserRequests.Count == 1) throw new HttpRequestException("transport failure");
            if (AddUserResultFactory is not null)
            {
                return Task.FromResult(AddUserResultFactory(messageId, AddUserRequests.Count));
            }
            if (AmbiguousFirstAdd && AddUserRequests.Count == 1)
            {
                return Task.FromResult(TenantCommandSubmissionResult.Ambiguous(messageId!, "Tenants.Correction.State.UnableToVerify"));
            }
            return AddUserResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted(messageId!, "tracking-safe"));
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRole request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
        {
            ChangeRoleRequests.Add(request);
            return Task.FromResult(TenantCommandSubmissionResult.Accepted(messageId!, "tracking-safe"));
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
        {
            StatusHandles.Add(handle);
            if (ThrowFirstStatus && StatusHandles.Count == 1) throw new HttpRequestException("status failure");
            return StatusTask ?? Task.FromResult(Status);
        }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfiguration request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubTenantQueryGateway(TenantDetail detail, TenantAuditSnapshot audit) : ITenantQueryGateway
    {
        public TenantRole? InitialRole { get; set; }

        public int CorrectionCaptureCount { get; private set; }

        public Task<TenantCorrectionProjection> GetTenantCorrectionProjectionAsync(
            string tenantId, string targetUserId, CancellationToken cancellationToken = default)
        {
            CorrectionCaptureCount++;
            TenantRole? role = CorrectionCaptureCount == 1
                ? InitialRole
                : detail.Members.FirstOrDefault(member => member.UserId == targetUserId)?.Role;
            return Task.FromResult(new TenantCorrectionProjection(tenantId, targetUserId,
                TenantStatus.Active, role, false, true, false, true,
                ReadModelFreshnessState.Current, ProjectionLifecycleState.Current,
                QueryResponseProvenance.ProjectionBacked)
                { ProjectionVersion = $"tenant-sequence:{CorrectionCaptureCount}", OwnerCount = 1 });
        }

        /// <summary>
        /// Explicit because <c>ITenantQueryGateway.GetTenantUsersAsync</c> is no longer a default interface
        /// method. This stub returns <c>Unavailable</c> deliberately: these tests do not exercise the member
        /// read, and an unavailable member surface is the correct fail-closed shape for them. Note this is
        /// the same value the removed default interface method returned, so a member-read regression is NOT
        /// caught here -- it is caught by the member-specific suites in
        /// <c>TenantDetailSurfaceTests</c>. (An earlier version of this remark claimed the opposite.)
        /// </summary>
        public Task<TenantUsersSnapshot> GetTenantUsersAsync(
            TenantUsersRequest request,
            TenantUsersSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.FromResult(TenantUsersSnapshot.Unavailable(request.TenantId));
        }

        public List<TenantDetailRequest> DetailRequests { get; } = [];

        public List<TenantAuditRequest> AuditRequests { get; } = [];

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            DetailRequests.Add(request);
            return Task.FromResult(TenantDetailSnapshot.Ready(detail, "\"detail-etag\"", ReadModelFreshnessState.Current));
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
            ["Tenants.Audit.Freshness.Aging"] = "Aging",
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Freshness.Stale"] = "Stale",
            ["Tenants.Audit.Freshness.Unknown"] = "Unknown",
            ["Tenants.Correction.Audit.AuditPending"] = "Corrective audit evidence is pending.",
            ["Tenants.Correction.Confirm.Cancel"] = "Cancel",
            ["Tenants.Correction.Confirm.Refresh"] = "Refresh status",
            ["Tenants.Correction.Confirm.RetryDelivery"] = "Retry correction delivery",
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
            ["Tenants.Correction.PreviewInput.emptyMembership"] = "Empty membership recovery",
            ["Tenants.Correction.Start.EmptyRecovery"] = "Current membership is empty. Recovery requires current global administrator authority and the explicitly chosen Owner role. This does not establish whether earlier membership existed.",
            ["Tenants.Correction.Start.Projection"] = "Current state from the tenant projection; current authority was checked.",
            ["Tenants.Correction.Start.TargetAbsent"] = "Target absent from the verified current membership",
            ["Tenants.Correction.PreviewInput.currentRole"] = "Current role",
            ["Tenants.Correction.PreviewInput.domain"] = "Domain",
            ["Tenants.Correction.PreviewInput.aggregateId"] = "Aggregate",
            ["Tenants.Correction.PreviewInput.intendedRole"] = "Intended role",
            ["Tenants.Correction.PreviewInput.originalAuditReference"] = "Original audit reference",
            ["Tenants.Correction.PreviewInput.tenantId"] = "Tenant",
            ["Tenants.Correction.PreviewInput.userId"] = "User",
            ["Tenants.Correction.Lifecycle.Title"] = "Correction lifecycle",
            ["Tenants.Correction.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Correction.Preview.AuditExpectation.Text"] = "A corrective audit record may appear after projection confirmation. This audit response cannot associate it with this attempt, so no receipt link is shown. Inspect audit or escalate.",
            ["Tenants.Correction.Preview.Consequence.Membership"] = "A new membership event may be appended if current projection truth allows it.",
            ["Tenants.Correction.Preview.Consequence.RoleChange"] = "A new role-change event may be appended if current projection truth allows it.",
            ["Tenants.Correction.Preview.Consequence.Unsupported"] = "No corrective command will be submitted without reusable support.",
            ["Tenants.Correction.Preview.Consequences"] = "Known consequences",
            ["Tenants.Correction.Preview.CurrentProjectionReady"] = "Current tenant projection is available for {0}.",
            ["Tenants.Correction.Preview.CurrentProjectionUnavailable"] = "Current tenant projection is unavailable.",
            ["Tenants.Correction.Preview.CurrentRole"] = "Current role",
            ["Tenants.Correction.Preview.Freshness"] = "Current read freshness",
            ["Tenants.Correction.Preview.Provenance"] = "Current read source",
            ["Tenants.Correction.Preview.Provenance.ProjectionBacked"] = "Verified tenant projection",
            ["Tenants.Correction.Preview.Provenance.Unverified"] = "Projection source could not be verified",
            ["Tenants.Correction.Preview.IntendedRole"] = "Intended role",
            ["Tenants.Correction.Preview.Readiness"] = "Current authority and admission",
            ["Tenants.Correction.Preview.Readiness.Ready"] = "Current authority, membership, projection version, and command support are ready. Confirm rechecks them.",
            ["Tenants.Correction.Preview.Readiness.Blocked"] = "Correction is blocked by current evidence, command support, viewport, or another tenant command. Resolve the reason above and refresh.",
            ["Tenants.Correction.Preview.OwnerImpact"] = "Owner access impact",
            ["Tenants.Correction.Preview.OwnerImpact.Bootstrap"] = "Current tenant owner count: {0}. The empty tenant receives an explicit owner; current global administrator authority is required.",
            ["Tenants.Correction.Preview.OwnerImpact.Changes"] = "Current tenant owner count: {0}. This command may grant or change tenant owner access.",
            ["Tenants.Correction.Preview.OwnerImpact.None"] = "Current tenant owner count: {0}. This command does not change tenant owner access.",
            ["Tenants.Correction.Preview.OwnerImpact.LastOwner"] = "Current tenant owner count: {0}. Demoting the last owner can leave this tenant with no owner. This command is not blocked; review the intended access before confirming.",
            ["Tenants.Correction.Preview.OriginalTime"] = "Original evidence time (UTC)",
            ["Tenants.Correction.Preview.Readiness.Submitted"] = "A command attempt was submitted. See the lifecycle and audit states for its outcome.",
            ["Tenants.Correction.Preview.ChangedAtConfirm"] = "Current membership or command facts changed. Review this updated preview and confirm again.",
            ["Tenants.Correction.Unavailable.ProjectionVersionUnavailable"] = "A current ordered projection version is unavailable. Refresh the tenant projection before confirming.",
            ["Tenants.Correction.Unavailable.ConfirmReadFailed"] = "Current tenant evidence could not be refreshed. Confirm again to retry the read before any command is sent.",
            ["Tenants.Correction.Unavailable.ProjectionRegressed"] = "The new tenant projection is older than the previewed evidence. Refresh the tenant projection and review before confirming.",
            ["Tenants.Correction.Failure.Failed"] = "The tenant correction failed. Refresh current evidence before considering another attempt.",
            ["Tenants.Correction.Failure.Rejected"] = "The tenant correction was rejected. Review current evidence and the rejection before taking further action.",
            ["Tenants.Correction.Failure.Degraded"] = "The tenant correction could not be fully verified. Refresh the retained command status.",
            ["Tenants.Correction.Failure.UnableToVerify"] = "The tenant correction outcome cannot be verified. Refresh the retained command status.",
            ["Tenants.Correction.Status.Pending"] = "The retained command status is still pending. Refresh this same attempt later.",
            ["Tenants.Correction.Status.Retryable"] = "The retained command status could not be read. Refresh this same attempt again.",
            ["Tenants.Correction.Unavailable.AttemptExpired"] = "The delivery window ended and tenant command admission is released. Refresh this retained attempt's status or start a correction from fresh current evidence.",
            ["Tenants.Correction.Unavailable.AttemptExpiredWithoutCorrelation"] = "The delivery window ended before status tracking became available, and tenant command admission is released. Escalate to verify this outcome or start a correction from fresh current evidence.",
            ["Tenants.Correction.Unavailable.CurrentStateIndeterminate"] = "Current member state is indeterminate.",
            ["Tenants.Correction.Resume"] = "Resume correction status",
            ["Tenants.Correction.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Correction.Preview.RecoveryPath.Text"] = "Refresh status, inspect audit evidence, continue read-only, or start a different correction if current projection truth conflicts.",
            ["Tenants.Correction.Preview.Unknown.HistoricalRole"] = "Historical role evidence can be stale; the selected intended role is authoritative for the new command.",
            ["Tenants.Correction.Preview.Unknown.SignalR"] = "Live notifications can nudge a refresh but do not prove correction success.",
            ["Tenants.Correction.Preview.Unknown.Unsupported"] = "Global administrator command support is unavailable in this UI surface.",
            ["Tenants.Correction.Preview.Unknowns"] = "Known unknowns",
            ["Tenants.Correction.Proof.Link"] = "View corrective proof from {0}",
            ["Tenants.Correction.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.Correction.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.Correction.Role.TenantReader"] = "Tenant reader",
            ["Tenants.Correction.RoleChoice.Label"] = "Choose intended role",
            ["Tenants.Correction.RoleChoice.Placeholder"] = "Select role",
            ["Tenants.Correction.State.Accepted"] = "Command accepted; projection confirmation is pending.",
            ["Tenants.Correction.State.AlreadyApplied"] = "Current projection already shows the intended state.",
            ["Tenants.Correction.State.Confirmed"] = "Projection confirms the intended state; waiting for corrective audit proof.",
            ["Tenants.Correction.State.Degraded"] = "Command processing is degraded; refresh status or inspect audit evidence.",
            ["Tenants.Correction.State.Failed"] = "Corrective command failed before acceptance.",
            ["Tenants.Correction.State.Previewed"] = "Preview is ready for deliberate confirmation.",
            ["Tenants.Correction.State.ProjectionPending"] = "Command events are stored; projection confirmation is pending.",
            ["Tenants.Correction.State.Rejected"] = "Corrective command was rejected.",
            ["Tenants.Correction.State.RequestSent"] = "Corrective command request was sent.",
            ["Tenants.Correction.State.UnableToVerify"] = "Correction cannot be verified from current evidence.",
            ["Tenants.Correction.Unavailable.CommandSupportUnavailable"] = "The tenant correction command path is not connected.",
            ["Tenants.Correction.Unavailable.AggregateBusy"] = "Another tenant command is being reconciled. Wait or refresh its status before starting a correction.",
            ["Tenants.Correction.Unavailable.NarrowViewportUnavailable"] = "Use a wider viewport to review the required safety context.",
            ["Tenants.Correction.Unavailable.OriginalTimeUnavailable"] = "The original evidence time could not be verified. Refresh the audit evidence before starting correction.",
            ["Tenants.Correction.Unavailable.OwnerCountUnavailable"] = "The current tenant owner count is unavailable. Refresh the tenant projection before confirming.",
            ["Tenants.Correction.Unavailable.AuthorizationIndeterminate"] = "Current access could not be verified. Refresh or request permission before starting correction.",
            ["Tenants.Correction.Unavailable.CommandProofUnavailable"] = "The completed correction cannot yet be verified against current tenant evidence. Refresh status or escalate before relying on the result.",
            ["Tenants.Correction.Unavailable.CurrentProjectionUnavailable"] = "Current projection evidence is unavailable.",
            ["Tenants.Correction.Title"] = "Start correction",
            ["Tenants.Correction.Unavailable.AlreadyApplied"] = "The current projection already matches the intended state.",
            ["Tenants.Correction.Unavailable.CurrentRoleConflict"] = "Current projection shows this user with a different role; start a role-change correction instead.",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
        };
    }
}
