using System.Text.Json.Serialization;

namespace Hexalith.Tenants.Contracts.Events;

/// <summary>Records the creation of a tenant.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Name">The tenant display name.</param>
/// <param name="Description">The optional description.</param>
/// <param name="CreatedAt">When the tenant was created.</param>
/// <param name="IsSynthetic">Whether this is an environment's synthetic tenant. Historical events leave it unset.</param>
[method: JsonConstructor]
public record TenantCreated(
    string TenantId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsSynthetic = null) : IEventPayload
{
    /// <summary>Records the creation of a regular tenant using the original event signature.</summary>
    /// <param name="TenantId">The tenant identifier.</param>
    /// <param name="Name">The tenant display name.</param>
    /// <param name="Description">The optional description.</param>
    /// <param name="CreatedAt">When the tenant was created.</param>
    public TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt)
        : this(TenantId, Name, Description, CreatedAt, IsSynthetic: null)
    {
    }

    /// <summary>Reads the original event fields.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="name">The tenant display name.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="createdAt">When the tenant was created.</param>
    public void Deconstruct(out string tenantId, out string name, out string? description, out DateTimeOffset createdAt)
        => (tenantId, name, description, createdAt) = (TenantId, Name, Description, CreatedAt);
}
