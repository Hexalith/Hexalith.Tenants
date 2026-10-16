---
title: 'DW-337 consistent tracked dispatch ambiguity'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'dispatch'
baseline_commit: '5f41f089157ef077c5145f34a260004cafa5349c'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Identical uncertain delivery can retain a global-administrator command but terminate or throw from a lifecycle/configuration submission, preventing consistent same-ID recovery.

**Approach:** Implement DW-337's settled September 6 decision: share the existing global transport classifier across tracked grant/remove administrator, set/remove configuration, and enable/disable tenant dispatch. Behavior changes belong to the four lifecycle/configuration operations.

## Boundaries & Constraints

**Always:** Retain exact message ID, scope and payload. Reuse operation-specific `SubmissionEvidence.Ambiguous` keys and whole EN/FR resources from DW-234/DW-251/DW-316. Apply transport uncertainty before permanent rejection mapping, matching existing global precedence. Preserve caller cancellation, permanent/domain rejection codes, validation and public signatures.

**Never:** Automatically redispatch, mint a replacement ID after uncertainty, infer acceptance/confirmation, or expose exception text. No changes to create/member/metadata classification, status lookup, audit mapping (DW-469), retained admission, proof, deadlines, resources, dependencies/gitlinks or release gates. Preserve historical findings, completed closures, archive and sprint. No stage/commit/push/deploy.

## I/O & Edge-Case Matrix

| Input/state | Expected behavior |
| --- | --- |
| Gateway `Retryable == true`, or HTTP 408/429/>=500 regardless of flag | `RequestSent`, ambiguous, original ID, null correlation/rejection/literal, operation key |
| HTTP transport error, timeout, plain/task cancellation with caller token uncanceled | Same ambiguous result |
| Cancellation exception with caller token canceled | Propagate cancellation; no fabricated result |
| Nonretryable 400/401/403/409/418, recognized domain rejection | Existing operation-specific terminal mapping; no ambiguity |
| Recognized rejection plus retryable flag/status | Uncertainty takes precedence, as in global dispatch |
| Invalid request or explicit malformed ID | Existing validation; zero transport calls |
| JSON/unrelated exception | Existing propagation; classifier does not broaden |
| Explicit retry after ambiguity | Same ID/scope/payload; exactly one send per invocation |

</frozen-after-approval>

## Code Map

No intent gaps, irreversible actions or external dependencies. Two code/test files; no new public surface. [Selection evidence](dw-337-planning-evidence.md) explains P2 priority, stale candidates and diagnostics.

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: reuse global policy and `TenantCommandSubmissionResult.Ambiguous`; preserve terminal mappers and untracked delegation.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: extend `AmbiguousGrantExceptions`, narrow transport cases and captured-request retry pattern; retain localization/classification matrix and shipped-resource assertions.

## Tasks & Acceptance

**Execution:**

- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` — add the six-operation edge matrix, including currently failing retryable-400/plain-cancellation/timeout cases, synchronous throws and faulted tasks. Pin cancellation, terminal/no-dispatch controls, retry envelopes and shipped EN/FR keys.
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` — extract one private classifier and use it in all six tracked methods before their existing permanent mappers. Preserve global behavior and existing operation keys; avoid unrelated refactoring.
- [x] `_bmad-output/implementation-artifacts/dw-337-validation-evidence.md` — record final commands, initial failures, independent review, exact source/dependency provenance and remaining holds after implementation.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/maintenance-backlog.md`, `_bmad-output/implementation-artifacts/release-readiness.md` — only after acceptance, reconcile DW-337 and counts; append evidence while preserving original text/history and every unrelated record. DW-285/DW-470 remain separate acceptance reconciliation.

**Acceptance Criteria:**

- Given any of the six tracked operations, when the same matrix input occurs, then classification/cancellation agree while operation keys, permanent mappings and identifiers retain their contracts.
- Given an ambiguous submission, when the caller explicitly retries, then captured requests match ID, tenant, domain, aggregate, command type and payload, with one transport call per invocation and no fabricated success.
- Given shipped EN/FR resources and hostile exception details, when results are localized, then the exact existing operation message resolves without backend text or key leakage.
- Given maintained UI suites and unchanged governance/browser checks, when validation completes, then relevant tests pass without altered assertions, gates, timeouts or exclusions; separate broad blockers remain recorded.

## Implementation Notes

Implemented one private classifier before the existing permanent mappers in all six tracked methods. Added 422 gateway cases, including synchronous/faulted delivery, caller cancellation, terminal/domain controls, invalid input, exact same-ID retries and existing EN/FR copy. No public API, resource, dependency or gate change. [Complete validation evidence](dw-337-validation-evidence.md) retains commands, hashes, initial failures and independent holds.

Parent acceptance inspected the unified diff including untracked artifacts and audited the final Debug/Release TRX: all eight frozen matrix rows execute and pass. Each operation has 42 ambiguity/retry cases and 12 propagation cases; permanent/domain/no-dispatch controls also pass. Only DW-337 is reconciled after this acceptance. Three independent workflow reviews and the bounded diagnostic correction are complete; the triage log below and validation evidence record final checks and unchanged blockers.

## Spec Change Log

- 2026-10-16: User resumed the approved spec through bmad-build; status advanced to in-progress and baseline/frozen intent were preserved. Implementation, maintained validation and parent acceptance completed; only DW-337 and its counts are reconciled. Broad/package/runtime and all unrelated release holds remain.
- 2026-10-16: User accepted the walkthrough recommendations. Retain the private classifier, explicit caller-owned retries, cancellation behavior and existing EN/FR resources. Add one deterministic cancellation-during-await regression, including exact token forwarding and one-send/no-replacement-ID assertions; do not expand the repeated-retry matrix. This is a test/documentation follow-up with no production behavior change. Current follow-up validation is appended to `dw-337-validation-evidence.md`; the earlier review and result counts remain historical evidence.

### Walkthrough clarification: classification and recovery

DW-337 standardizes gateway classification and verifies that an explicit retry supplied by a caller preserves the exact command envelope. It does not add a configuration UI redispatch action or establish complete recovery for every operation. Configuration status lookup currently requires a correlation ID, which an ambiguous result does not provide; the set-configuration flow also clears the entered value after submission. Retaining the attempt therefore preserves uncertainty without guaranteeing that the current UI can resolve it. Existing operation-specific messages remain in scope unchanged. Any additional recovery path needs its own payload-handling, authorization, admission and evidence decisions; all separate recovery holds remain open.

## Review Triage Log

Three context-free reviewers ran against the complete unified diff at `/tmp/tenants-dw337-7p8nqkjg.diff`. Edge-case hunter returned `[]`; verification-gap reviewer reported no gaps. Blind hunter supplied ten suggestions (its forced finding floor); each is triaged separately below before grouping. No runtime defect, intent gap or spec loopback was found.

| ID | Finding | Verdict | Route and evidence |
| --- | --- | --- | --- |
| BH-1 | Review log empty while closure text points to it | low | Reject: this snapshot was captured during the active review, whose next step fills this log. The suggested fix edits this build's spec, which the workflow explicitly rejects as a review-finding fix; no completed final record is missing. |
| BH-2 | Gateway ambiguity cases have indistinguishable TRX names | low | patch: independently confirmed 252 results but only 60 distinct display names. Give the existing test exception details status/flag labels so a developer can identify and filter a failing matrix input; retain the hostile canary and all assertions. This is a direct test-data correction, with no new public surface or runtime change. |
| BH-3 | Stub does not assert forwarded cancellation token | low | Reject as optional future-mutation coverage: all six unchanged transport calls explicitly forward the token, and the 72 new propagation cases pin the changed filter behavior. No token-loss path exists in the diff; catching a hypothetical later forwarding edit would add capture/parameter machinery to test unchanged plumbing. |
| BH-4 | No held submission cancels while awaiting | false | The classifier reads `cancellationToken.IsCancellationRequested` at exception-filter evaluation; it captures no pre-await cancellation state. Passing caller-canceled and uncanceled exception controls exercise its two branches. A held schedule cannot select a different branch for the same failure-time token state. |
| BH-5 | Faulted cancellation exceptions omit canceled-task semantics | false | Awaiting a canceled task surfaces OperationCanceledException/TaskCanceledException to this same filter; production never inspects Task.Status. Both exception types, synchronous/faulted delivery and both caller-token states are covered. No different classifier behavior remains untested. |
| BH-6 | No repeated ambiguous explicit retry | false | The gateway retains no attempt counter/state, and no retry-specific branch exists. The passing exact-envelope test asserts one send per invocation and caller reuse; another uncertain invocation runs the identical classifier and cannot mint a replacement ID. |
| BH-7 | Generic terminal statuses only use Retryable=false | false | The sole retryability check is `== true`, so null and false take the same status/permanent paths; none of the five unchanged permanent mapper implementations reads Retryable. Operation-specific domain controls already exercise null and false. No missing metadata behavior can select a new terminal branch. |
| BH-8 | No retryable 401/403 matrix case | false | The shared classifier checks explicit retryability independently of HTTP status before every authorization mapper. Existing retryable 400/409 inputs carry recognized InsufficientPermissions evidence, so the same authorization rejection is demonstrably bypassed. Neither 401 nor 403 has a separate branch in the classifier. |
| BH-9 | Untracked wrappers omit each newly unified input | false | All six unchanged wrappers are direct expression-bodied calls to their tracked counterpart, forwarding request/token and generating their existing initial ID. Existing untracked 503/localization controls run that delegation; there is no alternate wrapper transport classifier for the new cases to miss. |
| BH-10 | Session log/TRX hashes lack a durable artifact store | low | Reject: durable repository evidence already records exact commands, counts, red failure family, source/dependency/binary hashes and limits; raw session paths are explicitly identified as such. No missing product result is established. Adding an external artifact-publishing/storage workflow is more than a direct correction and is outside this local uncommitted fix. |

Only BH-2 survives grouping, as one diagnostic correction. All other findings have the individual disposition above; none is silently merged or deferred. BH-2 is fixed by adding status/flag labels to existing hostile exception detail strings, with all inputs/assertions preserved. The affected 294 tests pass with distinct names. Parent post-review verification passes 768 focused/5954 full UI tests in each configuration and both unchanged Chromium lanes; all 252 six-operation ambiguity names are distinct. Fresh broad/strict-package failures remain exactly the documented two NU1903/seven API errors, with maintained binaries restored. No finding remains open.

## Verification

Final post-review results: Debug source and maintained Release each pass 768 focused gateway/5,954 full UI tests, zero failures/skips; both original Chromium lanes, gitlink and whitespace checks pass. [Final review, commands, hashes and preserved blockers](dw-337-validation-evidence.md#completed-independent-workflow-review-and-final-verification).

From the repository root, after approval:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5532
```

Repeat with `-c Release -p:UseHexalithProjectReferences=false` on the build and `-c Release` on tests. Run browser, broad/package and provenance commands in [the validation plan](dw-337-planning-evidence.md#implementation-validation). Preserve the maintained FrontComposer-source exception. Record skips/blockers separately. Before code edits, establish Aspire health using the owning skill. Approved on 2026-10-16 with the user's instruction to stop before implementation.
