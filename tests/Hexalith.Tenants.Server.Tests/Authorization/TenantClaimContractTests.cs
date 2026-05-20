using System.Security.Claims;
using System.Text.Json;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Contracts.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Authorization;

public class TenantClaimContractTests {
    private const string TenantClaimType = "eventstore:tenant";

    private readonly EventStoreClaimsTransformation _transformation =
        new(NullLogger<EventStoreClaimsTransformation>.Instance);

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme));

    private static string[] TenantClaims(ClaimsPrincipal principal)
        => principal.FindAll(TenantClaimType).Select(c => c.Value).ToArray();

    [Fact]
    public void ConfigureJwtBearerOptionsPreservesOriginalJwtClaimNames() {
        var options = new JwtBearerOptions();
        var authOptions = Options.Create(new EventStoreAuthenticationOptions {
            Issuer = "hexalith-dev",
            Audience = "hexalith-tenants",
            SigningKey = "this-is-a-development-signing-key-minimum-32-chars",
        });
        var configurer = new ConfigureJwtBearerOptions(authOptions, NullLoggerFactory.Instance);

        configurer.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        options.MapInboundClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task TenantsJsonArraySourceClaimNormalizesToEventStoreTenantClaims() {
        string tenants = JsonSerializer.Serialize(new[] { "system", "tenant-a" });
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("tenants", tenants));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);

        TenantClaims(result).ShouldBe(["system", "tenant-a"]);
        result.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("user-1");
    }

    [Fact]
    public async Task TenantsSpaceDelimitedSourceClaimNormalizesToEventStoreTenantClaims() {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("tenants", "system tenant-a"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);

        TenantClaims(result).ShouldBe(["system", "tenant-a"]);
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("tid")]
    public async Task SingularSourceClaimNormalizesToEventStoreTenantClaim(string claimType) {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(claimType, "system"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);

        TenantClaims(result).ShouldBe(["system"]);
    }

    [Fact]
    public async Task TenantIdSourceClaimTakesPrecedenceOverTidFallback() {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("tenant_id", "system"),
            new Claim("tid", "tenant-a"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);

        TenantClaims(result).ShouldBe(["system"]);
    }

    [Fact]
    public async Task MultipleSourceClaimTypesProduceExplicitEffectiveTenantSet() {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("tenants", "system"),
            new Claim("tenant_id", "tenant-a"),
            new Claim("tid", "tenant-b"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);

        TenantClaims(result).ShouldBe(["system", "tenant-a"]);
    }

    [Fact]
    public async Task DuplicateSourceTenantValuesAreRetainedAndStillAuthorizeSystemTenant() {
        string tenants = JsonSerializer.Serialize(new[] { "system", "system" });
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("tenants", tenants));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        TenantClaims(result).ShouldBe(["system", "system"]);
        validation.IsAuthorized.ShouldBeTrue();
    }

    [Fact]
    public async Task WhitespaceOnlyTenantSourceClaimFailsClosedForSystemTenant() {
        string tenants = JsonSerializer.Serialize(new[] { " " });
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim("tenants", tenants));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        validation.IsAuthorized.ShouldBeFalse();
        validation.ReasonCode.ShouldBe(AuthorizationFailureReason.PrincipalNotMember);
    }

    [Fact]
    public async Task DirectEventStoreTenantClaimShortCircuitsConflictingSourceClaims() {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(TenantClaimType, "system"),
            new Claim("tenants", "tenant-a"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);

        TenantClaims(result).ShouldBe(["system"]);
        result.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("user-1");
    }

    // P11: extended to cover tab and newline alongside space and empty string. The documented
    // contract ("whitespace tenant claims survive normalization and fail closed at validation")
    // is now pinned across the four whitespace-shape inputs the validator's
    // `string.IsNullOrWhiteSpace` filter accepts. The test name was renamed
    // (`BlankOrEmpty...`) because the previous name said "Blank" but covered empty-string too.
    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task BlankOrEmptyDirectEventStoreTenantClaimIsNotRepairedBySourceClaims(string directValue) {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(TenantClaimType, directValue),
            new Claim("tenants", "system"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        TenantClaims(result).ShouldBe([directValue]);
        validation.IsAuthorized.ShouldBeFalse();
        validation.ReasonCode.ShouldBe(AuthorizationFailureReason.PrincipalNotMember);
    }

    // P9: extended to cover every global-admin claim shape that `GlobalAdministratorHelper`
    // accepts and the boolean parser's casing contract. A regression that tightens the helper
    // (e.g., dropping role-based shapes, requiring exact `true` casing) would otherwise show up
    // only when a production IdP emitted the dropped shape.
    [Theory]
    [InlineData("global_admin", "true")]
    [InlineData("global_admin", "True")]
    [InlineData("global_admin", "TRUE")]
    [InlineData("is_global_admin", "true")]
    [InlineData("role", "GlobalAdministrator")]
    [InlineData("role", "global-administrator")]
    [InlineData("role", "global-admin")]
    [InlineData("roles", "[\"GlobalAdministrator\"]")]
    [InlineData("roles", "GlobalAdministrator other-role")]
    [InlineData("roles", "user,GlobalAdministrator")]
    public async Task GlobalAdministratorMissingTenantClaimIsAuthorizedForSystemTenant(string claimType, string claimValue) {
        // Global administrators bypass tenant matching in ClaimsTenantValidator. This test locks
        // the documented host behavior across every accepted global-admin claim shape so the
        // global-admin tenant-claim contract cannot regress silently. The Tenants host does NOT
        // register the EventStore rate limiter, so the "anonymous" partition fallback consequence
        // does not apply here — see docs/production-auth-claim-contract.md#global-administrators.
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "admin-user"),
            new Claim(claimType, claimValue));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        TenantClaims(result).ShouldBeEmpty();
        validation.IsAuthorized.ShouldBeTrue();
    }

    // P10: extended to cover the boolean-parser deny shapes alongside the no-claim case. The
    // helper accepts only `bool.TryParse` truthy values, so `global_admin=false`, `=""`, `=yes`,
    // and `=1` must all NOT elevate. Locking these in stops a future "presence == bypass"
    // refactor from silently elevating tokens carrying a non-truthy boolean.
    [Theory]
    [InlineData(null, null)]
    [InlineData("global_admin", "false")]
    [InlineData("global_admin", "")]
    [InlineData("global_admin", "yes")]
    [InlineData("global_admin", "1")]
    [InlineData("is_global_admin", "false")]
    public async Task NonGlobalAdministratorMissingTenantClaimFailsClosedForSystemTenant(string? claimType, string? claimValue) {
        // Companion to GlobalAdministratorMissingTenantClaim... — proves that the same missing-
        // tenant principal without truthy global-admin evidence is denied. Spec Tasks line 47
        // requires non-global-admin and global-admin missing-tenant cases be kept distinct.
        ClaimsPrincipal principal = claimType is null
            ? CreatePrincipal(new Claim("sub", "regular-user"))
            : CreatePrincipal(new Claim("sub", "regular-user"), new Claim(claimType, claimValue ?? string.Empty));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        TenantClaims(result).ShouldBeEmpty();
        validation.IsAuthorized.ShouldBeFalse();
        validation.ReasonCode.ShouldBe(AuthorizationFailureReason.PrincipalNotMember);
    }

    // Pins docs/production-auth-claim-contract.md:13 — "Do not use `name` as the trusted subject."
    // A token carrying only `name` (no `sub`) must NOT be promoted to NameIdentifier and must
    // NOT authorize without an eventstore:tenant claim. Sourced from 11-2 review deferred-work
    // (see _bmad-output/implementation-artifacts/deferred-work.md).
    [Fact]
    public async Task NameOnlyClaimWithoutSubDoesNotEstablishTrustedSubject() {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("name", "display-only-user"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        // NOTE: assert on the Claim itself, not `?.Value`. The conditional access on a missing
        // claim short-circuits the whole expression to null and `.ShouldBeNull()` is never
        // called — silently no-op. Existing tests use `?.Value.ShouldBe("expected")` for
        // positive checks, which is correct because they expect the claim to be present.
        result.FindFirst("sub").ShouldBeNull();
        result.FindFirst(ClaimTypes.NameIdentifier).ShouldBeNull();
        TenantClaims(result).ShouldBeEmpty();
        validation.IsAuthorized.ShouldBeFalse();
        validation.ReasonCode.ShouldBe(AuthorizationFailureReason.PrincipalNotMember);
    }
}
