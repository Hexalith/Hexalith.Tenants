using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.SupportSafety;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class SupportSafeCopyButtonTests : FluentBunitContext
{
    [Fact]
    public void Copy_button_writes_literal_value_without_identifier_normalization()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", "Tenant.Mixed-01").SetVoidResult();

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "Tenant.Mixed-01")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier Tenant.Mixed-01")
            .Add(button => button.TestId, "tenants-list-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe("Tenant.Mixed-01");
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-list-copy-reference-feedback']").TextContent.ShouldContain("Copied"));
        cut.Find("[data-testid='tenants-list-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Theory]
    [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("Tenant.With.Mixed.Case-01")]
    [InlineData("  tenant/%2F?x=é&glyph=о  ")]
    [InlineData("tenant-cursor-metadata-etag-jwt-payload")]
    public void Copy_button_preserves_approved_identifier_literals_exactly(string value)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", value).SetVoidResult();

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, value)
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, $"Copy tenant identifier {value}")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bearer raw-token")]
    public void Copy_button_omits_unapproved_or_empty_values_before_js_interop(string value)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText");

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, value)
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.SafeConfigurationValue)
            .Add(button => button.IsApproved, false)
            .Add(button => button.AccessibleName, "Copy visible configuration value")
            .Add(button => button.TestId, "tenants-config-copy-reference"));

        writeHandler.Invocations.ShouldBeEmpty();
        cut.Markup.ShouldBeEmpty();
        cut.Markup.ShouldNotContain("raw-token", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-copy-reference']").ShouldBeEmpty();
        cut.FindAll("[role='status']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Copy_button_omits_values_without_an_accessible_name(string? accessibleName)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "tenant.alpha")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, accessibleName!));

        cut.Markup.ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Copy_button_reports_js_failure_without_false_success()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetException(new JSException("TENANTS_CLIPBOARD_NOT_ALLOWED"));

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "tenant.alpha")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Clipboard permission"));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Select the value and copy it manually");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-atomic").ShouldBe("true");
    }

    [Theory]
    [InlineData("TENANTS_CLIPBOARD_INSECURE", "browser context")]
    [InlineData("TENANTS_CLIPBOARD_MISSING", "Clipboard unavailable")]
    public void Copy_button_reports_unavailable_clipboard_states_without_false_success(string errorCode, string expectedFeedback)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetException(new JSException(errorCode));

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "tenant.alpha")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain(expectedFeedback));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Select the value and copy it manually");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-atomic").ShouldBe("true");
        cut.Find("[data-testid='tenants-copy-reference']").GetAttribute("aria-label").ShouldBe("Copy tenant identifier tenant.alpha");
    }

    [Fact]
    public void Copy_button_reports_unexpected_js_failure_without_false_success()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetException(new JSException("TENANTS_CLIPBOARD_FAILED"));

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "tenant.alpha")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Copy failed"));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Select the value and copy it manually");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Copy_button_reports_disconnected_circuit_without_value_disclosure()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetException(new JSDisconnectedException("Circuit disconnected"));

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "tenant.alpha")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Clipboard disconnected"));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Select the value and copy it manually");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Copy_button_reports_canceled_interop_as_a_non_success_recovery()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetException(new OperationCanceledException());

        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']")
            .TextContent.ShouldBe("Copy could not be completed. Select the value and copy it manually."));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Repeated_identical_copy_outcomes_are_republished_through_the_live_region(bool succeeds)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", "tenant.alpha");
        if (succeeds)
        {
            writeHandler.SetVoidResult();
        }
        else
        {
            writeHandler.SetException(new JSException("TENANTS_CLIPBOARD_FAILED"));
        }

        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");
        cut.Find("[data-testid='tenants-copy-reference']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        int firstOutcomeRender = cut.RenderCount;

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() =>
        {
            writeHandler.Invocations.Count.ShouldBe(2);
            cut.RenderCount.ShouldBeGreaterThanOrEqualTo(firstOutcomeRender + 2);
        });
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent
            .ShouldBe(succeeds ? "Copied." : "Copy failed. Select the value and copy it manually.");
    }

    [Fact]
    public async Task Input_change_while_module_import_is_pending_aborts_before_clipboard_write()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: true, delayWrite: false);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        Task activation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        await runtime.ImportRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Render(parameters => parameters
            .Add(button => button.Value, "tenant.beta")
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.beta"));
        runtime.ImportRelease.SetResult(true);
        await activation;

        runtime.Writes.ShouldBeEmpty();
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Disposal_waits_for_pending_import_and_disposes_resolved_module_once()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: true, delayWrite: false);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        Task activation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        await runtime.ImportRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Task disposal = cut.Instance.DisposeAsync().AsTask();
        await Task.Yield();
        disposal.IsCompleted.ShouldBeFalse();

        await cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        runtime.ImportCount.ShouldBe(1);

        runtime.ImportRelease.SetResult(true);
        await Task.WhenAll(activation, disposal);

        runtime.Writes.ShouldBeEmpty();
        runtime.DisposeCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();

        await cut.Instance.DisposeAsync();
        runtime.DisposeCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("approval")]
    [InlineData("kind")]
    [InlineData("accessible-name")]
    public async Task Eligibility_change_while_module_import_is_pending_aborts_before_clipboard_write(string changedInput)
    {
        var runtime = new ControllableClipboardRuntime(delayImport: true, delayWrite: false);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        Task activation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        await runtime.ImportRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        switch (changedInput)
        {
            case "approval":
                cut.Render(parameters => parameters.Add(button => button.IsApproved, false));
                break;
            case "kind":
                cut.Render(parameters => parameters.Add(button => button.ValueKind, SupportSafeCopyValueKind.Unknown));
                break;
            case "accessible-name":
                cut.Render(parameters => parameters.Add(button => button.AccessibleName, string.Empty));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changedInput), changedInput, null);
        }

        runtime.ImportRelease.SetResult(true);
        await activation;

        runtime.Writes.ShouldBeEmpty();
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public async Task Input_change_while_write_is_pending_does_not_publish_stale_feedback()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: false, delayWrite: true);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        Task activation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        await runtime.WriteRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Render(parameters => parameters
            .Add(button => button.Value, "tenant.beta")
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.beta"));
        runtime.WriteRelease.SetResult(true);
        await activation;

        runtime.Writes.ShouldBe(["tenant.alpha"]);
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Disposal_waits_for_pending_write_disposes_module_once_and_suppresses_feedback()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: false, delayWrite: true);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        Task activation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        await runtime.WriteRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Task disposal = cut.Instance.DisposeAsync().AsTask();
        await Task.Yield();
        disposal.IsCompleted.ShouldBeFalse();

        runtime.WriteRelease.SetResult(true);
        await Task.WhenAll(activation, disposal);

        runtime.Writes.ShouldBe(["tenant.alpha"]);
        runtime.DisposeCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();

        await cut.Instance.DisposeAsync();
        runtime.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Activation_after_disposal_completes_starts_no_clipboard_interop()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: false, delayWrite: false);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        await cut.Instance.DisposeAsync();
        await cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());

        runtime.ImportCount.ShouldBe(0);
        runtime.Writes.ShouldBeEmpty();
        runtime.DisposeCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Concurrent_disposal_callers_wait_for_one_module_disposal()
    {
        var runtime = new ControllableClipboardRuntime(
            delayImport: false,
            delayWrite: false,
            delayDispose: true);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");
        await cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());

        Task firstDisposal = cut.Instance.DisposeAsync().AsTask();
        await runtime.DisposeRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task secondDisposal = cut.Instance.DisposeAsync().AsTask();

        firstDisposal.IsCompleted.ShouldBeFalse();
        secondDisposal.IsCompleted.ShouldBeFalse();
        runtime.DisposeRelease.SetResult(true);
        await Task.WhenAll(firstDisposal, secondDisposal);

        runtime.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Unexpected_module_disposal_failure_faults_every_disposal_caller_consistently()
    {
        var runtime = new ControllableClipboardRuntime(
            delayImport: false,
            delayWrite: false,
            disposeException: new InvalidOperationException("sensitive-disposal-detail"));
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");
        await cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());

        Task firstDisposal = cut.Instance.DisposeAsync().AsTask();
        Task secondDisposal = cut.Instance.DisposeAsync().AsTask();
        InvalidOperationException firstFailure = await Should.ThrowAsync<InvalidOperationException>(firstDisposal);
        InvalidOperationException secondFailure = await Should.ThrowAsync<InvalidOperationException>(secondDisposal);
        InvalidOperationException subsequentFailure = await Should.ThrowAsync<InvalidOperationException>(
            cut.Instance.DisposeAsync().AsTask());

        secondFailure.ShouldBeSameAs(firstFailure);
        subsequentFailure.ShouldBeSameAs(firstFailure);
        runtime.DisposeCount.ShouldBe(1);
        cut.Markup.ShouldNotContain("sensitive-disposal-detail", Case.Insensitive);
    }

    [Theory]
    [InlineData("js")]
    [InlineData("canceled")]
    public async Task Known_module_teardown_failures_complete_without_disclosing_details(string failureKind)
    {
        Exception exception = failureKind is "js"
            ? new JSException("sensitive-js-teardown-detail")
            : new OperationCanceledException("sensitive-cancellation-detail");
        var runtime = new ControllableClipboardRuntime(
            delayImport: false,
            delayWrite: false,
            disposeException: exception);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");
        await cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();

        runtime.DisposeCount.ShouldBe(1);
        cut.Markup.ShouldNotContain("sensitive-", Case.Insensitive);
    }

    [Fact]
    public async Task Rapid_overlapping_activations_issue_only_one_clipboard_write()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: false, delayWrite: true);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        Task firstActivation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        await runtime.WriteRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task secondActivation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        runtime.WriteRelease.SetResult(true);
        await Task.WhenAll(firstActivation, secondActivation);

        runtime.Writes.ShouldBe(["tenant.alpha"]);
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBe("Copied.");
    }

    [Fact]
    public async Task New_activation_clears_the_previous_result_while_clipboard_write_is_pending()
    {
        var runtime = new ControllableClipboardRuntime(delayImport: false, delayWrite: true, delayWriteFromInvocation: 2);
        Services.AddSingleton<IJSRuntime>(runtime);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");

        await cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBe("Copied.");

        Task secondActivation = cut.Find("[data-testid='tenants-copy-reference']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => runtime.Writes.Count.ShouldBe(2));

        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();
        runtime.WriteRelease.SetResult(true);
        await secondActivation;
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBe("Copied.");
    }

    [Fact]
    public void Input_identity_change_clears_prior_feedback()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetVoidResult();
        IRenderedComponent<SupportSafeCopyButton> cut = RenderApprovedButton("tenant.alpha");
        cut.Find("[data-testid='tenants-copy-reference']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBe("Copied."));

        cut.Render(parameters => parameters
            .Add(button => button.Value, "tenant.beta")
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.beta"));

        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldBeEmpty();
    }

    [Fact]
    public void Copy_button_uses_focus_preserving_button_semantics()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        module.SetupVoid("writeText", "tenant.alpha").SetVoidResult();

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, "tenant.alpha")
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        // The only interactive element is a single Fluent button (rendered as <fluent-button>) that
        // keeps DOM focus on click, and the feedback is a non-interactive live region, so copying
        // never moves focus elsewhere. Asserting the node name also guards against a raw-HTML regression.
        cut.FindAll("[data-testid='tenants-copy-reference']").Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-copy-reference']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").NodeName.ShouldBe("SPAN");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-atomic").ShouldBe("true");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").HasAttribute("tabindex").ShouldBeFalse();

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Copied"));
        cut.FindAll("[data-testid='tenants-copy-reference']").Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("Tenant.Mixed-01", SupportSafeCopyValueKind.TenantId)]
    [InlineData("USER/Case.Sensitive-01", SupportSafeCopyValueKind.UserId)]
    [InlineData("billing.connectionString", SupportSafeCopyValueKind.ConfigurationKey)]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.raw.payload", SupportSafeCopyValueKind.SafeConfigurationValue)]
    public void Classifier_allows_any_non_empty_literal_only_when_explicitly_approved(string value, SupportSafeCopyValueKind kind)
    {
        SupportSafeCopyClassifier.Classify(value, kind, isApproved: true).ShouldBe(SupportSafeCopyEligibility.Allowed);
    }

    [Theory]
    [InlineData(false, SupportSafeCopyValueKind.TenantId)]
    [InlineData(false, SupportSafeCopyValueKind.ApprovedReference)]
    [InlineData(true, SupportSafeCopyValueKind.Unknown)]
    public void Classifier_fails_closed_without_both_explicit_approval_and_non_default_kind(
        bool isApproved,
        SupportSafeCopyValueKind kind)
    {
        SupportSafeCopyClassifier.Classify("tenant.alpha", kind, isApproved).ShouldBe(SupportSafeCopyEligibility.Unsafe);
    }

    [Fact]
    public void Classifier_fails_closed_for_undefined_future_value_kinds()
    {
        SupportSafeCopyClassifier.Classify(
            "tenant.alpha",
            (SupportSafeCopyValueKind)999,
            isApproved: true).ShouldBe(SupportSafeCopyEligibility.Unsafe);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Classifier_treats_whitespace_only_values_as_empty(string? value)
    {
        SupportSafeCopyClassifier.Classify(value, SupportSafeCopyValueKind.TenantId, isApproved: true)
            .ShouldBe(SupportSafeCopyEligibility.Empty);
    }

    [Fact]
    public void Copy_kind_and_eligibility_defaults_fail_closed()
    {
        ((int)SupportSafeCopyValueKind.Unknown).ShouldBe(0);
        ((int)SupportSafeCopyEligibility.Unsafe).ShouldBe(0);
    }

    [Fact]
    public void Copy_button_imposes_no_length_limit_on_an_approved_literal()
    {
        string value = $"  tenant/{new string('x', 4096)}/終  ";
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", value).SetVoidResult();

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, value)
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, "Copy tenant identifier")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("[data-testid='tenants-copy-reference']").Click();

        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe(value);
    }

    [Fact]
    public void Copy_classifier_source_does_not_parse_or_normalize_identifier_values()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string classifier = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "SupportSafety",
            "SupportSafeCopyClassifier.cs"));

        classifier.ShouldNotContain("Guid.TryParse");
        classifier.ShouldNotContain("Ulid.TryParse");
        classifier.ShouldNotContain("ToUpperInvariant");
        classifier.ShouldNotContain("ToLowerInvariant");
        classifier.ShouldNotContain(".Trim(");
        classifier.ShouldNotContain("UnsafeFragments");
        classifier.ShouldNotContain(".Contains(");

        string component = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Shared",
            "SupportSafeCopyButton.razor"));
        component.ShouldContain("_observedValue = IsEligible ? Value : string.Empty;");
        component.ShouldNotContain("_observedValue = Value;");

        File.Exists(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "SupportSafety",
            "SupportSafeCopyValueKind.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "SupportSafety",
            "SupportSafeCopyEligibility.cs")).ShouldBeTrue();
    }

    private IRenderedComponent<SupportSafeCopyButton> RenderApprovedButton(string value)
        => Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, value)
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.IsApproved, true)
            .Add(button => button.AccessibleName, $"Copy tenant identifier {value}")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Canceled"] = "Copy could not be completed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Insecure"] = "Clipboard is unavailable in this browser context. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.PermissionDenied"] = "Clipboard permission was not granted. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }

    private sealed class ControllableClipboardRuntime(
        bool delayImport,
        bool delayWrite,
        int delayWriteFromInvocation = 1,
        bool delayDispose = false,
        Exception? disposeException = null) : IJSRuntime, IJSObjectReference
    {
        public TaskCompletionSource<bool> ImportRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ImportRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> WriteRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> WriteRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> DisposeRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> DisposeRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string> Writes { get; } = [];

        public int DisposeCount { get; private set; }

        public int ImportCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => identifier switch
            {
                "import" => ImportAsync<TValue>(),
                "writeText" => WriteAsync<TValue>(args),
                _ => throw new JSException($"Unexpected JS invocation '{identifier}'."),
            };

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            DisposeRequested.TrySetResult(true);
            if (delayDispose)
            {
                await DisposeRelease.Task.ConfigureAwait(false);
            }

            if (disposeException is not null)
            {
                throw disposeException;
            }
        }

        private async ValueTask<TValue> ImportAsync<TValue>()
        {
            ImportCount++;
            ImportRequested.TrySetResult(true);
            if (delayImport)
            {
                await ImportRelease.Task.ConfigureAwait(false);
            }

            return (TValue)(object)this;
        }

        private async ValueTask<TValue> WriteAsync<TValue>(object?[]? args)
        {
            Writes.Add((string)args!.Single()!);
            WriteRequested.TrySetResult(true);
            if (delayWrite && Writes.Count >= delayWriteFromInvocation)
            {
                await WriteRelease.Task.ConfigureAwait(false);
            }

            return default!;
        }
    }
}
