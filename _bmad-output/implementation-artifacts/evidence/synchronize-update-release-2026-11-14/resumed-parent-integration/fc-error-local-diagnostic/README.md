# Complete e0 Error-only local diagnostic

The complete existing Error-handling segment finished in **3812.328 seconds** with tool exit **0** and authenticated provenance verification exit **0**. Its reported Error-only score is **66.88%**. The unchanged strict both-segment validator exits **1**: Happy evidence is absent and Error has untriaged **630 Survived, 186 NoCoverage and one Timeout**. This is diagnostic evidence for e0, not a full gate, acceptance, or qualification of 695d/future source.

| Original final JSON status | Count |
| --- | ---: |
| Killed | 1647 |
| Survived | 630 |
| NoCoverage | 186 |
| Timeout | 1 |
| CompileError | 717 |
| Ignored | 562 |

All **2278 planned executable mutants** completed. The additional186 NoCoverage entries were not executed; total live inventory is2464. All717 raw CompileError candidates have the exact pinned nonexecuted compiler-rollback shape and remain preserved. No disposition, ownership, equivalence, deferral or human approval was added.

The final Timeout is **14021**, `Parsing/SourceTypeNameFormatter.cs:200–202`: removal of the body of `while (parts.Count > 0)` removes its `Pop`, so the mutated namespace loop cannot make progress. Its debug console line says `Test run ... is success`; the final original JSON marks it Timeout. Consequently the partial631 success observations are **not**631 final Survived results. Final report statuses supersede those partial console tallies.

The original **303 cases** and every coverage/killing test ID resolve in `source-and-case-bindings.json`. `problem-mutants.json` indexes each actual Survived/NoCoverage/Timeout by source path, span, replacement and ID, with no disposition. Full coverage/killing arrays and original source/test bodies remain in the raw JSON. Larger survivor groups include Parsing/DomainModel152, Transforms/FormFieldModel57, CommandRendererModel50, RazorModelTransform35 and ColumnModel34; these counts establish no equivalence or acceptance.

The private clone was explicitly detached at `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`. Frozen Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` was exported from existing root Git objects, without nested checkout/init. Source and catalog raw input inventories are separate files. The unmodified authenticated private definition produced `5.1.0-hexalith.compat.1+upstream.7052fa6c.patch.b631f905b07a`, build digest `3c083161562b01bef116b3cbeb54924d3749f6e9100401ffc026e59798b1735b`. Actual compiled metadata/runtime hashes and pre-execution source/config/tool receipt are retained.

The run used the original complete Error category union, original mutation roots, concurrency2, additional timeout60000 and threshold60. The documented MTP/processor/compiler-server environment was retained. No proposed13-case regression file, GitHub context, workflow/config/tool patch, triage edit, retained backend operation or shared Git mutation was introduced. Owner HEAD independently advanced to695d under the parent and remains clean; this does not change this run's e0 attribution.

Replay used one invocation from the root:

```sh
TMPDIR=/dev/shm run-with-temp python3 _bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/fc-error-local-diagnostic/run.py
```

`run.py` intentionally refuses an existing attempt directory; replay must use a fresh evidence destination while retaining the original attempt. All exact phase commands/exits are in `attempt-01/execution.json`. Tool restore, full Release restore, private tool preparation and actual SourceTools build passed. Strict validation ran unchanged after the original reports were copied; no fake Happy evidence was supplied.

Useful files under `attempt-01/`:

- `summary.json`: compact counts, bindings, exits and cleanup evidence.
- `original-error-artifacts/reports/source-tools-error-handling.json` and `.html`: untouched originals, copied before validator normalization.
- `original-error-artifacts/tool-provenance.json` and `private-tool-build-provenance.json`: exact source/config/tool and private compiled build identity.
- `complete-error-segment.log`: full actual console and resource diagnostics.
- `strict-both-segment-validator.log` and `post-validator-artifacts/mutation-validation-errors.txt`: actual refusal.
- `post-validator-artifacts/`: normalized reports and original-source/status audit from strict validation.
- `source-and-case-bindings.json`, `problem-mutants.json`, `nonexecuted-compiler-candidates.json`: bounded derived inventories.
- `artifact-manifest.json` and `final-artifact-manifest.json`: original capture hashes and complete derived-packet hashes.

All original captured log/artifact hashes were rechecked. The wrapper and private workspace were removed; no owned processes remain. Existing long-lived local workers were untouched. Observed memory headroom remained ample; raw resource samples are retained. No report, score or status was rewritten to manufacture qualification.
