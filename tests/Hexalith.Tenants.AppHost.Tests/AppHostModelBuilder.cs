using System.Security.Cryptography;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Supplies distinct owner material for model-only tests without claiming a live backend.</summary>
internal static class AppHostModelBuilder
{
    internal static Task<IDistributedApplicationTestingBuilder> CreateAsync(params string[] args)
    {
        string custodyDirectory = Path.Combine(Path.GetTempPath(), $"tenants-model-custody-{Guid.NewGuid():N}");
        string digestKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        string[] modelArgs =
        [
            // Live-route CI exports this profile for its runtime. Model assertions select it explicitly.
            "--TENANTS_SOURCE_CATALOG_PROFILE=false",
            .. args,
            $"--Tenants:DevelopmentRouteCatalog:CustodyDirectory={custodyDirectory}",
            "--Tenants:DevelopmentRouteCatalog:AuthorityComponentName=routing-tenants-owner",
            "--Tenants:DevelopmentRouteCatalog:AuthorityStateKey=tenants-owner-authority",
            "--Tenants:DevelopmentRouteCatalog:ActiveDigestKeyGeneration=development-v1",
            $"--Tenants:DevelopmentRouteCatalog:DigestKeys:development-v1={digestKey}",
            "--Tenants:DevelopmentRouteCatalog:SecretMapGeneration=development-v1",
        ];
        return DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Tenants_AppHost>(modelArgs);
    }

    internal static async Task ApplyEnvironmentCallbackAsync(EnvironmentCallbackAnnotation annotation, EnvironmentCallbackContext context)
    {
        try
        {
            await annotation.Callback(context);
        }
        catch (InvalidOperationException error) when (error.Message == "The actual owner CLI has not been rendered.")
        {
            // Model inspections cannot run the catalog owner's executable credential observer.
        }
    }
}
