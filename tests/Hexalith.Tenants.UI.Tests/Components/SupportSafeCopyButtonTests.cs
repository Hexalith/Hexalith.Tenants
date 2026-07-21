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
            .TextContent.ShouldBe("Copy was canceled. Select the value and copy it manually."));
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
            ["Tenants.Copy.Feedback.Canceled"] = "Copy was canceled. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Insecure"] = "Clipboard is unavailable in this browser context. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.PermissionDenied"] = "Clipboard permission was not granted. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }

    private sealed class ControllableClipboardRuntime(bool delayImport, bool delayWrite) : IJSRuntime, IJSObjectReference
    {
        public TaskCompletionSource<bool> ImportRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ImportRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> WriteRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> WriteRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string> Writes { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => identifier switch
            {
                "import" => ImportAsync<TValue>(),
                "writeText" => WriteAsync<TValue>(args),
                _ => throw new JSException($"Unexpected JS invocation '{identifier}'."),
            };

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private async ValueTask<TValue> ImportAsync<TValue>()
        {
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
            if (delayWrite)
            {
                await WriteRelease.Task.ConfigureAwait(false);
            }

            return default!;
        }
    }
}
