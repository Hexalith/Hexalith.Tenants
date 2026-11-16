# Tenants 8.0.1 — released 2026-11-16

[Published release](https://github.com/Hexalith/Hexalith.Tenants/releases/tag/v8.0.1), source **`ab67fd6bc308b5a641b0ca398d776e347193c168`**. Completion documentation is a later commit; the release tag stays on this source.

| Frozen root dependency | Full gitlink |
| --- | --- |
| AI.Tools | `7ef8166ef87e1365f86d1a1b3aaf0235545177d4` |
| Builds | `fdda17c0a4538b0f7c99f813c20ca24b49006530` |
| Commons | `ab5e92faa3bab1fa43b8ecadd423432dfe652bb2` |
| EventStore | `8b6d5bee5217b3f4bbd8fd689ad887aca863961b` |
| FrontComposer | `9ea805dc7bbdf048b282e66e94115add295099a3` |
| McpCli | `4cf120c410a668d08c6ba70d59962c3a2c031952` |
| Memories | `65bfb98dca7f65c005213507b15fbe4ab45f2e19` |
| Platform | `ead9ff0d4268efaa7054b3a14b45956b647f6171` |
| PolymorphicSerializations | `476cfccac3c5ba694a8e0ef11b1157728b9ec093` |

Exact-source Tenants gates succeeded: [CI 43876176872](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/43876176872), [source integration 43876176366](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/43876176366), [Story Guards 43876176408](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/43876176408), [CodeQL 43876176808](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/43876176808), [Commitlint 43876176890](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/43876176890): **17 successful jobs, one existing configured performance skip**. [Release 43880703312](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/43880703312) succeeded, including both published container checks (`tenants`, `tenants-api`). Production approval history records `jpiquot`; persistent protections were unchanged.

Selected owner runs succeeded: FrontComposer CI43856960601, Quality43856959801 (all eight jobs), CodeQL43856960406, Commitlint43856960438; EventStore CI43862076142 and integration43862075310 (attempt2); Builds43834424445; Memories43840056991; McpCli43752669619; Platform43833448719,43833448613,43843447635. EventStore's seven push-applicable required contexts passed; its PR-only dependency review was inapplicable. Its intentionally unbound live authority evaluation remains a failure, separate from the passing required fixtures.

All five NuGet packages are listed at **8.0.1**, with repository source **`ab67fd6bc308b5a641b0ca398d776e347193c168`**: [Contracts](https://www.nuget.org/packages/Hexalith.Tenants.Contracts/8.0.1), [Client](https://www.nuget.org/packages/Hexalith.Tenants.Client/8.0.1), [Server](https://www.nuget.org/packages/Hexalith.Tenants.Server/8.0.1), [Testing](https://www.nuget.org/packages/Hexalith.Tenants.Testing/8.0.1), [Aspire](https://www.nuget.org/packages/Hexalith.Tenants.Aspire/8.0.1). Independent verification passed official SHA512, native signatures and payload equality apart from NuGet's repository signature against candidate artifact14231529383, producing run43880703312/attempt1/job133345717175. The complete receipt and signature logs remain local under `evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/root-8.0.1-public-verification-attempt-2/`; the incomplete earlier receipt remains preserved.

Compatibility holds: verified FrontComposer6.0.0 and EventStore5.1.1 packages; Testing.Integration3.128.4 bootstrap and McpCli.Abstractions2.0.1 cohort. McpCli's later source needs unavailable Projects.Contracts2.0.0-story66.3. See [deferred work](deferred-work.md) for the Development duplicate-port issue and earlier owner findings. FrontComposer6.0.1 publication, further mutation runs, migration/census approval and pipeline modernization remain separate work; no broader qualification is inferred.

The [approved pragmatic policy](../planning-artifacts/sprint-change-proposal-2026-11-16-pragmatic-release-policy.md) grants the release-scoped local-checker exception, expiring after completion bookkeeping. Original gates remain; the proposed Dapr evidence-only step was deferred. Bulk diagnostics remain local after forward removal from the release tree; the intermediate public commit remains in history. Earlier maintainer pushes reported Platform's PR-only bypass and EventStore's PR-only/eight-status-check bypass; selected required checks subsequently passed. Other FrontComposer pushes may resume.
