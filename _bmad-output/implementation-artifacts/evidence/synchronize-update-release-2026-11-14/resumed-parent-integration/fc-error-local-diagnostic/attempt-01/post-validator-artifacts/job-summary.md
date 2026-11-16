## Mutation Evidence

- Stryker tool: 5.1.0-hexalith.compat.1+upstream.7052fa6c.patch.b631f905b07a (official base pin 5.1.0; exact private provenance required=True)
- Target manifest: tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json
- Approved roots: src/Hexalith.FrontComposer.SourceTools/Parsing, src/Hexalith.FrontComposer.SourceTools/Transforms
- Target file count: 28
- Submodules: root-level checkout only; no recursive nested submodule command is used

### Segments
- error-handling: report artifacts/mutation/error-handling/reports/source-tools-error-handling.json, liveMutants=2464, survived=630, noCoverage=186, timeout=1, compileError=717 (nonexecuted invalid candidates=717)
