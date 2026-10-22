# DW-507 / DW-509 / DW-510 / DW-513 / DW-518 validation evidence

Date: 2026-10-22. Scope: command status and replay documentation only.

## Revisions and ownership

- Tenants starting revision: `1ccdacd11acb27fa9b7055ea2c44c98828867489`.
- Original EventStore gitlink: `5ce5f1d84b88c808cc8a526b9decf149de977e55`.
- EventStore implementation base: `70b2206b531a71c883fbe2e4749047b399d26447` (fetched main, clean before editing). The six target documents and status/replay controller models were unchanged between the original gitlink and this main base. EventStore main was fast-forwarded before editing so the owning commit can be pushed without rewriting shared history. The final Tenants gitlink necessarily includes those already-published ancestor commits.
- A concurrent session committed and pushed the six owning documentation files as `92b22bac631cd27f67e81e48e800459bd91af77b` (`build: sync local changes via /pushall`) before this task committed them. The review target was updated to that exact committed range; no owning documentation was lost. The independently validated documentation-message candidate below was not used for that external commit; it is reserved for this task's review follow-up commit.
- Owning changes are seven Markdown documents in EventStore after the review consistency patch added the linked recovery guide. Tenants owns the five ledger closures, build spec, this evidence and the gitlink update. No nested submodule was initialized or updated.

## Closure matrix

| Record | Corrected documentation | Source contract checked |
| --- | --- | --- |
| DW-507 | Command API response table includes nullable `retryable`, `recoveryReasonCode`, `drainAttemptCount`; zero attempts, legacy/unrecorded values and polling implications are explicit. | `src/Hexalith.EventStore/Models/CommandStatusResponse.cs` constructor and `FromRecord`; `AggregateActor.WriteDrainAdvisoryStatusAsync`; `CommandStatusController.CreateStatusResult`; existing controller tests for armed, exhausted and legacy records. |
| DW-509 | Seven obsolete status route examples across security, lifecycle, first-domain-service and brownfield guides use submitted `messageId`. Security guidance describes message-primary storage plus tenant-scoped bounded compatibility and ambiguity. | `CommandStatusController.GetStatus`, `CommandsController` Location construction, `DaprCommandStatusStore` and `CommandStatusConstants.BuildKey`. |
| DW-510 | Actual replay route name remains `{correlationId}`, but message-first lookup, unique correlation compatibility and legacy fallback are explained. Original tracing and message identities remain distinct; ambiguity is a documented 409 outcome. | `ReplayController.Replay` direct/indexed/legacy lookup, status eligibility, Location and response construction; existing message-primary and ambiguous-correlation controller tests. |
| DW-513 | `failureReason` includes rejected concurrency conflicts; ordinary domain rejection may leave it null and uses `rejectionEventType` separately. | `ConcurrencyConflictExceptionHandler`, coordinated-conflict status writer and `AggregateActor.CompleteTerminalAsync`. |
| DW-518 | `eventCount` can be positive on eventful rejection or publication recovery and does not prove success. Existing optional sequence-proof restrictions remain. | `AggregateActor.CompleteTerminalAsync` writes count when accepted or count is positive; drain terminal records retain range count; status response copies the record count. |

DW-523 remains open: the status enum/controller still omit `Retry-After` for every `PublishFailed`, including armed recovery. This change documents that limitation and does not change the runtime classifier or replay eligibility. Other deferred entries and release/acceptance holds remain unchanged.

## Focused validation

All commands below ran from `references/Hexalith.EventStore` unless noted.

```bash
npx --yes markdownlint-cli2 docs/reference/command-api.md docs/guides/security-model.md docs/concepts/command-lifecycle.md docs/getting-started/first-domain-service.md docs/brownfield/api-contracts.md docs/brownfield/architecture.md
```

Exit 0. markdownlint-cli2 0.24.0 / markdownlint 0.42.0; six files; `Summary: 0 issues in 0 files`. One existing missing blank line before the resilience list in the touched brownfield architecture page was corrected. No lint rule was disabled.

The installed lychee 0.24.2 cannot parse the repository's legacy boolean `include_fragments = true`. Direct command:

```bash
lychee --config lychee.toml --offline docs/reference/command-api.md docs/guides/security-model.md docs/concepts/command-lifecycle.md docs/getting-started/first-domain-service.md docs/brownfield/api-contracts.md docs/brownfield/architecture.md
```

Exit 3: `TOML parse error at line 48, column 21`, `wanted string or table`. To retain the existing policy and check anchors, a temporary copy changed only that option to `include_fragments = "anchor-only"`:

```bash
python3 - <<'PY_CONFIG'
from pathlib import Path
source = Path('lychee.toml').read_text()
Path('/tmp/tenants-dw-status-lychee.toml').write_text(
    source.replace('include_fragments = true', 'include_fragments = "anchor-only"'))
PY_CONFIG
lychee --config /tmp/tenants-dw-status-lychee.toml --offline docs/reference/command-api.md docs/guides/security-model.md docs/concepts/command-lifecycle.md docs/getting-started/first-domain-service.md docs/brownfield/api-contracts.md docs/brownfield/architecture.md
```

Exit 0: `83 Total`, `44 Unique`, `69 OK`, `0 Errors`, `14 Excluded` under the existing policy. This is local file/fragment validation, not a claim about external website availability. A named status-lookup heading provides stable links in all affected guides.

`rg -n -F 'status/{correlationId}' docs` returned exit 1 with no matches. `git diff --check` passed in both repositories. Existing runtime tests were inspected as contract evidence, not executed; no runtime code changed and no new tests were added.

## Broad documentation gate

```bash
bash scripts/validate-docs.sh
```

Exit 1 at Markdown linting, with 13 existing issues in seven untouched files. Consequently later link/sample stages did not run. Exact final output:

```text
=== Stage 1/6: Markdown Linting ===
markdownlint-cli2 v0.24.0 (markdownlint v0.42.0)
Finding: docs/**/*.md README.md CONTRIBUTING.md CHANGELOG.md CODE_OF_CONDUCT.md !docs/reference/api/Hexalith.EventStore.*/** !CHANGELOG.md
Linting: 96 files
Summary: 13 issues in 7 files
docs/architecture/evidence-secret-scanning.md:63 error MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]
docs/architecture/evidence-secret-scanning.md:237 error MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]
docs/brownfield/project-overview.md:18 error MD040/fenced-code-language Fenced code blocks should have a language specified [Context: "```"]
docs/brownfield/project-overview.md:23:19 error MD034/no-bare-urls Bare URL used [Context: "https://github.com/Hexalith/He..."]
docs/brownfield/source-tree-analysis.md:7 error MD040/fenced-code-language Fenced code blocks should have a language specified [Context: "```"]
docs/brownfield/source-tree-analysis.md:44 error MD040/fenced-code-language Fenced code blocks should have a language specified [Context: "```"]
docs/brownfield/source-tree-analysis.md:115 error MD040/fenced-code-language Fenced code blocks should have a language specified [Context: "```"]
docs/guides/checkpointed-processes.md:76 error MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]
docs/guides/checkpointed-processes.md:87 error MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]
docs/guides/deployment-docker-compose.md:214 error MD028/no-blanks-blockquote Blank line inside blockquote
docs/guides/deployment-docker-compose.md:357 error MD028/no-blanks-blockquote Blank line inside blockquote
docs/guides/trusted-effects.md:39 error MD001/heading-increment Heading levels should only increment by one level at a time [Expected: h2; Actual: h3]
docs/security/workload-binding-authority.md:22 error MD001/heading-increment Heading levels should only increment by one level at a time [Expected: h2; Actual: h3]
FAILED: Markdown linting

Validation failed at Markdown linting. Fix the errors above and re-run.
```

The broad gate remains blocked separately from passing focused lint and local links; neither configuration nor unrelated pages were changed to conceal it.

## Commit message validation

The installed commitlint versions match each owning lockfile: EventStore 21.1.0 and Tenants 21.2.3. Each exact candidate was written to its temporary message file before validation.

EventStore:

```bash
npx --no-install commitlint --edit /tmp/tenants-dw-status-owner-message.txt --verbose
```

Exit 0:

```text
⧗   --- input ---
docs: correct command status and replay contracts
✔   found 0 problems, 0 warnings
```

Tenants (workspace root):

```bash
npx --no-install commitlint --edit /tmp/tenants-dw-status-parent-message.txt --verbose
```

Exit 0:

```text
⧗   --- input ---
docs: close five deferred command status records
✔   found 0 problems, 0 warnings
```

## Independent review

The configured Blind Hunter layer reviewed the owning range and root artifacts. No configured review layer was skipped. All six findings were checked against source and patched:

1. Medium: retryable true also covers durable recovery awaiting reminder re-arm (`drainReminderArmed || recoveryEntryTracked` in both first-pass and resumed publication failure). Corrected reference wording and clarified that tracking supplies no active-reminder or retry-deadline guarantee.
2. Medium: the linked recovery guide retained overly narrow true/null and terminal false descriptions. Reconciled all optional recovery-field meanings with the reference.
3. Medium: first-domain-service advised retaining message identity but omitted it from three required-field-validated requests. Added distinct message IDs, a matching returned ID and Location/polling example.
4. Medium: lifecycle diagram and text still described correlation as the status key. Changed them and the request/response example to distinguish message tracking from correlation tracing.
5. Medium: concurrent external commit advancement caused this evidence to capture the owning documentation commit as the clean editing base. Restored the previously recorded canonical base `70b2206b531a71c883fbe2e4749047b399d26447`; kept `92b22bac631cd27f67e81e48e800459bd91af77b` as the first owning documentation revision.
6. Medium: the neighboring rejectionEventType row incorrectly limited the marker to Rejected. Documented the first-pass/resumed eventful PublishFailed rejection marker and nullable drain advisories.

A nonexistent method name in preliminary evidence was also corrected to `WriteDrainAdvisoryStatusAsync` during source checking. No findings were deferred.

The concurrent session also captured the initial root artifacts and other submodule pointer advances in `1012e1e9a7a31c85a4df9c8ab08b2c37407c12b7`. This task's final commit preserves that external commit and records only the review closeout and its owning reference update.

## Final review validation and publication

Final seven-page commands:

```bash
npx --yes markdownlint-cli2 docs/reference/command-api.md docs/guides/security-model.md docs/concepts/command-lifecycle.md docs/getting-started/first-domain-service.md docs/brownfield/api-contracts.md docs/brownfield/architecture.md docs/operations/drain-failure-reason-codes.md
lychee --config /tmp/tenants-dw-status-lychee.toml --offline docs/reference/command-api.md docs/guides/security-model.md docs/concepts/command-lifecycle.md docs/getting-started/first-domain-service.md docs/brownfield/api-contracts.md docs/brownfield/architecture.md docs/operations/drain-failure-reason-codes.md
```

Both exit 0. Lint: seven files, zero issues. Local links: 83 total, 44 unique, 69 OK, zero errors, 14 existing exclusions. A Python JSON/identifier check also passed: all three first-domain-service requests carry distinct valid required message identities, the first response matches its request identity/correlation, and the lifecycle response retains its request identity. `git diff --check` passed in both owners after the patches.

Final owning revision: `f4df781cb7247c6a5f7549106b90d3748151edba` (`docs: correct command status and replay contracts`), pushed on EventStore main. The exact candidate above passed EventStore commitlint again immediately before publication. A concurrent commit writer advanced HEAD to this same desired commit while this session's `git commit --only -F /tmp/tenants-dw-status-owner-message.txt -- docs/reference/command-api.md docs/concepts/command-lifecycle.md docs/getting-started/first-domain-service.md docs/operations/drain-failure-reason-codes.md` was running; that command returned 128 (`cannot lock ref HEAD`, expected `92b22bac631cd27f67e81e48e800459bd91af77b`). Inspection confirmed all four reviewed follow-up files, the validated full message and a clean worktree in the resulting commit. `git push origin main` returned `Everything up-to-date`; no history rewrite or force push was needed.

The five closed ledger records retain their original evidence. Comparison to the starting Tenants revision confirms only DW-507/509/510/513/518 changed and DW-523 remains open. The broad docs gate still has the separately recorded 13-issue blocker; no runtime test or production acceptance claim is made.
