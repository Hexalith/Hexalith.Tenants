using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.UI.State.TenantList;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Qualifies protected FrontComposer pages and contextual audit navigation in the live topology.</summary>
[Collection("AuthenticatedTenantsTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class AuthenticatedTenantsUiRouteTests(AuthenticatedTenantsTopologyFixture fixture) : IDisposable
{
    private readonly IDisposable _daprTestLease = DaprTestExecutionGate.Enter();

    /// <inheritdoc/>
    public void Dispose() => _daprTestLease.Dispose();

    [DaprFact]
    public async Task ProtectedRoutesRenderForValidatedCallerAndCurrentTenant()
    {
        fixture.SkipIfUnavailable();
        fixture.ProjectionWorkersEnabledAtStartup.ShouldBeFalse("authenticated UI routes qualify the independent request-path profile");
        await WithSafeEvidenceAsync(TimeSpan.FromMinutes(5), async (_, phase, cancellationToken) =>
        {
            phase("native-admission-window");
            await fixture.BeginNativeReadQualificationAsync(cancellationToken);
            phase("sign-in");
            string cookie = await fixture.SignInAsync(AspireTopologyTests.CreateDemoJwt(), cancellationToken);
            string scopeMarkup = await GetMarkupAsync("/tenants", cookie, cancellationToken);
            scopeMarkup.ShouldNotContain("data-testid=\"fc-scope-blocked\"");
            scopeMarkup.ShouldContain("data-testid=\"tenants-workspace\"");
            phase("command-seed");
            AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, cancellationToken);
            phase("projection-pages");
            string? listCursor = await state.WaitForListPageAsync(fixture.TenantsApiClient, 20, cancellationToken);
            string? membershipCursor = await state.WaitForCallerMembershipPageAsync(fixture.TenantsApiClient, 20, cancellationToken);
            string corpusToken = await state.CreateCorpusReadTokenAsync(fixture.TenantsApiClient, cancellationToken);
            phase("sign-in");
            cookie = await fixture.SignInAsync(corpusToken, cancellationToken);
            phase("http-routes");
            foreach ((string route, string marker) in Routes(state, listCursor, membershipCursor))
            {
                TestContext.Current.TestOutputHelper?.WriteLine($"Checking protected surface: {marker}.");
                if (marker == "tenants-global-admins-area")
                {
                    // This surface deliberately resolves authority only from the interactive circuit.
                    // HTTP prerender must remain restricted; the live browser case requires real rows.
                    string prerender = await GetMarkupAsync(route, cookie, cancellationToken);
                    prerender.ShouldContain("data-testid=\"tenants-global-admins-restricted-header\"");
                    prerender.ShouldNotContain("data-testid=\"tenants-global-admins-row\"");
                    continue;
                }
                string markup = await GetReadyMarkupAsync(route, cookie, marker, state, cancellationToken);
                markup.ShouldNotContain("data-testid=\"fc-scope-blocked\"");
                markup.ShouldContain($"data-testid=\"{marker}\"");
                HasSeededReadyContent(markup, marker, state).ShouldBeTrue($"The {marker} surface must render its authoritative seeded rows.");
            }

            string detail = await GetMarkupAsync($"/tenants/{state.TenantId}", cookie, cancellationToken);
            detail.ShouldContain(state.Persisted.Name);
            detail.ShouldContain(state.TargetUserId);
            detail.Contains(state.Token, StringComparison.Ordinal).ShouldBeFalse("Rendered content must not contain the fixture access token.");
            detail.Contains(corpusToken, StringComparison.Ordinal).ShouldBeFalse("Rendered content must not contain the corpus access token.");
        });
    }

    [DaprFact]
    public async Task AmbiguousCurrentTenantDeniesProtectedContent()
    {
        fixture.SkipIfUnavailable();
        string markup = await GetMarkupAsync("/tenants", AuthenticatedTenantsTopologyFixture.CreateCookie(AspireTopologyTests.CreateDemoJwt(), ambiguousTenant: true), TestContext.Current.CancellationToken);
        markup.ShouldContain("data-testid=\"fc-scope-blocked\"");
        markup.ShouldNotContain("data-testid=\"tenants-workspace\"");
    }

    [DaprFact]
    public async Task LiveBrowserMeasuresResponsiveForcedColorsAndRestoresContextualAuditBackFocus()
    {
        fixture.SkipIfUnavailable();
        fixture.ProjectionWorkersEnabledAtStartup.ShouldBeFalse("authenticated UI routes qualify the independent request-path profile");
        // The responsive and contextual groups include four real native admission windows.
        // Keep one finite envelope for all cases, including their bounded read work.
        await WithSafeEvidenceAsync(TimeSpan.FromMinutes(25), async (outputPath, phase, cancellationToken) =>
        {
            phase("native-admission-window");
            await fixture.BeginNativeReadQualificationAsync(cancellationToken);
            phase("command-seed");
            AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, cancellationToken);
            phase("projection-pages");
            string? listCursor = await state.WaitForListPageAsync(fixture.TenantsApiClient, 20, cancellationToken);
            string? membershipCursor = await state.WaitForCallerMembershipPageAsync(fixture.TenantsApiClient, 20, cancellationToken);
            string corpusToken = await state.CreateCorpusReadTokenAsync(fixture.TenantsApiClient, cancellationToken);
            phase("sign-in");
            string cookie = await fixture.SignInAsync(corpusToken, cancellationToken);
            string directory = Path.GetDirectoryName(outputPath)!;
            string inputPath = Path.Combine(directory, "session.private.json");
            try
            {
                var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }

                await using (var input = new FileStream(inputPath, options))
                {
                    await JsonSerializer.SerializeAsync(input, new
                    {
                        baseUrl = fixture.TenantsUiClient.BaseAddress!.AbsoluteUri,
                        tenantId = state.TenantId,
                        tenantName = state.Persisted.Name,
                        targetUserId = state.TargetUserId,
                        callerId = AuthenticatedRouteState.CallerId,
                        listPath = listCursor is null ? "/tenants" : $"/tenants?cursor={Uri.EscapeDataString(listCursor)}",
                        myTenantsPath = membershipCursor is null ? "/tenants/my" : $"/tenants/my?cursor={Uri.EscapeDataString(membershipCursor)}",
                        cookie,
                        configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ? "Release" : "Debug",
                    }, cancellationToken: cancellationToken);
                }

                var start = new ProcessStartInfo("node") { WorkingDirectory = AuthenticatedTenantsTopologyFixture.RepositoryRoot, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add("tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-live-routes.mjs");
                start.ArgumentList.Add(inputPath);
                start.ArgumentList.Add(outputPath);
                phase("browser-start");
                using Process process = Process.Start(start).ShouldNotBeNull();
                // Output is deliberately discarded: the maintained validator writes only fixed safe
                // failure labels into result.json, never browser URLs, cookie tickets or access tokens.
                Task stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
                Task stderr = process.StandardError.ReadToEndAsync(cancellationToken);
                try
                {
                    phase("browser-run");
                    await process.WaitForExitAsync(cancellationToken);
                    await Task.WhenAll(stdout, stderr);
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                }

                phase("browser-result");
                File.Exists(outputPath).ShouldBeTrue("The live validator must produce its support-safe result artifact.");
                using JsonDocument evidence = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, cancellationToken));
                if (process.ExitCode != 0)
                {
                    TestContext.Current.TestOutputHelper?.WriteLine(DescribeBrowserFailure(evidence.RootElement));
                }
                process.ExitCode.ShouldBe(0, evidence.RootElement.GetRawText());
                evidence.RootElement.GetProperty("passed").GetBoolean().ShouldBeTrue();
                evidence.RootElement.GetProperty("cases").GetArrayLength().ShouldBeGreaterThanOrEqualTo(25);
                TestContext.Current.TestOutputHelper?.WriteLine($"Safe live browser evidence: {outputPath}");
            }
            finally
            {
                File.Delete(inputPath);
            }
        });
    }

    /// <summary>Retains only authored failure categories and bounded readiness values in generic TRX output.</summary>
    internal static string DescribeBrowserFailure(JsonElement evidence)
    {
        string Category(JsonElement value, string property)
        {
            if (!value.TryGetProperty(property, out JsonElement item) || item.ValueKind != JsonValueKind.String)
                return "unavailable";
            string? text = item.GetString();
            return text is { Length: > 0 and <= 96 } && text.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
                ? text : "unavailable";
        }
        if (!evidence.TryGetProperty("failure", out JsonElement failure)) return "Live browser failure: unavailable.";
        var flags = new Dictionary<string, object?>();
        if (evidence.TryGetProperty("readiness", out JsonElement readiness))
        {
            foreach (string name in new[] { "available", "currentBadgeVisible", "workspace", "grid", "loading", "error", "unauthorized", "degraded", "pagingRecovered", "cursorPresent", "requestedCursorPresent", "searchCursorPresent" })
                if (readiness.TryGetProperty(name, out JsonElement flag) && flag.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    flags[name] = flag.GetBoolean();
            foreach (string name in new[] { "seededRowCount", "currentBadgeCount", "listRowCount" })
                if (readiness.TryGetProperty(name, out JsonElement count) && count.ValueKind == JsonValueKind.Number && count.TryGetInt32(out int bounded) && bounded is >= 0 and <= 100)
                    flags[name] = bounded;
            if (readiness.TryGetProperty("pageSize", out JsonElement pageSize) && pageSize.ValueKind == JsonValueKind.Number && pageSize.TryGetInt32(out int size) && size is 20 or 50 or 100)
                flags["pageSize"] = size;
        }
        var progressFlags = new Dictionary<string, int>();
        if (evidence.TryGetProperty("progress", out JsonElement progress))
        {
            foreach (string name in new[] { "completedCases", "elapsedMilliseconds" })
                if (progress.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number
                    && value.TryGetInt32(out int bounded) && bounded >= 0
                    && (name == "elapsedMilliseconds" || bounded <= 100))
                    progressFlags[name] = bounded;
        }
        var focusFlags = new Dictionary<string, object?>();
        if (evidence.TryGetProperty("focus", out JsonElement focus) && focus.ValueKind == JsonValueKind.Object)
        {
            foreach (string name in new[] { "available", "launcherAttached", "launcherVisible", "launcherEnabled", "launcherHasDestination", "expectedFocus", "detailSurfacePresent", "detailCurrentState" })
                if (focus.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    focusFlags[name] = value.GetBoolean();
            if (focus.TryGetProperty("launcherCount", out JsonElement count) && count.ValueKind == JsonValueKind.Number
                && count.TryGetInt32(out int bounded) && bounded is >= 0 and <= 100)
                focusFlags["launcherCount"] = bounded;
            if (focus.TryGetProperty("activeElementCategory", out JsonElement active) && active.ValueKind == JsonValueKind.String
                && active.GetString() is "body" or "heading" or "button" or "input" or "link" or "other")
                focusFlags["activeElementCategory"] = active.GetString();
        }
        return $"Live browser failure: Phase={Category(failure, "phase")}, Label={Category(failure, "label")}, Readiness={JsonSerializer.Serialize(flags)}, Progress={JsonSerializer.Serialize(progressFlags)}, Focus={JsonSerializer.Serialize(focusFlags)}.";
    }

    private static IEnumerable<(string Route, string Marker)> Routes(AuthenticatedRouteState state, string? listCursor, string? membershipCursor)
    {
        yield return (listCursor is null ? "/tenants" : $"/tenants?cursor={Uri.EscapeDataString(listCursor)}", "tenants-workspace");
        yield return ($"/tenants/{state.TenantId}", "tenants-detail");
        yield return ($"/tenants/{state.TenantId}/audit", "tenants-audit-surface");
        yield return (membershipCursor is null ? "/tenants/my" : $"/tenants/my?cursor={Uri.EscapeDataString(membershipCursor)}", "tenants-my-page");
        yield return ($"/tenants/users?userId={state.TargetUserId}", "tenants-user-lookup");
        yield return ("/global-administrators", "tenants-global-admins-area");
    }

    /// <summary>Preserves bounded qualification evidence before sign-in, seeding, or browser launch can fail.</summary>
    /// <param name="limit">Maximum duration for the qualification run.</param>
    /// <param name="qualification">Qualification callback receiving output path, phase updater, and cancellation token.</param>
    /// <param name="beforeRetainMoveAsync">Optional test hook forwarded to <see cref="RetainBrowserProgressAsync"/>.</param>
    internal static async Task WithSafeEvidenceAsync(
        TimeSpan limit,
        Func<string, Action<string>, CancellationToken, Task> qualification,
        Func<string, Task>? beforeRetainMoveAsync = null)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"tenants-live-routes-{Guid.NewGuid():N}");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        string outputPath = Path.Combine(directory, "result.json");
        string phase = "initialization";
        await WriteWrapperEvidenceAsync(outputPath, phase, "qualification-not-completed", passed: false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(limit);
        try
        {
            await qualification(outputPath, current => phase = current, timeout.Token);
            if (!await HasBrowserEvidenceAsync(outputPath))
            {
                await WriteWrapperEvidenceAsync(outputPath, "http-routes", "qualification-completed", passed: true);
            }
        }
        catch (Exception exception)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"Live qualification exception category: {exception.GetType().Name}.");
            string label = exception is OperationCanceledException or TimeoutException ? "qualification-timeout" : "qualification-failed";
            if (!await RetainBrowserProgressAsync(outputPath, phase, label, beforeRetainMoveAsync)
                && !await HasBrowserEvidenceAsync(outputPath)
                && !await HasBrowserProgressAsync(outputPath))
            {
                await WriteWrapperEvidenceAsync(outputPath, phase, label, passed: false);
            }

            using JsonDocument failureEvidence = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            TestContext.Current.TestOutputHelper?.WriteLine(DescribeBrowserFailure(failureEvidence.RootElement));
            throw new InvalidOperationException($"Live qualification failed during {phase}. Safe evidence: {outputPath}");
        }
        finally
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"Safe live qualification evidence: {outputPath}");
        }
    }

    /// <summary>Atomically updates an in-progress browser checkpoint with wrapper failure metadata.</summary>
    /// <param name="outputPath">The checkpoint file path.</param>
    /// <param name="wrapperPhase">The enclosing wrapper phase.</param>
    /// <param name="label">The bounded failure label.</param>
    /// <param name="beforeMoveAsync">Optional test hook invoked after flushing the sibling temporary file and before atomic replacement.</param>
    /// <returns><see langword="true"/> when the checkpoint was updated; otherwise, <see langword="false"/>.</returns>
    internal static async Task<bool> RetainBrowserProgressAsync(
        string outputPath,
        string wrapperPhase,
        string label,
        Func<string, Task>? beforeMoveAsync = null)
    {
        try
        {
            if (TryParseBrowserProgress(await File.ReadAllTextAsync(outputPath)) is not { } parsed)
            {
                return false;
            }

            (JsonObject snapshot, string phase) = parsed;
            snapshot["passed"] = false;
            snapshot["failure"] ??= JsonSerializer.SerializeToNode(new { phase, label });
            snapshot["wrapperFailure"] = JsonSerializer.SerializeToNode(new { phase = wrapperPhase, label });
            snapshot["finishedUtc"] = JsonSerializer.SerializeToNode(DateTimeOffset.UtcNow);
            byte[] payload = Encoding.UTF8.GetBytes(snapshot.ToJsonString() + "\n");
            await WriteFileAtomicallyAsync(
                outputPath,
                stream => stream.WriteAsync(payload).AsTask(),
                beforeMoveAsync);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private static (JsonObject Snapshot, string Phase)? TryParseBrowserProgress(string json)
    {
        if (JsonNode.Parse(json) is JsonObject snapshot
            && snapshot["mode"] is JsonValue mode && mode.TryGetValue(out string? _)
            && snapshot["cases"] is JsonArray
            && snapshot["progress"] is JsonObject progress
            && progress["phase"] is JsonValue phaseValue && phaseValue.TryGetValue(out string? phase)
            && phase is { Length: > 0 and <= 96 }
            && phase.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
        {
            return (snapshot, phase);
        }

        return null;
    }

    private static async Task<bool> HasBrowserProgressAsync(string outputPath)
    {
        try
        {
            return TryParseBrowserProgress(await File.ReadAllTextAsync(outputPath)) is not null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private static async Task<bool> HasBrowserEvidenceAsync(string outputPath)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            JsonElement root = document.RootElement;
            return root.TryGetProperty("finishedUtc", out _)
                && root.TryGetProperty("mode", out _)
                && root.TryGetProperty("passed", out _)
                && root.TryGetProperty("cases", out _);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    /// <summary>Atomically writes wrapper-owned qualification evidence with private file permissions.</summary>
    /// <param name="outputPath">The evidence file path.</param>
    /// <param name="phase">The bounded qualification phase.</param>
    /// <param name="label">The bounded failure or completion label.</param>
    /// <param name="passed">Whether the qualification passed.</param>
    /// <param name="beforeMoveAsync">Optional test hook invoked after flushing the sibling temporary file and before atomic replacement.</param>
    internal static Task WriteWrapperEvidenceAsync(
        string outputPath,
        string phase,
        string label,
        bool passed,
        Func<string, Task>? beforeMoveAsync = null)
        => WriteFileAtomicallyAsync(
            outputPath,
            stream => JsonSerializer.SerializeAsync(stream, new
            {
                producer = "wrapper",
                passed,
                cases = Array.Empty<object>(),
                failure = passed ? null : new { phase, label },
                finishedUtc = DateTimeOffset.UtcNow,
            }),
            beforeMoveAsync);

    private static async Task WriteFileAtomicallyAsync(
        string destinationPath,
        Func<FileStream, Task> writeContentAsync,
        Func<string, Task>? beforeMoveAsync = null)
    {
        string fullDestinationPath = Path.GetFullPath(destinationPath);
        string directory = Path.GetDirectoryName(fullDestinationPath)
            ?? throw new InvalidOperationException("Evidence path requires a parent directory.");
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.tmp");
        bool moved = false;
        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous,
            };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            await using (var stream = new FileStream(tempPath, options))
            {
                await writeContentAsync(stream);
                await stream.FlushAsync();
                stream.Flush(flushToDisk: true);
            }

            if (beforeMoveAsync is not null)
            {
                await beforeMoveAsync(tempPath);
            }

            File.Move(tempPath, fullDestinationPath, overwrite: true);
            moved = true;
        }
        finally
        {
            if (!moved)
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private async Task<string> GetReadyMarkupAsync(string route, string cookie, string marker, AuthenticatedRouteState state, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        string markup;
        do
        {
            markup = await GetMarkupAsync(route, cookie, cancellationToken);
            if (HasSeededReadyContent(markup, marker, state))
            {
                WriteMarkupFlags(markup, marker, route, state);
                return markup;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
        while (DateTimeOffset.UtcNow < deadline);

        WriteMarkupFlags(markup, marker, route, state);
        return markup;
    }

    private static void WriteMarkupFlags(string markup, string marker, string route, AuthenticatedRouteState state)
    {
        bool Has(string value) => markup.Contains(value, StringComparison.Ordinal);
        TestContext.Current.TestOutputHelper?.WriteLine($"Protected readiness flags: Surface={marker}, CursorPresent={route.Contains("cursor=", StringComparison.Ordinal)}, "
            + $"ScopeBlocked={Has("data-testid=\"fc-scope-blocked\"")}, Grid={Has("data-testid=\"tenants-list-grid\"")}, "
            + $"Rows={System.Text.RegularExpressions.Regex.Matches(markup, "data-testid=\"tenants-list-tenant-id\"").Count}, SeededReference={Has(state.TenantId)}, "
            + $"Current={Has("truth-state-badge--current")}, Degraded={Has("data-testid=\"tenants-list-degraded\"")}, Error={Has("data-testid=\"tenants-list-error\"")}." );
    }

    private static bool HasSeededReadyContent(string markup, string marker, AuthenticatedRouteState state)
        => marker switch
        {
            "tenants-workspace" => markup.Contains("data-testid=\"tenants-list-grid\"", StringComparison.Ordinal)
                && markup.Contains($"title=\"{state.TenantId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-detail" => markup.Contains("data-testid=\"tenants-member-row\"", StringComparison.Ordinal)
                && markup.Contains($"id=\"tenants-member-{state.TargetUserId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-audit-surface" => markup.Contains("data-testid=\"tenants-audit-ready\"", StringComparison.Ordinal)
                && markup.Contains("data-testid=\"tenants-audit-row\"", StringComparison.Ordinal),
            "tenants-my-page" => markup.Contains("data-testid=\"tenants-my-row\"", StringComparison.Ordinal)
                && markup.Contains($"title=\"{state.TenantId}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            "tenants-user-lookup" => markup.Contains("data-testid=\"tenants-user-row\"", StringComparison.Ordinal)
                && markup.Contains($"id=\"{TenantDomIdentity.ElementId("tenants-user-row-", state.TenantId)}\"", StringComparison.Ordinal)
                && markup.Contains("truth-state-badge--current", StringComparison.Ordinal),
            _ => false,
        };

    private async Task<string> GetMarkupAsync(string route, string cookie, CancellationToken cancellationToken)
    {
        Uri destination = new(fixture.TenantsUiClient.BaseAddress!, route);
        for (int hop = 0; hop < 5; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, destination);
            request.Headers.Add("Cookie", cookie);
            using HttpResponseMessage response = await fixture.TenantsUiClient.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
            {
                Uri next = location.IsAbsoluteUri ? location : new Uri(destination, location);
                next.Scheme.ShouldBe(destination.Scheme);
                next.Authority.ShouldBe(destination.Authority);
                next.AbsolutePath.StartsWith("/authentication", StringComparison.Ordinal).ShouldBeFalse("Protected content must not restart authentication.");
                (next == destination).ShouldBeFalse("Canonical route recovery must not redirect to itself.");
                destination = next;
                continue;
            }

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        throw new InvalidOperationException("Protected route normalization exceeded its redirect bound.");
    }
}
