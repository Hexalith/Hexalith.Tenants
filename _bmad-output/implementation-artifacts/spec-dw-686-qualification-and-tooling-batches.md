---
title: 'Canonical boundary qualification and tooling follow-ups'
type: 'bugfix'
created: '2026-11-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '44d529248d08d6cb2594657d84eb8a823a4c27d5'
context: ['{project-root}/AGENTS.md']
---

<frozen-after-approval reason="human-owned intent">

## Intent

Qualify the current quickstart, retained package consumer, browser evidence, and native authorization and recovery coverage. Repair the four FrontComposer defects in its owning submodule. Implement the five named batches in subagents and defer test execution until all implementations are available.

## Boundaries & Constraints

Preserve existing storage and authorization, current package cohort checks, supported historical archives, browser evidence hygiene, and FrontComposer compatibility where the existing representation is unambiguous. Use `run-with-temp` for disposable checks. Keep all work uncommitted and avoid nested submodule initialization.

</frozen-after-approval>

## Code Map

- `docs/quickstart.md` and `tests/scripts/test_bootstrap_handoff.py` contain executable Markdown and its loopback fixture. `TenantCatalogDeclarations` and EventStore `CommandStatusController` define operation scope and exact permissions. Inspect the local realm template and bootstrap recovery documentation for shared helper behavior.
- `scripts/verify-catalog-package-consumer.py` builds `tools/catalog-package-consumer/Program.cs` with a single approved list today. Pre-recovery stable net10 archives cover 23 routes and 12 adapters. Current net11 covers 25 routes and 14 adapters. `Directory.Build.targets` already runs `ValidateCanonicalBoundaryPackageCohort` before nuspec generation.
- `validate-authenticated-workspace-focus.mjs` independently accepts the UI origin and hashes a Debug assembly. Neighbor validators use Aspire discovery, a registered UI process, and private evidence. Verify live identity before credentials or browser evidence can be used.
- `AuthenticatedTenantsRouteTests` already exercises native removal and owner refresh. The fixture and production `Program.cs` separately register the same gateway-backed reader. The source-catalog fixture has broker and actor evidence that can be reused.
- FrontComposer `AttributeParser` and emitter consumers lose projection container identity. `SchemaFingerprintTransform` joins collections before escaping. `CommandParser.ValidateDefaultValueType` uses mixed file/attribute coordinates. `eng/stryker_compat.py::prepare` publishes the cache path before verification.

## Tasks & Acceptance

**Execution**

- [x] `docs/quickstart.md` and the existing documentation regressions. Update executable administrator handoff and first tenant workflow for EventStore 5.6.0 and McpCli 3.0.0. Qualify request payload, scope headers, status, and generated REST reads.
- [x] `scripts/verify-catalog-package-consumer.py`, `tools/catalog-package-consumer/`, and `tests/scripts/`. Bind retained net10 to its independent historical route and adapter inventory, qualify a real archive, and exercise the four owner package preflights with both formerly qualified selections.
- [x] `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-workspace-focus.mjs` and focused control. Match the browser origin to the measured running UI assembly and fail closed for a stale host.
- [x] `src/Hexalith.Tenants/Program.cs`, native reader composition, and `tests/Hexalith.Tenants.IntegrationTests/`. Share production registration and extend authenticated tests to assert exact source evidence, stale-grant refusal, committed refresh snapshots, actual projection, and consumer convergence.
- [x] `references/Hexalith.FrontComposer/`. Carry source and CLR projection identity through generation, frame ambiguous skill collections, use the attribute’s file and coordinates in HFC1012, and make Stryker cache publication atomic with owned staging and retry controls.

**Acceptance criteria**

- Given an authorized local administrator, when following the current walkthrough, then administrator operations use `platform:global-administrators`, tenant commands and status use `platform:tenants`, and single-tenant reads use their route tenant identity.
- Given the corresponding retained and current archive pairs, when the consumer runs, then each matches its historical or current inventory. Given EventStore 3.133.0 or McpCli 1.1.0, when any of the four current owner packages packs without building, then preflight rejects that dependency selection.
- Given a matching running UI, when validating focus, then evidence includes verified assembly identity. Given a stale host, then validation fails before authentication.
- Given native grant and removal and an authenticated owner refresh, when the committed changes reach their readers, then the bound gateway rejects a stale grant and both the owner projection and registered consumer converge with the stored snapshots.
- Given a nested projection, a partial command, ambiguous corpus values, or interrupted Stryker preparation, when their respective tools run, then generated code compiles, diagnostics identify the attribute file, fingerprints distinguish element boundaries, and preparation can safely retry.

## Implementation Notes

- Quickstart qualification passed the 22 shell cases and seven C# documentation cases. The command path requires a configured catalog and an approved interactive issuer.
- Published net10 7.0.2, 7.0.6, and 7.0.7 matched 23 routes and 12 adapters. Published net11 8.1.1 matched 25 routes and 14 adapters. The real no-build preflight accepted the current cohort and rejected eight old-cohort owner pack attempts.
- The workspace verifier accepted the mapped child assembly of a healthy Aspire UI and reached the credential gate. All 147 browser Node checks passed. The controls exercised direct and launcher processes, a different origin, an in-place rewrite, same-path replacement, and a process restart at the same origin. Live browser verification reached the credential gate.
- The shipping integration build and 25 fixture checks passed. The cold authenticated REST profile passed all nine cases on fresh Dapr 1.19.2 and Redis using the exact EventStore 5.6.0 package producer checkout in a disposable workspace.
- FrontComposer passed 82 targeted SourceTools cases and 22 Stryker tooling cases. The latter exercise controlled preparation failures and retries.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind review 1, prelaunch bootstrap claims | high | The stock realm lacks the bound bootstrap permission. The walkthrough now specifies an approved issuer and canonical claims before the startup attempt. |
| Blind review 2, administrator hint | high | `CreateTenant` requires a verified administrator assertion. The walkthrough explicitly requires the hint and projected authority. |
| Blind review 3, status identity | high | Correlation fallback can resolve a different message. Polling now checks the returned message and, for retained attempts, correlation IDs. |
| Blind review 4, checkout identity | medium | Edited sources and a running DLL have separate identities. Evidence labels the Git metadata as the observed checkout. |
| Blind review 5, proxy attestation | maybe-false | The verification uses the live Aspire resource as the authoritative origin-to-launcher registration. A supported Aspire topology returning a stale proxy route while retaining the current registration would establish the remaining claim. |
| Blind review 6, nested artifact collision | high | A legal top-level projection can share the encoded nested stem. Allocate nested names against existing stems and qualify the colliding cases. |
| Blind review 7, inaccessible nested projection | medium | Public generated signatures cannot use a private nested type. Diagnose this input before emission. |
| Blind review 8, nested MCP URI collision | medium | The URI sanitizer can collapse distinct Unicode qualifiers. Preserve distinct CLR identities in the resulting URIs. |
| Blind review 9, runtime skill parser | medium | The MCP loader has its own existing fingerprint contract. Record the independent runtime collision for follow-up. |
| Blind review 10, additional REST control | low | The live bound stream and metadata observations already certify the granted and revoked subject. The handler retains the older projection deliberately to prove stale data cannot grant access. A second REST caller control would cover an additional route boundary. |
| Blind review 11, empty Stryker destination | medium | Standard directory rename can overwrite an empty directory. Use no-replace publication and a competing empty-directory control. |
| Edge review 1, nested hints | high | Roslyn rejects duplicate hints, including case variants. Include the complete nested identity in the generated hints and compile the distinguishing input. |
| Edge review 2, containing visibility | medium | Effective accessibility includes containing types. Address it before emitting public artifacts. |
| Edge review 3, sanitized qualifier | medium | Sanitization is not injective for valid source identifiers. Qualify unique generated protocol URIs. |
| Edge review 4, correlation fallback | high | The response can identify another command. Require exact handles before accepting status. |
| Edge review 5, expired credential instructions | low | The previous token-generation helper was removed. Both troubleshooting entries now direct the reader to the configured issuer. |
| Edge review 6, initial 7.0.0 pair | medium | The genuine archives have distinct source revisions and SDK floors. The documentation directs this pre-existing split pair to its original qualification without weakening the coordinated archive check. |
| Edge review 7, runtime corpus contract | medium | The separately implemented MCP loader retains its existing data encoding. Track the runtime follow-up independently. |
| Edge review 8, proxy destination | maybe-false | A stale endpoint with different registration or mapped identity is rejected. A stale proxy destination under an unchanged live registration would be a separate condition to demonstrate. |
| Verification review 1, compiled retained archive automation | medium | Three released pairs were compiled and discovered in this run. The current portable metadata suite cannot execute the historical package graph without its archive and runtime inputs. |
| Verification review 2, process replacement after capture | medium | The test now replaces an independently qualifying host at the same origin and rejects it against the original capture. The product Node hook runs the control. |
| Verification review 3, legacy helper isolation | medium | The recovery fixture now exercises legacy correlation-based polling without canonical scope headers and covers current hosted recovery separately. |
| Verification review 4, generated type collision | high | The nested regression now includes the encoded top-level name so an artifact-stem collision is visible at compilation. |

## Verification

- Passed 22 package consumer tests and four compiled public archive checks. The ordinary no-build owner pack checks succeeded for the current cohort and rejected all eight old-cohort controls.
- Three targeted synthetic artifact guard cases passed. The longer synthetic pack lifecycle run stopped before reporting a result. The real owner checks separately exercised those production preflight targets.
- Passed 22 quickstart shell checks, seven documentation checks, 147 Node checks, 25 integration fixture checks, and nine authenticated REST cases. Passed 82 FrontComposer SourceTools and 22 Stryker tooling cases.
- Live Aspire verified the running UI assembly before the credential gate. Upstream Stryker build execution was outside the deterministic tooling tests.
- The separate MCP skill-corpus loader fingerprint contract is tracked in deferred work.
