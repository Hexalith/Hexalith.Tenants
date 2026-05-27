using System.Text.Json.Serialization;

namespace Hexalith.Tenants.Contracts.Enums;

/// <summary>
/// Tenant lifecycle status. <see cref="Unknown"/> (ordinal 0) is the non-active sentinel:
/// an absent or unrecognized status deserializes here rather than defaulting to <see cref="Active"/>.
/// Serialized by name so consuming services never treat a missing status as an active tenant (TEN-2).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TenantStatus>))]
public enum TenantStatus {
    Unknown = 0,
    Active,
    Disabled,
}
