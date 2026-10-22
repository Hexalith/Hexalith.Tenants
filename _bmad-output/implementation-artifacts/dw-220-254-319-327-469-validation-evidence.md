# DW-220 / DW-254 / DW-319 / DW-327 / DW-469 validation

Date: 2026-10-22. Entry revision: `b85896a9664331e6eee282546f7e92178dac828e`.

## Change and boundaries

The four configuration-proof overloads on `ITenantQueryGateway` now require explicit implementations. Both production adapters already implement them. Four hand-written test gateways explicitly return unavailable instead of inheriting a silent default. Reflection pins all four abstract methods; the existing production proof tests remain intact.

All six identity-minting legacy command wrappers are obsolete on the interface and both concrete gateways, with guidance naming the required-identity tracked replacement. This implements DW-254's current migration/deprecation next-action while retaining its historical optional-identity decisions. Old signatures remain callable. Existing tracked capabilities, identity validation, payload, authorization and projection proof behavior are preserved. No extra optional-identity overloads are introduced.

Legacy configuration removal supplies safe English fallback text only when a non-accepted result lacks text. Existing safe messages, localized keys, rejection codes, ambiguity, message identity and correlation remain intact; tracked responses are unchanged. Tests cover invalid factory identity, ambiguous transport, accepted legacy lifecycle envelopes and exact caller-cancellation propagation. Obsolete-warning suppression is restricted to methods deliberately exercising compatibility.

Configuration trackers atomically return whether BeginDispatch reused a registration; both flows use the existing retry-aware audit mapper. Their rendered tests deliberately reuse dispatch-only registrations, without claiming a previously delivered configuration retry. Retained snapshots still prohibit configuration UI resubmission. First failures before sending leave audit NotStarted; reused registrations leave it AuditUnavailable. Forget and exact expiry reset Resumed to false with a new identity.

Lifecycle recovery passes explicit retry context. Review exposed an existing terminalization edge: a retry failing before dispatch set Failed and forgot the unresolved earlier delivery. That branch now remains RequestSent with the original identity, bounded ownership and localized ambiguity recovery. Enable and disable tests exercise first failure, real ambiguous delivery followed by retry failure, active ownership and actual parent disposal/remount. No notify-only or status-only confirmation is introduced.

## Baseline and build

The existing Aspire application was inspected before edits:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

All 23 resources were Running/Healthy. Raw configuration remains outside the repository at `/tmp/tenants-five-deferred-aspire-baseline.json`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial, reviewed and restored builds passed with zero warnings/errors. Final restored build: 17.08 seconds; `/tmp/tenants-five-deferred-build-restored.log`. Local validation uses Debug project references. No dependency update or package-mode policy change was needed.

## Test commands and behavior control

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --filter-method '*PreDispatchFailureKeepsAuditVisibleOnly*' --minimum-expected-tests 8 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-five-deferred/reviewed-retry

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --minimum-expected-tests 7187 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-five-deferred/full-final
```

The initial full suite passed 7,178/7,178 before review. The reviewed focused cases pass 8/8 with zero failures/skips. Final full suite: **7,187/7,187 pass**, zero failures/skips, 1m 51.030s. Exact results and hashes are recorded in [the companion run manifest](dw-220-254-319-327-469-run-manifest.json).

An isolated local behavior control temporarily restored the old configuration audit calls and disabled the new lifecycle retention branch/retry-aware mapping. Its build passed; the same eight rendered cases then failed exactly four reused/retried cases, while all four initial-failure controls passed. Lifecycle rejected Failed instead of retained RequestSent; configuration rejected NotStarted instead of AuditUnavailable. Original reviewed source bytes were restored in a finally block and rebuilt successfully before final acceptance. This control does not claim full historical source replay. Logs: `/tmp/tenants-five-deferred-replay-{build,tests}.log`; result: `/tmp/tenants-five-deferred-replay-result.json`.

## Independent review

The bmad-build Blind Hunter reviewed the diff with a six-finding floor. One functional finding and five verification/clarity findings were addressed:

1. Retain lifecycle identity/ownership when a retry fails before dispatch; qualify actual remount.
2. Name the configuration tests as registration resumption and document their boundary.
3. Cover enable as well as disable.
4. Pin fresh identity classification after Forget/expiry.
5. Pin callable legacy lifecycle acceptance envelopes.
6. Pin exact legacy removal cancellation propagation.

The first recheck correctly caught an incomplete test edit; the final independent recheck confirmed all six addressed and no remaining concrete findings. No review work was deferred. The spec contains the complete triage decisions.

## Ledger, format and commit checks

```bash
python3 _bmad-output/implementation-artifacts/dw-220-254-319-327-469-verify.py

dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --include tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TrackedGatewayContractTests.cs --verify-no-changes --verbosity quiet

node_modules/.bin/commitlint --edit /tmp/tenants-five-deferred-commit-message.txt --verbose

git -c core.safecrlf=false diff --check
```

The ledger verifier preserves all 657 IDs and all 652 unrelated record bodies; selected original fields are preserved under `pre-tracked-contract-`. Exactly five index rows close. Result: 495 done, 162 open across 152 rows, with every open ID indexed exactly once. Dependency pointers and historical preambles are unchanged. Duplicate-ID and immutable-field controls reject. The JSON result is committed alongside the reusable verifier.

Commitlint uses the lockfile-pinned local CLI 21.2.3 and accepted the exact message with zero problems/warnings. The formatting command passed with no changes; git diff whitespace checks also passed. Formatting checks the new C# contract test without broad formatting churn. No backend, archive, sprint, release workflow or release-readiness decision changes are included. These local UI results do not resolve unrelated hosted/package/runtime gates.
