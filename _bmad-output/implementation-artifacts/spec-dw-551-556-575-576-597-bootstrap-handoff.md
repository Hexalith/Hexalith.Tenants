---
title: 'Bootstrap administrator compatibility and safe operator handoff'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '870d677cbac5bb54d8a542a289421da8a9243b53'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Bootstrap assumes a Keycloak endpoint and offers no in-process recovery. The quickstart uses an invalid bootstrap probe and lacks an executable administrator handoff. The UI rejects the shipped administrator's valid multivalued tenant claim.

**Approach:** Complete DW-551, DW-556, DW-575, DW-576 and DW-597 as one administrator-access batch. Establish endpoint and recovery policies, document authoritative bootstrap verification and a local human grant, and recognize exact system membership among valid tenant claims. Verify, commit and push to main as requested.

**Decisions (user approved 2026-10-23):** Add a validated configurable token endpoint, retaining the Keycloak default when absent. Preserve one startup attempt and document explicit operator recovery rather than adding automatic retries. The endpoint must be an absolute HTTP(S) URI without userinfo, query or fragment; require HTTPS outside Development, reuse existing authority/subject/response safeguards, and reject invalid configured overrides without falling back. Treat the configured endpoint as an operator-trusted credential destination.

## Boundaries & Constraints

**Always:** Bind bootstrap target, delegated subject and authenticated actor. Retain HTTPS outside Development, bounded responses, cancellation and secret-free diagnostics. Verify projected membership after command acceptance. Preserve malformed-claim, ambiguous-identity and subject-corroboration guards.

**Never:** Reveal generated credentials, invent tokens, delete state, bypass authorization, change AppHost wiring, update dependencies/submodules, or amend the outstanding revocation policy.

## I/O & Edge-Case Matrix

| Input / State | Required behavior |
| --- | --- |
| Valid authority and matching delegated subject | Acquire the credential under the selected endpoint contract |
| Invalid endpoint, insecure production transport or wrong subject | Send no bootstrap command; log safe reason only |
| Credential/command failure or ambiguous delivery | Follow selected recovery; verify status and membership before further submission |
| Configured administrator exists | Authenticate and verify authoritative membership before handoff |
| Provisioned human | Existing administrator submits SetGlobalAdministrator; wait for status and projected membership before human use |
| system plus valid tenant claims | Recognize system; malformed/ambiguous evidence remains indeterminate and missing exact system remains denied |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs` — AcquireFromAuthorityAsync validates authority, bounds the response and checks sub, but appends the Keycloak endpoint.
- `src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs` — schedules one attempt after ApplicationStarted; logs accepted, already-bootstrapped and failure outcomes.
- `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs` — already binds bootstrap target to authenticated actor; retain DW-567 repair.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs` — ResolveSystemScopeEvidence rejects distinct multivalued scopes; retain per-value validation.
- `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs` — reuse authority, cancellation, response-boundary and diagnostic fixtures.
- `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs` — split current valid-multivalue and malformed-value assertions; exercise resolver and policy.
- `docs/quickstart.md`, `docs/production-auth-readiness.md`, `docs/production-auth-claim-contract.md` — settings, verification, recovery and handoff.
- Existing shared security parameters security-eventstore-client-username/password accept operator-selected secrets. AppHost binds fixed subject 11111111-1111-1111-1111-111111111111. Use these inputs without reference-code edits.

## Tasks & Acceptance

**Execution:**
- [x] Bootstrap implementation/tests above — qualify the chosen endpoint and recovery contracts; update readiness guidance.
- [x] Claim resolver/policy tests above — accept valid multivalued system membership; cover duplicates, wrong case, absent system, malformed values and cross-identity evidence, including shipped realm shape.
- [x] Auth documents above — replace the bootstrap-write probe with membership verification; provide executable secret-input, token, human grant, status and projection checks; explain persistent-Keycloak limitations.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` and matching evidence — close exactly the five verified records while preserving prior/unrelated content.

**Acceptance Criteria:**
- Given invalid authority/subject evidence, when bootstrap runs, then no command dispatches and diagnostics expose no secrets.
- Given bootstrap failure, when selected recovery runs, then the configured identity and existing membership are verified before continuing.
- Given a provisioned human, when the administrator grants authority, then instructions require projected membership before human use.
- Given shipped valid multitenant claims, when UI authority resolves, then exact system membership is recognized while malformed/ambiguous principals fail closed.
- Given the entry ledger, when completion is verified, then exactly DW-551/556/575/576/597 close.

## Implementation Notes

- User approved applying both recommended policies and continuing this build on 2026-10-23. No further policy question remains.
- Baseline focused bootstrap tests: 85 passed, zero failed/skipped, direct built-assembly execution.

- Entry: 870d677cbac5bb54d8a542a289421da8a9243b53, clean main matching origin/main.
- Footprint: bootstrap/claims, focused tests, three guides, spec/evidence. External publication is the authorized Git push.
- Aspire start/describe: all resources Running/Healthy. Bootstrap telemetry nevertheless reports HTTP 400; resource health does not prove bootstrap. Source Debug Server baseline builds with zero warnings/errors.

- Parent diff/matrix audit: all four execution tasks complete. Endpoint/subject rows are covered by the 118 bootstrap cases; recovery and handoff rows by 20 executable documentation fixtures and seven documentation tests; multivalued principal row by 94 policy/resolver cases. Both full affected suites pass in the recorded final lanes. Parent reran the documentation/preservation checker, whitespace and gitlink checks successfully.

- Final qualification: parent complete Server run passes 1,475/1,475; maintained Python loopback tests pass 19/19. Earlier complete UI serialized run passes 7,448/7,448; its source/test changes are unchanged by review patches. Exact-five ledger, whitespace, gitlink and pinned commitlint checks pass. The parent-started Aspire instance was stopped successfully after baseline inspection. User explicitly authorized committing and pushing main.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| Blind 1 | medium: the credential-aware launch is followed by another unconditional launch; a reader can start a second topology without the selected parameters. | patch |
| Blind 2 | medium: initial membership failure executes exit before recovery helper definitions, destroying the shell required by recovery. | patch |
| Blind 3 | medium: curl has no deadline, so a stalled response defeats bounded polling iterations; inspectable helper confirms this. | patch |
| Blind 4 | medium: both mutation functions overwrite nonempty retained IDs before a second POST, so repeating an unresolved function loses recovery identity. | patch |
| Blind 5 | medium: the subsequent human authentication fence has no HANDOFF_PROJECTED guard, so an interactive caller can continue after failure. | patch |
| Blind 6 | medium: hosted startup generates IDs but neither records them nor retains the accepted response; the newly documented recovery lacks an obtainable startup handle. | patch |
| Blind 7 | medium: membership helpers discard existing freshness/lifecycle/version headers while claiming current evidence; current UI projection loader explicitly checks them. | patch |
| Blind 8 | low: Failed is not in the shipped CommandStatus enum, while PublishFailed is; the helper waits until timeout rather than explicitly stopping unresolved publication recovery. It does not actually resubmit automatically. | patch |
| Blind 9 | maybe-false: default-handler redirect policy is not established by the mocked transport tests. The same HttpClient SendAsync/handler behavior predates this change; a real-handler 307/308 credential-forwarding test and owning transport policy would settle it. | defer as pre-existing, unverified medium |
| Blind 10 | medium: shell fixtures replace platform_api and token_subject, so actual curl descriptors/config and subject decoding are not executed. | patch |
| Edge 1 | medium: carried distinct reviewer report of the same unbounded curl call as Blind 3. | patch, group Blind 3 |
| Edge 2 | medium: carried distinct reviewer report of the same duplicate AppHost launch as Blind 1. | patch, group Blind 1 |
| Edge 3 | medium: carried distinct reviewer report of the same shell exit/missing helpers as Blind 2. | patch, group Blind 2 |
| Verification gap 1 | medium: pre-verified gap; normal tests check substrings, while executable fixtures are only an implementation artifact with no CI invocation. A missing-projection success mutation escapes normal checks. | patch |
| Verification recheck 1 | medium: pre-verified remaining gap; removing the bootstrap projection wait in memory still passes all 18 maintained cases because only the human path has a missing-projection assertion. | patch: add the direct completed-bootstrap/missing-projection negative and guard-removal control |
| Final recheck | resolved: general reviewer independently confirmed all nine actionable original findings; verification reviewer confirmed CI wiring and transport fixtures. Parent added and passed the direct bootstrap missing-projection case, then proved that removing only its projection guard in memory makes it fail. | all patches qualified; pre-existing redirect question retained as an unnumbered follow-up |

## Verification

Build affected Server/UI test projects in Debug with source references. Run focused bootstrap/claim-policy and both full affected suites plus relevant documentation checks. Validate shell/JSON examples without exposing secrets; distinguish live from deterministic evidence. Prove exact-five ledger preservation, run git diff --check, validate the complete message with pinned commitlint, fetch/inspect divergence, commit, push main and verify remote revision.
