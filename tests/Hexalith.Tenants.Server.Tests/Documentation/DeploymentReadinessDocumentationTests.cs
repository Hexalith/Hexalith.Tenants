using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class DeploymentReadinessDocumentationTests {
    private static readonly Regex CompactJwtRegex = new(
        "\\beyJ[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\b",
        RegexOptions.Compiled);

    private static readonly Regex RawBearerTokenRegex = new(
        "Bearer\\s+(?!<redacted-access-token>)(?!<token-redacted>)[A-Za-z0-9._~+/=-]{20,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PrivateNetworkAddressRegex = new(
        "\\b(?:10\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}|172\\.(?:1[6-9]|2\\d|3[0-1])\\.\\d{1,3}\\.\\d{1,3}|192\\.168\\.\\d{1,3}\\.\\d{1,3})\\b",
        RegexOptions.Compiled);

    [Fact]
    public void Deployment_readiness_guide_exists_and_links_required_sources() {
        string guide = ReadGuide();
        string readme = File.ReadAllText(RepositoryPath("README.md"));

        string[] requiredLinks =
        [
            "production-auth-readiness.md",
            "production-auth-claim-contract.md",
            "quickstart.md",
            "../deploy/dapr/README.md",
            "../_bmad-output/implementation-artifacts/tests/test-summary.md",
        ];

        foreach (string requiredLink in requiredLinks) {
            guide.ShouldContain(requiredLink);
        }

        guide.ShouldContain("Story 7.6A-D evidence source");
        readme.ShouldContain("docs/deployment-readiness.md");
        File.Exists(RepositoryPath("docs", "deployment-readiness.md")).ShouldBeTrue();
    }

    [Fact]
    public void Deployment_readiness_evidence_summary_preserves_story_lanes_and_live_boundaries() {
        string summary = ReadTestSummary();

        string[] requiredStoryLanes =
        [
            "Story 7.6A",
            "Story 7.6B",
            "Story 7.6C",
            "Story 7.6D",
            "Story 7.6E Dev Story - Deployment Readiness Checklist and Evidence Template",
        ];

        foreach (string requiredStoryLane in requiredStoryLanes) {
            summary.ShouldContain(requiredStoryLane);
        }

        string[] requiredEvidenceTerms =
        [
            "Story 7.6A-D smoke-test lanes remain the source evidence",
            "Server focused via direct xUnit",
            "Integration focused via direct xUnit",
            "Full direct xUnit regression suite",
            "Passed:",
            "failed",
            "skipped",
            "Live Evidence Boundary",
            "No live production or production-like deployment evidence was collected for Story 7.6E",
            "must not be inferred from skipped or deterministic-local tests",
            "pass/fail/skip counts",
        ];

        foreach (string requiredEvidenceTerm in requiredEvidenceTerms) {
            summary.ShouldContain(requiredEvidenceTerm);
        }

        string[] supportSafeTerms =
        [
            "compact JWTs",
            "bearer tokens",
            "signing keys",
            "decoded payloads",
            "raw command/event payloads",
            "private hosts",
            "concrete connection strings",
            "real tenant/user identifiers",
            "PII",
        ];

        foreach (string supportSafeTerm in supportSafeTerms) {
            summary.ShouldContain(supportSafeTerm);
        }
    }

    [Fact]
    public void Deployment_readiness_guide_contains_required_controls_and_separates_local_from_production() {
        string guide = ReadGuide();

        string[] requiredTerms =
        [
            "issuer",
            "audience",
            "eventstore:tenant",
            "HTTPS metadata",
            "signing/authority source",
            "DAPR components",
            "service invocation",
            "health endpoints",
            "environment variables",
            "IdP claim mappings",
            "DAPR prerequisites",
            "AppHost overrides",
            "verification commands",
            "Authentication__JwtBearer__Authority",
            "Authentication__JwtBearer__Issuer",
            "Authentication__JwtBearer__Audience",
            "Authentication__JwtBearer__RequireHttpsMetadata",
            "Authentication__JwtBearer__SigningKey",
            "eventstore-admin-ui",
            "deadletter.tenants.events",
            "receiver-specific deny-by-default access control",
            "unhealthy readiness returns HTTP 503",
            "PublishFailed",
            "at-least-once",
            "no fixed DAPR sidecar ports",
            "no recursive submodule initialization",
        ];

        foreach (string requiredTerm in requiredTerms) {
            guide.ShouldContain(requiredTerm);
        }

        ShouldOccurBefore(guide, "## Production IdP Readiness", "## Local Development Token Boundary");
        guide.ShouldContain("Production readiness evidence uses OIDC authority-based JWT validation");
        guide.ShouldContain("Local HMAC tokens and local Keycloak examples are development-only");
        guide.ShouldContain("what this proves");
        guide.ShouldContain("what this does not prove");
        guide.ShouldNotContain("DevOnlySigningKey-AtLeast32Chars!");
        guide.ShouldNotContain("sample users, passwords, realm settings, or local `sslRequired`");
    }

    [Fact]
    public void Deployment_readiness_evidence_template_contains_required_metadata_classifications_controls_and_boundaries() {
        string guide = ReadGuide();

        string[] requiredTemplateTerms =
        [
            "environment_alias",
            "run_datetime_utc",
            "commit_sha_or_package_version",
            "operator_alias",
            "reviewer_alias",
            "run_profile",
            "deterministic-local",
            "prepared-apphost",
            "production-like",
            "production",
            "final_classification",
            "evidence_source_links",
            "redaction_statement",
            "reviewer_verdict",
            "pass",
            "environment-blocker",
            "product-failure",
            "configuration-gap",
            "instrumentation-gap",
            "documentation-gap",
            "not-claimable",
            "auth",
            "DAPR components",
            "service invocation",
            "health/readiness",
            "command path",
            "query path",
            "pub/sub recovery",
            "evidence boundaries",
            "live_evidence_boundary",
            "skipped DAPR/AppHost tests are not passing deployment proof",
            "compact JWTs",
            "bearer tokens",
            "signing keys",
            "decoded token payloads",
            "raw command/event payloads",
            "private hosts",
            "concrete connection strings",
            "real tenant/user identifiers",
            "PII",
        ];

        foreach (string requiredTemplateTerm in requiredTemplateTerms) {
            guide.ShouldContain(requiredTemplateTerm);
        }
    }

    [Fact]
    public void Deployment_readiness_guide_and_template_are_support_safe() {
        string guide = ReadGuide();

        CompactJwtRegex.IsMatch(guide).ShouldBeFalse("Published readiness docs must not contain compact JWTs.");
        RawBearerTokenRegex.IsMatch(guide).ShouldBeFalse("Published readiness docs must not contain raw bearer tokens.");
        PrivateNetworkAddressRegex.IsMatch(guide).ShouldBeFalse("Published readiness docs must not contain private network addresses.");

        string[] forbiddenProductionEvidence =
        [
            "connectionString=",
            "Password=",
            "BEGIN PRIVATE KEY",
            "-----BEGIN",
            "@example.com",
            "tenant-a-prod",
            "user-123",
        ];

        foreach (string forbidden in forbiddenProductionEvidence) {
            guide.ShouldNotContain(forbidden);
        }
    }

    [Fact]
    public void Deployment_readiness_guide_does_not_claim_EventStore_validator_support_for_Tenants_evidence() {
        string guide = ReadGuide();
        string validator = File.ReadAllText(RepositoryPath("references", "Hexalith.EventStore", "scripts", "validate-operational-evidence.py"));

        validator.ShouldContain("query-operational-evidence/v1");
        validator.ShouldContain("signalr-operational-evidence/v1");
        validator.ShouldNotContain("tenants-deployment-readiness");
        guide.ShouldContain("The EventStore operational evidence validator currently supports query and SignalR operational-evidence schemas only.");
        guide.ShouldContain("Do not claim this Tenants deployment readiness template is validated by that script.");
    }

    private static string ReadGuide()
        => File.ReadAllText(RepositoryPath("docs", "deployment-readiness.md"));

    private static string ReadTestSummary()
        => File.ReadAllText(RepositoryPath("_bmad-output", "implementation-artifacts", "tests", "test-summary.md"));

    private static void ShouldOccurBefore(string text, string earlier, string later) {
        int earlierIndex = text.IndexOf(earlier, StringComparison.Ordinal);
        int laterIndex = text.IndexOf(later, StringComparison.Ordinal);

        earlierIndex.ShouldBeGreaterThanOrEqualTo(0);
        laterIndex.ShouldBeGreaterThanOrEqualTo(0);
        earlierIndex.ShouldBeLessThan(laterIndex);
    }

    private static string RepositoryPath(params string[] segments) {
        string repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string direct = Path.GetFullPath(Path.Combine(
            new[] { repoRoot }.Concat(segments).ToArray()));
        if (File.Exists(direct) || Directory.Exists(direct)) {
            return direct;
        }

        if (segments is ["references", "Hexalith.EventStore", ..]) {
            string parentEventStore = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, "..", ".." }.Concat(segments.Skip(2)).ToArray()));
            if (File.Exists(parentEventStore) || Directory.Exists(parentEventStore)) {
                return parentEventStore;
            }
        }

        // A dependent module (e.g. Hexalith.EventStore) is a nested submodule of this repository
        // that may be left uninitialized when this repository is itself a submodule of a parent
        // that checks the dependency out as a sibling checkout. Fall back to that sibling.
        if (segments is ["references", not null, ..] && segments[1].StartsWith("Hexalith.", StringComparison.Ordinal)) {
            string siblingReference = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, ".." }.Concat(segments.Skip(1)).ToArray()));
            if (File.Exists(siblingReference) || Directory.Exists(siblingReference)) {
                return siblingReference;
            }
        }

        if (segments.Length > 0 && segments[0].StartsWith("Hexalith.", StringComparison.Ordinal)) {
            string sibling = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, ".." }.Concat(segments).ToArray()));
            if (File.Exists(sibling) || Directory.Exists(sibling)) {
                return sibling;
            }
        }

        return direct;
    }
}
