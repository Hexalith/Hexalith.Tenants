# Five deferred module manifest inventory records

Qualified on 2026-10-22 from Tenants
`205cbc5019343d344643b530fb03aee82af2840d`. Exactly DW-629, DW-630, DW-631,
DW-635 and DW-636 close. The original fields remain in each ledger record;
superseded fields carry the `pre-manifest-reconciliation-` prefix.

This is static inventory/schema qualification. It does not qualify a running
topology, an authorization revocation guarantee, executable recovery, production
replay, Platform enrollment, or release readiness. No application, AppHost,
dependency pointer, authentication implementation or stored data changed.

## Findings and resulting declaration

| Record | Evidence and resolution |
| --- | --- |
| DW-629 | The v2 schema's `identifier` is kebab-case. `topic.name` uses that type; dotted physical names are supported by `topic.subscription.topic`. Platform's `PlatformDaprBindings` skips topics without `subscription` before generating Dapr subscriptions. The unchanged `tenants-events` inventory has none, so it creates no physical topic mapping. A static assertion checks the actual AppHost publisher override. The existing AppHost and event contract use `tenants.events`; the new inventory guide explains the distinction and the physical-name override required for any future subscriber. |
| DW-630 | Current exact metadata comparison finds 13 tenant operations (nine commands/four queries), three global-administrator operations and one user-membership query. Nine previously omitted tenant operations were added in `b185ae80de474bedc6c60af38bacc5550d4e8cd7`. Public interfaces are complete for current REST-decorated contracts; internal bootstrap and delegated queries without REST decoration are excluded deliberately. |
| DW-631 | The administrator critical flow now lists existing list/set/remove operations. The documented scope is the complete administrator-management set. Other critical flows remain selected scenarios, while public interfaces hold the exhaustive decorated-operation inventory. This adds no operation or executable check. |
| DW-635 | The administrator surface and its three operations use the descriptive label `authenticated-current-global-administrator`. Existing query code checks persisted singleton membership after fixed scope validation, and commands check current aggregate membership. The guide distinguishes the authority sources and their limits. The schema accepts descriptive text; no new ASP.NET policy or token permission is registered. |
| DW-636 | The supported `recoveryItem` shape records rebuild-only singleton authority, global audit and distinct system-tenant audit read models, owned by tenants and dependent on eventstore history. No `restoreHook`, executable task or recovery hook is invented. The guide links the existing audit rollout runbook, independent histories, backups, immutable legacy evidence, serialization and complete persisted end-state requirements. Replay remains separately authorized. |

The operational explanation is in
[module-manifest-inventory.md](../../docs/module-manifest-inventory.md).
DW-562/DW-617 authorization lag and DW-627 distributed replay serialization remain
open. Existing production/runtime and release holds are unchanged. The new guide
does not replace the [audit rollout runbook](../../docs/audit-stream-rollout.md).

## Source identities and boundaries

- Tenants baseline: `205cbc5019343d344643b530fb03aee82af2840d`.
- Root-pinned Builds: `c0cbbbd4366d512cc1731397f14dd53a7a01580d`.
  `schemas/hexalith.module-manifest.v2.json` SHA-256:
  `888d719b58493b4ebe14975e96262eca87e00afee8fe3a451a83ac7da366293e`.
- Platform consumer inspected at
  `c65d87a6f8f4a05fa3bea4e24ac778bf23f12cd6`:
  `src/Hexalith.Platform.Composition/PlatformDaprBindings.cs`, subscription loop
  and `PhysicalTopic` resolution. This is source semantics evidence, not a
  claim of a deployed Platform revision or successful composition.
- Current owned sources:
  `src/Hexalith.Tenants.Contracts/{Commands,Queries}`,
  `src/Hexalith.Tenants/Queries/Handlers/GetGlobalAdministratorsQueryHandler.cs`,
  `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`,
  `TenantQueryHandlerBase.GlobalAdminProjectionKey`, and `TenantAuditStreams`.

The initial root was clean at `82c2e9ad51f4d9ceeae9dcde33d7e0273524a105`.
The remote had advanced by one commit, so main was fast-forwarded to the baseline
above. Its existing Builds gitlink then differed from the original checkout.
The first attempted exact-schema lookup failed because that object was absent:

```text
git -C references/Hexalith.Builds show c0cbbbd4366d512cc1731397f14dd53a7a01580d:schemas/hexalith.module-manifest.v2.json
fatal: path 'schemas/hexalith.module-manifest.v2.json' exists on disk, but not in 'c0cbbbd4366d512cc1731397f14dd53a7a01580d'
```

After inspecting the clean submodule, fetched that exact already-pinned commit.
Its schema matched the original checkout byte-for-byte. A nonrecursive
`git submodule update -- references/Hexalith.Builds` aligned only that clean root
dependency checkout with main's existing pointer. No nested dependency was
initialized or updated and no dependency pointer is included in this change.

## Reproduce and verify

Run from the repository root with Python 3 and the existing `jsonschema` 4.26.0:

```bash
PYTHONDONTWRITEBYTECODE=1 python3 _bmad-output/implementation-artifacts/dw-629-630-631-635-636-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-629-630-631-635-636-manifest-inventory.md
git diff --check
```

All commands exit 0. The checker uses the exact Builds schema Git object above,
not the state of a moving checkout, and reads the exact Platform consumer object.
The two consumer assertions are source-presence checks, not executable behavior
proof. Manual inspection of the complete pinned subscription loop confirms the
early continue precedes subscription generation and `PhysicalTopic` returns the
explicit override or inventory fallback. No composer execution is claimed.
It recursively scans current contract sources (excluding build output), fails
explicitly on unsupported `RestRoute` syntax/scopes and checks route-prefix
boundaries. It compares `CommandType`/`QueryType` metadata against every public
interface. The manifest's semantic
`list-global-administrators` name is explicitly mapped from the contract's
`get-global-administrators` type.

The checker rejects all eight in-memory harmful manifest controls:

1. Dotted logical inventory name.
2. Missing public operation.
3. Weakened administrator operation policy.
4. Omitted removal in the administrator critical flow.
5. Absent administrator critical flow.
6. Invented executable restore hook.
7. Added deployment subscription.
8. Changed unrelated tenant-management flow.

Two additional parser controls reject unsupported attribute syntax and a
lookalike absolute route outside the declared prefix boundary.

It proves that reversing only the intended authorization/critical-flow/recovery
fields reconstructs the entire baseline manifest. Exactly the selected five
ledger record bodies change, all original fields remain, and every unrelated
maintenance row retains its exact text/order. Open IDs are covered exactly once.

```text
657 unique IDs: 470 done / 187 open
173 maintenance rows
done=470, actionable=108, blocked=15, awaiting-decision=23,
accepted-limitation=9, uncertain=32
Story gitlink guard: RESULT: PASS; no references/ pointer changes
```

The fixed-snapshot checker intentionally fails after later unrelated ledger
changes. Application builds, Aspire startup and browser tests do not validate
this static declaration/documentation change and were not run. No native
Platform CLI or full enrollment acceptance is claimed by schema validation.

## Independent review

The one-shot Blind Hunter reviewed 56.573 kB of changed content with finding
floor `min(floor(sqrt(56.573) + 1), 10) = 8`. All eight findings were accepted and
patched; none was deferred. Their individual verdicts and evidence are retained
in the owning spec. Corrections preserve unrelated critical flows exactly,
scan recursive contract sources and reject unsupported route declarations,
validate route boundaries and the actual publisher override, qualify source-only
consumer checks, expose the authority storage key, link from the audit rollout
runbook and retain the full validated commit candidate/output below.

Targeted independent recheck accepts all eight corrections with no remaining
issues. It independently rejects removal, renaming and operation changes in the
unrelated tenant flow, verifies the durable candidate digest, and passes the
focused verifier (eight manifest/two parser controls), story gitlink guard and
whitespace check. No new deferred record is needed. Final local documentation
links also pass. Fetch confirms no remote divergence before publication.

## Commit validation

The repository-installed `@commitlint/cli` reports 21.2.3, matching the lockfile.
The exact full candidate was saved to
`/tmp/dw-629-630-631-635-636-commit-message.txt` and validated with:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-629-630-631-635-636-commit-message.txt --verbose
```

Result: exit 0, `found 0 problems, 0 warnings`. The complete output is retained at
`/tmp/dw-629-630-631-635-636-commitlint.log`. Candidate SHA-256:
`91f6171f12a44ee1d351902f3296b9bc0dbb302286d436528febd2f6fc7084fc`.
Its subject is `docs(module): qualify five deferred manifest inventory records`.

Full validated candidate (exact UTF-8/LF bytes between the fence lines):

```text
docs(module): qualify five deferred manifest inventory records

Describe logical topics and complete public REST operations, include
administrator removal in its critical flow, and record existing membership
authorization and rebuild-only recovery prerequisites.

Retain focused schema, contract-inventory and ledger conservation evidence.
Close DW-629, DW-630, DW-631, DW-635 and DW-636 without changing runtime
policies, executable recovery hooks or release acceptance.
```

Complete successful validator output:

```text
⧗   --- input ---
docs(module): qualify five deferred manifest inventory records

Describe logical topics and complete public REST operations, include
administrator removal in its critical flow, and record existing membership
authorization and rebuild-only recovery prerequisites.

Retain focused schema, contract-inventory and ledger conservation evidence.
Close DW-629, DW-630, DW-631, DW-635 and DW-636 without changing runtime
policies, executable recovery hooks or release acceptance.
✔   found 0 problems, 0 warnings
```
