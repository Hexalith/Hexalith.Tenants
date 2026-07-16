using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class QuickstartDocumentationTests {
    private static readonly Regex JsonFenceRegex = new(
        "```json\\s*(?<json>.*?)\\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex UlidRegex = new(
        "^[0-9A-HJKMNP-TV-Z]{26}$",
        RegexOptions.Compiled);

    [Fact]
    public void Quickstart_prerequisite_validation_covers_blocking_local_setup_before_first_command() {
        string quickstart = ReadQuickstart();

        quickstart.ShouldContain("dotnet --version");
        quickstart.ShouldContain("10.0.302");
        quickstart.ShouldContain("global.json");
        quickstart.ShouldContain("dapr --version");
        quickstart.ShouldContain("dapr init");
        quickstart.ShouldContain("dapr init --slim");
        quickstart.ShouldContain("Docker");
        quickstart.ShouldContain("docker info");
        quickstart.ShouldContain("git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories");
        quickstart.ShouldContain("Do not run `git submodule update --init --recursive`");
        quickstart.ShouldContain("dotnet build Hexalith.Tenants.slnx --configuration Release");
        quickstart.ShouldContain("dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj");
        quickstart.ShouldContain("EventStore command gateway");
        quickstart.ShouldContain("Keycloak");
        quickstart.ShouldContain("eventstore:tenant=system");
        quickstart.ShouldContain("Validate Before the First Command");
    }

    [Fact]
    public void Quickstart_referenced_local_paths_exist() {
        string repoRoot = RepositoryPath();
        string quickstart = ReadQuickstart();

        string[] requiredPaths =
        [
            "Hexalith.Tenants.slnx",
            "global.json",
            "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj",
            "src/Hexalith.Tenants.AppHost/Program.cs",
            "src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json",
            "src/Hexalith.Tenants/appsettings.Development.json",
            "references/Hexalith.EventStore/src/Hexalith.EventStore/appsettings.Development.json",
            "docs/quickstart.md",
            "docs/production-auth-claim-contract.md",
            "docs/production-auth-readiness.md",
            "deploy/dapr/README.md",
            "samples/Hexalith.Tenants.Sample/Program.cs",
        ];

        foreach (string path in requiredPaths) {
            File.Exists(RepositoryPath(path.Split('/'))).ShouldBeTrue($"{path} must exist because the quickstart references it.");
        }

        quickstart.ShouldContain("Hexalith.Tenants.Contracts");
        quickstart.ShouldContain("Hexalith.Tenants.Client");
        File.Exists(Path.Combine(repoRoot, "src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(repoRoot, "src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj")).ShouldBeTrue();
    }

    [Fact]
    public void Quickstart_command_gateway_and_status_routes_match_EventStore_source() {
        string quickstart = ReadQuickstart();
        string commandsController = File.ReadAllText(RepositoryPath("references", "Hexalith.EventStore", "src", "Hexalith.EventStore", "Controllers", "CommandsController.cs"));
        string commandStatusController = File.ReadAllText(RepositoryPath("references", "Hexalith.EventStore", "src", "Hexalith.EventStore", "Controllers", "CommandStatusController.cs"));

        commandsController.ShouldContain("[Route(\"api/v1/commands\")]");
        commandStatusController.ShouldContain("[Route(\"api/v1/commands/status\")]");
        quickstart.ShouldContain("POST /api/v1/commands");
        quickstart.ShouldContain("GET /api/v1/commands/status/{correlationId}");
        quickstart.ShouldContain("curl -fsS \"{eventstore-url}/swagger/v1/swagger.json\" | rg '\"/api/v1/commands\"'");
        quickstart.ShouldNotContain("POST /api/commands");
        quickstart.ShouldNotContain("GET /api/commands/status");
    }

    [Fact]
    public void Quickstart_hmac_fallback_targets_EventStore_development_auth_settings() {
        string quickstart = ReadQuickstart();
        string eventStoreDevelopmentSettings = File.ReadAllText(RepositoryPath("references", "Hexalith.EventStore", "src", "Hexalith.EventStore", "appsettings.Development.json"));
        string tenantsDevelopmentSettings = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants", "appsettings.Development.json"));

        eventStoreDevelopmentSettings.ShouldContain("\"Audience\": \"hexalith-eventstore\"");
        eventStoreDevelopmentSettings.ShouldContain("\"SigningKey\": \"DevOnlySigningKey-AtLeast32Chars!\"");
        tenantsDevelopmentSettings.ShouldContain("\"Audience\": \"hexalith-tenants\"");

        quickstart.ShouldContain("references/Hexalith.EventStore/src/Hexalith.EventStore/appsettings.Development.json");
        quickstart.ShouldContain("aud=\"hexalith-eventstore\"");
        quickstart.ShouldContain("aud\":\"hexalith-eventstore\"");
        quickstart.ShouldContain("DevOnlySigningKey-AtLeast32Chars!");
        quickstart.ShouldContain("audience `hexalith-eventstore`");
        quickstart.ShouldNotContain("aud=\"hexalith-tenants\"");
        quickstart.ShouldNotContain("aud\":\"hexalith-tenants\"");
        quickstart.ShouldNotContain("this-is-a-development-signing-key-minimum-32-chars");
    }

    [Fact]
    public void Quickstart_first_command_examples_are_valid_EventStore_command_requests() {
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
        createTenant.GetProperty("aggregateId").GetString().ShouldBe("my-first-tenant");
        createTenant.GetProperty("payload").GetProperty("TenantId").GetString().ShouldBe(createTenant.GetProperty("aggregateId").GetString());
        createTenant.GetProperty("payload").GetProperty("Name").GetString().ShouldBe("My First Tenant");
        createTenantPayload.TenantId.ShouldBe(createTenant.GetProperty("aggregateId").GetString());
        createTenantPayload.Name.ShouldBe("My First Tenant");
        AssertUlidMessageId(createTenant);

        JsonElement addUser = commands["AddUserToTenant"];
        AddUserToTenant addUserPayload = DeserializePayload<AddUserToTenant>(addUser);
        addUser.GetProperty("tenant").GetString().ShouldBe("system");
        addUser.GetProperty("domain").GetString().ShouldBe("tenants");
        addUser.GetProperty("aggregateId").GetString().ShouldBe("my-first-tenant");
        addUser.GetProperty("payload").GetProperty("TenantId").GetString().ShouldBe(addUser.GetProperty("aggregateId").GetString());
        addUser.GetProperty("payload").GetProperty("Role").GetString().ShouldBe("TenantContributor");
        addUserPayload.TenantId.ShouldBe(addUser.GetProperty("aggregateId").GetString());
        addUserPayload.Role.ShouldBe(TenantRole.TenantContributor);
        AssertUlidMessageId(addUser);
    }

    [Fact]
    public void Quickstart_documents_success_rejection_and_corrective_actions_without_raw_logs_as_primary_signal() {
        string quickstart = ReadQuickstart();

        quickstart.ShouldContain("202 Accepted");
        quickstart.ShouldContain("Location");
        quickstart.ShouldContain("status polling endpoint");
        quickstart.ShouldContain("status: \"Completed\"");
        quickstart.ShouldContain("statusCode\": 4");
        quickstart.ShouldContain("eventCount: 1");
        quickstart.ShouldContain("GET /api/tenants/{tenantId}");
        quickstart.ShouldContain("status: \"Rejected\"");
        quickstart.ShouldContain("rejectionEventType");
        quickstart.ShouldContain("failureReason");
        quickstart.ShouldContain("GlobalAdminAlreadyBootstrappedRejection");
        quickstart.ShouldContain("TenantAlreadyExistsRejection");
        quickstart.ShouldContain("Use a different `aggregateId` and matching `payload.TenantId`");
    }

    private static Dictionary<string, JsonElement> ExtractCommandExamples() {
        Dictionary<string, JsonElement> commands = new(StringComparer.Ordinal);
        foreach (Match match in JsonFenceRegex.Matches(ReadQuickstart())) {
            using JsonDocument document = JsonDocument.Parse(match.Groups["json"].Value);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("commandType", out JsonElement commandType)) {
                continue;
            }

            string key = commandType.GetString() ?? string.Empty;
            commands[key] = root.Clone();
        }

        commands.Keys.ShouldContain("BootstrapGlobalAdmin");
        commands.Keys.ShouldContain("CreateTenant");
        commands.Keys.ShouldContain("AddUserToTenant");
        return commands;
    }

    private static void AssertUlidMessageId(JsonElement command) {
        string? messageId = command.GetProperty("messageId").GetString();

        messageId.ShouldNotBeNullOrWhiteSpace();
        messageId.ShouldNotContain("<");
        UlidRegex.IsMatch(messageId).ShouldBeTrue($"{messageId} should be a concrete ULID-shaped idempotency key.");
    }

    private static T DeserializePayload<T>(JsonElement command)
        where T : class {
        T? payload = command.GetProperty("payload").Deserialize<T>();
        return payload.ShouldNotBeNull();
    }

    private static string ReadQuickstart()
        => File.ReadAllText(RepositoryPath("docs", "quickstart.md"));

    private static string RepositoryPath(params string[] segments) {
        string repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string direct = Path.GetFullPath(Path.Combine(
            new[] { repoRoot }.Concat(segments).ToArray()));
        if (File.Exists(direct) || Directory.Exists(direct)) {
            return direct;
        }

        if (segments is ["references", "Hexalith.EventStore", ..]) {
            string parentEventStore = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, "..", ".." }.Concat(segments.Skip(2)).ToArray()));
            if (File.Exists(parentEventStore) || Directory.Exists(parentEventStore)) {
                return parentEventStore;
            }
        }

        // A dependent module (e.g. Hexalith.EventStore) is a nested submodule of this repository
        // that may be left uninitialized when this repository is itself a submodule of a parent
        // that checks the dependency out as a sibling checkout. Fall back to that sibling.
        if (segments is ["references", not null, ..] && segments[1].StartsWith("Hexalith.", StringComparison.Ordinal)) {
            string siblingReference = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, ".." }.Concat(segments.Skip(1)).ToArray()));
            if (File.Exists(siblingReference) || Directory.Exists(siblingReference)) {
                return siblingReference;
            }
        }

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
