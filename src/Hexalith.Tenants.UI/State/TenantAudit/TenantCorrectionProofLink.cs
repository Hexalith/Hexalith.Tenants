namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>The original and corrective audit receipt references associated by a correction flow.</summary>
public sealed record TenantCorrectionProofLink(
    string OriginalAuditReference,
    string CorrectiveAuditReference,
    DateTimeOffset OriginalTimestamp,
    DateTimeOffset CorrectiveTimestamp,
    string Narrative);
