---
title: 'Preserve route heading visibility during eager form initialization'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
route: 'oneshot'
baseline_commit: 'be43b928ce700ae7433b1d078eec356ccd6079f8'
owning_repository_baseline_commit: 'e4df7256be0e89b48c830c0792033ecfeabec9ad'
gitlink_audit_ref: 'ae40c9657175ae5567cdd182384d3840af64d8a2'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="User authorized fixing CI failures and bypassing human validation">

## Intent

Fix the FrontComposer Firefox Quality failure exposed while synchronizing Tenants dependencies. Its route-contract palette activation test focuses the Counter heading, but an eager CompactInline form starts smooth scrolling before route focus and subsequently scrolls that heading above the banner. Preserve the existing heading geometry assertion and all automated gates. Suppress eager expand-in-row scrolling when the document has no operator focus (no active element or the body), retaining the existing heading guard and later user-triggered expansion. Preserve reduced-motion behavior and existing scroll correction semantics.

</frozen-after-approval>

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.FrontComposer`

- `references/Hexalith.EventStore`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`

## Completion Notes List

- The root release sync moved `references/Hexalith.FrontComposer` to the verified Firefox route focus and generator snapshot revision.
- That same root sync moved `references/Hexalith.Builds` to its already verified SDK fixture revision; its implementation is recorded in the companion spec.
- `references/Hexalith.Builds` 10163c3e4ffd84ddb9b2831dea767a04da04f2ef -> 70850b18e0d0b80facd2df0809974020dd935732
- `references/Hexalith.FrontComposer` e4df7256be0e89b48c830c0792033ecfeabec9ad -> f842711d67004200e3248b3c42b7d9bcb2146907

Historical audit reconciliation (2026-11-15): `ae40c9657175ae5567cdd182384d3840af64d8a2` is the latest committed story-associated pointer event before the current documentation-only repair. The unchanged baseline is `be43b928ce700ae7433b1d078eec356ccd6079f8`. The complete baseline-to-endpoint inventory below includes incidental workspace dependency movement; it records historical range contents and grants no runtime, publication, enrollment or maintainer approval. Original implementation notes, checklists and source-specific receipts retain their original attribution.

| Dependency | Historical baseline | Historical audit endpoint |
| --- | --- | --- |
| `references/Hexalith.Builds` | `10163c3e4ffd84ddb9b2831dea767a04da04f2ef` | `725754075e1d6369430a3adf94d46fb71e6eaf9e` |
| `references/Hexalith.EventStore` | `f06abada8dc3aab4a5fd240847fd680d515c98a5` | `16dff1f018e8a68a9d244975e8228d96429eb61c` |
| `references/Hexalith.FrontComposer` | `e4df7256be0e89b48c830c0792033ecfeabec9ad` | `b105c846c31844441f1cff84b5c187da0eff3546` |
| `references/Hexalith.McpCli` | `edaecb04b1a8edf78f6ad28e5bf253db594003f4` | `58afd4766849e33e75471c9047ee945025ca3b6e` |
| `references/Hexalith.Memories` | `d9d61d8a95c7dc8827acf68ae723859711703729` | `d959bdae5af9c22f53573eda71c091cb5c0aae4c` |
| `references/Hexalith.Platform` | `1f5958e24d1b414d8956d079270f89556e23415d` | `8432397c66ebc0fd22e5671ce620a7d2139f7453` |

The original owning-repository baseline `e4df7256be0e89b48c830c0792033ecfeabec9ad` and creation record `35ddf94679ea92fb937b59f4202c4352bf7edf49` remain historical provenance. The corrected superproject baseline already exists in both this endpoint and the event-base story, so this later audit uses that unchanged committed start. Its earlier correction endpoint was `35ddf94679ea92fb937b59f4202c4352bf7edf49`; the broader endpoint above additionally retains the later story-associated pointer event.

## Implementation Notes

- Work in `references/Hexalith.FrontComposer`, currently clean main at `e4df7256be0e89b48c830c0792033ecfeabec9ad`; inspect owning repository guidance, configuration, and existing focused tests before editing. Do not load any skill from references. Do not stage, commit, push, or initialize nested submodules; root owns those operations.
- Change only `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js` and its existing `tests/e2e/scripts/expand-in-row.test.mjs` regressions unless evidence shows another minimal change is required.
- Quality run `41553133076`, Firefox job `125645411183`: `tests/e2e/specs/route-contract.spec.ts` palette route activation test expects focused heading y >= banner bottom 48; three attempts returned y -124/-133/-154. Trace showed shell scrollTop continuing 144 -> 167 -> 184 after heading focus. Chromium, WebKit, and all other Quality jobs passed.
- Existing `fc-expandinrow.js` skips only when an h1 already owns focus, so eager firstRender initialization races earlier than this guard. The proposed correction prevents starting that animation while no operator owns focus; do not mask the browser failure by loosening assertions, adding retries, or skipping Firefox.
- Add a focused regression proving body focus queues no scroll or animation; verify absent active element too if the existing harness supports it. Preserve and run tests for user-triggered scroll, reduced motion, and heading protection.
- Run the existing Node test file. Run the existing focused Firefox route-contract test using its tracked configuration if practical; report exact commands/results and any environment limitation. Do not start an Aspire AppHost just for this non-Aspire browser fixture.
- Return concise changes and verification evidence. Root will review the diff and complete remote CI before claiming this fix complete.

## Review Triage Log

| Finding | Verdict | Evidence and action |
| --- | --- | --- |
| Blind 1: blanket body-focus guard suppresses user row expansion | medium; patch | Verified generated row handlers dispatch ExpandRowAction without guaranteeing trigger focus. Use an optional internal JS deferToRouteFocus flag only from eager CompactInline initialization; retain the normal row wrapper's one-argument behavior and public C# API. |
| Blind 2: correction still runs after focus transitions to body/null | medium; patch | The callback checks only connection and heading focus. Apply the same eager-only focus protection to deferred correction, with explicit transition regressions. |
| Blind 3: tests are mocked DOM only | false | The implementation also ran the repository's actual Firefox palette route-contract test successfully with its unchanged geometry assertion. Preserve that browser regression and validate generated eager-call arguments plus ordinary row behavior after patching. |

Edge-case review returned no findings. Verification-gap review found no gaps. Re-engaged the implementation agent for the narrowly scoped interop correction.

- Final patch uses an optional internal JS deferToRouteFocus flag only from generated eager CompactInline initialization. Normal row expansion keeps its existing one-argument interop behavior, including body focus. The same scoped guard protects queued correction. Public C# APIs remain unchanged.
- Verification passed after rebuilding changed generator/assets: Release build with zero warnings/errors; 20 Node regressions; 8 generated CompactInline class tests; actual Firefox palette route-contract test with unchanged heading geometry assertion. Reports: `/tmp/fc-firefox-route-focus-scoped-implementation`. Remote Quality CI will supply full cross-browser evidence.

- Owning-repository commit `a1a9280b28e291ee5cfaf9b7b6e3b5e4500ae805` passed its locked commitlint preflight and was pushed after fetching and confirming a linear continuation of origin/main.

- Root additionally ran the source generator snapshot class: 7 of 35 tests initially failed solely because generated interop gained the explicit true argument. Compared every received snapshot to its prior expected text and asserted exactly that single-line change; refreshed seven snapshots preserving original bytes/line endings otherwise. Rerun: all 35 passed, zero skips. Snapshot commit `f842711d67004200e3248b3c42b7d9bcb2146907` passed locked commitlint.

## Resumed Review — 2026-11-15

The named build resumes this existing `in-review` spec. The repair was already committed and consumed by Tenants; this run closes the outstanding review and remote CI verification. Tenants HEAD is `d73267fdbe84d09ccddf0a6492390e32d8938405`; FrontComposer HEAD is `b22be84bdbb23d9263a8835496ad0c95adf1843f`. Pre-existing dirty files in both repositories were preserved, with no submodule initialization, staging, commits, pushes, or gitlink changes.

The review diff covers the complete original repair in its owning repository, `e4df7256be0e89b48c830c0792033ecfeabec9ad` → `f842711d67004200e3248b3c42b7d9bcb2146907`, rather than attributing intervening unrelated development to this fix. The integrated scroll module and Node regression file were unchanged since that repair before the documentation patch below. Reviewers inspected the diff and current callers; fresh verification uses the current workspace. All three independent review layers completed. Edge-case and verification-gap reviewers returned no findings.

| Finding | Verdict | Evidence and action |
| --- | --- | --- |
| Resumed blind 1: every generated first render defers, including a hypothetical standalone user-opened form with body focus | false | No supported consumer exhibiting that failure was identified. The actual generated user expansion path uses `FcExpandInRowDetail.OnAfterRenderAsync` → `ExpandInRowJSModule.InitializeAsync`, which retains the one-argument call and scrolls with body focus. Both modes are observed by the Node matrix; the eager generated call passing `true` is observed by bUnit. Suppressing generated eager work without operator focus is the approved behavior. |
| Resumed blind 2: navigation chrome or tab focus can still start eager scrolling before heading focus | false | The guard does not claim all navigation elements are headings. In the current integrated route owner, `fc-focus.js` reveals incoming headings with `scrollIntoView({ block: 'start', behavior: 'instant' })` before focusing and confirming them, cancelling incoming smooth scrolling. The unchanged Firefox route-contract assertion and all three remote browser jobs pass. |
| Resumed blind 3: the queued guard does not cancel an already-started smooth scroll | false | The callback suppresses additional correction; current route focus owns cancellation through its instant heading reveal. This division is explicit at `fc-focus.js` immediately before `ConfirmRouteHeading`. The existing real Firefox regression passes without retrying or changing geometry assertions. |
| Resumed blind 4: mocked geometry covers only bottom overflow, leaving the threshold, top overflow, and no-correction branches untested | low; rejected | Those algorithm branches and the geometry fixture predate this repair and remain unchanged. No incorrect boundary result was demonstrated. Parameterizing this harness would add unrelated coverage complexity without addressing a verified regression; the route-focus matrix directly observes every changed guard. |
| Resumed blind 5: the module header omits the new optional argument and immediate-return semantics | low; patch | Clarified `deferToRouteFocus`, its generated caller, the ordinary row default, and the absence of a queued retry. Only the existing module comment changed; CRLF was preserved. The 20 Node tests pass after this documentation patch. |

No findings were deferred. The only new source edit is the interop documentation clarification; the runtime repair remains the existing committed implementation.

## Verification — 2026-11-15

Fresh focused checks passed: **20 Node regressions, 8 CompactInline bUnit tests, 35 renderer-generator tests, and the actual Firefox palette route-contract test (64 total; zero failures or skips)**. Both focused Debug builds passed with zero warnings and errors. Build outputs were contained in the private `TMPDIR` supplied by `run-with-temp`; the owned Counter host was stopped and waited for before cleanup. The standalone Counter fixture needs no Aspire AppHost.

Commands, logs, original repair diff, and Playwright reports are retained in [the evidence directory](evidence/fix-firefox-route-focus-ci-2026-11-15/). Reproduce the fresh build, C# tests, and Firefox check from `references/Hexalith.FrontComposer` with:

```bash
run-with-temp bash ../../_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/verify.sh
run-with-temp node --test tests/e2e/scripts/expand-in-row.test.mjs
git diff --check
```

The verification script uses the tracked Playwright configuration, a fresh Debug Counter host, one Firefox worker, and zero retries. It preserves the original heading geometry assertion. `NuGetAudit=false` and `MinVerVersionOverride=1.0.0` are local build environment pins only; no tracked CI gate was changed.

GitHub confirms the original repair's [Quality run 41559059800](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41559059800) succeeded, including Firefox job `125665255299`, Chromium job `125665255373`, and WebKit job `125665255366`. The latest completed Quality run at the local FrontComposer HEAD, [43728107585](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43728107585), also passed Firefox job `132854367075`, Chromium job `132854366982`, and WebKit job `132854367204`. Its overall failure is in the separate `Prepare exact governance catalog objects` build step, not the browser jobs. A newer remote revision `60d2088c26e5cedb7c61471d2e4b813a86996ea2` had CI running when inspected; no success is claimed for that in-progress run. API results are retained in `ci-results.json`.
