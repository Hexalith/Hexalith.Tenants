# Adversarial Divergence Review

Verdict: pass after author fixes.

Attack model: two teams build one level down while obeying the spine literally.

Cases tested:

- Tenant list team registers `/tenants`, while Global Administrators team registers `/global-administrators` as a second Tenants shell entry. AD-1 blocks this.
- Users tab team builds a complete "All Users" inventory by paging tenant members, while another team treats Users as lookup-only. AD-2 blocks the inventory claim and ties Users to the existing lookup endpoint.
- Tenant list search team renders row data from Memories, while detail pages hydrate through Tenants reads. AD-10 blocks this by making Memories id-only.
- Audit receipt team renders raw event payload fragments, while command panels use safe localized copy. AD-9 blocks raw payload output and assigns receipt assembly to the BFF.
- Command teams independently choose concurrency policies: create tenant allows concurrent submits, remove user uses one-at-a-time, configuration edit uses optimistic success. The initial draft was too implicit; AD-12 now blocks the divergence.
- Hosting team tries to move Tenants UI into FrontComposer as a generic app sample, while module team expects a domain-owned container. The initial draft was too quiet; AD-13 now blocks the divergence.

Residual risk:

- The spine cannot by itself prove that every existing component follows AD-3 and AD-11; the existing conformance tests remain the enforcement mechanism.

