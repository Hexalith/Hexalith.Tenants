extern alias TenantsApi;

using System.Diagnostics;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public sealed class TenantsApiProductionAuthenticationTests
{
    private const string DomainSection = "Authentication:JwtBearer";
    private const string ApiSection = "EventStore:Authentication";
    private const string Authority = "https://identity.example.test";
    private const string Audience = "tenants-test";

    [Theory]
    [InlineData(DomainSection, false)]
    [InlineData(ApiSection, false)]
    [InlineData(ApiSection, true)]
    public void ProductionAppliesConfiguredOidcContractToBearerScheme(string section, bool useAuthorityAlias)
    {
        Dictionary<string, string?> settings = CreateSettings(section);
        if (useAuthorityAlias)
        {
            settings.Remove($"{section}:Authority");
            settings["Oidc:Authority"] = Authority;
        }

        using var host = CreateHost(settings);
        JwtBearerOptions bearer = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        bearer.Authority.ShouldBe(Authority);
        bearer.RequireHttpsMetadata.ShouldBeTrue();
        bearer.MapInboundClaims.ShouldBeFalse();
        bearer.TokenValidationParameters.ValidIssuer.ShouldBe(Authority);
        bearer.TokenValidationParameters.ValidAudiences.ShouldBe([Audience]);
        bearer.TokenValidationParameters.ValidAlgorithms.ShouldBe([SecurityAlgorithms.RsaSha256]);
        bearer.TokenValidationParameters.IssuerSigningKey.ShouldBeNull();
    }

    [Theory]
    [InlineData("Authority", null, "Authority")]
    [InlineData("Authority", "http://identity.example.test", "Authority")]
    [InlineData("Issuer", "   ", "Issuer")]
    [InlineData("Audience", null, "Audience")]
    [InlineData("RequireHttpsMetadata", "false", "RequireHttpsMetadata")]
    [InlineData("AllowedAlgorithms:0", null, "AllowedAlgorithms")]
    [InlineData("AllowedAlgorithms:0", " ", "AllowedAlgorithms")]
    [InlineData("AllowedAlgorithms:0", "HS256", "AllowedAlgorithms")]
    [InlineData("AllowedAlgorithms:1", "unsupported", "AllowedAlgorithms")]
    public async Task ProductionRejectsUnsafeConfigurationBeforeServingAsync(string key, string? value, string expectedSetting)
    {
        Dictionary<string, string?> settings = CreateSettings(ApiSection);
        if (value is null)
        {
            settings.Remove($"{ApiSection}:{key}");
        }
        else
        {
            settings[$"{ApiSection}:{key}"] = value;
        }

        string failure = await ReadStartupValidationFailureAsync(settings);
        failure.ShouldContain(expectedSetting);
        failure.ShouldNotContain(Authority);
    }

    [Theory]
    [InlineData(DomainSection + ":SigningKey", "private-test-marker")]
    [InlineData(ApiSection + ":SigningKey", "private-test-marker")]
    [InlineData("Jwt:SigningKey", "private-test-marker")]
    [InlineData(ApiSection + ":SigningKey", "   ")]
    public async Task ProductionRejectsSigningKeyUnderAnySupportedNameAsync(string key, string value)
    {
        Dictionary<string, string?> settings = CreateSettings(DomainSection);
        settings[key] = value;
        string failure = await ReadStartupValidationFailureAsync(settings);

        failure.ShouldContain(key);
        failure.ShouldNotContain(value);
    }

    // The process reports its original startup error even if the host immediately disposes
    // its services before a WebApplicationFactory client could observe the failure.
    private static async Task<string> ReadStartupValidationFailureAsync(Dictionary<string, string?> settings)
    {
        DirectoryInfo directory = Directory.CreateTempSubdirectory("tenants-api-startup-");
        try
        {
            ProcessStartInfo start = TenantsApiProcess.CreateStartInfo(directory.FullName, Environments.Production);
            foreach ((string key, string? value) in settings)
            {
                start.ArgumentList.Add($"--{key}={value}");
            }

            using Process process = TenantsApiProcess.Start(start, out Task<string> output, out Task<string> error);
            try
            {
                await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30));
                process.ExitCode.ShouldNotBe(0);
                const string marker = "Microsoft.Extensions.Options.OptionsValidationException: ";
                string? validation = (await error).Split('\n').FirstOrDefault(line => line.Contains(marker, StringComparison.Ordinal));
                validation.ShouldNotBeNull("The API process must report an options validation exception at startup.");
                return validation[(validation.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..].TrimEnd('\r');
            }
            finally
            {
                await TenantsApiProcess.StopAsync(process, output, error);
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static Dictionary<string, string?> CreateSettings(string section) => new()
    {
        [$"{section}:Authority"] = Authority,
        [$"{section}:Issuer"] = Authority,
        [$"{section}:Audience"] = Audience,
        [$"{section}:AllowedAlgorithms:0"] = SecurityAlgorithms.RsaSha256,
    };

    private static WebApplicationFactory<TenantsApi::Program> CreateHost(Dictionary<string, string?> settings)
        => new WebApplicationFactory<TenantsApi::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(settings);
            });
        });
}
