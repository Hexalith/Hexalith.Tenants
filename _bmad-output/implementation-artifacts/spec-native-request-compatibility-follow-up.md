---
title: 'Repair native catalog request compatibility and audited offline preparation'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
baseline_commit: '60daa5198169f093b11abd1bafca5a45afe254fe'
owning_repository_baseline_commit: '56a57c5ecc590bbea56567f1a0daa9dc512c56bc'
---

## Intent

Complete the existing unchanged21-vector-plus-smoke native runtime gate against authentic public EventStore4.0.4 and Tenants7.0.0. Hosted run41628872425 proved genuine three-participant serving catalogs in16 cases and owned cleanup in all22, but exposed request compatibility and isolated advisory-refresh failures. The user authorized necessary CI repairs, pushes and unattended release. No privacy-policy exception is inferred from this authorization.

## Boundaries

The generic production query transport must let the committed native catalog select its optional actor target. Preserve contract descriptors, artifact approval, inventories, all vector bytes and semantic request fields. Do not rewrite requests in a test gateway or add module-specific production branches. Keep server catalog admission, authentication and persisted business assertions.

The real fixture administrator may carry the SDK assertion hint needed to invoke independent owner verification. The hint cannot bootstrap membership or substitute for current authenticated persisted source. Exact tenant/domain/permission grants remain bounded. Actual public SDK plus genuine Tenants owner controls must prove member success, nonmember stripping and unavailable-source refusal; signed/forged token controls remain enforced.

The isolated native reader may omit a redundant network advisory refresh only after the identical package ID/version/content-hash graph has passed online vulnerability auditing. Retain and verify source-bound audit evidence before that reader runs; absent, changed or unaudited inputs must fail. No warning suppression, ignored feed failures, fabricated cache or dependency substitution.

No local business AppHost/resources or nested submodule initialization. Preserve concurrent commits and source selections. Root owns review, commits and remote qualification.

## File List

- tests/Hexalith.Tenants.Server.Tests/Authorization/TenantsGlobalAdministratorVerifierTests.cs
- references/Hexalith.McpCli
- references/Hexalith.Builds
- references/Hexalith.EventStore
- references/Hexalith.FrontComposer
- references/Hexalith.Memories
- references/Hexalith.Platform

## Verification

Run focused Core query transport, fixture identity, reader audit/receipt and actual Root owner assertion controls. Preserve offline complete-vector validation and strict published archive checks. Review the complete owning-repository follow-up delta from56a57c5 together with the Root owner regression delta from60daa519 using the three required independent layers. Parent verifies final affected suites after triage. Full acceptance still requires a successful hosted exact-source native21-plus-smoke job with persisted effects and owned cleanup in every case.

Implementation evidence: `/tmp/tenants-release-20261103-07JciR/owner-native-assertion-handoff.json` and the native implementer's follow-up handoff. Earlier scoped story/review evidence remains in `spec-native-mcpcli-runtime-cohort.md` and `spec-qualify-published-mcpcli-7.0.0-enrollment.md`.

## Tasks

- [x] Actual generic query transport and verified fixture assertion corrected without changing approved inputs.
- [x] Exact reader graph online audit retained and checked before isolated preparation.
- [x] Actual Root owner plus public SDK positive and negative assertion controls pass.
- [x] Three reviews collected, each finding triaged and surviving fixes verified.
- [ ] Exact hosted21-plus-smoke native acceptance passes with full persisted behavior and cleanup.

## Review Triage Log

### Hosted correction and superseding SDK contract

The locally verified generic omission was pushed as bd2f8ee and reconciled with concurrent85fba07 in f292f2f4a0cfe101a98bc91a9574b3a4cec16ccf. Full parent verification passed324Manifest cases and727applicable Core cases (four expected platform/benchmark skips);83of84CI controls passed initially and the one stale explicit error-command expectation passed after its direct correction. These results did not establish complete CLI/published-wire compatibility.

Hosted CI41647054451 exposed a genuine regression:7CLI captured-wire cases and immutable Parties packaged replay/enrollment/preflight reject the missing projectionActorType. It is a required frozen wire field, so changing those expectations or validators would be wrong. SDK326 also strictly requires ActorType=null for HTTP and ProjectionActor for actor dispatch, checked before digesting and again at gateway/router admission. A Parties-only legacy ActorType is invalid. Exact artifact/source proof is mcpcli-f292-selector-diagnosis.json.

The transport-omission approach is superseded by spec-native-query-selector-compatibility-cohort.md: restore generic descriptor-to-wire behavior, add genuine finite owner-declared digest-covered compatibility selectors in the shared SDK, qualify/publish the new real cohort, then enroll only actual qualified owner sources. All package-byte/audit, signed nonmember, cleanup and evidence improvements remain. No frozen vector/plan/inventory/validator is relaxed. Full hosted acceptance remains pending.

All three context-free layers reviewed the frozen49,119-byte follow-up diff. Each finding is classified independently before grouping.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind1: coverage runs after package build targets | medium | GenerateAsync runs build before RequireCoverage. An uncovered restored build dependency can execute before refusal. Patch: separate restore, coverage, then no-restore build. |
| Blind2: assets hashes do not bind consumed package bytes | medium | RequireCoverage compares two metadata strings without hashing the actual archive or extracted assets. The exact-byte claim needs actual byte verification. Patch: authenticate the cached archive and consumed extracted assets against the retained hash before build/execution. |
| Blind3: copied owner DLLs bypass package coverage | false | The only caller supplies the separately qualified Parties source outputs; ci.require_prepared_provenance rechecks gateway_package_provenance, including the complete adapter output inventory, before either runner. GenerateAsync copies and rechecks each actual source hash, refuses disagreeing duplicates/reader binaries, and NativeCatalogRuntime verifies the actual loaded artifact receipt. These are explicitly qualified source artifacts rather than additional unbound package graph entries. |
| Blind4: suppressed advisories can produce an unconditional audited receipt | medium | reader_audit_provenance checks audit mode/settings/logs but not applicable audit suppressions. Patch: refuse suppressed audit advisories/NU19xx warnings and retain the effective no-suppression evidence. |
| Blind5: required low audit level is not passed explicitly | medium | Receipt requires low while both source and Integration builds pass only enablement and mode. An inherited different level can abort preparation after successful builds. Patch: pin low in both online build paths and test it. |
| Blind6: forged-token test does not exercise gateway authentication | false | Existing GatewaySmokeTests.AuthenticatedQueryReturnsSuccessfulDocument already sends anonymous and fixture wrong-key requests through the actual pinned gateway and requires401 before its successful query. The new offline signature test supplements that real required gate; permissive gateway authentication fails the existing smoke. |
| Blind7: no signed nonmember command crosses the persisted owner boundary | medium | The new owner cases correctly verify actual SDK stripping, but construct envelopes directly. Add the missing authenticated nonmember administrator-hint command to the existing hosted smoke and verify rejection with unchanged persisted business state. No immutable vector change is needed. |
| Blind8: no test proves receiver selection with a stale descriptive actor | false | Existing unchanged Parties live vectors call the actual frozen1.5.1 descriptor through both heads and assert persisted query results. The qualified native catalog binds projection while those authenticated descriptors retain the legacy actor names, confirmed by mcpcli-native-query-selector-proof.json. The complete required hosted gate is exactly this receiver-selection regression; its acceptance is still pending. |
| Edge1: changed cached bytes can retain audited assets metadata | medium | Independently confirms Blind2 at NativeCatalogAudit.RequireCoverage. Route to the same exact-byte patch, retaining this separate verdict. |
| Edge2: uncovered package targets can run before rejection | medium | Independently confirms Blind1's build-before-coverage path. Route to the same restore/validate/build patch, retaining this separate verdict. |
| Verification1: actual GenerateAsync path never tests unaudited refusal | medium | Pre-verified mutation gap: removing the call leaves helper tests passing. Patch: controlled dotnet/private-directory test supplies an uncovered or changed restored graph, proves GenerateAsync refuses before build/catalog execution and retains ownership cleanup. |

Survivors group into pre-build exact package coverage (Blind1,Blind2,Edge1,Edge2,Verification1), complete explicit audit policy (Blind4,Blind5), and signed nonmember persisted-owner refusal (Blind7). All are existing demonstrated states with no public API addition and route to the original native implementer as focused patches. No deferred finding or frozen-intent change is required.
