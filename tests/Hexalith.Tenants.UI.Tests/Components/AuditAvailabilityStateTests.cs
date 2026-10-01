using System.Globalization;

using AngleSharp.Dom;

using Bunit;
using Bunit.Web.AngleSharp;

using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AuditAvailabilityStateTests : FluentBunitContext
{
    private const string EscalationHref = "/support/audit-incident";

    private static readonly string[] RecoveryVerbWords =
        ["wait", "refresh", "retry", "inspect", "continue", "read-only", "escalate", "request permission"];

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "pending", BadgeColor.Informative, "ClipboardClock", "Audit pending", "polite")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "delayed", BadgeColor.Warning, "ClockWarning", "Audit delayed", "polite")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "unavailable", BadgeColor.Severe, "DocumentProhibited", "Audit unavailable", "assertive")]
    [InlineData(TenantCommandAuditState.MissingSupport, "missingsupport", BadgeColor.Subtle, "ClockToolbox", "Missing implementation support", "assertive")]
    public void Incomplete_states_render_their_design_role_icon_label_explanation_and_politeness(
        TenantCommandAuditState auditState,
        string expectedCss,
        BadgeColor expectedColor,
        string expectedIcon,
        string expectedLabel,
        string expectedLiveRegion)
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, auditState));

        IElement root = cut.Find("[data-testid='tenants-audit-availability']");
        root.GetAttribute("data-state").ShouldBe(expectedCss);
        root.GetAttribute("aria-label").ShouldBe(expectedLabel);
        root.HasAttribute("aria-live").ShouldBeFalse();

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Appearance.ShouldBe(BadgeAppearance.Tint);
        badge.Color.ShouldBe(expectedColor);
        badge.Color.ShouldNotBe(BadgeColor.Success);
        badge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe(expectedIcon);
        badge.IconStart.Size.ShouldBe(IconSize.Size20);

        IElement badgeElement = cut.Find("[data-testid='tenants-audit-availability-badge']");
        badgeElement.TextContent.Trim().ShouldBe(expectedLabel);
        badgeElement.GetAttribute("aria-label").ShouldBe(expectedLabel);
        badgeElement.QuerySelectorAll("svg").ShouldHaveSingleItem().GetAttribute("aria-hidden").ShouldBe("true");

        cut.Find("[data-testid='tenants-audit-availability-announcement']").GetAttribute("aria-live")
            .ShouldBe(expectedLiveRegion);
        string explanation = cut.Find("[data-testid='tenants-audit-availability-explanation']").TextContent;
        explanation.ShouldBe(Explanations[auditState]);
        cut.Markup.ShouldNotContain("AuditPending", Case.Sensitive);
        cut.Markup.ShouldNotContain("audit_pending", Case.Insensitive);
    }

    [Fact]
    public void Proven_availability_is_the_only_success_badge_and_carries_no_explanation()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditAvailable));

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Color.ShouldBe(BadgeColor.Success);
        badge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe("DocumentCheckmark");
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("available");
        cut.Find("[data-testid='tenants-audit-availability-announcement']").GetAttribute("aria-live").ShouldBe("polite");
        cut.FindAll("[data-testid='tenants-audit-availability-explanation']").ShouldBeEmpty();
    }

    [Fact]
    public void Every_state_uses_a_distinct_glyph_so_forced_colors_keep_the_meaning()
    {
        RegisterLocalizer();
        TenantCommandAuditState[] states =
        [
            TenantCommandAuditState.AuditPending,
            TenantCommandAuditState.AuditDelayed,
            TenantCommandAuditState.AuditUnavailable,
            TenantCommandAuditState.AuditAvailable,
            TenantCommandAuditState.MissingSupport,
        ];

        string[] glyphs = [.. states.Select(state => Render<AuditAvailabilityState>(parameters => parameters
                .Add(component => component.AuditState, state))
            .FindComponent<FluentBadge>().Instance.IconStart.ShouldNotBeNull().GetType().Name)];

        glyphs.Distinct(StringComparer.Ordinal).Count().ShouldBe(states.Length);
    }

    [Fact]
    public void Not_started_renders_nothing()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.NotStarted));

        cut.Markup.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "refresh,inspectaudit")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "refresh,inspectaudit,escalate")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "refresh,continuereadonly,inspectaudit,escalate")]
    [InlineData(TenantCommandAuditState.MissingSupport, "continuereadonly,inspectaudit,escalate")]
    [InlineData(TenantCommandAuditState.AuditAvailable, "inspectaudit,continuereadonly")]
    public void Each_state_renders_its_canonical_recovery_set_with_stable_default_testids(
        TenantCommandAuditState auditState,
        string expectedVerbs)
    {
        ArgumentNullException.ThrowIfNull(expectedVerbs);
        RegisterLocalizer();
        RegisterComposition(EscalationHref);

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, auditState)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnInspectAudit, () => { })
            .Add(component => component.OnContinueReadOnly, () => { }));

        string[] rendered = [.. cut.FindAll("[data-recovery-verb]").Select(action => action.GetAttribute("data-recovery-verb")!)];
        rendered.ShouldBe(expectedVerbs.Split(','));
        foreach (string verb in rendered)
        {
            cut.Find($"[data-testid='tenants-audit-availability-recovery-{verb}']")
                .GetAttribute("data-recovery-verb").ShouldBe(verb);
        }

        // Waiting is conveyed by the explanation; a Wait control would be a live button that does nothing.
        cut.FindAll("[data-recovery-verb='wait']").ShouldBeEmpty();
    }

    [Fact]
    public void Each_recovery_runs_only_its_named_existing_path()
    {
        RegisterLocalizer();
        int refreshCount = 0;
        int inspectCount = 0;
        int continueCount = 0;

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => refreshCount++)
            .Add(component => component.OnInspectAudit, () => inspectCount++)
            .Add(component => component.OnContinueReadOnly, () => continueCount++));

        cut.Find("[data-recovery-verb='refresh']").Click();
        (refreshCount, inspectCount, continueCount).ShouldBe((1, 0, 0));

        cut.Find("[data-recovery-verb='inspectaudit']").Click();
        (refreshCount, inspectCount, continueCount).ShouldBe((1, 1, 0));

        cut.Find("[data-recovery-verb='continuereadonly']").Click();
        (refreshCount, inspectCount, continueCount).ShouldBe((1, 1, 1));
    }

    [Fact]
    public void Recovery_controls_without_a_real_path_do_not_render()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnContinueReadOnly, () => { }));

        cut.Find("[data-recovery-verb='continuereadonly']");
        cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
        cut.FindAll("[data-recovery-verb='inspectaudit']").ShouldBeEmpty();
        cut.FindAll("[data-recovery-verb='escalate']").ShouldBeEmpty();
    }

    [Fact]
    public void A_state_without_any_renderable_recovery_renders_no_empty_actions_region()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.MissingSupport));

        cut.FindAll("[data-recovery-verb]").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-availability-actions']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-availability-explanation']").TextContent
            .ShouldBe(Explanations[TenantCommandAuditState.MissingSupport]);
    }

    [Fact]
    public void Escalate_is_an_anchor_to_the_configured_local_destination_only()
    {
        RegisterLocalizer();
        RegisterComposition(EscalationHref);

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditDelayed));

        IElement escalate = cut.Find("[data-testid='tenants-audit-availability-recovery-escalate']");
        escalate.NodeName.ShouldBe("FLUENT-ANCHOR-BUTTON");
        escalate.GetAttribute("href").ShouldBe(EscalationHref);
        // Same support-safe wording as the audit page's link to the same destination.
        escalate.TextContent.Trim().ShouldBe("Escalate without diagnostics");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://support.example/audit")]
    [InlineData("//support.example/audit")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/support/audit?ticket=1")]
    [InlineData("/support/audit#incident")]
    [InlineData("/support/../admin")]
    [InlineData("/support\\audit")]
    [InlineData("%2F%2Fsupport.example")]
    public void Absent_or_non_local_escalation_configuration_renders_no_escalate_control(string? configuredHref)
    {
        RegisterLocalizer();
        RegisterComposition(configuredHref);

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { }));

        cut.FindAll("[data-recovery-verb='escalate']").ShouldBeEmpty();
        cut.FindAll("a, fluent-anchor-button").ShouldBeEmpty();
        cut.Find("[data-recovery-verb='refresh']");
    }

    [Fact]
    public void A_host_that_already_escalates_the_same_failure_suppresses_the_duplicate_link()
    {
        RegisterLocalizer();
        RegisterComposition(EscalationHref);

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.SuppressEscalation, true)
            .Add(component => component.OnRefresh, () => { }));

        cut.FindAll("[data-recovery-verb='escalate']").ShouldBeEmpty();
        cut.Find("[data-recovery-verb='refresh']");
    }

    [Fact]
    public void Three_unchanged_refreshes_withdraw_refresh_note_the_limit_and_focus_the_state_line()
    {
        RegisterLocalizer();
        RegisterComposition(EscalationHref);
        int refreshCount = 0;

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => refreshCount++)
            .Add(component => component.OnContinueReadOnly, () => { })
            .Add(component => component.OnInspectAudit, () => { }));

        for (int attempt = 1; attempt < TenantAuditAvailability.MaximumUnchangedRetries; attempt++)
        {
            cut.Find("[data-recovery-verb='refresh']").Click();
            cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
        }

        JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(0);
        cut.Find("[data-recovery-verb='refresh']").Click();

        refreshCount.ShouldBe(TenantAuditAvailability.MaximumUnchangedRetries);
        cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
        IElement limit = cut.Find("[data-testid='tenants-audit-availability-retry-limit']");
        limit.TextContent.Trim().ShouldBe(Values["Tenants.Audit.Availability.RetryLimit"]);

        // The other recoveries stay; only Refresh is withdrawn.
        string[] remaining = [.. cut.FindAll("[data-recovery-verb]").Select(action => action.GetAttribute("data-recovery-verb")!)];
        remaining.ShouldBe(["continuereadonly", "inspectaudit", "escalate"]);

        // Focus moves to the programmatically focusable state line, which the limit note describes.
        IElement stateLine = cut.Find("[data-testid='tenants-audit-availability-state']");
        stateLine.GetAttribute("tabindex").ShouldBe("-1");
        stateLine.GetAttribute("aria-describedby").ShouldBe(limit.GetAttribute("id"));
        ElementReference focused = JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeOfType<ElementReference>();
        focused.Id.ShouldNotBeNullOrWhiteSpace();
        focused.Id.ShouldBe(CapturedStateLineReference(cut.Instance).Id);

        // The limit note and focus change never enter the live region.
        cut.Find("[data-testid='tenants-audit-availability-announcement']")
            .QuerySelector("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeNull();
    }

    [Fact]
    public async Task Clicks_while_the_host_refresh_is_in_flight_are_ignored_not_counted()
    {
        RegisterLocalizer();
        var pending = new TaskCompletionSource();
        int hostRefreshes = 0;
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, EventCallback.Factory.Create(this, () =>
            {
                hostRefreshes++;
                return hostRefreshes == 1 ? pending.Task : Task.CompletedTask;
            })));

        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.Find("[data-recovery-verb='refresh']").Click();

        // The focused button stays in place and the repeated clicks neither re-invoke the host nor count.
        hostRefreshes.ShouldBe(1);
        cut.Find("[data-recovery-verb='refresh']");
        await cut.InvokeAsync(pending.SetResult);

        // The completed refresh finishes on the dispatcher after the host task; wait until it has settled.
        System.Reflection.FieldInfo inFlight = typeof(AuditAvailabilityState)
            .GetField("_refreshInFlight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        System.Reflection.FieldInfo counted = typeof(AuditAvailabilityState)
            .GetField("_unchangedRefreshCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        SpinWait.SpinUntil(
            () => !(bool)inFlight.GetValue(cut.Instance)! && (int)counted.GetValue(cut.Instance)! == 1,
            TimeSpan.FromSeconds(5)).ShouldBeTrue();
        cut.WaitForAssertion(() => cut.FindAll("[data-recovery-verb='refresh']").ShouldHaveSingleItem());

        // Only the completed refresh counted: two more unchanged refreshes are needed to reach the bound.
        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.Find("[data-recovery-verb='refresh']");
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty());
        hostRefreshes.ShouldBe(3);
    }

    [Theory]
    [MemberData(nameof(RendererTeardownFocusExceptions))]
    public async Task Refresh_failure_after_renderer_teardown_is_non_fatal(Exception exception)
    {
        RegisterLocalizer();
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshPending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, EventCallback.Factory.Create(this, async () =>
            {
                refreshStarted.SetResult();
                await refreshPending.Task.ConfigureAwait(false);
            })));

        Task activation = cut.InvokeAsync(() => InvokeRefreshAsync(cut.Instance));
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Instance.Dispose();
        cut.Dispose();
        refreshPending.SetException(exception);

        await activation.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SuccessfulRefreshAfterDisposalCompletesWithoutCountingARetry()
    {
        RegisterLocalizer();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, EventCallback.Factory.Create(this, () =>
            {
                started.SetResult();
                return pending.Task;
            })));

        Task activation = cut.Find("[data-recovery-verb='refresh']").ClickAsync(new MouseEventArgs());
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        AuditAvailabilityState instance = cut.Instance;
        // Disposing the bUnit wrapper alone leaves its component live; use renderer disposal.
        await DisposeComponentsAsync().WaitAsync(TimeSpan.FromSeconds(5));
        pending.SetResult();

        await activation.WaitAsync(TimeSpan.FromSeconds(5));
        CapturedRefreshCount(instance).ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(RendererTeardownFocusExceptions))]
    public async Task FinalizationContainsDispatcherTeardownButPreservesLiveFailures(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        RegisterLocalizer();
        foreach (bool disposed in new[] { false, true })
        {
            IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
                .Add(component => component.AuditState, TenantCommandAuditState.AuditPending));
            if (disposed)
            {
                cut.Instance.Dispose();
            }

            // Fail the dispatcher itself, independently of host-task faults and successful dispatch after disposal.
            Dispatcher failingDispatcher = Substitute.For<Dispatcher>();
            failingDispatcher.InvokeAsync(Arg.Any<Action>()).Returns(_ => Task.FromException(exception));
            System.Reflection.FieldInfo dispatcherField = Renderer.GetType()
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(Dispatcher));
            object originalDispatcher = dispatcherField.GetValue(Renderer)!;
            dispatcherField.SetValue(Renderer, failingDispatcher);
            try
            {
                Func<Task> finalize = () => ((Task)typeof(AuditAvailabilityState)
                    .GetMethod("FinalizeRefreshAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .Invoke(cut.Instance, [TenantAuditAvailabilityState.Pending, true])!)
                    .WaitAsync(TimeSpan.FromSeconds(5));
                if (disposed)
                {
                    await finalize();
                }
                else
                {
                    Exception observed = exception switch
                    {
                        ObjectDisposedException => await Should.ThrowAsync<ObjectDisposedException>(finalize),
                        TaskCanceledException => await Should.ThrowAsync<TaskCanceledException>(finalize),
                        _ => await Should.ThrowAsync<InvalidOperationException>(finalize),
                    };
                    observed.GetType().ShouldBe(exception.GetType());
                    if (exception is not TaskCanceledException)
                    {
                        observed.ShouldBeSameAs(exception);
                    }
                }

                failingDispatcher.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(Dispatcher.InvokeAsync))
                    .ShouldBe(1);
            }
            finally
            {
                dispatcherField.SetValue(Renderer, originalDispatcher);
            }
        }
    }

    [Theory]
    [MemberData(nameof(RendererTeardownFocusExceptions))]
    public async Task Live_refresh_failure_surfaces_without_counting_and_allows_retry(Exception exception)
    {
        RegisterLocalizer();
        int hostRefreshes = 0;
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, EventCallback.Factory.Create(this, () =>
                ++hostRefreshes == 1 ? Task.FromException(exception) : Task.CompletedTask)));

        Func<Task> refresh = () => cut.InvokeAsync(() => InvokeRefreshAsync(cut.Instance))
            .WaitAsync(TimeSpan.FromSeconds(5));
        Exception observed = exception switch
        {
            ObjectDisposedException => await Should.ThrowAsync<ObjectDisposedException>(refresh),
            TaskCanceledException => await Should.ThrowAsync<TaskCanceledException>(refresh),
            _ => await Should.ThrowAsync<InvalidOperationException>(refresh),
        };
        // Async cancellation propagates as a canceled task and may recreate the cancellation exception.
        if (exception is not TaskCanceledException)
        {
            observed.ShouldBeSameAs(exception);
        }
        CapturedRefreshCount(cut.Instance).ShouldBe(0);
        cut.FindAll("[data-recovery-verb='refresh']").ShouldHaveSingleItem();

        await refresh();

        hostRefreshes.ShouldBe(2);
        CapturedRefreshCount(cut.Instance).ShouldBe(1);
        cut.FindAll("[data-recovery-verb='refresh']").ShouldHaveSingleItem();
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Dispatcher_finalization_keeps_the_gate_closed_until_the_retry_is_counted()
    {
        RegisterLocalizer();
        // Inline continuations make SetResult return only after finalization has been queued off dispatcher.
        var pending = new TaskCompletionSource();
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var dispatcherHeld = new ManualResetEventSlim();
        using var clickAgain = new ManualResetEventSlim();
        int hostRefreshes = 0;
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, EventCallback.Factory.Create(this, () =>
            {
                hostRefreshes++;
                refreshStarted.TrySetResult();
                return hostRefreshes == 1 ? pending.Task : Task.CompletedTask;
            })));

        Task activation = cut.Find("[data-recovery-verb='refresh']").ClickAsync(new MouseEventArgs());
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task blocker = Task.Run(() => Renderer.Dispatcher.InvokeAsync(() =>
        {
            dispatcherHeld.Set();
            clickAgain.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            // This click runs on the held dispatcher before its queued finalization can release the gate.
            cut.Find("[data-recovery-verb='refresh']").Click();
        }));
        try
        {
            dispatcherHeld.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            await Task.Run(pending.SetResult).WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            clickAgain.Set();
            await blocker.WaitAsync(TimeSpan.FromSeconds(5));
        }

        await activation.WaitAsync(TimeSpan.FromSeconds(5));
        hostRefreshes.ShouldBe(1);
        CapturedRefreshCount(cut.Instance).ShouldBe(1);

        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty());
        hostRefreshes.ShouldBe(3);
    }

    [Theory]
    [MemberData(nameof(RendererTeardownFocusExceptions))]
    public async Task Disposed_focus_handoff_contains_renderer_teardown(Exception exception)
    {
        RegisterLocalizer();
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetException(exception);
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending));
        AuditAvailabilityState instance = cut.Instance;
        typeof(AuditAvailabilityState)
            .GetField("_focusStateLinePending", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(instance, true);
        instance.Dispose();
        cut.Dispose();

        await InvokeAfterRenderAsync(instance);
    }

    /// <summary>
    /// Verifies that a focus timeout is best effort while unrelated failures remain visible on a live component.
    /// </summary>
    /// <param name="exception">The failure returned by the focus interop call.</param>
    [Theory]
    [MemberData(nameof(RendererTeardownFocusExceptions))]
    public async Task LiveFocusHandoffContainsCancellationButPreservesOtherFailures(Exception exception)
    {
        RegisterLocalizer();
        JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true)
            .SetException(exception);
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending));
        typeof(AuditAvailabilityState)
            .GetField("_focusStateLinePending", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(cut.Instance, true);

        Func<Task> focus = () => InvokeAfterRenderAsync(cut.Instance).WaitAsync(TimeSpan.FromSeconds(5));
        if (exception is TaskCanceledException)
        {
            // Interop can time out while the component is live. Focus is best effort and never ends the circuit.
            await focus();
            JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBe(CapturedStateLineReference(cut.Instance));
            CapturedRefreshCount(cut.Instance).ShouldBe(0);
            cut.Find("[data-testid='tenants-audit-availability-state']");
            return;
        }

        Exception observed = exception switch
        {
            ObjectDisposedException => await Should.ThrowAsync<ObjectDisposedException>(focus),
            _ => await Should.ThrowAsync<InvalidOperationException>(focus),
        };
        observed.ShouldBeSameAs(exception);
    }

    public static TheoryData<Exception> RendererTeardownFocusExceptions
        => new()
        {
            new ObjectDisposedException("renderer"),
            new TaskCanceledException("Renderer focus was cancelled during teardown."),
            new InvalidOperationException("Renderer is no longer interactive."),
        };

    private static Task InvokeAfterRenderAsync(AuditAvailabilityState instance)
        => (Task)typeof(AuditAvailabilityState)
            .GetMethod("OnAfterRenderAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(instance, [false])!;

    private static Task InvokeRefreshAsync(AuditAvailabilityState instance)
        => (Task)typeof(AuditAvailabilityState)
            .GetMethod("RefreshAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(instance, null)!;

    private static int CapturedRefreshCount(AuditAvailabilityState instance)
        => (int)typeof(AuditAvailabilityState)
            .GetField("_unchangedRefreshCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(instance)!;

    [Theory]
    [InlineData(TenantCommandAuditState.MissingSupport)]
    [InlineData(TenantCommandAuditState.AuditAvailable)]
    public void A_refresh_that_leaves_no_refresh_control_moves_focus_to_the_state_line(TenantCommandAuditState refreshedState)
    {
        RegisterLocalizer();
        IRenderedComponent<AuditAvailabilityState>? cut = null;
        cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, () =>
                cut!.Render(p => p.Add(component => component.AuditState, refreshedState))));

        cut.Find("[data-recovery-verb='refresh']").Click();

        cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
        ElementReference focused = JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeOfType<ElementReference>();
        focused.Id.ShouldBe(CapturedStateLineReference(cut.Instance).Id);
    }

    [Fact]
    public void A_refresh_that_keeps_a_refresh_control_leaves_focus_on_it()
    {
        RegisterLocalizer();
        IRenderedComponent<AuditAvailabilityState>? cut = null;
        cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, () =>
                cut!.Render(p => p.Add(component => component.AuditState, TenantCommandAuditState.AuditDelayed))));

        cut.Find("[data-recovery-verb='refresh']").Click();

        cut.Find("[data-recovery-verb='refresh']");
        JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(0);
    }

    [Fact]
    public void A_state_change_resets_the_unchanged_retry_count()
    {
        RegisterLocalizer();
        int refreshCount = 0;
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, () => refreshCount++));

        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.Find("[data-recovery-verb='refresh']").Click();

        cut.Render(parameters => parameters.Add(component => component.AuditState, TenantCommandAuditState.AuditDelayed));
        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.Find("[data-recovery-verb='refresh']").Click();

        refreshCount.ShouldBe(4);
        cut.Find("[data-recovery-verb='refresh']");
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();

        cut.Find("[data-recovery-verb='refresh']").Click();
        cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-availability-retry-limit']");
    }

    [Fact]
    public void A_refresh_that_changes_the_state_does_not_count_toward_the_limit()
    {
        RegisterLocalizer();
        IRenderedComponent<AuditAvailabilityState>? cut = null;
        TenantCommandAuditState[] sequence =
        [
            TenantCommandAuditState.AuditUnavailable,
            TenantCommandAuditState.AuditDelayed,
            TenantCommandAuditState.AuditUnavailable,
            TenantCommandAuditState.AuditDelayed,
        ];
        int step = 0;
        cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, sequence[0])
            .Add(component => component.OnRefresh, () =>
            {
                step++;
                cut!.Render(p => p.Add(component => component.AuditState, sequence[step]));
            }));

        for (int attempt = 0; attempt < sequence.Length - 1; attempt++)
        {
            cut.Find("[data-recovery-verb='refresh']").Click();
        }

        cut.Find("[data-recovery-verb='refresh']");
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
    }

    [Fact]
    public void The_live_region_holds_only_state_and_explanation_and_identical_renders_do_not_reannounce()
    {
        RegisterLocalizer();
        RegisterComposition(EscalationHref);

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnInspectAudit, () => { })
            .Add(component => component.OnContinueReadOnly, () => { }));

        cut.FindAll("[aria-live]").ShouldHaveSingleItem();
        IElement announcement = cut.Find("[data-testid='tenants-audit-availability-announcement']");
        announcement.GetAttribute("aria-atomic").ShouldBe("true");
        announcement.QuerySelector("[data-testid='tenants-audit-availability-state']").ShouldNotBeNull();
        announcement.QuerySelector("[data-testid='tenants-audit-availability-explanation']").ShouldNotBeNull();
        announcement.QuerySelectorAll("[data-recovery-verb], fluent-button, fluent-anchor-button, a").ShouldBeEmpty();
        string announced = announcement.TextContent;
        foreach (string actionLabel in new[] { "Retry status lookup", "Continue read-only", "Inspect audit", "Escalate without diagnostics" })
        {
            announced.ShouldNotContain(actionLabel, Case.Insensitive);
        }

        // Keep the same live-region nodes, including text nodes; replacing them with identical markup would
        // still cause assistive technology to see new announcement content.
        string announcedMarkup = announcement.OuterHtml;
        string markup = cut.Markup;
        INode[] originalNodes = CaptureNodes(announcement);
        for (int render = 0; render < 2; render++)
        {
            cut.Render(parameters => parameters.Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable));
            INode[] currentNodes = CaptureNodes(cut.Find("[data-testid='tenants-audit-availability-announcement']"));
            currentNodes.Length.ShouldBe(originalNodes.Length);
            for (int node = 0; node < originalNodes.Length; node++)
            {
                currentNodes[node].ShouldBeSameAs(originalNodes[node]);
            }
        }

        cut.Find("[data-testid='tenants-audit-availability-announcement']").OuterHtml.ShouldBe(announcedMarkup);
        cut.Markup.ShouldBe(markup);

        static INode[] CaptureNodes(INode node)
        {
            INode unwrapped = node is IElementWrapper<IElement> wrapper ? wrapper.WrappedElement : node;
            return [unwrapped, .. unwrapped.ChildNodes.SelectMany(CaptureNodes)];
        }
    }

    [Fact]
    public void Receipt_audit_reads_explain_the_failed_read_without_naming_a_recovery()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.IsReceiptAuditRead, true));

        string explanation = cut.Find("[data-testid='tenants-audit-availability-explanation']").TextContent;
        explanation.ShouldBe(Values["Tenants.Audit.Receipt.Availability.Unavailable.Reason"]);
        foreach (string verb in RecoveryVerbWords)
        {
            explanation.ShouldNotContain(verb, Case.Insensitive);
        }
    }

    [Fact]
    public void Source_is_rendered_as_a_stable_data_attribute()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.Source, "edit-metadata"));

        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-audit-source").ShouldBe("edit-metadata");

        IRenderedComponent<AuditAvailabilityState> unsourced = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending));
        unsourced.Find("[data-testid='tenants-audit-availability']").HasAttribute("data-audit-source").ShouldBeFalse();
    }

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "Audit en attente")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "Audit retardé")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "Audit indisponible")]
    [InlineData(TenantCommandAuditState.AuditAvailable, "Audit disponible")]
    [InlineData(TenantCommandAuditState.MissingSupport, "Support d’implémentation manquant")]
    public void French_culture_renders_accented_whole_strings_while_selectors_stay_culture_independent(
        TenantCommandAuditState auditState,
        string expectedLabel)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            Services.AddLocalization();

            IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
                .Add(component => component.AuditState, auditState)
                .Add(component => component.OnInspectAudit, () => { }));

            cut.Find("[data-testid='tenants-audit-availability-badge']").TextContent.Trim().ShouldBe(expectedLabel);
            cut.Find("[data-testid='tenants-audit-availability-recovery-inspectaudit']")
                .TextContent.Trim().ShouldBe("Inspecter l’audit");
            cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state")
                .ShouldBe(TenantAuditAvailability.FromCommandAuditState(auditState).State!.Value.ToString().ToLowerInvariant());
            if (auditState is not TenantCommandAuditState.AuditAvailable)
            {
                string explanation = cut.Find("[data-testid='tenants-audit-availability-explanation']").TextContent;
                explanation.ShouldNotStartWith("Tenants.");
                explanation.ShouldNotContain("Tenants.Audit", Case.Sensitive);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void Suspended_recovery_refresh_respects_owner_capability_probe_ownership(
        bool ownerStartsAuthorityProbe,
        int expectedCascadeProbes)
    {
        RegisterLocalizer();
        var owner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int ownerRefreshes = 0;
        int ownerCompletions = 0;
        int cascadedProbes = 0;
        long ownerProbeVersion = 0;
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(p => p.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(p => p.OwnerRefreshIncludesAuditAuthority, ownerStartsAuthorityProbe)
            .Add(p => p.OnRefresh, EventCallback.Factory.Create(this, async () =>
            {
                ownerRefreshes++;
                await owner.Task.ConfigureAwait(false);
                if (ownerStartsAuthorityProbe)
                {
                    Interlocked.Increment(ref ownerProbeVersion);
                }
                ownerCompletions++;
            })));
        cut.Instance.AuditAuthorityRefreshVersion = () => Volatile.Read(ref ownerProbeVersion);
        cut.Instance.AuditAuthorityRefresh = () =>
        {
            cascadedProbes++;
            return Task.CompletedTask;
        };

        cut.Find("[data-recovery-verb='refresh']").Click();
        ownerRefreshes.ShouldBe(1);
        cascadedProbes.ShouldBe(0);
        owner.SetResult();

        cut.WaitForAssertion(() => ownerCompletions.ShouldBe(1));
        cut.WaitForAssertion(() => cascadedProbes.ShouldBe(expectedCascadeProbes));
    }

    [Fact]
    public void Terminal_owner_refresh_that_skips_projection_retries_audit_capability_once()
    {
        RegisterLocalizer();
        var owner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int cascadedProbes = 0;
        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(p => p.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(p => p.OwnerRefreshIncludesAuditAuthority, true)
            .Add(p => p.OnRefresh, EventCallback.Factory.Create(this, () => owner.Task)));
        cut.Instance.AuditAuthorityRefreshVersion = () => 0;
        cut.Instance.AuditAuthorityRefresh = () =>
        {
            cascadedProbes++;
            return Task.CompletedTask;
        };

        cut.Find("[data-recovery-verb='refresh']").Click();
        cascadedProbes.ShouldBe(0);
        owner.SetResult();
        cut.WaitForAssertion(() => cascadedProbes.ShouldBe(1));
    }

    [Fact]
    public void Recovery_actions_are_native_keyboard_operable_fluent_controls()
    {
        RegisterLocalizer();
        RegisterComposition(EscalationHref);

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnContinueReadOnly, () => { })
            .Add(component => component.OnInspectAudit, () => { }));

        cut.Find("[data-testid='tenants-audit-availability-actions']").GetAttribute("aria-label")
            .ShouldBe("Audit availability recovery actions");
        cut.Find("[data-testid='tenants-audit-availability-actions']").GetAttribute("role")
            .ShouldBe("group");
        foreach (string verb in new[] { "refresh", "continuereadonly", "inspectaudit", "escalate" })
        {
            IElement action = cut.Find($"[data-recovery-verb='{verb}']");

            action.NodeName.ShouldBeOneOf("FLUENT-BUTTON", "FLUENT-ANCHOR-BUTTON");
            action.TextContent.ShouldNotBeNullOrWhiteSpace();
            action.HasAttribute("disabled").ShouldBeFalse();
            action.HasAttribute("tabindex").ShouldBeFalse();
        }
    }

    [Fact]
    public void Inspect_audit_reuses_the_host_entry_point_inside_a_stable_recovery_shell()
    {
        RegisterLocalizer();
        RenderFragment inspectAuditAction = builder =>
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "href", "/tenants/tenant.alpha/audit?source=command-result");
            builder.AddAttribute(2, "data-testid", "tenants-command-audit-entrypoint");
            builder.AddContent(3, "Inspect audit");
            builder.CloseElement();
        };

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditDelayed)
            .Add(component => component.InspectAuditAction, inspectAuditAction));

        IElement shell = cut.Find("[data-testid='tenants-audit-availability-recovery-inspectaudit']");
        shell.QuerySelector("[data-testid='tenants-command-audit-entrypoint']").ShouldNotBeNull()
            .GetAttribute("href").ShouldBe("/tenants/tenant.alpha/audit?source=command-result");
    }

    [Fact]
    public void Every_command_flow_hosts_the_shared_control_with_a_distinct_source_and_no_duplicate_state_copy()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Tenants");
        string[] flows =
        [
            "CreateTenantFlow.razor",
            Path.Combine("Members", "AddTenantMemberFlow.razor"),
            Path.Combine("Members", "ChangeTenantMemberRoleFlow.razor"),
            Path.Combine("Members", "RemoveTenantMemberFlow.razor"),
            Path.Combine("Metadata", "EditTenantMetadataFlow.razor"),
            Path.Combine("Lifecycle", "TenantLifecycleCommandFlow.razor"),
            Path.Combine("Configuration", "SetTenantConfigurationFlow.razor"),
            Path.Combine("Configuration", "RemoveTenantConfigurationFlow.razor"),
        ];
        HashSet<string> sources = new(StringComparer.Ordinal);

        foreach (string flow in flows)
        {
            string source = File.ReadAllText(Path.Combine(componentsRoot, flow));
            System.Text.RegularExpressions.Match control = System.Text.RegularExpressions.Regex.Match(
                source,
                "<AuditAvailabilityState AuditState=\"@_snapshot\\.AuditState\"\\s+Source=\"(?<source>[a-z-]+)\"");
            control.Success.ShouldBeTrue($"{flow} must host the shared control and pass its Source.");
            sources.Add(control.Groups["source"].Value).ShouldBeTrue($"{flow} reuses another flow's Source.");

            source.ShouldContain("\"Tenants.Audit.EntryPoint.Accessible.Command\", AuditStateLabel,", Case.Sensitive, flow);

            // The Inspect-audit recovery shows the canonical verb, never the "Audit evidence" noun.
            source.ShouldContain("Label=\"@Localizer[\"Tenants.Audit.Availability.Action.InspectAudit\"]\"", Case.Sensitive, flow);
            source.ShouldNotContain("Tenants.Audit.EntryPoint.Label", Case.Sensitive, flow);
            source.ShouldContain("TenantAuditAvailability.StateLabelKeyFor(_snapshot.AuditState)", Case.Sensitive, flow);
            source.ShouldNotContain("AvailabilityText", Case.Sensitive, flow);
            source.ShouldContain("InspectAuditAction=\"@CommandAuditEntryPoint\"", Case.Sensitive, flow);
            source.ShouldContain("=> AuditReadDenied ? null : CommandAuditEntryPointTemplate;", Case.Sensitive, flow);
            source.ShouldNotContain("<InspectAuditAction>", Case.Sensitive, flow);
            source.ShouldContain("OnRefresh=\"@(CanRequeryAudit ? EventCallback.Factory.Create(this, ", Case.Sensitive, flow);
            source.ShouldNotContain("AuditText", Case.Sensitive, flow);
            System.Text.RegularExpressions.Regex.IsMatch(source, "\"Tenants\\.[A-Za-z.]+\\.Audit\\.\\{").ShouldBeFalse(flow);
        }

        sources.Count.ShouldBe(flows.Length);
    }

    [Fact]
    public void Rendered_class_hooks_match_the_390px_browser_fixture()
    {
        RegisterLocalizer();
        RegisterComposition(EscalationHref);
        RenderFragment inspectAuditAction = builder =>
        {
            builder.OpenElement(0, "fluent-anchor-button");
            builder.AddAttribute(1, "class", "tenants-audit-entrypoint");
            builder.AddAttribute(2, "href", "/tenants/tenant.alpha/audit");
            builder.AddContent(3, "Audit evidence");
            builder.CloseElement();
        };

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnContinueReadOnly, () => { })
            .Add(component => component.InspectAuditAction, inspectAuditAction));

        // The rendered structure the browser fixture reproduces: a vertical announcement stack, a horizontal
        // action stack whose direct children are the four recoveries, and the class hooks the CSS targets.
        IElement root = cut.Find("[data-testid='tenants-audit-availability']");
        IElement announcement = cut.Find("[data-testid='tenants-audit-availability-announcement']");
        IElement badge = cut.Find("[data-testid='tenants-audit-availability-badge']");
        IElement actions = cut.Find("[data-testid='tenants-audit-availability-actions']");
        actions.GetAttribute("role").ShouldBe("group");
        IElement[] recoveries = [.. actions.Children];
        recoveries.Select(action => action.GetAttribute("data-recovery-verb"))
            .ShouldBe(["refresh", "continuereadonly", "inspectaudit", "escalate"]);
        foreach (IElement recovery in recoveries.Where(action => action.GetAttribute("data-recovery-verb") != "inspectaudit"))
        {
            recovery.LocalName.ShouldBeOneOf("fluent-button", "fluent-anchor-button");
            recovery.GetAttribute("class").ShouldBe("tenants-audit-availability__action");
        }

        IElement shell = recoveries.Single(action => action.GetAttribute("data-recovery-verb") == "inspectaudit");
        shell.GetAttribute("class").ShouldBe("tenants-audit-availability__action-shell");
        shell.Children.ShouldHaveSingleItem().ClassList.ShouldContain("tenants-audit-entrypoint");

        string harness = File.ReadAllText(Path.Combine(
            ProjectRoot(), "tests", "Hexalith.Tenants.UI.Tests", "Browser", "tenants-focus-browser-validation.html"));
        string fixture = harness[harness.IndexOf("<section id=\"availability-fixture\"", StringComparison.Ordinal)..
            harness.IndexOf("<output id=\"validation-report\">", StringComparison.Ordinal)];
        fixture.ShouldContain($"class=\"{root.GetAttribute("class")}\"");
        fixture.ShouldContain($"data-state=\"{root.GetAttribute("data-state")}\"");
        fixture.ShouldContain($"class=\"{announcement.GetAttribute("class")}\"");
        fixture.ShouldContain($"aria-live=\"{announcement.GetAttribute("aria-live")}\"");
        fixture.ShouldContain($"class=\"{badge.GetAttribute("class")}\"");
        fixture.ShouldContain($"color=\"{badge.GetAttribute("color")}\"");
        fixture.ShouldContain($"class=\"{actions.GetAttribute("class")}\"");
        IElement fixtureActions = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(fixture)
            .QuerySelector("#availability-actions")!;
        fixtureActions.GetAttribute("role").ShouldBe(actions.GetAttribute("role"));
        fixture.ShouldContain("class=\"tenants-audit-availability__action-shell\"");
        fixture.ShouldContain("class=\"tenants-audit-entrypoint\"");
        foreach (IElement recovery in recoveries)
        {
            fixture.ShouldContain($"data-recovery-verb=\"{recovery.GetAttribute("data-recovery-verb")}\"");
        }

        System.Text.RegularExpressions.Regex.Count(fixture, "class=\"tenants-audit-availability__action\"").ShouldBe(3);

        // One coherent state: the fixture carries only the unavailable copy (French), never another state's.
        fixture.ShouldContain("Audit indisponible");
        fixture.ShouldNotContain("Support d’implémentation manquant");
        fixture.ShouldNotContain("missingsupport");
    }

    [Fact]
    public void BrowserFixtureRecoveryNameMatchesProductionFrenchResources()
    {
        CultureInfo previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            Services.AddLocalization();
            IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
                .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
                .Add(component => component.OnRefresh, () => { }));
            string name = cut.Find("[data-testid='tenants-audit-availability-actions']")
                .GetAttribute("aria-label")!;
            name.ShouldNotBeNullOrWhiteSpace();
            string harness = File.ReadAllText(Path.Combine(
                ProjectRoot(), "tests", "Hexalith.Tenants.UI.Tests", "Browser", "tenants-focus-browser-validation.html"));
            IElement fixtureActions = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(harness)
                .QuerySelector("#availability-actions")!;

            fixtureActions.GetAttribute("aria-label").ShouldBe(name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public void Availability_css_keeps_badges_stacking_forced_colors_focus_and_stable_dimensions()
    {
        string css = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditAvailabilityState.razor.css"));

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain("@media (max-width: 767px)");
        css.ShouldContain(":focus-visible");
        css.ShouldContain("min-height: 2rem");

        // Fluent component roots never receive the scope attribute: badge, action-stack, and action rules
        // must be anchored on the plain-HTML root with ::deep or they silently never apply.
        css.ShouldContain(".tenants-audit-availability ::deep .tenants-audit-availability__badge");
        css.ShouldContain(".tenants-audit-availability ::deep .tenants-audit-availability__actions");
        css.ShouldContain(".tenants-audit-availability ::deep .tenants-audit-availability__action");
        css.ShouldContain("flex-direction: column !important");
        css.ShouldContain(".tenants-audit-availability__action-shell ::deep .tenants-audit-entrypoint");
        css.ShouldNotContain(".tenants-audit-availability__icon");
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static ElementReference CapturedStateLineReference(AuditAvailabilityState component)
        => (ElementReference)(typeof(AuditAvailabilityState)
            .GetField("_stateLineElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(component)
            ?? throw new InvalidOperationException("The state line reference was not captured."));

    private void RegisterLocalizer()
        => Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

    private void RegisterComposition(string? escalationHref)
        => Services.AddSingleton<ITenantsBffComposition>(new EscalationComposition(escalationHref));

    private static readonly Dictionary<TenantCommandAuditState, string> Explanations = new()
    {
        [TenantCommandAuditState.AuditPending] = "The command's events are stored, but its audit record is not readable yet. It normally appears shortly, and no proof is claimed until it does.",
        [TenantCommandAuditState.AuditDelayed] = "The audit record is taking longer than expected to become readable. No proof is claimed until it can be read.",
        [TenantCommandAuditState.AuditUnavailable] = "The audit status could not be read or verified after the command was sent. This does not mean the record does not exist, and no proof is claimed.",
        [TenantCommandAuditState.MissingSupport] = "In-panel audit verification is not available for this command, so this panel cannot match an audit record to the attempt. The recorded outcome above is unchanged.",
    };

    // Exact shipped English values: the suite-wide localizer-double parity gate rejects any divergence.
    private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
    {
        ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
        ["Tenants.Audit.Recovery.Action.Escalate"] = "Escalate without diagnostics",
        ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
        ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
        ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
        ["Tenants.Audit.Availability.RetryLimit"] = "Repeated retries left this state unchanged, so retrying is no longer offered here.",
        ["Tenants.Audit.Availability.Reason.Pending"] = "The command's events are stored, but its audit record is not readable yet. It normally appears shortly, and no proof is claimed until it does.",
        ["Tenants.Audit.Availability.Reason.Delayed"] = "The audit record is taking longer than expected to become readable. No proof is claimed until it can be read.",
        ["Tenants.Audit.Availability.Reason.Unavailable"] = "The audit status could not be read or verified after the command was sent. This does not mean the record does not exist, and no proof is claimed.",
        ["Tenants.Audit.Availability.Reason.MissingSupport"] = "In-panel audit verification is not available for this command, so this panel cannot match an audit record to the attempt. The recorded outcome above is unchanged.",
        ["Tenants.Audit.Receipt.Availability.Unavailable.Reason"] = "This audit read could not verify the requested evidence. This does not mean the record does not exist, and the recorded outcome is unchanged.",
        ["Tenants.Audit.Availability.State.Available"] = "Audit available",
        ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
        ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
        ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
        ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
    };

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));
    }

    private sealed class EscalationComposition(string? escalationHref) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public string? AuditEscalationRecoveryHref => escalationHref;
    }
}
