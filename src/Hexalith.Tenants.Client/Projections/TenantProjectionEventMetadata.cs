namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Bounded metadata for the last tenant event applied to a local projection.
/// </summary>
public record TenantProjectionEventMetadata(
    string LastMessageId,
    long LastSequenceNumber,
    DateTimeOffset LastUpdatedAt,
    string LastCorrelationId);
