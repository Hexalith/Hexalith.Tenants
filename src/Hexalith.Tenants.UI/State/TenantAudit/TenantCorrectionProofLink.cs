namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Bidirectional audit receipt association backed by attempt-specific evidence.</summary>
public sealed record TenantCorrectionProofLink(
    string OriginalAuditReference,
    string CorrectiveAuditReference,
    DateTimeOffset OriginalTimestamp,
    DateTimeOffset CorrectiveTimestamp,
    string Narrative);
