namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record TenantCommandTrackingHandle(
    string MessageId,
    string CorrelationId,
    string? AggregateId = null)
{
    /// <summary>Gets the internal expected domain when an aggregate name cannot identify command scope.</summary>
    internal string? ExpectedDomain { get; init; }
}
