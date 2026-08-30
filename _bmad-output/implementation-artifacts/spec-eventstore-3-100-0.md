---
title: 'Align Tenants on EventStore 3.100.0 and SDK 10.0.400'
type: 'chore'
created: '2026-08-30'
status: 'done'
review_loop_iteration: 0
baseline_commit: '4a3eec38d071de0e6622b8b418c59d470ad41c3e'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenants should consume Hexalith.EventStore `3.100.0` on .NET SDK `10.0.400`. The live catalog, EventStore gitlink, and `global.json` already sit on those versions, but agent planning facts still record EventStore `3.99.0`.

**Approach:** Confirm both consume-path pins (advance only if they lag), then reconcile Tenants-owned EventStore version facts. Do not invent a product-code or SDK-runner migration: `v3.99.0..v3.100.0` has no `src/` API delta, and Tenants already pins SDK `10.0.400` with `rollForward: latestPatch` and Microsoft.Testing.Platform.

## Boundaries & Constraints

**Always:** Own edits in Tenants. Keep NuGet versions in the Builds catalog (`HexalithEventStoreVersion`); Tenants `Directory.Packages.props` stays an import-only shim. Pin SDK `10.0.400` in root `global.json` with `rollForward: latestPatch` and `test.runner` `Microsoft.Testing.Platform`. Preserve complementary EventStore source/package pairs and default package mode. Declare every `references/` gitlink that moves from `baseline_commit` `4a3eec38d071de0e6622b8b418c59d470ad41c3e`. Leave an already-correct EventStore `3.100.0` pin, `v3.100.0` gitlink, or SDK `10.0.400` pin unchanged.

**Ask First:** Any EventStore product-code, host, AppHost, or test migration if restore/build fails against `3.100.0`. Any SDK pin other than `10.0.400`, any .NET 11 / `net11.0` move, or dropping Microsoft.Testing.Platform. Any edit inside a submodule, including regenerating `references/Hexalith.Builds/Tools/package-version-audit.json` (stale EventStore `3.99.0` vs catalog `3.100.0`). Any bump of Memories, FrontComposer, Aspire, Dapr, or other families.

**Never:** Resume `_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`. Override CPM locally (`CentralPackageVersionOverrideEnabled` is false). Convert EventStore source/package selection. Rewrite Debug/Release docs beyond the EventStore version string. Change `rollForward` or chase a newer `10.0.4xx` pin (patches already roll forward). Edit EventStore/FrontComposer/Builds historical `_bmad-output` text. Recursive or `--remote` submodule updates. Commit, stage, or push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| EventStore already current | Catalog `HexalithEventStoreVersion` is `3.100.0` and EventStore gitlink is `v3.100.0` (`10051a68eb1db322a4f7fa91934d880ce1409687`) | Leave EventStore pins and gitlinks unchanged; update Tenants-owned EventStore version facts | N/A |
| EventStore consume path lags | Catalog or EventStore gitlink still on `3.99.0` while nuget.org lists `3.100.0` | Advance only those EventStore pins/gitlinks to `3.100.0` | Stop if the package is unlisted or restore rejects it |
| SDK already current | Root `global.json` is `10.0.400` / `latestPatch` / MTP | Leave the SDK pin unchanged | N/A |
| SDK pin lags | Root `global.json` still on `10.0.302` or another 10.0 band | Set `sdk.version` to `10.0.400`; keep `rollForward` and MTP | Stop if the SDK cannot resolve locally; do not invent a runner migration |
| API incompatibility | Release restore/build fails after EventStore `3.100.0` or SDK `10.0.400` | Halt; do not invent adapters | Ask First before any product-code or runner change |
| Builds audit lag | `package-version-audit.json` still records EventStore `3.99.0` | Record the mismatch; do not edit Builds | Ask First to regenerate audit in Builds |
| Historical submodule text | `3.99.0` or `10.0.302` inside `references/**` | Leave untouched | N/A |

</frozen-after-approval>

## Code Map

- `global.json:2-8` -- Tenants SDK selector **already `10.0.400`**, `rollForward: latestPatch`, `test.runner` Microsoft.Testing.Platform. **Advance only if it lags.**
- `docs/quickstart.md:17` and `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs:21-29` -- quickstart must quote the parsed `global.json` SDK version. **Read-only unless `global.json` changes.**
- `_bmad-output/project-context.md:32,42` -- SDK facts already `10.0.400`. **Read-only for SDK.**
- `_bmad-output/planning-artifacts/architecture.md:365,838` -- architecture already records SDK `10.0.400`. **Read-only for SDK.**
- `.github/workflows/ci.yml:20-23` -- reusable `domain-ci.yml@main` with `test-platform: microsoft-testing-platform`; SDK comes from `global.json`. **Read-only.**
- `Directory.Packages.props:1-14` -- read-only Tenants CPM shim; no EventStore versions here.
- `references/Hexalith.Builds/Props/Directory.Packages.props:4,8,40-52` -- `CentralPackageVersionOverrideEnabled=false`; `HexalithEventStoreVersion` **already `3.100.0`**. **Read-only unless Ask First.**
- `references/Hexalith.Builds` gitlink `e1026cb61162546571ee0102c525bcf42b9ce7fa` -- catalog already `3.100.0`. **Read-only.**
- `references/Hexalith.EventStore` gitlink `10051a68eb1db322a4f7fa91934d880ce1409687` -- exact tag `v3.100.0`; `git diff v3.99.0..v3.100.0 -- src/` is empty. **Advance only if it lags.**
- `Directory.Build.props:56-63` -- default `UseHexalithProjectReferences=false`; EventStore source is opt-in. **Read-only.**
- `_bmad-output/project-context.md:34` -- stale EventStore `3.99.0` fact (do not rewrite Debug-source wording beyond the EventStore version).
- `_bmad-output/planning-artifacts/architecture.md:259-261` -- stale EventStore `3.99.0` baseline dated 2026-08-29.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs:136-244` -- EventStore complementary source/package pairs; no hardcoded `3.99.0`/`3.100.0`.
- `scripts/validate-story-gitlinks.py` -- fail-closed gitlink declaration vs `baseline_commit`.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- EventStore rows still `3.99.0`; Builds-owned. **Read-only.**
- `_bmad-output/implementation-artifacts/spec-refresh-dependencies.md` -- different in-progress refresh. **Do not resume.**

## Tasks & Acceptance

**Execution:**
- [x] `global.json` -- verify SDK `10.0.400`, `rollForward: latestPatch`, and Microsoft.Testing.Platform; set the version to `10.0.400` only if it lags -- fulfills the SDK pin without a .NET 11 move.
- [x] `docs/quickstart.md` -- if `global.json` changed, keep the documented SDK string equal to the parsed pin so `QuickstartDocumentationTests` still pass -- skip when the pin is already `10.0.400`.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` and `references/Hexalith.EventStore` -- verify `HexalithEventStoreVersion` is `3.100.0` and the EventStore gitlink is `v3.100.0`; advance only the lagging EventStore pin or gitlink -- completes the consume path without touching other families.
- [x] `_bmad-output/project-context.md:34` -- replace the EventStore pin `3.99.0` with `3.100.0` -- stops agents from reintroducing the old version.
- [x] `_bmad-output/planning-artifacts/architecture.md:259-261` -- replace the EventStore baseline `3.99.0` with `3.100.0` -- keeps architecture facts aligned with the catalog.
- [x] `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md` -- if any `references/` gitlink moves from `4a3eec38d071de0e6622b8b418c59d470ad41c3e`, declare path, reason, and exact SHA in this spec's File List or Completion Notes -- satisfies `validate-story-gitlinks.py`.

**Acceptance Criteria:**
- Given root `global.json`, when parsed, then `sdk.version` is `10.0.400`, `rollForward` is `latestPatch`, and `test.runner` is `Microsoft.Testing.Platform`.
- Given the Builds catalog and EventStore gitlink, when inspected, then `HexalithEventStoreVersion` is `3.100.0` and `references/Hexalith.EventStore` is exactly `v3.100.0`.
- Given Tenants-owned docs excluding `references/`, when searched for EventStore `3.99.0`, then `_bmad-output/project-context.md` and `_bmad-output/planning-artifacts/architecture.md` record `3.100.0` and no other Tenants-owned EventStore pin remains on `3.99.0`.
- Given default package mode, when Release restore/build and package governance run, then they pass without EventStore source/package policy changes or a runner migration.
- Given this spec's `baseline_commit`, when `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md` runs, then it passes.

## Spec Change Log

- 2026-08-30: Reconciled Tenants-owned EventStore facts to `3.100.0`. Consume-path pins and the EventStore gitlink were already on `3.100.0` / `v3.100.0` and were left unchanged.

## File List

No `references/` gitlink moved from `baseline_commit` `4a3eec38d071de0e6622b8b418c59d470ad41c3e`.

## Completion Notes

- Consume-path pins were already current: root `global.json` is SDK `10.0.400` / `rollForward: latestPatch` / `test.runner` Microsoft.Testing.Platform; catalog `HexalithEventStoreVersion` is `3.100.0`; `references/Hexalith.EventStore` is exactly `v3.100.0` (`10051a68eb1db322a4f7fa91934d880ce1409687`). Left unchanged.
- Tenants-owned EventStore version facts updated from `3.99.0` to `3.100.0` in `../project-context.md` and `../planning-artifacts/architecture.md`. Debug/Release source-vs-package wording was not rewritten.
- Builds audit lag: `references/Hexalith.Builds/Tools/package-version-audit.json` still records EventStore `3.99.0` while the catalog pin is `3.100.0`. Recorded; Builds was not edited. Ask First to regenerate the audit in Builds.

## Design Notes

Planning-time tree (`4a3eec38d071de0e6622b8b418c59d470ad41c3e`) already consumes EventStore `3.100.0` via Builds `e1026cb` and EventStore `10051a68` (`v3.100.0`), and already pins SDK `10.0.400`. EventStore `v3.100.0` itself is SDK/CI, not a consumer API change. "Latest SDK" here means pin `10.0.400`, not the newest 10.0 patch string and not .NET 11. Do not treat the stale Builds audit JSON as a Tenants edit.

## Verification

**Commands:**
- `jq -e '.sdk.version=="10.0.400" and .sdk.rollForward=="latestPatch" and .test.runner=="Microsoft.Testing.Platform"' global.json` -- expected: exit 0.
- `dotnet --version` -- expected: `10.0.400` or a later `10.0.xxx` patch under `latestPatch`.
- `git -C references/Hexalith.EventStore describe --tags --exact-match HEAD` -- expected: `v3.100.0`.
- `rg "HexalithEventStoreVersion" references/Hexalith.Builds/Props/Directory.Packages.props` -- expected: `3.100.0`.
- `rg -g '!references/**' 'EventStore.*3\\.99\\.0|3\\.99\\.0.*EventStore' _bmad-output src tests` -- expected: no Tenants-owned EventStore `3.99.0` pins.
- `dotnet restore Hexalith.Tenants.slnx -p:Configuration=Release && dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release` -- expected: package governance passes.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md && git diff --check && git submodule status` -- expected: declared gitlinks, whitespace, and submodule state pass.

## Suggested Review Order

- Agent planning pin now matches the live EventStore catalog.
  [`project-context.md:34`](../project-context.md#L34)

- Architecture baseline pin updated to the same EventStore version.
  [`architecture.md:260`](../planning-artifacts/architecture.md#L260)

- Builds audit lag remains Ask First, recorded for later.
  [`deferred-work.md:2668`](deferred-work.md#L2668)
