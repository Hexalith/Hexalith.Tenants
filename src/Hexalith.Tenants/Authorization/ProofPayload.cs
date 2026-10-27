namespace Hexalith.Tenants.Authorization;

/// <summary>Preserves the version-one bound owner-source proof payload.</summary>
/// <param name="Version">The proof format version.</param>
/// <param name="Caller">The workload that signs the proof.</param>
/// <param name="Tenant">The exact source tenant.</param>
/// <param name="Domain">The exact source domain.</param>
/// <param name="AggregateId">The exact source aggregate.</param>
/// <param name="IssuedAt">The issuance time in Unix seconds.</param>
/// <param name="ExpiresAt">The expiry time in Unix seconds.</param>
/// <param name="Nonce">The independently generated replay nonce.</param>
internal sealed record ProofPayload(int Version, string Caller, string Tenant, string Domain, string AggregateId,
    long IssuedAt, long ExpiresAt, string Nonce);
