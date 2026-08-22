namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Contains the immutable result of one high-impact action evaluation.
/// </summary>
/// <param name="TenantId">Literal tenant identifier.</param>
/// <param name="Action">Evaluated action.</param>
/// <param name="Stage">Evaluated stage.</param>
/// <param name="IsEligible">Whether the stage may proceed.</param>
/// <param name="UnavailableReason">Canonical evidence blocker.</param>
/// <param name="SafeMessageKey">Whole-string localized result key.</param>
/// <param name="RecoveryKey">Whole-string localized recovery key.</param>
/// <param name="DomainOutcome">Safe expected domain outcome, orthogonal to evidence blockers.</param>
/// <param name="RequiresFriction">Whether aging or refreshing evidence requires visible friction.</param>
public sealed record TenantHighImpactActionAvailability(
    string TenantId,
    TenantHighImpactAction Action,
    TenantHighImpactEvaluationStage Stage,
    bool IsEligible,
    TenantHighImpactUnavailableReason UnavailableReason,
    string SafeMessageKey,
    string RecoveryKey,
    TenantHighImpactDomainOutcome DomainOutcome,
    bool RequiresFriction);
