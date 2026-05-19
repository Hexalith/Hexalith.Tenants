using Hexalith.EventStore.Authentication;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Configuration;

internal sealed class ValidateTenantProductionAuthenticationOptions(IHostEnvironment environment) : IValidateOptions<EventStoreAuthenticationOptions> {
    private const string AuthenticationSectionName = "Authentication:JwtBearer";

    public ValidateOptionsResult Validate(string? name, EventStoreAuthenticationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        if (!environment.IsProduction()) {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];

        ValidateAuthority(options.Authority, failures);
        ValidateRequiredText(options.Issuer, $"{AuthenticationSectionName}:Issuer", failures);
        ValidateRequiredText(options.Audience, $"{AuthenticationSectionName}:Audience", failures);
        ValidateSigningKey(options.SigningKey, failures);

        if (!options.RequireHttpsMetadata) {
            failures.Add($"{AuthenticationSectionName}:RequireHttpsMetadata must be true for production OIDC authentication.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateAuthority(string? authority, List<string> failures) {
        if (string.IsNullOrWhiteSpace(authority)) {
            failures.Add($"{AuthenticationSectionName}:Authority must be configured as an absolute HTTPS URI for production OIDC authentication.");
            return;
        }

        if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? authorityUri)
            || !string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) {
            failures.Add($"{AuthenticationSectionName}:Authority must be an absolute HTTPS URI for production OIDC authentication.");
        }
    }

    private static void ValidateRequiredText(string? value, string key, List<string> failures) {
        if (string.IsNullOrWhiteSpace(value)) {
            failures.Add($"{key} must be configured with a non-empty value.");
        }
    }

    private static void ValidateSigningKey(string? signingKey, List<string> failures) {
        if (signingKey is null or { Length: 0 }) {
            return;
        }

        failures.Add($"{AuthenticationSectionName}:SigningKey must be empty for production OIDC authentication; configure {AuthenticationSectionName}:Authority instead.");
    }
}
