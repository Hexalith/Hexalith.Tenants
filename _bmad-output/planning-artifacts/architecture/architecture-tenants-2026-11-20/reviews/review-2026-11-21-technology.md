# Technology review, 21 November 2026

The package versions, Dapr profiles and test-fixture ownership in the update agree with their declarations. Hosted qualification remains open.

| Check | Evidence |
| --- | --- |
| Package selection | Tenants `Directory.Build.props` sets McpCli 3.0.0 and imports Builds. The catalog at `e1380b8` selects EventStore 5.4.0, FrontComposer 6.0.1 and Memories 4.1.0. At the gitlink recorded by Tenants `f15f6170`, the corresponding defaults are 5.3.1, 6.0.0 and 4.0.0. The root SDK declaration and AppHost project specify 11.0.100 and 17.0.0 respectively. |
| Dapr | The pulled catalog selects runtime 1.19.2 and CLI 1.19.1. The authenticated-routes job explicitly requests runtime 1.18.7 and CLI 1.18.2. |
| Host provenance | The selector reads the EventStore release declaration from Builds. The package verifier matches the host checkout with the restored Aspire package's producer commit, checks its hash, and records each of four runtime manifest hashes, runtime targets and package identities. |
| Fixture ownership | AspireTopologyFixture and TenantsDaprTestFixture derive from Tenants-owned harnesses. The integration project consumes EventStore.Testing.Integration for shared diagnostics. |

Two editorial findings concern reproducibility. Cite the Dapr qualification note at its immutable upstream revision because the recorded Builds gitlink lacks that file. Describe the manifest evidence as hashes, runtime targets and selected packages to match the verifier output.
