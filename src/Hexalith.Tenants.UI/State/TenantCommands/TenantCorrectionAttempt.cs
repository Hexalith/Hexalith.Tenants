using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>One retained tenant correction and its aggregate admission.</summary>
internal sealed record TenantCorrectionAttempt(
    string TenantId,
    string MessageId,
    TenantCorrectionPreviewSnapshot Snapshot,
    TenantAggregateCommandLease Lease,
    DateTimeOffset StartedAtUtc);
