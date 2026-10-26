using System.Security.Cryptography;
using System.Text.Json;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>Fingerprints the committed semantics and owner watermarks of the shared tenant index.</summary>
public static class TenantIndexProjectionVersion
{
    /// <summary>Creates opaque producer authority only when every represented owner has a positive watermark.</summary>
    /// <param name="model">The index after the retry transform has merged the current owner.</param>
    /// <returns>A deterministic validator, or no authority for incomplete legacy coverage.</returns>
    public static string? Create(TenantIndexReadModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Tenants is null || model.UserTenants is null
            || model.AggregateSequences is not { Count: > 0 } sequences
            || sequences.Any(owner => owner.Value <= 0)
            || model.Tenants.Values.Any(tenant => tenant is null)
            || model.Tenants.Keys.Any(owner => !sequences.ContainsKey(owner))
            || model.UserTenants.Values.Any(tenants => tenants is null
                || tenants.Keys.Any(owner => !sequences.ContainsKey(owner))))
        {
            return null;
        }

        using MemoryStream canonical = new();
        using (Utf8JsonWriter writer = new(canonical))
        {
            writer.WriteStartArray();
            writer.WriteStartArray();
            foreach ((string owner, TenantIndexEntry tenant) in model.Tenants.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray();
                writer.WriteStringValue(owner);
                writer.WriteStringValue(tenant.Name);
                writer.WriteNumberValue((int)tenant.Status);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteStartArray();
            foreach ((string user, Dictionary<string, Contracts.Enums.TenantRole> tenants) in model.UserTenants.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray();
                writer.WriteStringValue(user);
                writer.WriteStartArray();
                foreach (var membership in tenants.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    writer.WriteStartArray();
                    writer.WriteStringValue(membership.Key);
                    writer.WriteNumberValue((int)membership.Value);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteStartArray();
            foreach ((string owner, long sequence) in sequences.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                writer.WriteStartArray();
                writer.WriteStringValue(owner);
                writer.WriteNumberValue(sequence);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndArray();
        }

        return "tenant-index:sha256:" + Convert.ToHexStringLower(SHA256.HashData(canonical.ToArray()));
    }
}
