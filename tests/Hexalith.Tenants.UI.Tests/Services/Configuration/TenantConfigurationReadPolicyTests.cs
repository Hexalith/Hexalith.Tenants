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
using Microsoft.Extensions.Logging;

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
    public async Task Resolver_contains_non_cancellation_authentication_provider_faults_as_indeterminate()
    {
        TenantConfigurationPrincipalEvidence evidence = await Resolver(
            circuitProvider: new FaultingAuthenticationStateProvider(
                new InvalidOperationException("unsafe provider detail"))).ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        evidence.Subject.ShouldBeNull();
    }

    [Fact]
    public async Task Resolver_propagates_entry_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Resolver(circuitPrincipal: Principal(new Claim("sub", "operator.circuit")))
                .ResolveAsync(cancellation.Token));
    }

    [Fact]
    public async Task Resolver_propagates_caller_cancellation_after_authentication_read_starts()
    {
        var provider = new PendingAuthenticationStateProvider();
        using var cancellation = new CancellationTokenSource();
        Task<TenantConfigurationPrincipalEvidence> resolution = Resolver(
            circuitProvider: provider,
            userContextSubject: "operator.circuit")
            .ResolveAsync(cancellation.Token)
            .AsTask();
        await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        cancellation.Cancel();
        provider.Complete(Principal(new Claim("sub", "operator.circuit")));

        _ = await Should.ThrowAsync<OperationCanceledException>(resolution);
    }

    [Fact]
    public async Task Resolver_prefers_current_circuit_identity_over_stale_authenticated_http_identity()
    {
        ClaimsPrincipal staleHttpPrincipal = Principal(
            new Claim("sub", "operator.http"),
            new Claim("eventstore:tenant", "system"),
            new Claim("roles", "[\"global-admin\"]"));
        ClaimsPrincipal currentCircuitPrincipal = Principal(
            new Claim("sub", "operator.circuit"),
            new Claim("roles", "[\"tenant-reader\"]"));

        TenantConfigurationPrincipalEvidence evidence = await Resolver(
            httpPrincipal: staleHttpPrincipal,
            circuitPrincipal: currentCircuitPrincipal,
            userContextSubject: "operator.circuit").ResolveAsync();

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
    public async Task Resolver_treats_conflicting_explicit_administrator_evidence_as_indeterminate()
    {
        TenantConfigurationPrincipalEvidence evidence = await Resolver(httpPrincipal: Principal(
            new Claim("sub", "operator-1"),
            new Claim("eventstore:tenant", "system"),
            new Claim("global_admin", "true"),
            new Claim("is_global_admin", "false"))).ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        evidence.Subject.ShouldBeNull();
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
    public void An_emptied_environment_override_cannot_clear_a_declared_display_safe_list()
    {
        // `[]`, `""` and an emptied environment override are a single observable state at this layer
        // (Value == "" with no element children), so the provider cannot fail closed on an empty value
        // without taking the shipped valid-empty default dark. What bounds that residual is this: an
        // emptied override does not shorten an already-declared list, because the declaring provider's
        // element children still win. Approval cannot be erased from the environment.
        const string variable = "Tenants__ConfigurationReadPolicy__DisplaySafe";
        string? original = Environment.GetEnvironmentVariable(variable);
        IConfiguration configuration;
        try
        {
            Environment.SetEnvironmentVariable(variable, string.Empty);
            configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes("""
                    {
                      "Tenants": {
                        "ConfigurationReadPolicy": {
                          "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                          "DisplaySafe": ["billing.mode"]
                        }
                      }
                    }
                    """)))
                .AddEnvironmentVariables()
                .Build();
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, original);
        }

        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(configuration).Resolve(
            "tenant.alpha",
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["billing.mode"] = "visible",
            }),
            policy);

        policy.IsAvailable.ShouldBeTrue();
        composition.SafeModel.Rows.ShouldHaveSingleItem().Key.ShouldBe("billing.mode");
    }

    [Fact]
    public void An_emptied_display_safe_element_is_rejected_rather_than_silently_dropped()
    {
        // The one override shape that does reach the bound list — `…__DisplaySafe__0=` — arrives as an
        // empty element rather than as a shorter list, and semantic validation already fails closed on
        // it. Without that guard the emptied slot would bind as an unapprovable blank key.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [],
                  "DisplaySafe": ["", "billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        policy.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void An_empty_display_safe_scalar_approves_nothing_rather_than_widening_approval()
    {
        // Documented residual of the same decision: `"DisplaySafe": ""` is indistinguishable from the
        // valid-empty `[]` default, so it resolves as available with nothing approved. The direction is
        // what makes that acceptable — a malformed empty declaration can only remove rows, never add
        // one, and never promotes an authorized prefix into display approval.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ""
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["billing.mode"] = "hidden",
            }),
            policy);

        policy.IsAvailable.ShouldBeTrue();
        policy.DisplaySafeKeys.ShouldBeEmpty();
        composition.SafeModel.Rows.ShouldBeEmpty();
        composition.SanitizedDetail.Configuration.ShouldBeEmpty();
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

    [Fact]
    public async Task Administrator_role_without_system_scope_is_a_non_administrator_that_keeps_explicit_grants()
    {
        // Review decision 1: an administrator role scoped to a non-system tenant is a well-formed claim
        // that does not meet the wildcard bar. Treating it as indeterminate stripped the caller's own
        // explicit grants and took the whole surface dark.
        ClaimsPrincipal principal = Principal(
            new Claim("sub", "operator.alpha"),
            new Claim("eventstore:tenant", "tenant.alpha"),
            new Claim("roles", "[\"global-admin\"]"));

        TenantConfigurationPrincipalEvidence evidence = await Resolver(httpPrincipal: principal).ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.NonAdministrator);

        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", evidence);

        policy.IsAvailable.ShouldBeTrue();
        policy.IsGlobalAdministrator.ShouldBeFalse();
        policy.AuthorizedPrefixes.ShouldBe(["billing"]);
    }

    [Fact]
    public async Task Subject_claim_normalization_cannot_unlock_a_literal_deployment_grant()
    {
        // The accessor normalizes before yielding UserId. That normalized value cannot corroborate a
        // different raw claim, because deployment grants are keyed by the literal authenticated subject.
        ClaimsPrincipal principal = Principal(
            new Claim("sub", " operator.alpha "),
            new Claim("roles", "[\"tenant-reader\"]"));

        TenantConfigurationPrincipalEvidence evidence = await Resolver(
            httpPrincipal: principal,
            userContextSubject: "operator.alpha").ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        evidence.Subject.ShouldBeNull();

        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", evidence);

        policy.IsAvailable.ShouldBeFalse();
        policy.AuthorizedPrefixes.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("system", "tenant.alpha")]
    [InlineData("system", " ")]
    public async Task Conflicting_or_malformed_tenant_scope_cannot_unlock_the_administrator_wildcard(
        string firstScope,
        string secondScope)
    {
        ClaimsPrincipal principal = Principal(
            new Claim("sub", "operator.alpha"),
            new Claim("eventstore:tenant", firstScope),
            new Claim("eventstore:tenant", secondScope),
            new Claim("roles", "[\"global-admin\"]"));

        TenantConfigurationPrincipalEvidence evidence = await Resolver(httpPrincipal: principal).ResolveAsync();

        evidence.State.ShouldBe(TenantConfigurationPrincipalEvidenceState.Indeterminate);
        evidence.Subject.ShouldBeNull();
    }

    [Fact]
    public void Indeterminate_principal_evidence_makes_policy_unavailable_even_when_the_section_is_valid()
    {
        // The resolver tests prove Indeterminate is produced; this proves the provider acts on it.
        // Without the guard the grant filter runs with a null subject and returns Available with zero
        // prefixes, so an unauthenticated or cross-identity caller would see authorization-safe empty
        // rather than unavailable.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.Indeterminate());

        policy.IsAvailable.ShouldBeFalse();
        policy.AuthorizedPrefixes.ShouldBeEmpty();
        policy.DisplaySafeKeys.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("tenant.beta", "operator.alpha")]
    [InlineData("TENANT.ALPHA", "operator.alpha")]
    [InlineData("tenant.alpha", "OPERATOR.ALPHA")]
    [InlineData("tenant.alpha", "operator.beta")]
    public void Grants_apply_only_to_their_exact_ordinal_tenant_and_subject(string grantTenant, string grantSubject)
    {
        // Every prior fixture granted and queried tenant.alpha, so dropping the tenant conjunct — a
        // cross-tenant policy leak — or switching either comparison to OrdinalIgnoreCase survived.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration($$"""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "{{grantTenant}}", "Subject": "{{grantSubject}}", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        policy.IsAvailable.ShouldBeTrue();
        policy.AuthorizedPrefixes.ShouldBeEmpty();
    }

    [Fact]
    public void Display_approval_is_exact_and_ordinal_so_a_case_variant_key_is_not_approved()
    {
        // The prior confusable fixture key was itself listed in DisplaySafe, so it exercised the prefix
        // gate rather than the display set. A global administrator bypasses prefix matching, which is
        // what makes the display set's comparer the only thing standing between a case variant and a
        // rendered row.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [],
                  "DisplaySafe": ["billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.GlobalAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["billing.mode"] = "visible",
                ["Billing.MODE"] = "case-variant-hidden",
                ["BILLING.MODE"] = "case-variant-hidden-upper",
            }),
            policy);

        composition.SafeModel.Rows.ShouldHaveSingleItem().Key.ShouldBe("billing.mode");
        composition.SafeModel.Rows.ShouldHaveSingleItem().Value.ShouldBe("visible");
    }

    [Theory]
    [InlineData(".billing")]
    [InlineData("бilling.mode")]
    [InlineData("Вilling.mode")]
    public void Empty_segment_and_confusable_keys_cannot_broaden_an_ordinal_prefix_grant(string key)
    {
        // The story required leading/consecutive empty segments and visually confusable prefixes to be
        // pinned at the policy level; none existed. The Cyrillic cases look like `billing` but are
        // different code points, so an ordinal grant must not match them.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration($$"""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["{{key}}"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal) { [key] = "hidden" }),
            policy);

        composition.SafeModel.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void A_consecutive_empty_segment_stays_inside_the_granted_namespace()
    {
        // Pins the boundary rather than assuming it: `billing..mode` begins with `billing.`, so grant
        // `billing` authorizes it. It is an odd key, not an escape from the namespace, and the ordinal
        // rule must not be "fixed" into rejecting it.
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing..mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal) { ["billing..mode"] = "visible" }),
            policy);

        composition.SafeModel.Rows.ShouldHaveSingleItem().Key.ShouldBe("billing..mode");
        composition.SafeModel.Rows.ShouldHaveSingleItem().Namespace.ShouldBe("billing");
    }

    [Fact]
    public void Unavailable_policy_composes_an_unavailable_read_model_rather_than_a_safe_empty_one()
    {
        // The composer's fail-closed branch was never executed by any test: replacing it with
        // Available(tenantId, []) survived the whole suite, so a malformed deployment policy would
        // render the authorization-safe empty state instead of the required unavailable state.
        TenantConfigurationReadPolicyResolution unavailable = new TenantConfigurationReadPolicyProvider(
            Configuration("{ \"Tenants\": { \"ConfigurationReadPolicy\": { \"PrefixGrants\": \"scalar\", \"DisplaySafe\": [] } } }"))
            .Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        unavailable.IsAvailable.ShouldBeFalse();

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal) { ["billing.mode"] = "trial" }),
            unavailable);

        composition.SafeModel.IsAvailable.ShouldBeFalse();
        composition.SafeModel.Rows.ShouldBeEmpty();
        composition.ManagementContext.IsAvailable.ShouldBeFalse();
        composition.SanitizedDetail.Configuration.ShouldBeEmpty();
    }

    [Fact]
    public void Reauthorizing_against_an_unavailable_policy_drops_previously_safe_rows()
    {
        TenantConfigurationReadPolicyResolution granted = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));
        TenantConfigurationComposition composed = TenantConfigurationSafeComposer.Compose(
            Detail(new Dictionary<string, string>(StringComparer.Ordinal) { ["billing.mode"] = "trial" }),
            granted);
        composed.SafeModel.Rows.ShouldHaveSingleItem();

        TenantConfigurationReadPolicyResolution revoked = TenantConfigurationReadPolicyResolution.Unavailable();
        (TenantConfigurationSafeModel safe, TenantConfigurationManagementContext management) =
            TenantConfigurationSafeComposer.Reauthorize(composed.SafeModel, TenantStatus.Active, revoked, degraded: false);

        safe.IsAvailable.ShouldBeFalse();
        safe.Rows.ShouldBeEmpty();
        management.IsAvailable.ShouldBeFalse();
        management.RemovableRows.ShouldBeEmpty();
    }

    [Fact]
    public void Configuration_reload_invalidates_cached_grants_before_the_next_resolution()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenants:ConfigurationReadPolicy:PrefixGrants:0:TenantId"] = "tenant.alpha",
                ["Tenants:ConfigurationReadPolicy:PrefixGrants:0:Subject"] = "operator.alpha",
                ["Tenants:ConfigurationReadPolicy:PrefixGrants:0:Prefix"] = "billing",
                ["Tenants:ConfigurationReadPolicy:DisplaySafe:0"] = "billing.mode",
            })
            .Build();
        TenantConfigurationReadPolicyProvider provider = new(configuration);
        TenantConfigurationPrincipalEvidence principal =
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha");

        provider.Resolve("tenant.alpha", principal).AuthorizedPrefixes.ShouldBe(["billing"]);

        configuration["Tenants:ConfigurationReadPolicy:PrefixGrants:0:Subject"] = "operator.other";
        configuration.Reload();

        TenantConfigurationReadPolicyResolution reloaded = provider.Resolve("tenant.alpha", principal);
        reloaded.IsAvailable.ShouldBeTrue();
        reloaded.AuthorizedPrefixes.ShouldBeEmpty();
    }

    [Fact]
    public void Policy_diagnostics_are_structured_safe_and_emitted_once_per_invalid_configuration_load()
    {
        const string rawDeclaration = "raw-secret-prefix";
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenants:ConfigurationReadPolicy:PrefixGrants"] = rawDeclaration,
            })
            .Build();
        CapturingLogger<TenantConfigurationReadPolicyProvider> logger = new();
        TenantConfigurationReadPolicyProvider provider = new(configuration, logger);
        TenantConfigurationPrincipalEvidence principal =
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha");

        _ = provider.Resolve("tenant.alpha", principal);
        _ = provider.Resolve("tenant.alpha", principal);
        configuration.Reload();
        _ = provider.Resolve("tenant.alpha", principal);
        _ = provider.Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.Indeterminate());

        var deploymentEntries = logger.Entries
            .Where(static entry => entry.EventId.Id == 2101)
            .ToArray();
        deploymentEntries.Length.ShouldBe(2);
        deploymentEntries.ShouldAllBe(static entry => entry.Level == LogLevel.Warning);
        deploymentEntries.ShouldAllBe(static entry => entry.Message.Contains(
            nameof(TenantConfigurationPolicyFailure.ScalarCollection),
            StringComparison.Ordinal));

        var principalEntry = logger.Entries.Single(static entry => entry.EventId.Id == 2100);
        principalEntry.Level.ShouldBe(LogLevel.Debug);
        principalEntry.Message.ShouldContain(
            nameof(TenantConfigurationPolicyFailure.IndeterminatePrincipal),
            Case.Sensitive);
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(rawDeclaration, StringComparison.Ordinal));
    }

    [Fact]
    public void Composed_rows_do_not_track_later_mutation_of_the_caller_owned_dictionary()
    {
        // The previous defensive-copy test composed under an empty DisplaySafe list, so the rows were
        // empty with or without a copy and the assertion could not fail. This one composes a real row
        // first, then both adds to and removes from the source.
        Dictionary<string, string> raw = new(StringComparer.Ordinal)
        {
            ["billing.mode"] = "trial",
        };
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode", "billing.later"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(Detail(raw), policy);

        raw["billing.later"] = "later-value";
        _ = raw.Remove("billing.mode");

        composition.SafeModel.Rows.ShouldHaveSingleItem().Key.ShouldBe("billing.mode");
        composition.SafeModel.Rows.ShouldHaveSingleItem().Value.ShouldBe("trial");
        composition.ManagementContext.RemovableRows.ShouldHaveSingleItem().Key.ShouldBe("billing.mode");
    }

    [Fact]
    public void A_null_configuration_value_omits_only_that_row()
    {
        // The contract types values as non-null but System.Text.Json does not enforce it. Throwing
        // inside the composer made the gateway's blanket catch discard the entire tenant detail.
        Dictionary<string, string> raw = new(StringComparer.Ordinal)
        {
            ["billing.mode"] = "trial",
            ["billing.broken"] = null!,
        };
        TenantConfigurationReadPolicyResolution policy = new TenantConfigurationReadPolicyProvider(Configuration("""
            {
              "Tenants": {
                "ConfigurationReadPolicy": {
                  "PrefixGrants": [{ "TenantId": "tenant.alpha", "Subject": "operator.alpha", "Prefix": "billing" }],
                  "DisplaySafe": ["billing.mode", "billing.broken"]
                }
              }
            }
            """)).Resolve("tenant.alpha", TenantConfigurationPrincipalEvidence.NonAdministrator("operator.alpha"));

        TenantConfigurationComposition composition = TenantConfigurationSafeComposer.Compose(
            Detail(raw),
            policy);

        composition.SafeModel.IsAvailable.ShouldBeTrue();
        composition.SafeModel.Rows.ShouldHaveSingleItem().Key.ShouldBe("billing.mode");
    }

    private static TenantConfigurationPrincipalResolver Resolver(
        ClaimsPrincipal? httpPrincipal = null,
        ClaimsPrincipal? circuitPrincipal = null,
        AuthenticationStateProvider? circuitProvider = null,
        bool supplyUserContext = true,
        string? userContextSubject = null)
    {
        HttpContextAccessor http = new()
        {
            HttpContext = httpPrincipal is null ? null : new DefaultHttpContext { User = httpPrincipal },
        };
        CircuitServicesAccessor circuit = new();
        if (circuitPrincipal is not null || circuitProvider is not null)
        {
            circuit.Services = new ServiceCollection()
                .AddSingleton(circuitProvider ?? new StubAuthenticationStateProvider(circuitPrincipal!))
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

    private sealed class FaultingAuthenticationStateProvider(Exception exception) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromException<AuthenticationState>(exception);
    }

    private sealed class PendingAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly TaskCompletionSource<AuthenticationState> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            Entered.TrySetResult();
            return _completion.Task;
        }

        public void Complete(ClaimsPrincipal principal)
            => _completion.SetResult(new AuthenticationState(principal));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => EmptyScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception), exception));

        private sealed class EmptyScope : IDisposable
        {
            public static EmptyScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
