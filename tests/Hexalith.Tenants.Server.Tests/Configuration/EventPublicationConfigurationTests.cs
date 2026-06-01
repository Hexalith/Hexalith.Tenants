using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.Tenants.Server.Tests.Configuration;

public class EventPublicationConfigurationTests {
    private const string EventTopic = "tenants.events";
    private const string DeadLetterTopic = "deadletter.tenants.events";

    [Fact]
    public void Appsettings_ConfiguresSharedTenantEventTopicForGlobalAdministratorsDomain() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        configuration["EventStore:Publisher:PubSubName"].ShouldBe("pubsub");
        configuration["EventStore:Publisher:TopicOverrides:global-administrators"].ShouldBe(EventTopic);
    }

    [Fact]
    public void Appsettings_ConfiguresTenantDomainServicesToUseTenantsProcessMethod() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:AppId"].ShouldBe("tenants");
        configuration["EventStore:DomainServices:Registrations:system|tenants|v1:MethodName"].ShouldBe("process");
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:AppId"].ShouldBe("tenants");
        configuration["EventStore:DomainServices:Registrations:system|global-administrators|v1:MethodName"].ShouldBe("process");
    }

    [Fact]
    public void TenantsHost_ExposesDomainProcessorAndProjectionRoutes() {
        string program = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants", "Program.cs"));

        program.ShouldContain("app.MapPost(\"/process\"");
        program.ShouldContain("DomainServiceRequest request");
        program.ShouldContain("handler.ProcessAsync(request");
        program.ShouldContain("app.MapPost(\"/project\"");
        program.ShouldContain("ProjectionRequest request");
        program.ShouldContain("ProjectionDispatcher");
        ShouldOccurBefore(program, "app.UseMiddleware<CorrelationIdMiddleware>();", "app.MapPost(\"/process\"");
        ShouldOccurBefore(program, "app.UseExceptionHandler();", "app.MapPost(\"/process\"");
        ShouldOccurBefore(program, "app.UseCloudEvents();", "app.MapPost(\"/process\"");
        ShouldOccurBefore(program, "app.UseAuthentication();", "app.MapPost(\"/process\"");
        ShouldOccurBefore(program, "app.UseAuthorization();", "app.MapPost(\"/process\"");
        ShouldOccurBefore(program, "app.UseAuthorization();", "app.MapPost(\"/project\"");
    }

    [Fact]
    public void EventStoreHostAppsettings_ConfiguresSharedTenantEventTopicForGlobalAdministratorsDomain() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(RepositoryPath("Hexalith.EventStore", "src", "Hexalith.EventStore", "appsettings.json"), optional: false)
            .Build();

        configuration["EventStore:Publisher:PubSubName"].ShouldBe("pubsub");
        configuration["EventStore:Publisher:TopicOverrides:global-administrators"].ShouldBe(EventTopic);
    }

    [Fact]
    public void AppHostPubSub_ConfiguresTenantDeadLetterTopicAndCurrentAppIds() {
        string path = RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "pubsub.yaml");

        YamlMappingNode root = LoadYaml(path);
        Scalar(root, "metadata", "name").ShouldBe("pubsub");
        Scalar(root, "spec", "type").ShouldBe("pubsub.redis");
        MetadataValue(root, "enableDeadLetter").ShouldBe("true");
        MetadataValue(root, "deadLetterTopic").ShouldBe(DeadLetterTopic);
        Scopes(root).ShouldBe(["eventstore", "sample"], ignoreOrder: true);
    }

    [Fact]
    public void AppHostStateStore_ConfiguresCurrentAppIds() {
        string path = RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "statestore.yaml");

        YamlMappingNode root = LoadYaml(path);
        Scalar(root, "metadata", "name").ShouldBe("statestore");
        Scalar(root, "spec", "type").ShouldBe("state.redis");
        MetadataValue(root, "actorStateStore").ShouldBe("true");
        Scopes(root).ShouldBe(["eventstore", "eventstore-admin", "tenants"], ignoreOrder: true);
    }

    [Fact]
    public void ProductionDaprTemplates_ExistForRequiredTenantsContracts() {
        string deployDapr = RepositoryPath("deploy", "dapr");

        string[] expectedFiles =
        [
            "statestore.yaml",
            "pubsub.yaml",
            "resiliency.yaml",
            "accesscontrol.tenants.yaml",
            "accesscontrol.eventstore.yaml",
            "accesscontrol.eventstore-admin.yaml",
            "README.md",
        ];

        foreach (string file in expectedFiles) {
            File.Exists(Path.Combine(deployDapr, file)).ShouldBeTrue($"{file} must exist in deploy/dapr.");
        }
    }

    [Fact]
    public void ProductionStateStore_UsesStableNameActorMetadataPlaceholdersAndScopes() {
        YamlMappingNode root = LoadYaml(RepositoryPath("deploy", "dapr", "statestore.yaml"));

        Scalar(root, "metadata", "name").ShouldBe("statestore");
        Scalar(root, "spec", "type").ShouldBe("state.redis");
        Scalar(root, "spec", "version").ShouldBe("v1");
        MetadataValue(root, "actorStateStore").ShouldBe("true");
        MetadataValue(root, "redisHost").ShouldNotBeNullOrWhiteSpace();
        MetadataValue(root, "redisPassword").ShouldNotBeNull().ShouldContain("{secretKeyRef:");
        Scopes(root).ShouldBe(["eventstore", "eventstore-admin", "tenants"], ignoreOrder: true);
    }

    [Fact]
    public void ProductionPubSub_UsesStableNameDeadLetterPlaceholdersAndPublisherSubscriberScopes() {
        YamlMappingNode root = LoadYaml(RepositoryPath("deploy", "dapr", "pubsub.yaml"));

        Scalar(root, "metadata", "name").ShouldBe("pubsub");
        Scalar(root, "spec", "type").ShouldBe("pubsub.redis");
        Scalar(root, "spec", "version").ShouldBe("v1");
        MetadataValue(root, "enableDeadLetter").ShouldBe("true");
        MetadataValue(root, "deadLetterTopic").ShouldBe(DeadLetterTopic);
        MetadataValue(root, "redisHost").ShouldNotBeNullOrWhiteSpace();
        MetadataValue(root, "redisPassword").ShouldNotBeNull().ShouldContain("{secretKeyRef:");
        MetadataValue(root, "publishingScopes").ShouldBe("sample=");
        MetadataValue(root, "subscriptionScopes").ShouldBe("sample=tenants.events");
        Scopes(root).ShouldBe(["eventstore", "sample"], ignoreOrder: true);
    }

    [Fact]
    public void DaprComponentSets_DefineExactlyOneActorStateStore() {
        string[][] componentSets =
        [
            [
                RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "statestore.yaml"),
                RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "pubsub.yaml"),
            ],
            [
                RepositoryPath("deploy", "dapr", "statestore.yaml"),
                RepositoryPath("deploy", "dapr", "pubsub.yaml"),
            ],
        ];

        foreach (string[] componentSet in componentSets) {
            string[] actorStateStores = componentSet
                .Select(LoadYaml)
                .Where(root => MetadataValue(root, "actorStateStore") == "true")
                .Select(root => Scalar(root, "metadata", "name"))
                .OfType<string>()
                .ToArray();

            actorStateStores.ShouldBe(["statestore"]);
        }
    }

    [Fact]
    public void ProductionTenantsAccessControl_AllowsEventStoreOnlyForInternalRoutes() {
        YamlMappingNode root = LoadYaml(RepositoryPath("deploy", "dapr", "accesscontrol.tenants.yaml"));

        Scalar(root, "metadata", "name").ShouldBe("accesscontrol-tenants");
        Scalar(root, "spec", "accessControl", "defaultAction").ShouldBe("deny");
        Policies(root).Select(policy => Scalar(policy, "appId")).ShouldBe(["eventstore"]);

        YamlMappingNode policy = SinglePolicy(root, "eventstore");
        Scalar(policy, "defaultAction").ShouldBe("deny");

        YamlMappingNode[] operations = Sequence(policy, "operations").OfType<YamlMappingNode>().ToArray();
        operations.Length.ShouldBe(2);
        operations.Select(operation => Scalar(operation, "name")).ShouldBe(["/process", "/project"], ignoreOrder: true);
        foreach (YamlMappingNode operation in operations) {
            ScalarValues(operation, "httpVerb").ShouldBe(["POST"]);
            Scalar(operation, "action").ShouldBe("allow");
        }
    }

    [Fact]
    public void ProductionReceiverAccessControl_IsDenyByDefaultAndDoesNotGrantBroadTenantsAccess() {
        YamlMappingNode eventStore = LoadYaml(RepositoryPath("deploy", "dapr", "accesscontrol.eventstore.yaml"));
        YamlMappingNode admin = LoadYaml(RepositoryPath("deploy", "dapr", "accesscontrol.eventstore-admin.yaml"));

        Scalar(eventStore, "spec", "accessControl", "defaultAction").ShouldBe("deny");
        Scalar(admin, "spec", "accessControl", "defaultAction").ShouldBe("deny");

        SinglePolicy(eventStore, "eventstore-admin");
        Policies(eventStore).Select(policy => Scalar(policy, "appId")).ShouldNotContain("tenants");
        Policies(eventStore).Select(policy => Scalar(policy, "appId")).ShouldNotContain("sample");
        Policies(eventStore).Select(policy => Scalar(policy, "appId")).ShouldNotContain("eventstore-admin-ui");
        Policies(admin).ShouldBeEmpty();
    }

    [Fact]
    public void LocalAccessControl_IsClearlyLocalOnlyAndProductionUsesReceiverSpecificFiles() {
        string localPath = RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "accesscontrol.yaml");
        string localContent = File.ReadAllText(localPath);
        YamlMappingNode local = LoadYaml(localPath);

        localContent.ShouldContain("Local development only");
        localContent.ShouldContain("Production MUST use receiver-specific deny-by-default configs");
        Scalar(local, "spec", "accessControl", "defaultAction").ShouldBe("allow");

        string[] productionAccessControlFiles = Directory
            .GetFiles(RepositoryPath("deploy", "dapr"), "accesscontrol.*.yaml", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        productionAccessControlFiles.ShouldBe(
            [
                "accesscontrol.eventstore-admin.yaml",
                "accesscontrol.eventstore.yaml",
                "accesscontrol.tenants.yaml",
            ]);
    }

    [Fact]
    public void ProductionTemplates_DoNotContainConcreteSecretsOrPrivateHosts() {
        string deployDapr = RepositoryPath("deploy", "dapr");
        string[] files = Directory.GetFiles(deployDapr, "*.yaml", SearchOption.TopDirectoryOnly);

        foreach (string file in files) {
            string content = File.ReadAllText(file);
            content.ShouldNotContain("password=");
            content.ShouldNotContain("AccountKey=");
            content.ShouldNotContain("SharedAccessKey=");
            content.ShouldNotContain("localhost");
            content.ShouldNotContain("127.0.0.1");
            if (content.Contains("redis", StringComparison.OrdinalIgnoreCase)
                || content.Contains("trustDomain", StringComparison.OrdinalIgnoreCase)) {
                content.ShouldContain("{");
                content.ShouldContain("}");
            }
        }
    }

    [Fact]
    public void DaprDocumentation_RecordsLocalSlimProductionPrerequisitesAndFailureTriage() {
        string quickstart = File.ReadAllText(RepositoryPath("docs", "quickstart.md"));
        string deployment = File.ReadAllText(RepositoryPath("deploy", "dapr", "README.md"));
        string combined = quickstart + Environment.NewLine + deployment;

        combined.ShouldContain("dapr init");
        combined.ShouldContain("dapr init --slim");
        combined.ShouldContain("localhost:6379");
        combined.ShouldContain("50005");
        combined.ShouldContain("6050");
        combined.ShouldContain("50006");
        combined.ShouldContain("6060");
        combined.ShouldContain("missing state store");
        combined.ShouldContain("missing pub/sub");
        combined.ShouldContain("missing placement");
        combined.ShouldContain("missing scheduler");
        combined.ShouldContain("wrong AppId");
        combined.ShouldContain("wrong component name");
        combined.ShouldContain("wrong component scope");
        combined.ShouldContain("denied service invocation");
    }

    [Fact]
    public void TenantsDomainPackages_DoNotReferenceProviderSpecificInfrastructurePackages() {
        string[] projectFiles =
        [
            RepositoryPath("src", "Hexalith.Tenants.Contracts", "Hexalith.Tenants.Contracts.csproj"),
            RepositoryPath("src", "Hexalith.Tenants.Client", "Hexalith.Tenants.Client.csproj"),
            RepositoryPath("src", "Hexalith.Tenants.Server", "Hexalith.Tenants.Server.csproj"),
            RepositoryPath("src", "Hexalith.Tenants.Testing", "Hexalith.Tenants.Testing.csproj"),
            RepositoryPath("src", "Hexalith.Tenants", "Hexalith.Tenants.csproj"),
        ];

        string[] forbiddenPackagePrefixes =
        [
            "AWSSDK",
            "Azure.Messaging",
            "Azure.Data",
            "Azure.Storage",
            "Confluent.Kafka",
            "Microsoft.Data.SqlClient",
            "MongoDB.Driver",
            "MySqlConnector",
            "Npgsql",
            "RabbitMQ.Client",
            "StackExchange.Redis",
            "System.Data.SqlClient",
        ];

        foreach (string projectFile in projectFiles) {
            XDocument project = XDocument.Load(projectFile);
            string[] packageReferences = project
                .Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .OfType<string>()
                .ToArray();

            foreach (string packageReference in packageReferences) {
                forbiddenPackagePrefixes.Any(
                    forbidden => packageReference.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase)).ShouldBeFalse(
                    $"{Path.GetFileName(projectFile)} must keep provider-specific infrastructure behind DAPR component YAML.");
            }
        }
    }

    private static YamlMappingNode LoadYaml(string path) {
        using var reader = new StreamReader(path);
        var yaml = new YamlStream();
        yaml.Load(reader);
        return (YamlMappingNode)yaml.Documents[0].RootNode;
    }

    private static string? MetadataValue(YamlMappingNode root, string name) {
        var metadata = (YamlSequenceNode)Node(root, "spec", "metadata");
        return metadata
            .OfType<YamlMappingNode>()
            .Where(node => Scalar(node, "name") == name)
            .Select(node => Scalar(node, "value"))
            .SingleOrDefault();
    }

    private static YamlMappingNode SinglePolicy(YamlMappingNode root, string appId)
        => Policies(root).Single(policy => Scalar(policy, "appId") == appId);

    private static YamlMappingNode[] Policies(YamlMappingNode root)
        => Sequence(root, "spec", "accessControl", "policies").OfType<YamlMappingNode>().ToArray();

    private static string[] Scopes(YamlMappingNode root)
        => ScalarValues(root, "scopes");

    private static string[] ScalarValues(YamlMappingNode root, params string[] path)
        => Sequence(root, path)
            .OfType<YamlScalarNode>()
            .Select(node => node.Value)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static YamlSequenceNode Sequence(YamlMappingNode root, params string[] path)
        => (YamlSequenceNode)Node(root, path);

    private static string? Scalar(YamlMappingNode root, params string[] path)
        => ((YamlScalarNode)Node(root, path)).Value;

    private static YamlNode Node(YamlMappingNode root, params string[] path) {
        YamlNode current = root;
        foreach (string segment in path) {
            current = ((YamlMappingNode)current).Children[new YamlScalarNode(segment)];
        }

        return current;
    }

    private static void ShouldOccurBefore(string content, string earlier, string later) {
        int earlierIndex = content.IndexOf(earlier, StringComparison.Ordinal);
        int laterIndex = content.IndexOf(later, StringComparison.Ordinal);

        earlierIndex.ShouldBeGreaterThanOrEqualTo(0, $"{earlier} should exist.");
        laterIndex.ShouldBeGreaterThanOrEqualTo(0, $"{later} should exist.");
        earlierIndex.ShouldBeLessThan(laterIndex, $"{earlier} should occur before {later}.");
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
