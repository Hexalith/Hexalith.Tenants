---
status: done
date: '2026-10-31'
baseline_commit: c12761223eafbe863d0a20c946685873173a9da4
---

# Adopt the qualified first OCI publication inventory reader

## Intent

Release41211426470 passed exact-source gates and prepared all five6.0.0 packages,
including the actual package-only catalog consumer. It stopped before publication
because the shared execution35f185 reader rejected tenants-api's authenticated
initial tag-list404/NAME_UNKNOWN response. The retained native tenants repository
returns200 with68 tags. No6.0.0 tag or package was created by that attempt.

## File Scope

Allowed files for this story:

- `.github/workflows/release.yml`
- `_bmad-output/implementation-artifacts/spec-adopt-first-oci-inventory-reader.md`

## Change

Adopt exact owning Builds commit ff3b7628132059bc42e04fa8c98c1c427b528f62 in the
normal release workflow's three execution declarations. The full shared change
and refusal matrix were inspected by the parent before safe owner integration.
The new reader accepts only bounded, valid, duplicate-free authenticated initial
404/NAME_UNKNOWN as an empty inventory. Exact-tag absence, all other lookup
failures and every source/identity/authority/version/overwrite gate remain enforced.

All Builds manifest, action, workflow, catalog and dependency paths are unchanged
from a22b55f; the original producer checkout and outputs remain untouched. No
runtime graph, dependency version, recovery path, SDK availability or native/public
behavior changes in this adoption. The pending normal release stays6.0.0.

## Verification

Shared reader proof:40/40 affected tests pass; baseline reproduces exactly the new
positive-control error. Actual authenticated registry inventory and independent
exact6.0.0 tag-absence proofs pass. Evidence remains in the scoped owning Builds
spec and `/tmp/builds-first-oci-actual-registry-proof.json`.

The affected release workflow/recovery contract and provenance controls pass249/249,
zero errors/failures/skips/not-run, in337.569s. This includes the mandatory recovery
fixture subprocess suite. The disposable test assembly's selected sources match
current tracked source after repository line-ending normalization; its workflow
input is the exact updated file. Log: `/tmp/tenants-first-oci-release-workflow-tests.log`.
All three execution declarations bind the exact shared fix. Pinned commitlint passes.

Require exact-source hosted CI/source-reference gates and normal release next.
Preserve protected production approval, actual archives, both images and immutable
source/destination evidence. Do not rerun the unchanged failed release source.
