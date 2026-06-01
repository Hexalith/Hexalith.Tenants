using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events.Rejections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class CompensatingCommandsDocumentationTests {
    private static readonly Regex JsonFenceRegex = new(
        "```json\\s*(?<json>.*?)\\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex UlidRegex = new(
        "^[0-9A-HJKMNP-TV-Z]{26}$",
        RegexOptions.Compiled);

    [Fact]
    public void Compensating_guide_references_current_command_role_and_rejection_contracts() {
        string guide = ReadGuide();

        string[] requiredTerms =
        [
            nameof(AddUserToTenant),
            nameof(RemoveUserFromTenant),
            nameof(ChangeUserRole),
            nameof(SetTenantConfiguration),
            nameof(RemoveTenantConfiguration),
            nameof(DisableTenant),
            nameof(EnableTenant),
            $"{nameof(TenantRole)}.{nameof(TenantRole.TenantOwner)}",
            $"{nameof(TenantRole)}.{nameof(TenantRole.TenantContributor)}",
            $"{nameof(TenantRole)}.{nameof(TenantRole.TenantReader)}",
            nameof(TenantNotFoundRejection),
            nameof(TenantDisabledRejection),
            nameof(UserAlreadyInTenantRejection),
            nameof(UserNotInTenantRejection),
            nameof(RoleEscalationRejection),
            nameof(ConfigurationLimitExceededRejection),
            nameof(ConfigurationKeyNotFoundRejection),
            nameof(TenantLifecycleStateAlreadySetRejection),
            nameof(InsufficientPermissionsRejection),
            "NoOp",
        ];

        foreach (string requiredTerm in requiredTerms) {
            guide.ShouldContain(requiredTerm);
        }

        string contractsRoot = RepositoryPath("src", "Hexalith.Tenants.Contracts");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "AddUserToTenant.cs")).ShouldContain("public record AddUserToTenant");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "RemoveUserFromTenant.cs")).ShouldContain("public record RemoveUserFromTenant");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "ChangeUserRole.cs")).ShouldContain("public record ChangeUserRole");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "SetTenantConfiguration.cs")).ShouldContain("public record SetTenantConfiguration");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "RemoveTenantConfiguration.cs")).ShouldContain("public record RemoveTenantConfiguration");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "DisableTenant.cs")).ShouldContain("public record DisableTenant");
        File.ReadAllText(Path.Combine(contractsRoot, "Commands", "EnableTenant.cs")).ShouldContain("public record EnableTenant");
        Enum.GetNames<TenantRole>().ShouldBe(["Unknown", "TenantOwner", "TenantContributor", "TenantReader"]);
    }

    [Fact]
    public void Compensating_guide_references_source_backed_files_that_anchor_the_claims() {
        string guide = ReadGuide();

        string[] sourcePaths =
        [
            "src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs",
            "src/Hexalith.Tenants.Server/Aggregates/TenantState.cs",
            "src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs",
            "src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs",
            "docs/event-contract-reference.md",
        ];

        foreach (string sourcePath in sourcePaths) {
            File.Exists(RepositoryPath(sourcePath.Split('/'))).ShouldBeTrue($"{sourcePath} must exist because the guide cites it.");
            guide.ShouldContain(sourcePath);
        }
    }

    [Fact]
    public void Compensating_guide_command_gateway_and_status_routes_match_EventStore_source() {
        string guide = ReadGuide();
        string commandsController = File.ReadAllText(RepositoryPath("Hexalith.EventStore", "src", "Hexalith.EventStore", "Controllers", "CommandsController.cs"));
        string commandStatusController = File.ReadAllText(RepositoryPath("Hexalith.EventStore", "src", "Hexalith.EventStore", "Controllers", "CommandStatusController.cs"));

        commandsController.ShouldContain("[Route(\"api/v1/commands\")]");
        commandStatusController.ShouldContain("[Route(\"api/v1/commands/status\")]");
        guide.ShouldContain("POST /api/v1/commands");
        guide.ShouldContain("GET /api/v1/commands/status/{correlationId}");
        guide.ShouldNotContain("POST /api/commands");
        guide.ShouldNotContain("GET /api/commands/status");
    }

    [Fact]
    public void Compensating_guide_covers_each_scenario_with_safe_commands_and_expected_errors() {
        string mistakenRemoval = ExtractSection("### Mistaken User Removal", "### Wrong Role Assignment");
        mistakenRemoval.ShouldContain("Safe command path:");
        mistakenRemoval.ShouldContain(nameof(AddUserToTenant));
        mistakenRemoval.ShouldContain(nameof(RemoveUserFromTenant));
        mistakenRemoval.ShouldContain($"{nameof(TenantRole)}.{nameof(TenantRole.TenantOwner)}");
        mistakenRemoval.ShouldContain($"{nameof(TenantRole)}.{nameof(TenantRole.TenantContributor)}");
        mistakenRemoval.ShouldContain($"{nameof(TenantRole)}.{nameof(TenantRole.TenantReader)}");
        mistakenRemoval.ShouldContain("Expected rejection cases:");
        mistakenRemoval.ShouldContain(nameof(TenantNotFoundRejection));
        mistakenRemoval.ShouldContain(nameof(TenantDisabledRejection));
        mistakenRemoval.ShouldContain(nameof(UserAlreadyInTenantRejection));
        mistakenRemoval.ShouldContain(nameof(UserNotInTenantRejection));
        mistakenRemoval.ShouldContain(nameof(RoleEscalationRejection));
        mistakenRemoval.ShouldContain(nameof(InsufficientPermissionsRejection));

        string wrongRole = ExtractSection("### Wrong Role Assignment", "### Configuration Mistake");
        wrongRole.ShouldContain("Safe command path:");
        wrongRole.ShouldContain(nameof(ChangeUserRole));
        wrongRole.ShouldContain("NewRole");
        wrongRole.ShouldContain("Expected rejection or no-op cases:");
        wrongRole.ShouldContain(nameof(TenantNotFoundRejection));
        wrongRole.ShouldContain(nameof(TenantDisabledRejection));
        wrongRole.ShouldContain(nameof(UserNotInTenantRejection));
        wrongRole.ShouldContain(nameof(RoleEscalationRejection));
        wrongRole.ShouldContain(nameof(InsufficientPermissionsRejection));
        wrongRole.ShouldContain("NoOp");
        wrongRole.ShouldContain("same-role");

        string configuration = ExtractSection("### Configuration Mistake", "### Tenant Lifecycle Correction");
        configuration.ShouldContain("Safe command path:");
        configuration.ShouldContain(nameof(SetTenantConfiguration));
        configuration.ShouldContain(nameof(RemoveTenantConfiguration));
        configuration.ShouldContain("Expected rejection or no-op cases:");
        configuration.ShouldContain(nameof(TenantNotFoundRejection));
        configuration.ShouldContain(nameof(TenantDisabledRejection));
        configuration.ShouldContain(nameof(ConfigurationLimitExceededRejection));
        configuration.ShouldContain(nameof(ConfigurationKeyNotFoundRejection));
        configuration.ShouldContain(nameof(InsufficientPermissionsRejection));
        configuration.ShouldContain("NoOp");
        configuration.ShouldContain("same key and same value");

        string lifecycle = ExtractSection("### Tenant Lifecycle Correction", "## Audit and Verification");
        lifecycle.ShouldContain("Safe command path:");
        lifecycle.ShouldContain(nameof(EnableTenant));
        lifecycle.ShouldContain(nameof(DisableTenant));
        lifecycle.ShouldContain("trusted global administrator");
        lifecycle.ShouldContain("Expected rejection cases:");
        lifecycle.ShouldContain(nameof(TenantNotFoundRejection));
        lifecycle.ShouldContain(nameof(TenantLifecycleStateAlreadySetRejection));
        lifecycle.ShouldContain(nameof(InsufficientPermissionsRejection));
        lifecycle.ShouldContain(nameof(TenantDisabledRejection));
    }

    [Fact]
    public void Compensating_guide_command_examples_are_valid_EventStore_command_requests() {
        Dictionary<string, JsonElement> commands = ExtractCommandExamples();

        string[] expectedCommands =
        [
            nameof(RemoveUserFromTenant),
            nameof(AddUserToTenant),
            nameof(ChangeUserRole),
            nameof(SetTenantConfiguration),
            nameof(RemoveTenantConfiguration),
            nameof(EnableTenant),
        ];

        foreach (string expectedCommand in expectedCommands) {
            commands.Keys.ShouldContain(expectedCommand);
        }

        foreach (JsonElement command in commands.Values) {
            command.GetProperty("tenant").GetString().ShouldBe("system");
            command.GetProperty("domain").GetString().ShouldBe("tenants");
            command.GetProperty("aggregateId").GetString().ShouldBe(command.GetProperty("payload").GetProperty("TenantId").GetString());
            AssertUlidMessageId(command);
        }

        DeserializePayload<RemoveUserFromTenant>(commands[nameof(RemoveUserFromTenant)]).TenantId.ShouldBe("acme-corp");
        DeserializePayload<AddUserToTenant>(commands[nameof(AddUserToTenant)]).Role.ShouldBe(TenantRole.TenantContributor);
        DeserializePayload<ChangeUserRole>(commands[nameof(ChangeUserRole)]).NewRole.ShouldBe(TenantRole.TenantReader);
        DeserializePayload<SetTenantConfiguration>(commands[nameof(SetTenantConfiguration)]).Value.ShouldBe("read-only");
        DeserializePayload<RemoveTenantConfiguration>(commands[nameof(RemoveTenantConfiguration)]).Key.ShouldBe("sample.access.mode");
        DeserializePayload<EnableTenant>(commands[nameof(EnableTenant)]).TenantId.ShouldBe("acme-corp");
    }

    [Fact]
    public void Compensating_guide_role_and_status_values_deserialize_by_enum_name() {
        Dictionary<string, JsonElement> commands = ExtractCommandExamples();

        commands[nameof(AddUserToTenant)].GetProperty("payload").GetProperty("Role").GetString().ShouldBe("TenantContributor");
        DeserializePayload<AddUserToTenant>(commands[nameof(AddUserToTenant)]).Role.ShouldBe(TenantRole.TenantContributor);

        commands[nameof(ChangeUserRole)].GetProperty("payload").GetProperty("NewRole").GetString().ShouldBe("TenantReader");
        DeserializePayload<ChangeUserRole>(commands[nameof(ChangeUserRole)]).NewRole.ShouldBe(TenantRole.TenantReader);

        TenantLifecycleStateAlreadySetRejection rejection = ExtractJsonObject("RequestedStatus")
            .Deserialize<TenantLifecycleStateAlreadySetRejection>()
            .ShouldNotBeNull();

        rejection.CurrentStatus.ShouldBe(TenantStatus.Disabled);
        rejection.RequestedStatus.ShouldBe(TenantStatus.Disabled);
        rejection.CommandName.ShouldBe(nameof(DisableTenant));
    }

    [Fact]
    public void Compensating_guide_rejects_hidden_undo_and_distinguishes_audit_from_rejections() {
        string guide = ReadGuide();

        guide.ShouldContain("not hidden undo");
        guide.ShouldContain("event deletion");
        guide.ShouldContain("event mutation");
        guide.ShouldContain("projection editing");
        guide.ShouldContain("direct state-store repair");
        guide.ShouldContain("original event remains in history");
        guide.ShouldContain("correction appends a new event");
        guide.ShouldContain("EventStore command status proves the submitted command outcome");
        guide.ShouldContain("tenant audit query rows prove successful corrective events");
        guide.ShouldContain("Rejected compensating commands do not produce successful corrective audit events");
    }

    [Fact]
    public void Compensating_guide_avoids_unsafe_sample_content() {
        string guide = ReadGuide();

        guide.ShouldNotContain("Authorization: Bearer ");
        guide.ShouldNotContain("eyJ");
        guide.ShouldNotContain("client_secret");
        guide.ShouldNotContain("password=");
        guide.ShouldNotContain("System.Exception");
        guide.ShouldNotContain(" at Hexalith.");
        guide.ShouldNotContain("@example.");
    }

    [Fact]
    public void Related_documentation_navigation_links_to_compensating_guide() {
        Dictionary<string, string> documents = new(StringComparer.Ordinal) {
            ["README.md"] = File.ReadAllText(RepositoryPath("README.md")),
            ["docs/event-contract-reference.md"] = File.ReadAllText(RepositoryPath("docs", "event-contract-reference.md")),
            ["docs/cross-aggregate-timing.md"] = File.ReadAllText(RepositoryPath("docs", "cross-aggregate-timing.md")),
            ["docs/demo.md"] = File.ReadAllText(RepositoryPath("docs", "demo.md")),
        };

        foreach (KeyValuePair<string, string> document in documents) {
            document.Value.Contains("compensating-commands.md", StringComparison.Ordinal)
                .ShouldBeTrue($"{document.Key} should link to the compensating command guide.");
        }
    }

    private static Dictionary<string, JsonElement> ExtractCommandExamples() {
        Dictionary<string, JsonElement> commands = new(StringComparer.Ordinal);
        foreach (Match match in JsonFenceRegex.Matches(ReadGuide())) {
            using JsonDocument document = JsonDocument.Parse(match.Groups["json"].Value);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("commandType", out JsonElement commandType)) {
                continue;
            }

            commands[commandType.GetString() ?? string.Empty] = root.Clone();
        }

        commands.Count.ShouldBeGreaterThan(0);
        return commands;
    }

    private static JsonElement ExtractJsonObject(string propertyName) {
        foreach (Match match in JsonFenceRegex.Matches(ReadGuide())) {
            using JsonDocument document = JsonDocument.Parse(match.Groups["json"].Value);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out _)) {
                return root.Clone();
            }
        }

        throw new InvalidOperationException($"No JSON example contains property {propertyName}.");
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

    private static string ReadGuide()
        => File.ReadAllText(RepositoryPath("docs", "compensating-commands.md"));

    private static string ExtractSection(string heading, string nextHeading) {
        string guide = ReadGuide();
        int start = guide.IndexOf(heading, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"{heading} must exist in the guide.");

        int end = guide.IndexOf(nextHeading, start + heading.Length, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start, $"{nextHeading} must follow {heading} in the guide.");

        return guide[start..end];
    }

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
