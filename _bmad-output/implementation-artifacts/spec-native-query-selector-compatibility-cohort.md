---
title: 'Preserve frozen query wire contracts with digest-bound native compatibility selectors'
type: 'feature'
created: '2026-11-03'
status: 'in-review'
baseline_commit: '33835a03e92579a26400e7cb7cf68d666c715b3e'
eventstore_baseline_commit: 'e97582bf43ffe8c2f7d958dd470a242c21bd4908'
mcpcli_baseline_commit: 'f292f2f4a0cfe101a98bc91a9574b3a4cec16ccf'
---

## Intent

Complete the user's repository/package/CI/release request while preserving the actual frozen Parties1.5.1 descriptor-to-wire contract and21 vectors. Hosted McpCli CI41647054451 proves that omitting ProjectionActorType breaks7 CLI cases and packaged loopback/enrollment/preflight. Source-exact diagnosis: `/tmp/tenants-release-20261103-07JciR/mcpcli-f292-selector-diagnosis.json`. SDK326 requires HTTP ActorType=null and actor targets to use ProjectionActor; both QueriesController and HandlerAwareQueryRouter reject the old supplied hints. A Parties-only alias in ActorType is invalid. The prior generic omission is superseded by genuine shared-SDK support.

## Boundaries

Restore generic descriptor-to-wire selector emission and its exact tests. Do not change frozen vectors, plans, inventory, public1.5.1 artifacts, validators or expected wire fields. No test-host request rewrite, source/compile-mode trick, wildcard alias, fallback, retry or invented metadata.

Add a finite explicitly owner-declared compatibility-selector collection to the SDK declaration model, covered by canonical declaration/catalog digests. Preserve all existing constructor signatures and old canonical bytes when the collection is absent. Enforce bounded exact valid unique names and the intended query/HTTP eligibility; retain the existing ActorType/TargetKind invariant. Use one shared matching rule in gateway ingress and HandlerAwareQueryRouter. A supplied nonempty selector is accepted only by existing exact target semantics or an explicitly enrolled compatibility entry; wrong/foreign/unenrolled selectors must retain refusal. An absent caller selector keeps its existing behavior. TargetKind, AppId, Method, handler, projection outputs, scope and permission checks still choose the actual receiver. Mutation must change the digest and fail existing authority checks. Never infer aliases from untrusted input or reuse unrelated schema fields.

Qualify and publish the resulting real SDK cohort through normal automated CI/release gates before claiming package availability. User authorization covers necessary upstream CI fixes and unattended releases; no exact release version is invented before semantic-release calculates it. Preserve all concurrent EventStore scanner, approved privacy-policy and public7 catalog work. The exact privacy normative bytes and all unrelated constraints stay intact.

After public SDK proof, add only the real Parties owner's two appropriate finite aliases (PartyDetailProjectionQueryActor; PartyIndexProjectionQueryActor for index/search), qualify and commit its source, and bind the native manifest to actual new immutable sources/packages. Actual published Tenants7 bytes remain authentic. Update the ordinary consumer/current package defaults and provenance coherently; preserve the separate legacy harness/compatibility pins and all loader/source checks.

The newly enrolled owner has an explicit minimum SDK cohort: an old SDK cannot compile the new member or read catalogs containing it. Preserve older exact SDK contracts and tests against their immutable historical owner sources, and qualify current owner source against the actual verified new public SDK. Keep the existing base API availability facts distinct from this new owner requirement. Retain old lane test counts and explicit refusal of old SDKs by the new owner; do not use reflection, conditional compilation, disabled lanes or silent count reductions. The concrete minimal transition is recorded in `/tmp/tenants-release-20261103-07JciR/parties-native-selector-compatibility-recommendation.json`.

No nested initialization/update, no local business AppHost/private runtime/Azure mutation. Offline builds, in-memory endpoints, owned inert loopback probes and compiled-model inspection are permitted. Root owns Git publication and remote acceptance.

## Tasks & Acceptance

- [x] SDK model/codec validation and shared admission rule preserve old byte/constructor compatibility and reject unbound selectors in both consumers.
- [x] Genuine owner-declared aliases are digest-bound; exact receiver/scope/permission authority is unchanged and covered by positive/negative controls.
- [x] Generic CLI wire restored; all ordinary CLI/MCP tests and immutable packaged replays/preflight pass.
- [ ] Real new SDK cohort passes automated CI and is published/independently verified before consumer enrollment claims.
- [ ] Actual qualified Parties/new consumer source selection passes full hosted21-plus-smoke, persisted effects, signed nonmember refusal and owned cleanup.

## File List

- references/Hexalith.EventStore
- references/Hexalith.McpCli
- references/Hexalith.Builds
- references/Hexalith.FrontComposer
- references/Hexalith.Memories
- references/Hexalith.Platform

## Verification

SDK focused contract codec/golden/digest/invalid-alias controls, both real admission consumers and actual committed receiver dispatch; individual affected Release/package builds. Existing old constructor/public Tenants7 binary compatibility must remain. Root will review the complete SDK feature delta with three independent layers, then run standard owner CI/release qualification. Consumer gate includes CLI/MCP ordinary projects, complete frozen packaged replays/preflight and actual hosted native22cases. No fake publication or local-only acceptance.

Implementation handoff: `/tmp/tenants-release-20261103-07JciR/sdk-native-selector-compatibility-handoff.json`. All12 owned SDK files are separate from the concurrently reviewed scanner/privacy/catalog story. Focused Contracts187/187 and Server40/40 passed without skips; an exact authentic publicTenants7 ABI recheck passed. Both affected Release builds passed with no warnings/errors. Three local package checks wrote only disposable private files using the existing4.0.4 label; they were never installed/published and are not public cohort proof. Constructor/Deconstruct signatures and absent-property goldens remain unchanged. Actual new SDK publication and owner enrollment remain pending.

## Review Triage Log

Three context-free layers reviewed the complete37,213-byte12-file SDK feature delta; the edge layer found no additional defect. Every other finding is classified independently before grouping. The separate privacy/scanner/catalog story retains its own reviewed baseline and17finding patch log.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind1: ABI test assumes active assembly is the published binary | medium | Server.Tests supports a Tenants project reference in source mode; comparing its normally loaded assembly to the published archive fails valid source builds. Patch: load the explicitly pinned historical fixture independently of the selected project/package graph. |
| Blind2: ABI test reconstructs the default cache path | medium | Runtime NUGET_PACKAGES-or-home lookup ignores supported RestorePackagesPath/globalPackagesFolder configuration. Patch: materialize the explicit pinned package fixture into test output using restore/build-resolved paths and verify its actual public bytes. |
| Blind3: docs omit older-reader deployment boundary | low | Closed old readers cannot understand a catalog containing the new property despite unchanged absent bytes/constructors. Direct correction: document coherent reader-cohort upgrade before enabling owner aliases; no new mechanism. |
| Blind4: compiled owner preservation/freeze has no positive test | medium | New CompiledRouteCatalogDeclarations assignment is reached only by selector-free fixtures. Patch: actual attributed compiled owner with nonempty entries, immutable copy/source-mutation isolation and committed-consumer use. |
| Blind5: positive normal composition/activation/readback is missing | medium | New compatibility tests construct snapshots directly and test only mutation refusal through activation. Patch: exercise successful existing schema-three composition/prepare-ready-commit/readback with enrolled selectors, preserving actual authority checks. |
| Blind6: direct controller invocation misses HTTP admission | medium | The test calls Submit on a controller instance, bypassing real binding/validation/filter/exception translation. Patch: in-memory HTTP gateway test proving enrolled dispatch and unenrolled public refusal without effects. |
| Blind7: changed consumers lack full actor-path regression | medium | Existing CatalogQueryRuntimeCore/composed-registration tests cover actor routing with omitted selector, but do not cover exact actor selector and compatibility-selector refusal through both changed consumers. Patch: fill those specific actor cases, preserving no HTTP dispatch. |
| Verification1: compiled-owner enrollment can silently disappear | medium | Pre-verified mutation gap: assigning null in the new reader still passes selector-free compiled tests and hand-built positive snapshots. Patch with Blind4 using actual compiled nonempty declarations and committed consumer. |
| Verification-other1: active assembly ABI comparison breaks source mode | medium | Independently confirmed Server.Tests project/package selection branches and the normal assembly assumption. Patch with Blind1–2; preserve a genuine public binary fixture rather than weakening equality. |

Survivors group into portable genuine ABI fixture (Blind1–2,Verification-other1), direct deployment documentation (Blind3), compiled-owner plus positive composition proof (Blind4–5,Verification1), actual HTTP admission (Blind6), and existing actor-path preservation (Blind7). All have explicit intended behavior and require only existing internal test/fixture paths or direct documentation; no public API expansion, new authority, intent loopback or deferred finding is needed. The original SDK implementer receives these focused patches.

## Exact-source verification checkpoint

SDK feature and all reviewed fixes are pushed through EventStore `0e5ecebc13e8d11afe6d46b6e6de5c877c58acc7`. Parent verification passed 310 selector/ABI/enrollment/scanner cases (274 scanner cases), plus all 37 retained-catalog parity cases, with no skips. The isolated fixture portability repair changes only repository discovery and a duplicate MSBuild argument; all catalog and secret-policy assertions remain. Full Azure unit187 and offline diagnostic15 checks also passed. Exact-source hosted CI and real SDK publication remain pending. Evidence: `/tmp/tenants-release-20261103-07JciR/eventstore-parent-fixture-final-proof.json` and `sdk-native-selector-compatibility-handoff.json`.

McpCli `2e80701c31aafdf00abee05ac469a45ed76dfdf5` passes hosted ordinary build/test, frozen contract/replay/discovery and enrollment jobs in run41657078924. Its Aspire preparation fails the native reader audit-policy check before runtime acceptance; retain the online low-level all-dependency audit and warnings-as-errors requirements while correcting the actual cause. This does not supply native acceptance or a new public SDK cohort.

Native audit preparation follow-up: McpCli `d92a5f1ec4ec635627749470b179422e5342f351` clears inherited `WarningsNotAsErrors` only for native qualification builds. The exact pinned Tenants source appends NU1901–NU1904 independently of `TreatWarningsAsErrors`; real MSBuild before/after and a removal regression reproduce the original refusal. All five focused parent controls pass. The online low-level all-dependency audit validator, archive/provenance checks and warning refusals remain unchanged. Receipt: `/tmp/tenants-release-20261103-07JciR/mcpcli-native-audit-fix-handoff.json`; parent log `mcpcli-native-audit-parent-final.log`.
