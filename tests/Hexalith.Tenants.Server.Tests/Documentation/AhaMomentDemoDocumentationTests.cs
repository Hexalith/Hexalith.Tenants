using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class AhaMomentDemoDocumentationTests {
    private static readonly Regex JsonFenceRegex = new(
        "```json\\s*(?<json>.*?)\\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex UlidRegex = new(
        "^[0-9A-HJKMNP-TV-Z]{26}$",
        RegexOptions.Compiled);

    private static readonly Regex JwtLikeTokenRegex = new(
        "eyJ[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+",
        RegexOptions.Compiled);

    [Fact]
    public void Demo_references_current_topology_routes_and_sample_subscription_sources() {
        string demo = ReadDemo();
        string bash = ReadScript("demo.sh");
        string powershell = ReadScript("demo.ps1");
        string readme = File.ReadAllText(RepositoryPath("README.md"));
        string appHost = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants.AppHost", "Program.cs"));
        string sampleProgram = File.ReadAllText(RepositoryPath("samples", "Hexalith.Tenants.Sample", "Program.cs"));
        string combined = string.Join('\n', demo, bash, powershell);

        readme.ShouldContain("docs/demo.md");
        appHost.ShouldContain("AddProject<HexalithTenantsSample>(\"sample\")");
        sampleProgram.ShouldContain("MapEventStoreDomainEvents()");

        string[] requiredReferences =
        [
            "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj",
            "eventstore",
            "sample",
            "POST /api/v1/commands",
            "GET /api/v1/commands/status/{correlationId}",
            "/api/v1/commands/status",
            "/access/{tenantId}/{userId}",
            "tenants.events",
            "MapEventStoreDomainEvents()",
            "GET {tenants-url}/api/tenants/acme-demo",
            "GET {tenants-url}/api/tenants/acme-demo/audit",
        ];

        foreach (string requiredReference in requiredReferences) {
            combined.ShouldContain(requiredReference);
        }
    }

    [Fact]
    public void Demo_json_command_examples_are_valid_EventStore_command_requests() {
        Dictionary<string, JsonElement> commands = ExtractCommandExamples();

        JsonElement bootstrap = commands["BootstrapGlobalAdmin"];
        bootstrap.GetProperty("tenant").GetString().ShouldBe("system");
        bootstrap.GetProperty("domain").GetString().ShouldBe("global-administrators");
        bootstrap.GetProperty("aggregateId").GetString().ShouldBe("global-administrators");
        bootstrap.GetProperty("payload").GetProperty("UserId").GetString().ShouldBe("admin-user");
        DeserializePayload<BootstrapGlobalAdmin>(bootstrap).UserId.ShouldBe("admin-user");
        AssertUlidMessageId(bootstrap);

        JsonElement createTenant = commands["CreateTenant"];
        CreateTenant createTenantPayload = DeserializePayload<CreateTenant>(createTenant);
        createTenant.GetProperty("tenant").GetString().ShouldBe("system");
        createTenant.GetProperty("domain").GetString().ShouldBe("tenants");
        createTenant.GetProperty("aggregateId").GetString().ShouldBe("acme-demo");
        createTenant.GetProperty("payload").GetProperty("TenantId").GetString().ShouldBe(createTenant.GetProperty("aggregateId").GetString());
        createTenantPayload.TenantId.ShouldBe("acme-demo");
        AssertUlidMessageId(createTenant);

        JsonElement addUser = commands["AddUserToTenant"];
        AddUserToTenant addUserPayload = DeserializePayload<AddUserToTenant>(addUser);
        addUser.GetProperty("tenant").GetString().ShouldBe("system");
        addUser.GetProperty("domain").GetString().ShouldBe("tenants");
        addUser.GetProperty("aggregateId").GetString().ShouldBe("acme-demo");
        addUser.GetProperty("payload").GetProperty("TenantId").GetString().ShouldBe(addUser.GetProperty("aggregateId").GetString());
        addUser.GetProperty("payload").GetProperty("Role").GetString().ShouldBe("TenantContributor");
        addUserPayload.Role.ShouldBe(TenantRole.TenantContributor);
        AssertUlidMessageId(addUser);

        JsonElement removeUser = commands["RemoveUserFromTenant"];
        RemoveUserFromTenant removeUserPayload = DeserializePayload<RemoveUserFromTenant>(removeUser);
        removeUser.GetProperty("tenant").GetString().ShouldBe("system");
        removeUser.GetProperty("domain").GetString().ShouldBe("tenants");
        removeUser.GetProperty("aggregateId").GetString().ShouldBe("acme-demo");
        removeUser.GetProperty("payload").GetProperty("TenantId").GetString().ShouldBe(removeUser.GetProperty("aggregateId").GetString());
        removeUserPayload.UserId.ShouldBe("jane-doe");
        AssertUlidMessageId(removeUser);
    }

    [Fact]
    public void Demo_scripts_correct_command_drift_and_poll_projection_transitions() {
        string bash = ReadScript("demo.sh");
        string powershell = ReadScript("demo.ps1");
        string combined = string.Join('\n', bash, powershell);

        combined.ShouldContain("global-administrators");
        combined.ShouldContain("BootstrapGlobalAdmin");
        combined.ShouldContain("New-Ulid");
        combined.ShouldContain("generate_ulid");
        combined.ShouldContain("TenantContributor");
        combined.ShouldContain("/access/");
        combined.ShouldContain("granted");
        combined.ShouldContain("denied");
        combined.ShouldContain("TimeoutSeconds");
        combined.ShouldContain("TIMEOUT_SECONDS");
        combined.ShouldContain("Commands accepted");
        combined.ShouldContain("Command status");
        combined.ShouldContain("Query evidence");

        combined.ShouldNotContain("\"messageId\":\"demo-");
        combined.ShouldNotContain("messageId = \"demo-");
        combined.ShouldNotContain("\"Role\":1");
        combined.ShouldNotContain("domain\":\"tenants\",\"aggregateId\":\"global-administrators\"");
        combined.ShouldNotContain("domain = \"tenants\"\n    aggregateId = \"global-administrators\"");
    }

    [Fact]
    public void Demo_assets_are_support_safe_and_distinguish_Keycloak_from_HMAC_fallback() {
        string combined = string.Join('\n', ReadDemo(), ReadScript("demo.sh"), ReadScript("demo.ps1"));

        combined.ShouldContain("Keycloak");
        combined.ShouldContain("hexalith-eventstore");
        combined.ShouldContain("EnableKeycloak=false");
        combined.ShouldContain("--hmac-dev-token");
        combined.ShouldContain("-HmacDevToken");
        combined.ShouldContain("TOKEN");

        JwtLikeTokenRegex.IsMatch(combined).ShouldBeFalse("Demo assets must not contain JWT-like raw token values.");
        combined.ShouldNotContain("client_secret");
        combined.ShouldNotContain("log full event payloads");
    }

    [Fact]
    public void Hmac_fallback_tokens_target_the_EventStore_command_gateway() {
        string bash = ReadScript("demo.sh");
        string powershell = ReadScript("demo.ps1");
        string combined = string.Join('\n', bash, powershell);

        combined.ShouldContain("aud\":\"hexalith-eventstore\"");
        combined.ShouldContain("aud = \"hexalith-eventstore\"");
        combined.ShouldContain("DevOnlySigningKey-AtLeast32Chars!");
        combined.ShouldNotContain("aud\":\"hexalith-tenants\"");
        combined.ShouldNotContain("aud = \"hexalith-tenants\"");
        combined.ShouldNotContain("this-is-a-development-signing-key-minimum-32-chars");
    }

    [Fact]
    public void Demo_explains_eventual_consistency_without_overclaiming_synchronous_or_plural_live_services() {
        string demo = ReadDemo();

        demo.ShouldContain("eventually consistent");
        demo.ShouldContain("EventStore is the durable source of truth");
        demo.ShouldContain("subscribers catch up asynchronously");
        demo.ShouldContain("local projection state");
        demo.ShouldContain("no custom polling or manual synchronization job is used");
        demo.ShouldContain("planned synchronous authorization plugin");
        demo.ShouldContain("current runnable AppHost includes one sample subscriber resource named `sample`");
        demo.ShouldContain("Additional services subscribe the same way");

        demo.ShouldNotContain("synchronous revocation");
        demo.ShouldNotContain("simultaneously");
        demo.ShouldNotContain("every subscribing service revokes access automatically");
        demo.ShouldNotContain("three live services");
    }

    [Fact]
    public void Demo_links_to_related_guides_without_duplicating_their_scope() {
        string demo = ReadDemo();

        demo.ShouldContain("[Quickstart](quickstart.md)");
        demo.ShouldContain("[Event Contract Reference](event-contract-reference.md)");
        demo.ShouldContain("[Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)");
        demo.ShouldContain("[Idempotent Event Processing](idempotent-event-processing.md)");
        demo.ShouldContain("[Cross-Aggregate Timing](cross-aggregate-timing.md)");
    }

    [Fact]
    public void Demo_has_Aspire_E2E_coverage_for_reactive_access_transition() {
        string aspireTests = File.ReadAllText(RepositoryPath("tests", "Hexalith.Tenants.IntegrationTests", "AspireTopologyTests.cs"));
        string appHost = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants.AppHost", "Program.cs"));

        aspireTests.ShouldContain("Aha_moment_demo_revokes_sample_access_from_tenant_events");
        aspireTests.ShouldContain("BootstrapGlobalAdmin");
        aspireTests.ShouldContain("CreateTenant");
        aspireTests.ShouldContain("AddUserToTenant");
        aspireTests.ShouldContain("RemoveUserFromTenant");
        aspireTests.ShouldContain("\"/api/v1/commands\"");
        aspireTests.ShouldContain("\"/api/v1/commands/status/{correlationId}\"");
        aspireTests.ShouldContain("\"granted\"");
        aspireTests.ShouldContain("\"denied\"");
        aspireTests.ShouldContain("TenantContributor");
        aspireTests.ShouldContain("GlobalAdministrator");
        appHost.ShouldContain("tenants.events");
        appHost.ShouldContain("AddProject<HexalithTenantsSample>(\"sample\")");
    }

    private static Dictionary<string, JsonElement> ExtractCommandExamples() {
        Dictionary<string, JsonElement> commands = new(StringComparer.Ordinal);
        foreach (Match match in JsonFenceRegex.Matches(ReadDemo())) {
            using JsonDocument document = JsonDocument.Parse(match.Groups["json"].Value);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("commandType", out JsonElement commandType)) {
                continue;
            }

            commands[commandType.GetString() ?? string.Empty] = root.Clone();
        }

        commands.Keys.ShouldBe(["BootstrapGlobalAdmin", "CreateTenant", "AddUserToTenant", "RemoveUserFromTenant"], ignoreOrder: true);
        return commands;
    }

    private static void AssertUlidMessageId(JsonElement command) {
        string? messageId = command.GetProperty("messageId").GetString();

        messageId.ShouldNotBeNullOrWhiteSpace();
        UlidRegex.IsMatch(messageId).ShouldBeTrue($"{messageId} should be a concrete ULID-shaped idempotency key.");
    }

    private static T DeserializePayload<T>(JsonElement command)
        where T : class {
        T? payload = command.GetProperty("payload").Deserialize<T>();
        return payload.ShouldNotBeNull();
    }

    private static string ReadDemo()
        => File.ReadAllText(RepositoryPath("docs", "demo.md"));

    private static string ReadScript(string fileName)
        => File.ReadAllText(RepositoryPath("scripts", fileName));

    private static string RepositoryPath(params string[] segments)
        => Path.GetFullPath(Path.Combine(
            new[] {
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
            }.Concat(segments).ToArray()));
}
