using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

public class DaprTestPrerequisiteDiagnosticsTests {
    [Fact]
    public void SkipReason_NamesDaprPrerequisiteCategoriesWithoutSecrets() {
        string reason = DaprTestPrerequisites.SkipReason;

        reason.ShouldContain("DAPR integration prerequisites are unavailable");
        reason.ShouldContain("dapr init");
        reason.ShouldContain("Redis");
        reason.ShouldContain("placement");
        reason.ShouldContain("scheduler");
        AssertSupportSafe(reason);
    }

    [Fact]
    public void FixturePrerequisiteFailureMessage_NamesDependencyCategoryAndPortOnly() {
        string message = TenantsDaprTestFixture.BuildPrerequisiteFailureMessage(
            [
                "Redis is not responding to PING on localhost:6379",
                $"Dapr placement service is not reachable on localhost:{(OperatingSystem.IsWindows() ? 6050 : 50005)}",
                $"Dapr scheduler service is not reachable on localhost:{(OperatingSystem.IsWindows() ? 6060 : 50006)}",
            ]);

        message.ShouldContain("Dapr infrastructure pre-flight check failed");
        message.ShouldContain("dapr init");
        message.ShouldContain("Redis");
        message.ShouldContain("localhost:6379");
        message.ShouldContain("placement");
        message.ShouldContain(OperatingSystem.IsWindows() ? "localhost:6050" : "localhost:50005");
        message.ShouldContain("scheduler");
        message.ShouldContain(OperatingSystem.IsWindows() ? "localhost:6060" : "localhost:50006");
        AssertSupportSafe(message);
    }

    [Theory]
    [InlineData("daprd exited immediately with code 1.")]
    [InlineData("Dapr sidecar did not become healthy within 60 seconds.")]
    [InlineData("component initialization failed for state.redis.")]
    [InlineData("statestore init timeout while loading actor state store.")]
    public void InfrastructureStartupClassifier_SkipsNarrowDaprStartupFailures(string message) {
        bool result = TenantsDaprTestFixture.IsDaprInfrastructureStartupFailure(new InvalidOperationException(message));

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Domain processing failed for command type CreateTenant.")]
    [InlineData("The statestore actor write failed after a processed command.")]
    [InlineData("Service invocation to /process returned a product error.")]
    [InlineData("Tenant aggregate rejected duplicate command input.")]
    public void InfrastructureStartupClassifier_DoesNotSkipProductFailures(string message) {
        bool result = TenantsDaprTestFixture.IsDaprInfrastructureStartupFailure(new InvalidOperationException(message));

        result.ShouldBeFalse();
    }

    [Fact]
    public void SupportSafeDiagnostic_RedactsSecretsTokensAndPrivateAddresses() {
        string diagnostic = TenantsDaprTestFixture.ToSupportSafeDiagnostic(
            "Bearer abcdefghijklmnopqrstuvwxyz12345 eyJheader.payload.signature Password=s3cr3t AccountKey=abc123 redis://cache.local:6379 10.1.2.3");

        diagnostic.ShouldContain("[redacted-token]");
        diagnostic.ShouldContain("[redacted-jwt]");
        diagnostic.ShouldContain("[redacted-secret]");
        diagnostic.ShouldContain("[redacted-connection]");
        diagnostic.ShouldContain("[redacted-private-address]");
        AssertSupportSafe(diagnostic);
    }

    [Fact]
    public void SupportSafeProcessDiagnostic_NamesCommandCategoryWithoutRawException() {
        const string diagnostic = "Domain processing failed for command type CreateTenant.";

        diagnostic.ShouldContain("CreateTenant");
        diagnostic.ShouldNotContain("Exception");
        diagnostic.ShouldNotContain("Payload");
        AssertSupportSafe(diagnostic);
    }

    private static void AssertSupportSafe(string value) {
        Regex compactJwt = new(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.Compiled);
        Regex bearerToken = new(@"Bearer\s+[A-Za-z0-9._~+/=-]{20,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex connectionString = new(
            @"(AccountKey=|SharedAccessKey=|Password=[^{}\s]|redis://|amqp://|Endpoint=sb://)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex rawPrivateAddress = new(
            @"(?<!localhost:)(?<!127\.0\.0\.1:)\b(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3})\b",
            RegexOptions.Compiled);

        compactJwt.IsMatch(value).ShouldBeFalse("DAPR prerequisite diagnostics must not include compact JWTs.");
        bearerToken.IsMatch(value).ShouldBeFalse("DAPR prerequisite diagnostics must not include bearer tokens.");
        connectionString.IsMatch(value).ShouldBeFalse("DAPR prerequisite diagnostics must not include concrete connection strings.");
        rawPrivateAddress.IsMatch(value).ShouldBeFalse("DAPR prerequisite diagnostics must not include private network addresses.");
    }
}
