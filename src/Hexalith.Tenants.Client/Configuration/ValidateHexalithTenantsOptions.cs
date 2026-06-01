using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Client.Configuration;

internal sealed class ValidateHexalithTenantsOptions : IValidateOptions<HexalithTenantsOptions> {
    public ValidateOptionsResult Validate(string? name, HexalithTenantsOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];
        AddRequiredFailure(failures, nameof(HexalithTenantsOptions.PubSubName), options.PubSubName);
        AddRequiredFailure(failures, nameof(HexalithTenantsOptions.TopicName), options.TopicName);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddRequiredFailure(List<string> failures, string optionName, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) {
            return;
        }

        failures.Add($"{nameof(HexalithTenantsOptions)}.{optionName} must be configured with a non-empty value.");
    }
}
