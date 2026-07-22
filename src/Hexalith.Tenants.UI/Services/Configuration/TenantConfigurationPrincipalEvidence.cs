namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Contains non-sensitive, fail-closed evidence derived from one authenticated identity.
/// </summary>
internal sealed class TenantConfigurationPrincipalEvidence
{
    private TenantConfigurationPrincipalEvidence(
        TenantConfigurationPrincipalEvidenceState state,
        string? subject)
    {
        State = state;
        Subject = subject;
    }

    /// <summary>Gets the evidence outcome.</summary>
    public TenantConfigurationPrincipalEvidenceState State { get; }

    /// <summary>Gets the literal authenticated subject when evidence is determinate.</summary>
    public string? Subject { get; }

    /// <summary>Creates indeterminate evidence.</summary>
    /// <returns>Indeterminate evidence.</returns>
    public static TenantConfigurationPrincipalEvidence Indeterminate()
        => new(TenantConfigurationPrincipalEvidenceState.Indeterminate, null);

    /// <summary>Creates proven non-administrator evidence.</summary>
    /// <param name="subject">Literal authenticated subject.</param>
    /// <returns>Non-administrator evidence.</returns>
    public static TenantConfigurationPrincipalEvidence NonAdministrator(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return new(TenantConfigurationPrincipalEvidenceState.NonAdministrator, subject);
    }

    /// <summary>Creates proven global-administrator evidence.</summary>
    /// <param name="subject">Literal authenticated subject.</param>
    /// <returns>Global-administrator evidence.</returns>
    public static TenantConfigurationPrincipalEvidence GlobalAdministrator(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return new(TenantConfigurationPrincipalEvidenceState.GlobalAdministrator, subject);
    }
}
