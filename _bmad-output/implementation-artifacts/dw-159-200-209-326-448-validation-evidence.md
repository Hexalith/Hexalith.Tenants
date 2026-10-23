# DW-159 / DW-200 / DW-209 / DW-326 / DW-448 validation

Date: 2026-10-23. Entry revision: `fc5d301dd07683475b7a1bcecf0ba5fab99ab265`.

Exactly five records close. All 657 IDs remain: **555 done / 102 open**, with 92 maintenance index rows covering the open IDs exactly once. Original fields are preserved under `pre-ui-maintenance-*` when superseded. Every unrelated numbered record and remaining maintenance row is unchanged. The prior live index summary was stale at 545/112 although the entry ledger already contained 550 done/107 open; the live table now reflects the ledger. Historical closure summaries remain intact. The repeatable [ledger check](dw-159-200-209-326-448-validate-ledger.py) produces [machine-readable evidence](dw-159-200-209-326-448-ledger-validation.json).

## Qualified behavior

| Record | Change and evidence |
| --- | --- |
| DW-159 | NotFound/Unauthorized configuration surfaces select Unavailable with either available or unavailable safe models. Rendered alert/assertive semantics, absent rows and withheld values are asserted. |
| DW-200 | The actual optional refresh coordinator subscribes to the created tenant. Notifications requery command status and projection evidence through a serialized drain. Held status/list/detail reads, late read failures, caller replacement, disposal, held setup cleanup, manual+nudge coalescing and automatic terminal rendering/unsubscription are covered. Actual workspace notification and five successive manual authority renewals exercise its production list evidence callback, stable caller scope, retained message/correlation IDs, continued subscription and no redispatch/feedback. Successful subscriptions reset the bounded setup-failure budget. Detached manual status completion remains qualified by the existing retained-flow suite. |
| DW-209 | MyTenantsPage, TenantAuditPage and GlobalAdministratorsPage render inside FrontComposerShell, asserting its full-width content measure and each page marker. Existing workspace/full-width and user-lookup/constrained assertions remain. |
| DW-326 | All ten complete-preview facts remain. Shipped EN/FR renderings pin Present/Current/Current proof text. Twelve unreachable resource variants per culture and stale localizer-double entries are removed. Existing incomplete/non-current refusal cases and full localizer parity pass. |
| DW-448 | A retained capped counter delays audit availability after three complete unmatched removal walks. Actual remount, two-page walks, failed read after two walks, configured escalation and later exact receipt recovery preserve confirmed access outcome, exact command identity and causal proof. Available evidence is not downgraded. |

## Final verification

Commands ran from the repository root, using Debug project references for local development and direct xUnit v3 assembly execution:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -m:1
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*CreateTenantFlowTests' -class '*TenantsWorkspaceTests' -class '*TenantRetainedCommandFlowTests'
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll
python3 _bmad-output/implementation-artifacts/dw-159-200-209-326-448-validate-ledger.py
git -c core.safecrlf=false diff --check
./node_modules/.bin/commitlint --edit /tmp/tenants-dw-five-commit-message.txt --verbose
```

All final commands exit 0. Build: **zero warnings/errors**. Focused recovery: **420/420**, zero failures/skips, 28.247 seconds. Full UI: **7,478/7,478**, zero errors/failures/skips/not-run, 145.116 seconds. Local raw logs: `/tmp/tenants-dw-five-build.log`, `/tmp/tenants-dw-five-final-recovery.log` and `/tmp/tenants-dw-five-full-ui.log`. The earlier focused eight-class lane included configuration, layouts, removal state/flow and localizer parity; the final full suite covers those on the final code.

Pinned commitlint CLI `21.2.3` validates the exact candidate `fix(ui): resolve five deferred evidence and recovery records` with **0 problems / 0 warnings**. Successful output is retained in `/tmp/tenants-dw-five-commitlint.log` and reproduced here before commit creation.

## Review and initial failures

The bmad-build one-shot route's independent blind review ran in a separate context. Its nine initial findings and three concrete recheck findings are individually triaged in the [owning spec](spec-dw-159-200-209-326-448-ui-maintenance.md). All actionable findings are patched; no new deferred record is added. The fixes include renderer-bound setup/publication, nonblocking retention/focus ordering, failed-setup-only budgeting, stable caller identity during authority renewal, and detached-only neutral manual status completion.

Initial failures were corrected rather than suppressed:

- The first four-class run passed 542/543: the new My Tenants shell fixture lacked a query response. The fixture now returns an explicit unavailable snapshot.
- A later 852-case run found stale resource doubles, fixture disposal that did not invoke the component lifecycle, missing escalation configuration and late status publication. The localizer doubles and lifecycle fixtures were corrected; publication guards were strengthened.
- A 1,005-case run and isolated held-setup replay exposed synchronization assumptions. The held setup fixture now awaits actual cleanup and replacement-subscription signals instead of relying on transport completion to cause another render.
- The first full suite passed 7,476/7,477 and failed the existing create ownerless-status case. `/tmp/tenants-dw-five-full-ui-before-ownerless-fix.log` retains that result. Exact retained manual completion after detachment is restored; stale attached-caller reads still refuse publication. The final 420-case recovery and 7,478-case full suite both pass.

## Runtime and scope limits

`aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` succeeded before code changes. Initial resource inspection showed security running and downstream resources waiting; later inspection showed EventStore/Tenants resources running and security exited. No healthy authenticated topology or browser acceptance is claimed. The task-owned AppHost was stopped successfully with `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`.

No submodule pointer, dependency version, archive, sprint status or unrelated policy changed. Existing authorization-revocation, same-caller permission-change, authenticated-runtime and release-readiness holds remain open. No release or deployment was performed. The explicit user request authorizes the final main-branch push.
