using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.Contracts.Commands;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.CommandPipeline;

public class GlobalAdminCommandEnvelopeTests {
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";

    [Fact]
    public void ToCommandEnvelope_strips_client_supplied_globalAdmin_extension_when_submit_command_is_not_global_admin() {
        SubmitCommand command = CreateCommand(
            isGlobalAdmin: false,
            extensions: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                [GlobalAdminExtensionKey] = "true",
                ["client-correlation"] = "safe-metadata",
            });

        CommandEnvelope envelope = command.ToCommandEnvelope();

        envelope.Extensions.ShouldNotBeNull();
        envelope.Extensions.ShouldNotContainKey(GlobalAdminExtensionKey);
        envelope.Extensions["client-correlation"].ShouldBe("safe-metadata");
    }

    [Fact]
    public void ToCommandEnvelope_adds_trusted_globalAdmin_extension_only_when_submit_command_is_global_admin() {
        SubmitCommand command = CreateCommand(
            isGlobalAdmin: true,
            extensions: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                [GlobalAdminExtensionKey] = "false",
                ["client-correlation"] = "safe-metadata",
            });

        CommandEnvelope envelope = command.ToCommandEnvelope();

        envelope.Extensions.ShouldNotBeNull();
        envelope.Extensions[GlobalAdminExtensionKey].ShouldBe("true");
        envelope.Extensions["client-correlation"].ShouldBe("safe-metadata");
    }

    private static SubmitCommand CreateCommand(bool isGlobalAdmin, Dictionary<string, string>? extensions) {
        var payload = new CreateTenant("acme", "Acme Corp", null);
        return new SubmitCommand(
            MessageId: UniqueIdHelper.GenerateSortableUniqueStringId(),
            Tenant: "system",
            Domain: "tenants",
            AggregateId: payload.TenantId,
            CommandType: nameof(CreateTenant),
            Payload: JsonSerializer.SerializeToUtf8Bytes(payload),
            CorrelationId: UniqueIdHelper.GenerateSortableUniqueStringId(),
            UserId: "actor-sub",
            Extensions: extensions,
            IsGlobalAdmin: isGlobalAdmin);
    }
}
