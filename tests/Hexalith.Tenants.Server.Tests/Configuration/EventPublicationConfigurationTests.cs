using Microsoft.Extensions.Configuration;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.Tenants.Server.Tests.Configuration;

public class EventPublicationConfigurationTests {
    [Fact]
    public void Appsettings_ConfiguresSharedTenantEventTopicForGlobalAdministratorsDomain() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        configuration["EventStore:Publisher:PubSubName"].ShouldBe("pubsub");
        configuration["EventStore:Publisher:TopicOverrides:global-administrators"].ShouldBe("tenants.events");
    }

    [Fact]
    public void EventStoreHostAppsettings_ConfiguresSharedTenantEventTopicForGlobalAdministratorsDomain() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(RepositoryPath("Hexalith.EventStore", "src", "Hexalith.EventStore", "appsettings.json"), optional: false)
            .Build();

        configuration["EventStore:Publisher:PubSubName"].ShouldBe("pubsub");
        configuration["EventStore:Publisher:TopicOverrides:global-administrators"].ShouldBe("tenants.events");
    }

    [Fact]
    public void AppHostPubSub_ConfiguresTenantDeadLetterTopicAndCurrentAppIds() {
        string path = RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "pubsub.yaml");

        using var reader = new StreamReader(path);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var spec = (YamlMappingNode)root.Children[new YamlScalarNode("spec")];
        var metadata = (YamlSequenceNode)spec.Children[new YamlScalarNode("metadata")];

        string? deadLetterTopic = metadata
            .OfType<YamlMappingNode>()
            .Where(node => node.Children.TryGetValue(new YamlScalarNode("name"), out YamlNode? name)
                && ((YamlScalarNode)name).Value == "deadLetterTopic")
            .Select(node => ((YamlScalarNode)node.Children[new YamlScalarNode("value")]).Value)
            .SingleOrDefault();

        deadLetterTopic.ShouldBe("deadletter.tenants.events");

        var scopes = (YamlSequenceNode)root.Children[new YamlScalarNode("scopes")];
        string[] scopeValues = scopes
            .OfType<YamlScalarNode>()
            .Select(node => node.Value)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        scopeValues.ShouldBe(["eventstore", "sample"]);
    }

    [Fact]
    public void AppHostStateStore_ConfiguresCurrentAppIds() {
        string path = RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "statestore.yaml");

        using var reader = new StreamReader(path);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var scopes = (YamlSequenceNode)root.Children[new YamlScalarNode("scopes")];

        string[] scopeValues = scopes
            .OfType<YamlScalarNode>()
            .Select(node => node.Value)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        scopeValues.ShouldBe(["eventstore", "eventstore-admin", "tenants"]);
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
