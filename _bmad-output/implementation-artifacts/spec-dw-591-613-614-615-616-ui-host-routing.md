---
title: 'Resolve five deferred Tenants UI host and router records'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'db2c6961724d1084d7035533ba41732878de14de'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-591, DW-613, DW-614, DW-615 and DW-616 as one bounded Tenants host/router correction. Give unmatched interactive routes a localized, accessible recovery page and a fixed safe link through the existing FrontComposer layout and Fluent components. Qualify the actual host's English/French document language, multi-tenant authenticated claim mapping, and anonymous scope boundary in the mandatory UI test lane. Qualify navigation to the shared FrontComposer home route using the mounted production interactive router.

Given an English or French request, when the actual host renders a document, then its html language matches the supported request culture. Given a signed-in principal with multiple authorization tenants and one current-tenant claim, when the actual host renders a scoped page, then its real FrontComposer accessor selects only the current tenant and the subject. Missing or conflicting current-tenant evidence fails closed. Given an anonymous request to any of the six canonical Tenants UI routes, then the actual host renders scope denial without domain content or gateway reads. Given one mounted interactive router, when navigating to an unknown route and then following recovery to /home, then the localized recovery and framework-owned home render in the shell without replacing the router.

Verify the focused and full UI suites, close exactly these five records with traceable evidence while preserving unrelated records and release holds, then commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation: current language proof inspects App.razor source; six hosted scope checks are advisory DaprFact tests; Program.cs already configures the correct alias but lacks signed-in host coverage; Routes has no recovery page and no interactive navigation regression. Reuse WebApplicationFactory and the existing shell/Fluxor bUnit setup. Use .NET 10 Router.NotFoundPage with a routed page, following the installed API and Microsoft documentation.
- Small reversible footprint: Routes, one new Razor page, EN/FR resources, focused host/router tests, this spec and closure evidence/backlog records. No product choices, new service API, dependency updates or deployment. Authentication fixtures use host-protected cookies and the real accessor, never a production authentication bypass.
- The existing Aspire app was inspected before edits: all 23 resources Running/Healthy. Development validation uses Debug source references. The five requested records form one host/router acceptance goal; final commit/push is explicitly authorized.

- Review extended the same recovery goal to intercepted-link HTTP fallbacks: Program re-executes only 404 responses at the same recovery route; 405 remains unchanged. Actual EN/FR signed-in and anonymous HTTP cases qualify both paths with Accept-Language. Opposing formatting/UI culture, enabled/disabled authentication on every canonical anonymous route, missing/conflicting subject claims and the framework focus target strengthen the acceptance checks.
- The mandatory full suite exposed an unchanged metadata admission race (7203 total, two failures: new-page layout inventory and held-authority metadata). The metadata case also failed alone and with eventual-state waiting. The narrow gate repair publishes the lifecycle revision before cancelling held authority in OnParametersSet; the existing regression preserves Idle, no delivery, no pending ownership and balanced activity assertions. The conformance inventory now names the new constrained page. No additional ledger record is closed.

## Review Triage Log

- Medium / patched: intercepted unknown links can force an HTTP request; the host now supplies 404 recovery and real request controls. Recheck also caught the stock middleware's wider status range; a filter and real 405 control preserve other failures.
- Low / rejected: browser activation of the Fluent recovery anchor is not exercised. The test checks the rendered native link target and routes that exact target on the same production Router; HTTP fallback is separately qualified. Browser transport/activation is explicitly outside this evidence, and adding a browser fixture would exceed a simple correction without a demonstrated defect in this shared component.
- Medium / patched: anonymous cases covered only the host without OIDC. Both configurations now run all six routes, including the real configured OIDC challenge on the privileged endpoint, without external discovery or replacing handlers.
- Medium / patched: identical formatting/UI culture inputs could hide a CurrentCulture regression. The new opposing-culture case requires French from UI culture independently of English formatting culture.
- Low / patched: query-only localization omitted browser language negotiation. EN/FR HTTP fallback tests use Accept-Language without culture query parameters.
- Medium / patched: signed-in negatives lacked missing/conflicting subjects. Both now require denial and zero domain-gateway resolutions.
- Low / rejected: observing constructed gateway scope does not qualify downstream request arguments. DW-614 covers the real host's alias mapping and rendered boundary, which are directly asserted; no downstream transport qualification is claimed and no wrong request was demonstrated.
- Medium / patched: the unrelated existing held-authority metadata race blocked the mandatory suite. Publishing lifecycle revision before cancellation closes the reproduced ordering window; all original terminal/safety expectations remain.
- False / withdrawn: the reviewer initially inspected Router v10.0.0 and concluded NotFound bypassed Found. The pinned runtime is 10.0.13, whose RenderNotFound calls Found; the added regression requires FocusOnNavigate to target RouteNotFoundPage with h1. No duplicate custom focus code is needed.
- Final independent recheck confirms the 404 filter ordering, authentication/subject controls and lifecycle ordering repair, with no remaining concrete findings. No review work was deferred.

- Final verification: full UI 7217/7217 passed, zero failures/errors/skips (133.664 seconds); focused affected classes 181/181 passed. Debug source and final test builds have zero warnings/errors. Scoped whitespace formatting, Git whitespace checks, exact-spec gitlink guard and five-record closure verifier pass. The formatter reports workspace-loading warnings only. Lockfile-pinned commitlint 21.2.3 validates the exact final message with zero problems/warnings. Raw command logs and tested-source hashes are retained in dw-591-613-614-615-616-raw-logs.zip.
