using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Binds and semantically validates deployment-owned configuration read policy.
/// </summary>
/// <remarks>
/// Validation is independent of the caller, so the bound policy is cached until the underlying
/// configuration reloads. Without that cache a full reflection bind and two set rebuilds ran on every
/// tenant-detail read, degraded reauthorization, and command reauthorization.
/// </remarks>
internal sealed partial class TenantConfigurationReadPolicyProvider
{
    private const string PolicySectionPath = "Tenants:ConfigurationReadPolicy";

    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantConfigurationReadPolicyProvider>? _logger;
    private readonly object _gate = new();
    private TenantConfigurationValidatedPolicy? _cached;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantConfigurationReadPolicyProvider"/> class.
    /// </summary>
    /// <param name="configuration">Host configuration carrying the policy section.</param>
    /// <param name="logger">Optional logger for non-sensitive failure categories.</param>
    public TenantConfigurationReadPolicyProvider(
        IConfiguration configuration,
        ILogger<TenantConfigurationReadPolicyProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
        _logger = logger;
        _ = ChangeToken.OnChange(configuration.GetReloadToken, Invalidate);
    }

    /// <summary>
    /// Resolves current policy for a tenant and authenticated principal.
    /// </summary>
    /// <param name="tenantId">Literal requested tenant identifier.</param>
    /// <param name="principal">Current fail-closed principal evidence.</param>
    /// <returns>Validated policy or an unavailable result.</returns>
    public TenantConfigurationReadPolicyResolution Resolve(
        string tenantId,
        TenantConfigurationPrincipalEvidence principal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.State is TenantConfigurationPrincipalEvidenceState.Indeterminate
            || string.IsNullOrWhiteSpace(principal.Subject))
        {
            // Per-request and often routine (an unauthenticated read), so this stays at Debug while
            // deployment faults are reported once per configuration load at Warning.
            if (_logger is not null)
            {
                Log.PolicyUnavailableForPrincipal(
                    _logger,
                    TenantConfigurationPolicyFailure.IndeterminatePrincipal);
            }
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        TenantConfigurationValidatedPolicy policy = GetValidatedPolicy();
        if (!policy.IsValid)
        {
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        if (principal.State is TenantConfigurationPrincipalEvidenceState.GlobalAdministrator)
        {
            return TenantConfigurationReadPolicyResolution.Available(
                true,
                ["*"],
                policy.DisplaySafeKeys,
                principal.State,
                principal.Subject);
        }

        string[] prefixes = policy.Grants
            .Where(grant => string.Equals(grant.TenantId, tenantId, StringComparison.Ordinal)
                && string.Equals(grant.Subject, principal.Subject, StringComparison.Ordinal))
            .Select(static grant => grant.Prefix!)
            .OrderBy(static prefix => prefix, StringComparer.Ordinal)
            .ToArray();
        return TenantConfigurationReadPolicyResolution.Available(
            false,
            prefixes,
            policy.DisplaySafeKeys,
            principal.State,
            principal.Subject);
    }

    private static bool HasScalarCollection(IConfigurationSection section, string childName)
    {
        IConfigurationSection child = section.GetSection(childName);

        // A non-empty value on a collection-shaped member is a scalar. An *empty* value cannot be
        // classified here: `[]`, `""` and an emptied environment override are one observable state
        // (Value == "" with no element children), and the empty-array form is the valid-empty
        // repository default, so failing closed on it would take the shipped appsettings.json dark.
        //
        // The residual is bounded, and deliberately accepted rather than traded for a redundant
        // cardinality discriminator. An empty declaration can only withhold approval, never grant it,
        // and an emptied environment override cannot shorten a declared list because the declaring
        // provider's element children still win. The one override shape that does reach the bound
        // list arrives as a blank element, which TryValidate rejects. All three are pinned by tests.
        return !string.IsNullOrEmpty(child.Value);
    }

    private static bool TryValidate(TenantConfigurationReadPolicyOptions options)
    {
        if (options.PrefixGrants is null || options.DisplaySafe is null)
        {
            return false;
        }

        HashSet<string> grants = new(StringComparer.Ordinal);
        foreach (TenantConfigurationPrefixGrantOptions grant in options.PrefixGrants)
        {
            if (grant is null
                || string.IsNullOrWhiteSpace(grant.TenantId)
                || string.IsNullOrWhiteSpace(grant.Subject)
                || !IsValidPrefix(grant.Prefix))
            {
                return false;
            }

            string identity = string.Concat(grant.TenantId, "\0", grant.Subject, "\0", grant.Prefix);
            if (!grants.Add(identity))
            {
                return false;
            }
        }

        HashSet<string> safeKeys = new(StringComparer.Ordinal);
        foreach (string safeKey in options.DisplaySafe)
        {
            if (string.IsNullOrWhiteSpace(safeKey) || !safeKeys.Add(safeKey))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidPrefix(string? prefix)
        => !string.IsNullOrWhiteSpace(prefix)
        && !prefix.EndsWith(".", StringComparison.Ordinal)
        && !prefix.Any(char.IsWhiteSpace)
        && !string.Equals(prefix, "*", StringComparison.Ordinal);

    private void Invalidate()
    {
        lock (_gate)
        {
            _cached = null;
        }
    }

    private TenantConfigurationValidatedPolicy GetValidatedPolicy()
    {
        lock (_gate)
        {
            _cached ??= BuildValidatedPolicy();
            return _cached;
        }
    }

    private TenantConfigurationValidatedPolicy BuildValidatedPolicy()
    {
        TenantConfigurationValidatedPolicy result = Build();
        if (!result.IsValid)
        {
            // Once per configuration load rather than once per request: a broken deployment should be
            // visible to an operator without flooding the log. The category names the fault class and
            // never the tenant, subject, prefix, key, or value that produced it.
            if (_logger is not null)
            {
                Log.PolicyUnavailableForDeployment(_logger, result.Failure);
            }
        }

        return result;

        TenantConfigurationValidatedPolicy Build()
        {
            IConfigurationSection section = _configuration.GetSection(PolicySectionPath);
            if (!section.Exists())
            {
                return TenantConfigurationValidatedPolicy.Invalid(TenantConfigurationPolicyFailure.MissingSection);
            }

            if (HasScalarCollection(section, nameof(TenantConfigurationReadPolicyOptions.PrefixGrants))
                || HasScalarCollection(section, nameof(TenantConfigurationReadPolicyOptions.DisplaySafe)))
            {
                return TenantConfigurationValidatedPolicy.Invalid(TenantConfigurationPolicyFailure.ScalarCollection);
            }

            TenantConfigurationReadPolicyOptions? options;
            try
            {
                options = section.Get<TenantConfigurationReadPolicyOptions>();
            }
            catch (Exception)
            {
                // Binder failures are deployment-policy defects. Map every safe bind failure to Unavailable
                // rather than letting non-InvalidOperationException shapes escape the composition boundary.
                return TenantConfigurationValidatedPolicy.Invalid(TenantConfigurationPolicyFailure.UnbindableSection);
            }

            return options is null || !TryValidate(options)
                ? TenantConfigurationValidatedPolicy.Invalid(TenantConfigurationPolicyFailure.InvalidDeclaration)
                : TenantConfigurationValidatedPolicy.Valid(
                    options.PrefixGrants.ToArray(),
                    options.DisplaySafe.ToArray());
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2100,
            Level = LogLevel.Debug,
            Message = "Tenant configuration read policy is unavailable. Category: {Failure}.")]
        public static partial void PolicyUnavailableForPrincipal(
            ILogger logger,
            TenantConfigurationPolicyFailure failure);

        [LoggerMessage(
            EventId = 2101,
            Level = LogLevel.Warning,
            Message = "Tenant configuration read policy is unavailable. Category: {Failure}.")]
        public static partial void PolicyUnavailableForDeployment(
            ILogger logger,
            TenantConfigurationPolicyFailure failure);
    }
}
