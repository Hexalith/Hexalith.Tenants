# Review prompt: blind adversarial hunter

Invoke the `bmad-review` skill with only the `adversarial` lens on the complete working-tree diff from baseline commit `578770679b9d3bc3fdf2a8a78190f24cdad8576e` in `/home/administrator/projects/hexalith/tenants`.

The diff includes all tracked and untracked files. Inspect it with:

```bash
git diff 578770679b9d3bc3fdf2a8a78190f24cdad8576e -- .
git status --short
```

Review the partial-release recovery workflow, its three scripts, `.releaserc.json`, and governance-test changes. Report concrete findings with file and line references, prioritizing unsafe publication, source identity, secret exposure, or immutable-artifact violations.
