using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Api.Services;

internal static class ProductionApiAuthentication
{
    private const string DomainSection = "Authentication:JwtBearer";
    private const string ApiSection = "EventStore:Authentication";

    internal static void Configure(JwtBearerOptions bearer, IConfiguration configuration, IHostEnvironment environment)
    {
        IConfigurationSection section = configuration.GetSection(DomainSection);
        if (!section.Exists())
        {
            section = configuration.GetSection(ApiSection);
        }

        JwtBearerAuthenticationOptions contract = section.Get<JwtBearerAuthenticationOptions>() ?? new();
        if (section.Path == ApiSection && string.IsNullOrWhiteSpace(contract.Authority))
        {
            contract = contract with { Authority = configuration["Oidc:Authority"] };
        }

        List<string> failures = [];
        foreach (string key in new[] { DomainSection + ":SigningKey", ApiSection + ":SigningKey", "Jwt:SigningKey" })
        {
            if (!string.IsNullOrEmpty(configuration[key]))
            {
                failures.Add($"{key} must be empty in Production.");
            }
        }

        if (string.IsNullOrWhiteSpace(contract.Audience))
        {
            failures.Add($"{section.Path}:Audience must be configured in Production.");
        }

        ValidateOptionsResult validation = JwtBearerAuthenticationContract.Validate(contract, environment, section.Path);
        if (validation.Failed)
        {
            failures.AddRange(validation.Failures);
        }

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerAuthenticationOptions), failures);
        }

        JwtBearerAuthenticationContract.Configure(bearer, contract);
    }
}
