using System.Security.Cryptography;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Verifies effective local key pairing and externally owned published enrollment.</summary>
public sealed class SourceAuthorityEnrollmentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunPublicKeyMatchesTheEffectivePrivateParameter(bool overridden)
    {
        using RSA supplied = RSA.Create(2048);
        string privateKey = Convert.ToBase64String(supplied.ExportPkcs8PrivateKey());
        List<string> args = ["--EnableKeycloak=true"];
        if (overridden) args.Add("--Parameters:tenants-source-private-key=" + privateKey);
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>([.. args]);
        ParameterResource parameter = builder.Resources.OfType<ParameterResource>()
            .Single(resource => resource.Name == "tenants-source-private-key");
        string effective = (await parameter.GetValueAsync(CancellationToken.None))!;
        if (overridden) string.Equals(effective, privateKey, StringComparison.Ordinal).ShouldBeTrue("the existing private parameter override must take effect");
        using RSA signer = RSA.Create();
        signer.ImportPkcs8PrivateKey(Convert.FromBase64String(effective), out _);
        IDictionary<string, object> environment = await EnvironmentAsync(builder, "eventstore");
        using RSA verifier = RSA.Create();
        verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String((string)environment["Authentication__BoundStreamRead__PublicKeys__tenants"]), out _);
        byte[] message = "effective-source-key-pair"u8.ToArray();
        verifier.VerifyData(message, signer.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).ShouldBeTrue();
        (await EnvironmentAsync(builder, "tenants"))["Authentication__BoundStreamRead__PrivateKey"].ShouldBeSameAs(parameter);
    }

    [Fact]
    public async Task PublishedModelLeavesSourceKeysToExternalEnrollment()
    {
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>(["--EnableKeycloak=true", "--publisher", "manifest", "--output-path",
                Path.Combine(Path.GetTempPath(), $"tenants-source-model-{Guid.NewGuid():N}.json")]);
        builder.Resources.Any(resource => resource.Name == "tenants-source-private-key").ShouldBeFalse();
        foreach (string name in new[] { "eventstore", "tenants" })
            (await EnvironmentAsync(builder, name)).Keys.ShouldNotContain(key => key.StartsWith("Authentication__BoundStreamRead__", StringComparison.Ordinal));
    }

    private static async Task<IDictionary<string, object>> EnvironmentAsync(IDistributedApplicationTestingBuilder builder, string name)
    {
        IResource resource = builder.Resources.Single(resource => resource.Name == name);
        var context = new EnvironmentCallbackContext(builder.ExecutionContext, resource, new Dictionary<string, object>());
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);
        return context.EnvironmentVariables;
    }
}
