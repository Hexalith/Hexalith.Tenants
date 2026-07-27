namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Holds the deployment policy after binding and semantic validation.
/// </summary>
/// <remarks>
/// Validation is independent of the caller, so this result is cached until configuration reloads and
/// then narrowed per tenant and principal. Keeping the two stages apart is what lets the expensive
/// bind happen once instead of on every tenant-detail read.
/// </remarks>
internal sealed class TenantConfigurationValidatedPolicy
{
    private TenantConfigurationValidatedPolicy(
        TenantConfigurationPolicyFailure failure,
        TenantConfigurationPrefixGrantOptions[] grants,
        string[] displaySafeKeys)
    {
        Failure = failure;
        Grants = grants;
        DisplaySafeKeys = displaySafeKeys;
    }

    /// <summary>Gets the validation failure category, or <see cref="TenantConfigurationPolicyFailure.None"/>.</summary>
    public TenantConfigurationPolicyFailure Failure { get; }

    /// <summary>Gets the validated prefix grants.</summary>
    public TenantConfigurationPrefixGrantOptions[] Grants { get; }

    /// <summary>Gets the validated exact display-safe keys.</summary>
    public string[] DisplaySafeKeys { get; }

    /// <summary>Gets whether the policy is usable.</summary>
    public bool IsValid => Failure is TenantConfigurationPolicyFailure.None;

    /// <summary>Creates an invalid policy carrying only its failure category.</summary>
    /// <param name="failure">Why validation failed.</param>
    /// <returns>Invalid policy.</returns>
    public static TenantConfigurationValidatedPolicy Invalid(TenantConfigurationPolicyFailure failure)
        => new(failure, [], []);

    /// <summary>Creates a validated policy.</summary>
    /// <param name="grants">Validated prefix grants.</param>
    /// <param name="displaySafeKeys">Validated exact display-safe keys.</param>
    /// <returns>Valid policy.</returns>
    public static TenantConfigurationValidatedPolicy Valid(
        TenantConfigurationPrefixGrantOptions[] grants,
        string[] displaySafeKeys)
        => new(TenantConfigurationPolicyFailure.None, grants, displaySafeKeys);
}
