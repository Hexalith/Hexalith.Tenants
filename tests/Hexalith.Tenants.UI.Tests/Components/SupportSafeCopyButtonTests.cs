using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.SupportSafety;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class SupportSafeCopyButtonTests : BunitContext
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
            .Add(button => button.AccessibleName, "Copy tenant identifier Tenant.Mixed-01")
            .Add(button => button.TestId, "tenants-list-copy-reference"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe("Tenant.Mixed-01");
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-list-copy-reference-feedback']").TextContent.ShouldContain("Copied"));
        cut.Find("[data-testid='tenants-list-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Theory]
    [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("Tenant.With.Mixed.Case-01")]
    public void Copy_button_preserves_guid_ulid_and_case_shaped_literals_exactly(string value)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", value).SetVoidResult();

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, value)
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.TenantId)
            .Add(button => button.AccessibleName, $"Copy tenant identifier {value}")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bearer raw-token")]
    [InlineData("System.InvalidOperationException: stack trace")]
    public void Copy_button_blocks_empty_and_unsafe_values_before_js_interop(string value)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText");

        IRenderedComponent<SupportSafeCopyButton> cut = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(button => button.Value, value)
            .Add(button => button.ValueKind, SupportSafeCopyValueKind.SafeConfigurationValue)
            .Add(button => button.AccessibleName, "Copy visible configuration value")
            .Add(button => button.TestId, "tenants-config-copy-reference"));

        cut.Find("button").Click();

        writeHandler.Invocations.ShouldBeEmpty();
        cut.Markup.ShouldNotContain("raw-token", Case.Insensitive);
        cut.Markup.ShouldNotContain("InvalidOperationException", Case.Insensitive);
        cut.Markup.ShouldNotContain("stack trace", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("assertive");
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
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Clipboard permission"));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("assertive");
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
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain(expectedFeedback));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("button").GetAttribute("aria-label").ShouldBe("Copy tenant identifier tenant.alpha");
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
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Copy failed"));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("Copied");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("assertive");
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
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Clipboard disconnected"));
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldNotContain("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("aria-live").ShouldBe("assertive");
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
            .Add(button => button.AccessibleName, "Copy tenant identifier tenant.alpha")
            .Add(button => button.TestId, "tenants-detail-copy-reference"));

        // The only interactive element is a real, type-safe button that keeps DOM focus on click,
        // and the feedback is a non-interactive live region, so copying never moves focus elsewhere.
        cut.FindAll("button").Count.ShouldBe(1);
        cut.Find("button").GetAttribute("type").ShouldBe("button");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").NodeName.ShouldBe("SPAN");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").HasAttribute("tabindex").ShouldBeFalse();

        cut.Find("button").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-detail-copy-reference-feedback']").TextContent.ShouldContain("Copied"));
        cut.FindAll("button").Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("Tenant.Mixed-01", SupportSafeCopyValueKind.TenantId)]
    [InlineData("USER/Case.Sensitive-01", SupportSafeCopyValueKind.UserId)]
    [InlineData("billing.mode", SupportSafeCopyValueKind.ConfigurationKey)]
    [InlineData("trial", SupportSafeCopyValueKind.SafeConfigurationValue)]
    public void Classifier_allows_support_safe_literals_without_guid_or_ulid_parsing(string value, SupportSafeCopyValueKind kind)
    {
        SupportSafeCopyClassifier.Classify(value, kind).ShouldBe(SupportSafeCopyEligibility.Allowed);
    }

    [Theory]
    [InlineData("billing.connectionString", SupportSafeCopyValueKind.ConfigurationKey)]
    [InlineData("secret-value", SupportSafeCopyValueKind.SafeConfigurationValue)]
    [InlineData("EventStore metadata raw cursor", SupportSafeCopyValueKind.ApprovedReference)]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.raw.payload", SupportSafeCopyValueKind.SafeConfigurationValue)]
    public void Classifier_blocks_payload_token_metadata_and_sensitive_configuration_values(string value, SupportSafeCopyValueKind kind)
    {
        SupportSafeCopyClassifier.Classify(value, kind).ShouldBe(SupportSafeCopyEligibility.Unsafe);
    }

    [Theory]
    [InlineData(SupportSafeCopyValueKind.TenantId)]
    [InlineData(SupportSafeCopyValueKind.UserId)]
    public void Classifier_blocks_raw_jwt_miswired_into_identifier_copy(SupportSafeCopyValueKind kind)
    {
        // A raw JWT contains no contiguous "jwt" text, only the base64url "eyJ" header marker;
        // the identifier deny-list must still fail closed on it.
        SupportSafeCopyClassifier
            .Classify("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", kind)
            .ShouldBe(SupportSafeCopyEligibility.Unsafe);
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
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed.",
            ["Tenants.Copy.Feedback.Insecure"] = "Clipboard is unavailable in this browser context.",
            ["Tenants.Copy.Feedback.PermissionDenied"] = "Clipboard permission was not granted.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }
}
