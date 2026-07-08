# Edge Case Hunter Review Prompt

Use the `bmad-review-edge-case-hunter` skill. Return only the JSON array required
by that skill.

You may read the project. Scope is the current changes for
`_bmad-output/implementation-artifacts/spec-gh-actions-28955588184-85913670217.md`
and the `references/Hexalith.Builds` submodule pointer `890553b..ea1d02f`.

Focus areas:
- GitHub Actions reusable workflow behavior
- Composite action semantics
- Retry behavior
- Dapr init partial cleanup
- Port wait edge cases
- Token/image-registry environment
- Whether callers still pass the intended runtime version

## Diff Commands

Run these commands from repository root to reconstruct the diff:

```bash
git diff --no-index -- /dev/null _bmad-output/implementation-artifacts/spec-gh-actions-28955588184-85913670217.md || true
git diff --submodule=log -- references/Hexalith.Builds
git -C references/Hexalith.Builds diff --no-renames 890553bd638b8ecba769555b81f81d80538dae25..ea1d02f8d0b3a34f6039262549b807b1e12729f3 -- .github/workflows/domain-ci.yml .github/workflows/domain-release.yml Github/dapr-init/action.yml Github/dapr-init/README.md
```
