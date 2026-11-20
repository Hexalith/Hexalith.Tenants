# Reconciliation of the 20 November 2026 technical audit

The product owner approved eight corrections within the existing MVP and later phases. The sprint tracker records the developer work as done and assigns four deployment or release checks. The evidence below comes from the approved audit of `1806474afe48` and the corrections published in `8318f917`. [Change proposal](../../sprint-change-proposal-2026-11-20.md) · [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml)

| Correction | Product acceptance or technical disposition | Local evidence recorded in the audit |
| --- | --- | --- |
| Roster before creation | FR-8 and NFR-2. For an absent tenant or a history containing only rejections, global administrators receive a missing-tenant result and ordinary callers receive forbidden. After creation, an authorized caller can read the roster, including a genuinely empty roster. | The Server suite passed the creation-history regression. |
| Downstream membership | FR-12 and NFR-3. Tenants can confirm a removal locally while a consumer is behind. A service enforcing access uses a durable checkpoint and authoritative history to repair gaps. While it cannot verify access, it denies or defers protected work. | Client coverage withheld a removal, restarted the consumer with the gap outstanding, repaired history and replayed an older grant. |
| Public API identity | NFR-2. Both the public REST API and domain host require valid Production OIDC settings before serving requests. The deployment guide owns configuration keys and the Development signing-key path. | Sixteen Production startup cases passed twice. The controller suite exercised an authenticated endpoint. |
| Release qualification | Release readiness. The cold bootstrap, persisted-state, authenticated REST and authenticated UI profiles must exercise the same source revision and shipping package set. Local manifest checks establish package selection. | The four local host manifests selected EventStore.Contracts 5.4.0 and McpCli.Abstractions 3.0.0. |
| Sample readouts | Integration and onboarding. The sample exposes its membership and configuration readouts only in Development. Its subscription and health endpoints retain their existing behavior. | The sample host tests covered the environment boundary. |
| Tenant-index cost | Discovery and performance evidence. Readers retain the same index version for equivalent state under replay. The allocation result describes one fixture. A partitioning decision requires deployed workload measurements. | The fixed digest matched. Allocation fell from 31.4 to 27.2 MiB per fingerprint at 10,000 tenants and 50,000 memberships. |
| Global-administrator grant | FR-19. The rendered grant action, submission and retry use the same eligibility decision. Existing caller, preview, focus and last-administrator safeguards still apply. | The UI suite passed, including 355 global-administrator component tests. |
| Onboarding | The release description identifies the operator actions available with the appropriate permissions. The quickstart owns PowerShell and Aspire prerequisites. Phase 2a remains the original read-only milestone. | The developer checked the documented tools and entry points. |

## Evidence still due

| Sprint tracker ID | Owner | Evidence needed |
| --- | --- | --- |
| `audit-2026-11-20-hosted-qualification` | Release owner | Record all four hosted profile results on the same source revision and shipping packages. |
| `audit-2026-11-20-consumer-rollout` | Consumer deployment owner | Demonstrate authorized history access, durable shared state, and correct access after a delayed removal and restart in the first enforcing deployment. |
| `audit-2026-11-20-identity-rollout` | Deployment owner | Verify actual issuer, audience, discovery TLS, ingress and key rotation. Record redacted evidence. |
| `audit-2026-11-20-index-capacity` | Deployment owner | Measure index size, write contention and update latency at representative load before deciding on partitioning. |

The original PRD and decision log define Phase 2a as read-only. The README now documents permission-gated commands delivered in later phases. This resolves the apparent scope conflict in the PRD’s original description of operators relying on raw API calls. The approved user journeys and their confirmation and focus behavior continue to apply. [PRD](prd.md) · [Decision history](.memlog.md) · [Current module capabilities](../../../../README.md)
