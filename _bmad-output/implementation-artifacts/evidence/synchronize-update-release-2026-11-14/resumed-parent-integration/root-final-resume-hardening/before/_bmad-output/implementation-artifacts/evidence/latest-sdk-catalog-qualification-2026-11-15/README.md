# Latest SDK catalog qualification — 2026-11-15

Public stable EventStore **5.1.0** passed the current local package qualification.
Existing selectors and guards required no source, configuration or test changes.

- Both actual Release test-project builds: **0 warnings, 0 errors**.
- **671 .NET cases and 18 Python controls** passed, with **0 failures/skips**.
- Fresh packed Contracts/Server consumer: **24 routes, 24 operations, 13 adapters**;
  candidate archive and loaded DLL hashes agree.
- Ten restored current SDK archives select **5.1.0**, all at embedded producer
  **49084f66f8d6545cae442875355a7066d5f5e73d**, and match public NuGet bytes.
  The frozen Testing.Integration **3.128.4** harness is recorded separately.
- All nine original submodule indices, checkout HEADs and statuses are identical
  before/after. The five pre-existing moved gitlinks remain external inputs.

The receipts and summary linked below describe the **original 2026-11-15
qualification snapshot**. They are preserved unchanged by replays. The original
run did not retain a `dotnet --info`/`--version` measurement; its SDK statement
records the repository policy and the contemporaneous observation, not a newly
measured replay toolchain.

Replay from the Tenants superproject:

```bash
run-with-temp python3 _bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/qualify.py
```

The driver runs jobs sequentially in one private disposable workspace, with
private package/HTTP caches and public-only SDK restore. Each replay writes to a
unique `replays/<UTC-timestamp>-<random>/` directory and prints that location.
It never replaces original receipts or another invocation's output. Each replay's
`qualification-result.json` records its own outcome, including early failures;
an interrupted run remains explicitly incomplete. Its `commands.json` contains
the exact expanded commands, UTC times, exit codes and log names. The original
manually assembled `qualification-summary.json` is never regenerated or reused
as a replay result.

Replay `toolchain.json`, `dotnet-info.log` and the copied `global.json` retain the
actual selected SDKs, installed toolchain details and inherited SDK/runner policy.
The same repository policy is copied into the shared scratch parent so both
repository projects and the separate package-consumer project inherit it.
Uninitialized optional submodules are explicitly recorded without a HEAD/status
from the ambient parent repository. The driver does not initialize
submodules, modify their sources or perform any Git mutation.

Read [qualification-summary.json](qualification-summary.json) for counts,
[public-archive-provenance.json](public-archive-provenance.json) for package
identities/sources/hashes, [restored-graphs.json](restored-graphs.json) for evaluated
graph selections, and [catalog-package-consumer.json](catalog-package-consumer.json)
for actual candidate hashes and compiled discovery. Source receipts bind the
working-tree base and the Builds/Platform inputs; the source hash receipt records
each copied repository's full tracked-file manifest digest and individual build
input hashes.

The additional historical story audit returns **1** in
[story-gitlinks.log](story-gitlinks.log): its original 9e5a991 baseline finds nine
historical pointer differences, eight undeclared by that historical File List.
It does not read `resume_baseline_commit`. This pre-existing audit mismatch
remains unresolved; the resume's spec requires preserving historical claims and
all existing gitlinks. The successful current qualification is recorded
separately from this failed historical audit.

This evidence qualifies local working-tree inputs at base
`2ec938397c5b330cf1e694e8cc442cb4ad022f2b`. It does not establish exact-source
remote CI, publication, native distributed execution or live Production trust.
No branches, staging, commits, pushes or remote workflow dispatches were made.

The replay-driver safeguards have a separate focused check:

```bash
run-with-temp python3 -B _bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/test_qualify.py
```

Its four tests use synthetic fixtures and mocked qualification/toolchain calls;
they do not rerun SDK builds, public restores or the original qualification.
Their result is retained in `driver-safeguards.log`.
