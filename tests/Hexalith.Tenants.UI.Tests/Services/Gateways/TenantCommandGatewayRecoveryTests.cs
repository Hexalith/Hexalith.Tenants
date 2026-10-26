using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed partial class TenantCommandGatewayTests
{
    [Theory]
    [InlineData("exact", true)]
    [InlineData("message", false)]
    [InlineData("tenant", false)]
    [InlineData("domain", false)]
    [InlineData("aggregate", false)]
    [InlineData("legacy", false)]
    [InlineData("correlation", false)]
    [InlineData("status", false)]
    public async Task RecoveryRequiresCompleteExactScopeBeforeRevealingCorrelation(string variant, bool valid)
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        string body = JsonSerializer.Serialize(new
        {
            MessageId = variant is "legacy" ? null : variant is "message" ? "other-message" : messageId,
            TenantId = variant is "legacy" ? null : variant is "tenant" ? "other" : "system",
            Domain = variant is "legacy" ? null : variant is "domain" ? "tenants" : "global-administrators",
            AggregateId = variant is "aggregate" ? "other" : "global-administrators",
            CorrelationId = variant is "correlation" ? "" : "original-correlation",
            Status = "Completed",
            StatusCode = variant is "status" ? 0 : (int)CommandStatus.Completed,
            Timestamp = DateTimeOffset.UtcNow,
            EventCount = 1,
            CommittedEventSequence = 12,
        });
        TenantCommandGateway gateway = StatusGateway(new StatusHandler(body));
        TenantCommandStatusResult result = await gateway.RecoverGlobalAdministratorStatusAsync(messageId);
        result.RecoveredCorrelationId.ShouldBe(valid ? "original-correlation" : null);
        result.HasVerifiedCommandIdentity.ShouldBe(valid);
        result.CommittedEventSequence.ShouldBe(valid ? 12L : null);
        JsonSerializer.Serialize(result).ShouldNotContain("original-correlation");
        // Ordinary tracking cannot adopt a different correlation, including a correctly scoped response.
        TenantCommandStatusResult ordinary = await gateway.GetStatusAsync(new(messageId, "wrong-correlation", "global-administrators"));
        ordinary.HasVerifiedCommandIdentity.ShouldBeFalse();
        ordinary.RecoveredCorrelationId.ShouldBeNull();
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, 0, null)]
    [InlineData(CommandStatus.Completed, 0, null)]
    [InlineData(CommandStatus.Completed, 1, 12L)]
    public async Task RecoveryKeepsRejectionAndExactPositiveEventProofDistinct(CommandStatus status, int count, long? expectedSequence)
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var body = JsonSerializer.Serialize(new
        {
            MessageId = messageId,
            CorrelationId = "original-correlation",
            TenantId = "system",
            Domain = "global-administrators",
            AggregateId = "global-administrators",
            Status = status.ToString(),
            StatusCode = (int)status,
            EventCount = count,
            CommittedEventSequence = 12,
            RejectionEventType = "LastGlobalAdministrator",
            Timestamp = DateTimeOffset.UtcNow
        });
        TenantCommandStatusResult result = await StatusGateway(new StatusHandler(body)).RecoverGlobalAdministratorStatusAsync(messageId);
        result.Status.ShouldBe(status);
        result.RecoveredCorrelationId.ShouldBe("original-correlation");
        result.CommittedEventSequence.ShouldBe(expectedSequence);
        result.EventCount.ShouldBe(count);
    }
}
