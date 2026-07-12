// <copyright file="ProjectionWriteConformanceFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

// Projection-write conformance fixture completed by Story 10.4.
// Test design reference: _bmad-output/test-artifacts/test-design-epic-10.md (T-R001-UNIT-001, T-R001-UNIT-002, T-R001-INT-001).
// The fixture drives production projection writes exclusively through TenantProjectionHandler.ProjectAsync,
// which routes into TenantProjectionWritePolicy. Tests cannot bypass the production helper by construction.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Server.Tests.Projections;

/// <summary>
/// Conformance fixture for projection-write safety tests (Story 10.4, R-001 trio).
/// Owns deterministic scripted state-store outcomes and a structured-state capturing
/// logger for negative diagnostic-content assertions (risk R-007).
/// Production behavior is exercised exclusively through <see cref="TenantProjectionHandler.ProjectAsync"/>.
/// </summary>
internal sealed class ProjectionWriteConformanceFixture
{
    public const string StateStoreName = "statestore";
    public const string TenantAuditProjectionKey = "audit:tenant-1";
    public const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    public const string TenantIndexKeyCategory = "tenant index";
    public const string TenantProjectionKey = "projection:tenants:tenant-1";
    public const string TenantId = "tenant-1";

    public ScriptedTenantProjectionStateStore StateStore { get; } = new();

    public CapturingLogger<TenantProjectionHandler> Logger { get; } = new();

    public Task<ProjectionResponse> RunProjectionHandlerAsync(ProjectionRequest request) =>
        new TenantProjectionHandler(StateStore, Logger).ProjectAsync(request);

    public void EnqueueSuccessfulTenantDetailSave()
    {
        StateStore.EnqueueRead<TenantReadModel>(TenantProjectionKey, null, null);
        StateStore.EnqueueTrySave(TenantProjectionKey, true);
    }

    public void EnqueueSuccessfulAuditSave()
    {
        StateStore.EnqueueRead<TenantAuditReadModel>(TenantAuditProjectionKey, null, null);
        StateStore.EnqueueTrySave(TenantAuditProjectionKey, true);
    }

    public void EnqueueSuccessfulIndexSave()
    {
        StateStore.EnqueueRead<TenantIndexReadModel>(TenantIndexProjectionKey, null, null);
        StateStore.EnqueueTrySave(TenantIndexProjectionKey, true);
    }

    // ---- Fixture contract API (AC5): per-key inspection helpers so future projections
    // do not duplicate LINQ boilerplate across tests.

    public int GetReadAttemptCount(string key) =>
        StateStore.ReadCalls.Count(c => c.Key == key);

    public int GetSaveAttemptCount(string key) =>
        StateStore.TrySaveAttempts.Count(a => a.Key == key);

    public SaveAttempt GetSaveAttempt(string key, int attemptIndex)
    {
        IReadOnlyList<SaveAttempt> saves = [.. StateStore.TrySaveAttempts.Where(a => a.Key == key)];
        return attemptIndex < 0 || attemptIndex >= saves.Count
            ? throw new InvalidOperationException(
                $"No save attempt at index {attemptIndex} for key '{key}'. Total attempts: {saves.Count}.")
            : saves[attemptIndex];
    }

    public IReadOnlyList<CapturedLog> GetLogEntries(int eventId) =>
        [.. Logger.Entries.Where(e => e.EventId.Id == eventId)];

    public static ProjectionRequest CreateRequest(params ProjectionEventDto[] events) =>
        new(TenantId, "tenants", TenantId, events);

    public static TenantReadModel SeedTenantReadModel(
        string tenantId = TenantId,
        string name = "Existing Tenant")
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated(tenantId, name, null, System.DateTimeOffset.UnixEpoch));
        return model;
    }

    public static TenantIndexReadModel SeedIndexWith(params (string TenantId, string Name)[] tenants)
    {
        var model = new TenantIndexReadModel();
        foreach ((string tenantId, string name) in tenants)
        {
            model.Apply(new TenantCreated(tenantId, name, null, System.DateTimeOffset.UnixEpoch));
        }

        return model;
    }

    public static TenantAuditReadModel SeedAuditWith(params TenantAuditEntry[] entries) =>
        new() {
            Entries = [.. entries],
        };

    public static TenantAuditEntry CreateAuditEntry(
        string eventId,
        string eventType,
        System.DateTimeOffset timestamp,
        string actorId = "actor-1",
        IReadOnlyDictionary<string, string>? narrativePayload = null) =>
        new(
            eventId,
            eventType,
            AuditEventCategory.Access,
            actorId,
            timestamp,
            TenantId,
            narrativePayload ?? new Dictionary<string, string>(StringComparer.Ordinal) { ["source"] = "persisted" });

    public static ProjectionEventDto CreateEvent(
        IEventPayload payload,
        string messageId,
        System.DateTimeOffset timestamp,
        string correlationId = "corr-1",
        string? userId = "actor-1") =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
            timestamp,
            correlationId,
            messageId,
            userId);
}

/// <summary>
/// Scripted, per-key, per-attempt state-store fake for projection-write conformance tests.
/// Fails fast on operations against a key that has been marked terminally failed so the
/// AC10 "no extra writes after terminal failure" invariant is enforced at the seam.
/// </summary>
internal sealed class ScriptedTenantProjectionStateStore : IReadModelStore
{
    private readonly Dictionary<string, Queue<object>> _reads = [];
    private readonly Dictionary<string, Queue<bool>> _trySaveResults = [];
    private readonly HashSet<string> _terminalFailureKeys = new(StringComparer.Ordinal);

    public List<ReadCall> ReadCalls { get; } = [];

    public List<SaveAttempt> TrySaveAttempts { get; } = [];

    public List<SaveAttempt> PlainSaveAttempts { get; } = [];

    public List<EraseAttempt> TryEraseAttempts { get; } = [];

    public void EnqueueRead<TValue>(string key, TValue? value, string? etag)
        where TValue : class
    {
        if (!_reads.TryGetValue(key, out Queue<object>? queue))
        {
            queue = new Queue<object>();
            _reads[key] = queue;
        }

        queue.Enqueue(new ReadModelEntry<TValue>(value, etag));
    }

    public void EnqueueTrySave(string key, bool result)
    {
        if (!_trySaveResults.TryGetValue(key, out Queue<bool>? queue))
        {
            queue = new Queue<bool>();
            _trySaveResults[key] = queue;
        }

        queue.Enqueue(result);
    }

    /// <summary>
    /// Marks a key as terminally failed. Subsequent reads or save attempts against that key
    /// throw, enforcing the AC10 invariant that production code must not retry after the
    /// retry budget is exhausted (Story 10.4, Task line 41).
    /// </summary>
    public void MarkTerminalFailure(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _terminalFailureKeys.Add(key);
    }

    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        System.Threading.CancellationToken cancellationToken = default)
        where TValue : class
    {
        if (_terminalFailureKeys.Contains(key))
        {
            throw new InvalidOperationException(
                $"AC10 violation: production code attempted to read key '{key}' after the retry budget was exhausted. " +
                "ReadModelWritePolicy must throw without further state-store traffic on retry exhaustion.");
        }

        ReadCalls.Add(new ReadCall(storeName, key, typeof(TValue)));
        if (!_reads.TryGetValue(key, out Queue<object>? queue) || queue.Count == 0)
        {
            throw new InvalidOperationException(
                $"Scripted state store has no remaining read outcomes for key '{key}'. " +
                "Either production code performed more reads than the test scripted, " +
                "or EnqueueRead must be called for every expected attempt.");
        }

        return Task.FromResult((ReadModelEntry<TValue>)queue.Dequeue());
    }

    public Task SaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        System.Threading.CancellationToken cancellationToken = default)
        where TValue : class
    {
        PlainSaveAttempts.Add(new SaveAttempt(storeName, key, value, string.Empty, typeof(TValue)));
        return Task.CompletedTask;
    }

    public Task<bool> TrySaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        System.Threading.CancellationToken cancellationToken = default)
        where TValue : class
    {
        if (_terminalFailureKeys.Contains(key))
        {
            throw new InvalidOperationException(
                $"AC10 violation: production code attempted to save key '{key}' after the retry budget was exhausted. " +
                "ReadModelWritePolicy must throw without further state-store traffic on retry exhaustion.");
        }

        TrySaveAttempts.Add(new SaveAttempt(storeName, key, value, etag, typeof(TValue)));
        if (!_trySaveResults.TryGetValue(key, out Queue<bool>? queue) || queue.Count == 0)
        {
            throw new InvalidOperationException(
                $"Scripted state store has no remaining TrySave outcomes for key '{key}'. " +
                "Either production code attempted to write more times than the test scripted, " +
                "or EnqueueTrySave must be called for every expected attempt.");
        }

        return Task.FromResult(queue.Dequeue());
    }

    public Task<bool> TryEraseAsync(
        string storeName,
        string key,
        string etag,
        System.Threading.CancellationToken cancellationToken = default)
    {
        if (_terminalFailureKeys.Contains(key))
        {
            throw new InvalidOperationException(
                $"AC10 violation: production code attempted to erase key '{key}' after the retry budget was exhausted. " +
                "ReadModelWritePolicy must throw without further state-store traffic on retry exhaustion.");
        }

        TryEraseAttempts.Add(new EraseAttempt(storeName, key, etag));
        return Task.FromResult(true);
    }
}

internal sealed record ReadCall(string StoreName, string Key, System.Type ValueType);

internal sealed record SaveAttempt(
    string StoreName,
    string Key,
    object Value,
    string ETag,
    System.Type ValueType);

internal sealed record EraseAttempt(string StoreName, string Key, string ETag);

/// <summary>
/// Capturing <see cref="ILogger{TCategoryName}"/> that records the full structured-state
/// key/value pairs for source-generated log calls (risk R-007). Enables AC11 / AC3
/// assertions on individual log fields rather than brittle full-message substring matches.
/// </summary>
internal sealed class CapturingLogger<TCategory> : ILogger<TCategory>
{
    public List<CapturedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        System.Exception? exception,
        System.Func<TState, System.Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        IReadOnlyDictionary<string, object?>? structured = ExtractStructuredState(state);
        Entries.Add(new CapturedLog(
            logLevel,
            eventId,
            formatter(state, exception),
            state?.ToString() ?? string.Empty,
            structured,
            exception));
    }

    private static IReadOnlyDictionary<string, object?>? ExtractStructuredState<TState>(TState state)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
        {
            var dict = new Dictionary<string, object?>(pairs.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> pair in pairs)
            {
                dict[pair.Key] = pair.Value;
            }

            return dict;
        }

        return null;
    }
}

internal sealed record CapturedLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    string StateText,
    IReadOnlyDictionary<string, object?>? StructuredState,
    System.Exception? Exception);
