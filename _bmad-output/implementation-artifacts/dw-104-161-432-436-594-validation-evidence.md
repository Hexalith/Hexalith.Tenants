# Authenticated route qualification — acceptance pending

Baseline: `8804c10b8a900860ea2394c81eff78d23bb3c0a4`.
The user approved [the spec](spec-dw-104-161-432-436-594-authenticated-routes.md) on 2026-10-25.
Each numbered record remains open until its own actual acceptance checks pass. The five-item batch is incomplete while the conditional-read incompatibility remains unresolved.

## Implementation

- Remove the duplicate EventStore channel-token registration; the pinned platform helper already owns it.
- Assert unique per-service token resources and matching application/sidecar ownership.
- Include the SDK development-certificate trust directory alongside system OpenSSL trust, preserving certificate verification.
- Keep bounded TLS cause categories without copying arbitrary exception messages.
- Add live, command-seeded REST and authenticated UI/browser qualification, while preserving six anonymous denial cases.
- Add a focused blocking Release/package CI job with minimum counts and skip rejection; retain the existing broad advisory Aspire lane.

## Executed evidence

| Check | Result |
| --- | --- |
| Initial `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | CLI2/AppHost134: duplicate `eventstore-app-api-token` |
| Start after token repair | Required EventStore, Tenants, API, UI, sample and security resources report healthy; optional resources are separately unqualified |
| `dotnet dev-certs https --trust` with `SSL_CERT_DIR` including the SDK trust directory | Succeeded; independent HTTPS API `/alive` probe returned 200 |
| AppHost security graph, maintained MTP, Debug/package | Post-review 10 passed; zero failures/skips |
| TLS failure/redaction cases | Post-review 5 passed; zero failures/skips |
| Historical persisted-state regression | Local1/1 and hosted1/1 passed with zero skips; hosted Release/package job121191435285 in run40200821264 |
| Generated-controller HTTP contracts | 44/44 passed, zero skips |
| Anonymous UI route cases | 6/6 passed, zero skips |
| REST-client metadata/304 cases | 128/128 passed, zero skips |
| IntegrationTests build, Debug/package | Zero warnings/errors |
| `node --check` on the new live browser validator | Passed |
| `actionlint .github/workflows/ci.yml` | Passed |
| Genuine signed OIDC sign-in and protected pages | Production discovery/code exchange and token relay work; local HTTP5 data pages plus intentional restricted administrator prerender pass, and browser reaches all6 authorized data pages |
| Initial worker-enabled state-seeded attempts | Failed locally and on clean CI: completed member commands sometimes leave persisted aggregate sequence1 instead of2; no assertion was skipped |

Exact maintained build invocation:

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithMemoriesFromSource=false -m:1 -nr:false
```

Live commands use the same properties, `--no-build`, an exact class/method filter, `--minimum-expected-tests`, and `--fail-skips on`; `SSL_CERT_DIR` includes `$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs`.

The required source-mode command `dotnet build tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -nr:false` failed at pinned Memories `EventStoreMemoriesCommandStore.cs:22`, CS0246 for `IEventStoreGatewayBoundCommandProofSource`. The corresponding IntegrationTests source build and an explicit Memories-only selector fallback hit the same dependency incompatibility. No dependency was updated and no gate was weakened.

The early REST fixture also revealed an overbroad test assumption: index/audit reads do not promise a producer version. The six-route test retains real payload/freshness checks, while tenant detail supplies the separate recognized-304 proof. Existing absent-authority behavior and the independent DW-568 remain unchanged. Earlier failing attempts are diagnostic evidence only.

## Independent review corrections

All three layers completed. The spec retains one verdict/evidence row for each of 13 findings. Patches cover actual tenant-list widths/forced colors and keyboard access, real authorized seeded content, source-specific launcher focus and complete Back context, atomic private cookie-file creation, persisted metadata and cursorless recovery comparisons, complete evidence stability checks, circuit failures/interaction, and nested TLS redaction. Existing maintained missing-claim tests remain authoritative; no new cookie-versus-bearer policy was invented.

The optional pinned Memories host also fails package compilation: `dotnet build references/Hexalith.Memories/src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj -c Debug -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -m:1 -nr:false` exits1, CS0234 at `ProvisionRediSearchActivity.cs:44` for `TenantIngestionOwnership`. The focused fixtures now use an explicit `EnableMemories=false` AppHost opt-out. Default startup keeps Memories; graph tests cover both modes. No Dapr identity/policy or dependency pin changes. The focused CI job collects independent selected-suite failures after a successful common build, retaining each as blocking.

A full AppHost invocation initially set an erroneous minimum of12: all11 tests passed, but MTP correctly rejected that count with exit9. The corrected minimum is11; this does not change any repository gate.

## Continued qualification and material limits

Candidate `3b60914ace5201a711fb51c1036d3679e0512ec8` was pushed to a temporary draft validation PR55. Another session merged that exact candidate into main while checks ran; no parent history was rewritten. [Hosted candidate run40200821264](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40200821264) and [main run40201107518](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40201107518) are failure records, not complete acceptance.

Candidate hosted results: unique-token model10/10, exact historical persisted-authority1/1 and anonymous6/6 passed. REST2/3 passed; conditional detail returned200 instead of304. UI1/3 passed; a sequence stall and a forged-cookie fixture that failed to populate the real framework token store prevented positive browser proof. The corrected fixture now signs in through the real signed OIDC exchange; only the ambiguous-scope negative uses a protected test ticket.

Clean CI reproduced the sequence stall; competing local hosts are therefore not a sufficient explanation. The focused qualification now sets the existing `EventStore__Projections__WorkersEnabled=false` setting to exercise real immediate request-path delivery independently of background workers. Persisted sequences then progressed through all commands in successful local attempts. This is a test profile, not a claimed worker-concurrency repair. Normal application configuration retains workers. Later local runs also observed internal workload signature mismatches and domain500s consistent with competing Dapr hosts; those failed runs remain failures. No unrelated host was stopped and no Dapr identity/policy was changed.

The local production UI check exposed a canonical user-lookup self-redirect during prerender. The panel now renders when already canonical; focused component41/41 and full UI7,705/7,705 pass with zero failures/skips, including the new regression. Full UI used Debug/package `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --minimum-expected-tests 7000 --fail-skips on`. The administrator page intentionally withholds data during HTTP prerender under its existing circuit-only principal policy. HTTP coverage asserts the restriction; real browser coverage requires ready administrator rows and no restricted/unavailable state. No authorization policy was weakened.

TLS validation stays enabled in .NET and Chromium. The public SDK development certificate is installed in Chromium's NSS trust store. An independent local HTTPS server using that same certificate returned200 in Chromium. The browser's active store is `~/.local/share/pki/nssdb`; CI initializes both the legacy `~/.pki/nssdb` and current store before SDK trust/import, accommodating browser store selection. No private certificate or authentication material is retained in evidence.

DW-161 is blocked by pinned upstream behavior. Gateway3.127 `QueriesController.ShouldEvaluateConditionalRequest`/`ETagMatches` require the internal `SelfRoutingETag` format, but the Tenants detail query emits its opaque state-store ETag. The emitted strong validator therefore receives200, not304. The pinned gateway client's304 path also omits ProjectionVersion and ServedAt. No public codec/configuration seam fixes this here. The approved spec forbids dependency updates; hand-encoding the platform's private format would duplicate platform plumbing. The conditional case remains blocking and unskipped. Ordinary six-read proof now runs separately so that this failure cannot suppress its last four routes. Index/audit producer-version policy stays in the independent open DW-568.

Broader validation is also incomplete. The local Contracts suite produced698 total/656pass/42fail/0skip, including existing release-recovery/publication-script validator incompatibilities; these are not claimed as passing or independently reproduced at the baseline. The one stale workflow-action policy assertion was updated to allow only the already approved direct actions at full immutable40-hex SHAs, and its focused test passes1/1. The main reusable build gate failed package-consumer validation: `Hexalith.Tenants.Client.0.0.0-ci-test.nupkg`, dependency range expected `[5.10.1,)`, found `[0.0.0-ci-test,)`. Package/release files and dependency pins were not changed by this task.

The second independent review completed all three layers and records every additional finding in the spec. Corrections cover publish-mode token preservation, paged membership seeds, exact roles/audit events, real incompatible protected cursors, measured scrolling/hit visibility, ready contextual audit evidence, capture scope, blocking TLS diagnostics and early safe failure artifacts.

## Final focused local checks

- AppHost project:13/13 passed, zero skips, including run/publish token ownership.
- TLS diagnostics:5/5 passed, zero skips.
- Private early-failure/timeout artifacts:3/3 passed, zero skips; the exact class is also blocking in CI.
- Strengthened real REST class:4 total,3 passed/1 failed/0 skipped. All six reads compare actual data, distinct persisted roles and seeded audit events; real protected-cursor recovery and anonymous denial pass. Only `ConditionalDetailReadRecognizesPersistedAuthority` fails, expected304/actual200. Exact command: `EventStore__Projections__WorkersEnabled=false SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs" dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build --filter-class Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsRouteTests --minimum-expected-tests 4 --fail-skips on`.
- Browser progression: all6 data pages, real filter change/reset, and phone audit layout pass. Physical scrolling exposed the native Fluent5 table sizing defect; corrected table sizing and130/100/110px phone columns retain all3 pinned safety cells at measured390px while scrollLeft moves0→750. The pinned identity cell now hosts the same authority-bound audit component on phones, with a shared RenderFragment retaining context and distinct selectors. Desktop keeps its original action slot. Focused208 affected cases pass after correcting the existing CSS-exception rationale placement (207 initial passes plus the single failing conformance case rechecked1/1). Physical action bounds and the complete matrix remain pending.
- Browser binding is explicitly limited to on-disk UI/Shell binaries and Tenants/FrontComposer sources. It does not claim to bind running backend binaries or other dependency source contents. Independent integration results qualify backend behavior.

## Final reviewed local result

The complete authenticated UI class passes3/3 with0failures/0skips. Its real browser validator passes all25 cases: six protected data pages; server filter change/reset; six audit and six list viewport/forced-colors combinations; detail/member Back; three responsive list Back controls; external return rejection. Copy and audit controls have actual containment, pointer, Tab/focus-outline checks. [Retained safe JSON](dw-104-161-432-436-594-live-browser-local.json) includes source/binary/validator captures, measured dimensions/scroll offsets and25 successful case records. No cookies, access tokens or signed URLs are retained. Its source diff digest was checked against the final source before indexing and matched.

Producer repair certifies only a complete canonical tenant history and the actual retried merged audit evidence. Unknown/malformed/skipped/gapped/conflicting/unmatched legacy evidence receives no producer authority. The status converter's read fallback is explicitly distinguished from canonical producer status strings; legitimate rejection enums are preserved. The original isolated malformed-status probe now returns null instead of tenant-sequence:2. Focused producer64/64 and downstream22/22 pass without skips. Full server1505 ran1504pass/1fail/0skip: unchanged `DeploymentReadinessDocumentationTests.Deployment_readiness_guide_does_not_claim_EventStore_validator_support_for_Tenants_evidence` reads the missing pinned `references/Hexalith.EventStore/scripts/validate-operational-evidence.py`. No unrelated documentation/dependency gate was weakened.

Final full UI7706/7706 passes after the visible-launcher JS correction, zero skips. The previous6 component failures were old one-launcher-per-row assumptions; updated assertions verify both copies, exact row ownership, unique safe focus anchors, disabled unsupported identities and unique reason targets. The focused208 component/conformance cases and23 identity cases pass. Chromium focus controls pass phone/desktop/forced-colors and reject a deliberately broken first-candidate implementation.

Final live REST4 has3pass/1fail/0skip. Ordinary six-read proof now also requires the actual audit producer version to match the persisted command prefix. Distinct roles, exact audit events, protected-cursor scope rejection/recovery and anonymous denial pass. Only conditional detail fails with expected304/actual200. The platform ownership/dependency-freeze blocker remains as described above; DW-568's separate index contract and record remain open and unchanged.

Exact final commands use the build invocation above, then:

```bash
EventStore__Projections__WorkersEnabled=false SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs" dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build --filter-class Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsUiRouteTests --minimum-expected-tests 3 --fail-skips on
EventStore__Projections__WorkersEnabled=false SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs" dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Debug --no-build --filter-class Hexalith.Tenants.IntegrationTests.AuthenticatedTenantsRouteTests --minimum-expected-tests 4 --fail-skips on
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --minimum-expected-tests 7000 --fail-skips on
dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --minimum-expected-tests 1495 --fail-skips on
```

## Outstanding acceptance

Run corrected hosted Release/package checks and conserve ledger/index closure for individually qualified records. DW-161 remains open until the actual conditional authority contract passes. This checkpoint does not claim the completed five-item batch, a green broader release pipeline, healthy optional Memories/search topology or repaired projection background workers.
