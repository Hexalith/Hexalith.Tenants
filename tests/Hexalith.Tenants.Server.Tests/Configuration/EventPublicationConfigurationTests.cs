using System.Text.Json;
using System.Text.RegularExpressions;
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
    public void AppHost_DaprTopology_UsesStableResourceNamesAppIdsAndDynamicSidecarPorts() {
        string program = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants.AppHost", "Program.cs"));
        string options = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants.Aspire", "HexalithTenantsAspireOptions.cs"));
        string extension = File.ReadAllText(RepositoryPath("src", "Hexalith.Tenants.Aspire", "HexalithTenantsExtensions.cs"));

        string[] requiredResourceNames = ["eventstore", "eventstore-admin", "eventstore-admin-ui", "tenants", "sample"];
        foreach (string resourceName in requiredResourceNames) {
            program.ShouldContain($"AddProject<");
            program.ShouldContain($"\"{resourceName}\"");
        }

        string[] programAppIds = ["eventstore", "eventstore-admin", "eventstore-admin-ui", "sample"];
        foreach (string appId in programAppIds) {
            program.ShouldContain($"AppId = \"{appId}\"");
        }

        options.ShouldContain("AppId");
        options.ShouldContain("\"tenants\"");
        options.ShouldContain("StateStoreName");
        options.ShouldContain("\"statestore\"");
        options.ShouldContain("PubSubName");
        options.ShouldContain("\"pubsub\"");
        extension.ShouldContain(".WithMetadata(\"actorStateStore\", \"true\")");
        extension.ShouldContain(".WithReference(stateStore)");
        extension.ShouldContain(".WithReference(pubSub)");
        program.ShouldContain("ResolveDaprConfigPath");
        program.ShouldContain("accesscontrol.yaml");
        program.ShouldContain("accesscontrol.eventstore-admin.yaml");
        program.ShouldNotContain("DaprHttpPort =");
        program.ShouldNotContain("DaprGrpcPort =");
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
    public void DaprSmokeContracts_NameDeploymentInputsAndDenyUnexpectedServiceInvocation() {
        YamlMappingNode localStateStore = LoadYaml(RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "statestore.yaml"));
        YamlMappingNode localPubSub = LoadYaml(RepositoryPath("src", "Hexalith.Tenants.AppHost", "DaprComponents", "pubsub.yaml"));
        YamlMappingNode productionStateStore = LoadYaml(RepositoryPath("deploy", "dapr", "statestore.yaml"));
        YamlMappingNode productionPubSub = LoadYaml(RepositoryPath("deploy", "dapr", "pubsub.yaml"));
        YamlMappingNode tenantsAccessControl = LoadYaml(RepositoryPath("deploy", "dapr", "accesscontrol.tenants.yaml"));
        string docs = File.ReadAllText(RepositoryPath("deploy", "dapr", "README.md"))
            + Environment.NewLine
            + File.ReadAllText(RepositoryPath("docs", "quickstart.md"));

        foreach (YamlMappingNode stateStore in new[] { localStateStore, productionStateStore }) {
            Scalar(stateStore, "metadata", "name").ShouldBe("statestore");
            MetadataValue(stateStore, "actorStateStore").ShouldBe("true");
            Scopes(stateStore).ShouldContain("eventstore");
            Scopes(stateStore).ShouldContain("tenants");
            Scopes(stateStore).ShouldContain("eventstore-admin");
        }

        foreach (YamlMappingNode pubSub in new[] { localPubSub, productionPubSub }) {
            Scalar(pubSub, "metadata", "name").ShouldBe("pubsub");
            MetadataValue(pubSub, "deadLetterTopic").ShouldBe(DeadLetterTopic);
            Scopes(pubSub).ShouldContain("eventstore");
        }

        string[] diagnosticTerms =
        [
            "missing state store",
            "missing pub/sub",
            "missing placement",
            "missing scheduler",
            "wrong AppId",
            "wrong component name",
            "wrong component scope",
            "denied service invocation",
            "statestore",
            "pubsub",
            "localhost:6379",
            OperatingSystem.IsWindows() ? "6050" : "50005",
            OperatingSystem.IsWindows() ? "6060" : "50006",
        ];

        foreach (string diagnosticTerm in diagnosticTerms) {
            docs.ShouldContain(diagnosticTerm);
        }

        Scalar(tenantsAccessControl, "spec", "accessControl", "defaultAction").ShouldBe("deny");
        Policies(tenantsAccessControl).Select(policy => Scalar(policy, "appId")).ShouldBe(["eventstore"]);
        YamlMappingNode eventStorePolicy = SinglePolicy(tenantsAccessControl, "eventstore");
        Scalar(eventStorePolicy, "defaultAction").ShouldBe("deny");
        Sequence(eventStorePolicy, "operations")
            .OfType<YamlMappingNode>()
            .Select(operation => $"{Scalar(operation, "action")} {string.Join(',', ScalarValues(operation, "httpVerb"))} {Scalar(operation, "name")}")
            .ShouldBe(["allow POST /process", "allow POST /project"], ignoreOrder: true);
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
    public void DaprSmokeEvidenceDocs_DoNotContainConcreteSecretsTokensOrPrivateConnectionDetails() {
        string[] redactedEvidenceFiles =
        [
            RepositoryPath("deploy", "dapr", "README.md"),
            .. Directory.GetFiles(RepositoryPath("deploy", "dapr"), "*.yaml", SearchOption.TopDirectoryOnly),
        ];
        string quickstart = File.ReadAllText(RepositoryPath("docs", "quickstart.md"));

        Regex compactJwt = new(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.Compiled);
        Regex bearerToken = new(@"Bearer\s+[A-Za-z0-9._~+/=-]{20,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex privateConnectionString = new(
            @"(AccountKey=|SharedAccessKey=|Password=[^{}\s]|redis://|amqp://|Endpoint=sb://)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex rawPrivateAddress = new(
            @"(?<!localhost:)(?<!127\.0\.0\.1:)\b(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3})\b",
            RegexOptions.Compiled);

        foreach (string file in redactedEvidenceFiles) {
            string content = File.ReadAllText(file);
            compactJwt.IsMatch(content).ShouldBeFalse($"{file} must not include compact JWTs.");
            bearerToken.IsMatch(content).ShouldBeFalse($"{file} must not include bearer tokens.");
            privateConnectionString.IsMatch(content).ShouldBeFalse($"{file} must not include concrete connection strings or passwords.");
            rawPrivateAddress.IsMatch(content).ShouldBeFalse($"{file} must not include private network addresses.");
        }

        compactJwt.IsMatch(quickstart).ShouldBeFalse("quickstart must not include compact JWTs.");
        bearerToken.IsMatch(quickstart).ShouldBeFalse("quickstart must not include bearer tokens.");
        rawPrivateAddress.IsMatch(quickstart).ShouldBeFalse("quickstart must not include private network addresses.");
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
    public void LocalKeycloakRealm_AdminUserAuthorizesQuickstartCommandDomains() {
        string realmPath = RepositoryPath("src", "Hexalith.Tenants.AppHost", "KeycloakRealms", "hexalith-realm.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(realmPath));

        JsonElement adminUser = document.RootElement
            .GetProperty("users")
            .EnumerateArray()
            .Single(user => user.GetProperty("username").GetString() == "admin-user");

        JsonElement attributes = adminUser.GetProperty("attributes");
        attributes.GetProperty("tenants").EnumerateArray().Select(value => value.GetString()).OfType<string>().ShouldContain("system");
        attributes.GetProperty("domains").EnumerateArray().Select(value => value.GetString()).OfType<string>().ShouldBe(
            ["global-administrators", "tenants", "orders", "inventory", "counter"],
            ignoreOrder: true);
        attributes.GetProperty("permissions").EnumerateArray().Select(value => value.GetString()).OfType<string>().ShouldContain("command:submit");
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
