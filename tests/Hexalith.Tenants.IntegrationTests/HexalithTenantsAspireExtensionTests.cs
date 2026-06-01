using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Tenants.AppHost;
using Hexalith.Tenants.Aspire;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

[Trait("Category", "ApplicationModel")]
public class HexalithTenantsAspireExtensionTests {
    [Fact]
    public void AddHexalithTenants_DefaultOptions_CreateExpectedDaprResources() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        HexalithTenantsResources resources = builder.AddHexalithTenants(tenants);

        resources.StateStore.Resource.Name.ShouldBe("statestore");
        resources.StateStore.Resource.Type.ShouldBe("state.redis");
        resources.PubSub.Resource.Name.ShouldBe("pubsub");
        resources.CommandApi.ShouldBeSameAs(tenants);
        resources.StateStore.ShouldNotBeNull();
        resources.PubSub.ShouldNotBeNull();

        IReadOnlyDictionary<string, string> stateStoreMetadata = GetDaprComponentMetadata(resources.StateStore.Resource);
        stateStoreMetadata.Keys.ShouldContain("actorStateStore", GetAnnotationDump(resources.StateStore.Resource));
        stateStoreMetadata["actorStateStore"].ShouldBe("true");
        stateStoreMetadata.Keys.ShouldContain("redisHost", GetAnnotationDump(resources.StateStore.Resource));
        stateStoreMetadata["redisHost"].ShouldBe("localhost:6379");
    }

    [Fact]
    public void AddHexalithTenants_DefaultOptions_WireTenantsDaprSidecarAndReferences() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        HexalithTenantsResources resources = builder.AddHexalithTenants(tenants);

        IDaprSidecarResource sidecar = GetDaprSidecar(tenants.Resource);
        DaprSidecarOptions options = GetDaprSidecarOptions(sidecar);
        IDaprComponentResource[] referencedComponents = GetReferencedDaprComponents(sidecar);

        options.AppId.ShouldBe("tenants");
        options.Config.ShouldBeNull();
        options.DaprHttpPort.ShouldBeNull();
        options.DaprGrpcPort.ShouldBeNull();
        referencedComponents.ShouldContain(resources.StateStore.Resource);
        referencedComponents.ShouldContain(resources.PubSub.Resource);
    }

    [Fact]
    public void AddHexalithTenants_CompatibleConfigPathOverload_ConfiguresDaprSidecar() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        HexalithTenantsResources resources = builder.AddHexalithTenants(tenants, "/tmp/legacy-accesscontrol.yaml");

        IDaprSidecarResource sidecar = GetDaprSidecar(tenants.Resource);
        DaprSidecarOptions options = GetDaprSidecarOptions(sidecar);
        IDaprComponentResource[] referencedComponents = GetReferencedDaprComponents(sidecar);

        options.AppId.ShouldBe("tenants");
        options.Config.ShouldBe("/tmp/legacy-accesscontrol.yaml");
        referencedComponents.ShouldContain(resources.StateStore.Resource);
        referencedComponents.ShouldContain(resources.PubSub.Resource);
    }

    [Fact]
    public void AddHexalithTenants_OptionsInstance_OverrideNamesAndConfigPath() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        HexalithTenantsResources resources = builder.AddHexalithTenants(
            tenants,
            new HexalithTenantsAspireOptions {
                AppId = "instance-tenants",
                StateStoreName = "instance-state",
                PubSubName = "instance-pubsub",
                DaprConfigPath = "/tmp/instance-accesscontrol.yaml",
                StateStoreComponentType = "state.redis",
                RedisHost = "redis.instance.test:6379",
            });

        IDaprSidecarResource sidecar = GetDaprSidecar(tenants.Resource);
        DaprSidecarOptions options = GetDaprSidecarOptions(sidecar);
        IReadOnlyDictionary<string, string> stateStoreMetadata = GetDaprComponentMetadata(resources.StateStore.Resource);

        resources.StateStore.Resource.Name.ShouldBe("instance-state");
        resources.StateStore.Resource.Type.ShouldBe("state.redis");
        resources.PubSub.Resource.Name.ShouldBe("instance-pubsub");
        options.AppId.ShouldBe("instance-tenants");
        options.Config.ShouldBe("/tmp/instance-accesscontrol.yaml");
        stateStoreMetadata["redisHost"].ShouldBe("redis.instance.test:6379");
    }

    [Fact]
    public void AddHexalithTenants_ConfiguredOptions_OverrideNamesAndConfigPath() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        HexalithTenantsResources resources = builder.AddHexalithTenants(
            tenants,
            options => {
                options.AppId = "custom-tenants";
                options.StateStoreName = "custom-state";
                options.PubSubName = "custom-pubsub";
                options.DaprConfigPath = "/tmp/tenants-accesscontrol.yaml";
                options.RedisHost = "redis.example.test:6380";
            });

        IDaprSidecarResource sidecar = GetDaprSidecar(tenants.Resource);
        DaprSidecarOptions options = GetDaprSidecarOptions(sidecar);
        IReadOnlyDictionary<string, string> stateStoreMetadata = GetDaprComponentMetadata(resources.StateStore.Resource);

        resources.StateStore.Resource.Name.ShouldBe("custom-state");
        resources.PubSub.Resource.Name.ShouldBe("custom-pubsub");
        options.AppId.ShouldBe("custom-tenants");
        options.Config.ShouldBe("/tmp/tenants-accesscontrol.yaml");
        stateStoreMetadata.Keys.ShouldContain("redisHost", GetAnnotationDump(resources.StateStore.Resource));
        stateStoreMetadata["redisHost"].ShouldBe("redis.example.test:6380");
    }

    [Theory]
    [InlineData("AppId")]
    [InlineData("StateStoreName")]
    [InlineData("PubSubName")]
    [InlineData("StateStoreComponentType")]
    [InlineData("RedisHost")]
    [InlineData("DaprConfigPath")]
    public void AddHexalithTenants_InvalidOptions_FailBeforeBuildExecution(string invalidProperty) {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            builder.AddHexalithTenants(
                tenants,
                options => SetStringProperty(options, invalidProperty, " ")));

        exception.Message.ShouldContain(invalidProperty);
    }

    [Theory]
    [InlineData("AppId", "tenant service")]
    [InlineData("StateStoreName", "tenant state")]
    [InlineData("PubSubName", "tenant pubsub")]
    [InlineData("StateStoreComponentType", "state redis")]
    [InlineData("RedisHost", "redis example.test:6379")]
    public void AddHexalithTenants_OptionsWithEmbeddedWhitespace_FailBeforeBuildExecution(
        string invalidProperty,
        string invalidValue) {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            builder.AddHexalithTenants(
                tenants,
                options => SetStringProperty(options, invalidProperty, invalidValue)));

        exception.Message.ShouldContain(invalidProperty);
        exception.Message.ShouldContain("whitespace");
    }

    [Fact]
    public void AddHexalithTenants_StateStoreComponentTypeWithoutProvider_FailsBeforeBuildExecution() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");

        ArgumentException exception = Should.Throw<ArgumentException>(() =>
            builder.AddHexalithTenants(
                tenants,
                options => options.StateStoreComponentType = "state"));

        exception.Message.ShouldContain(nameof(HexalithTenantsAspireOptions.StateStoreComponentType));
        exception.Message.ShouldContain("category.provider");
    }

    [Fact]
    public void AddHexalithTenants_NullBuilderForConfigureOptionsOverload_FailsBeforeInvokingCallback() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants");
        var callbackInvoked = false;

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            HexalithTenantsExtensions.AddHexalithTenants(
                null!,
                tenants,
                _ => callbackInvoked = true));

        exception.ParamName.ShouldBe("builder");
        callbackInvoked.ShouldBeFalse();
    }

    [Fact]
    public void AddHexalithTenants_NullTenantsForConfigureOptionsOverload_FailsBeforeInvokingCallback() {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder([]);
        var callbackInvoked = false;

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            builder.AddHexalithTenants(
                null!,
                _ => callbackInvoked = true));

        exception.ParamName.ShouldBe("tenants");
        callbackInvoked.ShouldBeFalse();
    }

    private static IReadOnlyDictionary<string, string> GetDaprComponentMetadata(IDaprComponentResource resource) {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal);

        foreach (IResourceAnnotation annotation in resource.Annotations) {
            AddMetadata(annotation, metadata);
            AddConfiguredMetadata(annotation, metadata);

            object? nestedMetadata = annotation.GetType().GetProperty("Metadata")?.GetValue(annotation);
            if (nestedMetadata is System.Collections.IEnumerable metadataItems and not string) {
                foreach (object item in metadataItems) {
                    AddMetadata(item, metadata);
                }
            }
        }

        return metadata;
    }

    private static void AddConfiguredMetadata(IResourceAnnotation annotation, Dictionary<string, string> metadata) {
        object? configure = annotation.GetType().GetProperty("Configure")?.GetValue(annotation);
        if (configure is not Delegate configureDelegate) {
            return;
        }

        Type schemaType = configureDelegate.Method.GetParameters()[0].ParameterType;
        object schema = Activator.CreateInstance(schemaType)
            ?? throw new InvalidOperationException($"Could not create DAPR component schema {schemaType.FullName}.");
        InitializeObjectGraph(schema, 0);

        object? result = configureDelegate.DynamicInvoke(schema, CancellationToken.None);
        if (result is Task task) {
            task.GetAwaiter().GetResult();
        }

        AddMetadataFromObjectGraph(schema, metadata, 0);
    }

    private static void AddMetadataFromObjectGraph(object target, Dictionary<string, string> metadata, int depth) {
        if (depth > 4) {
            return;
        }

        AddMetadata(target, metadata);
        foreach (System.Reflection.PropertyInfo property in target.GetType().GetProperties()) {
            if (!property.CanRead) {
                continue;
            }

            object? value = property.GetValue(target);
            if (value is null || value is string) {
                continue;
            }

            if (value is System.Collections.IEnumerable items) {
                foreach (object item in items) {
                    AddMetadataFromObjectGraph(item, metadata, depth + 1);
                }
            }
            else if (ShouldInitializeChildren(value.GetType())) {
                AddMetadataFromObjectGraph(value, metadata, depth + 1);
            }
        }
    }

    private static void InitializeObjectGraph(object target, int depth) {
        if (depth > 2) {
            return;
        }

        foreach (System.Reflection.PropertyInfo property in target.GetType().GetProperties()) {
            if (!property.CanRead) {
                continue;
            }

            object? value = property.GetValue(target);
            if (value is null && property.CanWrite) {
                value = CreateDefaultValue(property.PropertyType);
                if (value is not null) {
                    property.SetValue(target, value);
                }
            }

            if (value is not null && ShouldInitializeChildren(value.GetType())) {
                InitializeObjectGraph(value, depth + 1);
            }
        }
    }

    private static object? CreateDefaultValue(Type propertyType) {
        if (propertyType == typeof(string) || propertyType.IsValueType) {
            return null;
        }

        if (TryCreateList(propertyType, out object? list)) {
            return list;
        }

        return propertyType.GetConstructor(Type.EmptyTypes) is not null
            ? Activator.CreateInstance(propertyType)
            : null;
    }

    private static bool TryCreateList(Type collectionType, out object? collection) {
        collection = null;
        if (!collectionType.IsGenericType) {
            return false;
        }

        Type itemType = collectionType.IsGenericType
            ? collectionType.GetGenericArguments()[0]
            : typeof(object);
        object list = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))
            ?? throw new InvalidOperationException($"Could not create collection for {collectionType.FullName}.");

        if (!collectionType.IsInstanceOfType(list)) {
            return false;
        }

        collection = list;
        return true;
    }

    private static bool ShouldInitializeChildren(Type type)
        => type != typeof(string)
            && !type.IsValueType
            && !typeof(Delegate).IsAssignableFrom(type)
            && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

    private static void AddMetadata(object metadataItem, Dictionary<string, string> metadata) {
        string? name = metadataItem.GetType().GetProperty("MetadataName")?.GetValue(metadataItem)?.ToString()
            ?? metadataItem.GetType().GetProperty("Name")?.GetValue(metadataItem)?.ToString();
        object? value = metadataItem.GetType().GetProperty("Metadata")?.GetValue(metadataItem)
            ?? metadataItem.GetType().GetProperty("Value")?.GetValue(metadataItem);

        if (!string.IsNullOrWhiteSpace(name)) {
            metadata[name] = value?.ToString() ?? string.Empty;
        }
    }

    private static string GetAnnotationDump(IResource resource)
        => string.Join(
            "; ",
            resource.Annotations.Select(annotation =>
                $"{annotation.GetType().FullName}: {string.Join(", ", annotation.GetType().GetProperties().Select(property => property.Name + "=" + FormatValue(property.GetValue(annotation))))}"));

    private static string FormatValue(object? value) {
        if (value is null) {
            return "<null>";
        }

        if (value is string text) {
            return text;
        }

        if (value is System.Collections.IEnumerable items) {
            return "[" + string.Join("|", items.Cast<object>().Select(item => item.GetType().FullName + ":" + item)) + "]";
        }

        return value.ToString() ?? string.Empty;
    }

    private static IDaprSidecarResource GetDaprSidecar(IResource resource) {
        DaprSidecarAnnotation annotation = resource.Annotations
            .OfType<DaprSidecarAnnotation>()
            .Single();

        return annotation.Sidecar;
    }

    private static DaprSidecarOptions GetDaprSidecarOptions(IDaprSidecarResource sidecar) {
        DaprSidecarOptionsAnnotation annotation = sidecar.Annotations
            .OfType<DaprSidecarOptionsAnnotation>()
            .Single();

        return annotation.Options;
    }

    private static IDaprComponentResource[] GetReferencedDaprComponents(IDaprSidecarResource sidecar)
        => sidecar.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .Select(annotation => annotation.Component)
            .ToArray();

    private static void SetStringProperty(HexalithTenantsAspireOptions options, string propertyName, string value)
        => typeof(HexalithTenantsAspireOptions)
            .GetProperty(propertyName)
            ?.SetValue(options, value);
}
