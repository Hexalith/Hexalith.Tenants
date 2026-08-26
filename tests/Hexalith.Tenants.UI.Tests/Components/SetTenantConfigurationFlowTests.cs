using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class SetTenantConfigurationFlowTests : FluentBunitContext
{
    [Fact]
    public void Namespace_and_suffix_are_composed_once_with_literal_case_and_preview_is_ten_fact_redacted()
    {
        StubTenantCommandGateway gateway = RegisterServices();
        TenantSetConfigurationIntent? observed = null;
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["Billing"]),
            intent =>
            {
                observed = intent;
                return Preview(intent, TenantSetConfigurationCurrentState.Different);
            });

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key-suffix']").Change("Mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("Raw-Secret");

        observed.ShouldNotBeNull();
        observed.NamespacePrefix.ShouldBe("Billing");
        observed.KeySuffix.ShouldBe("Mode");
        observed.FullKey.ShouldBe("Billing.Mode");
        cut.FindAll("[data-testid='tenants-config-set-preview-item']").Count.ShouldBe(10);
        cut.Find("[data-testid='tenants-config-set-preview-key']").TextContent.ShouldBe("Billing.Mode");
        cut.Find("[data-testid='tenants-config-set-preview']").TextContent.ShouldNotContain("Raw-Secret", Case.Sensitive);
    }

    [Fact]
    public void Non_global_operator_selects_only_server_reflected_namespace()
    {
        StubTenantCommandGateway gateway = RegisterServices();
        TenantSetConfigurationIntent? observed = null;
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["billing", "security"]),
            intent =>
            {
                observed = intent;
                return Preview(intent, TenantSetConfigurationCurrentState.Absent);
            });

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-config-set-namespace", "security");
        cut.Find("[data-testid='tenants-config-set-key-suffix']").Change("Mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change("enabled");

        observed.ShouldNotBeNull().FullKey.ShouldBe("security.Mode");
        cut.Find("[data-testid='tenants-config-set-namespace']").TagName.ShouldBe("FLUENT-DROPDOWN");
    }

    [Fact]
    public void Global_administrator_enters_literal_namespace_without_normalization()
    {
        StubTenantCommandGateway gateway = RegisterServices();
        TenantSetConfigurationIntent? observed = null;
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["*"], globalAdministrator: true),
            intent =>
            {
                observed = intent;
                return Preview(intent, TenantSetConfigurationCurrentState.Absent);
            });

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-namespace']").Change("Custom.Space");
        cut.Find("[data-testid='tenants-config-set-key-suffix']").Change("Key");
        cut.Find("[data-testid='tenants-config-set-value']").Change("value");

        observed.ShouldNotBeNull().FullKey.ShouldBe("Custom.Space.Key");
        cut.Find("[data-testid='tenants-config-set-namespace']").TagName.ShouldBe("FLUENT-TEXT-INPUT");
    }

    [Theory]
    [InlineData(248, true)]
    [InlineData(249, false)]
    public void Full_key_enforces_256_character_boundary_after_single_composition(int suffixLength, bool valid)
    {
        StubTenantCommandGateway gateway = RegisterServices();
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["billing"]),
            intent => Preview(intent, TenantSetConfigurationCurrentState.Absent));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key-suffix']").Change(new string('k', suffixLength));
        cut.Find("[data-testid='tenants-config-set-value']").Change("value");

        cut.FindAll("[data-testid='tenants-config-set-preview']").Any().ShouldBe(valid);
    }

    [Theory]
    [InlineData(1024, true)]
    [InlineData(1025, false)]
    public void Value_enforces_1024_character_boundary(int valueLength, bool valid)
    {
        StubTenantCommandGateway gateway = RegisterServices();
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["billing"]),
            intent => Preview(intent, TenantSetConfigurationCurrentState.Absent));

        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key-suffix']").Change("mode");
        cut.Find("[data-testid='tenants-config-set-value']").Change(new string('v', valueLength));

        cut.FindAll("[data-testid='tenants-config-set-preview']").Any().ShouldBe(valid);
    }

    [Fact]
    public void Exact_matching_preview_is_already_applied_without_dispatch()
    {
        StubTenantCommandGateway gateway = RegisterServices();
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["billing"]),
            intent => Preview(intent, TenantSetConfigurationCurrentState.Matching));

        CompleteForm(cut, "mode", "secret");
        cut.Find("form").Submit();

        gateway.SetConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Markup.ShouldNotContain("secret", Case.Sensitive);
    }

    [Fact]
    public void Dispatch_uses_caller_message_id_aggregate_status_and_causal_projection_advancement()
    {
        StubTenantCommandGateway gateway = RegisterServices();
        string projectionVersion = "tenant-sequence:41";
        TenantCommandTrackingHandle? statusHandle = null;
        gateway.StatusAsync = handle =>
        {
            statusHandle = handle;
            return Task.FromResult(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));
        };
        IRenderedComponent<SetTenantConfigurationFlow> cut = RenderFlow(
            gateway,
            Context(["billing"]),
            intent => Preview(intent, TenantSetConfigurationCurrentState.Different, "tenant-sequence:41"),
            intent => Proof(intent, TenantConfigurationProjectionProofKind.SetConfirmed, projectionVersion));

        CompleteForm(cut, "mode", "secret");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        gateway.SetConfigurationCallCount.ShouldBe(1);
        gateway.LastMessageId.ShouldNotBeNullOrWhiteSpace();
        gateway.LastSetConfigurationRequest.ShouldNotBeNull().Key.ShouldBe("billing.mode");
        statusHandle.ShouldNotBeNull().AggregateId.ShouldBe("tenant.alpha");
        cut.Markup.ShouldNotContain("secret", Case.Sensitive);

        projectionVersion = "tenant-sequence:42";
        cut.Find("[data-testid='tenants-config-set-refresh']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.SetConfigurationCallCount.ShouldBe(1);
    }

    [Fact]
    public void Ambiguous_attempt_is_adopted_after_remount_without_redispatch_and_can_be_abandoned()
    {
        StubTenantCommandGateway gateway = RegisterServices();
        gateway.SubmissionFactory = (_, messageId) => TenantCommandSubmissionResult.Ambiguous(
            messageId,
            "Tenants.Configuration.Set.SubmissionEvidence.Ambiguous");
        Func<TenantSetConfigurationIntent, TenantSetConfigurationPreview> preview = intent =>
            Preview(intent, TenantSetConfigurationCurrentState.Different);
        Func<TenantSetConfigurationIntent, TenantConfigurationProjectionProof> proof = intent =>
            Proof(intent, TenantConfigurationProjectionProofKind.SetNotConfirmed, "tenant-sequence:41");

        IRenderedComponent<SetTenantConfigurationFlow> first = RenderFlow(gateway, Context(["billing"]), preview, proof);
        CompleteForm(first, "mode", "secret");
        first.Find("form").Submit();
        first.WaitForAssertion(() => first.Instance.Snapshot.RetainsAttempt.ShouldBeTrue());
        first.Dispose();

        IRenderedComponent<SetTenantConfigurationFlow> remounted = RenderFlow(gateway, Context(["billing"]), preview, proof);
        remounted.WaitForAssertion(() => remounted.Find("[data-testid='tenants-config-set-abandon']"));
        gateway.SetConfigurationCallCount.ShouldBe(1);
        remounted.Find("[data-testid='tenants-config-set-submit']").GetAttribute("disabled").ShouldNotBeNull();

        remounted.Find("[data-testid='tenants-config-set-abandon']").Click();
        remounted.Instance.Snapshot.RetainsAttempt.ShouldBeFalse();
        gateway.SetConfigurationCallCount.ShouldBe(1);
    }

    [Fact]
    public void Styles_keep_narrow_fail_closed_forced_colors_and_fluent_two_tokens()
    {
        string styles = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Configuration",
            "SetTenantConfigurationFlow.razor.css"));

        styles.ShouldContain("@media (max-width: 767px)");
        styles.ShouldContain("::deep .tenants-config-set__form");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain("var(--colorBrandStroke1");
        styles.ShouldNotContain("--accent-fill-rest");
    }

    private StubTenantCommandGateway RegisterServices()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var gateway = new StubTenantCommandGateway();
        Services.AddLocalization();
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton(new TenantSetConfigurationAttemptTracker());
        return gateway;
    }

    private IRenderedComponent<SetTenantConfigurationFlow> RenderFlow(
        StubTenantCommandGateway gateway,
        TenantConfigurationManagementContext context,
        Func<TenantSetConfigurationIntent, TenantSetConfigurationPreview> preview,
        Func<TenantSetConfigurationIntent, TenantConfigurationProjectionProof>? proof = null)
        => Render<SetTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.Lifecycle, ProjectionLifecycleState.Current)
            .Add(p => p.PreviewEvidenceProvider, intent => Task.FromResult(preview(intent)))
            .Add(p => p.ProjectionEvidenceProvider, intent => Task.FromResult(
                proof?.Invoke(intent)
                    ?? Proof(intent, TenantConfigurationProjectionProofKind.SetNotConfirmed, "tenant-sequence:41"))));

    private static void CompleteForm(
        IRenderedComponent<SetTenantConfigurationFlow> cut,
        string suffix,
        string value)
    {
        cut.Find("[data-testid='tenants-config-set-open']").Click();
        cut.Find("[data-testid='tenants-config-set-key-suffix']").Change(suffix);
        cut.Find("[data-testid='tenants-config-set-value']").Change(value);
    }

    private static TenantConfigurationManagementContext Context(
        IReadOnlyList<string> prefixes,
        bool globalAdministrator = false)
        => TenantConfigurationManagementContext.Available(
            "tenant.alpha",
            TenantStatus.Active,
            globalAdministrator,
            prefixes,
            []);

    private static TenantSetConfigurationPreview Preview(
        TenantSetConfigurationIntent intent,
        TenantSetConfigurationCurrentState currentState,
        string projectionVersion = "tenant-sequence:41")
        => TenantSetConfigurationPreview.Create(
            intent,
            TenantStatus.Active,
            currentState,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            projectionVersion,
            isAuthorized: true);

    private static TenantConfigurationProjectionProof Proof(
        TenantSetConfigurationIntent intent,
        TenantConfigurationProjectionProofKind kind,
        string projectionVersion)
        => TenantConfigurationProjectionProof.Create(
            intent.TenantId,
            kind,
            projectionVersion,
            intent.AttemptFingerprint);

    private static string ProjectRoot()
    {
        string directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Hexalith.Tenants.slnx"))) return directory;
            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public bool SupportsTrackedSetConfigurationDispatch => true;
        public Func<SetTenantConfiguration, string, TenantCommandSubmissionResult>? SubmissionFactory { get; set; }
        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; set; }
        public SetTenantConfiguration? LastSetConfigurationRequest { get; private set; }
        public string? LastMessageId { get; private set; }
        public int SetConfigurationCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationTrackedAsync(
            SetTenantConfiguration request,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            SetConfigurationCallCount++;
            LastSetConfigurationRequest = request;
            LastMessageId = messageId;
            return Task.FromResult(SubmissionFactory?.Invoke(request, messageId)
                ?? TenantCommandSubmissionResult.Accepted(messageId, "correlation-1"));
        }

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfiguration request,
            CancellationToken cancellationToken = default)
            => SetTenantConfigurationTrackedAsync(request, "legacy-message", cancellationToken);

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
            => StatusAsync?.Invoke(handle) ?? Task.FromResult(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));
        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));
        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));
        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));
        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));
    }

}
