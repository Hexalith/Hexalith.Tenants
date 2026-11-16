---
title: 'Activate current dependency seal and finite mutation inventory repair'
status: 'ready-for-activation'
source_commit: 'a03ee98a8964d56506160bcd9c73de992df56f58'
parent_commit: 'ebd3f76dc0b320d455917c593ff86526b58d0c77'
---

The selected upstream dependency revisions changed the Platform source projection and Builds SDK selection shape. The source commit seals those selected revisions and admits only the exact current FrontComposer selection alongside retained compatibility shapes. It also accounts for three enum-only files whose completed native mutation reports contain empty mutant arrays. Those exceptions require exact canonical source hashes; active results remain audited and untriaged survivors remain refused.

File list:

- `eng/dependency-graph-policy.json`
- `eng/dependency_graph.py`
- `eng/validate-stryker-reports.ps1`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json`
- `tests/eng/test_dependency_graph.py`
- `tests/eng/test_mutation_triage.py`
- `tests/ci-governance/fixtures/builds-sdk42-package-511-qualification.xml`
- This activation record.

The actual current catalog passed the focused reader validation; 36 SDK selection methods/374 subcases and six real MSBuild contexts passed. Nine final enum inventory methods/13 subcases passed through 19 real PowerShell calls, including a demonstrated before/after source-drift refusal. Three independent review layers completed and every finding was triaged; the finite source-binding and active-result controls address the remaining patches.

Historical full native run 43807658283 remains attributed to source `11c8377a858a756c4e4069bf08e62db1f58e1733`: both segments exceeded their original thresholds, but the aggregate failed the three enum inventory checks. This record does not change that outcome or claim current-source native qualification. No thresholds, mutation predicates, private provenance, execution authorities, retained cohorts or approval records changed.

This separate documentation-only commit activates the already committed source seal using the existing event-base policy protocol. Fresh exact-source CI, Quality, security, provenance, release and public package checks remain required. A further full mutation sweep is outside this bounded validation repair. No story 16.2/16.3 execution, runtime transfer or human approval is claimed.
