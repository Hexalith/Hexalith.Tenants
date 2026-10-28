using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Authorization;
using System.Diagnostics;
using System.Text.Json;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Server.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Authorization;

public sealed class TenantsGlobalAdministratorAuthorityTests
{
    [Theory]
    [InlineData(nameof(GlobalAdministratorAlreadyExistsRejection), "{}")]
    [InlineData(nameof(GlobalAdministratorNotFoundRejection), "{}")]
    [InlineData(nameof(LastGlobalAdministratorRejection), "{}")]
    [InlineData(nameof(GlobalAdministratorAlreadyExistsRejection), "{\"userId\":\" \"}")]
    [InlineData(nameof(InsufficientPermissionsRejection), "{\"commandName\":\"SetGlobalAdministrator\"}")]
    [InlineData(nameof(InsufficientPermissionsRejection), "{\"actorUserId\":\"admin\"}")]
    [InlineData(nameof(InsufficientPermissionsRejection), "{\"actorUserId\":\"admin\",\"commandName\":\" \"}")]
    public async Task MalformedKnownRejectionCannotAuthenticateThePrefix(string type, string fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var source = GlobalAdministratorSourceFixture.Source(appended: [new GlobalAdminAlreadyBootstrappedRejection("system")]);
        byte[] payload = System.Text.Encoding.UTF8.GetBytes(fields.Insert(1, "\"tenantId\":\"system\"" + (fields.Length > 2 ? "," : "")));
        source = source with
        {
            Events = [source.Events[0], source.Events[1] with
        {
            EventTypeName = typeof(GlobalAdminAlreadyBootstrappedRejection).Namespace + "." + type,
            Payload = payload,
        }]
        };
        var authority = GlobalAdministratorSourceFixture.Authority(source, out _, out _);
        await Should.ThrowAsync<TenantsGlobalAdministratorAuthorityUnavailableException>(() =>
            authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifiedNativeMembershipIsSharedOnlyWithinTheRequest()
    {
        var source = GlobalAdministratorSourceFixture.Source();
        var authority = GlobalAdministratorSourceFixture.Authority(source, out var reader, out _);
        (await authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await authority.IsCurrentGlobalAdministratorAsync("other", TestContext.Current.CancellationToken)).ShouldBeFalse();
        await reader.Received(1).ReadAsync(source.Identity, Arg.Any<CancellationToken>());
        var next = GlobalAdministratorSourceFixture.Authority(GlobalAdministratorSourceFixture.Source(revoked: true), out _, out _);
        (await next.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("gap")]
    [InlineData("malformed")]
    [InlineData("wrong-tenant")]
    [InlineData("missing-user")]
    [InlineData("legacy")]
    [InlineData("over-bound")]
    [InlineData("replaced")]
    [InlineData("erased")]
    [InlineData("unavailable")]
    [InlineData("duplicate-alias")]
    [InlineData("metadata-version")]
    [InlineData("payload-version")]
    [InlineData("payload-bound")]
    public async Task IncompleteOrChangingEvidenceNeverGrants(string change)
    {
        var source = GlobalAdministratorSourceFixture.Source();
        var item = source.Events[0];
        var supplied = change switch
        {
            "unknown" => source with { Events = [item with { EventTypeName = "Unknown" }] },
            "gap" => source with { Events = [item with { SequenceNumber = 2 }] },
            "malformed" => source with { Events = [item with { Payload = "{"u8.ToArray() }] },
            "wrong-tenant" => source with { Events = [item with { Payload = """{"tenantId":"other","userId":"admin"}"""u8.ToArray() }] },
            "missing-user" => source with { Events = [item with { Payload = """{"tenantId":"system"}"""u8.ToArray() }] },
            "duplicate-alias" => source with { Events = [item with { Payload = """{"tenantId":"system","TenantId":"other","userId":"admin"}"""u8.ToArray() }] },
            "metadata-version" => source with { Events = [item with { MetadataVersion = 2 }] },
            "payload-version" => source with { Events = [item with { PayloadVersion = 2 }] },
            "payload-bound" => source with { Events = [item with { Payload = new byte[16 * 1024 * 1024 + 1] }] },
            "legacy" => source with { NativeProof = null },
            "over-bound" => source with { Head = 10_001 },
            _ => source,
        };
        var authority = GlobalAdministratorSourceFixture.Authority(supplied, out var reader, out var metadata);
        if (change == "unavailable") reader.ReadAsync(Arg.Any<AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(null, "source-unavailable"));
        if (change is "replaced" or "erased") metadata.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(
            new NativeSourceObservation(change == "erased" ? source.NativeProof! with { Erased = true } : source.NativeProof! with { CumulativeDigest = new string('f', 64) }, source.ObservedAt));
        await Should.ThrowAsync<TenantsGlobalAdministratorAuthorityUnavailableException>(() =>
            authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task KnownRejectionsReplayWithoutChangingMembership()
    {
        var source = GlobalAdministratorSourceFixture.Source(appended: [
            new GlobalAdminAlreadyBootstrappedRejection("system"),
            new GlobalAdministratorAlreadyExistsRejection("system", "admin"),
            new GlobalAdministratorNotFoundRejection("system", "other"),
            new LastGlobalAdministratorRejection("system", "admin"),
            new InsufficientPermissionsRejection("system", "other", null, "command")]);
        var authority = GlobalAdministratorSourceFixture.Authority(source, out _, out _);
        (await authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task PreCancelledAuthorityNeverExecutesDependencies()
    {
        var authority = GlobalAdministratorSourceFixture.Authority(GlobalAdministratorSourceFixture.Source(), out var reader, out _);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => authority.IsCurrentGlobalAdministratorAsync("admin", cancellation.Token));
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(default!);
    }

    [Fact]
    public async Task RepresentativeBoundedReplayCostIsRecorded()
    {
        var source = GlobalAdministratorSourceFixture.Source(pairs: 4999, revoked: true);
        source.Events.Count.ShouldBe(10_000);
        var authority = GlobalAdministratorSourceFixture.Authority(source, out _, out _);
        var timer = Stopwatch.StartNew();
        (await authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken)).ShouldBeFalse();
        TestContext.Current.TestOutputHelper!.WriteLine($"Folded {source.Events.Count} events ({source.Events.Sum(item => item.Payload.Length)} bytes) in {timer.Elapsed.TotalMilliseconds:F2} ms; authenticated transport excluded.");
    }
}
