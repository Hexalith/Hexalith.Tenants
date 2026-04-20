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
