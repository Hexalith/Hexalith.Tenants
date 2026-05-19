using Hexalith.EventStore.Authentication;
using Hexalith.Tenants.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Configuration;

public class AuthenticationConfigurationTests {
    private const string AuthenticationSectionName = "Authentication:JwtBearer";
    private const string SecretSigningKey = "do-not-echo-this-production-signing-key";

    [Fact]
    public void ProductionAppSettingsAuthenticationShouldFailValidation() {
        IConfiguration configuration = CreateConfiguration();

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => ValidateOptions(configuration, Environments.Production));

        string message = string.Join(Environment.NewLine, exception.Failures);
        message.ShouldContain(AuthenticationSectionName);
        message.ShouldContain("Authority");
        message.ShouldContain("SigningKey");
        message.ShouldNotContain(SecretSigningKey);
    }

    [Fact]
    public void ProductionOidcOverridesShouldPassValidation() {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> {
            [$"{AuthenticationSectionName}:Authority"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Issuer"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Audience"] = "hexalith-tenants",
            [$"{AuthenticationSectionName}:SigningKey"] = string.Empty,
            [$"{AuthenticationSectionName}:RequireHttpsMetadata"] = "true",
        });

        Should.NotThrow(() => ValidateOptions(configuration, Environments.Production));
    }

    [Fact]
    public void ProductionEnvironmentVariablesShouldOverrideCommittedPlaceholders() {
        const string prefix = "HEXALITH_TENANTS_AUTH_TEST_";
        IReadOnlyDictionary<string, string?> variables = new Dictionary<string, string?> {
            [$"{prefix}Authentication__JwtBearer__Authority"] = "https://identity.example.test",
            [$"{prefix}Authentication__JwtBearer__Issuer"] = "https://identity.example.test",
            [$"{prefix}Authentication__JwtBearer__Audience"] = "hexalith-tenants",
            [$"{prefix}Authentication__JwtBearer__SigningKey"] = string.Empty,
            [$"{prefix}Authentication__JwtBearer__RequireHttpsMetadata"] = "true",
        };

        WithEnvironmentVariables(variables, () => {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables(prefix)
                .Build();

            Should.NotThrow(() => ValidateOptions(configuration, Environments.Production));
        });
    }

    [Fact]
    public void DevelopmentAppSettingsAuthenticationShouldPassValidation() {
        IConfiguration configuration = CreateConfiguration(null, includeDevelopment: true);

        Should.NotThrow(() => ValidateOptions(configuration, Environments.Development));
    }

    [Theory]
    [InlineData("Authority")]
    [InlineData("Issuer")]
    [InlineData("Audience")]
    [InlineData("SigningKey")]
    public void ProductionWhitespaceAuthenticationSettingsShouldFailValidation(string key) {
        var overrides = new Dictionary<string, string?> {
            [$"{AuthenticationSectionName}:Authority"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Issuer"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Audience"] = "hexalith-tenants",
            [$"{AuthenticationSectionName}:SigningKey"] = string.Empty,
            [$"{AuthenticationSectionName}:RequireHttpsMetadata"] = "true",
            [$"{AuthenticationSectionName}:{key}"] = new string(' ', 40),
        };

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => ValidateOptions(CreateConfiguration(overrides), Environments.Production));

        string message = string.Join(Environment.NewLine, exception.Failures);
        message.ShouldContain($"{AuthenticationSectionName}:{key}");
        message.ShouldNotContain(new string(' ', 40));
    }

    [Theory]
    [InlineData("identity.example.test")]
    [InlineData("/identity")]
    [InlineData("http://identity.example.test")]
    [InlineData("not a uri")]
    public void ProductionAuthorityShouldRequireAbsoluteHttpsUri(string authority) {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> {
            [$"{AuthenticationSectionName}:Authority"] = authority,
            [$"{AuthenticationSectionName}:Issuer"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Audience"] = "hexalith-tenants",
            [$"{AuthenticationSectionName}:SigningKey"] = string.Empty,
            [$"{AuthenticationSectionName}:RequireHttpsMetadata"] = "true",
        });

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => ValidateOptions(configuration, Environments.Production));

        string message = string.Join(Environment.NewLine, exception.Failures);
        message.ShouldContain($"{AuthenticationSectionName}:Authority");
        message.ShouldNotContain(authority);
    }

    [Fact]
    public void ProductionAuthorityWithSigningKeyShouldFailWithoutEchoingSecret() {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> {
            [$"{AuthenticationSectionName}:Authority"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Issuer"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Audience"] = "hexalith-tenants",
            [$"{AuthenticationSectionName}:SigningKey"] = SecretSigningKey,
            [$"{AuthenticationSectionName}:RequireHttpsMetadata"] = "true",
        });

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => ValidateOptions(configuration, Environments.Production));

        string message = string.Join(Environment.NewLine, exception.Failures);
        message.ShouldContain($"{AuthenticationSectionName}:Authority");
        message.ShouldContain($"{AuthenticationSectionName}:SigningKey");
        message.ShouldNotContain(SecretSigningKey);
    }

    [Fact]
    public void ProductionAuthorityWithDisabledHttpsMetadataShouldFail() {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> {
            [$"{AuthenticationSectionName}:Authority"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Issuer"] = "https://identity.example.test",
            [$"{AuthenticationSectionName}:Audience"] = "hexalith-tenants",
            [$"{AuthenticationSectionName}:SigningKey"] = string.Empty,
            [$"{AuthenticationSectionName}:RequireHttpsMetadata"] = "false",
        });

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => ValidateOptions(configuration, Environments.Production));

        string message = string.Join(Environment.NewLine, exception.Failures);
        message.ShouldContain($"{AuthenticationSectionName}:RequireHttpsMetadata");
    }

    [Fact]
    public void StartupValidationShouldComposeEventStoreAndTenantsValidators() {
        IConfiguration configuration = CreateConfiguration();

        OptionsValidationException exception = Should.Throw<OptionsValidationException>(
            () => ValidateOptionsOnStart(configuration, Environments.Production));

        string message = string.Join(Environment.NewLine, exception.Failures);
        message.ShouldContain("either 'Authority' (production OIDC) or 'SigningKey'");
        message.ShouldContain($"{AuthenticationSectionName}:Authority");
    }

    private static IConfiguration CreateConfiguration(
        IReadOnlyDictionary<string, string?>? overrides = null,
        bool includeDevelopment = false) {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false);

        if (includeDevelopment) {
            _ = builder.AddJsonFile("appsettings.Development.json", optional: false);
        }

        if (overrides is not null) {
            _ = builder.AddInMemoryCollection(overrides);
        }

        return builder.Build();
    }

    private static void ValidateOptions(IConfiguration configuration, string environmentName)
        => CreateServiceProvider(configuration, environmentName)
            .GetRequiredService<IOptions<EventStoreAuthenticationOptions>>()
            .Value
            .ShouldNotBeNull();

    private static void ValidateOptionsOnStart(IConfiguration configuration, string environmentName)
        => CreateServiceProvider(configuration, environmentName)
            .GetRequiredService<IStartupValidator>()
            .Validate();

    private static ServiceProvider CreateServiceProvider(IConfiguration configuration, string environmentName) {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environmentName));
        services.AddOptions<EventStoreAuthenticationOptions>()
            .Bind(configuration.GetSection(AuthenticationSectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EventStoreAuthenticationOptions>, ValidateEventStoreAuthenticationOptions>();
        services.AddSingleton<IValidateOptions<EventStoreAuthenticationOptions>, ValidateTenantProductionAuthenticationOptions>();
        return services.BuildServiceProvider();
    }

    private static void WithEnvironmentVariables(IReadOnlyDictionary<string, string?> variables, Action action) {
        Dictionary<string, string?> originalValues = variables.ToDictionary(
            static pair => pair.Key,
            static pair => Environment.GetEnvironmentVariable(pair.Key));

        try {
            foreach (KeyValuePair<string, string?> variable in variables) {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }

            action();
        }
        finally {
            foreach (KeyValuePair<string, string?> variable in originalValues) {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Hexalith.Tenants.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
