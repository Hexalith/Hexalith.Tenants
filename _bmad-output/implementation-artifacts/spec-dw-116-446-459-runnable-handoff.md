---
title: 'Runnable deferred assistive-technology handoff'
type: 'chore'
created: '2026-10-30'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make `docs/tenants-ui-deferred-at-handoff.md` runnable by extending the existing authenticated browser fixture with terminal controls for S3 (reject exactly one fallback continuation and allow the authorized first page) and C1–C3 (select status evidence independently of projection delivery). Reuse the signed local OIDC fixture, production UI process, and existing loopback HTTP/SignalR upstream; add no production hooks or new control service. Document exact Windows/NVDA prerequisites, setup, actions and evidence boundaries. Verify the controls in the browser and obtain independent review, then commit and push reviewed changes to main. Keep DW-116/446/459 open until actual speech passes; leave DW-137 and DW-466 unchanged.

</frozen-after-approval>

## Implementation Notes

- Planning facts: no unresolved intent gaps; test fixture changes have no domain or deployment effects. The requested final Git publication is authorized. The bounded footprint is the existing authenticated administrator runner, handoff helpers/browser checks, this protocol and validation evidence. Use the oneshot route.
- Reuse `validate-authenticated-administrator-dialogs.mjs` standalone signed OIDC startup and cleanup. Opt-in handoff modes must bypass unrelated administrator scenarios and retain existing default behavior.
- Reuse the navigation fixture's search response and one-shot invalid-cursor pattern. Keep opaque cursors internal. Use terminal stdin, not an HTTP control endpoint.
- C1 returns exact-attempt EventsStored HTTP evidence; C2 returns HTTP 200 `{}` for an unverifiable status (404 is propagation lag). Projection visibility is a separate control, initially unable to confirm the submitted attempt. Same-state refresh reuses status without dispatching another command.
- Browser acceptance will exercise the same control parser as manual use, including EN/FR notices, first Pending/Unavailable, both Refresh controls, projection release, and zero unexpected commands/circuit failures. No speech claim or ledger closure follows browser evidence.

- Implementation: added `deferred-handoff-fixture.mjs` and `validate-deferred-handoff.mjs`, selected through opt-in modes on the existing authenticated administrator runner. Existing modes retain their scenarios. Controls use stdin; HTTP/SignalR/OIDC infrastructure is reused. Projection hold is immediate absent evidence, preventing timeout-driven state changes.
- Evidence records Fluent package/module/assembly hashes, source/fixture identities, session ordinal/culture/start-end times, and sanitized UI activation/notice/audit-state plus upstream response-category times. Manual sessions have no automated pass verdict or closure eligibility.
- Windows protocol now includes native setup, culture sessions, exact EN/FR buttons/keyboard steps and a same-machine speech timeline. Native Windows/NVDA was unavailable here; no speech observed.

## Review Triage Log

Independent blind review and a separate protocol review ran with the inherited model. No configured review layer was skipped. Both reviewers confirmed no remaining findings after corrections.

| Finding | Verdict and evidence | Disposition |
| --- | --- | --- |
| Malformed/unauthenticated command refusals escaped accounting | Medium: reproduced direct malformed POST returning 400 with zero unexpected commands | Patched centralized send/refusal accounting; direct invalid-request checks pass |
| Browser search bypassed documented debounce interaction | Medium: URL navigation skipped Search tenants input | Patched browser checks to fill/Tab/wait for debounced search |
| CSS-only C3 directions lacked spoken button names | Medium: both controls need distinct EN/FR names for NVDA navigation | Added exact resource labels, locations and Tab/Enter steps |
| Speech/trigger/DOM timestamps lacked alignment method | Medium: search notices/activation were not observed | Added UTC UI and upstream timelines plus same-machine clock/recording protocol; browser asserts event coverage |
| Fluent runtime identities missing | Medium: packet could not identify tested component runtime | Added package version, served module SHA-256 and assembly SHA-256 |
| Session records lacked culture/ordinal/time boundaries | Medium: search-only sessions could be ambiguous | Added session ordinal, culture and start/end times |
| Empty manual session printed passing qualification | Medium: zero-case run appeared successful | Manual packet has `passed=null`, separate completion field and explicit no-verdict console text; closure remains false |
| JSON null parsed but dereferenced before refusal | Medium: both reviewers reproduced null claims/command TypeError | Added object/non-null/array guards; reviewers independently verified seven refusal shapes, no exceptions and exact accounting |

- Final browser verification: Chromium 154.0.8037.57 and Firefox 155.0 each pass 28/28 EN/FR cases, with eight simulated Creates per run and zero unexpected commands/circuit errors. A headed Xvfb terminal smoke records EN→FR fresh sessions, controls and quit with no automated verdict. Exact source/runtime hashes match final runner/helper bytes. Evidence and commands: [validation](dw-116-446-459-runnable-handoff-validation.md).
- Whole ledger, archive and sprint-status files remain byte-identical to baseline; DW-116/446/459 stay open and DW-137/466 unchanged. Main publication is explicitly authorized by the user and follows pinned commitlint validation.
