namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents one configuration entry that passed both authorization and display-safety policy.
/// </summary>
public sealed class TenantConfigurationSafeRow
{
    internal TenantConfigurationSafeRow(string @namespace, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        Namespace = @namespace;
        Key = key;
        Value = value;
    }

    /// <summary>Gets the longest matching authorized namespace.</summary>
    public string Namespace { get; }

    /// <summary>Gets the literal full key.</summary>
    public string Key { get; }

    /// <summary>Gets the positively approved literal value.</summary>
    public string Value { get; }
}
