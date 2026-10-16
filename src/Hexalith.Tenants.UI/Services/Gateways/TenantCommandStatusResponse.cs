namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Backend command status evidence validated by the command gateway.</summary>
/// <param name="CorrelationId">Correlation that must exactly match the tracked command.</param>
/// <param name="Status">Case-sensitive command status name or numeric enum representation to validate.</param>
/// <param name="StatusCode">Optional numeric status, which must agree with the parsed status when supplied.</param>
/// <param name="Timestamp">Backend observation timestamp; it does not prove command completion.</param>
/// <param name="AggregateId">Optional aggregate identity to compare with the expected command scope.</param>
/// <param name="EventCount">Optional event count; event evidence also requires verified command identity.</param>
/// <param name="RejectionEventType">Backend rejection identifier mapped only to fixed safe code and message choices.</param>
/// <param name="FailureReason">Untrusted backend detail matched only against approved complete machine codes.</param>
/// <param name="TimeoutDuration">Optional backend timeout metadata, never visible copy or command proof.</param>
/// <param name="MessageId">Optional exact command identity; its absence on legacy records leaves identity unverified.</param>
/// <param name="TenantId">Optional tenant scope; committed proof requires an exact expected scope match.</param>
/// <param name="Domain">Optional domain scope; committed proof requires an exact expected scope match.</param>
/// <param name="CommittedEventSequence">Optional commit sequence, retained only for a matching completed command with positive, consistent event evidence and verified aggregate scope.</param>
internal sealed record TenantCommandStatusResponse(
    string CorrelationId,
    string Status,
    int? StatusCode,
    DateTimeOffset Timestamp,
    string? AggregateId,
    int? EventCount,
    string? RejectionEventType,
    string? FailureReason,
    string? TimeoutDuration,
    string? MessageId = null,
    string? TenantId = null,
    string? Domain = null,
    long? CommittedEventSequence = null);
