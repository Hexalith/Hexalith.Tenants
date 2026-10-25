# Authenticated route qualification — acceptance pending

Baseline: `8804c10b8a900860ea2394c81eff78d23bb3c0a4`.
The user approved [the spec](spec-dw-104-161-432-436-594-authenticated-routes.md) on 2026-10-25.
All five numbered records remain open until their actual acceptance checks pass.

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
| Historical persisted-state regression | Local 1/1 passed, zero skips; hosted proof remains pending |
| Generated-controller HTTP contracts | 44/44 passed, zero skips |
| Anonymous UI route cases | 6/6 passed, zero skips |
| REST-client metadata/304 cases | 128/128 passed, zero skips |
| IntegrationTests build, Debug/package | Zero warnings/errors |
| `node --check` on the new live browser validator | Passed |
| `actionlint .github/workflows/ci.yml` | Passed |
| Live positive-cookie `/tenants` assertion | Passed before command seeding; production scope boundary recognizes the fixture ticket |
| State-seeded REST/UI attempts | Failed: completed member commands did not produce the expected persisted aggregate sequence within 60 seconds; several local AppHosts share Dapr identities/state |

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

## Outstanding acceptance

Run the focused live REST, authenticated UI/browser, anonymous routes and exact persisted-state regression successfully, including the required hosted Release/package result for DW-594. Retain source/binary-bound safe browser results; complete independent review and ledger/index conservation before any closure. No healthy full topology, successful browser qualification or completed five-item batch is claimed by this checkpoint.
