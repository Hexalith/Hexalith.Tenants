using System.Globalization;
using System.Text;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

/// <summary>
/// Covers the server-side composition seam directly.
/// </summary>
/// <remarks>
/// The submit-time re-authorization path had no test at all: both revocation tests in the command-flow
/// suites asserted against a hand-written <c>ReauthorizeProvider</c> lambda, so they exercised the
/// component guard honouring an answer rather than the server producing one. Removing the cross-tenant
/// check, or rebuilding the context from the passed-in safe model instead of re-resolving policy,
/// survived the entire suite.
/// </remarks>
public sealed class TenantsBffCompositionTests
{
    private const string GrantedPolicy = """
        {
          "Tenants": {
            "ConfigurationReadPolicy": {
              "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
              "DisplaySafe": ["billing.mode"]
            }
          }
        }
        """;

    private const string RevokedPolicy = """
        {
          "Tenants": {
            "ConfigurationReadPolicy": {
              "PrefixGrants": [],
              "DisplaySafe": ["billing.mode"]
            }
          }
        }
        """;

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void FixedScopeCapabilitiesMapOnlyToTheirCorrespondingProductionSeams(
        bool supportsDispatch,
        bool supportsStatus,
        bool supportsRequery)
    {
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.SupportsGlobalAdministratorDispatch.Returns(supportsDispatch);
        gateway.SupportsTrackedGlobalAdministratorDispatch.Returns(supportsDispatch);
        gateway.SupportsCommandStatusLookup.Returns(supportsStatus);
        var composition = new TenantsBffComposition(
            gateway,
            readSurface: new TenantsReadSurfaceAvailability(supportsRequery));

        composition.IsGlobalAdministratorDispatchConnected.ShouldBe(supportsDispatch);
        composition.IsGlobalAdministratorStatusConnected.ShouldBe(supportsStatus);
        composition.IsGlobalAdministratorRequeryConnected.ShouldBe(supportsRequery);
    }

    [Fact]
    public void ProductionPreviewReadinessReflectsOnlyInstalledDownstreamFlows()
    {
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        var composition = new TenantsBffComposition(
            gateway,
            readSurface: new TenantsReadSurfaceAvailability(IsConnected: true));

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeFalse();
        composition.IsGlobalAdministratorRemovePreviewReady.ShouldBeFalse();
    }

    [Fact]
    public void ProductionGrantPreviewReadinessRequiresConcretePrincipalComposition()
    {
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        var composition = new TenantsBffComposition(
            gateway,
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            readSurface: new TenantsReadSurfaceAvailability(IsConnected: true),
            resourceLocalizer: ResolvedGrantLocalizer());

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeTrue();
    }

    // Every other readiness test substitutes the localizer and feeds it a test-local copy of the key list,
    // so none of them can notice a shipped resource going missing, blank, or losing its count placeholders
    // -- and this gate is a production kill switch for the whole grant surface when it returns false.
    // Resolve the real localizer over the real compiled resources instead, and prove the keys the gate
    // demands are the keys a real preview actually renders.
    [Fact]
    public void GrantPreviewReadinessHoldsAgainstTheShippedEnglishAndFrenchResources()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddLogging()
            .AddLocalization()
            .BuildServiceProvider();
        var composition = new TenantsBffComposition(
            Substitute.For<ITenantCommandGateway>(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            readSurface: new TenantsReadSurfaceAvailability(IsConnected: true),
            resourceLocalizer: provider.GetRequiredService<IStringLocalizer<TenantsResources>>());

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeTrue();
    }

    [Fact]
    public async Task RemovalPreviewReadinessAndSelfRemovalHoldAgainstShippedEnglishAndFrenchResources()
    {
        string[] canonicalRemoveKeys =
        [
            "Tenants.GlobalAdministrators.Remove.Launch",
            "Tenants.GlobalAdministrators.Remove.Preview.Title",
            "Tenants.GlobalAdministrators.Remove.Preview.Scope",
            "Tenants.GlobalAdministrators.Remove.Preview.Scope.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.Target",
            "Tenants.GlobalAdministrators.Remove.Preview.Target.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.Counts",
            "Tenants.GlobalAdministrators.Remove.Preview.Counts.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.AuthorityChange",
            "Tenants.GlobalAdministrators.Remove.Preview.AuthorityChange.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.Freshness",
            "Tenants.GlobalAdministrators.Remove.Preview.Freshness.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.Audit",
            "Tenants.GlobalAdministrators.Remove.Preview.Audit.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.CallerTargetContext",
            "Tenants.GlobalAdministrators.Remove.Preview.CallerTargetContext.Self.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.CallerTargetContext.Other.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.KnownConsequences",
            "Tenants.GlobalAdministrators.Remove.Preview.KnownConsequences.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.KnownUnknowns",
            "Tenants.GlobalAdministrators.Remove.Preview.KnownUnknowns.Value",
            "Tenants.GlobalAdministrators.Remove.Preview.Acknowledge",
            "Tenants.GlobalAdministrators.Remove.Preview.Confirm",
            "Tenants.GlobalAdministrators.Remove.Cancel",
            "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Authorization",
            "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Target",
            "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Evidence",
            "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.TargetMissing",
            "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.LastAdministrator",
            "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Localization",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Authorization",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Target",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.TargetMissing",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.LastAdministrator",
            "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Localization",
            "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous",
            "Tenants.GlobalAdministrators.Remove.DeliveryRetry",
            "Tenants.GlobalAdministrators.Remove.DeliveryRetry.Recovery",
            "Tenants.GlobalAdministrators.Remove.UnableToVerify.TrackingMismatch",
            "Tenants.GlobalAdministrators.Remove.UnableToVerify.EventEvidence",
            "Tenants.GlobalAdministrators.Remove.UnableToVerify.StatusTimeout",
            "Tenants.GlobalAdministrators.Remove.UnableToVerify.UnsupportedSubmission",
            "Tenants.GlobalAdministrators.Remove.Status.Pending",
            "Tenants.GlobalAdministrators.Remove.Status.Unknown",
            "Tenants.GlobalAdministrators.Remove.Status.PublishFailed",
            "Tenants.GlobalAdministrators.Remove.Status.Rejected",
            "Tenants.GlobalAdministrators.Remove.Status.Rejected.LastAdministrator",
            "Tenants.GlobalAdministrators.Remove.Status.Rejected.NotFound",
            "Tenants.GlobalAdministrators.Remove.Status.Rejected.Permission",
            "Tenants.GlobalAdministrators.Remove.Status.TimedOut",
            "Tenants.GlobalAdministrators.Remove.Status.Failed",
            "Tenants.GlobalAdministrators.Remove.Recovery.Rejected",
            "Tenants.GlobalAdministrators.Remove.Recovery.Failed",
            "Tenants.GlobalAdministrators.Remove.Recovery.PublishFailed",
            "Tenants.GlobalAdministrators.Remove.Recovery.TimedOut",
            "Tenants.GlobalAdministrators.Remove.Confirm.EvidenceRequired",
            "Tenants.GlobalAdministrators.Remove.Confirm.StillPresent",
            "Tenants.GlobalAdministrators.Remove.Confirm.VersionNotAdvanced",
            "Tenants.GlobalAdministrators.Remove.Projection.UnableToVerify",
        ];
        TenantsBffComposition.RequiredRemoveFactKeys.ShouldBe(canonicalRemoveKeys);
        ServiceProvider provider = new ServiceCollection()
            .AddLogging()
            .AddLocalization()
            .BuildServiceProvider();
        var composition = new TenantsBffComposition(
            Substitute.For<ITenantCommandGateway>(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("  Target.Admin  ")),
            readSurface: new TenantsReadSurfaceAvailability(IsConnected: true),
            resourceLocalizer: provider.GetRequiredService<IStringLocalizer<TenantsResources>>());

        GlobalAdministratorRemovePreview preview = await composition
            .ComposeGlobalAdministratorRemovePreviewAsync(
                "  Target.Admin  ",
                CompleteGlobalAdministrators("projection-v1", "  Target.Admin  ", "other-admin"));

        composition.IsGlobalAdministratorRemovePreviewReady.ShouldBeTrue();
        preview.IsComplete.ShouldBeTrue();
        preview.IsSelfRemoval.ShouldBeTrue();
        preview.CurrentAdministratorCount.ShouldBe(2);
        preview.ResultingAdministratorCount.ShouldBe(1);
        preview.TargetUserId.ShouldBe("  Target.Admin  ");
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            IStringLocalizer<TenantsResources> localizer = provider
                .GetRequiredService<IStringLocalizer<TenantsResources>>();
            foreach (string cultureName in new[] { "en", "fr" })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                IReadOnlyDictionary<string, LocalizedString> shipped = localizer
                    .GetAllStrings(includeParentCultures: true)
                    .ToDictionary(static resource => resource.Name, StringComparer.Ordinal);
                foreach (string key in canonicalRemoveKeys)
                {
                    shipped.ShouldContainKey(key);
                    shipped[key].Value.ShouldNotBeNullOrWhiteSpace();
                    shipped[key].Value.ShouldNotBe(key);
                }
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    [Fact]
    public void GrantPreviewReadinessCoversEveryFactKeyARealPreviewRenders()
    {
        GlobalAdministratorGrantPreview preview = GlobalAdministratorGrantPreview.Create(
            "target-admin",
            CompleteGlobalAdministrators("projection-v1", "existing-admin"),
            isAuthorized: true);
        preview.IsComplete.ShouldBeTrue();

        string[] renderedFactKeys =
        [
            preview.ScopeFactKey!,
            "Tenants.GlobalAdministrators.Grant.Preview.Counts.Value",
            preview.AuthorityChangeFactKey!,
            preview.FreshnessFactKey!,
            preview.RecoveryFactKey!,
            preview.AuditFactKey!,
            preview.CallerTargetContextFactKey!,
            preview.KnownConsequencesFactKey!,
            preview.KnownUnknownsFactKey!,
        ];
        string[] renderedChromeKeys =
        [
            "Tenants.GlobalAdministrators.Grant.Preview.Launch",
            "Tenants.GlobalAdministrators.Grant.Preview.Title",
            "Tenants.GlobalAdministrators.Grant.Preview.Scope",
            "Tenants.GlobalAdministrators.Grant.Preview.Target",
            "Tenants.GlobalAdministrators.Grant.Preview.Counts",
            "Tenants.GlobalAdministrators.Grant.Preview.AuthorityChange",
            "Tenants.GlobalAdministrators.Grant.Preview.Freshness",
            "Tenants.GlobalAdministrators.Grant.Preview.Recovery",
            "Tenants.GlobalAdministrators.Grant.Preview.Audit",
            "Tenants.GlobalAdministrators.Grant.Preview.CallerTargetContext",
            "Tenants.GlobalAdministrators.Grant.Preview.KnownConsequences",
            "Tenants.GlobalAdministrators.Grant.Preview.KnownUnknowns",
            "Tenants.GlobalAdministrators.Grant.Preview.Acknowledge",
            "Tenants.GlobalAdministrators.Grant.Preview.Confirm",
            "Tenants.GlobalAdministrators.Grant.Cancel",
        ];

        TenantsBffComposition.RequiredGrantFactKeys.Count.ShouldBe(26);
        TenantsBffComposition.RequiredGrantFactKeys.Distinct(StringComparer.Ordinal).Count().ShouldBe(26);
        foreach (string key in renderedFactKeys.Concat(renderedChromeKeys))
        {
            key.ShouldNotBeNullOrWhiteSpace();
            TenantsBffComposition.RequiredGrantFactKeys.ShouldContain(key);
        }

        TenantsBffComposition.RequiredGrantFactKeys.ShouldContain(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization");
        TenantsBffComposition.RequiredGrantFactKeys.ShouldContain(
            "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Localization");
    }

    [Fact]
    public async Task GrantPreviewFailsClosedWhenLocalizerIsAbsent()
    {
        var composition = new TenantsBffComposition(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            readSurface: new TenantsReadSurfaceAvailability(IsConnected: true));

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync(
                "target-admin",
                CompleteGlobalAdministrators("projection-v1", "existing-admin"));

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeFalse();
        preview.IsComplete.ShouldBeFalse();
        preview.UnavailableReasonKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization");
    }

    [Fact]
    public async Task GrantPreviewPreservesLiteralTargetAndOwnsEveryRequiredSafeFact()
    {
        TenantsBffComposition composition = Composition(
            RevokedPolicy,
            TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha"));
        const string target = "  CaseSensitive/User.01  ";

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync(
                target,
                CompleteGlobalAdministrators("projection-v1", "existing-admin"));

        preview.TargetUserId.ShouldBe(target);
        preview.ScopeTenantId.ShouldBe("system");
        preview.ScopeDomain.ShouldBe("global-administrators");
        preview.ScopeAggregateId.ShouldBe("global-administrators");
        preview.CurrentAdministratorCount.ShouldBe(1);
        preview.ResultingAdministratorCount.ShouldBe(2);
        preview.HasAllSafeFacts.ShouldBeTrue();
        preview.IsComplete.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Tenants.GlobalAdministrators.Grant.Preview.Counts.Value")]
    [InlineData("Tenants.GlobalAdministrators.Grant.Preview.Title")]
    [InlineData("Tenants.GlobalAdministrators.Grant.Cancel")]
    [InlineData("Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization")]
    public async Task GrantPreviewFailsClosedWhenARequiredLocalizedStringIsUnresolved(string unresolvedKey)
    {
        IStringLocalizer<TenantsResources> localizer = ResolvedGrantLocalizer(
            (_, candidate) => string.Equals(candidate, unresolvedKey, StringComparison.Ordinal)
                ? candidate
                : DefaultGrantResourceValue(candidate));
        var composition = new TenantsBffComposition(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            resourceLocalizer: localizer);

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync(
                "target-admin",
                CompleteGlobalAdministrators("projection-v1", "existing-admin"));

        preview.IsComplete.ShouldBeFalse();
        preview.UnavailableReasonKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization");
        preview.RecoveryKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Localization");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("Tenants.GlobalAdministrators.Grant.Preview.KnownConsequences.Value")]
    public async Task GrantPreviewFailsClosedWhenLocalizedValueIsWhitespaceOrKeyEcho(string unresolvedValue)
    {
        const string key = "Tenants.GlobalAdministrators.Grant.Preview.KnownConsequences.Value";
        IStringLocalizer<TenantsResources> localizer = ResolvedGrantLocalizer(
            (_, candidate) => string.Equals(candidate, key, StringComparison.Ordinal)
                ? unresolvedValue
                : DefaultGrantResourceValue(candidate));
        var composition = new TenantsBffComposition(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            resourceLocalizer: localizer);

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync(
                "target-admin",
                CompleteGlobalAdministrators("projection-v1", "existing-admin"));

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeFalse();
        preview.IsComplete.ShouldBeFalse();
        preview.UnavailableReasonKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization");
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    public async Task GrantPreviewRequiresEveryExplicitEnglishAndFrenchResource(string missingCultureName)
    {
        const string key = "Tenants.GlobalAdministrators.Grant.Preview.KnownUnknowns.Value";
        IStringLocalizer<TenantsResources> localizer = ResolvedGrantLocalizer(
            include: (culture, candidate) => !string.Equals(candidate, key, StringComparison.Ordinal)
                || !string.Equals(culture.Name, missingCultureName, StringComparison.Ordinal));
        var composition = new TenantsBffComposition(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            resourceLocalizer: localizer);

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync(
                "target-admin",
                CompleteGlobalAdministrators("projection-v1", "existing-admin"));

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeFalse();
        preview.IsComplete.ShouldBeFalse();
        preview.UnavailableReasonKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization");
    }

    [Theory]
    [InlineData("Current: {0}")]
    [InlineData("Resulting: {1}")]
    [InlineData("Counts without placeholders")]
    [InlineData("Malformed: {0} {2}")]
    public async Task GrantPreviewFailsClosedForInvalidOrIncompleteCountCompositeFormat(string countFormat)
    {
        const string key = "Tenants.GlobalAdministrators.Grant.Preview.Counts.Value";
        IStringLocalizer<TenantsResources> localizer = ResolvedGrantLocalizer(
            (_, candidate) => string.Equals(candidate, key, StringComparison.Ordinal)
                ? countFormat
                : DefaultGrantResourceValue(candidate));
        var composition = new TenantsBffComposition(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha")),
            resourceLocalizer: localizer);

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync(
                "target-admin",
                CompleteGlobalAdministrators("projection-v1", "existing-admin"));

        composition.IsGlobalAdministratorGrantPreviewReady.ShouldBeFalse();
        preview.IsComplete.ShouldBeFalse();
        preview.UnavailableReasonKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Localization");
    }

    [Fact]
    public async Task GrantPreviewFailsClosedForUnauthorizedCallerWithoutUsingRowsAsOracle()
    {
        TenantsBffComposition composition = Composition(
            RevokedPolicy,
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));
        GlobalAdministratorsSnapshot snapshot = CompleteGlobalAdministrators(
            "projection-v1",
            "hidden-admin") with
        {
            Rows = new ThrowingGlobalAdministratorRows(),
        };

        GlobalAdministratorGrantPreview preview = await composition
            .ComposeGlobalAdministratorGrantPreviewAsync("target-admin", snapshot);

        preview.IsAuthorized.ShouldBeFalse();
        preview.IsComplete.ShouldBeFalse();
        preview.UnavailableReasonKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Authorization");
    }

    [Fact]
    public async Task Reauthorize_returns_current_scope_when_the_grant_still_stands()
    {
        TenantConfigurationSafeModel safeModel = SafeModel(GrantedPolicy);

        TenantConfigurationManagementContext context = await Composition(GrantedPolicy)
            .ReauthorizeConfigurationManagementAsync("tenant.alpha", TenantStatus.Active, safeModel);

        context.IsAvailable.ShouldBeTrue();
        context.IsKeyAuthorized("billing.mode").ShouldBeTrue();
        context.FindRemovableRow("billing.mode").ShouldNotBeNull();
    }

    [Fact]
    public async Task Reauthorize_drops_scope_when_the_grant_was_revoked_since_the_page_rendered()
    {
        // The safe model still carries the row that was authorized at render time; re-authorization has
        // to consult current deployment policy rather than trust it.
        TenantConfigurationSafeModel safeModel = SafeModel(GrantedPolicy);
        safeModel.Rows.ShouldHaveSingleItem();

        TenantConfigurationManagementContext context = await Composition(RevokedPolicy)
            .ReauthorizeConfigurationManagementAsync("tenant.alpha", TenantStatus.Active, safeModel);

        context.IsKeyAuthorized("billing.mode").ShouldBeFalse();
        context.RemovableRows.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reauthorize_fails_closed_for_a_safe_model_belonging_to_another_tenant()
    {
        TenantConfigurationSafeModel safeModel = SafeModel(GrantedPolicy);

        TenantConfigurationManagementContext context = await Composition(GrantedPolicy)
            .ReauthorizeConfigurationManagementAsync("tenant.beta", TenantStatus.Active, safeModel);

        context.IsAvailable.ShouldBeFalse();
        context.RemovableRows.ShouldBeEmpty();
        context.TenantId.ShouldBe("tenant.beta");
    }

    [Theory]
    [InlineData("billing.mode", true)]
    [InlineData("billing", true)]
    [InlineData("billingother.mode", false)]
    [InlineData("Billing.mode", false)]
    [InlineData("secret.key", false)]
    public async Task Key_authorization_follows_the_ordinal_prefix_grant(string key, bool expected)
    {
        bool authorized = await Composition(GrantedPolicy)
            .IsConfigurationKeyAuthorizedAsync("tenant.alpha", key);

        authorized.ShouldBe(expected);
    }

    [Fact]
    public async Task Key_authorization_fails_closed_when_policy_is_unavailable()
    {
        bool authorized = await Composition(
                "{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": \"scalar\", \"DisplaySafe\": [] } } }")
            .IsConfigurationKeyAuthorizedAsync("tenant.alpha", "billing.mode");

        authorized.ShouldBeFalse();
    }

    [Fact]
    public async Task Global_administrator_key_authorization_requires_no_explicit_prefix_grants()
    {
        bool authorized = await Composition(
                RevokedPolicy,
                TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha"))
            .IsConfigurationKeyAuthorizedAsync("tenant.alpha", "unlisted.literal.key");

        authorized.ShouldBeTrue();
    }

    [Fact]
    public async Task Bff_composes_TenantOwner_role_and_namespace_scope_as_separate_evidence()
    {
        TenantsBffComposition composition = Composition(GrantedPolicy);
        TenantConfigurationComposition detail = await composition.ComposeTenantDetailAsync(Detail());

        detail.ManagementContext.AuthorityState.ShouldBe(TenantConfigurationAuthorityState.TenantOwner);
        detail.ManagementContext.IsKeyAuthorized("billing.mode").ShouldBeTrue();
        TenantHighImpactBffEvidence evidence = ((ITenantsBffComposition)composition).ComposeTenantHighImpactEvidence(
            detail.SanitizedDetail,
            detail.ManagementContext,
            TenantLifecycleAuthorizationReflectionState.MissingPermission);

        evidence.ConfigurationAuthority.ShouldBe(TenantHighImpactAuthorityEvidence.Authorized);
        evidence.ConfigurationScope.ShouldBe(TenantHighImpactNamespaceScopeEvidence.Authorized);
        evidence.LifecycleAuthority.ShouldBe(TenantHighImpactAuthorityEvidence.MissingPermission);
        evidence.ConfigurationPreview.ShouldBe(TenantHighImpactPreviewEvidence.Ready);
    }

    [Fact]
    public async Task Prefix_scope_without_TenantOwner_role_does_not_authorize_configuration_mutation()
    {
        TenantsBffComposition composition = Composition(GrantedPolicy);
        TenantDetail nonOwner = Detail(
            [new TenantMember("operator.alpha", TenantRole.TenantReader)]);

        TenantConfigurationComposition detail = await composition.ComposeTenantDetailAsync(nonOwner);
        TenantHighImpactBffEvidence evidence = ((ITenantsBffComposition)composition).ComposeTenantHighImpactEvidence(
            detail.SanitizedDetail,
            detail.ManagementContext,
            TenantLifecycleAuthorizationReflectionState.Authorized);

        detail.ManagementContext.IsKeyAuthorized("billing.mode").ShouldBeTrue();
        detail.ManagementContext.HasMutationAuthority.ShouldBeFalse();
        evidence.ConfigurationScope.ShouldBe(TenantHighImpactNamespaceScopeEvidence.Authorized);
        evidence.ConfigurationAuthority.ShouldBe(TenantHighImpactAuthorityEvidence.MissingPermission);
    }

    [Fact]
    public async Task Submit_time_reauthorization_reproves_TenantOwner_from_current_sanitized_members()
    {
        TenantsBffComposition composition = Composition(GrantedPolicy);
        TenantConfigurationSafeModel safeModel = SafeModel(GrantedPolicy);
        TenantDetail currentWithoutOwner = Detail(
            [new TenantMember("operator.alpha", TenantRole.TenantReader)]);

        TenantConfigurationManagementContext context = await composition
            .ReauthorizeConfigurationManagementAsync(currentWithoutOwner, safeModel);

        context.IsKeyAuthorized("billing.mode").ShouldBeTrue();
        context.HasMutationAuthority.ShouldBeFalse();
        context.AuthorityState.ShouldBe(TenantConfigurationAuthorityState.MissingPermission);
    }

    [Fact]
    public async Task Submit_time_reauthorization_preserves_matching_TenantOwner_authority()
    {
        TenantsBffComposition composition = Composition(GrantedPolicy);
        TenantConfigurationSafeModel safeModel = SafeModel(GrantedPolicy);

        TenantConfigurationManagementContext context = await composition
            .ReauthorizeConfigurationManagementAsync(Detail(), safeModel);

        context.IsKeyAuthorized("billing.mode").ShouldBeTrue();
        context.AuthorityState.ShouldBe(TenantConfigurationAuthorityState.TenantOwner);
        context.HasMutationAuthority.ShouldBeTrue();
    }

    [Fact]
    public async Task Runtime_null_member_is_ignored_and_authority_fails_closed_without_throwing()
    {
        TenantsBffComposition composition = Composition(GrantedPolicy);
        TenantConfigurationSafeModel safeModel = SafeModel(GrantedPolicy);

        TenantConfigurationManagementContext context = await composition
            .ReauthorizeConfigurationManagementAsync(Detail([null!]), safeModel);

        context.AuthorityState.ShouldBe(TenantConfigurationAuthorityState.MissingPermission);
        context.HasMutationAuthority.ShouldBeFalse();
    }

    [Fact]
    public async Task Global_administrator_authority_is_reflected_without_TenantOwner_membership()
    {
        TenantsBffComposition composition = Composition(
            RevokedPolicy,
            TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha"));

        TenantConfigurationComposition detail = await composition.ComposeTenantDetailAsync(
            Detail([new TenantMember("someone.else", TenantRole.TenantReader)]));

        detail.ManagementContext.AuthorityState.ShouldBe(TenantConfigurationAuthorityState.GlobalAdministrator);
        detail.ManagementContext.HasMutationAuthority.ShouldBeTrue();
        detail.ManagementContext.IsKeyAuthorized("unlisted.literal.key").ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, TenantLifecycleAuthorizationReflectionState.Authorized)]
    [InlineData(1, TenantLifecycleAuthorizationReflectionState.MissingPermission)]
    [InlineData(2, TenantLifecycleAuthorizationReflectionState.Indeterminate)]
    public async Task Administrator_reflections_use_the_strict_principal_resolution(
        int evidenceKind,
        TenantLifecycleAuthorizationReflectionState expected)
    {
        TenantConfigurationPrincipalEvidence evidence = evidenceKind switch
        {
            0
                => TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha"),
            1
                => TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"),
            _ => TenantConfigurationPrincipalEvidence.Indeterminate(),
        };

        TenantsBffComposition composition = Composition(RevokedPolicy, evidence);
        TenantLifecycleAuthorizationReflectionState globalReflection = await composition
            .ResolveGlobalAdministratorsAuthorizationAsync();
        TenantLifecycleAuthorizationReflectionState lifecycleReflection = await composition
            .ResolveLifecycleAuthorizationAsync();

        globalReflection.ShouldBe(expected);
        lifecycleReflection.ShouldBe(expected);
    }

    [Fact]
    public async Task Interface_default_compose_and_reauthorize_fail_closed_to_unavailable_safe_models()
    {
        ITenantsBffComposition composition = new DefaultOnlyComposition();
        TenantDetail detail = Detail();

        TenantConfigurationComposition composed = await composition.ComposeTenantDetailAsync(detail);
        composed.SafeModel.IsAvailable.ShouldBeFalse();
        composed.ManagementContext.IsAvailable.ShouldBeFalse();
        composed.SanitizedDetail.Configuration.ShouldBeEmpty();

        TenantConfigurationComposition reauthorized = await composition.ReauthorizeTenantDetailAsync(
            composed.SanitizedDetail,
            TenantConfigurationSafeModel.Available(
                "tenant.alpha",
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]),
            degraded: false);
        reauthorized.SafeModel.IsAvailable.ShouldBeFalse();
        reauthorized.ManagementContext.IsAvailable.ShouldBeFalse();

        TenantConfigurationManagementContext management =
            await composition.ReauthorizeConfigurationManagementAsync(
                "tenant.alpha",
                TenantStatus.Active,
                TenantConfigurationSafeModel.Available(
                    "tenant.alpha",
                    [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]));
        management.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task Composition_without_principal_or_policy_deps_fails_closed_on_compose_and_reauthorize()
    {
        TenantsBffComposition composition = new(new UnavailableTenantCommandGateway());
        TenantDetail detail = Detail();

        TenantConfigurationComposition composed = await composition.ComposeTenantDetailAsync(detail);
        composed.SafeModel.IsAvailable.ShouldBeFalse();
        composed.ManagementContext.IsAvailable.ShouldBeFalse();
        composed.SanitizedDetail.Configuration.ShouldBeEmpty();

        TenantConfigurationComposition reauthorized = await composition.ReauthorizeTenantDetailAsync(
            composed.SanitizedDetail,
            TenantConfigurationSafeModel.Available(
                "tenant.alpha",
                [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]),
            degraded: true);
        reauthorized.SafeModel.IsAvailable.ShouldBeFalse();
        reauthorized.ManagementContext.IsAvailable.ShouldBeFalse();

        TenantConfigurationManagementContext management =
            await composition.ReauthorizeConfigurationManagementAsync(
                "tenant.alpha",
                TenantStatus.Active,
                TenantConfigurationSafeModel.Available(
                    "tenant.alpha",
                    [new TenantConfigurationSafeRow("billing", "billing.mode", "trial")]));
        management.IsAvailable.ShouldBeFalse();
    }

    /// <summary>
    /// A composition assembled without a principal resolver must fail closed, not fall back to the
    /// <c>HttpContext.User</c> reflection this story discarded. Every existing test supplies a resolver, so
    /// restoring <c>return GlobalAdministratorsAuthorizationReflection;</c> survived the whole suite.
    /// </summary>
    [Fact]
    public async Task Composition_without_a_principal_resolver_fails_closed_on_both_authorization_seams()
    {
        TenantsBffComposition composition = new(new UnavailableTenantCommandGateway());

        (await composition.ResolveGlobalAdministratorsAuthorizationAsync())
            .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        (await composition.ResolveLifecycleAuthorizationAsync())
            .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
    }

    /// <summary>
    /// The interface default must also fail closed. Every implementation in the repo overrides the async
    /// resolver, so nothing exercised the default -- and forwarding it to the synchronous property would let
    /// any future implementation silently inherit the discarded HTTP-only interpretation. Both authorization
    /// seams must be pinned: lifecycle defaults by forwarding to the global-administrators default.
    /// </summary>
    [Fact]
    public async Task Interface_default_authorization_resolution_fails_closed_rather_than_forwarding()
    {
        ITenantsBffComposition composition = new DefaultOnlyComposition();

        (await composition.ResolveGlobalAdministratorsAuthorizationAsync())
            .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        (await composition.ResolveLifecycleAuthorizationAsync())
            .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
    }

    [Fact]
    public async Task Set_preview_classifies_exact_value_without_exposing_raw_projection_value()
    {
        TenantSetConfigurationIntent intent = new(
            "tenant.alpha",
            "billing",
            "mode",
            "billing.mode",
            TenantSetConfigurationValueFingerprint.Create("enterprise"));

        TenantSetConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeSetConfigurationPreviewAsync(
                Detail(),
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsComplete.ShouldBeTrue();
        preview.CurrentState.ShouldBe(TenantSetConfigurationCurrentState.Different);
        preview.GetType().GetProperties()
            .ShouldNotContain(property => property.Name.Contains("Value", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Set_preview_resolves_authority_before_configuration_lookup()
    {
        TenantSetConfigurationIntent intent = new(
            "tenant.alpha",
            "security",
            "mode",
            "security.mode",
            TenantSetConfigurationValueFingerprint.Create("enabled"));
        TenantDetail detail = Detail() with { Configuration = new ThrowingConfiguration() };

        TenantSetConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeSetConfigurationPreviewAsync(
                detail,
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsAuthorized.ShouldBeFalse();
        preview.CurrentState.ShouldBe(TenantSetConfigurationCurrentState.Unknown);
    }

    [Theory]
    [InlineData(TenantRole.TenantReader)]
    [InlineData(TenantRole.TenantContributor)]
    [InlineData(TenantRole.Unknown)]
    public async Task Set_preview_requires_owner_membership_before_configuration_lookup(TenantRole role)
    {
        TenantSetConfigurationIntent intent = new(
            "tenant.alpha",
            "billing",
            "mode",
            "billing.mode",
            TenantSetConfigurationValueFingerprint.Create("enabled"));
        TenantDetail detail = Detail([new TenantMember("operator.alpha", role)]) with
        {
            Configuration = new ThrowingConfiguration(),
        };

        TenantSetConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeSetConfigurationPreviewAsync(
                detail,
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsAuthorized.ShouldBeFalse();
        preview.CurrentState.ShouldBe(TenantSetConfigurationCurrentState.Unknown);
    }

    [Fact]
    public async Task Set_preview_requires_the_current_subject_to_be_present_before_configuration_lookup()
    {
        TenantSetConfigurationIntent intent = new(
            "tenant.alpha",
            "billing",
            "mode",
            "billing.mode",
            TenantSetConfigurationValueFingerprint.Create("enabled"));
        TenantDetail detail = Detail([]) with { Configuration = new ThrowingConfiguration() };

        TenantSetConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeSetConfigurationPreviewAsync(
                detail,
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsAuthorized.ShouldBeFalse();
        preview.CurrentState.ShouldBe(TenantSetConfigurationCurrentState.Unknown);
    }

    [Theory]
    [InlineData(true, TenantRemoveConfigurationCurrentState.Present)]
    [InlineData(false, TenantRemoveConfigurationCurrentState.Absent)]
    public async Task Remove_preview_classifies_authorized_presence_without_exposing_raw_value(
        bool containsTarget,
        TenantRemoveConfigurationCurrentState expectedState)
    {
        const string rawValue = "raw-remove-secret";
        TenantRemoveConfigurationIntent intent = new("tenant.alpha", "billing", "billing.mode");
        IReadOnlyDictionary<string, string> configuration = containsTarget
            ? new Dictionary<string, string>(StringComparer.Ordinal) { ["billing.mode"] = rawValue }
            : new Dictionary<string, string>(StringComparer.Ordinal) { ["billing.other"] = rawValue };
        TenantDetail detail = Detail() with { Configuration = configuration };

        TenantRemoveConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeRemoveConfigurationPreviewAsync(
                detail,
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsAuthorized.ShouldBeTrue();
        preview.IsAuthoritative.ShouldBeTrue();
        preview.CurrentState.ShouldBe(expectedState);
        preview.GetType().GetProperties()
            .ShouldNotContain(property => property.Name.Contains("Value", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Remove_preview_resolves_namespace_authority_before_configuration_lookup()
    {
        TenantRemoveConfigurationIntent intent = new("tenant.alpha", "security", "security.mode");
        TenantDetail detail = Detail() with { Configuration = new ThrowingConfiguration() };

        TenantRemoveConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeRemoveConfigurationPreviewAsync(
                detail,
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsAuthorized.ShouldBeFalse();
        preview.IsAuthoritative.ShouldBeFalse();
        preview.CurrentState.ShouldBe(TenantRemoveConfigurationCurrentState.Unknown);
    }

    [Theory]
    [InlineData(TenantRole.TenantReader)]
    [InlineData(TenantRole.TenantContributor)]
    [InlineData(TenantRole.Unknown)]
    public async Task Remove_preview_resolves_mutation_authority_before_configuration_lookup(TenantRole role)
    {
        TenantRemoveConfigurationIntent intent = new("tenant.alpha", "billing", "billing.mode");
        TenantDetail detail = Detail([new TenantMember("operator.alpha", role)]) with
        {
            Configuration = new ThrowingConfiguration(),
        };

        TenantRemoveConfigurationPreview preview = await Composition(GrantedPolicy)
            .ComposeRemoveConfigurationPreviewAsync(
                detail,
                intent,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current,
                "tenant-sequence:41");

        preview.IsAuthorized.ShouldBeFalse();
        preview.IsAuthoritative.ShouldBeFalse();
        preview.CurrentState.ShouldBe(TenantRemoveConfigurationCurrentState.Unknown);
    }

    private static TenantsBffComposition Composition(
        string json,
        TenantConfigurationPrincipalEvidence? evidence = null)
        => new(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                evidence ?? TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha")),
            policyProvider: new TenantConfigurationReadPolicyProvider(Configuration(json)),
            resourceLocalizer: ResolvedGrantLocalizer());

    private static IStringLocalizer<TenantsResources> ResolvedGrantLocalizer(
        Func<CultureInfo, string, string>? value = null,
        Func<CultureInfo, string, bool>? include = null)
    {
        Func<CultureInfo, string, string> resolve = value
            ?? ((_, candidate) => DefaultGrantResourceValue(candidate));
        Func<CultureInfo, string, bool> contains = include ?? ((_, _) => true);
        IStringLocalizer<TenantsResources> localizer = Substitute.For<IStringLocalizer<TenantsResources>>();
        localizer[Arg.Any<string>()].Returns(callInfo =>
        {
            string key = callInfo.Arg<string>();
            string localizedValue = resolve(CultureInfo.CurrentUICulture, key);
            return new LocalizedString(key, localizedValue, resourceNotFound: false);
        });
        localizer.GetAllStrings(includeParentCultures: false).Returns(_ => TenantsBffComposition.RequiredGrantFactKeys
            .Where(key => contains(CultureInfo.CurrentUICulture, key))
            .Select(key => new LocalizedString(
                key,
                resolve(CultureInfo.CurrentUICulture, key),
                resourceNotFound: false))
            .ToArray());
        return localizer;
    }

    private static string DefaultGrantResourceValue(string key)
        => string.Equals(
            key,
            "Tenants.GlobalAdministrators.Grant.Preview.Counts.Value",
            StringComparison.Ordinal)
            ? "Current complete count: {0}; resulting count: {1}"
            : $"resolved:{key}";

    private static TenantConfigurationSafeModel SafeModel(string json)
    {
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration(json))
            .Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));
        return TenantConfigurationSafeComposer.Compose(Detail(), policy).SafeModel;
    }

    private static IConfiguration Configuration(string json)
        => new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();

    private static GlobalAdministratorsSnapshot CompleteGlobalAdministrators(
        string projectionVersion,
        params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(static userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: $"\"{projectionVersion}\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = projectionVersion,
            IsCompleteEvidence = true,
        };

    private static TenantDetail Detail(IReadOnlyList<TenantMember>? members = null)
        => new(
            "tenant.alpha",
            "Alpha",
            "Description",
            TenantStatus.Active,
            members ?? [new TenantMember("operator.alpha", TenantRole.TenantOwner)],
            new Dictionary<string, string>(StringComparer.Ordinal) { ["billing.mode"] = "trial" },
            DateTimeOffset.UtcNow);

    /// <summary>
    /// Implements only the two required members, so the async authorization seam comes from the interface
    /// default. Its synchronous reflection deliberately answers <c>Authorized</c>: if the default ever
    /// forwarded to that property again, this implementation would authorize and the test would fail.
    /// </summary>
    private sealed class DefaultOnlyComposition : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
            => TenantLifecycleAuthorizationReflectionState.Authorized;
    }

    private sealed class StubPrincipalResolver(TenantConfigurationPrincipalEvidence evidence)
        : ITenantConfigurationPrincipalResolver
    {
        public ValueTask<TenantConfigurationPrincipalEvidence> ResolveAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(evidence);
    }

    private sealed class ThrowingConfiguration : IReadOnlyDictionary<string, string>
    {
        public IEnumerable<string> Keys => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        public IEnumerable<string> Values => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        public int Count => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        public string this[string key] => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        public bool ContainsKey(string key) => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        public bool TryGetValue(string key, out string value)
            => throw new InvalidOperationException("Raw configuration was inspected before authority.");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ThrowingGlobalAdministratorRows : IReadOnlyList<GlobalAdministratorRow>
    {
        public int Count => throw new InvalidOperationException("Rows were inspected before authority.");

        public GlobalAdministratorRow this[int index]
            => throw new InvalidOperationException("Rows were inspected before authority.");

        public IEnumerator<GlobalAdministratorRow> GetEnumerator()
            => throw new InvalidOperationException("Rows were inspected before authority.");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
