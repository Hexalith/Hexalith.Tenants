# Historical baseline CI policy repair — 2026-10-29

The general guard policy now accepts independently recorded creation evidence
for a rejected historical baseline. Both this repair and the paging spec meet
their applicable local completion gates. No hosted CI run was created, and no
source repository commit, staging, branch, push or gitlink operation occurred.
Disposable committed fixtures are isolated in temporary clones.

## Verified provenance

- Source HEAD: `1d8c31d2abe809829e10ce1ddadc978c79489f3e`.
- Independently discovered sole creation: `7716f0b11423eb54d74935b6cc6e3edf405dc400`.
- Creation's recorded `baseline_revision` and exact single parent:
  `6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513`.
- Historical endpoint: `ded413dfd4786983585c7f56a399825cc1afbae3`.
- Preserved rejected baseline: `23b2a7691ae667fd160a48a5d7293f3cf38d48fe`.

The rejected object is an available canonical commit in the source repository;
Git returns exit 1 for its ancestry to the endpoint. The endpoint records both
that rejected `baseline_commit` and the creation's original `baseline_revision`.
The full identity walk verifies sole creation, exact parent and retained history.
All nine historical gitlink targets remain declared. No spec-specific branch was
added to production code. The existing unavailable-baseline repair remains valid.

## Commands and exact results

Paths below are relative to the repository root; output links contain the full
logs. Fixture commit messages were validated with the repository's installed
`@commitlint/cli@21.2.3` before use, as recorded in [fixture-commitlint.txt](fixture-commitlint.txt).

| Command | Final result | Evidence |
| --- | --- | --- |
| `python3 tests/scripts/test_validate_changed_story_gitlinks.py` | exit 0; 101 tests, 19.160s, OK | [log](changed-story-tests.txt) |
| `python3 tests/scripts/test_validate_story_gitlinks.py` | exit 0; 77 tests, 8.417s, OK | [log](production-guard-tests.txt) |
| `python3 tests/scripts/test_pushall_detached_head.py` | exit 0; 13 tests, 2.899s, OK | [log](pushall-attachment-tests.txt) |
| `python3 tests/scripts/test_pushall_default_branch.py` | exit 0; 37 tests, 16.114s, OK | [log](pushall-default-tests.txt) |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | exit 0; zero warnings/errors, 2.41s | [log](ui-build.txt) |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | final exit 0; 8,052 total, zero errors/failures/skips/not-run, 91.863s | [log](ui-tests.txt) |
| `python3 _bmad-output/implementation-artifacts/evidence/global-admin-projection-paging-2026-10-29/check-ci-provenance.py` | exit 0; PASS at recorded historical endpoint | [command/output record](history-results.json) |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md --ref ded413dfd4786983585c7f56a399825cc1afbae3` | exit 0; nine declared targets, RESULT: PASS | [command/output record](history-results.json) |
| `python3 _bmad-output/implementation-artifacts/evidence/ci-historical-baseline-repair-2026-10-29/verify-history.py` | exit 0; actual-history and disposable committed-event verification PASS | [summary](history-verification.txt), [all commands/results](history-results.json) |

The committed-event probe runs actual repository history in two disposable clones.
The shared-object clone sees the rejected object; a natural `--no-local` transport
clone does not receive the unreachable object. The source object remains available
throughout. Both clones pass repair push and PR events plus a later documentation
edit, while refusing false creation, pointer movement and pointer move/revert
events with exit 1. The unchanged source's old committed event remains refused:
uncommitted metadata cannot retroactively bless it. Full canonical fixture IDs
and exact JSON payloads are retained with the command records.

## Review and failure evidence

All three context-free layers ran before triage: Blind Hunter (finding floor 5),
Edge Case Hunter and Verification Gap Reviewer. Every finding is classified in
the separate policy spec. All accepted findings were patched; none were deferred.
Follow-up reviews closed every finding and reported no remaining blocker. Blind
review passed seven focused tests, edge review passed five (2.268s), and verification
review passed six. These are separate reviewer checks, not additional suite totals.

Review found and reproduced fresh-clone refusal, conflicting duplicate proof fields,
unterminated historical frontmatter, missing isolated inspection-error coverage,
and lost Git error diagnostics. The [red regression run](review-regressions-red.txt)
ran five tests with nine failing subcases before their fixes. The [initial actual
history probe](history-verification-initial.txt) independently reproduced fresh-clone
refusal; its [command records](history-results-initial.json) are preserved. Final
fixtures also exercise endpoint ancestor-graph inspection failure.

The first expanded 96-test run had two fixture expectation failures: the rejected
commit accidentally matched another empty fixture commit, and one expected an
earlier diagnostic instead of the actual endpoint-proof refusal. A distinct,
validated fixture message and corrected diagnostic fixed both; the subsequent
96-test run and final 101-test run passed.

The first full UI run returned exit 1: 8,052 total, one timeout in
`BudgetTimerObservesThrowingProviderCallbacksAndPreservesCompletedFatal(fatal: True)`
at `TenantQueryGatewayTests.Deadlines.cs:63`, 100.479s. This same failure is recorded
in prior paging evidence. The [initial log](ui-tests-initial.txt) is retained.
The exact isolated command
`tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -method '*BudgetTimerObservesThrowingProviderCallbacksAndPreservesCompletedFatal'`
then passed both cases, exit 0, 0.203s ([log](ui-deadline-rerun.txt)). The unchanged
full UI rerun passed all 8,052. No unrelated production fix or causal attribution
is claimed.

## Preservation and runtime prerequisite

The [preservation record](preservation.json) compares all 4,511 tracked files at
invocation: exactly the four authorized maintained files changed; the other 4,507
retain their initial SHA-256, including both preexisting modified UI test files,
Story 5.7, production code and the deferred-work ledger. Initial file hashes and
gitlink index entries are retained in [the compressed snapshot](initial-preservation.json.gz).
The [original paging spec](paging-spec-at-invocation.md.gz) was reconstructed before
completion edits and its bytes match the invocation hash, preserving all prior
notes and rejected-baseline evidence. HEAD and all current gitlinks are unchanged.

The repository-mandated runtime prerequisite used the Aspire skill. `aspire ps
--format Json` initially returned `[]`; `aspire start --apphost
src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` exited 0. `aspire describe
--apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json`
and `aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
--format Json` exited 0; all 30 resources were running/healthy. `aspire stop --apphost
src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` exited 0; final `aspire ps
--format Json` returned `[]`. [Resource-state summary](runtime-summary.json).
No live domain command or new browser qualification was required or claimed for
this CI policy repair.

The final whitespace, Python syntax, both spec audits, provenance and preservation
checks are recorded in [final-checks.json](final-checks.json). Git's existing CRLF
normalization notices do not represent whitespace errors.
