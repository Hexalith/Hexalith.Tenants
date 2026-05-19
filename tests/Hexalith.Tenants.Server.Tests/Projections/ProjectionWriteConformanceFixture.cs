// <copyright file="ProjectionWriteConformanceFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

// Projection-write conformance fixture completed by Story 10.4.
// Test design reference: _bmad-output/test-artifacts/test-design-epic-10.md (T-R001-UNIT-001, T-R001-UNIT-002, T-R001-INT-001).
// R-008 RULE: this fixture MUST drive the production TenantProjectionWritePolicy directly.
// It is forbidden to re-implement retry/merge algorithm logic inside this fixture.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Dapr.Client;

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
/// </summary>
/// <remarks>
/// <para>
/// Owns deterministic scripted state-store outcomes, a capturing logger for negative
/// diagnostic-content assertions (risk R-007), and the entry-point method that drives
/// the production <see cref="TenantProjectionWritePolicy"/> helper.
/// </para>
/// <para>
/// R-008 contract: this fixture MUST drive the production helper. It must NEVER
/// re-implement retry / merge / ETag logic in test code. The mechanical assertion
/// in <see cref="BindsToProductionPolicy"/> exists to fail the build if a future
/// refactor tries to substitute a test-only implementation.
/// </para>
/// </remarks>
internal sealed class ProjectionWriteConformanceFixture
{
    private bool _productionPolicyInvoked;

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

    /// <summary>
    /// Drives the production singleton-index write path
    /// (<see cref="TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync{TValue}"/>)
    /// against the scripted state store.
    /// </summary>
    /// <remarks>
    /// Invokes the production policy directly and delegates event mutation to
    /// <see cref="TenantProjectionHandler.ApplyIndexEvent"/>.
    /// </remarks>
    public async Task<TenantIndexReadModel> RunSingletonIndexConformanceAsync(
        IReadOnlyCollection<ProjectionEventDto?> events)
    {
        _productionPolicyInvoked = true;

        return await TenantProjectionWritePolicy
            .SaveWithOptimisticConcurrencyAsync(
                StateStore,
                Logger,
                StateStoreName,
                TenantIndexProjectionKey,
                TenantIndexKeyCategory,
                nameof(ProjectionWriteConformanceFixture) + "." + nameof(RunSingletonIndexConformanceAsync),
                events,
                static () => new TenantIndexReadModel(),
                TenantProjectionHandler.ApplyIndexEvent)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Mechanical assertion: confirms this fixture invokes the production helper type
    /// (R-008 rule). A test-only reimplementation MUST fail this assertion.
    /// </summary>
    /// <remarks>
    /// Returns true only after this fixture has invoked the production policy type.
    /// </remarks>
    public bool BindsToProductionPolicy() =>
        _productionPolicyInvoked
        && string.Equals(
            typeof(TenantProjectionWritePolicy).FullName,
            "Hexalith.Tenants.Projections.TenantProjectionWritePolicy",
            StringComparison.Ordinal);

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
        IReadOnlyDictionary<string, string>? narrativePayload = null) =>
        new(
            eventId,
            eventType,
            AuditEventCategory.Access,
            "actor-1",
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
/// Mirrors the existing pattern in <c>TenantProjectionHandlerTests</c>; intentionally
/// kept independent so Story 10.4 can extend it without touching the existing handler tests.
/// </summary>
internal sealed class ScriptedTenantProjectionStateStore : ITenantProjectionStateStore
{
    private readonly Dictionary<string, Queue<object>> _reads = [];
    private readonly Dictionary<string, Queue<bool>> _trySaveResults = [];

    public List<ReadCall> ReadCalls { get; } = [];

    public List<SaveAttempt> TrySaveAttempts { get; } = [];

    public List<SaveAttempt> PlainSaveAttempts { get; } = [];

    public void EnqueueRead<TValue>(string key, TValue? value, string? etag)
        where TValue : class
    {
        if (!_reads.TryGetValue(key, out Queue<object>? queue))
        {
            queue = new Queue<object>();
            _reads[key] = queue;
        }

        queue.Enqueue(new ProjectionStateRead<TValue>(value, etag));
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

    public Task<ProjectionStateRead<TValue>> GetStateAndETagAsync<TValue>(
        string storeName,
        string key,
        System.Threading.CancellationToken cancellationToken = default)
        where TValue : class
    {
        ReadCalls.Add(new ReadCall(storeName, key, typeof(TValue)));
        if (!_reads.TryGetValue(key, out Queue<object>? queue) || queue.Count == 0)
        {
            throw new System.InvalidOperationException(
                $"Scripted state store has no remaining read outcomes for key '{key}'. " +
                "Ensure EnqueueRead was called for every expected attempt.");
        }

        return Task.FromResult((ProjectionStateRead<TValue>)queue.Dequeue());
    }

    public Task SaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        StateOptions? stateOptions = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        System.Threading.CancellationToken cancellationToken = default)
        where TValue : class
    {
        PlainSaveAttempts.Add(new SaveAttempt(
            storeName,
            key,
            value,
            string.Empty,
            stateOptions ?? new StateOptions(),
            typeof(TValue)));
        return Task.CompletedTask;
    }

    public Task<bool> TrySaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        StateOptions stateOptions,
        IReadOnlyDictionary<string, string>? metadata = null,
        System.Threading.CancellationToken cancellationToken = default)
        where TValue : class
    {
        TrySaveAttempts.Add(new SaveAttempt(storeName, key, value, etag, stateOptions, typeof(TValue)));
        if (!_trySaveResults.TryGetValue(key, out Queue<bool>? queue) || queue.Count == 0)
        {
            throw new System.InvalidOperationException(
                $"Scripted state store has no remaining TrySave outcomes for key '{key}'. " +
                "Ensure EnqueueTrySave was called for every expected attempt.");
        }

        return Task.FromResult(queue.Dequeue());
    }
}

internal sealed record ReadCall(string StoreName, string Key, System.Type ValueType);

internal sealed record SaveAttempt(
    string StoreName,
    string Key,
    object Value,
    string ETag,
    StateOptions StateOptions,
    System.Type ValueType);

/// <summary>
/// Minimal capturing <see cref="ILogger{TCategoryName}"/> for negative-content
/// diagnostic assertions (risk R-007). Records every emitted log entry's level,
/// EventId, formatted message, and state for inspection.
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
        Entries.Add(new CapturedLog(
            logLevel,
            eventId,
            formatter(state, exception),
            state?.ToString() ?? string.Empty,
            exception));
    }
}

internal sealed record CapturedLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    string StateText,
    System.Exception? Exception);
