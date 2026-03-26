using System.Text.Json;

using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.DomainProcessing;
using Hexalith.Tenants.Contracts.Commands;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.DomainProcessing;

public class DomainServiceRequestHandlerTests {
    [Fact]
    public async Task ProcessAsync_WhenFirstProcessorHasMismatchedState_UsesNextProcessor() {
        var first = new FakeDomainProcessor(
            _ => throw new InvalidOperationException(
                "Unable to rehydrate aggregate 'TenantState' from event type 'GlobalAdministratorSet'. No matching Apply method found on state 'TenantState'."));
        var second = new FakeDomainProcessor(_ => Task.FromResult(DomainResult.NoOp()));
        var handler = new DomainServiceRequestHandler([first, second], NullLogger<DomainServiceRequestHandler>.Instance);

        DomainServiceWireResult result = await handler.ProcessAsync(CreateRequest());

        result.IsRejection.ShouldBeFalse();
        result.Events.ShouldBeEmpty();
        first.CallCount.ShouldBe(1);
        second.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessAsync_WhenRehydrationFailsForMalformedHistory_DoesNotTreatItAsMismatch() {
        var first = new FakeDomainProcessor(
            _ => throw new InvalidOperationException(
                "Unable to rehydrate aggregate state 'TenantState'. Historical event is missing required string property 'eventTypeName'."));
        var second = new FakeDomainProcessor(_ => Task.FromResult(DomainResult.NoOp()));
        var handler = new DomainServiceRequestHandler([first, second], NullLogger<DomainServiceRequestHandler>.Instance);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.ProcessAsync(CreateRequest()));

        ex.Message.ShouldContain("Historical event is missing required string property 'eventTypeName'.");
        first.CallCount.ShouldBe(1);
        second.CallCount.ShouldBe(0);
    }

    private static DomainServiceRequest CreateRequest() {
        var command = new CreateTenant("acme", "Acme Corp", null);
        var envelope = new CommandEnvelope(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "tenant-root",
            "tenants",
            "acme",
            typeof(CreateTenant).FullName!,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            "user-1",
            null);

        return new DomainServiceRequest(envelope, null);
    }

    private sealed class FakeDomainProcessor(Func<DomainServiceRequest, Task<DomainResult>> handler) : IDomainProcessor {
        public int CallCount { get; private set; }

        public Task<DomainResult> ProcessAsync(CommandEnvelope command, object? currentState) {
            CallCount++;
            return handler(new DomainServiceRequest(command, currentState));
        }
    }
}
