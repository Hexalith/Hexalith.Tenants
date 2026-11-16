# Bounded hash/equality follow-through — no owner decision

This is read-only owner analysis and private finite diagnostics on tracked base `b34be9245443d21b72a6076cf205bbbc8a8c521f`, plus the four byte-bound reviewed working-input test files. The original Error inventory remains e0-specific. Parent-supplied partial Happy counts are context only; no final score, threshold result, accepted triage or current-source mutation qualification is claimed.

## Existing contract and consumer findings

`existing-assertions.json` records all 28 original assertion locations, actual contexts/methods and source hashes. Twenty-five concern SourceTools models; three concern unrelated Shell/Contracts types. Six source locations assert different hashes for finite selected unequal values; 22 assert equal-value/hash congruence (including the three unrelated types). The template-marker helper repeats one of those six assertions for eleven independently changed fields. Those are finite examples, not a universal demand that all unequal objects have different hashes.

- `Incremental/DomainModelCacheEqualityTests.cs:70` explicitly requires DisplayFormat/window metadata in equality and hashing; its changed-window test currently asserts equality only. The existing finite WhenState test also asserts different hashes. This supports a **candidate** finite window-participation regression, subject to owner review.
- `CommandDensityTests.cs:87` promises density/icon hash participation but its hash pair actually varies only IconName. `CommandParserTests.cs:660/679` checks type/policy pairs, `RegistrationModelTransformTests.cs:82` checks route membership, and `ProjectionTemplateMarkerTests.cs:20` checks eleven marker fields. They do not establish an arbitrary numeric hash algorithm or a collision-free model domain.
- The other SourceTools assertions check equal independent copies, repaired parse models, or equal transform results. The new reviewed DiagnosticInfo test adds another equal-copy hash assertion; it still does not establish unequal-object hash uniqueness.
- Current tracked architecture requires pure equatable IR, and current epics preserve that as LEGACY-NFR-3. The source's ADR-006/009 references explain value caching but are not an additional collision budget. No tracked numeric hash value, full-domain injectivity, minimum distinct-hash ratio, or generic HashSet probe budget was found.
- Existing generator tests/docs use the legacy NFR8 name for a warm incremental median below500ms. `IncrementalRebuildBenchmarkTests` uses one warmup and five sampled deltas plus emitted-output/Modified checks. Drift has a separate25-declaration/20-sample budget. The current `quality.yml` Performance lane is explicitly advisory (`continue-on-error: true`); documentation that says CI blocks performance does not turn a synthetic collection budget into a new gate.

The production call scan retained in the prior packet finds model hashes called only by nested hash methods. `FrontComposerGenerator` gives Roslyn the three parse-result provider types and their collected arrays. Razor/Fluxor/form/renderer/registration DTOs are constructed **inside output callbacks**, rather than supplied to their own incremental providers (`FrontComposerGenerator.cs:176` and`:264`). The eight transform hash methods account for219 of the418 mapped Error hash IDs; the source does not show a current production hash-based collection of those DTOs. Public visibility in the analyzer assembly permits external use but does not itself establish a supported collision-performance guarantee or a frozen Contracts API. External clients and unexercised Roslyn configurations remain unknown.

## Actual finite results

`attempt-02` uses uninstrumented existing hash methods. The private counter class is additive/unused in those phases; the actual hash method bytes remain original or the named exact literal mutation. Each array control uses128 distinct two-element public `EquatableArray` values and records equality probes in a legal external `HashSet`; it asserts collection correctness and equal-copy hashes, **no new collision-quality limit**.

| Source variant | Distinct hashes | Insertion element-equality probes | Lookup probes | Collection/cache/output checks | Existing500ms assertion |
| --- | ---: | ---: | ---: | --- | --- |
| Original | 128 | 0 | 256 | Pass | Pass |
| 13541: array loop skips all elements | 1 | 8128 | 8384 | Pass | Pass |
| 13545: recurrence addition becomes subtraction | 128 | 0 | 256 | Pass | Pass |
| 13546: recurrence multiplication becomes division | 5 | 1866 | 2122 | Pass | Pass |

The constant/division variants have demonstrated **bounded external collision cost** while preserving collection results and equal-value congruence. This workload is not an identified tracked production collection, so the numbers do not establish a product budget violation. Lookup counts include both element comparisons for each successful two-element lookup. HashSet bucket traversal without element comparison is not measured, so equal probe counts are not a universal timing-equivalence proof.

For13545 and fixed length n, unchecked arithmetic gives `Hminus = 2 * 17 * 31^n - Hplus` modulo2^32. That bijection preserves full-hash collision equivalence for equal-length arrays. It does not preserve numeric output, promise cross-length collision equivalence, or prove external bucket/performance behavior. No test should reject it simply because its integers differ.

The one proposed finite participation test (`RelativeTimeHashParticipationTests.cs`) checks actual parsed/renderer seven- and fourteen-day metadata, unequal model values, and different hashes for that specific pair. Original passes; literal13419 (drop `RelativeTimeWindowDays` contribution) compiles and fails the final hash-participation assertion. The actual metadata still differs correctly under the literal variant. Thus this is evidence for the **existing finite participation expectation**, not a stale-rendering defect or a general hash-law violation. Only this candidate test is exported in `candidate-relative-time-hash-participation.patch`; the observation harness is not proposed as owner product code.

## Real generic cache observation and limits

`attempt-04` instruments all19 model hash methods and typed `EquatableArray.Equals` **only in private source**, with their exact mapping, changed source bytes and hashes retained. A direct independent-model positive control produces nonzero hash and equality counters; counters reset before actual generation. The real Roslyn5.12 driver then performs initial generation, identical-input reuse, a same-path trivia-only edit with equal IR, and a WhenState edit. Existing cache/output assertions pass. Original and13541 each record:

- Eight typed array equality calls across the real generator scenario.
- Zero entries into all19 model hash methods.
- Identical initial/changed emitted-output SHA pairs between original and literal13541, while the actual WhenState edit changes the output.

This proves that this real bounded cache path uses value comparison without calling those hashes. It does not prove all possible providers/platform versions/configurations or external clients never call them. `attempt-03` retains the earlier smaller zero-count observation without the positive control/trivia edit; final conclusions use attempt04. No counter/timing assertion was added to production source or accepted as a gate.

## Proposed follow-through

1. Review the single finite window-participation candidate against the existing written test contract. If adopted, run it and the unchanged complete gate on the actual new source; original e0 reports are not reusable qualification.
2. Retain13541/13546 as externally observable collision-quality risks with concrete finite evidence. Any further quality test should name an actual supported workload/budget or a consciously accepted finite participation expectation. Do not invent a new threshold to kill them.
3. Retain the fixed-length arithmetic proof for13545 and conditional congruence for other pure arithmetic. Do not demand exact numeric hash values or force every arithmetic survivor into a defect category.
4. Do not blanket-classify all418 as equivalent or deferred. Current generator behavior, finite existing participation requirements, external collision risks and unresolved public consumers are distinct evidence. Parent/owner must make any disposition; no owner, approval, follow-up acceptance, exclusion, filter, threshold, tool or provenance change is authored here.

## Preserved attempts and reproduction

All jobs use fresh `/dev/shm` `run-with-temp` workspaces, exact read-only archives of b34 and frozen Builds73e60af8, private caches, MTP/Release, one MSBuild node, two logical processors and disabled compiler/server reuse. Exact commands, source bindings, resource observations, full raw output and TRX are separate files:

```text
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-controls-retry.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-instrumented-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-instrumented-positive-controls.py
python3 <this-directory>/final-audit.py
```

Attempt01 retains the private harness CS8122 build refusal (pattern syntax inside a Shouldly expression tree), before tests. Attempt02 retains the complete successful uninstrumented controls, expected13419 test failure, and later private instrumentation CS0103 refusal in three namespaces. Attempt03 corrects namespace qualification and retains its limited observations. Attempt04 adds positive controls/trivia comparison and completes both instrumented phases. No warning suppression or source-wide mutation rerun was used to repair these private harness failures. Summary/manifests verify original raw/source bindings, owner preservation and owned cleanup. All publication/gate decisions remain parent-owned.
