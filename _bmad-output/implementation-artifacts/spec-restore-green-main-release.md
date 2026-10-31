---
title: 'Restore green main for the Tenants release'
type: 'bugfix'
created: '2026-10-31'
status: 'done'
route: 'oneshot'
baseline_commit: 'ad973f08d01b7bb65a4798c7b5f6f0d6824aba0b'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="user authorized CI repairs, push, release dispatch and production approval">

## Intent

Restore CI, Source-reference integration and Story Guards on the live main tip,
then dispatch Release and verify the five manifest packages on NuGet.
Preserve all blocking suites and package-only Release builds. The published
EventStore 3.128.4 integration harness still has the legacy fixture APIs; the
descriptor-only API exists in the newer source checkout. Keep the existing
bootstrap suites on that explicit published compatibility harness in both
dependency modes, with runtime libraries still sourced in the source lane.
Use the existing development-catalog source compilation flag for APIs absent
from the published runtime packages. Do not copy shared infrastructure into Tenants
or weaken tests. A full descriptor migration requires separate runner-owned
suites because current tests assert fixture-owned fakes and startup controls.

</frozen-after-approval>

## Implementation Notes

- Initial tree was clean on main; remote main matched the baseline above.
- Live Story Guards gitlink validation passed. Its browser graph and package CI
  failed on unpublished routing metadata types; Source-reference integration
  failed on the changed exported fixture APIs.
- Keep the compatibility harness centrally pinned at 3.128.4; source builds keep
  their EventStore runtime project references. Gate unpublished catalog APIs with
  the existing HEXALITH_DEVELOPMENT_CATALOG_SOURCE constant.
- Package compilation exposed two more unpublished APIs: adapter discovery and
  shared HTTP security enrollment. Source uses the shared security implementation;
  package mode configures its human-only bearer policy with published credential
  kind constants and preserves explicit authorization registration.
- Source constructor changes require catalog authority in isolated query proofs.
  Supply validated canonical snapshots and real dispatch leases, preserving real
  query handlers and REST/UI propagation assertions.
- HTTP execution exposed obsolete gateway tests hosted on the now-internal-only
  Tenants domain app. Move command/status producer tests to a test-only gateway
  composition using the published Gateway registration extensions; retain the
  domain fixture and its workload credentials for internal endpoint tests.
- Guard regression evidence: 77 story-gitlink tests and 101 changed-story tests
  passed. The event entry point itself requires a GitHub event payload; local
  invocation without GITHUB_EVENT_PATH correctly refused to infer a range.
- Removed the Contracts source edge's McpCli Version override: it compiled a
  reference to 1.1.0 while the source producer and copied runtime assembly were
  1.0.0. Source identities now agree; the published package pin stays authoritative
  in package mode.
- Implementation validation passed: package Release integration build and Debug
  source integration build both had zero warnings/errors. Package HTTP/security/
  structural/projection/audit checks passed 208 tests; the complete command-runtime
  class passed 162; source provenance passed four and compiled catalog discovery
  passed one. All reported zero skips. Whitespace verification of the new gateway
  fixture passed (workspace-load warnings only).
- The exact commit message was validated with the pinned @commitlint/cli 21.2.3:
  `node node_modules/@commitlint/cli/cli.js --edit /tmp/tenants-ci-fix-commit-message.txt --verbose`.
  Result: zero problems, zero warnings; evidence is retained at
  `/tmp/tenants-ci-fix-commitlint.log`. Live CI and release publication follow this
  implementation commit and remain subject to the unchanged workflow gates.
- Live-topology inspection found a second compatibility boundary: the shared
  Aspire helper launches source executable hosts. Current EventStore source now
  requires catalog identity/profile while this frozen topology supplies neither.
  Disposable package-CI jobs select source revision
  717a06e16c3afd81b904eaf4de646b74b13262d2, verified from all inspected 3.128.4
  package nuspecs, through the existing package preparation hook. The source lane
  and tracked submodule gitlink remain current; catalog admission is not weakened.
- The preparation hook preserves current source in build-and-test; only the three
  declared topology executor jobs select frozen hosts. Disposable-clone validation
  proved exact selection, preservation in build-and-test, refusal outside Actions,
  refusal for unknown jobs/dirty checkouts, and no change to this workspace.
- Wider package validation passed Client 50, Testing 182, UI 8,054, Sample 40,
  AppHost 42 and Server 1,624 tests. Contracts had 769 passing tests and two
  governance failures: an attribute-only metadata reader and four missing parent
  Version-removal entries. Both were repaired and their focused tests passed (2/2)
  after a warning-free package build. The unchanged complementary dependency and
  dependency-owned-version requirements remain enforced.
- Follow-up script/workflow review reported no actionable findings. The follow-up
  commit message also passed pinned commitlint with zero problems/warnings;
  evidence: /tmp/tenants-bootstrap-host-commitlint.log.

## Review Triage Log

- High, patched: package API workload principals reached dispatch when shared
  source security was compiled out. A package-compatible human bearer policy now
  rejects workload/channel/operation claims and conflicting assertion headers.
- Medium, patched: the structural suite requires explicit AddAuthorization.
  Restored the registration alongside the source/package policy selection.
- Medium, patched: source catalog coverage bypassed compiled discovery. The
  existing test now reads operations through compiled owner declarations and
  verifies the adapter assembly enrollment.
- Follow-up review found no remaining actionable issues in the catalog authority,
  human policy or gateway/domain test separation. No review findings were deferred.
