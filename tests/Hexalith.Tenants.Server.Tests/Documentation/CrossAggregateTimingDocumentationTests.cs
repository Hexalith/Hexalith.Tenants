using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class CrossAggregateTimingDocumentationTests {
    [Fact]
    public void Timing_guide_documents_command_lifecycle_and_subscriber_propagation_window() {
        string guide = ReadGuide();

        string[] requiredTerms =
        [
            "sequenceDiagram",
            "POST /api/v1/commands",
            "GET /api/v1/commands/status/{correlationId}",
            "EventsStored",
            "EventsPublished",
            "Completed",
            "PublishFailed",
            "tenants.events",
            "deadletter.tenants.events",
            "MapEventStoreDomainEvents()",
            "EventStoreDomainEventProcessor",
            "TenantProjectionEventHandler",
            "ITenantProjectionStore",
            "/access/{tenantId}/{userId}",
        ];

        foreach (string requiredTerm in requiredTerms) {
            guide.ShouldContain(requiredTerm);
        }

        guide.ShouldContain("EventStore command gateway");
        guide.ShouldContain("MediatR/SubmitCommandHandler");
        guide.ShouldContain("AggregateActor");
        guide.ShouldContain("Tenants domain processor");
        guide.ShouldContain("EventStore state store");
        guide.ShouldContain("DAPR pub/sub");
        guide.ShouldContain("Sample/consumer endpoint");
        guide.ShouldContain("authoritative persistence boundary");
        guide.ShouldContain("eventual-consistency window");
        guide.ShouldContain("command status polling");
        guide.ShouldContain("subscriber redelivery");
        guide.ShouldContain("republish");
    }

    [Fact]
    public void Timing_guide_references_source_backed_files_that_anchor_the_claims() {
        string guide = ReadGuide();
        string repoRoot = RepositoryPath();

        string[] sourcePaths =
        [
            "Hexalith.EventStore/docs/concepts/command-lifecycle.md",
            "Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs",
            "src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs",
            "src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs",
            "samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs",
            "src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml",
            "src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml",
            "deploy/dapr/pubsub.yaml",
            "deploy/dapr/resiliency.yaml",
        ];

        foreach (string sourcePath in sourcePaths) {
            File.Exists(Path.Combine(repoRoot, sourcePath)).ShouldBeTrue($"{sourcePath} must exist because the guide cites it.");
            guide.ShouldContain(sourcePath);
        }
    }

    [Fact]
    public void Timing_guide_matches_current_EventStore_command_status_contract() {
        string guide = ReadGuide();
        string controller = File.ReadAllText(RepositoryPath(
            "Hexalith.EventStore",
            "src",
            "Hexalith.EventStore",
            "Controllers",
            "CommandStatusController.cs"));

        string[] statusNames = Enum.GetNames<CommandStatus>();
        statusNames.ShouldBe(
            [
                "Received",
                "Processing",
                "EventsStored",
                "EventsPublished",
                "Completed",
                "Rejected",
                "PublishFailed",
                "TimedOut",
            ]);

        foreach (string statusName in statusNames) {
            guide.ShouldContain($"`{statusName}`");
            controller.ShouldContain($"**{statusName}**");
        }

        string[] terminalStatuses = Enum
            .GetValues<CommandStatus>()
            .Where(status => status.IsTerminal())
            .Select(static status => status.ToString())
            .ToArray();

        terminalStatuses.ShouldBe(["Completed", "Rejected", "PublishFailed", "TimedOut"]);
        guide.ShouldContain("terminal states");
        guide.ShouldContain("`Completed` means EventStore persisted and published to pub/sub");
        guide.ShouldContain("it does not mean every subscriber projection has updated");
    }

    [Fact]
    public void Timing_guide_matches_current_DAPR_pubsub_component_contracts() {
        string guide = ReadGuide();
        YamlMappingNode localPubSub = LoadYaml(RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "pubsub.yaml"));
        YamlMappingNode localResiliency = LoadYaml(RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "resiliency.yaml"));
        YamlMappingNode productionPubSub = LoadYaml(RepositoryPath("deploy", "dapr", "pubsub.yaml"));
        YamlMappingNode productionResiliency = LoadYaml(RepositoryPath("deploy", "dapr", "resiliency.yaml"));

        foreach (YamlMappingNode component in new[] { localPubSub, productionPubSub }) {
            Scalar(component, "metadata", "name").ShouldBe("pubsub");
            MetadataValue(component, "enableDeadLetter").ShouldBe("true");
            MetadataValue(component, "deadLetterTopic").ShouldBe("deadletter.tenants.events");
            Scopes(component).ShouldBe(["eventstore", "sample"], ignoreOrder: true);
        }

        foreach (YamlMappingNode resiliency in new[] { localResiliency, productionResiliency }) {
            Scalar(resiliency, "kind").ShouldBe("Resiliency");
            Scalar(resiliency, "metadata", "name").ShouldBe("resiliency");
            Scalar(resiliency, "spec", "targets", "components", "pubsub", "inbound", "retry").ShouldBe("pubsubRetryInbound");
            Scalar(resiliency, "spec", "targets", "components", "pubsub", "inbound", "timeout").ShouldBe("subscriberTimeout");
            Scalar(resiliency, "spec", "policies", "retries", "pubsubRetryInbound", "policy").ShouldBe("exponential");
            Scalar(resiliency, "spec", "policies", "retries", "pubsubRetryInbound", "maxRetries").ShouldBe("10");
        }

        MetadataValue(productionPubSub, "subscriptionScopes").ShouldBe("sample=tenants.events");

        guide.ShouldContain("`tenants.events`");
        guide.ShouldContain("`deadletter.tenants.events`");
        guide.ShouldContain("`resiliency.yaml`");
        guide.ShouldContain("DAPR pub/sub");
        guide.ShouldContain("subscriber failure");
        guide.ShouldContain("redeliver");
    }

    [Fact]
    public void Timing_guide_distinguishes_authoritative_history_from_projection_state() {
        string guide = ReadGuide();

        guide.ShouldContain("source-of-truth write history");
        guide.ShouldContain("Tenants query projections");
        guide.ShouldContain("consumer local projections");
        guide.ShouldContain("can lag independently");
        guide.ShouldContain("DAPR delivery is at-least-once");
        guide.ShouldContain("idempotent handlers");
        guide.ShouldContain("SequenceNumber");
        guide.ShouldContain("aggregate-local only");
        guide.ShouldContain("must not assume cross-service ordering");
        guide.ShouldContain("current MVP consumers must design for eventual consistency and fail closed");
        guide.ShouldContain("planned EventStore authorization plugin");
        guide.ShouldContain("future/optional");
    }

    [Fact]
    public void Timing_guide_avoids_unsafe_waiting_security_and_diagnostic_guidance() {
        string guide = ReadGuide();

        guide.ShouldContain("Do not use `Thread.Sleep`");
        guide.ShouldContain("fixed-delay waits");
        guide.ShouldContain("status polling");
        guide.ShouldContain("bounded retry/backoff");
        guide.ShouldContain("projection metadata");
        guide.ShouldContain("health, log, and trace evidence");
        guide.ShouldContain("local projection rebuild");
        guide.ShouldContain("support-safe diagnostics");

        guide.ShouldNotContain("Authorization: Bearer ");
        guide.ShouldNotContain("eyJ");
        guide.ShouldNotContain("client_secret");
        guide.ShouldNotContain("password=");
        guide.ShouldNotContain("full serialized event payload");
        guide.ShouldNotContain("wait N seconds means correct");
        guide.ShouldNotContain("subscribers enforce access synchronously");
    }

    [Fact]
    public void Related_documentation_navigation_links_to_timing_guide() {
        Dictionary<string, string> documents = new(StringComparer.Ordinal) {
            ["README.md"] = File.ReadAllText(RepositoryPath("README.md")),
            ["docs/demo.md"] = File.ReadAllText(RepositoryPath("docs", "demo.md")),
            ["docs/event-contract-reference.md"] = File.ReadAllText(RepositoryPath("docs", "event-contract-reference.md")),
            ["docs/idempotent-event-processing.md"] = File.ReadAllText(RepositoryPath("docs", "idempotent-event-processing.md")),
            ["docs/sample-consuming-service-walkthrough.md"] = File.ReadAllText(RepositoryPath("docs", "sample-consuming-service-walkthrough.md")),
        };

        foreach (KeyValuePair<string, string> document in documents) {
            document.Value.Contains("cross-aggregate-timing.md", StringComparison.Ordinal)
                .ShouldBeTrue($"{document.Key} should link to the timing guide.");
        }
    }

    private static string ReadGuide()
        => File.ReadAllText(RepositoryPath("docs", "cross-aggregate-timing.md"));

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

    private static YamlMappingNode LoadYaml(string path) {
        using var reader = new StringReader(File.ReadAllText(path));
        var stream = new YamlStream();
        stream.Load(reader);

        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static string? Scalar(YamlMappingNode node, params string[] path) {
        YamlNode current = node;
        foreach (string segment in path) {
            if (current is not YamlMappingNode mapping
                || !mapping.Children.TryGetValue(new YamlScalarNode(segment), out YamlNode? next)) {
                return null;
            }

            current = next;
        }

        return ((YamlScalarNode)current).Value;
    }

    private static string? MetadataValue(YamlMappingNode root, string name) {
        YamlMappingNode spec = (YamlMappingNode)root.Children[new YamlScalarNode("spec")];
        var metadata = (YamlSequenceNode)spec.Children[new YamlScalarNode("metadata")];
        foreach (YamlMappingNode entry in metadata.Children.OfType<YamlMappingNode>()) {
            if (Scalar(entry, "name") == name) {
                return Scalar(entry, "value");
            }
        }

        return null;
    }

    private static string[] Scopes(YamlMappingNode root)
        => ((YamlSequenceNode)root.Children[new YamlScalarNode("scopes")])
            .Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value!)
            .ToArray();
}
