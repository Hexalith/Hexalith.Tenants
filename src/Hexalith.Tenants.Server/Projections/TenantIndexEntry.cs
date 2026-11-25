using System.Text.Json.Serialization;

using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>Summarizes the tenant state needed by tenant list queries.</summary>
/// <param name="Name">The tenant display name.</param>
/// <param name="Status">The tenant lifecycle status.</param>
public sealed record TenantIndexEntry(string Name, TenantStatus Status)
{
    /// <summary>Gets the creation-time marker for synthetic tenants.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSynthetic { get; init; }
}
