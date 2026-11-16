# Complete local Happy: bounded Razor recovery proposal

The complete **local original e0** report is authenticated here by SHA256 `91144b286ea92cf254d0e4039384f99d2eeaaf84aac4fb1dcd7c0b41743abb71`. It reports1783Killed/549Survived/1Timeout/131NoCoverage. `complete-local-problems-and-nocoverage.json` captures all681 local problem IDs and every actual NoCoverage span, plus the localKilled12812 contrast row. The hosted console instead records1782/550/1/131 and rawSuccess12812; no local outcome is transferred to hosted evidence.

The preceding four-file25-case proposal remains byte-preserved. This separate one-file/**five-case** proposal is tested privately on unchanged b34 tracked source plus the owner's four reviewed test-file overlay. It changes no owner source, traits/filters, exclusions, thresholds, tool, provenance or accepted triage. The original Happy threshold failure remains unresolved.

## Concrete source-supported gaps

The original `Hfc1025DiagnosticTests` and `RazorModelTransformTests` classes carry `MutationErrorHandling`; Happy excludes that legacy category. Their isolated checks exist in the ordinary/Error suite. The new `MutationErrorRegression` cases combine adverse→valid source transitions and healthy-sibling preservation rather than changing those traits.

- **Timeline fallback:** a source-valid Timeline projection without a date emits the full HFC1022 warning at the projection declaration and uses declaration order. Adding only the date property clears the warning and emits actual `OrderByDescending(x => x.Timestamp)` ordering. Healthy projection output remains identical and repaired output compiles. This detects14921/14923.
- **Partial badge fallback:** one annotated member out of three emits one Info HFC1025 naming the property, coverage count and exact unannotated members, with declaration coordinates. The generated source retains text cases for unannotated members. Adding only the missing badges clears the diagnostic, retains complete badge metadata/arms, preserves healthy output and compiles. This detects14941/14943/14945/14946/14947/14948/14949.
- **Field-format recovery:** changing only a string field's type to bool, TimeOnly or collection preserves the documented transform format (`Yes/No`, `t`, `Count`) and the actual emitted `RenderSlotField(... format: ...)` metadata. TimeOnly additionally emits `ToString("t", CultureInfo.CurrentCulture)`. These are real template/slot consumers, not unused implementation counters. Healthy projection output remains identical and repaired code compiles. This detects15029/15037/15043.

`attempt-01` baseline passes **5/5**. All14 selected literal variants compile;12 are detected. The proposal uses real parser/generator source inputs, reuses a generator driver across the corrected input, preserves tree path/options and compares healthy output bytes. No numeric hashes or new performance requirements are asserted.

## Honest survivors and limits

- **14950:** changing raw `DiagnosticInfo.Severity` from Info to empty does not change the actual emitted HFC1025 severity. `FrontComposerGenerator.GetDescriptor` dispatches by ID to `DiagnosticDescriptors.BadgeSlotFallbackApplied`, whose canonical severity is Info. The direct raw transform-diagnostic API still returns changed data, so this is not a universal equivalence claim; no artificial generator failure was added.
- **14940:** `total-annotated` becomes `total+annotated` only as List capacity. The collected members, diagnostic and emitted code remain correct in this source-supported fixture. Allocation/performance/extreme external inputs are separate unknowns. No assertion of a private capacity was invented.
- **14143:** the documented public `ColumnModel.SupportsFilter` getter changes its default branch, but tracked production sources do not read that getter. Actual emitted filter behavior uses category-specific code independently. Existing direct getter assertions are legacy-category tests. No UI effect or product defect is claimed from this unused getter; a public-API ownership decision remains separate.
- The full131 local NoCoverage inventory includes25 hash-method rows and106 non-hash rows (parent's inventory). This packet exercises the stated Razor paths; it does not claim to close every template, policy/default, formatting or payload gap.

All literal statuses remain those of the immutable original **local** report. `literal-controls.json` records actual private class/source/TRX bindings and outcomes; these are not accepted owner records or an updated mutation score. The local/hosted12812 discrepancy is preserved explicitly.

## Reproduction

```text
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/package-proposal.py
python3 <this-directory>/final-audit.py
```

Each job uses fresh owned scratch/caches, read-only b34/Builds73e60af8 archives, MTP/Release, two logical processors and disabled compiler/build-server reuse. Exact commands, raw logs, captured literal source, TRX, source/report hashes and cleanup are retained. The one-file owner-style patch is `private-razor-recovery-proposal.patch`; style and read-only apply checks are recorded separately. Parent owns any integration, full current-source gate run and disposition.
