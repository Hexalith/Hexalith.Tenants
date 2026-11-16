# Private non-hash contract/recovery proposal

This packet contains four prospective test files / **25 cases**, on exact tracked base `b34be9245443d21b72a6076cf205bbbc8a8c521f` plus the owner's four previously reviewed working-input files. No owner file was changed. The proposal preserves current admission, diagnostics and fallback behavior; all classes retain the existing `MutationErrorRegression` trait. No filters, target roots, exclusions, thresholds, tool, provenance or accepted triage were changed.

The parent reports the completed original e0 hosted Happy result as72.36%,1782Killed/550Survived/1Timeout plus131NoCoverage, exit2 below80. That is a real unresolved gate failure. This private packet does not supply a new mutation score, predict a passing score, classify all survivors as defects, or qualify current/future source. Original Error/Happy observations remain attributed to e0. The exact full Happy JSON was not yet available to this investigation.

## Proposed observable corrections

| File | Cases | Contract exercised |
| --- | ---: | --- |
| `CommandContractBoundaryTests.cs` | 11 | Abstract/generic diagnostic reason and location; missing policy/icon argument recovery; compiler-approved sbyte/byte/ushort/char→int defaults; malformed DefaultValue; nullable null default; wrong→correct global target key. |
| `TemplateContractBoundaryTests.cs` | 8 | Exact/build/minor/major version behavior, full packed-version reasons/locations, invalid role, missing Parameter, missing marker arguments, and abstract-template refusal/recovery. |
| `AliasSignatureContractTests.cs` | 4 | Alias-only and co-aliased references, escaped keywords, actual generated assignments, nullable nested arrays and generic type signatures. Repaired signatures round-trip through Roslyn including nullability and generated output compiles. |
| `ModelDefaultComparisonContractTests.cs` | 2 | Omitted/empty location defaults and each model's actual generic comparer/HashSet identity. No numeric hashes are asserted. |

Producer recovery cases reuse a real generator driver across an adverse input and its corrected source, preserve the syntax-tree path/options, compare healthy-sibling generated output, and compile repaired generated output. Numeric defaults first carry incompatible decimal metadata, then repair only the default type/value. DefaultValue validates metadata; these cases do not invent field initialization behavior. Global target recovery preserves the producer's existing `:P` fallback without claiming downstream backend scope acceptance.

Template tests keep a healthy projection/template descriptor while repairing only the adverse component/marker input. Role/context/type refusal, warning/error reasons, source coordinates and manifest inclusion are checked. Version exact/build cases preserve compatibility; minor warns while retaining the descriptor; major warns and excludes it before repair.

Formatter tests permit either valid deterministic named alias. They assert actual provenance/keyword/type syntax and compiled assignment behavior, rather than prescribing Min over Max solely to reject a mutation. Invalid aliases/types retain compiler diagnostics and generator continuity while the healthy sibling survives; the repaired source must produce valid, semantically faithful generated signatures.

## Actual verification

- `attempt-01`: initial18-case baseline17pass/1fail. The failed premise assumed command location participates in value identity. Raw output/TRX and original private inputs are retained.
- `attempt-02`: corrected18-case baseline passes;19 finite literal controls compile. Seventeen are detected;13089 and13216 survive.13089 actually changes ushort→int, which the first conversion rows omitted.
- `attempt-03`:19-case baseline adds the supported ushort row and passes.13089/13109 are detected.14003 (valid Max alias choice) and12935 (short-circuited empty-policy helper branch) pass honestly.
- The entire preceding packet and19-case patch are preserved in sibling `../fc-contract-gap-followthrough-pre-recovery-01/`; no earlier raw attempt was edited.
- `attempt-05`: expanded25-case recovery baseline21pass/4fail. All four failures came from an unsupported assertion that unresolved types/aliases must suppress static assignment. The actual source-nameability rule admits syntactically valid error symbols; existing editing guidance permits generated code while compiler errors remain. This failed expectation is retained rather than relabelled as a product defect.
- `attempt-06`: final25-case baseline passes. Thirteen finite literal controls compile and fail meaningful assertions;14003 still passes. Fresh detected IDs:12915,13013,13089,13730,13870,13996,13143,13027,13047,13736,13748,13939,13973. This is a finite control set, not a full mutation rerun.
- `package-check-07`: final prospective patch/source/style checks, including one type per file, public XML summaries/parameters, attribute indentation, declared25-case inventory and read-only apply check.

`per-control-findings.json` binds every tested ID to its exact source span/replacement, original Error status, actual private test-source hashes, source hashes, TRX and outcomes. Earlier literal detections retain their earlier private source closure; they are not relabelled fresh detections of the final recovery proposal. `partial-happy-*.json` retains matching point-in-time raw success/failed/not-covered observations with exact bytes-read hashes; those are not final mutant statuses.

## Systematic remaining boundaries

`remaining-error-nocoverage-review.json` enumerates all186 original Error NoCoverage IDs and links the finite controls where present. The original complete per-ID ledger remains the broader source/cause index. This bounded packet leaves unverified IDs explicit; private control success is not owner acceptance or proof of complete-cohort coverage.

- **12935:** `IsPolicyNameWellFormed` is private and its sole caller short-circuits null/whitespace before calling it. Its trimmed-empty branch cannot be forced through ordinary Parse input. The finite control survives; no reflective/internal-only test was invented.
- **14003:** choosing the other admitted alias changes a public formatted string but still yields usable provenance and compiled generated assignments in these cases. Exact alias-selection stability/external clients remain a boundary; no universal equivalence or accepted disposition is authored.
- **13216:** CommandModel equality/hash omit source location, while DomainModel identity includes it. The generic comparison controls cannot detect changed internal command-location normalization. Diagnostic-origin/default-overload consumers beyond this comparison path remain unresolved.
- **13050:** null TypedConstant.Type after earlier IsNull handling needs an actual supported Roslyn/metadata fixture; no fabricated AttributeData is supplied.
- **13912/13915:** formatter null-argument guards are internal; ordinary parse callers supply real type/compilation objects. A direct internal null test would not establish a public generated-output defect.
- **Template non-class/custom-provider paths and absent namespace/path fallbacks:** current generator predicates and ordinary Roslyn symbol contracts bound reachability, while public custom-provider boundaries must not be generalized away. Existing provider tests cover only their stated contexts.
- **Function pointers/alias signatures:** unsafe assignment is not newly admitted. Current callers mark unsafe shapes unavailable for generated assignment; no compiling-assignment contract is invented for an unsupported shape.
- **Remaining policy escaping/truncation, inherited/default overloads and transform fallbacks:** retain the original ledger's finite follow-up proposals pending complete Happy source-specific reports. The four constructor-default uncertainties are not blanket-classified from one successful comparison.

The single doc-backed window-hash participation candidate remains separately available in `../fc-hash-quality-followthrough/`; it is not duplicated here. No universal hash uniqueness/distribution/performance requirement is added.

## Reproduction and cleanup

All disposable jobs use one fresh `run-with-temp` invocation per attempt with `TMPDIR=/dev/shm`, read-only Git archives of b34 and its root-declared Builds73e60af8, private caches, MTP/Release, two logical processors and disabled build/compiler server reuse. Exact commands, all exits, raw output, source bindings and TRX are retained outside scratch. Scripts refuse existing attempt directories.

```text
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-controls-retry.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-followup-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/package-proposal.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-recovery-controls.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/run-recovery-controls-final.py
env TMPDIR=/dev/shm run-with-temp python3 <this-directory>/package-recovery-proposal.py
python3 <this-directory>/final-audit.py
```

`summary.json` and `evidence-manifest.json` bind the final patch, prior packet, observed counts, unchanged owner bytes and owned cleanup. Parent owns source integration, full current-source verification, any genuine disposition and publication.
