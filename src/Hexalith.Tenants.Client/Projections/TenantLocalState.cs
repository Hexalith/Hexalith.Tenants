using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Per-tenant read model built from tenant event stream.
/// Consuming services use this to enforce access and react to tenant changes.
/// </summary>
public class TenantLocalState {
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantLocalState"/> class.
    /// </summary>
    public TenantLocalState() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantLocalState"/> class by copying an existing state.
    /// </summary>
    /// <param name="other">The state to copy.</param>
    public TenantLocalState(TenantLocalState other) {
        ArgumentNullException.ThrowIfNull(other);
        TenantId = other.TenantId;
        Name = other.Name;
        Description = other.Description;
        Status = other.Status;
        Members = new Dictionary<string, TenantRole>(other.Members, StringComparer.Ordinal);
        Configuration = new Dictionary<string, string>(other.Configuration, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tenant status. Defaults to <see cref="TenantStatus.Unknown"/> (the non-active
    /// sentinel) so a state built before <c>TenantCreated</c> is applied never reads as active (TEN-2).
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Unknown;

    /// <summary>
    /// Gets the tenant members mapped by user ID to their role.
    /// Keyed case-sensitively (Ordinal) per the Identifier Casing Contract in
    /// docs/production-auth-claim-contract.md — do not switch to OrdinalIgnoreCase (TEN-3).
    /// </summary>
    public Dictionary<string, TenantRole> Members { get; init; } = [];

    /// <summary>
    /// Gets the tenant configuration mapped by key to value.
    /// </summary>
    public Dictionary<string, string> Configuration { get; init; } = [];

    /// <summary>
    /// Creates a deep copy of the current state.
    /// </summary>
    /// <returns>A cloned state instance.</returns>
    public TenantLocalState Clone() => new(this);
}
