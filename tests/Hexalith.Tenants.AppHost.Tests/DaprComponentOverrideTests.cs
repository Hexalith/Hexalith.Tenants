using Aspire.Hosting;
using Aspire.Hosting.Testing;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Verifies trusted fresh-state qualification wiring without starting services or modifying existing state.</summary>
public sealed class DaprComponentOverrideTests
{
    [Fact]
    public async Task PairedOverridesUseTheExistingSharedComponentsAndPreserveDefaultTemplates()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        string defaults = Path.Combine(root, "src/Hexalith.Tenants.AppHost/DaprComponents");
        string directory = Path.Combine(Path.GetTempPath(), "tenants-component-model-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        byte[] state = await File.ReadAllBytesAsync(Path.Combine(defaults, "statestore.yaml"));
        byte[] pubsub = await File.ReadAllBytesAsync(Path.Combine(defaults, "pubsub.yaml"));
        try
        {
            foreach (string file in Directory.GetFiles(defaults, "*.yaml"))
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
            string statePath = Path.Combine(directory, "statestore.yaml");
            string pubsubPath = Path.Combine(directory, "pubsub.yaml");
            await File.WriteAllBytesAsync(statePath, state);
            await File.WriteAllBytesAsync(pubsubPath, pubsub);
            await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Hexalith_Tenants_AppHost>(["--EnableKeycloak=true",
                    "--Dapr:StateStoreComponentPath=" + statePath, "--Dapr:PubSubComponentPath=" + pubsubPath]);
            DaprComponentResource[] components = builder.Resources.OfType<DaprComponentResource>().ToArray();
            components.Single(resource => resource.Name == "statestore").Options!.LocalPath.ShouldBe(statePath);
            components.Single(resource => resource.Name == "pubsub").Options!.LocalPath.ShouldBe(pubsubPath);
            components.Single(resource => resource.Name == "memories-secretstore").Options!.LocalPath
                .ShouldBe(Path.Combine(directory, "secretstore.memories.yaml"));
            components.Single(resource => resource.Name == "memories-llm").Options!.LocalPath
                .ShouldBe(Path.Combine(directory, "llm.memories.yaml"));
            (await File.ReadAllBytesAsync(Path.Combine(defaults, "statestore.yaml"))).ShouldBe(state);
            (await File.ReadAllBytesAsync(Path.Combine(defaults, "pubsub.yaml"))).ShouldBe(pubsub);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("StateStore")]
    [InlineData("PubSub")]
    public async Task PartialOverridesAreRefusedBeforeAnyResourceStarts(string component)
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Hexalith_Tenants_AppHost>(["--EnableKeycloak=true",
                    "--Dapr:" + component + "ComponentPath=unconfigured.yaml"]);
        });
        exception.Message.ShouldBe("Dapr state-store and pub/sub component path overrides must be supplied together.");
    }
}
