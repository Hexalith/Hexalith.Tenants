using System.Reflection;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>
/// Verifies the package-mode AppHost model binds Memories to the consumer-owned secret store.
/// </summary>
/// <param name="output">Receives the inspected AppHost build-mode evidence.</param>
[Trait("Category", "Integration")]
public sealed class MemoriesSecretStoreResourceGraphTests(ITestOutputHelper output)
{
    private const string MemoriesResourceName = "memories";
    private const string SecretStoreResourceName = "memories-secretstore";

    /// <summary>
    /// Builds the model without starting resources and verifies the exact Memories secret-store relationships.
    /// </summary>
    [Fact]
    public async Task PackageModeAppHostUsesTheConsumerOwnedMemoriesSecretStore()
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>();
        Assembly appHostAssembly = builder.AppHostAssembly.ShouldNotBeNull();
        output.WriteLine($"AppHost assembly: {appHostAssembly.Location}");
        AssemblyMetadataAttribute[] buildMetadata = appHostAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
        string[] propertyNames = ["UseNuGetDeps", "UseHexalithProjectReferences", "HexalithMemoriesFromSource"];
        Dictionary<string, string?> properties = propertyNames.ToDictionary(
            static name => name,
            name => buildMetadata.Where(attribute => attribute.Key == name).ShouldHaveSingleItem().Value,
            StringComparer.Ordinal);
        foreach (string name in propertyNames)
        {
            output.WriteLine($"{name}={properties[name] ?? "<empty>"}");
        }

        string.Equals(properties["UseNuGetDeps"], "true", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(
            "The inspected AppHost must be compiled in package mode.");
        string.Equals(properties["UseHexalithProjectReferences"], "false", StringComparison.OrdinalIgnoreCase).ShouldBeTrue(
            "Source-built models do not establish package-mode evidence.");
        (string.IsNullOrEmpty(properties["HexalithMemoriesFromSource"])
            || string.Equals(properties["HexalithMemoriesFromSource"], "false", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue(
            "The inspected AppHost must resolve the centrally pinned Memories Aspire package.");

        await using DistributedApplication app = await builder.BuildAsync();

        DistributedApplicationModel model = app.Services.GetRequiredService<DistributedApplicationModel>();
        DaprComponentResource secretStore = model.Resources
            .OfType<DaprComponentResource>()
            .Where(static resource => resource.Name == SecretStoreResourceName)
            .ShouldHaveSingleItem();

        string expectedPath = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, "DaprComponents", "secretstore.memories.yaml"));
        secretStore.Type.ShouldBe("secretstores.local.file");
        Path.GetFullPath(secretStore.Options.ShouldNotBeNull().LocalPath.ShouldNotBeNull()).ShouldBe(expectedPath);

        ProjectResource memories = model.Resources
            .OfType<ProjectResource>()
            .Where(static resource => resource.Name == MemoriesResourceName)
            .ShouldHaveSingleItem();
        IDaprSidecarResource sidecar = memories.Annotations
            .OfType<DaprSidecarAnnotation>()
            .ShouldHaveSingleItem()
            .Sidecar;

        memories.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .Where(annotation => ReferenceEquals(annotation.Component, secretStore))
            .ShouldHaveSingleItem();
        sidecar.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .Where(annotation => ReferenceEquals(annotation.Component, secretStore))
            .ShouldHaveSingleItem();
        memories.Annotations
            .OfType<WaitAnnotation>()
            .Where(annotation => ReferenceEquals(annotation.Resource, secretStore))
            .ShouldHaveSingleItem();
    }
}
