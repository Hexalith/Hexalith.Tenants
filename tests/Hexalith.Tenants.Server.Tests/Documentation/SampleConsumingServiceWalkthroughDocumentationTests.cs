using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class SampleConsumingServiceWalkthroughDocumentationTests {
    private static readonly Regex CSharpFenceRegex = new(
        "```csharp\\s*(?<code>.*?)\\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex JwtLikeTokenRegex = new(
        "eyJ[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+",
        RegexOptions.Compiled);

    [Fact]
    public void Walkthrough_references_required_sample_files_and_related_guidance() {
        string walkthrough = ReadWalkthrough();
        string repoRoot = RepositoryPath();

        string[] requiredPaths =
        [
            "samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj",
            "samples/Hexalith.Tenants.Sample/Program.cs",
            "samples/Hexalith.Tenants.Sample/Handlers/SampleLoggingEventHandler.cs",
            "samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs",
            "samples/Hexalith.Tenants.Sample/Endpoints/TenantConfigurationEndpoints.cs",
            "src/Hexalith.Tenants.AppHost/Program.cs",
            "src/Hexalith.Tenants.AppHost/HexalithTenantsSample.cs",
            "samples/Hexalith.Tenants.Sample.Tests/Registration/SampleRegistrationTests.cs",
            "samples/Hexalith.Tenants.Sample.Tests/Endpoints/AccessCheckEndpointsTests.cs",
            "samples/Hexalith.Tenants.Sample.Tests/Endpoints/TenantConfigurationEndpointsTests.cs",
            "docs/event-contract-reference.md",
            "docs/idempotent-event-processing.md",
            "docs/cross-aggregate-timing.md",
        ];

        foreach (string path in requiredPaths) {
            File.Exists(Path.Combine(repoRoot, path)).ShouldBeTrue($"{path} must exist because the walkthrough references it.");
            walkthrough.ShouldContain(path);
        }

        walkthrough.ShouldContain("File map");
        walkthrough.ShouldContain("DI/subscription setup");
        walkthrough.ShouldContain("AppHost sample registration");
        walkthrough.ShouldContain("sample tests");
    }

    [Fact]
    public void Walkthrough_documents_current_subscription_setup_and_under_twenty_line_target() {
        string walkthrough = ReadWalkthrough();
        string program = File.ReadAllText(RepositoryPath("samples", "Hexalith.Tenants.Sample", "Program.cs"));
        string[] meaningfulLines = MeaningfulTenantRegistrationLines(program);

        meaningfulLines.Length.ShouldBeLessThan(20);
        walkthrough.ShouldContain("under 20 meaningful lines");
        walkthrough.ShouldContain("AddHexalithTenants()");
        walkthrough.ShouldContain("AddEventStoreDomainEventHandler<UserAddedToTenant, SampleLoggingEventHandler>()");
        walkthrough.ShouldContain("AddEventStoreDomainEventHandler<UserRemovedFromTenant, SampleLoggingEventHandler>()");
        walkthrough.ShouldContain("AddEventStoreDomainEventHandler<TenantDisabled, SampleLoggingEventHandler>()");
        walkthrough.ShouldContain("UseCloudEvents()");
        walkthrough.ShouldContain("MapSubscribeHandler()");
        walkthrough.ShouldContain("MapEventStoreDomainEvents()");
        walkthrough.ShouldContain("EventStoreDomainEventsOptions");
        walkthrough.ShouldContain("`EventStoreDomainEventsOptions` supplies these subscription defaults");
        walkthrough.ShouldContain("pubsub");
        walkthrough.ShouldContain("tenants.events");
        walkthrough.ShouldContain("`MapEventStoreDomainEvents()` maps the programmatic subscription endpoint at");
        walkthrough.ShouldContain("/tenants/events");
        walkthrough.ShouldNotContain("Programmatic subscription endpoint: `/tenants/events`");
        walkthrough.ShouldContain("must not create one DAPR topic per tenant event type");
        walkthrough.ShouldContain("reusable");
        walkthrough.ShouldContain("sample-specific");
    }

    [Fact]
    public void Walkthrough_documents_projection_events_access_configuration_and_eventual_consistency() {
        string walkthrough = ReadWalkthrough();

        string[] projectionEvents =
        [
            "TenantCreated",
            "TenantUpdated",
            "TenantDisabled",
            "TenantEnabled",
            "UserAddedToTenant",
            "UserRemovedFromTenant",
            "UserRoleChanged",
            "TenantConfigurationSet",
            "TenantConfigurationRemoved",
        ];

        foreach (string eventName in projectionEvents) {
            walkthrough.ShouldContain(eventName);
        }

        walkthrough.ShouldContain("TenantProjectionEventHandler");
        walkthrough.ShouldContain("ITenantProjectionStore");
        walkthrough.ShouldContain("TenantLocalState");
        walkthrough.ShouldContain("/access/{tenantId}/{userId}");
        walkthrough.ShouldContain("TenantRole.Unknown");
        walkthrough.ShouldContain("out-of-range");
        walkthrough.ShouldContain("/configuration/{tenantId}/sample");
        walkthrough.ShouldContain("sample.");
        walkthrough.ShouldContain("billing.plan");
        walkthrough.ShouldContain("eventual consistency");
        walkthrough.ShouldContain("EventStore remains the durable source of truth");
        walkthrough.ShouldContain("local projection state");
    }

    [Fact]
    public void Walkthrough_documents_copy_adapt_deployment_and_security_boundaries() {
        string walkthrough = ReadWalkthrough();

        walkthrough.ShouldContain("Safe to copy");
        walkthrough.ShouldContain("Application-specific");
        walkthrough.ShouldContain("Deployment supplied");
        walkthrough.ShouldContain("DAPR AppId");
        walkthrough.ShouldContain("OIDC/JWT tokens");
        walkthrough.ShouldContain("tenant IDs");
        walkthrough.ShouldContain("user IDs");
        walkthrough.ShouldContain("production storage connection strings");
        walkthrough.ShouldContain("SampleLoggingEventHandler");
        walkthrough.ShouldContain("intentionally does not log the sample user ID or role");
        walkthrough.ShouldContain("InMemoryTenantProjectionStore");
        walkthrough.ShouldContain("durable `ITenantProjectionStore`");
        walkthrough.ShouldContain("bounded/shared deduplication");
        walkthrough.ShouldContain("Do not log full event payloads");

        JwtLikeTokenRegex.IsMatch(walkthrough).ShouldBeFalse("The walkthrough must not include JWT-like raw token values.");
        walkthrough.ShouldNotContain("Authorization: Bearer ");
        walkthrough.ShouldNotContain("password=");
        walkthrough.ShouldNotContain("client_secret");
    }

    [Fact]
    public void Walkthrough_csharp_registration_snippet_matches_current_sample_program() {
        string walkthrough = ReadWalkthrough();
        string program = File.ReadAllText(RepositoryPath("samples", "Hexalith.Tenants.Sample", "Program.cs"));
        string[] sourceMeaningfulLines = MeaningfulTenantRegistrationLines(program);

        string registrationSnippet = CSharpFenceRegex
            .Matches(walkthrough)
            .Select(static match => match.Groups["code"].Value)
            .Single(code => code.Contains("AddHexalithTenants", StringComparison.Ordinal));

        string[] documentedMeaningfulLines = MeaningfulTenantRegistrationLines(registrationSnippet);

        documentedMeaningfulLines.ShouldBe(sourceMeaningfulLines);
    }

    [Fact]
    public void Navigation_links_to_walkthrough_and_uses_current_registration_wording() {
        string readme = File.ReadAllText(RepositoryPath("README.md"));
        string quickstart = File.ReadAllText(RepositoryPath("docs", "quickstart.md"));
        string demo = File.ReadAllText(RepositoryPath("docs", "demo.md"));

        readme.ShouldContain("docs/sample-consuming-service-walkthrough.md");
        quickstart.ShouldContain("[Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)");
        demo.ShouldContain("[Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)");
        demo.ShouldContain("under 20 meaningful tenant registration lines");
        demo.ShouldNotContain("12 lines of DI config");
    }

    private static string[] MeaningfulTenantRegistrationLines(string source)
        => source
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(IsTenantRegistrationLine)
            .ToArray();

    private static bool IsTenantRegistrationLine(string line)
        => !string.IsNullOrWhiteSpace(line)
            && !line.StartsWith("//", StringComparison.Ordinal)
            && (
                line.Contains("AddHexalithTenants", StringComparison.Ordinal)
                || line.Contains("AddEventStoreDomainEventHandler", StringComparison.Ordinal)
                || line.Contains("UseCloudEvents", StringComparison.Ordinal)
                || line.Contains("MapSubscribeHandler", StringComparison.Ordinal)
                || line.Contains("MapEventStoreDomainEvents", StringComparison.Ordinal));

    private static string ReadWalkthrough()
        => File.ReadAllText(RepositoryPath("docs", "sample-consuming-service-walkthrough.md"));

    private static string RepositoryPath(params string[] segments) {
        string repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string direct = Path.GetFullPath(Path.Combine(
            new[] { repoRoot }.Concat(segments).ToArray()));
        if (File.Exists(direct) || Directory.Exists(direct)) {
            return direct;
        }

        // A dependent module (e.g. Hexalith.EventStore) is a nested submodule of this repository
        // that may be left uninitialized when this repository is itself a submodule of a parent
        // that checks the dependency out as a root-level sibling. Fall back to that sibling.
        if (segments.Length > 0 && segments[0].StartsWith("Hexalith.", StringComparison.Ordinal)) {
            string sibling = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, ".." }.Concat(segments).ToArray()));
            if (File.Exists(sibling) || Directory.Exists(sibling)) {
                return sibling;
            }
        }

        return direct;
    }
}
