using System.Security.Claims;
using System.Text;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Configuration;

public sealed class TenantConfigurationReadPolicyTests
{
    [Fact]
    public async Task Resolver_uses_one_authenticated_http_identity_for_subject_scope_and_administrator_evidence()
    {
        ClaimsPrincipal principal = Principal(
            new Claim("sub", "operator.alpha"),
            new Claim("eventstore:tenant", "system"),
            new Claim("roles", "[\"tenant-reader\",\"global-admin\"]"));
        TenantConfigurationPrincipalResolver resolver = Resolver(httpPrincipal: principal);

        TenantConfigurationPrincipalEvidence evidence = await resolver.ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.GlobalAdministrator);
        evidence.Subject.ShouldBe("operator.alpha");
    }

    [Fact]
    public async Task Resolver_uses_circuit_authentication_state_when_http_context_is_unavailable()
    {
        ClaimsPrincipal principal = Principal(new Claim("sub", "operator.circuit"));
        TenantConfigurationPrincipalResolver resolver = Resolver(circuitPrincipal: principal);

        TenantConfigurationPrincipalEvidence evidence = await resolver.ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.NonAdministrator);
        evidence.Subject.ShouldBe("operator.circuit");
    }

    [Fact]
    public async Task Resolver_fails_closed_for_multiple_authenticated_identities_and_malformed_role_collections()
    {
        ClaimsPrincipal multiple = new(
        [
            new ClaimsIdentity([new Claim("sub", "operator.alpha")], "first"),
            new ClaimsIdentity([new Claim("eventstore:tenant", "system"), new Claim("global_admin", "true")], "second"),
        ]);
        TenantConfigurationPrincipalEvidence crossIdentity = await Resolver(httpPrincipal: multiple).ResolveAsync();
        TenantConfigurationPrincipalEvidence malformed = await Resolver(httpPrincipal: Principal(
            new Claim("sub", "operator.alpha"),
            new Claim("roles", "[\"global-admin\""))).ResolveAsync();

        crossIdentity.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        malformed.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        crossIdentity.Subject.ShouldBeNull();
        malformed.Subject.ShouldBeNull();
    }

    [Fact]
    public async Task Resolver_requires_user_context_subject_to_match_the_authenticated_identity()
    {
        ClaimsPrincipal principal = Principal(new Claim("sub", "operator.alpha"));

        TenantConfigurationPrincipalEvidence missing = await Resolver(
            httpPrincipal: principal,
            supplyUserContext: false).ResolveAsync();
        TenantConfigurationPrincipalEvidence mismatched = await Resolver(
            httpPrincipal: principal,
            userContextSubject: "operator.beta").ResolveAsync();

        missing.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        mismatched.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        missing.Subject.ShouldBeNull();
        mismatched.Subject.ShouldBeNull();
    }

    [Theory]
    [InlineData("global_admin", "true")]
    [InlineData("is_global_admin", "true")]
    [InlineData(ClaimTypes.Role, "GlobalAdministrator")]
    [InlineData("role", "global-administrator")]
    [InlineData("roles", "tenant-reader,global-admin")]
    [InlineData("roles", "[\"tenant-reader\",\"global-admin\"]")]
    public async Task Resolver_supports_each_positive_administrator_claim_shape(string claimType, string claimValue)
    {
        ClaimsPrincipal principal = Principal(
            new Claim("sub", "operator.alpha"),
            new Claim("eventstore:tenant", "system"),
            new Claim(claimType, claimValue));

        TenantConfigurationPrincipalEvidence evidence = await Resolver(httpPrincipal: principal).ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.GlobalAdministrator);
    }

    [Fact]
    public void Composer_requires_literal_longest_prefix_and_exact_positive_display_approval()
    {
        IConfiguration configuration = Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [
                    { "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "a" },
                    { "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "a.deep" },
                    { "TenantId": "tenant.alpha", "Subject": "someone.else", "Prefix": "hidden" }
                  ],
                  "DisplaySafe": ["a", "a.", "a.deep.value", "ab.value", "A.value", "hidden.value"]
                }
              }
            }
            """);
        TenantConfigurationReadPolicyProvider provider = new(configuration);
        TenantConfigurationReadPolicyResolution policy = provider.Resolve(
            "tenant.alpha",
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));
        TenantDetail detail = Detail(new Dictionary<string, string>
        {
            ["a"] = "exact",
            ["a."] = "empty-segment",
            ["a.deep.value"] = "nested",
            ["a.unregistered"] = "undefined",
            ["ab.value"] = "sibling",
            ["A.value"] = "case-confusable",
            ["hidden.value"] = "hidden",
        });

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(detail, policy);

        composition.SafeModel.Rows.Select(static row => row.Key).ShouldBe(["a", "a.", "a.deep.value"]);
        composition.SafeModel.Rows.Single(static row => row.Key == "a.deep.value").Namespace.ShouldBe("a.deep");
        composition.SanitizedDetail.Configuration.ShouldBeEmpty();
        composition.ManagementContext.IsKeyAuthorized("a").ShouldBeTrue();
        composition.ManagementContext.IsKeyAuthorized("a.more").ShouldBeTrue();
        composition.ManagementContext.IsKeyAuthorized("ab.value").ShouldBeFalse();
    }

    [Fact]
    public void Global_administrator_receives_only_the_namespace_wildcard_while_values_still_need_exact_approval()
    {
        IConfiguration configuration = Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [],
                  "DisplaySafe": ["approved.value"]
                }
              }
            }
            """);
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(configuration).Resolve(
            "tenant.alpha",
            TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>
            {
                ["approved.value"] = "visible",
                ["unregistered.value"] = "hidden",
            }),
            policy);

        composition.ManagementContext.AuthorizedPrefixes.ShouldBe(["*"]);
        composition.SafeModel.Rows.ShouldHaveSingleItem().Key.ShouldBe("approved.value");
        composition.SafeModel.Rows.ShouldHaveSingleItem().Value.ShouldBe("visible");
    }

    [Theory]
    [InlineData("{ \"Tenants\": { } }")]
    [InlineData("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": \"a\", \"DisplaySafe\": [] } } }")]
    [InlineData("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": [], \"DisplaySafe\": \"a\" } } }")]
    [InlineData("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": [{ \"TenantId\": \"tenant.alpha\", \"Subject\": \"operator.alpha\", \"Prefix\": \"a.\" }], \"DisplaySafe\": [] } } }")]
    [InlineData("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": [{ \"TenantId\": \"tenant.alpha\", \"Subject\": \"operator.alpha\", \"Prefix\": \"a b\" }], \"DisplaySafe\": [] } } }")]
    public void Missing_malformed_scalar_or_invalid_policy_is_unavailable_without_throwing(string json)
    {
        TenantConfigurationReadPolicyProvider provider = new(Configuration(json));

        TenantConfigurationReadPolicyResolution policy = provider.Resolve(
            "tenant.alpha",
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        policy.IsAvailable.ShouldBeFalse();
    }

    [Theory]
    [InlineData("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": [{ \"TenantId\": \"tenant.alpha\", \"Subject\": \"operator.alpha\", \"Prefix\": \"a\" }, { \"TenantId\": \"tenant.alpha\", \"Subject\": \"operator.alpha\", \"Prefix\": \"a\" }], \"DisplaySafe\": [] } } }")]
    [InlineData("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": [], \"DisplaySafe\": [\"a\", \"a\"] } } }")]
    public void Duplicate_policy_entries_are_unavailable(string json)
    {
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration(json)).Resolve(
            "tenant.alpha",
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        policy.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Valid_empty_policy_is_safe_empty_and_defensively_copies_caller_owned_data()
    {
        Dictionary<string, string> raw = new(StringComparer.Ordinal)
        {
            ["hidden.key"] = "hidden-value",
        };
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [],
                  "DisplaySafe": []
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(Detail(raw), policy);
        raw["later.key"] = "later-value";

        policy.IsAvailable.ShouldBeTrue();
        composition.SafeModel.IsAvailable.ShouldBeTrue();
        composition.SafeModel.Rows.ShouldBeEmpty();
        composition.ManagementContext.RemovableRows.ShouldBeEmpty();
        composition.SanitizedDetail.Configuration.ShouldBeEmpty();
    }

    private static TenantConfigurationPrincipalResolver Resolver(
        ClaimsPrincipal? httpPrincipal = null,
        ClaimsPrincipal? circuitPrincipal = null,
        bool supplyUserContext = true,
        string? userContextSubject = null)
    {
        HttpContextAccessor http = new()
        {
            HttpContext = httpPrincipal is null ? null : new DefaultHttpContext { User = httpPrincipal },
        };
        CircuitServicesAccessor circuit = new();
        if (circuitPrincipal is not null)
        {
            circuit.Services = new ServiceCollection()
                .AddSingleton<AuthenticationStateProvider>(new StubAuthenticationStateProvider(circuitPrincipal))
                .BuildServiceProvider();
        }

        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns(supplyUserContext
            ? userContextSubject ?? SubjectFrom(httpPrincipal) ?? SubjectFrom(circuitPrincipal)
            : null);
        return new TenantConfigurationPrincipalResolver(http, circuit, userContext);
    }

    private static string? SubjectFrom(ClaimsPrincipal? principal)
        => principal?.Claims.SingleOrDefault(static claim => string.Equals(claim.Type, "sub", StringComparison.Ordinal))?.Value;

    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));

    private static IConfiguration Configuration(string json)
        => new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();

    private static TenantDetail Detail(IReadOnlyDictionary<string, string> configuration)
        => new(
            "tenant.alpha",
            "Alpha",
            "Description",
            TenantStatus.Active,
            [new TenantMember("operator.alpha", TenantRole.TenantOwner)],
            configuration,
            DateTimeOffset.UtcNow);

    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }
}
