## [3.2.5](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.2.4...v3.2.5) (2026-07-12)


### Bug Fixes

* update Hexalith.Memories subproject reference ([49f23a3](https://github.com/Hexalith/Hexalith.Tenants/commit/49f23a30809de9085c8263edaa1cd56d69b7ba81))

## [3.2.4](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.2.3...v3.2.4) (2026-07-12)


### Bug Fixes

* resolve CI restore submodule version drift ([#26](https://github.com/Hexalith/Hexalith.Tenants/issues/26)) ([0415f1d](https://github.com/Hexalith/Hexalith.Tenants/commit/0415f1d97f61d49098c5fe05d70c6343a7cf5abc))
* update subproject references for Hexalith components ([1c72beb](https://github.com/Hexalith/Hexalith.Tenants/commit/1c72beb39b51c1cecc96f5f93829bdf7d38435ad))

## [3.2.3](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.2.2...v3.2.3) (2026-07-09)


### Bug Fixes

* align CI/CD workflows with standards, enhance release gating, and improve validation scripts ([5738721](https://github.com/Hexalith/Hexalith.Tenants/commit/57387211738a85d8cd542de2332647c7cc70b182))
* remove obsolete release configuration and status files ([05d29f1](https://github.com/Hexalith/Hexalith.Tenants/commit/05d29f16c73464303eb601344621f211e9efbbbd))
* **tests:** harden package governance assertions ([69708aa](https://github.com/Hexalith/Hexalith.Tenants/commit/69708aaabb196f0875a741296a62c479aca58fd2))
* update PackageGovernanceTests to align with current CI/CD standards and remove obsolete configurations ([b2f190a](https://github.com/Hexalith/Hexalith.Tenants/commit/b2f190ac74239312437064960e6bb2b2a4180d01))
* update subproject reference for Hexalith.Builds ([d7e8cfe](https://github.com/Hexalith/Hexalith.Tenants/commit/d7e8cfef5fb650f632ff2055dae7a25acc41e993))

## [3.2.2](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.2.1...v3.2.2) (2026-07-09)


### Bug Fixes

* update subproject reference for Hexalith.FrontComposer ([c506c97](https://github.com/Hexalith/Hexalith.Tenants/commit/c506c97cfba7cb28c9f5a5b700abc4a3b9cb21db))

## [3.2.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.2.0...v3.2.1) (2026-07-08)


### Bug Fixes

* **ci:** preflight release publish secrets ([a1e8cc5](https://github.com/Hexalith/Hexalith.Tenants/commit/a1e8cc5e14fb461299eb426d4bcf1a4b0ba2bd95))

# [3.2.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.1.1...v3.2.0) (2026-07-08)


### Features

* add preflight script to validate release secrets before publishing ([08b8201](https://github.com/Hexalith/Hexalith.Tenants/commit/08b82017e26d8f73c9bfc5416930d6387e3c4865))

## [3.1.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.1.0...v3.1.1) (2026-07-08)


### Bug Fixes

* **ci:** finish Dapr init bootstrap remediation ([fd84cfd](https://github.com/Hexalith/Hexalith.Tenants/commit/fd84cfdaecc55e7ab2817880bf87b57324a82470))

# [3.1.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v3.0.0...v3.1.0) (2026-07-08)


### Features

* add acceptance auditor, blind hunter, edge case hunter prompts and spec for Dapr init bootstrap fix ([d0cecb2](https://github.com/Hexalith/Hexalith.Tenants/commit/d0cecb24868dfcc9e8582bdac5607c3090826e43))

# [3.0.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.4.2...v3.0.0) (2026-07-08)


* feat!: release tenants container on zot ([09699ca](https://github.com/Hexalith/Hexalith.Tenants/commit/09699ca352a169318f283b88dc5312860aaeccd4))


### Bug Fixes

* **deps:** update Hexalith.Builds and Hexalith.EventStore submodule references ([d054581](https://github.com/Hexalith/Hexalith.Tenants/commit/d0545811d025eb3d81c1a47f479f16b46015db5e))
* **deps:** update Hexalith.Builds submodule reference ([08659c4](https://github.com/Hexalith/Hexalith.Tenants/commit/08659c426f3a665950a643292751baa7a92ec31c))


### Features

* enhance project reference handling and update EventStore version ([7c461c9](https://github.com/Hexalith/Hexalith.Tenants/commit/7c461c9afcbc52194b1580b351ad967480bc3668))


### BREAKING CHANGES

* publish the Tenants container image through the semantic-release pipeline.

## [2.4.2](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.4.1...v2.4.2) (2026-07-08)


### Bug Fixes

* align RestApi generator dependency for consistent CI restore/build behavior ([7a3eb34](https://github.com/Hexalith/Hexalith.Tenants/commit/7a3eb34e35bb955471a3be588ceec2b18f03f015))

## [2.4.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.4.0...v2.4.1) (2026-07-08)


### Bug Fixes

* **ci:** normalize coverage paths and release publish ([432f9f8](https://github.com/Hexalith/Hexalith.Tenants/commit/432f9f807620f7478ee3cd4bae6ca993f826215e))

# [2.4.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.3.0...v2.4.0) (2026-07-08)


### Bug Fixes

* **ci:** resolve package and sample test failures ([7850647](https://github.com/Hexalith/Hexalith.Tenants/commit/78506475fda4d34f0a1250ab8ad9e77ddced857b))
* update EventStore project references to use the latest version ([e2a197d](https://github.com/Hexalith/Hexalith.Tenants/commit/e2a197dc4129f59acb5745b96f0353f70b979eeb))


### Features

* **api:** harden generated tenants api host ([f3844d3](https://github.com/Hexalith/Hexalith.Tenants/commit/f3844d34e314b96b7e5caf63aee2c5a5f2cbcf6a))
* update AI assistant instructions and improve submodule handling ([c016842](https://github.com/Hexalith/Hexalith.Tenants/commit/c016842b36f333cf70edd7d7c15711731c72f8b9))
* update project reference conditions for Debug configuration ([e9cbe82](https://github.com/Hexalith/Hexalith.Tenants/commit/e9cbe824ce2033faef7ca666ca65018a2184e070))
* update UseHexalithProjectReferences condition and increment EventStore source gateway version ([dde9c45](https://github.com/Hexalith/Hexalith.Tenants/commit/dde9c45c782efd90e80c000019c3c3759d596e58))

# [2.3.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.2.0...v2.3.0) (2026-07-06)


### Features

* add ApiScope to command and query routes for tenant management ([b7ae7bd](https://github.com/Hexalith/Hexalith.Tenants/commit/b7ae7bdb15947b82f031503618e08ffe10b0e4cc))
* enhance Dapr end-to-end tests with AssertAccepted method for command acceptance checks ([3aaf2cf](https://github.com/Hexalith/Hexalith.Tenants/commit/3aaf2cf188ffa4aee68098255769d69f2710837e))
* update query metadata and align EventStore gateway version for compatibility ([a7fa6bd](https://github.com/Hexalith/Hexalith.Tenants/commit/a7fa6bd9cc473aaa1338c01e857e6e74e7715aa1))

# [2.2.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.1.2...v2.2.0) (2026-07-03)


### Bug Fixes

* adopt HexalithEventStoreSecurityExtensions for improved security handling in Tenants AppHost ([ae26b39](https://github.com/Hexalith/Hexalith.Tenants/commit/ae26b396498eb16b7dbcfe760b5e2ae4b6e81bc7))
* **bootstrap:** quiet idempotent global-admin bootstrap on restart ([0dd28c3](https://github.com/Hexalith/Hexalith.Tenants/commit/0dd28c3885ce4d72ec50fb3a0e0f167605506c0d))
* **eventstore:** stop /tenants/events poison-message loop via submodule bump ([#25](https://github.com/Hexalith/Hexalith.Tenants/issues/25)) ([cc56938](https://github.com/Hexalith/Hexalith.Tenants/commit/cc5693838bd1f8e0dd6fab533b90aef9fe1c3b92)), closes [Hexalith/Hexalith.EventStore#274](https://github.com/Hexalith/Hexalith.EventStore/issues/274)
* refactor DAPR service address resolution for improved clarity and configuration handling ([d3034b0](https://github.com/Hexalith/Hexalith.Tenants/commit/d3034b0a9c5d7daad76cb46c6c65d863751d094b))
* **references:** mark Hexalith submodules as dirty after updates ([aa67c00](https://github.com/Hexalith/Hexalith.Tenants/commit/aa67c0073e8d7ff7ff6c38968f6f32925f1fc11f))
* **references:** mark Hexalith.EventStore subproject as dirty after updates ([47a6eaf](https://github.com/Hexalith/Hexalith.Tenants/commit/47a6eaf49643c534bc2004391674e04326a2df51))
* **sprint-status:** update last_updated date and change eventstore-read-model-freshness-metadata status to done ([ba14356](https://github.com/Hexalith/Hexalith.Tenants/commit/ba14356a8b2b648eda24a4dd7fbd25d60e0d674d))
* **tenants:** address navigation and cursor issues in tenant panels and workspace ([f30d874](https://github.com/Hexalith/Hexalith.Tenants/commit/f30d87433505d48273970b3e498b13dafafd05a2))
* update HexalithEventStore and HexalithFrontComposer paths for correct submodule resolution ([0071a44](https://github.com/Hexalith/Hexalith.Tenants/commit/0071a44448ac30bf3974a903ca0b03dbab0717c8))
* update submodule paths and add references for Hexalith components ([9a86cb5](https://github.com/Hexalith/Hexalith.Tenants/commit/9a86cb50d3ebb5a9311b76f52f1488793bec0893))
* update subproject commit reference for Hexalith.FrontComposer ([110fd59](https://github.com/Hexalith/Hexalith.Tenants/commit/110fd59a797fdb86fbed707f289bc05bb80b7f9c))
* update subproject commit reference for Hexalith.Memories ([cd6319e](https://github.com/Hexalith/Hexalith.Tenants/commit/cd6319e50034fd6fcf706e2379bf69640c72aadc))
* update subproject commit references for Hexalith modules ([ae09c8f](https://github.com/Hexalith/Hexalith.Tenants/commit/ae09c8fae2b8b84d86972bc7d447c630aba68e45))
* update workflow references to specific commit hashes for stability ([1a64a34](https://github.com/Hexalith/Hexalith.Tenants/commit/1a64a3451ae7ec215882fdda949c697a94694b97))
* update workflow references to use the main branch for stability ([80bd492](https://github.com/Hexalith/Hexalith.Tenants/commit/80bd492e5f1c03dd3bb41955bdaadc40723bd4e0))


### Features

* Add Epic 1 retrospective follow-through proposal and update sprint status action items ([6e21b7c](https://github.com/Hexalith/Hexalith.Tenants/commit/6e21b7c8460ff260081c81f165e2859c58f75c6e))
* Add Epic 2 retrospective follow-through proposal and register action items in sprint status ([31a2295](https://github.com/Hexalith/Hexalith.Tenants/commit/31a22956a4e10cef3c1a092fa33a38fc6d6659b6))
* Add Epic 4 retrospective follow-through proposal and register action items in sprint status ([5c1f211](https://github.com/Hexalith/Hexalith.Tenants/commit/5c1f211b014bb13828d926bc041a833a42e6c850))
* add Hexalith.Commons references and update project configurations ([9a1da70](https://github.com/Hexalith/Hexalith.Tenants/commit/9a1da70689bca49e2ee9b0c44fe411ed0ea368f0))
* Add Microsoft.OpenApi package version 2.9.0 to Directory.Packages.props ([6a4aeec](https://github.com/Hexalith/Hexalith.Tenants/commit/6a4aeec8dea66cff8d63c3d733642a1dc0dbab9a))
* add MyTenantsPanel and UserMembershipLookupPanel components ([675dced](https://github.com/Hexalith/Hexalith.Tenants/commit/675dced1d5e393aa8e93e584dff1ca16c0f8b5ae))
* Add retrospectives for Epic 3 and Epic 5, including action items and participant feedback ([6e78294](https://github.com/Hexalith/Hexalith.Tenants/commit/6e782942b13413d9a61a0ccbb4292d0684371af3))
* add service registration helpers for Tenants UI module ([e6327cd](https://github.com/Hexalith/Hexalith.Tenants/commit/e6327cd4fa7984bec49866843b075be1044675a2))
* add TenantQueryGateway registration for service configuration ([f2a3e68](https://github.com/Hexalith/Hexalith.Tenants/commit/f2a3e6805cb9426b3d8534dac730035304c1f7a8))
* Complete cleanup of correction projection refresh logic ([ac21621](https://github.com/Hexalith/Hexalith.Tenants/commit/ac21621b4ff94bee869e38df7751866b1d96031c))
* downgrade Microsoft.OpenApi package version to 2.9.0 ([72ad2ee](https://github.com/Hexalith/Hexalith.Tenants/commit/72ad2eed953c0f899465bcf60819cb3ad312119e))
* enhance package management and dependencies across projects ([bc33493](https://github.com/Hexalith/Hexalith.Tenants/commit/bc33493da445f4304cdd9aca72a974fb39d16f51))
* enhance project references and conditions for Hexalith dependencies in multiple projects ([bdccfbc](https://github.com/Hexalith/Hexalith.Tenants/commit/bdccfbc34cb62473465c64de1341a678960c42ca))
* Enhance tenant-domain correction path and audit page resilience ([ac3b8d5](https://github.com/Hexalith/Hexalith.Tenants/commit/ac3b8d5aa19b19c9c922ee58494f7e3cb539b2d7))
* implement dual-mode project references for Hexalith dependencies, ensuring source builds when submodules are present and fallback to NuGet packages otherwise ([cda1195](https://github.com/Hexalith/Hexalith.Tenants/commit/cda11955a446a9699c89484d20ad0e52b075f57c))
* Implement fail-closed guard for global-administrator correction pagination and document submodule changes ([0fd3f8d](https://github.com/Hexalith/Hexalith.Tenants/commit/0fd3f8d1abde2d2f67fc7ba6a6f93ae17c0cc9b4))
* Refactor CI workflow to use reusable domain pipeline and reduce complexity ([98c13f8](https://github.com/Hexalith/Hexalith.Tenants/commit/98c13f8520cc0aad30fba55e0be8dd711585fc19))
* Refactor Dapr installation in CI workflows to use shared composite action ([c13197d](https://github.com/Hexalith/Hexalith.Tenants/commit/c13197dd18f0c1907fd172d6678067fc12c3dadf))
* **tenants:** add sprint change proposal for Tenants module navigation consolidation and tabbed workspace ([0df740a](https://github.com/Hexalith/Hexalith.Tenants/commit/0df740aac6e793967adf6c1450d9a7388092c9b9))
* **tenants:** add X-Hexalith-Is-Stale header for freshness classification and update related tests ([ad7312b](https://github.com/Hexalith/Hexalith.Tenants/commit/ad7312b27f0a24861f965b9add603ec5d39084ea))
* **tenants:** enhance UI with search and status filters, update app title, and improve tenant ID display ([dc6cda8](https://github.com/Hexalith/Hexalith.Tenants/commit/dc6cda8b657922423ab944c9195a5a5cab06aa26))
* **tenants:** finalize tabbed workspace implementation and enhance user lookup functionality ([a789a42](https://github.com/Hexalith/Hexalith.Tenants/commit/a789a42e9467e22239e715a5ff1e644c621e2780))
* **tenants:** implement tabbed workspace navigation for Tenants module and update sprint status ([60d99ea](https://github.com/Hexalith/Hexalith.Tenants/commit/60d99ea950e011cdbe1dfb46471b548ae87af8f0))
* **tenants:** update icon for Tenants navigation and enhance UI tests for icon validation ([7000aea](https://github.com/Hexalith/Hexalith.Tenants/commit/7000aea46f3cbd254ac4253f626e907b0e48bf76))
* Update audit evidence and freshness documentation ([bd5fe8a](https://github.com/Hexalith/Hexalith.Tenants/commit/bd5fe8ade39f8a2212f1d869feb57db29d83fbd7))
* Update code quality and style rules in project context documentation ([671c282](https://github.com/Hexalith/Hexalith.Tenants/commit/671c282d183bc1111a16c88d9ee1761d361e4ce7))
* update conditions for UseHexalithProjectReferences and Hexalith.EventStore.ServiceDefaults package version ([62edda5](https://github.com/Hexalith/Hexalith.Tenants/commit/62edda51107e9ba954b43ebbf116e8ac2ba76558))
* update Dapr version to 1.18.0 in CI and Release workflows; add new workflows for CodeQL, Commitlint, and Dependency Review ([a989733](https://github.com/Hexalith/Hexalith.Tenants/commit/a989733e4e3a795e77ed9a74ab55abda8229a7b3))
* Update Hexalith package versions and add Hexalith.Commons.UniqueIds reference ([564d071](https://github.com/Hexalith/Hexalith.Tenants/commit/564d0717df434ddd97094085ed734686024c5ccd))
* Update Hexalith subproject references and add CI/CD specification for EventStore ([536f89d](https://github.com/Hexalith/Hexalith.Tenants/commit/536f89df0548e396d9c32809026f41055e0d3783))
* update Hexalith.EventStore and Hexalith.Memories package versions to 3.21.0 and 1.34.1 respectively ([5d3e255](https://github.com/Hexalith/Hexalith.Tenants/commit/5d3e255413f39b73f8ad379224f3eaa9a0dc28e9))
* Update Hexalith.EventStore subproject commit reference ([7a63890](https://github.com/Hexalith/Hexalith.Tenants/commit/7a63890bb73d2eeb56b9693051eb4c28ec940f85))
* Update package versions for ByteAether.Ulid, StackExchange.Redis, Microsoft.OpenApi, Microsoft.FluentUI.AspNetCore.Components, and YamlDotNet ([d97d5ca](https://github.com/Hexalith/Hexalith.Tenants/commit/d97d5cac97f6dce67b3623c0f875ebe450ce23e1))
* Update Story 5.8 to review status and enhance projection refresh logic ([02e4dfb](https://github.com/Hexalith/Hexalith.Tenants/commit/02e4dfb168d721182861b78df1a8f60f59750d5f))
* update subproject commit references for Hexalith components ([c554495](https://github.com/Hexalith/Hexalith.Tenants/commit/c55449532dcf715a5e956e1aaa7448ce43cc6686))
* Update subproject commits for Hexalith references ([3bcc783](https://github.com/Hexalith/Hexalith.Tenants/commit/3bcc783cd98fb2d9dedea3d903e798f610b2b779))
* update subproject references and configure AppTitle in settings for Hexalith Tenants UI ([fe21e8b](https://github.com/Hexalith/Hexalith.Tenants/commit/fe21e8b1a6c622959c397d6886ca068a725a38eb))
* Update tenant management UI components and tests for authorization and state handling ([2f12bd0](https://github.com/Hexalith/Hexalith.Tenants/commit/2f12bd080ff43c7c3a2e18f08bee024b06311583))

## [2.1.2](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.1.1...v2.1.2) (2026-06-26)


### Bug Fixes

* update OpenTelemetry and commitlint package versions ([a0c9728](https://github.com/Hexalith/Hexalith.Tenants/commit/a0c97287708282570f7a4eb7c9b1628dfb0b5e6c))

## [2.1.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.1.0...v2.1.1) (2026-06-26)


### Bug Fixes

* update Aspire.AppHost.Sdk version to 13.4.6 ([43f548c](https://github.com/Hexalith/Hexalith.Tenants/commit/43f548ca872d97d0dc671a7947cc48299f03dbf6))

# [2.1.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.0.3...v2.1.0) (2026-06-25)


### Bug Fixes

* **ui:** bump FrontComposer to route-heading keyboard focus-ring fix ([cbd39f3](https://github.com/Hexalith/Hexalith.Tenants/commit/cbd39f302000483eefb1ced15f13714eba151331))
* update Hexalith.AI.Tools subproject commit reference ([63a2f3d](https://github.com/Hexalith/Hexalith.Tenants/commit/63a2f3dc697271e6469c6713e33098a7b0f2894a))
* update Hexalith.FrontComposer subproject commit reference ([58fdaef](https://github.com/Hexalith/Hexalith.Tenants/commit/58fdaef696f77247c7a8efb29615557864d28b0b))
* update Hexalith.FrontComposer subproject commit reference ([84ba449](https://github.com/Hexalith/Hexalith.Tenants/commit/84ba449ab8ef6bbf2a22d480b868402f4a1ca75e))
* update Hexalith.FrontComposer subproject commit reference ([ff0ac7e](https://github.com/Hexalith/Hexalith.Tenants/commit/ff0ac7e3985b0a395745e3192e3b1ec4943eed05))
* update Hexalith.FrontComposer subproject commit reference ([7007e45](https://github.com/Hexalith/Hexalith.Tenants/commit/7007e4589fdf68d83a8f3a5147d7416b4489e86c))
* update subproject commit reference for Hexalith.EventStore ([5b8b572](https://github.com/Hexalith/Hexalith.Tenants/commit/5b8b572343d6d70054112c8782cbdee163c31107))
* update subproject commit references for Hexalith.EventStore and Hexalith.FrontComposer ([910c223](https://github.com/Hexalith/Hexalith.Tenants/commit/910c2232f7ec5b472e95ca55cb722b6ea998d24a))
* update subproject commit references for Hexalith.EventStore and Hexalith.Memories ([60a1cd1](https://github.com/Hexalith/Hexalith.Tenants/commit/60a1cd102b2618419533201357e3177180d8d223))
* update subproject references and YAML configurations for Memories integration ([fc1b40c](https://github.com/Hexalith/Hexalith.Tenants/commit/fc1b40cd6af0876af8238a8ecf39d53aa8962ec6))
* update subproject references to latest commits across multiple modules ([8f33562](https://github.com/Hexalith/Hexalith.Tenants/commit/8f33562e0aa3f80374f34402f0b4af9d6da34a44))


### Features

* adopt EventStore read-model freshness metadata in Tenants ([116d5af](https://github.com/Hexalith/Hexalith.Tenants/commit/116d5af72ed46dc706292f1102355c9ae164a468))
* **aspire:** reinstate Hexalith.Tenants.Aspire + consume Memories/Tenants AppHost helpers ([46a53bd](https://github.com/Hexalith/Hexalith.Tenants/commit/46a53bd91c863346701d52eb4ad79dcd21a15b20))
* enhance tenant query handlers with global admin claim support and add tests for authorization scenarios ([fcef6b8](https://github.com/Hexalith/Hexalith.Tenants/commit/fcef6b842e0db3c9f51be4fd7b150806196353ff))
* improve TenantsWorkspace ergonomics by removing duplicate navigation links and obsolete CSS ([ebec1db](https://github.com/Hexalith/Hexalith.Tenants/commit/ebec1dbbb90e2bccfe9a6bedb71783192b3da4f2))
* wrap CreateTenantFlow in a collapsed accordion to improve UI layout and avoid duplicate title ([b2c3a66](https://github.com/Hexalith/Hexalith.Tenants/commit/b2c3a66ab01fa9ceb24c1e0ebb3c52e77df3e668))

## [2.0.3](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.0.2...v2.0.3) (2026-06-22)


### Bug Fixes

* update subproject commit reference for Hexalith.EventStore ([50533f1](https://github.com/Hexalith/Hexalith.Tenants/commit/50533f14f507d3ea1a9f581ad0a3d569e2be651d))

## [2.0.2](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.0.1...v2.0.2) (2026-06-22)


### Bug Fixes

* update subproject commit reference for Hexalith.EventStore ([7b20767](https://github.com/Hexalith/Hexalith.Tenants/commit/7b207670e094fc59b59ab9e14e1ebd0e2df8d5a6))

## [2.0.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v2.0.0...v2.0.1) (2026-06-22)


### Bug Fixes

* update subproject commit reference for Hexalith.EventStore ([4f56d91](https://github.com/Hexalith/Hexalith.Tenants/commit/4f56d9163c2f5f865c7eb71bda276a6da63600d8))

# [2.0.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.15.0...v2.0.0) (2026-06-22)


* fix(contracts)!: fail-safe role/status enum defaults and consumer-contract hardening (TEN-1…5) ([#19](https://github.com/Hexalith/Hexalith.Tenants/issues/19)) ([f46264a](https://github.com/Hexalith/Hexalith.Tenants/commit/f46264a98d95ed239f7f2dde791f1ff30aa62848))


### Bug Fixes

* add placeholder text for role selection in AddTenantMemberFlow component ([81ccac6](https://github.com/Hexalith/Hexalith.Tenants/commit/81ccac6b61162dcfdbf7d7a084db93d6b856e553))
* ensure EventCallback is invoked on the correct Dispatcher to prevent circuit teardown ([c3935a5](https://github.com/Hexalith/Hexalith.Tenants/commit/c3935a54d1964aad52b23db4adf3f0f01e4a3105))
* **events:** align tenant event topic naming ([c09b42e](https://github.com/Hexalith/Hexalith.Tenants/commit/c09b42e80b167bf76d29a01a0c90dc0449a54c56))
* mark Hexalith.Memories subproject as dirty ([f4281a1](https://github.com/Hexalith/Hexalith.Tenants/commit/f4281a100468c90acc6b3a4fd9d7a3f14bc9579e))
* projection null safety, test coverage, deps update ([0155fe3](https://github.com/Hexalith/Hexalith.Tenants/commit/0155fe36378646edf5cc9d2f271c9ec8910c3e4f))
* **release:** align package dependency-boundary validator with actual published surface ([8cd4f46](https://github.com/Hexalith/Hexalith.Tenants/commit/8cd4f4671ec9d1118e31fbcede599d0b93b21c5b))
* **story-automator:** support letter-suffixed story ids ([9c6d976](https://github.com/Hexalith/Hexalith.Tenants/commit/9c6d97648b8213812831d8b7c76e1b392eb90c0c))
* **submodules:** pull EventStore topic-validator fix and bump FrontComposer ([060f1d1](https://github.com/Hexalith/Hexalith.Tenants/commit/060f1d1852cf442dd2383a4e0de01f6f0cf3959c))
* **tests:** exclude node_modules from container governance scan ([137c3ed](https://github.com/Hexalith/Hexalith.Tenants/commit/137c3ed80b5895388cf1200fa248a0bf5a68d3a5))
* **ui:** register IEventStoreGatewayClient for TenantCommandGateway ([94ff246](https://github.com/Hexalith/Hexalith.Tenants/commit/94ff2462871a155c095e93a07f123383ee98fbff))
* Update Hexalith.EventStore project reference to use HexalithEventStoreRoot variable ([038d845](https://github.com/Hexalith/Hexalith.Tenants/commit/038d845ffd844a70101dd40e11d017cba87c1b62))
* update last_updated timestamp in sprint-status.yaml and add init log for story automator ([d4aff69](https://github.com/Hexalith/Hexalith.Tenants/commit/d4aff694211983e34af4e1051d7e06dac69ab49b))
* update Redis configuration in statestore.yaml to use default values ([d6c7052](https://github.com/Hexalith/Hexalith.Tenants/commit/d6c7052917bbf0c0bed192b9b503dad514794039))
* update subproject commit and mark FrontComposer aggregate extraction status as done ([cd0579f](https://github.com/Hexalith/Hexalith.Tenants/commit/cd0579fa1fae1ecb820f876e4525159c3a5fbd5c))
* update subproject commit reference for Hexalith.EventStore ([7c00990](https://github.com/Hexalith/Hexalith.Tenants/commit/7c0099071246d0aafd5264444045a1c09ab09967))
* update subproject commits and change status to in-progress for FrontComposer aggregate extraction ([687f1e3](https://github.com/Hexalith/Hexalith.Tenants/commit/687f1e3084bbdadf7dfe62cce8dd1040348bbc96))
* update xUnit package versions to stable releases ([7ff1a90](https://github.com/Hexalith/Hexalith.Tenants/commit/7ff1a90856471712c9b9cf170106818f4c1c6087))


### Features

* add AdminOperationalIndexMetadata for operational index management ([0ca69d5](https://github.com/Hexalith/Hexalith.Tenants/commit/0ca69d5ad3bd901aa3ad4de5002d65de9dd2e254))
* add DAPR configuration store component for local development ([4d3dd87](https://github.com/Hexalith/Hexalith.Tenants/commit/4d3dd878b1f190c8ebf203e1d6e9c4465bae4f48))
* Add DAPR Pub/Sub component configuration for local development ([92bf113](https://github.com/Hexalith/Hexalith.Tenants/commit/92bf113c057f027098c70507ba81c528e116cb25))
* add Hexalith.Memories submodule and update README for initialization ([5427b15](https://github.com/Hexalith/Hexalith.Tenants/commit/5427b157b920351cd398eb6c47b7c4c5d7bd71f6))
* Add implementation readiness assessment report v2 for June 5 ([21f3e23](https://github.com/Hexalith/Hexalith.Tenants/commit/21f3e233668d224f5f7a733d9f18090ee7806660))
* Add initial log entry for story automator on June 5 ([d3e16f0](https://github.com/Hexalith/Hexalith.Tenants/commit/d3e16f07182f933b91e03b237de08f28f9034aa9))
* Add orchestration and complexity files for Hexalith.Tenants epic breakdown ([7310a54](https://github.com/Hexalith/Hexalith.Tenants/commit/7310a548f434645cafb809c9bce9946c31809eb8))
* Add preflight snapshot for story automator ([edc7f12](https://github.com/Hexalith/Hexalith.Tenants/commit/edc7f1286001e77c4788bcf4afd1e64598c34ec9))
* Add Sprint Change Proposal for Deferred Work Triage and Impact Analysis ([3856eeb](https://github.com/Hexalith/Hexalith.Tenants/commit/3856eeb2a40b62d6d4fe6169ff1ac74b9df2d06b))
* Add sprint change proposal for Fluent UI layout conformance and update sprint status ([974eac7](https://github.com/Hexalith/Hexalith.Tenants/commit/974eac7fe6b7b6bc2545c2c51adb170ed587482a))
* add sprint change proposal for reusable aggregate pages and Memories-backed tenant search ([46aabf6](https://github.com/Hexalith/Hexalith.Tenants/commit/46aabf632ca27191ae46f0fdeb734be47ff95cf4))
* Add sprint change proposal for shared domain-service infrastructure extraction ([d6c2584](https://github.com/Hexalith/Hexalith.Tenants/commit/d6c258412efe34434c1c0de0690447d35f66972a))
* Approve and apply Sprint Change Proposal for 2026-06-05 ([b440e64](https://github.com/Hexalith/Hexalith.Tenants/commit/b440e642df3cdfb26332b1ffea4ad3708e36cd1b))
* Archive old epic story artifacts and create orchestration documents ([43016d3](https://github.com/Hexalith/Hexalith.Tenants/commit/43016d344e0534281f50a68f0196317812fb323e))
* **dependencies:** update Aspire and Microsoft.Extensions package versions ([278fe2c](https://github.com/Hexalith/Hexalith.Tenants/commit/278fe2c33beeb5f83dfa061ef9714ee074ffa40a))
* enhance authentication flow by acquiring JWT for EventStore commands and update Keycloak realm configuration ([04a321b](https://github.com/Hexalith/Hexalith.Tenants/commit/04a321bd6f6f6abbd9851c4f20a38ed170b4e49b))
* Enhance auto-detection of Hexalith.FrontComposer location in Directory.Build.props ([96dc874](https://github.com/Hexalith/Hexalith.Tenants/commit/96dc87409441dd8e99ac3254ef4ca711764cd1f9))
* Enhance ETag handling and freshness validation in Tenant Query ([af909ad](https://github.com/Hexalith/Hexalith.Tenants/commit/af909ad4ee8193c1692574a36c64e4548ca3bb4b))
* Enhance tenant query functionality with ETag support and API client improvements ([600080c](https://github.com/Hexalith/Hexalith.Tenants/commit/600080ca9de72698b45762a6cca9ca2dd4034e1b))
* Enhance tenant search functionality with pagination and improved state management ([7ef796f](https://github.com/Hexalith/Hexalith.Tenants/commit/7ef796f0ef9546d32f375b9963e2be7c7636485a))
* Enhance TenantDetailPage with fallback for unnamed tenants and update localization resources ([c7da059](https://github.com/Hexalith/Hexalith.Tenants/commit/c7da05969802fdd429b322e96c496e33e5b4be26))
* **hooks:** add stop hook for story automator with command and timeout ([ecc5b25](https://github.com/Hexalith/Hexalith.Tenants/commit/ecc5b252e5c95bcc4f5627998aa23b8c7bd79064))
* Implement cross-cutting stories for tenant query freshness, ETag handling, and UI governance hardening ([f12db93](https://github.com/Hexalith/Hexalith.Tenants/commit/f12db931aafb01f2698d94f175e84728b51e6455))
* implement deferred and pending work (Correct Course 2026-06-21) ([62a94b0](https://github.com/Hexalith/Hexalith.Tenants/commit/62a94b02c6d21f271b7178328b301dbaf5d65ed2))
* Implement Memories Search-Index Ingestion for Tenant Search ([4273bbe](https://github.com/Hexalith/Hexalith.Tenants/commit/4273bbee7bb95d44d450917075496b3260a3b1f0))
* Integrate EventStore client and refactor tenant event handling ([ea20e58](https://github.com/Hexalith/Hexalith.Tenants/commit/ea20e588a393d20e558d98361730ac2210ee35f4))
* Reconcile fallback approvals and FrontComposer readiness for Tenants Management UI ([26c2c27](https://github.com/Hexalith/Hexalith.Tenants/commit/26c2c2787c50f5e3e6bbce9213e7626330c83663))
* **story-1.1:** Establish EventStore-Native Solution Structure ([fff8fda](https://github.com/Hexalith/Hexalith.Tenants/commit/fff8fda1dcca1d62ffd7c11704909aabffe0c3b8))
* **story-1.1:** Tenants UI Host Bootstrap ([b969cbe](https://github.com/Hexalith/Hexalith.Tenants/commit/b969cbebdcfd40eb4217246e21e8a9b43a628343))
* **story-1.2:** Configure Central Build and Package Governance ([76065d4](https://github.com/Hexalith/Hexalith.Tenants/commit/76065d4eb3f2bbe21ae2adbad88e35df6cadb0a1))
* **story-1.2:** Tenant List Triage ([3967f40](https://github.com/Hexalith/Hexalith.Tenants/commit/3967f4061c045f0cfe5ff8aa394439fd5267b7a8))
* **story-1.3:** Add CI Quality Gates for Build, Test, Coverage, and Package Validation ([6ce94b8](https://github.com/Hexalith/Hexalith.Tenants/commit/6ce94b8adc807cadc3a6686ac689cfa5ee59be23))
* **story-1.3:** Tenant Detail Navigation and Overview ([f28f789](https://github.com/Hexalith/Hexalith.Tenants/commit/f28f789cc4f57a507c2f09985e8900ef5f4ac482))
* **story-1.4:** My Tenants Self-Audit View ([3077c7e](https://github.com/Hexalith/Hexalith.Tenants/commit/3077c7efffae858031928a386962a062a0892f5e))
* **story-1.4:** Verify Consumer Package Reference Experience ([344ffa5](https://github.com/Hexalith/Hexalith.Tenants/commit/344ffa5f738a5da61bb40426f3be4ba9e2c6bcb6))
* **story-1.5:** User Membership Lookup ([4f059a1](https://github.com/Hexalith/Hexalith.Tenants/commit/4f059a1248dbc74d1bf7478cef203a7fcca17ba3))
* **story-1.6:** Read-Only Tenant Configuration View ([32366bc](https://github.com/Hexalith/Hexalith.Tenants/commit/32366bc2b74ba4f47e69a2b9ef98655187731960))
* **story-1.7:** Tenant Member Table and Action Availability ([bcb1911](https://github.com/Hexalith/Hexalith.Tenants/commit/bcb1911797bffb569b91fea033dd57f7b2071187))
* **story-1.8:** Support-Safe Identifier Copy and Epic 1 Readiness Evidence ([58ad9b3](https://github.com/Hexalith/Hexalith.Tenants/commit/58ad9b391fe19bac0a55b2a3e9ede956dfdc928e))
* **story-2.1:** Bootstrap the Initial Global Administrator ([e0c0a54](https://github.com/Hexalith/Hexalith.Tenants/commit/e0c0a54d7230c96b54bdb3e231400f3ccf0cc152))
* **story-2.1:** Create Tenant with Projection-Confirmed Command Lifecycle ([7c55231](https://github.com/Hexalith/Hexalith.Tenants/commit/7c5523101727b24cd989765c5d8e06c9a8704d19))
* **story-2.2:** Add User to Tenant with Explicit Role ([0c62e91](https://github.com/Hexalith/Hexalith.Tenants/commit/0c62e916b062eb5ab2db98ed7920be430c5ca1d9))
* **story-2.2:** enforce global-admin authorization on tenant lifecycle commands ([8819b71](https://github.com/Hexalith/Hexalith.Tenants/commit/8819b7180b96c728a7e7a4c4918ce5a448104958))
* **story-2.2:** Manage Global Administrator Assignments ([bddfda5](https://github.com/Hexalith/Hexalith.Tenants/commit/bddfda5cfd1ebdc02b7bb2fc5714cd13529b110c))
* **story-2.3:** authorize global administrators for cross-tenant governance ([1c58824](https://github.com/Hexalith/Hexalith.Tenants/commit/1c5882490d9c6f10b4f67557431bc5bce2fc39d6))
* **story-2.3:** Change Tenant Member Role ([1118f18](https://github.com/Hexalith/Hexalith.Tenants/commit/1118f18fdb756c6ea28112e71def836f5ea0c023))
* **story-2.4:** create and update tenants ([c996c3b](https://github.com/Hexalith/Hexalith.Tenants/commit/c996c3b883186d3eaa7f62be3ebfeb5d88b61a88))
* **story-2.4:** Remove Tenant Member with Consequence Preview ([6e4b4e8](https://github.com/Hexalith/Hexalith.Tenants/commit/6e4b4e8ad1d6ebc7fe34aa18e6ff03a2206eefdb))
* **story-2.5:** disable and re-enable tenants ([bd1e935](https://github.com/Hexalith/Hexalith.Tenants/commit/bd1e935a89d64a4aba146c544722a877056c85c4))
* **story-2.5:** Edit Tenant Metadata with Safe Validation ([f130cef](https://github.com/Hexalith/Hexalith.Tenants/commit/f130cef6498ba328c10bb02230964e1b5fd7007e))
* **story-2.6:** return structured tenant governance rejections ([fe6c361](https://github.com/Hexalith/Hexalith.Tenants/commit/fe6c36169e87a690a12e75fc69f9227a8658d421))
* **story-2.7:** preserve command source of truth when pub-sub is unavailable ([aad45de](https://github.com/Hexalith/Hexalith.Tenants/commit/aad45de4933e37228783509f8490796b2bbb267d))
* **story-3.1:** Add Users to a Tenant with Explicit Roles ([a00d490](https://github.com/Hexalith/Hexalith.Tenants/commit/a00d4907f0ee5d9d267f91fb4bb9e1c0b6ae444a))
* **story-3.1:** Tenant Lifecycle Command Availability and Blocked-State Guardrail ([a1aa2d4](https://github.com/Hexalith/Hexalith.Tenants/commit/a1aa2d40641ac76d5adc6a1ec5dea768c59d34f7))
* **story-3.2:** Disable or Enable Tenant with High-Impact Confirmation ([3cbdeab](https://github.com/Hexalith/Hexalith.Tenants/commit/3cbdeab78e228fd5bf28dcb84e0341b88d7c25e9)), closes [Hi#Impact](https://github.com/Hi/issues/Impact)
* **story-3.2:** Remove Users from a Tenant ([f807411](https://github.com/Hexalith/Hexalith.Tenants/commit/f8074110d82cbbbd87462b00c24df56350da8d23))
* **story-3.3:** add tenant configuration command foundations ([8e49477](https://github.com/Hexalith/Hexalith.Tenants/commit/8e49477499879c20389e36ad8152d0688c385a58))
* **story-3.3:** Change Tenant User Roles with Escalation Protection ([d6e5fc4](https://github.com/Hexalith/Hexalith.Tenants/commit/d6e5fc4bf23be1dda97a5c9d777e5c25e345abca))
* **story-3.3:** Set Tenant Configuration Key Value with Consequence Preview ([7443050](https://github.com/Hexalith/Hexalith.Tenants/commit/74430503e53c6f82678489813cc501f71d768e28))
* **story-3.4:** Enforce Tenant-Scoped Role Behavior ([47bb606](https://github.com/Hexalith/Hexalith.Tenants/commit/47bb606a1e95fb18e436b34396456aa5c83c0fdf))
* **story-3.4:** Remove Tenant Configuration Key with Consequence Preview ([f76e0b0](https://github.com/Hexalith/Hexalith.Tenants/commit/f76e0b069068013778f28d575abeda7cb08a25a4))
* **story-3.5:** Set Tenant Configuration Entries ([fec602a](https://github.com/Hexalith/Hexalith.Tenants/commit/fec602a9c4d5e520bd96e2e11141cbd94f402240))
* **story-3.6:** Remove Tenant Configuration Entries ([f26cfa2](https://github.com/Hexalith/Hexalith.Tenants/commit/f26cfa27b045203c787850e0ed7fffa4db9b77d3))
* **story-3.7:** Enforce Tenant Configuration Limits ([8db9c41](https://github.com/Hexalith/Hexalith.Tenants/commit/8db9c4148bf62e7795e37ed62c4a138006f10acb))
* **story-3.8:** Reject Conflicting Concurrent Tenant Modifications ([9a779f0](https://github.com/Hexalith/Hexalith.Tenants/commit/9a779f097b4bf30be7b8632736cda879a578b3de))
* **story-4.1:** Global Administrators Navigation and Read Contract Readiness ([76cea86](https://github.com/Hexalith/Hexalith.Tenants/commit/76cea863cdf8e8392b2077f8370a8bfae54b3e11))
* **story-4.1:** Publish Tenant Domain Events as CloudEvents ([557de8d](https://github.com/Hexalith/Hexalith.Tenants/commit/557de8d07a2d3a78cc0ea25395778ce0136feed1))
* **story-4.2:** Expose Consumer DI Registration for Tenant Client Services ([17879ed](https://github.com/Hexalith/Hexalith.Tenants/commit/17879eda01de5f053326cf52fb358fbfea052fce))
* **story-4.2:** Review Global Administrators from Fixed Aggregate ([dc5b899](https://github.com/Hexalith/Hexalith.Tenants/commit/dc5b899246837923c2018ed8e666874df5800cbc))
* **story-4.3:** grant global administrator with projection confirmation ([911627e](https://github.com/Hexalith/Hexalith.Tenants/commit/911627e485910e681c4c63d193ebdd35a0dad6cc))
* **story-4.3:** Grant Global Administrator with Projection Confirmation ([dcb8c41](https://github.com/Hexalith/Hexalith.Tenants/commit/dcb8c41ac215ea22b45e63c7ca46feecaa843280))
* **story-4.3:** Register Tenant Event Handlers in Under Twenty Lines ([a51c5bd](https://github.com/Hexalith/Hexalith.Tenants/commit/a51c5bd76e6fb8a2814e48962777a41be1300053))
* **story-4.4:** Build Local Consumer Projection from Tenant Events ([15e3d69](https://github.com/Hexalith/Hexalith.Tenants/commit/15e3d690f9731248341c912bd77f45668f95e805))
* **story-4.4:** Remove Global Administrator with Last-Admin Hard Stop ([2355e55](https://github.com/Hexalith/Hexalith.Tenants/commit/2355e552fd5b887eecd283b0103c5203ebc5242e))
* **story-4.5:** React to Tenant Access Lifecycle and Configuration Changes ([e396f0a](https://github.com/Hexalith/Hexalith.Tenants/commit/e396f0a91305b88c39c3abdeb87a28c4709114a3))
* **story-4.6:** Provide Idempotent Consumer Guidance and Sample Service ([7f2a89e](https://github.com/Hexalith/Hexalith.Tenants/commit/7f2a89e4a80ad5396a5213111d1efc0d815c062e))
* **story-5.10:** Query Tenant Access Audit History ([4f343d2](https://github.com/Hexalith/Hexalith.Tenants/commit/4f343d228e62003434ac9abc4527ae352942b9f5))
* **story-5.1:** Persist Per-Tenant Detail Projections Without Silent Write Loss ([9345bf0](https://github.com/Hexalith/Hexalith.Tenants/commit/9345bf0b4f633b4373cf062ed6db469fd2015702))
* **story-5.1:** Tenant Audit Trail DataGrid ([497a4ac](https://github.com/Hexalith/Hexalith.Tenants/commit/497a4ac449ab2c1f160c132c001151d881b0040a))
* **story-5.2:** Persist the Shared Tenant Index Projection Without Silent Write Loss ([7ddd400](https://github.com/Hexalith/Hexalith.Tenants/commit/7ddd400222a066ef14b121a574576b74b1f8a026))
* **story-5.2:** Scoped Audit Evidence Entry Points ([77bb935](https://github.com/Hexalith/Hexalith.Tenants/commit/77bb935ea1c04cf258077bb510d0dc0c50ca6c79))
* **story-5.3:** Persist the Tenant Audit Projection Without Silent Write Loss ([54376be](https://github.com/Hexalith/Hexalith.Tenants/commit/54376bed839c2e7d31dfc698b3e0a4c8e36f6df8))
* **story-5.3:** Support-Safe Audit Evidence Receipt ([a5ca6e3](https://github.com/Hexalith/Hexalith.Tenants/commit/a5ca6e3886bf0623a1ff8da8cdfd6fade8516652))
* **story-5.4:** Audit Availability State Recovery ([8a24128](https://github.com/Hexalith/Hexalith.Tenants/commit/8a24128989e348768173875ffb3c38bbd7461657))
* **story-5.4:** Expose Projection Write Conflict Diagnostics and Recovery Evidence ([419989e](https://github.com/Hexalith/Hexalith.Tenants/commit/419989ee8a1d3a27ec27ab303dcfc942e483da72))
* **story-5.5:** Enforce Query-Side Authorization and Isolation ([f94fc36](https://github.com/Hexalith/Hexalith.Tenants/commit/f94fc366c219b561c101f1e93859584f97ce017b))
* **story-5.5:** Start Forward Correction from Audit Evidence ([eeb0a49](https://github.com/Hexalith/Hexalith.Tenants/commit/eeb0a49d87bc39b9582e1c1680340e9a05e881d0))
* **story-5.6:** Preview and Confirm Correction with Linked Proof ([d62aeee](https://github.com/Hexalith/Hexalith.Tenants/commit/d62aeee4429c5d01b5993a67743308edfd8b3325))
* **story-5.6:** Provide Safe Cursor-Based Pagination for Query Endpoints ([c468d7b](https://github.com/Hexalith/Hexalith.Tenants/commit/c468d7bd2ab148a854be3513abe4be2bd8b83de7))
* **story-5.7:** Query a Paginated Tenant List ([6e2b87a](https://github.com/Hexalith/Hexalith.Tenants/commit/6e2b87ab25a9ecc04139fe4dd956683cf3bf6bc5))
* **story-5.8:** Implement query for tenant details and users ([b766ca8](https://github.com/Hexalith/Hexalith.Tenants/commit/b766ca82b083ac5570c7c14bf66178be0ccd4a1b))
* **story-5.8:** Query Tenant Details and Tenant Users ([451abc9](https://github.com/Hexalith/Hexalith.Tenants/commit/451abc991c4871df6fe2de93b4827b11bcacdece))
* **story-5.9:** Query the Tenants a User Belongs To ([42ae2f1](https://github.com/Hexalith/Hexalith.Tenants/commit/42ae2f1639d46452dfa33b2b0dcc9431c4721f3a))
* **story-6.1:** Provide In-Memory Tenant Test Fakes ([23990aa](https://github.com/Hexalith/Hexalith.Tenants/commit/23990aa8ba94f9789bd3bda208cc6194e0613ba7))
* **story-6.2:** Reuse Production Aggregate Logic in Testing Fakes ([8a1e60e](https://github.com/Hexalith/Hexalith.Tenants/commit/8a1e60ede91b0c98a1becf8efa5b8e945997f79f))
* **story-6.3:** Add Production/Fake Conformance Tests ([3be7ce3](https://github.com/Hexalith/Hexalith.Tenants/commit/3be7ce368cff9795e6f3ff2f766bf66c8e1ce991))
* **story-6.4:** Support Consumer Tenant Isolation Tests ([f28d816](https://github.com/Hexalith/Hexalith.Tenants/commit/f28d816aece38293fb9fb22b70c153645d419450))
* **story-7.1:** Provide Aspire Hosting Extensions for Tenants ([25d53a3](https://github.com/Hexalith/Hexalith.Tenants/commit/25d53a3b037fa5a68bed43796f6e04ae7fd49ec6))
* **story-7.2:** Configure DAPR Components for Local and Production Deployment ([7f33bb4](https://github.com/Hexalith/Hexalith.Tenants/commit/7f33bb4dd43b6eeab949b5398c0198c9db576a4b))
* **story-7.3:** Validate Production Authentication and EventStore Tenant Claims ([0f94de4](https://github.com/Hexalith/Hexalith.Tenants/commit/0f94de438d2200265b72823068734c8aea11ce28))
* **story-7.4:** Expose Tenant Command and Event Metrics with OpenTelemetry ([d28e799](https://github.com/Hexalith/Hexalith.Tenants/commit/d28e799ded04570b99e9232b4b1e809cfc891204))
* **story-7.5:** Prove Stateless Operation, Health, and Startup Reconstruction ([11a32f8](https://github.com/Hexalith/Hexalith.Tenants/commit/11a32f81d94fc3cf81a729de22b658cc2e3c2ed3))
* **story-7.6A:** Validate Production Auth Smoke Tests ([4db3ca7](https://github.com/Hexalith/Hexalith.Tenants/commit/4db3ca787d9882414b7cbcce96ea74f9cf015ebc))
* **story-7.6B:** Validate DAPR Component and Service Invocation Smoke Tests ([d20a990](https://github.com/Hexalith/Hexalith.Tenants/commit/d20a99077056d9ad9f4b8463b1cfe2f3e63e5540))
* **story-7.6C:** Validate Health and Dependency Readiness Smoke Tests ([910fa23](https://github.com/Hexalith/Hexalith.Tenants/commit/910fa23821a0fa5c6279c8bf871f2ad5005c1a73))
* **story-7.6D:** Validate Pub/Sub Recovery and Catch-Up Evidence ([de520a9](https://github.com/Hexalith/Hexalith.Tenants/commit/de520a989a2067eefc5f4951f0b1f929a7137ac1))
* **story-7.6E:** Publish the Deployment Readiness Checklist and Evidence Template ([8e76465](https://github.com/Hexalith/Hexalith.Tenants/commit/8e76465bd7522a28c60d6a6452d7fc65e84bf11c))
* **story-8.1:** Create a Prerequisite-Validated Quickstart ([6c5e1f7](https://github.com/Hexalith/Hexalith.Tenants/commit/6c5e1f7515e21fd786eaaadb0638fdddf34000d1))
* **story-8.2:** Publish the Event Contract Reference ([6541bdd](https://github.com/Hexalith/Hexalith.Tenants/commit/6541bddf3e63d7d74d3176d1f3797d242477d777))
* **story-8.3:** Document the Sample Consuming Service Walkthrough ([54384f8](https://github.com/Hexalith/Hexalith.Tenants/commit/54384f8dfe330426000554161a47d9c3175f9a0e))
* **story-8.4:** Produce the Reactive Access Aha Moment Demo ([c0b3abd](https://github.com/Hexalith/Hexalith.Tenants/commit/c0b3abd1763661221b58ab250a67015a14606625))
* **story-8.5:** Document Cross-Aggregate Timing and Eventual Consistency ([1b1ac98](https://github.com/Hexalith/Hexalith.Tenants/commit/1b1ac9897a057387b642a5bb13cfa7f0d55763e2))
* **story-8.6:** Document Compensating Command Patterns ([19e31f2](https://github.com/Hexalith/Hexalith.Tenants/commit/19e31f21166e277c8a01b644c6313e93c2ccc41d))
* **story-9.1:** Map Fluent UI and FrontComposer Dependencies for Tenant Admin Screens ([91f16d1](https://github.com/Hexalith/Hexalith.Tenants/commit/91f16d188f25746134fb96d824d4d92ef890feb4))
* **story-9.2:** Specify the Operations Shell and Read-Only Access Review Surfaces ([00abb6c](https://github.com/Hexalith/Hexalith.Tenants/commit/00abb6c93a8580176118a4e3c9a6d567f6da4dc8))
* **story-9.3:** Define Truth State Freshness and Unavailable Action Patterns ([1d34fbc](https://github.com/Hexalith/Hexalith.Tenants/commit/1d34fbc0421f0a5623838f70d328853a282ec666))
* **story-9.4:** Specify the RemoveUserFromTenant Command Capable Journey ([cc44239](https://github.com/Hexalith/Hexalith.Tenants/commit/cc442399689898c3aea2b3f89f7562396da2177d))
* **story-9.5:** Specify Audit Evidence and Compensating Recovery UI Patterns ([abefeaf](https://github.com/Hexalith/Hexalith.Tenants/commit/abefeaf62bfae10c91f1d04477385024ed01b7a2))
* **story-9.6:** Specify Responsive Operational Layout and Visual System Usage ([440dc74](https://github.com/Hexalith/Hexalith.Tenants/commit/440dc7459a31412553c6910d192aa2ddd03b37a6))
* **story-9.7:** Define Accessibility Localization and UI Acceptance Evidence ([8624680](https://github.com/Hexalith/Hexalith.Tenants/commit/862468070eb2aa56a9c1a6ee2d1a22aba743eed7))
* **tests:** Add behavioral coverage for UnavailableTenantQueryGateway with new test cases ([ed82ef3](https://github.com/Hexalith/Hexalith.Tenants/commit/ed82ef30f081502d523bbaa5071509e6fd8fc970))
* **tests:** add global admin extension handling in integration tests and telemetry ([9240d0c](https://github.com/Hexalith/Hexalith.Tenants/commit/9240d0cf67660bab0edf6e76faa9b4f50882c8cd))
* **tests:** add null checks for configuration and service parameters in TenantsDaprTestFixture ([96b8da0](https://github.com/Hexalith/Hexalith.Tenants/commit/96b8da0dfc17d0558b9076a452caca84ddf9c687))
* **tests:** extract DAPR/Aspire test harness to Hexalith.EventStore.Testing.Integration ([107da73](https://github.com/Hexalith/Hexalith.Tenants/commit/107da73e3bb18734b4b81c5cf73df2d8aceb7163))
* **ui:** wire per-user Keycloak sign-in and EventStore token relay ([96c2a2c](https://github.com/Hexalith/Hexalith.Tenants/commit/96c2a2cbe82da66d455d61c623e1554e7801e866))
* Update DAPR deployment documentation and configuration for tenant event handling ([f1cc6ba](https://github.com/Hexalith/Hexalith.Tenants/commit/f1cc6ba4aeb3e2cd252bef15670f7effb32febbd))
* Update sprint status and add FrontComposer aggregate list/detail extraction story ([bab04f6](https://github.com/Hexalith/Hexalith.Tenants/commit/bab04f67f9acf86da57e93830f1e995fc0a8317f))


### BREAKING CHANGES

* enum wire format changes from integer to name and role/status ordinals shift (Unknown=0). Pre-v1.0; consumers must deserialize enums by name.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>

* test: add enum fail-safe, casing, and projection conformance tests

EnumFailSafeTests (missing field to Unknown, bad name to JsonException, by-name serialization), TenantLocalStateCasingTests (Ordinal membership keys, Unknown status default), InMemoryTenantProjectionConformanceTests (drift guard for unwired success events), and Unknown-role rejection tests in TenantAggregateTests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>

* docs: document enum sentinel, casing contract, and fail-closed handling

event-contract-reference: enums serialize by name with an Unknown sentinel; replace the prior 'treat unknown roles as TenantReader' guidance with fail-closed handling. production-auth-claim-contract: add the Identifier Casing Contract section.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>

# [1.15.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.14.0...v1.15.0) (2026-05-19)


### Features

* Update sprint status and implement projection write conformance tests ([3e27ab6](https://github.com/Hexalith/Hexalith.Tenants/commit/3e27ab658a71a649cd711a6a15e08ce5e7bb029c))

# [1.14.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.13.0...v1.14.0) (2026-05-19)


### Features

* **cancellation:** implement cancellation token checks in tenant projection queries and update related tests ([095be3b](https://github.com/Hexalith/Hexalith.Tenants/commit/095be3b7b0e18822231c73f2d6418490d7cee10e))

# [1.13.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.12.0...v1.13.0) (2026-05-18)


### Features

* Update subproject commits and finalize Story 9.4 with code review findings ([9a5f4e4](https://github.com/Hexalith/Hexalith.Tenants/commit/9a5f4e43f58336a1c4c1f12f4e7f2c5baaeb20d4))

# [1.12.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.11.0...v1.12.0) (2026-05-18)


### Features

* Upgrade .NET SDK to 10.0.300 and refactor pagination handling ([8960a91](https://github.com/Hexalith/Hexalith.Tenants/commit/8960a91be61f0e288aab26e76138870eadd9d5dc))

# [1.11.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.10.0...v1.11.0) (2026-05-17)


### Features

* finalize query policy for disabled tenants and orphan memberships; apply review findings and update status to done ([92604f9](https://github.com/Hexalith/Hexalith.Tenants/commit/92604f99ff224e9cb6b652e50377afde9b91f6f0))

# [1.10.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.9.0...v1.10.0) (2026-05-17)


### Features

* create EventStore projection cancellation API prerequisite story context ([6bef5c9](https://github.com/Hexalith/Hexalith.Tenants/commit/6bef5c99842278acc1512c76ffffafe4d2920d33))
* implement query policy for disabled tenants and orphan memberships ([3f9e368](https://github.com/Hexalith/Hexalith.Tenants/commit/3f9e368499585e7d51dfacf3fe3df7aba7e9d30e))

# [1.9.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.8.0...v1.9.0) (2026-05-17)


### Features

* Update sprint status and implement audit projection write safety ([a2010bf](https://github.com/Hexalith/Hexalith.Tenants/commit/a2010bf084d107c9e56b3b4b25e608559c72a4bb))

# [1.8.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.7.3...v1.8.0) (2026-05-17)


### Features

* Implement tenant query cursor encoding and validation ([107edaa](https://github.com/Hexalith/Hexalith.Tenants/commit/107edaade2217c6cca1c3e14ed202a637fe2aaf4))

## [1.7.3](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.7.2...v1.7.3) (2026-05-17)


### Bug Fixes

* update Aspire package versions and preflight results ([f0cb359](https://github.com/Hexalith/Hexalith.Tenants/commit/f0cb3596e3129a83dca1189dfc69a0b615473c50))

## [1.7.2](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.7.1...v1.7.2) (2026-05-17)


### Bug Fixes

* **preflight:** update pre-dev preflight results to reflect successful checks ([68fdae6](https://github.com/Hexalith/Hexalith.Tenants/commit/68fdae60bfc03fce44532b9ab9ae20a6a0c200d0))

## [1.7.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.7.0...v1.7.1) (2026-05-17)


### Bug Fixes

* **bmad:** restore missing completed story artifacts ([3fdac06](https://github.com/Hexalith/Hexalith.Tenants/commit/3fdac06e91fd52ee25b62a86f9e25959ccaf14f9))

# [1.7.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.6.0...v1.7.0) (2026-05-16)


### Features

* **preflight:** update preflight results and add new checks for code review ([5506e60](https://github.com/Hexalith/Hexalith.Tenants/commit/5506e604d426284b0f6c0dee0012cdb8c84c9f7b))

# [1.6.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.5.0...v1.6.0) (2026-05-16)


### Features

* Implement stable cursor pagination under role and membership changes ([fdc6e9e](https://github.com/Hexalith/Hexalith.Tenants/commit/fdc6e9e1152f96cec2a53cebbb244a505332f004))

# [1.5.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.4.1...v1.5.0) (2026-05-16)


### Features

* **preflight:** add predev preflight results for 2026-05-16 ([f43dc69](https://github.com/Hexalith/Hexalith.Tenants/commit/f43dc69243ee6d7129ccf7cea3eb693cc44098bd))

## [1.4.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.4.0...v1.4.1) (2026-05-16)


### Bug Fixes

* **server:** harden tenant audit projection queries ([d625185](https://github.com/Hexalith/Hexalith.Tenants/commit/d6251857dcf7f7c245cfb430c49aaedf7d4ee3c2))

# [1.4.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.3.0...v1.4.0) (2026-05-14)


### Features

* **audit:** Implement tenant audit functionality with filtering and pagination ([32bb865](https://github.com/Hexalith/Hexalith.Tenants/commit/32bb865fb38f4de65c3315b12f3c1632e38dd6fc))

# [1.3.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.2.1...v1.3.0) (2026-05-14)


### Features

* add Hexalith.AI.Tools and Hexalith.FrontComposer submodules ([a048709](https://github.com/Hexalith/Hexalith.Tenants/commit/a048709b246b3005393d0dc876d9b6b238f77281))
* add predev preflight output files for tracking hardening results ([cd27f5a](https://github.com/Hexalith/Hexalith.Tenants/commit/cd27f5ab189184df8b35e61dfce175130ab3dd75))
* Add Sprint Change Proposal for Implementation Readiness Alignment ([7ae697b](https://github.com/Hexalith/Hexalith.Tenants/commit/7ae697ba8175e7252a1b059233ccb58c68734f2a))
* finalize GetUserTenants scoped authorization with timing-uniformity patch and update related tests ([938d370](https://github.com/Hexalith/Hexalith.Tenants/commit/938d370bae64acc0c9995bb95430914f3ea0a050))
* implement GetUserTenants scoped authorization and update sprint status ([7232151](https://github.com/Hexalith/Hexalith.Tenants/commit/7232151d33adb2345bc52b6e351e6a1a3c25286b))
* implement Tenant Audit Projection and Query with updated acceptance criteria and tasks ([a916c86](https://github.com/Hexalith/Hexalith.Tenants/commit/a916c862cb04391d0c63914802056c88e3df1662))
* implement TenantOwner scoped filtering in GetUserTenantsQuery and update related tests ([a968729](https://github.com/Hexalith/Hexalith.Tenants/commit/a968729c16e54de19b593767bc0f976ae1044a61))
* update predev preflight output with latest results and add new preflight file ([a7fd29c](https://github.com/Hexalith/Hexalith.Tenants/commit/a7fd29c80d949e4b4a0eca94476a9edbc7f2c61e))

## [1.2.1](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.2.0...v1.2.1) (2026-05-13)


### Bug Fixes

* add missing query contract import in TenantsProjectionActor ([e2b7cbb](https://github.com/Hexalith/Hexalith.Tenants/commit/e2b7cbb449a35298142ad5ef698b5b5c459dcd77))

# [1.2.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.1.0...v1.2.0) (2026-05-13)


### Features

* **tenants:** wire JWT bearer authentication and harden release pipeline ([92e9307](https://github.com/Hexalith/Hexalith.Tenants/commit/92e9307c39ea40d0afa35ae81b59fa452485c485))

# [1.1.0](https://github.com/Hexalith/Hexalith.Tenants/compare/v1.0.0...v1.1.0) (2026-05-12)


### Bug Fixes

* **domain-processing:** match MissingApplyMethodException for processor fall-through ([0fecb6c](https://github.com/Hexalith/Hexalith.Tenants/commit/0fecb6c051399f9cf7c724d713c9a0f2b4678e72))
* handle replayed tenant rejection events ([1609a57](https://github.com/Hexalith/Hexalith.Tenants/commit/1609a579bfe44e7dba233e9774efe5be8c459bc0))
* **projections:** add live global-administrator projection handler and /project domain dispatch ([ac63d48](https://github.com/Hexalith/Hexalith.Tenants/commit/ac63d483553627bef1cc4f09f239b917b49f942d))
* **projections:** harden global admin projection dispatch ([5469a6b](https://github.com/Hexalith/Hexalith.Tenants/commit/5469a6b74e87944c23e3bbeeb5928646b187301e))
* **tenants:** defer bootstrap until host started and handle idempotent conflict ([c7a8365](https://github.com/Hexalith/Hexalith.Tenants/commit/c7a83657a6dec441307462d5dfd5456cef8b554b))
* **tests:** shorten drain delays to stabilise DrainRecovery test ([6e85635](https://github.com/Hexalith/Hexalith.Tenants/commit/6e856356f2766f6748194d9a7b6eaac29655eba0))
* update subproject commit reference in Hexalith.EventStore ([0a74c4d](https://github.com/Hexalith/Hexalith.Tenants/commit/0a74c4d3c3ac64db23fcbe3dcf7d408b793c79e4))


### Features

* add post-epic-1 foundation readiness gates and update sprint status ([05c9d77](https://github.com/Hexalith/Hexalith.Tenants/commit/05c9d7753163d737a3fc1213e625deea3cfe6207))

# 1.0.0 (2026-04-20)


### Bug Fixes

* **apphost:** add EventStore submodule projects to solution for VS debug ([#8](https://github.com/Hexalith/Hexalith.Tenants/issues/8)) ([1f0def0](https://github.com/Hexalith/Hexalith.Tenants/commit/1f0def09a4ed89c8f7341bf168d48d0df0392b0d))
* **apphost:** use builder.AppHostDirectory for DAPR config resolution ([#11](https://github.com/Hexalith/Hexalith.Tenants/issues/11)) ([f5a3d4e](https://github.com/Hexalith/Hexalith.Tenants/commit/f5a3d4e9935c4e927271ecd9699a1e32981a76b5))
* **bootstrap:** prevent host crash on DAPR actor timeout during bootstrap ([238d242](https://github.com/Hexalith/Hexalith.Tenants/commit/238d24281c94f1ae63b6732227dd71e38b2bf59e))
* **build:** update EventStore submodule with dynamic Tenants path resolution ([7ccb6e7](https://github.com/Hexalith/Hexalith.Tenants/commit/7ccb6e7d136193e32aed7c663447ba4dc2727e22))
* **ci:** add missing build step to aspire-tests job ([#10](https://github.com/Hexalith/Hexalith.Tenants/issues/10)) ([b06d6ec](https://github.com/Hexalith/Hexalith.Tenants/commit/b06d6ec2f189f3a001fd8d90892a664e3e36cb38))
* **ci:** add package-lock.json for npm ci in release workflow ([16eeaa3](https://github.com/Hexalith/Hexalith.Tenants/commit/16eeaa3faae9ba089c5e0dfd5c0f1c73a495ddfe))
* **ci:** remove Dapr-dependent integration tests from release workflow ([d9f26ca](https://github.com/Hexalith/Hexalith.Tenants/commit/d9f26ca659a08e279f0e23d8b8ea5c03e52ad630))
* **ci:** update EventStore submodule to valid remote commit ([#13](https://github.com/Hexalith/Hexalith.Tenants/issues/13)) ([d3a222e](https://github.com/Hexalith/Hexalith.Tenants/commit/d3a222ed25bd0dea036114774eae78590ef1b43b))
* **ci:** update EventStore submodule with IHttpClientFactory registration ([#15](https://github.com/Hexalith/Hexalith.Tenants/issues/15)) ([3179376](https://github.com/Hexalith/Hexalith.Tenants/commit/3179376139a1879e139d7d83af619b1ca1d61463)), closes [Hexalith/Hexalith.EventStore#191](https://github.com/Hexalith/Hexalith.EventStore/issues/191)
* **ci:** use Dapr CLI 1.17.1 (1.17.4 does not exist) ([8687d6e](https://github.com/Hexalith/Hexalith.Tenants/commit/8687d6ebfdc96cae015d8f8f9a4f24beee08ceb4))
* Code review fixes for Story 3.2 Role Behavior Enforcement ([0e55463](https://github.com/Hexalith/Hexalith.Tenants/commit/0e5546394d75a2c94960917b4130b18d872020fb)), closes [#5](https://github.com/Hexalith/Hexalith.Tenants/issues/5)
* **release:** remove unused prerelease NSubstitute dependency from Testing library ([eb90fb9](https://github.com/Hexalith/Hexalith.Tenants/commit/eb90fb90371c826ac490f3847331a3a3691b961b))
* **server:** register MediatR pipeline, exception handlers, and RBAC extensions ([5b523fc](https://github.com/Hexalith/Hexalith.Tenants/commit/5b523fc6b351026cf17611c99a293d9f442da7b9))
* **server:** restore RBAC extensions and register EventStore controllers ([8f3790f](https://github.com/Hexalith/Hexalith.Tenants/commit/8f3790f6707b2712df808e24cfc8c73d585d639f)), closes [#1](https://github.com/Hexalith/Hexalith.Tenants/issues/1)
* **server:** use public setters on state and projection models for JSON deserialization ([ed0c823](https://github.com/Hexalith/Hexalith.Tenants/commit/ed0c82308fa5a1910e54cad59999208041f4af7b))
* **tenants:** remove server pipeline to fix tenant creation deadlock ([e6189e5](https://github.com/Hexalith/Hexalith.Tenants/commit/e6189e5e34ffe036a53d5ed0cdd4c1890fec6701))
* **tests:** add xUnit1051 to NoWarn property in test projects ([c83c35e](https://github.com/Hexalith/Hexalith.Tenants/commit/c83c35ebeb46f2d22fa8af39834e355543a23d9c))
* **tests:** update bootstrap tests to match HTTP-based implementation ([#14](https://github.com/Hexalith/Hexalith.Tenants/issues/14)) ([c367508](https://github.com/Hexalith/Hexalith.Tenants/commit/c3675083cb4903d257a9636de2facb9386e7e0e1))
* update task verification and documentation for smoke tests in Story 1.1 ([b7a82d1](https://github.com/Hexalith/Hexalith.Tenants/commit/b7a82d1a2c276a494005a0949ee538a26e3cdcc0))


### Features

* **actors:** introduce TenantProjectionRouting for actor type name and update references ([c0cf6d7](https://github.com/Hexalith/Hexalith.Tenants/commit/c0cf6d75d3303320ef1ea541512da7eb7a4126e6))
* Add DAPR end-to-end tests and fixtures for tenant management ([b4a3f50](https://github.com/Hexalith/Hexalith.Tenants/commit/b4a3f5018dcfe61c93b9e2014e39b3a03c2c59cd))
* Add design decisions and assumptions for tenant projections ([968791d](https://github.com/Hexalith/Hexalith.Tenants/commit/968791da06f1784dbfced8133811a2448f774261))
* add EventStore Admin Server and UI to Aspire topology with access control configuration ([037621d](https://github.com/Hexalith/Hexalith.Tenants/commit/037621d01d2c55b18717773a73e0df89f039ee4d))
* add initial MCP server configuration for Aspire ([c5da95e](https://github.com/Hexalith/Hexalith.Tenants/commit/c5da95e97bc9010ffc2940b5ebc4886437835fcb))
* Add InsufficientPermissionsRejection event for handling permission rejections ([79584b5](https://github.com/Hexalith/Hexalith.Tenants/commit/79584b581f96718182afc17dccc3447e274ef9ce))
* Add projections and read models for global administrators and tenants ([751e496](https://github.com/Hexalith/Hexalith.Tenants/commit/751e496f8db71be45cdefd145366af71a6434489))
* Add Sprint Change Proposals for EventStore alignment and research findings ([e9645ca](https://github.com/Hexalith/Hexalith.Tenants/commit/e9645ca1078a2c80d723ea7475c0719ed0bd9560))
* Add tenant configuration management story with validation and command handling ([45ec965](https://github.com/Hexalith/Hexalith.Tenants/commit/45ec965925357a3f35be200cab9d26f36502db0a))
* add tenant projections and switch to redis state ([e03c5b5](https://github.com/Hexalith/Hexalith.Tenants/commit/e03c5b55c922e7f185c36e4898f3c681e98c3a9f))
* Add UX design specification and amend architecture with UX-driven decisions ([3afbe30](https://github.com/Hexalith/Hexalith.Tenants/commit/3afbe30e1856f27998b2ba14ff09484cffd24c60))
* **ci:** replace MinVer with semantic-release for automated versioning and changelog generation ([c520911](https://github.com/Hexalith/Hexalith.Tenants/commit/c520911d65e2d4bfae727fcd5e2bd69a6797bd3e))
* **container:** enable .NET SDK container support ([#16](https://github.com/Hexalith/Hexalith.Tenants/issues/16)) ([94d5eb7](https://github.com/Hexalith/Hexalith.Tenants/commit/94d5eb755bed8ad65117bccd0e8b1da3eaf613c1))
* **core:** modernize startup, telemetry, and tests ([1cf8a37](https://github.com/Hexalith/Hexalith.Tenants/commit/1cf8a3788748692b5ce78d286f579b30b5fdf70a))
* Enhance tenant configuration management with null guards and additional boundary tests ([f9f9279](https://github.com/Hexalith/Hexalith.Tenants/commit/f9f927960a147ccdea2f592670eebe197bd8c979))
* Enhance user-role management with conditional validator tasks and updated test cases ([dd4a9d4](https://github.com/Hexalith/Hexalith.Tenants/commit/dd4a9d4b05c5476f16853b85f3c95386e6ee1f31))
* Finalize CommandApi Bootstrap & Event Publishing implementation with review resolutions and configuration updates ([fd1b5d9](https://github.com/Hexalith/Hexalith.Tenants/commit/fd1b5d99a08835a4a8d2e561f95ccd4c50b469ec))
* Implement cross-tenant index projection with read model and entry classes, including unit tests ([c60c942](https://github.com/Hexalith/Hexalith.Tenants/commit/c60c9429a08eee85b7cb2334fa2b07bdd5669c7d))
* Implement RBAC for tenant management commands ([4216ccd](https://github.com/Hexalith/Hexalith.Tenants/commit/4216ccd68001874e4a38bd17ed80d79a1f375130))
* Implement Story 7.1 - Add Sample to AppHost Aspire topology and create smoke tests ([e09dbea](https://github.com/Hexalith/Hexalith.Tenants/commit/e09dbeab083e869f44d931b49b42e78a9f5d0e6f))
* Implement tenant configuration management with DI registration and unit tests ([33ab49e](https://github.com/Hexalith/Hexalith.Tenants/commit/33ab49ed0a40b7cf228c02aacc0a9e4e9dea9846))
* Implement tenant configuration management with validation and RBAC support ([9753e09](https://github.com/Hexalith/Hexalith.Tenants/commit/9753e098fffc999eead8b6195536eb25a048c828))
* Implement tenant event handling and projection management ([04de61f](https://github.com/Hexalith/Hexalith.Tenants/commit/04de61f5a4e5a5729812a0560ff812b00c8e8d1e))
* Implement user-role management in TenantAggregate ([fc66d2a](https://github.com/Hexalith/Hexalith.Tenants/commit/fc66d2ac2ab43ddab462daff90f412414546212a))
* Introduce rejection events pattern and related command/event structures ([2f57512](https://github.com/Hexalith/Hexalith.Tenants/commit/2f57512393c711095b6cdbf15ea0da1d8383fc42))
* Refactor QueryResult creation in TenantsProjectionActor and add launchSettings.json ([2beb009](https://github.com/Hexalith/Hexalith.Tenants/commit/2beb009c7c66c82e88847f4c3e8abe8fc9fae58a))
* **status:** Update status of Story 7.2 to done after review completion ([eb627fd](https://github.com/Hexalith/Hexalith.Tenants/commit/eb627fd362be12c7c73565e82b2855bfc70d85ef))
* **telemetry:** Enhance telemetry for command processing and query execution ([2a01463](https://github.com/Hexalith/Hexalith.Tenants/commit/2a0146310977e2fb54b62b5ab2b75d235cb71a32))
* **tests:** Add InMemoryTenantService and TenantTestHelpers for integration testing ([3e4ef10](https://github.com/Hexalith/Hexalith.Tenants/commit/3e4ef108d838c3b14916c2bed0bd9e9465935e1b))
* update appHostPath in settings.json and mark submodules as dirty ([6deb175](https://github.com/Hexalith/Hexalith.Tenants/commit/6deb175db47bbe13cf21c1b8b595e98d3ec40dcc))
* Update sprint status and enhance domain processing error handling ([3c954c3](https://github.com/Hexalith/Hexalith.Tenants/commit/3c954c33df72c2c946858829af27e6f5998cd75c))
* Update sprint status to reflect completed epics and add changelog ([f44dfda](https://github.com/Hexalith/Hexalith.Tenants/commit/f44dfdaab5f7ac45b0782fc55f4f7bfe83976ad0))
* Update Story 2.4 status to review and refine acceptance criteria ([f7a03c5](https://github.com/Hexalith/Hexalith.Tenants/commit/f7a03c5efd5b1d57741f9108ec4fb5b00d3bd856)), closes [#5](https://github.com/Hexalith/Hexalith.Tenants/issues/5)
* Update tenant configuration management and validation ([ed9474b](https://github.com/Hexalith/Hexalith.Tenants/commit/ed9474b329d8fb766bc2079614b47ca09a94e5ff))

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - YYYY-MM-DD

### Added

- Tenant lifecycle management (Create, Update, Disable, Enable) via event-sourced TenantAggregate
- User-role management with three roles (TenantOwner, TenantContributor, TenantReader)
- Global administrator management with bootstrap mechanism
- Tenant key-value configuration with namespace conventions and limits
- Event-driven integration via DAPR pub/sub (CloudEvents 1.0)
- Tenant discovery and query endpoints with cursor-based pagination
- In-memory testing fakes with production-parity domain logic
- .NET Aspire hosting extensions and AppHost topology
- OpenTelemetry instrumentation for command and event processing
- Comprehensive documentation: quickstart guide, event contract reference, cross-aggregate timing, compensating commands
- CI/CD pipeline with GitHub Actions (build, test, NuGet publish)
- Sample consuming service demonstrating event subscription and access enforcement

[Unreleased]: https://github.com/Hexalith/Hexalith.Tenants/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Hexalith/Hexalith.Tenants/releases/tag/v0.1.0
