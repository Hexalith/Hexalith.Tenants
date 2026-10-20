# DW-439 validation evidence — 2026-10-20

Entry revision: `1f0f4edb6be91dd07028c0303a3f308841c3b414`, clean main.

## Scope

The current investigation identifies fifteen `Tenants.Audit.Receipt.*` French resource values; the historical ledger recorded examples rather than an exhaustive count. These values now use the missing accents and the established U+2019 apostrophe convention. Twelve of those values previously contained ASCII apostrophes. The other 37 receipt values are unchanged. The existing French partial-receipt expectation in `SupportSafetyBoundaryTests.D5UserShapesAreRedactedInMappedAndDirectAuditConsumers` follows the corrected shipped copy. Existing tests are reused for this copy correction; no runtime behavior or new testing framework is introduced.

The full English resource and other French keys are unchanged. All 52 receipt keys and all 1,717 resource keys retain exact EN/FR parity; every placeholder is preserved. DW-439 alone is selected for closure. Historical findings, unrelated ledger records, dependency pointers, archive/sprint and existing release holds remain preserved.

## Changed receipt keys

All keys below have prefix `Tenants.Audit.Receipt.`:

`Action.Escalate`, `Action.Retry`, `Action.Wait`, `Field.CommandReference`, `State.Degraded`, `State.Delayed`, `State.InvalidReference`, `State.MissingSupport`, `State.Partial`, `State.Pending`, `State.Ready`, `State.Stale`, `State.Unauthorized`, `State.Unavailable`, `Title`.

## Verification

From the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 6284 --no-ansi --no-progress --results-directory /tmp/dw-439/full --report-xunit-trx --report-xunit-trx-filename results.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-439-french-receipt-copy.md
git diff --check
```

Debug source build: exit 0, zero warnings/errors (`/tmp/dw-439-build.log`). The initial full UI run exits 2: **6,283 passed, one failed, zero skipped**. Its only failure is the unchanged `CorrectionStartPanelTests.Remounted_request_sent_panel_uses_advanced_tracker_before_refreshing_status`, which expects Accepted after refresh but observes RequestSent at line 1986. Full log: `/tmp/dw-439-tests.log`; report: `/tmp/dw-439/full/results.trx`. The focused correction-panel class recheck passes **118/118**, zero failures/skips; the final full UI rerun passes **6,284/6,284**, zero failures/skips. Both exit 0. No code changed between the initial run and either passing recheck; no causal attribution or original-production reproduction is claimed. Own-story gitlink validation and whitespace checks pass. This run does not change markup, CSS, focus or viewport behavior and makes no new browser or assistive-technology qualification claim.

The following resource-integrity check passes. It rejects duplicate or changed key sets, changes outside the fifteen receipt values, altered placeholders, and any remaining straight apostrophe within receipt copy:

```python
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

BASE = '1f0f4edb6be91dd07028c0303a3f308841c3b414'
ROOT = Path('src/Hexalith.Tenants.UI/Resources')
FRENCH = ROOT / 'TenantsResources.fr.resx'
def values(root):
    rows = root.findall('data')
    result = {row.attrib['name']: row.findtext('value', '') for row in rows}
    if len(result) != len(rows):
        raise RuntimeError('Duplicate keys')
    return result
old = values(ET.fromstring(subprocess.check_output(['git', 'show', f'{BASE}:{FRENCH}'])))
new = values(ET.parse(FRENCH).getroot())
english = values(ET.parse(ROOT / 'TenantsResources.resx').getroot())
if set(old) != set(new) or set(new) != set(english) or len(new) != 1717:
    raise RuntimeError('Resource key parity changed')
changed = [key for key in old if old[key] != new[key]]
if len(changed) != 15 or any(not key.startswith('Tenants.Audit.Receipt.') for key in changed):
    raise RuntimeError('Unexpected changed values')
receipts = [key for key in new if key.startswith('Tenants.Audit.Receipt.')]
if len(receipts) != 52 or any(chr(39) in new[key] for key in receipts):
    raise RuntimeError('Receipt count or apostrophe convention changed')
for key in new:
    if re.findall(r'\{[^}]*\}', old[key]) != re.findall(r'\{[^}]*\}', new[key]):
        raise RuntimeError(('Changed placeholders', key))
print('PASS: 15 French receipt values; 52 receipt / 1717 total keys; placeholders preserved')
```

Manual inspection of the diff confirms the correct French accents, apostrophes and unchanged meaning; the check above verifies structural integrity rather than French editorial quality.

## Runtime baseline

`aspire ps --format Json --non-interactive` returned no running AppHosts. `aspire start --no-build --isolated --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive` exited 0. `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive` recorded 23 resources, ten Running and thirteen Waiting, including the UI. `aspire wait tenants-ui --timeout 25 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` subsequently exited 0 and reported the UI healthy. `aspire otel logs --severity Error --limit 3 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive` still reported Memories `InitializeTenantRegistryActivity` errors. This is not evidence of an entirely healthy distributed topology. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` succeeded before rebuilding. No shared configuration or dependencies were changed.

## Commit validation

Pinned `@commitlint/cli@21.2.3`, invoked with `node_modules/.bin/commitlint --edit /tmp/dw-439/commit-message.txt --verbose`, exits 0 and reports `found 0 problems, 0 warnings`. The exact full message is:

```text
fix(ui): correct French audit receipt accents and apostrophes
```

The original successful output is retained in `/tmp/dw-439/commitlint.txt`. The requested Git publication targets main; no release workflow dispatch is part of this change.

## Independent review

The independent blind reviewer returned three documentation findings; all are triaged low and addressed without changing frozen intent or runtime scope:

- Clarify that fifteen is the current investigation count, rather than a count stated by the historical ledger. Recorded above and in implementation notes.
- Identify the fifteen changed resource keys independently of the diff. The exact key list is now recorded above.
- Distinguish structural resource parity and editorial inspection from real French rendering coverage. Existing `AuditEvidenceReceiptTests.French_resources_render_and_copy_the_complete_seven_field_summary`, `French_unavailable_receipt_renders_receipt_specific_recovery_copy_and_retries`, and the French cases of `SupportSafetyBoundaryTests.D5UserShapesAreRedactedInMappedAndDirectAuditConsumers` exercise shipped resource rendering and copy safety; most other receipt cases use neutral localizer doubles. Parity alone does not validate French accents. No product defect or deferred follow-up was identified by this review.

The reviewer rechecked all three clarifications and the actual French value diff, confirmed each addressed, and found no concrete unresolved issue. No follow-up was deferred.

## Final acceptance and ledger integrity

The failed initial run remains above. Passing rechecks used these commands:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*CorrectionStartPanelTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-439/remount-recheck --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 6284 --no-ansi --no-progress --results-directory /tmp/dw-439/full-recheck --report-xunit-trx --report-xunit-trx-filename results.trx
```

Logs are `/tmp/dw-439-remount-recheck.log` and `/tmp/dw-439-tests-recheck.log`. Final TRX inventories contain 118 and 6,284 passing result entries respectively, with no failed or skipped entries. The full suite includes all 82 receipt component cases, 293 support-safety cases, 91 composition cases and three localizer-double parity cases; each class passes in both full runs. Existing French render/copy coverage is described above rather than equated with all 82 receipt cases.

The executed `/tmp/dw-439/close-ledger.py` gates closure on the exact all-passing final TRX inventory, then verifies only DW-439 changes among all 656 ledger records and retains each replaced historical field under `pre-dw-439-*`. Final inventory is 395 done / 261 open. The index retains 236 rows covering those open IDs exactly once: DW-439 is removed from its grouped row while DW-270/DW-306/DW-324 remain open. Dependency pointers, deferred archive, sprint status and unrelated records have no differences from entry. Existing release qualification holds are unchanged.
