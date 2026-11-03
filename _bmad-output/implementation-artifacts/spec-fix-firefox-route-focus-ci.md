---
title: 'Preserve route heading visibility during eager form initialization'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'be43b928ce700ae7433b1d078eec356ccd6079f8'
owning_repository_baseline_commit: 'e4df7256be0e89b48c830c0792033ecfeabec9ad'
gitlink_original_baseline: 'e4df7256be0e89b48c830c0792033ecfeabec9ad'
gitlink_creation_ref: '35ddf94679ea92fb937b59f4202c4352bf7edf49'
gitlink_audit_ref: '35ddf94679ea92fb937b59f4202c4352bf7edf49'
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

## Completion Notes List

- The root release sync moved `references/Hexalith.FrontComposer` to the verified Firefox route focus and generator snapshot revision.
- That same root sync moved `references/Hexalith.Builds` to its already verified SDK fixture revision; its implementation is recorded in the companion spec.
- `references/Hexalith.Builds` 10163c3e4ffd84ddb9b2831dea767a04da04f2ef -> 70850b18e0d0b80facd2df0809974020dd935732
- `references/Hexalith.FrontComposer` e4df7256be0e89b48c830c0792033ecfeabec9ad -> f842711d67004200e3248b3c42b7d9bcb2146907

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
