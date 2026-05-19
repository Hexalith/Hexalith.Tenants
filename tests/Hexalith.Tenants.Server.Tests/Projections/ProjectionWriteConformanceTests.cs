// <copyright file="ProjectionWriteConformanceTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

// Projection-write conformance tests completed by Story 10.4.
// Story: 10.4 (10-4-projection-write-conformance-and-recovery-tests).
// Risk: R-001 (silent last-writer-wins on projection:tenant-index:singleton) — score 9, BLOCK.
// Test design: _bmad-output/test-artifacts/test-design-epic-10.md.
//
// The fixture invokes the production TenantProjectionWritePolicy directly (R-008 rule).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

/// <summary>
/// Tier 1 conformance tests for the singleton tenant-index projection write path.
/// Covers R-001 (silent LWW on projection:tenant-index:singleton).
/// </summary>
public class ProjectionWriteConformanceTests
{
    [Fact]
    public async Task TenantDetail_ConflictThenSuccess_ReplaysIncomingBatchOnFreshReloadedStateAsync()
    {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        TenantReadModel externallyReloaded = ProjectionWriteConformanceFixture.SeedTenantReadModel(name: "Externally Updated");
        externallyReloaded.Members["external-user"] = TenantRole.TenantReader;
        externallyReloaded.Configuration["external"] = "kept";

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

        List<SaveAttempt> saves = fixture.StateStore.TrySaveAttempts
            .Where(a => a.Key == ProjectionWriteConformanceFixture.TenantProjectionKey)
            .ToList();
        saves.Count.ShouldBe(2);
        saves[0].ETag.ShouldBe("tenant-etag-1");
        saves[1].ETag.ShouldBe("tenant-etag-2");
        saves[0].Value.ShouldNotBeSameAs(saves[1].Value);
        saves[1].Value.ShouldBeSameAs(externallyReloaded);

        TenantReadModel saved = (TenantReadModel)saves[1].Value;
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
        //   Singleton index contains pre-existing tenant-A (attempt 1 reload).
        //   Concurrent write adds tenant-B externally between attempt 1 and attempt 2.
        //   Our incoming event applies tenant-C.
        //   On attempt 1, our save conflicts (ETag mismatch).
        //   On attempt 2, the reload returns the freshly persisted state (with tenant-B),
        //   we apply tenant-C on top, and save succeeds.
        //   Final saved index MUST contain {tenant-A, tenant-B, tenant-C} — zero loss.
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

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

        ProjectionEventDto[] events =
        [
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated("tenant-c", "Charlie", null, timestamp),
                "evt-c-1",
                timestamp),
        ];

        // ACT — drives production TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync.
        TenantIndexReadModel saved = await fixture.RunSingletonIndexConformanceAsync(events);

        // ASSERT — final state preserves ALL previously indexed tenants + applied incoming.
        saved.ShouldNotBeNull();
        saved.Tenants.Keys.ShouldBe(new[] { "tenant-a", "tenant-b", "tenant-c" }, ignoreOrder: true);
        saved.Tenants["tenant-a"].Name.ShouldBe("Alpha");
        saved.Tenants["tenant-b"].Name.ShouldBe("Bravo");
        saved.Tenants["tenant-c"].Name.ShouldBe("Charlie");

        // Attempt-count invariants (Story 10.4 AC#7).
        fixture.StateStore.ReadCalls
            .Count(c => c.Key == ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(2);
        fixture.StateStore.TrySaveAttempts
            .Count(s => s.Key == ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(2);

        // Per-attempt etag: attempt 1 uses stale etag, attempt 2 uses fresh etag.
        fixture.StateStore.TrySaveAttempts[0].ETag.ShouldBe("index-etag-1");
        fixture.StateStore.TrySaveAttempts[1].ETag.ShouldBe("index-etag-2");

        // Conflict warning emitted (EventId 100101); retry-exhausted NOT emitted.
        fixture.Logger.Entries.ShouldContain(e => e.EventId.Id == 100101 && e.Level == LogLevel.Warning);
        fixture.Logger.Entries.ShouldNotContain(e => e.EventId.Id == 100102);

        // R-008 fixture rule: production policy was invoked, not a test-only reimplementation.
        fixture.BindsToProductionPolicy().ShouldBeTrue();
    }

    [Fact]
    public async Task TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync()
    {
        // ARRANGE — 3 consecutive conflicts on the singleton index key.
        //   Production policy budget is MaxAttempts = 3 (TenantProjectionWritePolicy.MaxAttempts).
        //   On exhaustion, the helper throws InvalidOperationException AND emits the
        //   RetryExhausted structured log (EventId 100102, Error). Diagnostic MUST NOT
        //   contain payload bytes, tenant display names, user-controllable labels (R-007).
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        const string SensitiveTenantName = "Confidential Acquisitions Beta Tenant";
        const string SensitiveUserId = "actor-payload-secret-xyz";

        fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-1");
        fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-2");
        fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-3");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);

        ProjectionEventDto[] events =
        [
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated("tenant-x", SensitiveTenantName, null, timestamp),
                "evt-sensitive-1",
                timestamp,
                userId: SensitiveUserId),
        ];

        // ACT + ASSERT — exception type, message structure, no false success.
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await fixture.RunSingletonIndexConformanceAsync(events));

        exception.Message.ShouldContain("tenant index");
        exception.Message.ShouldContain("3 attempts");

        // Attempt-count invariants (Story 10.4 AC#7): exactly MaxAttempts reads + saves on this key.
        fixture.StateStore.ReadCalls
            .Count(c => c.Key == ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(3);
        fixture.StateStore.TrySaveAttempts
            .Count(s => s.Key == ProjectionWriteConformanceFixture.TenantIndexProjectionKey)
            .ShouldBe(3);

        // Structured-log emission shape (R-016): 2 conflict warnings + 1 retry-exhausted error.
        fixture.Logger.Entries
            .Count(e => e.EventId.Id == 100101 && e.Level == LogLevel.Warning)
            .ShouldBe(2);
        fixture.Logger.Entries
            .Count(e => e.EventId.Id == 100102 && e.Level == LogLevel.Error)
            .ShouldBe(1);

        // R-007 NEGATIVE CONTENT GATE — ZERO TOLERANCE.
        // No log entry (message OR formatted state) may contain sensitive payload content.
        foreach (CapturedLog entry in fixture.Logger.Entries)
        {
            entry.Message.ShouldNotContain(SensitiveTenantName);
            entry.Message.ShouldNotContain(SensitiveUserId);
            entry.StateText.ShouldNotContain(SensitiveTenantName);
            entry.StateText.ShouldNotContain(SensitiveUserId);
        }

        // R-008 fixture rule.
        fixture.BindsToProductionPolicy().ShouldBeTrue();
    }

    [Fact]
    public async Task TenantIndex_RetryExhaustionAfterTenantAndAuditSaves_FailsWithoutCrossKeyAtomicityClaimAsync()
    {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        fixture.EnqueueSuccessfulTenantDetailSave();
        fixture.EnqueueSuccessfulAuditSave();
        fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-1");
        fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-2");
        fixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-3");
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        fixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new TenantCreated(ProjectionWriteConformanceFixture.TenantId, "Acme", null, timestamp),
                "evt-created",
                timestamp));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.RunProjectionHandlerAsync(request));

        exception.Message.ShouldContain("tenant index");
        fixture.StateStore.TrySaveAttempts.Count(a => a.Key == ProjectionWriteConformanceFixture.TenantProjectionKey).ShouldBe(1);
        fixture.StateStore.TrySaveAttempts.Count(a => a.Key == ProjectionWriteConformanceFixture.TenantAuditProjectionKey).ShouldBe(1);
        fixture.StateStore.TrySaveAttempts.Count(a => a.Key == ProjectionWriteConformanceFixture.TenantIndexProjectionKey).ShouldBe(3);
        fixture.Logger.Entries.Count(e => e.EventId.Id == 100102 && e.Level == LogLevel.Error).ShouldBe(1);
    }

    [Fact]
    public async Task Audit_ConflictThenSuccess_PreservesPersistedAuthoritativeDuplicateAndOrdersByTimestampThenEventIdAsync()
    {
        var fixture = new ProjectionWriteConformanceFixture();
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        TenantAuditEntry persistedDuplicate = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "evt-added",
            nameof(UserRemovedFromTenant),
            timestamp.AddMinutes(2),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["source"] = "persisted" });
        TenantAuditEntry external = ProjectionWriteConformanceFixture.CreateAuditEntry(
            "evt-external",
            nameof(UserRoleChanged),
            timestamp.AddMinutes(3));

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
        ProjectionRequest request = ProjectionWriteConformanceFixture.CreateRequest(
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserAddedToTenant(ProjectionWriteConformanceFixture.TenantId, "user-1", TenantRole.TenantOwner),
                "evt-added",
                timestamp.AddMinutes(1)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRemovedFromTenant(ProjectionWriteConformanceFixture.TenantId, "user-2"),
                "evt-removed",
                timestamp.AddMinutes(3)),
            ProjectionWriteConformanceFixture.CreateEvent(
                new UserRoleChanged(ProjectionWriteConformanceFixture.TenantId, "user-3", TenantRole.TenantReader, TenantRole.TenantOwner),
                "evt-role",
                timestamp.AddMinutes(3)));

        _ = await fixture.RunProjectionHandlerAsync(request);

        List<SaveAttempt> auditSaves = fixture.StateStore.TrySaveAttempts
            .Where(a => a.Key == ProjectionWriteConformanceFixture.TenantAuditProjectionKey)
            .ToList();
        auditSaves.Count.ShouldBe(2);
        auditSaves[0].ETag.ShouldBe("audit-etag-1");
        auditSaves[1].ETag.ShouldBe("audit-etag-2");

        TenantAuditReadModel saved = (TenantAuditReadModel)auditSaves[1].Value;
        saved.Entries.Select(e => e.EventId).ShouldBe(["evt-added", "evt-external", "evt-removed", "evt-role"]);
        TenantAuditEntry savedDuplicate = saved.Entries.Single(e => e.EventId == "evt-added");
        savedDuplicate.EventType.ShouldBe(nameof(UserRemovedFromTenant));
        savedDuplicate.NarrativePayload["source"].ShouldBe("persisted");
    }

    [Fact]
    public async Task Audit_ReplayAfterAuditSaveAndLaterIndexFailure_DoesNotDuplicateEntriesAsync()
    {
        ProjectionRequest request = CreateAccessChangeRequest();
        var firstFixture = new ProjectionWriteConformanceFixture();
        firstFixture.EnqueueSuccessfulTenantDetailSave();
        firstFixture.EnqueueSuccessfulAuditSave();
        firstFixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-1");
        firstFixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-2");
        firstFixture.StateStore.EnqueueRead<TenantIndexReadModel>(
            ProjectionWriteConformanceFixture.TenantIndexProjectionKey,
            null,
            "index-etag-3");
        firstFixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        firstFixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);
        firstFixture.StateStore.EnqueueTrySave(ProjectionWriteConformanceFixture.TenantIndexProjectionKey, false);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => firstFixture.RunProjectionHandlerAsync(request));

        TenantAuditReadModel persistedAudit = (TenantAuditReadModel)firstFixture.StateStore.TrySaveAttempts
            .Single(a => a.Key == ProjectionWriteConformanceFixture.TenantAuditProjectionKey)
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

        TenantAuditReadModel replaySaved = (TenantAuditReadModel)replayFixture.StateStore.TrySaveAttempts
            .Single(a => a.Key == ProjectionWriteConformanceFixture.TenantAuditProjectionKey)
            .Value;
        replaySaved.Entries.Select(e => e.EventId).ShouldBe(["evt-added", "evt-removed", "evt-role"]);
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
