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

    [Fact]
    public async Task BlankDirectEventStoreTenantClaimIsNotRepairedBySourceClaims() {
        ClaimsPrincipal principal = CreatePrincipal(
            new Claim("sub", "user-1"),
            new Claim(TenantClaimType, " "),
            new Claim("tenants", "system"));

        ClaimsPrincipal result = await _transformation.TransformAsync(principal);
        TenantValidationResult validation = await new ClaimsTenantValidator()
            .ValidateAsync(result, "system", CancellationToken.None);

        TenantClaims(result).ShouldBe([" "]);
        validation.IsAuthorized.ShouldBeFalse();
        validation.ReasonCode.ShouldBe(AuthorizationFailureReason.PrincipalNotMember);
    }
}
