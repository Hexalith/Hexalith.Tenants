# Sprint change proposal for hosted qualification and deployment evidence

Applied on 21 November 2026 to the Tenants sprint plan at `9196587b`.

## Issue and impact

The sprint tracker still requested hosted evidence after all four live-route jobs finished. Its wording counted cold bootstrap and authenticated REST separately and omitted anonymous UI denial. The sprint generator also flagged the document title and overview heading in the epic plan. The two headings introduce the plan and its sequence of work. All 32 numbered stories were already tracked.

The correction affects release evidence under Epic 5, consumer rollout under Epic 2, and identity and capacity under Epic 1. The PRD and architecture already define the required behavior. The remaining work belongs to the deployment owners.

## Hosted results for the planning revision

CI run 44920915317, attempt 1, ran on Tenants `9196587bf7b94b9f8eb5e235934ca124b58942fb`. The candidate records Builds `055d7df5`, EventStore 5.4.0 from producer `2ec21f631a2e`, and McpCli 3.0.0. Its workflow selected Dapr CLI 1.18.2 and runtime 1.18.7. GitHub reports success for shipping SDK selection, package graph and backend verification, the suite-specific tests, and results upload in each job. [Candidate workflow](https://github.com/Hexalith/Hexalith.Tenants/blob/9196587bf7b94b9f8eb5e235934ca124b58942fb/.github/workflows/ci.yml) · [Package declaration](https://github.com/Hexalith/Hexalith.Builds/blob/055d7df5b49669d5b705ce163b0b2e187ecb9a50/Props/release-declarations.json)

| Suite | Verified behavior | Hosted result |
| --- | --- | --- |
| `rest` | Cold bootstrap, authenticated reads, public system denial and cursor recovery | [Passed](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44920915317/job/136896531406) |
| `persisted` | Worker-backed delivery and persisted state | [Passed](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44920915317/job/136896531365) |
| `anonymous-ui` | Anonymous browser access denied | [Passed](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44920915317/job/136896531338) |
| `authenticated-ui` | Authorized browser flows, responsive modes and contextual navigation | [Passed](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44920915317/job/136896531349) |

The overall workflow concluded **cancelled**. The separate Aspire job was cancelled and the performance job was skipped. The four live-route results close the November audit action for this revision. The subsequent `main` commit `c1387ebb` changed package settings and submodule revisions and has its own CI run. [Workflow conclusion](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44920915317) · [Subsequent candidate](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44922720931)

## Hosted qualification for 118293e1

CI run 44900478179, attempt 1, tested Tenants `118293e1ac0c6f4a50d2d440dd9958f371637212`. Each profile passed its designated test step. The table counts the profile-specific tests. The shared bootstrap, resource graph and TLS checks also passed. The SHA-256 of each downloaded archive matched the digest published by GitHub. [CI run 44900478179](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179)

| Profile | Tests passed / executed | Job | Test results and package provenance |
| --- | ---: | --- | --- |
| Cold bootstrap and authenticated REST | 9 / 9 | [REST job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/job/136835186897) | [REST artifact](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/artifacts/14706396952) |
| Worker-backed delivery and persisted state | 7 / 7 | [Persisted job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/job/136835186841) | [Persisted artifact](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/artifacts/14705962633) |
| Anonymous UI access denial | 6 / 6 | [Anonymous UI job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/job/136835186945) | [Anonymous UI artifact](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/artifacts/14706640169) |
| Authenticated browser flows | 3 / 3 | [Authenticated UI job](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/job/136835186840) | [Authenticated UI artifact](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/artifacts/14706755632) |

All four archives identify EventStore.Aspire 5.4.0 from producer `2ec21f631a2e292fba8c5537fe665ab275ed1081`. Each contains runtime manifest hashes for the AppHost, domain, API and UI. Each host selected EventStore.Contracts 5.4.0 and McpCli.Abstractions 3.0.0. This evidence qualifies the committed dependency revisions on that Tenants candidate. Later candidates have their own hosted results. [Architecture decision AD-14](architecture.md#ad-14---production-operations-and-scaling-are-platform-governed-adopted)

Overall CI failed on five EventStore package-version assertions in `PackageGovernanceTests`. The separate source-reference integration run failed when its EventStore host checkout differed from the restored package producer. These failures describe the earlier revision. [Build and test](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900478179/job/136835186929) · [Source-reference integration](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/44900477964/job/136835177870)

## Applied changes

| Artifact | Previous record | Correction |
| --- | --- | --- |
| Sprint tracker | Hosted qualification open, with three distinct suites named | Recorded the four suite results for the planning revision. Restored the earlier-grant replay and signing-key rotation checks to the deployment handoff. |
| PRD §14, addendum §J and architecture conformance table | Four outstanding checks, with historical local package evidence | Linked the candidate's hosted results and identified the three outstanding deployment checks. |
| Epic plan | “tenants - Epic Breakdown” and “Epic List” both triggered warnings | Renamed the headings “Tenants delivery plan” and “Delivery sequence” and linked the candidate evidence. |

This was a minor, direct adjustment to the planning records. It required one documentation pass. The principal risk was attributing a passing job to another candidate or to the entire release. The job links and full candidate revision establish the boundary. Product scope, story acceptance criteria and UX behavior required no edits.

## Deployment handoff

| Open check | Owner | Evidence that closes it |
| --- | --- | --- |
| First enforcing consumer | Consumer deployment owner | Identify the deployment by a safe alias. Using its durable shared store and authorized history, record the denied access decision during a sequence gap, the repaired checkpoint sequence, and continued denial after restart and replay of the earlier grant. [Consumer walkthrough](../../docs/sample-consuming-service-walkthrough.md) |
| Production identity | Deployment owner | Record redacted outcomes through the deployed ingress for TLS discovery, the configured issuer and audience, signing-key rotation, and authorized and denied requests. [Production authentication guide](../../docs/production-auth-readiness.md) |
| Tenant-index capacity | Deployment owner | Record the tenant and membership counts, index size, change rate, concurrent writers, write conflicts, exhausted retries and p95 update latency over a representative observation window. Compare latency with the deployment freshness target to decide whether partitioning warrants migration. [Audit baseline](sprint-change-proposal-2026-11-20.md) |

The latest GitHub deployment record available at the 21 November review was created on 18 November, before the audit. For each observation, record the environment alias, revision or package, time and reviewer. [Deployment history](https://api.github.com/repos/Hexalith/Hexalith.Tenants/deployments) · [Evidence template](../../docs/deployment-readiness.md#evidence-template)

The deployment owners can collect these observations independently. The user confirmed that deployment reports were unavailable at this review. The Product Owner can update each existing action when its evidence is available. [Sprint tracker](../implementation-artifacts/sprint-status.yaml)
