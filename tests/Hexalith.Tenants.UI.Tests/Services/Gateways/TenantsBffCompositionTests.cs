using System.Text;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.Configuration;

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

    private static TenantsBffComposition Composition(
        string json,
        TenantConfigurationPrincipalEvidence? evidence = null)
        => new(
            new UnavailableTenantCommandGateway(),
            principalResolver: new StubPrincipalResolver(
                evidence ?? TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha")),
            policyProvider: new TenantConfigurationReadPolicyProvider(Configuration(json)));

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

    private static TenantDetail Detail()
        => new(
            "tenant.alpha",
            "Alpha",
            "Description",
            TenantStatus.Active,
            [new TenantMember("operator.alpha", TenantRole.TenantOwner)],
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
}
