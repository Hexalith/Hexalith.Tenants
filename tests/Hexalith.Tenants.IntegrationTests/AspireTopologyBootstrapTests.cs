using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Models;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Checks cold-bootstrap authority and lifecycle wiring without starting Dapr or an AppHost.</summary>
[Collection("BootstrapReadiness")]
public sealed class AspireTopologyBootstrapTests
{
    [Theory]
    [InlineData(typeof(AspireTopologyFixture))]
    [InlineData(typeof(AuthenticatedTenantsTopologyFixture))]
    public void XUnitInitializationDispatchesToTheOwningFixture(Type fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        System.Reflection.InterfaceMapping mapping = fixture.GetInterfaceMap(typeof(IAsyncLifetime));
        int initialize = Array.FindIndex(mapping.InterfaceMethods, method => method.Name == nameof(IAsyncLifetime.InitializeAsync));
        mapping.TargetMethods[initialize].DeclaringType.ShouldBe(fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartupOverridesAreScopedAndRestoredOnSuccessOrFailure(bool fail)
    {
        string[] keys = ["Tenants__BootstrapGlobalAdminEnabled", "EventStore__ProjectionDispatch__CatalogRefreshInterval"];
        string?[] original = keys.Select(Environment.GetEnvironmentVariable).ToArray();
        try
        {
            Environment.SetEnvironmentVariable(keys[0], "original-bootstrap");
            Environment.SetEnvironmentVariable(keys[1], "original-interval");
            async ValueTask Initialize()
            {
                Environment.GetEnvironmentVariable(keys[0]).ShouldBe("false");
                Environment.GetEnvironmentVariable(keys[1]).ShouldBe("00:00:01");
                await Task.Yield();
                if (fail) throw new InvalidOperationException("startup-control");
            }

            if (fail) await Should.ThrowAsync<InvalidOperationException>(async () => await AspireTopologyFixture.WithBootstrapStartupSettingsAsync(Initialize));
            else await AspireTopologyFixture.WithBootstrapStartupSettingsAsync(Initialize);

            Environment.GetEnvironmentVariable(keys[0]).ShouldBe("original-bootstrap");
            Environment.GetEnvironmentVariable(keys[1]).ShouldBe("original-interval");
        }
        finally
        {
            for (int index = 0; index < keys.Length; index++) Environment.SetEnvironmentVariable(keys[index], original[index]);
        }
    }

    [Theory]
    [InlineData("{\"rejectionType\":\"Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdminAlreadyBootstrappedRejection\",\"reasonCode\":\"global-admin-already-bootstrapped-rejection\"}", "already-bootstrapped")]
    [InlineData("{\"reasonCode\":\"insufficient-permissions-rejection\",\"title\":\"Bearer credential-sentinel https://private.invalid/signed-url-sentinel\"}", "insufficient-permissions")]
    [InlineData("{\"rejectionType\":\"InsufficientPermissionsRejection\",\"reasonCode\":\"global-admin-already-bootstrapped-rejection\"}", "unclassified")]
    [InlineData("{\"rejectionType\":\"credential-sentinel\",\"detail\":\"signed-url-sentinel\"}", "unclassified")]
    [InlineData("{invalid signed-url-sentinel", "unclassified")]
    public async Task BootstrapDiagnosticsEmitOnlyAllowlistedCategories(string body, string expected)
    {
        using var content = new StringContent(body);
        string reason = await AspireTopologyFixture.BootstrapFailureReasonAsync(content, CancellationToken.None);
        reason.ShouldBe(expected);
        reason.ShouldNotContain("credential-sentinel");
        reason.ShouldNotContain("signed-url-sentinel");
        reason.Length.ShouldBeLessThan(32);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedBootstrapDiagnosticsFailClosedEvenWhenAlreadyBuffered(bool buffered)
    {
        using var content = new StringContent("{\"reasonCode\":\"global-admin-already-bootstrapped-rejection\"}" + new string(' ', 16384));
        if (buffered) await content.LoadIntoBufferAsync();
        (await AspireTopologyFixture.BootstrapFailureReasonAsync(content, CancellationToken.None)).ShouldBe("unclassified");
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("missing-payload")]
    [InlineData("different-user")]
    [InlineData("missing-version")]
    [InlineData("opaque-version")]
    [InlineData("zero-version")]
    [InlineData("unknown-provenance")]
    [InlineData("unknown-lifecycle")]
    [InlineData("stale")]
    [InlineData("missing-freshness")]
    [InlineData("degraded")]
    [InlineData("not-modified")]
    [InlineData("missing-etag")]
    [InlineData("missing-served-at")]
    [InlineData("old-served-at")]
    [InlineData("future-served-at")]
    [InlineData("forbidden")]
    [InlineData("unavailable")]
    public async Task MissingOrUntrustedAuthorityCannotCompleteReadiness(string shape)
    {
        var response = Ready();
        response = shape switch
        {
            "empty" => response with { Payload = new([], null, false) },
            "missing-payload" => response with { Payload = null },
            "different-user" => response with { Payload = new([new("another-user")], null, false) },
            "missing-version" => response with { Metadata = response.Metadata with { ProjectionVersion = null } },
            "opaque-version" => response with { Metadata = response.Metadata with { ProjectionVersion = "store-etag-only" } },
            "zero-version" => response with { Metadata = response.Metadata with { ProjectionVersion = "tenant-sequence:0" } },
            "unknown-provenance" => response with { Metadata = response.Metadata with { Provenance = QueryResponseProvenance.Unknown } },
            "unknown-lifecycle" => response with { Metadata = response.Metadata with { Lifecycle = ProjectionLifecycleState.Unknown } },
            "stale" => response with { Metadata = response.Metadata with { IsStale = true } },
            "missing-freshness" => response with { Metadata = response.Metadata with { IsStale = null } },
            "degraded" => response with { Metadata = response.Metadata with { IsDegraded = true } },
            "not-modified" => response with { Metadata = response.Metadata with { IsNotModified = true } },
            "missing-etag" => response with { Metadata = response.Metadata with { ETag = null } },
            "missing-served-at" => response with { Metadata = response.Metadata with { ServedAt = null } },
            "old-served-at" => response with { Metadata = response.Metadata with { ServedAt = DateTimeOffset.UtcNow.AddMinutes(-10) } },
            "future-served-at" => response with { Metadata = response.Metadata with { ServedAt = DateTimeOffset.UtcNow.AddMinutes(10) } },
            "forbidden" => response with { StatusCode = 403 },
            "unavailable" => response with { FailureKind = TenantsRestQueryFailureKind.Unavailable },
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int reads = 0;

        await Should.ThrowAsync<OperationCanceledException>(() => AspireTopologyFixture.WaitForBootstrapAuthorityAsync((_, _) =>
        {
            reads++;
            if (reads == 2) cancellation.Cancel();
            return Task.FromResult(reads == 1 ? response : Ready());
        }, null, false, cancellation.Token));

        reads.ShouldBe(2);
    }

    [Fact]
    public async Task ReadinessWaitsForActualProjectedCallerAfterUnavailableAndEmptyReads()
    {
        var current = Ready();
        Queue<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> responses = new(
        [
            current with { FailureKind = TenantsRestQueryFailureKind.Forbidden, StatusCode = 403, Payload = null },
            current with { Payload = new([], null, false) },
            current,
        ]);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await AspireTopologyFixture.WaitForBootstrapAuthorityAsync((_, _) => Task.FromResult(responses.Dequeue()), null, false, cancellation.Token);

        responses.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(202)]
    [InlineData(409)]
    public async Task CompleteHttpPathNeverBuffersOversizedStreamedBootstrapBodies(int status)
    {
        using var stream = new BootstrapDiagnosticStream(Encoding.UTF8.GetBytes(
            "{\"reasonCode\":\"global-admin-already-bootstrapped-rejection\"}" + new string(' ', 65536)));
        using HttpClient client = Client((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        { Content = new StreamContent(stream) }));
        bool cleaned = false;
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            AspireTopologyFixture.CompleteBootstrapAsync(client, "credential-sentinel", ReadReadyAsync,
                () => { cleaned = true; return ValueTask.CompletedTask; }, false, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe(status == 202 ? "bootstrap-command-acceptance-invalid" : "bootstrap-command-http-409:unclassified");
        stream.BytesRead.ShouldBe(8193);
        cleaned.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadinessTraversesProtectedCursorsAndChecksTheActualCallerPageMetadata()
    {
        const string cursor = "opaque-protected-cursor+/=scope";
        List<string?> observed = [];
        int callerPages = 0;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await AspireTopologyFixture.WaitForBootstrapAuthorityAsync((current, _) =>
        {
            observed.Add(current);
            if (current is null) return Task.FromResult(Ready() with { Payload = new([new("other-admin")], cursor, true) });
            current.ShouldBe(cursor);
            callerPages++;
            var response = Ready();
            return Task.FromResult(callerPages == 1 ? response with { Metadata = response.Metadata with { IsStale = true } } : response);
        }, 1, false, cancellation.Token);

        observed.ShouldBe(new string?[] { null, cursor, null, cursor });
    }

    [Fact]
    public async Task RepeatedProtectedCursorCannotCreateAnUnboundedPageLoop()
    {
        List<string?> observed = [];
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.WaitForBootstrapAuthorityAsync((cursor, _) =>
        {
            observed.Add(cursor);
            return Task.FromResult(Ready() with { Payload = new([], "same-protected-cursor", true) });
        }, null, false, cancellation.Token));
        observed.ShouldBe(new string?[] { null, "same-protected-cursor" });
    }

    [Fact]
    public async Task CancellationIsObservedBetweenProtectedPages()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int reads = 0;
        await Should.ThrowAsync<OperationCanceledException>(() => AspireTopologyFixture.WaitForBootstrapAuthorityAsync((cursor, _) =>
        {
            reads++;
            if (cursor is null) return Task.FromResult(Ready() with { Payload = new([], "issued-cursor", true) });
            cancellation.Cancel();
            return Task.FromResult(Ready());
        }, null, false, cancellation.Token));
        reads.ShouldBe(2);
    }

    [Theory]
    [InlineData(408)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    [InlineData(-1)]
    [InlineData(-2)]
    public async Task TransientSubmissionRetriesPreserveTheCompleteCommandIdentity(int transient)
    {
        List<string> submissions = [];
        string? messageId = null;
        using HttpClient client = Client(async (request, cancellationToken) =>
        {
            request.Headers.Authorization?.Parameter.ShouldBe("credential-sentinel");
            if (request.Method == HttpMethod.Get) return Terminal(messageId!, "Completed", 1);
            string body = await request.Content!.ReadAsStringAsync(cancellationToken);
            submissions.Add(body);
            messageId = JsonDocument.Parse(body).RootElement.GetProperty("messageId").GetString();
            if (submissions.Count == 1)
            {
                if (transient == -1) throw new HttpRequestException(HttpRequestError.ConnectionError, PrivateSentinel);
                if (transient == -2) throw new TaskCanceledException(PrivateSentinel);
                return new HttpResponseMessage((HttpStatusCode)transient);
            }
            return Accepted(messageId!);
        });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await AspireTopologyFixture.CompleteBootstrapAsync(client, "credential-sentinel", ReadReadyAsync,
            UnexpectedCleanupAsync, false, cancellation.Token);
        submissions.Count.ShouldBe(2);
        submissions[1].ShouldBe(submissions[0]);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(409)]
    [InlineData(422)]
    public async Task PermissionSchemaAndUnknownConflictFailuresAreNotRetried(int status)
    {
        int submissions = 0;
        bool cleaned = false;
        using HttpClient client = Client((_, _) =>
        {
            submissions++;
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            { Content = JsonContent.Create(new { reasonCode = "insufficient-permissions-rejection", detail = PrivateSentinel }) });
        });
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", UnexpectedReadAsync, () => { cleaned = true; return ValueTask.CompletedTask; },
            false, TestContext.Current.CancellationToken));
        submissions.ShouldBe(1);
        cleaned.ShouldBeTrue();
        exception.Message.ShouldBe($"bootstrap-command-http-{status}:insufficient-permissions");
        AssertSafe(exception);
    }

    [Theory]
    [InlineData("Completed", 1L, null, true)]
    [InlineData("Completed", 0L, null, false)]
    [InlineData("Completed", null, null, false)]
    [InlineData("Rejected", null, "GlobalAdminAlreadyBootstrappedRejection", true)]
    [InlineData("Rejected", null, "InsufficientPermissionsRejection", false)]
    [InlineData("PublishFailed", 1L, null, false)]
    [InlineData("TimedOut", null, null, false)]
    public async Task ActualTerminalBootstrapOutcomeIsRequired(string status, long? sequence, string? rejection, bool succeeds)
    {
        bool cleaned = false;
        using HttpClient client = CommandClient(messageId => Terminal(messageId, status, sequence, rejection));
        Task Run() => AspireTopologyFixture.CompleteBootstrapAsync(client, "credential-sentinel", ReadReadyAsync,
            () => { cleaned = true; return ValueTask.CompletedTask; }, false, TestContext.Current.CancellationToken);
        if (succeeds) await Run();
        else
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(Run);
            exception.Message.ShouldBe("bootstrap-command-terminal-invalid");
            AssertSafe(exception);
        }
        cleaned.ShouldBe(!succeeds);
    }

    [Theory]
    [InlineData("correlation")]
    [InlineData("message")]
    [InlineData("aggregate")]
    [InlineData("domain")]
    [InlineData("event-count")]
    [InlineData("status-code")]
    public async Task CompletedStatusMustProveThisBootstrapAndItsSingleCommittedEvent(string invalid)
    {
        using HttpClient client = CommandClient(messageId =>
        {
            var status = Completed(messageId, 1);
            status = invalid switch
            {
                "correlation" => status with { CorrelationId = "different" },
                "message" => status with { MessageId = "different" },
                "aggregate" => status with { AggregateId = "different" },
                "domain" => status with { Domain = "different" },
                "event-count" => status with { EventCount = 2 },
                "status-code" => status with { StatusCode = (int)CommandStatus.Rejected },
                _ => throw new ArgumentOutOfRangeException(nameof(invalid)),
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(status) };
        });
        await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", UnexpectedReadAsync, () => ValueTask.CompletedTask, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProjectedAuthorityCannotPrecedeTheActualSuccessfulCommit()
    {
        using HttpClient client = CommandClient(messageId => Terminal(messageId, "Completed", 3));
        List<string> versions = ["tenant-sequence:2", "tenant-sequence:3"];
        int reads = 0;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await AspireTopologyFixture.CompleteBootstrapAsync(client, "credential-sentinel", (_, _) =>
        {
            var response = Ready();
            return Task.FromResult(response with { Metadata = response.Metadata with { ProjectionVersion = versions[reads++] } });
        }, UnexpectedCleanupAsync, false, cancellation.Token);
        reads.ShouldBe(2);
    }

    [Theory]
    [InlineData("completed-first", true)]
    [InlineData("completed-later", false)]
    [InlineData("conflict", false)]
    [InlineData("rejected-already", false)]
    public async Task ColdModeRequiresANewCompletedFirstSequence(string shape, bool succeeds)
    {
        using HttpClient client = shape == "conflict"
            ? Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            { Content = JsonContent.Create(new { reasonCode = "global-admin-already-bootstrapped-rejection" }) }))
            : CommandClient(messageId => Terminal(messageId, shape == "rejected-already" ? "Rejected" : "Completed",
                shape == "completed-later" ? 2 : 1, shape == "rejected-already" ? "GlobalAdminAlreadyBootstrappedRejection" : null));
        bool cleaned = false;
        Task Run() => AspireTopologyFixture.CompleteBootstrapAsync(client, "credential-sentinel", ReadReadyAsync,
            () => { cleaned = true; return ValueTask.CompletedTask; }, true, TestContext.Current.CancellationToken);
        if (succeeds) await Run();
        else
        {
            var exception = await Should.ThrowAsync<InvalidOperationException>(Run);
            exception.Message.ShouldBe("bootstrap-cold-state-required");
        }
        cleaned.ShouldBe(!succeeds);
    }

    [Fact]
    public async Task KnownConflictStillRequiresActualProjectedCallerAuthority()
    {
        using HttpClient client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
        { Content = JsonContent.Create(new { reasonCode = "global-admin-already-bootstrapped-rejection" }) }));
        int reads = 0;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await AspireTopologyFixture.CompleteBootstrapAsync(client, "credential-sentinel", (_, _) =>
        {
            reads++;
            return Task.FromResult(reads == 1 ? Ready() with { Payload = new([], null, false) } : Ready());
        }, UnexpectedCleanupAsync, false, cancellation.Token);
        reads.ShouldBe(2);
    }

    [Fact]
    public async Task ColdModeRejectsReusedProjectedAuthorityEvenAfterAFirstSequenceCommit()
    {
        using HttpClient client = CommandClient(messageId => Terminal(messageId, "Completed", 1));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int reads = 0;
        bool cleaned = false;
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", (_, _) =>
            {
                if (++reads == 2) cancellation.Cancel();
                var response = Ready();
                return Task.FromResult(response with { Metadata = response.Metadata with { ProjectionVersion = "tenant-sequence:2" } });
            }, () => { cleaned = true; return ValueTask.CompletedTask; }, true, cancellation.Token));
        exception.Message.ShouldBe("bootstrap-authority-timeout");
        reads.ShouldBe(2);
        cleaned.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializationBoundarySanitizesTransportAndCleanupFailures(bool failCleanup)
    {
        using HttpClient client = Client((_, _) => throw new HttpRequestException(HttpRequestError.SecureConnectionError,
            PrivateSentinel, new InvalidOperationException(PrivateSentinel)));
        bool cleaned = false;
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", UnexpectedReadAsync, () =>
            {
                cleaned = true;
                if (failCleanup) throw new InvalidOperationException(PrivateSentinel);
                return ValueTask.CompletedTask;
            }, false, TestContext.Current.CancellationToken));
        cleaned.ShouldBeTrue();
        exception.Message.ShouldBe("bootstrap-command-failed" + (failCleanup ? ":cleanup-failed" : ""));
        AssertSafe(exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitializationBoundaryCleansUpCancellationDuringSubmissionOrAuthority(bool duringRead)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using HttpClient client = duringRead ? CommandClient(messageId => Terminal(messageId, "Completed", 1))
            : Client((_, _) => { cancellation.Cancel(); throw new OperationCanceledException(PrivateSentinel, cancellation.Token); });
        bool cleaned = false;
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", (_, _) =>
            {
                cancellation.Cancel();
                throw new OperationCanceledException(PrivateSentinel, cancellation.Token);
            }, () => { cleaned = true; return ValueTask.CompletedTask; }, false, cancellation.Token));
        exception.Message.ShouldBe(duringRead ? "bootstrap-authority-timeout" : "bootstrap-command-timeout");
        cleaned.ShouldBeTrue();
        AssertSafe(exception);
    }

    [Fact]
    public async Task TransientRetriesRemainBoundedByTheOriginalCancellation()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        List<string?> messageIds = [];
        using HttpClient client = Client(async (request, token) =>
        {
            using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            messageIds.Add(body.RootElement.GetProperty("messageId").GetString());
            if (messageIds.Count == 2) cancellation.Cancel();
            throw new HttpRequestException(HttpRequestError.ConnectionError, PrivateSentinel);
        });
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", UnexpectedReadAsync, () => ValueTask.CompletedTask, false, cancellation.Token));
        exception.Message.ShouldBe("bootstrap-command-timeout");
        messageIds.Count.ShouldBe(2);
        messageIds[1].ShouldBe(messageIds[0]);
        AssertSafe(exception);
    }

    [Fact]
    public async Task InitializationBoundarySanitizesReadFailureDetails()
    {
        using HttpClient client = CommandClient(messageId => Terminal(messageId, "Completed", 1));
        bool cleaned = false;
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => AspireTopologyFixture.CompleteBootstrapAsync(
            client, "credential-sentinel", (_, _) => throw new InvalidOperationException(PrivateSentinel),
            () => { cleaned = true; return ValueTask.CompletedTask; }, false, TestContext.Current.CancellationToken));
        exception.Message.ShouldBe("bootstrap-authority-unavailable");
        cleaned.ShouldBeTrue();
        AssertSafe(exception);
    }

    private const string PrivateSentinel = "Bearer credential-sentinel https://private.invalid/signed-url-sentinel";

    private static void AssertSafe(Exception exception)
    {
        exception.ToString().ShouldNotContain("credential-sentinel");
        exception.ToString().ShouldNotContain("signed-url-sentinel");
        exception.ToString().ShouldNotContain("private.invalid");
        exception.InnerException.ShouldBeNull();
    }

    private static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        => new(new BootstrapHttpMessageHandler(send)) { BaseAddress = new Uri("https://private.invalid") };

    private static HttpClient CommandClient(Func<string, HttpResponseMessage> terminal)
    {
        string? messageId = null;
        return Client(async (request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                request.RequestUri!.AbsolutePath.ShouldBe("/api/v1/commands/status/bootstrap-correlation");
                request.Headers.Authorization?.Parameter.ShouldBe("credential-sentinel");
                return terminal(messageId!);
            }
            using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            messageId = body.RootElement.GetProperty("messageId").GetString();
            return Accepted(messageId!);
        });
    }

    private static HttpResponseMessage Accepted(string messageId)
        => new(HttpStatusCode.Accepted) { Content = JsonContent.Create(new { correlationId = "bootstrap-correlation", messageId }) };

    private static CommandStatusResponse Completed(string messageId, long? sequence)
        => new("bootstrap-correlation", "Completed", (int)CommandStatus.Completed, DateTimeOffset.UtcNow,
            "global-administrators", 1, null, PrivateSentinel, null, messageId)
        { Domain = "global-administrators", CommittedEventSequence = sequence };

    private static HttpResponseMessage Terminal(string messageId, string status, long? sequence, string? rejection = null)
        => new(HttpStatusCode.OK) { Content = JsonContent.Create(Completed(messageId, sequence) with
        { Status = status, StatusCode = status == "Completed" ? (int)CommandStatus.Completed : (int)CommandStatus.Rejected, RejectionEventType = rejection }) };

    private static Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> ReadReadyAsync(string? _, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Ready());
    }

    private static Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> UnexpectedReadAsync(string? _, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Authority must not be read after this command failure.");

    private static ValueTask UnexpectedCleanupAsync() => throw new InvalidOperationException("Unexpected cleanup.");

    private static TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>> Ready()
        => new(new([new(AuthenticatedRouteState.CallerId)], null, false),
            new QueryResponseMetadata(ETag: "\"bootstrap-etag\"", IsNotModified: false, IsStale: false,
                IsDegraded: false, ProjectionVersion: "tenant-sequence:1", ServedAt: DateTimeOffset.UtcNow)
            { Provenance = QueryResponseProvenance.ProjectionBacked, Lifecycle = ProjectionLifecycleState.Current },
            TenantsRestQueryFailureKind.None, (int)HttpStatusCode.OK);
}
