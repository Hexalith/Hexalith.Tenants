# Review of the November 20 validation corrections

**Passed.** The updated PRD and addendum address the four high, seven medium and two low findings in the November 20 validation report. The review compared the edited requirements with the domain handlers, UI proof and read paths, approved decisions and sprint tracker. [Original findings and evidence](validation-report-pre-update-2026-11-20.md) · [Updated PRD](prd.md) · [Updated addendum](addendum.md)

| Original finding | Verified disposition |
| --- | --- |
| High · Audit performance contract | FR-20, NFR-1, §16.14 and addendum §F use the September 23 budgets and September 24 dedicated-run results. They identify the release candidate and the audit query, BFF mapping, grid and paging changes that trigger another run. |
| High · Configuration disclosure | FR-6 and addendum §C require an exact display-safe key for every viewer. Administrators bypass only the prefix check. The invalid-policy case withholds UI configuration while preserving the authorized API contract. |
| High · Last-owner recovery | FR-24 and addendum §D separate first enrollment from recovery. A previously populated active tenant with zero owners requires a global administrator to restore an owner. |
| High · Tenant audit pairing | UJ-4 and FR-23–25 separate confirmed access from proof of who caused the correction. Addendum §§D and G agree on missing causal support. The tenant path retains the original receipt and withholds a pair. |
| Medium · Governance correction availability | §7.9 and addendum §§A, B and D identify delivered Story 5.7. They describe the per-attempt authority, ordered evidence and last-administrator checks, along with same-circuit, reauthorized proof before pairing governance receipts. |
| Medium · SM-6 observation coverage | §15 proposes a release-candidate window, a release owner and the submitted qualification journeys as the cohort. The owner compares the first success shown or announced with same-command projection evidence available at that instant, reports coverage and unassessed attempts, and reports no result when none can be assessed. |
| Medium · Disabled-tenant behavior | FR-15 and addendum §D give separate outcomes for authorized reactivation, repeated transitions, unauthorized callers, ordinary mutations and bounded access refresh on a disabled tenant. |
| Medium · Hard deletion | FR-15, §14.3 and addendum §B place hard deletion outside UI scope. Any future CLI operation is subject to the McpCli contract and authorization decision. |
| Medium · Read routing and freshness | §11 and addendum §§C, D and I name the direct REST paths including fixed-scope governance audit, define response-level freshness evidence, and keep command/status calls on EventStore. |
| Medium · Governance story IDs | §14.2 and addendum §B map grant to 4.2, removal to 4.3 and correction to 5.7. The June numbering is explicitly historical. |
| Medium · Role promotion | FR-11 and addendum §D distinguish caller authority from valid role selection. Reader-to-contributor promotion succeeds for an authorized owner. Invalid targets receive the role rejection. |
| Low · Fluent version | §16.5 and addendum §D identify the central 5.1.0 selection and require the qualifying build’s actual restored version and accessibility evidence. |
| Low · Metadata editing | FR-14 and addendum §D name tenant contributors, owners and global administrators. Each successful edit of active-tenant metadata emits an update. |

Product and Release still need to confirm the proposed SM-6 measurement before its first report. The four deployment and release checks in addendum §J remain with their assigned owners.
