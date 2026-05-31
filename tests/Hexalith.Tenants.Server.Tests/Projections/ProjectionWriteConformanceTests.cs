// <copyright file="ProjectionWriteConformanceTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

// Projection-write conformance tests completed by Story 10.4.
// Story: 10.4 (10-4-projection-write-conformance-and-recovery-tests).
// Risk: R-001 (silent last-writer-wins on projection:tenant-index:singleton) — score 9, BLOCK.
// Test design: _bmad-output/test-artifacts/test-design-epic-10.md.
//
// All tests drive production behavior through TenantProjectionHandler.ProjectAsync,
// which routes into TenantProjectionWritePolicy. There is no direct test-only invocation path.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

/// <summary>
/// Conformance tests for the three projection write paths (tenant detail, tenant index, audit).
/// Covers R-001 (silent LWW on projection:tenant-index:singleton) and supporting Story 10.4 ACs.
/// </summary>
public class ProjectionWriteConformanceTests
{
    private const int ConflictEventIdInt = 100101;
    private const int RetryExhaustedEventIdInt = 100102;

    [Fact]
    public async Task TenantDetail_ConflictThenSuccess_ReplaysIncomingBatchOnFreshReloadedStateAsync()
    {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        // AC2 "exactly once": build the external reload state by replaying real projection events
        // through TenantReadModel.Apply, rather than mutating dictionaries directly. A regression
        // that double-applied an event would corrupt this seed via Apply itself.
        TenantReadModel externallyReloaded = ProjectionWriteConformanceFixture.SeedTenantReadModel(name: "Externally Updated");
        externallyReloaded.Apply(new UserAddedToTenant(
            ProjectionWriteConformanceFixture.TenantId,
            "external-user",
            TenantRole.TenantReader));
        externallyReloaded.Apply(new TenantConfigurationSet(
            ProjectionWriteConformanceFixture.TenantId,
            "external",
            "kept"));

        fixture.StateStore.EnqueueRead<TenantReadModel>(
            ProjectionWriteConformanceFixture.TenantProjectionKey,
            null,
            "tenant-etag-1");
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantProjectionKey,
            externallyReloaded,
            "tenant-etag-2");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, true);
        fixture.EnqueueSuccessfulAuditSave();
        fixture.EnqueueSuccessfulIndexSave();
        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                "evt-created",
                timestamp),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantOwner),
                "evt-added",
                timestamp.AddMinutes(1)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantConfigurationSet(ProjectionWriteConformanceFixture.TenantId, "feature", "enabled"),
                "evt-config",
                timestamp.AddMinutes(2)));

        _ = await fixture.RunProjectionHandlerAsync(request);

        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantProjectionKey).ShouldBe(2);
        SaveAttempt save1 = fixture.GetSaveAttempt(ProjectionWriteConformanceFixture.TenantProjectionKey, 0);
        SaveAttempt save2 = fixture.GetSaveAttempt(ProjectionWriteConformanceFixture.TenantProjectionKey, 1);
        save1.ETag.ShouldBe("tenant-etag-1");
        save2.ETag.ShouldBe("tenant-etag-2");
        save1.Value.ShouldNotBeSameAs(save2.Value);
        save2.Value.ShouldBeSameAs(externallyReloaded);

        TenantReadModel saved = (TenantReadModel)save2.Value;
        saved.TenantId.ShouldBe(ProjectionWriteConformanceFixture.TenantId);
        saved.Name.ShouldBe("Acme");
        saved.Members["external-user"].ShouldBe(TenantRole.TenantReader);
        saved.Members["user-1"].ShouldBe(TenantRole.TenantOwner);
        saved.Configuration["external"].ShouldBe("kept");
        saved.Configuration["feature"].ShouldBe("enabled");
    }

    [Fact]
    public async Task TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync()
    {
        // ARRANGE — R-001 BLOCKER scenario:
        //   Singleton index contains pre-existing tenant-a (attempt 1 reload).
        //   Concurrent write adds tenant-b externally between attempts 1 and 2.
        //   Our incoming event applies tenant-c.
        //   Attempt 1 save conflicts (ETag mismatch).
        //   Attempt 2 reload returns the freshly persisted state (with tenant-b),
        //   we apply tenant-c on top, and save succeeds.
        //   Final saved index MUST contain {tenant-a, tenant-b, tenant-c} — zero loss.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        const string IncomingTenantId = "tenant-c";
        string tenantDetailKey = $"projection:tenants:{IncomingTenantId}";
        string auditKey = $"audit:{IncomingTenantId}";

        // Tenant-detail (tenant-c) and audit writes succeed on first try.
        fixture.StateStore.EnqueueRead<TenantReadModel>(tenantDetailKey, null, null);
        fixture.StateStore.EnqueueTrySave(tenantDetailKey, true);
        fixture.StateStore.EnqueueRead<TenantAuditReadModel>(auditKey, null, null);
        fixture.StateStore.EnqueueTrySave(auditKey, true);

        // Attempt 1: reload sees A only, with stale etag.
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            ProjectionWriteConformanceFixture.SeedIndexWith(("tenant-a", "Alpha")),
            "index-etag-1");

        // Attempt 2: reload sees A + B (B was added externally), with fresh etag.
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            ProjectionWriteConformanceFixture.SeedIndexWith(("tenant-a", "Alpha"), ("tenant-b", "Bravo")),
            "index-etag-2");

        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, true);

        ProjectionRequest request = new(
            IncomingTenantId,
            "tenants",
            IncomingTenantId,
            [
                ProjectionWriteConformanceFixture.CreateEvent(
                    new TenantCreated(IncomingTenantId, "Charlie", null, timestamp),
                    "evt-c-1",
                    timestamp),
            ]);

        _ = await fixture.RunProjectionHandlerAsync(request);

        // Final state preserves ALL previously indexed tenants + applied incoming.
        SaveAttempt finalIndexSave = fixture.GetSaveAttempt(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, 1);
        TenantIndexReadModel saved = (TenantIndexReadModel)finalIndexSave.Value;
        saved.Tenants.Keys.ShouldBe(new[] { "tenant-a", "tenant-b", IncomingTenantId }, ignoreOrder: true);
        saved.Tenants["tenant-a"].Name.ShouldBe("Alpha");
        saved.Tenants["tenant-b"].Name.ShouldBe("Bravo");
        saved.Tenants[IncomingTenantId].Name.ShouldBe("Charlie");

        // Attempt-count invariants (AC7).
        fixture.GetReadAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey).ShouldBe(2);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey).ShouldBe(2);
        fixture.GetSaveAttempt(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, 0).ETag.ShouldBe("index-etag-1");
        fixture.GetSaveAttempt(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, 1).ETag.ShouldBe("index-etag-2");

        // Conflict warning emitted; retry-exhausted NOT emitted.
        fixture.GetLogEntries(ConflictEventIdInt).ShouldContain(e => e.Level == LogLevel.Warning);
        fixture.GetLogEntries(RetryExhaustedEventIdInt).ShouldBeEmpty();
    }

    [Fact]
    public async Task TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync()
    {
        // R-007 negative-content gate (AC3, AC11). Inject sensitive content into channels
        // the production policy MUST NOT log: tenant Name (payload), userId (event field),
        // TenantConfigurationSet.Value (payload). Assert: no log entry's message, state
        // text, or any structured-state value contains the sensitive probes.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        const string SensitiveTenantName = "Confidential Acquisitions Beta Tenant";
        const string SensitiveUserId = "actor-payload-secret-xyz";
        const string SensitiveConfigValue = "api-key-leak-canary-7f9c";
        const string SafeCorrelationId = "corr-test-1";
        const string SafeMessageId = "evt-test-1";

        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.EnqueueSuccessfulAuditSave();
        for (int attempt = 0; attempt < TenantProjectionWritePolicy.MaxAttempts; attempt++)
        {
            fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
                ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
                null,
                $"index-etag-{attempt + 1}");
            fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        }

        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, SensitiveTenantName, null, timestamp),
                SafeMessageId,
                timestamp,
                correlationId: SafeCorrelationId,
                userId: SensitiveUserId),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantConfigurationSet(ProjectionWriteConformanceFixture.TenantId, "secret-config", SensitiveConfigValue),
                "evt-config-1",
                timestamp.AddSeconds(1),
                correlationId: SafeCorrelationId,
                userId: SensitiveUserId));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.RunProjectionHandlerAsync(request));

        // AC10 enforcement: production code MUST stop after exhaustion.
        fixture.StateStore.MarkTerminalFailure(ProjectionWriteConformanceFixture.TenantIndexProjectionKey);

        exception.Message.ShouldContain("tenant index");
        exception.Message.ShouldContain($"{TenantProjectionWritePolicy.MaxAttempts} attempts");

        // AC7 attempt-count invariants.
        fixture.GetReadAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(TenantProjectionWritePolicy.MaxAttempts);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(TenantProjectionWritePolicy.MaxAttempts);

        // Structured-log shape (R-016): (MaxAttempts-1) conflict warnings + 1 retry-exhausted error.
        IReadOnlyList<CapturedLog> conflicts = fixture.GetLogEntries(ConflictEventIdInt);
        IReadOnlyList<CapturedLog> exhausted = fixture.GetLogEntries(RetryExhaustedEventIdInt);
        conflicts.Count(e => e.Level == LogLevel.Warning).ShouldBe(TenantProjectionWritePolicy.MaxAttempts - 1);
        exhausted.Count(e => e.Level == LogLevel.Error).ShouldBe(1);

        // R-007 negative-content gate (AC3, AC11). Iterate over every captured log AND
        // every structured-state value to prove no sensitive content leaks through any
        // channel (formatted message OR individual key/value pairs).
        string[] forbidden = [SensitiveTenantName, SensitiveUserId, SensitiveConfigValue];
        foreach (CapturedLog entry in fixture.Logger.Entries)
        {
            foreach (string secret in forbidden)
            {
                entry.Message.ShouldNotContain(secret);
                entry.StateText.ShouldNotContain(secret);
                if (entry.StructuredState is not null)
                {
                    foreach (object? value in entry.StructuredState.Values)
                    {
                        string? text = value?.ToString();
                        if (text is not null)
                        {
                            text.ShouldNotContain(secret);
                        }
                    }
                }
            }
        }

        // Positive structured-field assertions (AC11 prefers structured over substring).
        CapturedLog exhaustedEntry = exhausted.Single();
        exhaustedEntry.StructuredState.ShouldNotBeNull();
        exhaustedEntry.StructuredState!["StateKeyCategory"].ShouldBe("tenant index");
        exhaustedEntry.StructuredState["AttemptCount"].ShouldBe(TenantProjectionWritePolicy.MaxAttempts);
        exhaustedEntry.StructuredState["MaxAttempts"].ShouldBe(TenantProjectionWritePolicy.MaxAttempts);
        exhaustedEntry.StructuredState["CorrelationId"].ShouldBe(SafeCorrelationId);
    }

    [Fact]
    public async Task TenantIndex_RetryExhaustionAfterTenantAndAuditSaves_FailsWithoutCrossKeyAtomicityClaimAsync()
    {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.EnqueueSuccessfulAuditSave();
        for (int attempt = 0; attempt < TenantProjectionWritePolicy.MaxAttempts; attempt++)
        {
            fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
                ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
                null,
                $"index-etag-{attempt + 1}");
            fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        }

        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                "evt-created",
                timestamp));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.RunProjectionHandlerAsync(request));

        fixture.StateStore.MarkTerminalFailure(ProjectionWriteConformanceFixture.TenantIndexProjectionKey);

        exception.Message.ShouldContain("tenant index");

        // AC6: tenant-detail and audit were saved BEFORE index exhaustion — no rollback.
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantProjectionKey).ShouldBe(1);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantAuditProjectionKey).ShouldBe(1);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(TenantProjectionWritePolicy.MaxAttempts);

        fixture.GetLogEntries(RetryExhaustedEventIdInt)
            .Count(e => e.Level == LogLevel.Error)
            .ShouldBe(1);
    }

    [Fact]
    public async Task TenantDetail_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync()
    {
        // P14 / D3 / Task line 47: tenant-detail retry-exhaustion analog to the index test.
        // After tenant-detail exhausts, no audit or index writes must occur.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        for (int attempt = 0; attempt < TenantProjectionWritePolicy.MaxAttempts; attempt++)
        {
            fixture.StateStore.EnqueueRead<TenantReadModel>(
                ProjectionWriteConformanceFixture.TenantProjectionKey,
                null,
                $"tenant-etag-{attempt + 1}");
            fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, false);
        }

        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                "evt-created",
                timestamp));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.RunProjectionHandlerAsync(request));

        fixture.StateStore.MarkTerminalFailure(ProjectionWriteConformanceFixture.TenantProjectionKey);

        exception.Message.ShouldContain("tenant read-model");
        exception.Message.ShouldContain($"{TenantProjectionWritePolicy.MaxAttempts} attempts");

        // AC7: exactly MaxAttempts reads + saves; no audit or index traffic after exhaustion.
        fixture.GetReadAttemptCount(ProjectionWriteConformanceFixture.TenantProjectionKey)
            .ShouldBe(TenantProjectionWritePolicy.MaxAttempts);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantProjectionKey)
            .ShouldBe(TenantProjectionWritePolicy.MaxAttempts);
        fixture.GetReadAttemptCount(ProjectionWriteConformanceFixture.TenantAuditProjectionKey).ShouldBe(0);
        fixture.GetReadAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey).ShouldBe(0);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantAuditProjectionKey).ShouldBe(0);
        fixture.GetSaveAttemptCount(ProjectionWriteConformanceFixture.TenantIndexProjectionKey).ShouldBe(0);

        fixture.GetLogEntries(RetryExhaustedEventIdInt)
            .Count(e => e.Level == LogLevel.Error)
            .ShouldBe(1);
    }

    [Fact]
    public async Task Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicateAndOrdersByTimestampThenEventIdAsync()
    {
        // P8: prove "persisted wins" against payload MISMATCH. Persisted entry uses
        // a DISTINCT ActorId AND a distinct narrative payload from the incoming event.
        // A wholesale-replace regression would show the incoming ActorId, EventType, or
        // narrative-payload contents on the saved duplicate — caught by the assertions below.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        TenantAuditEntry persistedDuplicate = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "evt-added",
            nameof(UserRemovedFromTenant),
            timestamp.AddMinutes(2),
            actorId: "actor-persisted",
            narrativePayload: new Dictionary<string, string>(StringComparer.Ordinal) { ["source"] = "persisted" });
        TenantAuditEntry external = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "evt-external",
            nameof(UserRoleChanged),
            timestamp.AddMinutes(3),
            actorId: "actor-external");

        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantAuditProjectionKey,
            ProjectionWriteConformanceFixture.SeedAuditWith(persistedDuplicate),
            "audit-etag-1");
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantAuditProjectionKey,
            ProjectionWriteConformanceFixture.SeedAuditWith(persistedDuplicate, external),
            "audit-etag-2");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, true);
        fixture.EnqueueSuccessfulIndexSave();

        // Incoming "evt-added" is a UserAddedToTenant by actor-incoming with completely
        // different EventType/ActorId/Timestamp/payload from the persisted record.
        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantOwner),
                "evt-added",
                timestamp.AddMinutes(1),
                userId: "actor-incoming"),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRemovedFromTenant(ProjectionWriteConformanceFixture.TenantId, "user-2"),
                "evt-removed",
                timestamp.AddMinutes(3),
                userId: "actor-incoming"),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRoleChanged(ProjectionWriteConformanceFixture.TenantId, "user-3", TenantRole.TenantReader, TenantRole.TenantOwner),
                "evt-role",
                timestamp.AddMinutes(3),
                userId: "actor-incoming"));

        _ = await fixture.RunProjectionHandlerAsync(request);

        SaveAttempt finalAuditSave = fixture.GetSaveAttempt(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, 1);
        TenantAuditReadModel saved = (TenantAuditReadModel)finalAuditSave.Value;
        saved.Entries.Select(e => e.EventId).ShouldBe(["evt-added", "evt-external", "evt-removed", "evt-role"]);

        // P8 wholesale-replace check: every field of the duplicate entry MUST come from
        // the PERSISTED record, not from incoming.
        TenantAuditEntry savedDuplicate = saved.Entries.Single(e => e.EventId == "evt-added");
        savedDuplicate.EventType.ShouldBe(nameof(UserRemovedFromTenant));
        savedDuplicate.ActorId.ShouldBe("actor-persisted");
        savedDuplicate.Timestamp.ShouldBe(timestamp.AddMinutes(2));
        savedDuplicate.NarrativePayload.Count.ShouldBe(1);
        savedDuplicate.NarrativePayload["source"].ShouldBe("persisted");
    }

    [Fact]
    public async Task Audit_ReplayAfterAuditSaveAndLaterIndexFailure_DoesNotDuplicateEntriesAsync()
    {
        ProjectionRequest request = CreateAccessChangeRequest();
        var firstFixture = new ProjectionWriteConformanceFixture();
        firstFixture.EnqueueSuccessfulTenantDetailSave();
        firstFixture.EnqueueSuccessfulAuditSave();
        for (int attempt = 0; attempt < TenantProjectionWritePolicy.MaxAttempts; attempt++)
        {
            firstFixture.StateStore.EnqueueRead<TenantIndexReadModel>(
                ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
                null,
                $"index-etag-{attempt + 1}");
            firstFixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        }

        _ = await Should.ThrowAsync<InvalidOperationException>(() => firstFixture.RunProjectionHandlerAsync(request));

        TenantAuditReadModel persistedAudit = (TenantAuditReadModel)firstFixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, 0)
            .Value;
        var replayFixture = new ProjectionWriteConformanceFixture();
        replayFixture.EnqueueSuccessfulTenantDetailSave();
        replayFixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantAuditProjectionKey,
            persistedAudit,
            "audit-etag-replay");
        replayFixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, true);
        replayFixture.EnqueueSuccessfulIndexSave();

        _ = await replayFixture.RunProjectionHandlerAsync(request);

        TenantAuditReadModel replaySaved = (TenantAuditReadModel)replayFixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, 0)
            .Value;
        replaySaved.Entries.Select(e => e.EventId).ShouldBe(["evt-added", "evt-removed", "evt-role"]);
    }

    [Fact]
    public async Task TenantIndex_ReplayAfterPartialSuccess_DoesNotDuplicateOrLoseEntriesAsync()
    {
        // P11 / AC6: tenant-index replay idempotency. After a successful index save,
        // replaying the same projection batch must not duplicate or lose index entries.
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                "evt-created",
                timestamp),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantOwner),
                "evt-added",
                timestamp.AddMinutes(1)));

        // First run: writes through successfully.
        var firstFixture = new ProjectionWriteConformanceFixture();
        firstFixture.EnqueueSuccessfulTenantDetailSave();
        firstFixture.EnqueueSuccessfulAuditSave();
        firstFixture.EnqueueSuccessfulIndexSave();
        _ = await firstFixture.RunProjectionHandlerAsync(request);

        TenantIndexReadModel persistedIndex = (TenantIndexReadModel)firstFixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, 0)
            .Value;
        persistedIndex.Tenants[ProjectionWriteConformanceFixture.TenantId].Name.ShouldBe("Acme");
        persistedIndex.UserTenants["user-1"][ProjectionWriteConformanceFixture.TenantId].ShouldBe(TenantRole.TenantOwner);

        // Replay: same projection batch, persisted index returned as reload state.
        var replayFixture = new ProjectionWriteConformanceFixture();
        replayFixture.EnqueueSuccessfulTenantDetailSave();
        replayFixture.EnqueueSuccessfulAuditSave();
        replayFixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            persistedIndex,
            "index-etag-replay");
        replayFixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, true);

        _ = await replayFixture.RunProjectionHandlerAsync(request);

        TenantIndexReadModel replaySaved = (TenantIndexReadModel)replayFixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, 0)
            .Value;
        // Idempotent Apply: TenantCreated skipped if exists; UserAddedToTenant overwrites by user-id.
        replaySaved.Tenants.Count.ShouldBe(1);
        replaySaved.Tenants[ProjectionWriteConformanceFixture.TenantId].Name.ShouldBe("Acme");
        replaySaved.UserTenants["user-1"].Count.ShouldBe(1);
        replaySaved.UserTenants["user-1"][ProjectionWriteConformanceFixture.TenantId].ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task Conformance_MixedLifecycleAndMembershipAndConfiguration_OrderingPreservedAsync()
    {
        // P13 / D2 / AC4: exercise all 9 event types under conflict reload.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset t0 = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        TenantReadModel externallyReloaded = ProjectionWriteConformanceFixture.SeedTenantReadModel(name: "Acme Original");

        fixture.StateStore.EnqueueRead<TenantReadModel>(
            ProjectionWriteConformanceFixture.TenantProjectionKey,
            null,
            "tenant-etag-1");
        fixture.StateStore.EnqueueRead(
            ProjectionWriteConformanceFixture.TenantProjectionKey,
            externallyReloaded,
            "tenant-etag-2");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantProjectionKey, true);
        fixture.EnqueueSuccessfulAuditSave();
        fixture.EnqueueSuccessfulIndexSave();

        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, t0),
                "evt-1-created",
                t0),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantReader),
                "evt-2-added",
                t0.AddSeconds(1)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRoleChanged(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantReader, TenantRole.TenantOwner),
                "evt-3-role",
                t0.AddSeconds(2)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantConfigurationSet(ProjectionWriteConformanceFixture.TenantId, "feature.x", "on"),
                "evt-4-cfg-set",
                t0.AddSeconds(3)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantUpdated(ProjectionWriteConformanceFixture.TenantId, "Acme Renamed", null, DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")),
                "evt-5-updated",
                t0.AddSeconds(4)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantDisabled(ProjectionWriteConformanceFixture.TenantId, t0.AddSeconds(5)),
                "evt-6-disabled",
                t0.AddSeconds(5)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantEnabled(ProjectionWriteConformanceFixture.TenantId, t0.AddSeconds(6)),
                "evt-7-enabled",
                t0.AddSeconds(6)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantConfigurationRemoved(ProjectionWriteConformanceFixture.TenantId, "feature.x"),
                "evt-8-cfg-removed",
                t0.AddSeconds(7)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRemovedFromTenant(ProjectionWriteConformanceFixture.TenantId, "user-1"),
                "evt-9-removed",
                t0.AddSeconds(8)));

        _ = await fixture.RunProjectionHandlerAsync(request);

        TenantReadModel saved = (TenantReadModel)fixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantProjectionKey, 1).Value;
        saved.Name.ShouldBe("Acme Renamed");
        saved.Status.ShouldBe(TenantStatus.Active);
        saved.Configuration.ContainsKey("feature.x").ShouldBeFalse();
        saved.Members.ContainsKey("user-1").ShouldBeFalse();
    }

    [Fact]
    public async Task Audit_MalformedPayloadPreserved_AndInvariantFailureAbortsBeforeWritesAsync()
    {
        // P15a / D4 / Task line 60: malformed JSON payloads are skipped silently by
        // TenantAuditProjection.ProjectAuditEvents (try/catch on JsonException).
        // Invariant violations (missing UserId) propagate and MUST abort before any save.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        // Malformed payload bound to an audit-only event type (GlobalAdministratorSet).
        // ApplyEvent and ApplyIndexEvent skip unrecognized names, so only TenantAuditProjection
        // tries to deserialize — and its try/catch on JsonException makes the projection
        // resilient to malformed audit-only payloads.
        ProjectionEventDto malformed = new(
            typeof(GlobalAdministratorSet).FullName!,
            Encoding.UTF8.GetBytes("{ this is not valid json"),
            "json",
            1,
            timestamp,
            "corr-1",
            "evt-malformed",
            "actor-1");
        ProjectionEventDto valid = ProjectionWriteConformanceFixture.CreateEvent(
            new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantReader),
            "evt-valid",
            timestamp.AddSeconds(1));

        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.EnqueueSuccessfulAuditSave();
        fixture.EnqueueSuccessfulIndexSave();

        ProjectionRequest mixedRequest = ProjectionWriteConformanceFixture.CreateRequest(malformed, valid);
        _ = await fixture.RunProjectionHandlerAsync(mixedRequest);

        TenantAuditReadModel auditSaved = (TenantAuditReadModel)fixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, 0).Value;
        auditSaved.Entries.Select(e => e.EventId).ShouldBe(["evt-valid"]);

        // Invariant violation must throw BEFORE any save attempt.
        var invariantFixture = new ProjectionWriteConformanceFixture();
        ProjectionEventDto invariantViolating = new(
            typeof(UserAddedToTenant).FullName!,
            JsonSerializer.SerializeToUtf8Bytes(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-2", TenantRole.TenantReader)),
            "json",
            1,
            timestamp,
            "corr-1",
            "evt-no-user",
            UserId: null);
        ProjectionRequest invariantRequest = ProjectionWriteConformanceFixture.CreateRequest(invariantViolating);
        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => invariantFixture.RunProjectionHandlerAsync(invariantRequest));
        invariantFixture.StateStore.ReadCalls.ShouldBeEmpty();
        invariantFixture.StateStore.TrySaveAttempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task Audit_MixedEventTypeOrdering_PreservedAcrossConflictReloadAsync()
    {
        // P15b / D4: audit ordering across mixed lifecycle + membership + configuration
        // events under conflict reload. Final audit must sort by (Timestamp, EventId).
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset t0 = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        TenantAuditEntry persistedA = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "audit-a", nameof(TenantCreated), t0, actorId: "actor-persisted");
        TenantAuditEntry persistedB = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "audit-b", nameof(UserAddedToTenant), t0.AddSeconds(2), actorId: "actor-persisted");
        TenantAuditReadModel preReload = ProjectionWriteConformanceFixture.SeedAuditWith(persistedA, persistedB);
        TenantAuditEntry concurrentExternal = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "audit-mid", nameof(UserRoleChanged), t0.AddSeconds(1), actorId: "actor-external");
        TenantAuditReadModel postReload = ProjectionWriteConformanceFixture.SeedAuditWith(persistedA, concurrentExternal, persistedB);

        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.StateStore.EnqueueRead(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, preReload, "audit-etag-1");
        fixture.StateStore.EnqueueRead(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, postReload, "audit-etag-2");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, true);
        fixture.EnqueueSuccessfulIndexSave();

        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantConfigurationSet(ProjectionWriteConformanceFixture.TenantId, "feature.x", "on"),
                "incoming-cfg",
                t0.AddSeconds(3)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantUpdated(ProjectionWriteConformanceFixture.TenantId, "Acme Renamed", null, DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")),
                "incoming-update",
                t0.AddSeconds(4)));

        _ = await fixture.RunProjectionHandlerAsync(request);

        TenantAuditReadModel saved = (TenantAuditReadModel)fixture
            .GetSaveAttempt(ProjectionWriteConformanceFixture.TenantAuditProjectionKey, 1).Value;
        // Sort by (Timestamp, EventId Ordinal):
        //   audit-a (t0), audit-mid (t0+1s), audit-b (t0+2s), incoming-cfg (t0+3s), incoming-update (t0+4s).
        saved.Entries.Select(e => e.EventId)
            .ShouldBe(["audit-a", "audit-mid", "audit-b", "incoming-cfg", "incoming-update"]);
    }

    private static ProjectionRequest CreateAccessChangeRequest()
    {
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        return ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantReader),
                "evt-added",
                timestamp.AddMinutes(1)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRemovedFromTenant(ProjectionWriteConformanceFixture.TenantId, "user-2"),
                "evt-removed",
                timestamp.AddMinutes(2)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRoleChanged(ProjectionWriteConformanceFixture.TenantId, "user-3", TenantRole.TenantReader, TenantRole.TenantOwner),
                "evt-role",
                timestamp.AddMinutes(3)));
    }
}
