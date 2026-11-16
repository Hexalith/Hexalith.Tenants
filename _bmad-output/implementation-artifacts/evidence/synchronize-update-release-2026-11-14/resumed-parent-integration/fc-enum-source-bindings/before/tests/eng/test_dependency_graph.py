#!/usr/bin/env python3
"""Synthetic graph, semantic-policy, AD-8 diff, and materialization tests."""

from __future__ import annotations

import ast
import copy
import hashlib
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import dependency_graph as dg  # noqa: E402


def _run(args: list[str], cwd: pathlib.Path) -> bytes:
    proc = subprocess.run(["git", *args], cwd=str(cwd), capture_output=True)
    if proc.returncode != 0:
        raise RuntimeError(f"git {args} failed in {cwd}: {proc.stderr.decode('utf-8', 'replace')}")
    return proc.stdout


class TempGitRepo:
    def __init__(self, root: pathlib.Path) -> None:
        self.root = root
        root.mkdir(parents=True, exist_ok=True)
        _run(["init", "--quiet", "-b", "main"], root)
        _run(["config", "user.email", "test@example.com"], root)
        _run(["config", "user.name", "Test"], root)
        self._gitmodules_entries: list[str] = []

    def write_text(self, relative: str, content: str) -> pathlib.Path:
        return self.write_bytes(relative, content.encode("utf-8"))

    def write_bytes(self, relative: str, data: bytes) -> pathlib.Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        _run(["add", "--", relative], self.root)
        return path

    def link_gitlink(self, path: str, commit_sha: str) -> None:
        _run(["update-index", "--add", "--cacheinfo", f"160000,{commit_sha},{path}"], self.root)

    def add_submodule(self, name: str, path: str, url: str, commit_sha: str) -> None:
        self._gitmodules_entries.append(f'[submodule "{name}"]\n\tpath = {path}\n\turl = {url}\n')
        self.write_text(".gitmodules", "".join(self._gitmodules_entries))
        self.link_gitlink(path, commit_sha)

    def commit(self, message: str = "commit") -> str:
        _run(["-c", "commit.gpgsign=false", "commit", "--quiet", "-m", message], self.root)
        return self.head()

    def head(self) -> str:
        return _run(["rev-parse", "HEAD"], self.root).decode("ascii").strip()


BASELINE_CATALOG = (
    b"\xef\xbb\xbf<Project>\r\n"
    b"  <ItemGroup>\r\n"
    b'    <PackageVersion Include="Some.Package" Version="1.0.0" />\r\n'
    b"  </ItemGroup>\r\n"
    b"</Project>\r\n"
)

OWNER_SHIM = (
    "<Project>\n"
    "  <PropertyGroup>\n"
    "    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n"
    "  </PropertyGroup>\n"
    "</Project>\n"
)


class GraphFixture:
    """Builds a synthetic closed-world graph across temp git repositories."""

    def __init__(self, tmp_path: pathlib.Path) -> None:
        self.tmp_path = tmp_path
        self.repos: dict[str, TempGitRepo] = {}
        self.policy: dict = {
            "schema": dg.POLICY_SCHEMA,
            "builds_identity": self.identity("Builds"),
            "trusted_identities": [],
            "semantic_profiles": {},
            "profiles": {},
            "module_build_registry": {},
            "resource_limits": {
                "max_edges": 4096,
                "max_ls_tree_bytes_per_owner_commit": 67108864,
                "max_gitmodules_blob_bytes": 1048576,
                "max_catalog_blob_bytes": 4194304,
                "max_contract_tree_files": 16384,
                "max_contract_tree_blob_bytes": 16777216,
                "max_contract_tree_total_bytes": 268435456,
                "max_workflow_closure_depth": 16,
                "max_workflow_closure_sources": 256,
                "max_workflow_source_blob_bytes": 1048576,
                "max_workflow_source_total_bytes": 16777216,
            },
            "evaluator_authorizations": {"ci": [], "release": [], "post_release": []},
        }

    def add_repo(self, name: str) -> TempGitRepo:
        repo = TempGitRepo(self.tmp_path / name)
        self.repos[name] = repo
        self.policy["trusted_identities"].append({"identity": self.identity(name), "local_path": str(repo.root)})
        return repo

    @staticmethod
    def identity(name: str) -> str:
        return f"github.com/test/{name.lower()}"

    @staticmethod
    def url(name: str) -> str:
        return f"https://github.com/test/{name}.git"

    def link(self, owner: str, path: str, target: str, target_commit: str) -> None:
        self.repos[owner].add_submodule(target, path, self.url(target), target_commit)

    def register_module(self, name: str, *, evidence_only: bool = False) -> None:
        identity = self.identity(name)
        if evidence_only:
            self.policy["module_build_registry"][identity] = {
                "disposition": "evidence-only",
                "solution": None,
                "builds_contract_source": "none",
                "restore_argv": None,
                "build_argv": None,
            }
            return
        solution = f"{name}.slnx"
        self.policy["module_build_registry"][identity] = {
            "disposition": "build",
            "solution": solution,
            "builds_contract_source": "self" if name == "Builds" else "edge-tree:references/Builds",
            "restore_argv": [
                "dotnet", "restore", solution, "-p:Configuration=Release", "-p:UseNuGetDeps=true",
            ],
            "build_argv": [
                "dotnet", "build", solution, "--configuration", "Release", "--no-restore",
                "-p:UseNuGetDeps=true",
            ],
        }


class CollectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def test_depth1_depth2_collection_and_digest_determinism(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        mid = fx.add_repo("Mid")
        mid.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Mid", "references/Builds", "Builds", builds_commit)
        mid_commit = mid.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit()

        envelope1 = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        envelope2 = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertEqual(envelope1["graph_digest"], envelope2["graph_digest"])
        self.assertEqual(envelope1["edge_count"], 2)
        depth1 = [e for e in envelope1["edges"] if e["depth"] == 1]
        depth2 = [e for e in envelope1["edges"] if e["depth"] == 2]
        self.assertEqual(len(depth1), 1)
        self.assertEqual(depth1[0]["repository"], fx.identity("Mid"))
        self.assertEqual(len(depth2), 1)
        self.assertEqual(depth2[0]["repository"], fx.identity("Builds"))
        self.assertEqual(depth2[0]["catalog_sha256"], hashlib.sha256(BASELINE_CATALOG).hexdigest())

    def test_approved_depth2_targets_need_no_root_gitlinks_or_local_stores(self) -> None:
        fx = GraphFixture(self.tmp_path)
        target_commits = {}
        for name in ("Platform", "McpCli"):
            target = fx.add_repo(name)
            target.add_submodule(
                "Untrusted", "references/Untrusted",
                "https://github.com/other/untrusted.git", "1" * 40,
            )
            target_commits[name] = target.commit()
            entry = next(
                row for row in fx.policy["trusted_identities"]
                if row["identity"] == fx.identity(name)
            )
            entry["local_path"] = f"references/{name}"

        mid = fx.add_repo("Mid")
        for name, commit in target_commits.items():
            fx.link("Mid", f"references/{name}", name, commit)
        mid_commit = mid.commit()
        root = fx.add_repo("Root")
        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertEqual(envelope["edge_count"], 3)
        self.assertEqual(
            {(edge["repository"], edge["commit"]) for edge in envelope["edges"] if edge["depth"] == 2},
            {(fx.identity(name), commit) for name, commit in target_commits.items()},
        )
        self.assertEqual(
            [edge["repository"] for edge in envelope["edges"] if edge["depth"] == 1],
            [fx.identity("Mid")],
        )
        for name in target_commits:
            self.assertFalse((root.root / f"references/{name}").exists())
            with self.subTest(unapproved=name):
                policy = copy.deepcopy(fx.policy)
                policy["trusted_identities"] = [
                    row for row in policy["trusted_identities"]
                    if row["identity"] != fx.identity(name)
                ]
                with self.assertRaisesRegex(dg.GraphError, "unknown/untrusted repository identity"):
                    dg.collect_graph(root.root, fx.identity("Root"), root_commit, policy)

    def test_depth2_boundary_excludes_depth3(self) -> None:
        fx = GraphFixture(self.tmp_path)
        deepest = fx.add_repo("Deepest")
        deepest.write_text("marker.txt", "x")
        deepest_commit = deepest.commit()

        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        # Builds' own gitlink to Deepest would only surface as depth 3 (Builds' own edges are
        # never collected, since it is itself a depth-2 node here); it must be excluded.
        fx.link("Builds", "references/Deepest", "Deepest", deepest_commit)
        builds_commit = builds.commit()

        mid = fx.add_repo("Mid")
        mid.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Mid", "references/Builds", "Builds", builds_commit)
        mid_commit = mid.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertEqual(envelope["edge_count"], 2)
        self.assertEqual({e["depth"] for e in envelope["edges"]}, {1, 2})
        self.assertFalse(any(e["owner_repository"] == fx.identity("Builds") for e in envelope["edges"]))

    def test_self_back_reference_edge_is_recorded(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root_commit_stub = root.commit("stub")

        mid = fx.add_repo("Mid")
        mid.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Mid", "references/Root", "Root", root_commit_stub)  # back edge to root
        mid_commit = mid.commit()

        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit("add mid")

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        depth2 = [e for e in envelope["edges"] if e["depth"] == 2]
        self.assertEqual(len(depth2), 1)
        self.assertEqual(depth2[0]["repository"], fx.identity("Root"))

    def test_multiple_owners_selecting_same_builds_commit(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        for name in ("Alpha", "Beta"):
            owner = fx.add_repo(name)
            owner.write_text("Directory.Packages.props", OWNER_SHIM)
            fx.link(name, "references/Builds", "Builds", builds_commit)
            owner.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Alpha", "Alpha", fx.repos["Alpha"].head())
        fx.link("Root", "references/Beta", "Beta", fx.repos["Beta"].head())
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        builds_edges = [e for e in envelope["edges"] if e["repository"] == fx.identity("Builds")]
        self.assertEqual(len(builds_edges), 2)
        self.assertEqual(builds_edges[0]["catalog_sha256"], builds_edges[1]["catalog_sha256"])
        self.assertEqual(builds_edges[0]["catalog_sha256"], hashlib.sha256(BASELINE_CATALOG).hexdigest())
        self.assertIsNone(builds_edges[0]["catalog_contract_version"])

    def test_deterministic_edge_ordering(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        for name in ("Zeta", "Alpha", "Mu"):
            child = fx.add_repo(name)
            child.write_text("marker.txt", name)
            child_commit = child.commit()
            fx.link("Root", f"references/{name}", name, child_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        paths = [e["path"] for e in envelope["edges"]]
        self.assertEqual(paths, sorted(paths))

    def test_missing_gitmodules_mapping_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        # a gitlink with no corresponding .gitmodules entry
        root.link_gitlink("references/Ghost", "f" * 40)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_unknown_identity_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root.add_submodule("Untrusted", "references/Untrusted", "https://github.com/other/untrusted.git", "1" * 40)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_missing_catalog_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_text("README.md", "no catalog here")
        builds_commit = builds.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_malformed_gitmodules_url_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        bad_urls = [
            "https://user:pass@github.com/test/evil.git",  # userinfo
            "https://github.com:8443/test/evil.git",  # port
            "https://github.com/test/evil?x=1",  # query
            "https://not-github.com/test/evil.git",  # wrong host
            "https://github.com/test/%2e%2e",  # percent-escape
            "not-a-url",
        ]
        for index, url in enumerate(bad_urls):
            with self.subTest(url=url):
                with self.assertRaises(dg.GraphError):
                    dg.normalize_identity(url, f"case[{index}]")

    def test_unsafe_path_rejected(self) -> None:
        for path in ("/absolute", "a/../b", "a/./b", "a\\b", "", "a//b".replace("//", "/") + "/.."):
            with self.subTest(path=path):
                with self.assertRaises(dg.GraphError):
                    dg.normalize_path(path, "case")

    def test_edge_count_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_edges"] = 0
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        child = fx.add_repo("Child")
        child.write_text("marker.txt", "x")
        child_commit = child.commit()
        fx.link("Root", "references/Child", "Child", child_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_catalog_blob_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_catalog_blob_bytes"] = 4
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_gitmodules_blob_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_gitmodules_blob_bytes"] = 4
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_ls_tree_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_ls_tree_bytes_per_owner_commit"] = 1
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)


BASELINE_PROFILE = {
    "owner_checks": {"well_formed_project_root": True, "override_not_enabled": True},
    "selected_catalog_required_properties": {},
    "selected_catalog_required_packages": {},
}

REQUIRED_PACKAGE_PROFILE = {
    "owner_checks": {"no_local_override_for_selected_catalog_packages": True},
    "selected_catalog_required_properties": {},
    "selected_catalog_required_packages": {"Some.Package": "1.0.0"},
}

REQUIRED_PACKAGE_NAMES_PROFILE = {
    "owner_checks": {"no_local_override_for_selected_catalog_packages": True},
    "selected_catalog_required_properties": {},
    "selected_catalog_required_package_names": ["Some.Package"],
    "selected_catalog_required_packages": {},
}

REQUIRED_PROPERTY_PROFILE = {
    "owner_checks": {},
    "selected_catalog_required_properties": {"SomeVersion": "1.0.0"},
    "selected_catalog_required_packages": {},
}


class SemanticEvaluationTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def _catalog_at(self, version: str) -> bytes:
        return (
            b"\xef\xbb\xbf<Project>\r\n"
            b"  <ItemGroup>\r\n"
            b'    <PackageVersion Include="Some.Package" Version="' + version.encode("ascii") + b'" />\r\n'
            b"  </ItemGroup>\r\n"
            b"</Project>\r\n"
        )

    def _catalog_with_properties(self, values: list[str]) -> bytes:
        properties = b"".join(
            b"    <SomeVersion>" + value.encode("ascii") + b"</SomeVersion>\r\n"
            for value in values
        )
        return (
            b"\xef\xbb\xbf<Project>\r\n  <PropertyGroup>\r\n"
            + properties
            + b"  </PropertyGroup>\r\n</Project>\r\n"
        )

    def _catalog_with_property_body(self, body: bytes) -> bytes:
        """Build a catalog whose <Project> children are supplied verbatim.

        Lets a case express the conditional, ancestor-conditional, and Choose shapes the
        real shared catalog can take, which _catalog_with_properties cannot.
        """
        return b"\xef\xbb\xbf<Project>\r\n" + body + b"</Project>\r\n"

    def _evaluate_required_property_catalog(self, catalog: bytes) -> dict:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            catalog,
            "required-property-profile",
            REQUIRED_PROPERTY_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        return dg.evaluate_semantics(root.root, fx.policy, envelope)

    def _evaluate_required_property(self, values: list[str]) -> dict:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            self._catalog_with_properties(values),
            "required-property-profile",
            REQUIRED_PROPERTY_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        return dg.evaluate_semantics(root.root, fx.policy, envelope)

    def _build(self, fx: GraphFixture, owner_name: str, catalog_bytes: bytes, profile_name: str, profile: dict) -> tuple[TempGitRepo, str]:
        # one shared Builds repository per fixture (matching policy["builds_identity"] /
        # its single local_path); each call commits a new catalog state, mirroring how
        # different owners in reality pin different commits of the SAME Builds repo.
        builds = fx.repos.get("Builds") or fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", catalog_bytes)
        builds_commit = builds.commit(f"catalog for {owner_name}")

        owner = fx.add_repo(owner_name)
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link(owner_name, "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity(owner_name)] = profile_name
        fx.policy["profiles"][profile_name] = profile
        return owner, owner_commit

    def test_canonical_source_digest_rejects_changed_or_missing_platform_helper(self) -> None:
        for content in ("approved canonical source\n", "changed source\n", None):
            with self.subTest(content=content), tempfile.TemporaryDirectory() as temp:
                fx = GraphFixture(pathlib.Path(temp))
                root = fx.add_repo("Root")
                root.write_text("Directory.Packages.props", OWNER_SHIM)
                profile = copy.deepcopy(BASELINE_PROFILE)
                profile["owner_checks"]["canonical_source_files"] = {"DaprSelfHostedMtls.cs": hashlib.sha256(b"approved canonical source\n").hexdigest()}
                owner, _ = self._build(fx, "Owner", self._catalog_at("1.0.0"), "canonical-source-profile", profile)
                if content is not None:
                    owner.write_text("DaprSelfHostedMtls.cs", content)
                    owner.commit()
                fx.link("Root", "references/Owner", "Owner", owner.head())
                commit = root.commit()
                envelope = dg.collect_graph(root.root, fx.identity("Root"), commit, fx.policy)
                if content == "approved canonical source\n":
                    self.assertEqual(dg.evaluate_semantics(root.root, fx.policy, envelope)["selectors_validated"], 1)
                else:
                    with self.assertRaisesRegex(dg.GraphError, "canonical source.*missing or differs"):
                        dg.evaluate_semantics(root.root, fx.policy, envelope)

    def test_compatible_pointer_advance_passes_under_same_profile(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)

        ownerA, commitA = self._build(fx, "OwnerA", self._catalog_at("1.0.0"), "shared-profile", BASELINE_PROFILE)
        ownerB, commitB = self._build(fx, "OwnerB", self._catalog_at("2.0.0"), "shared-profile", BASELINE_PROFILE)
        self.assertNotEqual(commitA, commitB)

        fx.link("Root", "references/OwnerA", "OwnerA", commitA)
        fx.link("Root", "references/OwnerB", "OwnerB", commitB)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        semantics = dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertEqual(semantics["selectors_validated"], 2)

    def test_semantic_mismatch_fails_with_actionable_diagnostic(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        wrong_catalog = self._catalog_at("9.9.9")
        _owner, owner_commit = self._build(fx, "Owner", wrong_catalog, "required-profile", REQUIRED_PACKAGE_PROFILE)
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("Some.Package", str(ctx.exception))

    def test_required_property_match_passes(self) -> None:
        semantics = self._evaluate_required_property(["1.0.0"])
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_required_property_missing_fails_with_actionable_diagnostic(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property([])
        message = str(ctx.exception)
        self.assertIn("references/Builds", message)
        self.assertIn("SomeVersion expected exactly one value '1.0.0'", message)
        self.assertIn("found 0 values <missing>", message)

    def test_required_property_duplicate_fails_with_actionable_diagnostic(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property(["1.0.0", "2.0.0"])
        message = str(ctx.exception)
        self.assertIn("references/Builds", message)
        self.assertIn("SomeVersion expected exactly one value '1.0.0'", message)
        self.assertIn("found 2 values ['1.0.0', '2.0.0']", message)

    def test_required_property_mismatch_fails_with_actionable_diagnostic(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property(["2.0.0"])
        message = str(ctx.exception)
        self.assertIn("references/Builds", message)
        self.assertIn("SomeVersion expected '1.0.0', found '2.0.0'", message)

    # The real shared catalog declares every Hexalith version property as
    # Condition="'$(Name)' == ''". That canonical self-default form must pass; any other
    # condition, a conditional ancestor, or a Choose branch must be rejected, because the
    # effective value would then depend on state this validator cannot observe.
    def test_required_property_canonical_self_default_condition_passes(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n"
            b"    <SomeVersion Condition=\"'$(SomeVersion)' == ''\">1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        semantics = self._evaluate_required_property_catalog(catalog)
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_required_property_foreign_condition_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n"
            b"    <SomeVersion Condition=\"'$(NeverSet)' == 'yes'\">1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("canonical self-default", str(ctx.exception))

    def test_required_property_condition_naming_another_property_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n"
            b"    <SomeVersion Condition=\"'$(OtherVersion)' == ''\">1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("canonical self-default", str(ctx.exception))

    def test_required_property_under_conditional_group_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup Condition=\"'$(NeverSet)' == 'yes'\">\r\n"
            b"    <SomeVersion>1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("conditional group", str(ctx.exception))

    def test_required_property_selected_through_choose_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <Choose>\r\n"
            b"    <When Condition=\"'$(NeverSet)' == 'yes'\">\r\n"
            b"      <PropertyGroup>\r\n"
            b"        <SomeVersion>1.0.0</SomeVersion>\r\n"
            b"      </PropertyGroup>\r\n"
            b"    </When>\r\n"
            b"  </Choose>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("Choose branch", str(ctx.exception))

    def test_required_property_empty_element_reports_empty_string_not_none(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n    <SomeVersion />\r\n  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        message = str(ctx.exception)
        self.assertIn("SomeVersion expected '1.0.0', found ''", message)
        self.assertNotIn("None", message)

    def test_no_semantic_profile_mapping_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(fx, "Owner", self._catalog_at("1.0.0"), "unused-profile", BASELINE_PROFILE)
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        del fx.policy["semantic_profiles"][fx.identity("Owner")]

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError):
            dg.evaluate_semantics(root.root, fx.policy, envelope)

    def test_root_import_shim_violation_fails(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Include="Own.Package" Version="1.0.0" /></ItemGroup></Project>',
        )
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        profile = copy.deepcopy(BASELINE_PROFILE)
        profile["owner_checks"]["no_package_version_rows"] = True
        fx.policy["semantic_profiles"][fx.identity("Root")] = "root-shim-profile"
        fx.policy["profiles"]["root-shim-profile"] = profile

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("import shim", str(ctx.exception))

    def test_sibling_presence_required_package_version_may_move(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            self._catalog_at("2.0.11"),
            "presence-profile",
            REQUIRED_PACKAGE_NAMES_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        semantics = dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_sibling_presence_required_package_missing_fails_with_coordinates(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        empty_catalog = b"\xef\xbb\xbf<Project>\r\n  <ItemGroup>\r\n  </ItemGroup>\r\n</Project>\r\n"
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            empty_catalog,
            "presence-profile",
            REQUIRED_PACKAGE_NAMES_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        message = str(ctx.exception)
        self.assertIn("github.com/test/owner@", message)
        self.assertIn("references/Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn("Some.Package", message)
        self.assertIn("exactly one unmasked shared operation", message)

    def test_sibling_presence_required_package_nonliteral_version_fails_with_coordinates(self) -> None:
        rows = {
            "missing": b'    <PackageVersion Include="Some.Package" />\r\n',
            "empty": b'    <PackageVersion Include="Some.Package" Version="" />\r\n',
            "non-literal": b'    <PackageVersion Include="Some.Package" Version="$(SomeVersion)" />\r\n',
        }
        for name, row in rows.items():
            with self.subTest(name=name):
                catalog = (
                    b"\xef\xbb\xbf<Project>\r\n"
                    b"  <ItemGroup>\r\n"
                    + row
                    + b"  </ItemGroup>\r\n"
                    b"</Project>\r\n"
                )
                fx = GraphFixture(self.tmp_path / name)
                root = fx.add_repo("Root")
                root.write_text("Directory.Packages.props", OWNER_SHIM)
                _owner, owner_commit = self._build(
                    fx,
                    "Owner",
                    catalog,
                    "presence-profile",
                    REQUIRED_PACKAGE_NAMES_PROFILE,
                )
                fx.link("Root", "references/Owner", "Owner", owner_commit)
                root_commit = root.commit()

                envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
                with self.assertRaises(dg.GraphError) as ctx:
                    dg.evaluate_semantics(root.root, fx.policy, envelope)
                message = str(ctx.exception)
                self.assertIn("github.com/test/owner@", message)
                self.assertIn("references/Builds", message)
                self.assertIn("github.com/test/builds@", message)
                self.assertIn("Some.Package", message)
                self.assertIn("literal NuGet version", message)

    def test_sibling_presence_required_package_local_override_fails_with_coordinates(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("2.0.11"))
        builds_commit = builds.commit()

        owner = fx.add_repo("Owner")
        owner.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Update="Some.Package" Version="2.0.11" /></ItemGroup></Project>',
        )
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "presence-profile"
        fx.policy["profiles"]["presence-profile"] = REQUIRED_PACKAGE_NAMES_PROFILE
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        message = str(ctx.exception)
        self.assertIn("github.com/test/owner@", message)
        self.assertIn("references/Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn("Some.Package", message)
        self.assertIn("without local override", message)

    def test_no_local_override_violation_fails(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()

        owner = fx.add_repo("Owner")
        owner.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Update="Some.Package" Version="1.0.0" /></ItemGroup></Project>',
        )
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "no-override-profile"
        fx.policy["profiles"]["no-override-profile"] = REQUIRED_PACKAGE_PROFILE
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("without local override", str(ctx.exception))

    def test_no_minver_violation_fails(self) -> None:
        root_xml = dg.parse_project_xml(
            b'<Project><ItemGroup><PackageReference Include="MinVer" /></ItemGroup></Project>', "test"
        )
        with self.assertRaises(dg.GraphError):
            dg.assert_no_minver(root_xml, "test")

    def test_no_minver_missing_directory_build_props_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        owner, owner_commit = self._build(fx, "Owner", self._catalog_at("1.0.0"), "no-minver-profile", {
            "owner_checks": {"no_minver": True},
            "selected_catalog_required_properties": {},
            "selected_catalog_required_packages": {},
        })
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("missing Directory.Build.props while no_minver is required", str(ctx.exception))

    def test_no_inline_versions_in_tracked_files_fails(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()

        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        owner.write_text(
            "src/Widget.csproj",
            '<Project><ItemGroup><PackageReference Include="Some.Package" Version="1.0.0" /></ItemGroup></Project>\n',
        )
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "no-inline-profile"
        fx.policy["profiles"]["no-inline-profile"] = {
            "owner_checks": {
                "no_inline_versions_in_tracked_files": {"extensions": [".csproj", ".props", ".targets"]},
            },
            "selected_catalog_required_properties": {},
            "selected_catalog_required_packages": {},
        }
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("must not declare an inline Version", str(ctx.exception))

    def test_validate_with_zero_builds_selectors_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("no Builds-selector edges", str(ctx.exception))

    def test_duplicate_gitmodules_path_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()
        other = fx.add_repo("Other")
        other.write_text("README.md", "x\n")
        other_commit = other.commit()

        # Two .gitmodules entries sharing one path — last-wins must not be silent.
        root._gitmodules_entries.append(
            f'[submodule "builds"]\n\tpath = references/Shared\n\turl = {fx.url("Builds")}\n'
        )
        root._gitmodules_entries.append(
            f'[submodule "other"]\n\tpath = references/Shared\n\turl = {fx.url("Other")}\n'
        )
        root.write_text(".gitmodules", "".join(root._gitmodules_entries))
        root.link_gitlink("references/Shared", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError) as ctx:
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertIn("duplicate .gitmodules path", str(ctx.exception))

    def test_multi_owner_commit_pins_are_each_owner_checked(self) -> None:
        """Two Builds-selector edges from the same identity at different owner commits
        must run owner_checks against each commit, not only the first.
        """
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()

        # Fabricate an envelope: same owner identity, two owner_commits. The later pin
        # introduces a PackageVersion row that no_package_version_rows must catch.
        # Use a real owner repo so read_blob works for both commits.
        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        commit_clean = owner.commit("clean shim")
        owner.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Include="Own.Package" Version="1.0.0" /></ItemGroup></Project>\n',
        )
        commit_dirty = owner.commit("dirty shim")

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "shim-profile"
        fx.policy["profiles"]["shim-profile"] = {
            "owner_checks": {"no_package_version_rows": True},
            "selected_catalog_required_properties": {},
            "selected_catalog_required_packages": {},
        }

        envelope = {
            "schema": dg.SCHEMA,
            "root": {"repository": fx.identity("Root"), "commit": "cccccccccccccccccccccccccccccccccccccccc"},
            "edges": [
                {
                    "owner_repository": fx.identity("Owner"),
                    "owner_commit": commit_clean,
                    "path": "references/Builds",
                    "repository": fx.identity("Builds"),
                    "commit": builds_commit,
                    "depth": 1,
                },
                {
                    "owner_repository": fx.identity("Owner"),
                    "owner_commit": commit_dirty,
                    "path": "references/Builds",
                    "repository": fx.identity("Builds"),
                    "commit": builds_commit,
                    "depth": 1,
                },
            ],
            "graph_digest": "0" * 64,
        }
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("import shim", str(ctx.exception))
        self.assertIn(commit_dirty, str(ctx.exception))

    def test_guarded_imports_mismatch_fails(self) -> None:
        spec = {
            "import_projects": ["$(A)"],
            "import_conditions": ["true"],
            "required_properties": {},
        }
        xml = dg.parse_project_xml(b"<Project></Project>", "test")
        with self.assertRaises(dg.GraphError):
            dg.assert_guarded_imports(xml, spec, "test")

    def test_bom_crlf_violations_fail(self) -> None:
        with self.assertRaises(dg.GraphError):
            dg.assert_utf8_bom_and_crlf(b"<Project></Project>", "no-bom")
        with self.assertRaises(dg.GraphError):
            dg.assert_utf8_bom_and_crlf(b"\xef\xbb\xbf<Project>\n</Project>\n", "bare-lf")
        # correct BOM + CRLF must pass without raising
        dg.assert_utf8_bom_and_crlf(b"\xef\xbb\xbf<Project>\r\n</Project>\r\n", "ok")


class DiffAndMaterializationTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def _pointer_advance(self) -> tuple[GraphFixture, dict, dict]:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_base = builds.commit("base catalog")

        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Owner", "references/Builds", "Builds", builds_base)
        owner_base = owner.commit("base owner")

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Owner", "Owner", owner_base)
        root_base = root.commit("base root")

        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG + b"<!-- compatible advance -->\r\n")
        builds_candidate = builds.commit("compatible catalog advance")
        owner.link_gitlink("references/Builds", builds_candidate)
        owner_candidate = owner.commit("advance catalog")
        root.link_gitlink("references/Owner", owner_candidate)
        root_candidate = root.commit("advance owner")

        for name in ("Root", "Owner", "Builds"):
            fx.register_module(name)
        base_graph = dg.collect_graph(root.root, fx.identity("Root"), root_base, fx.policy)
        candidate_graph = dg.collect_graph(root.root, fx.identity("Root"), root_candidate, fx.policy)
        return fx, base_graph, candidate_graph

    def test_depth1_pointer_advance_subsumes_descendant_churn_and_builds_once(self) -> None:
        fx, base_graph, candidate_graph = self._pointer_advance()
        evidence = dg.diff_graphs(
            base_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=candidate_graph["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        self.assertEqual([row["repository"] for row in evidence["affected_modules"]], [fx.identity("Owner")])
        self.assertEqual(len(evidence["changes"]), 2)
        descendant = next(change for change in evidence["changes"] if (change["after"] or change["before"])["depth"] == 2)
        self.assertEqual(descendant["subsumed_by"], fx.identity("Owner"))
        self.assertEqual(
            evidence["affected_modules"][0]["restore_argv"],
            ["dotnet", "restore", "Owner.slnx", "-p:Configuration=Release", "-p:UseNuGetDeps=true"],
        )

    def test_unchanged_graph_builds_nothing(self) -> None:
        fx, base_graph, _candidate_graph = self._pointer_advance()
        evidence = dg.diff_graphs(
            base_graph,
            base_graph,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=base_graph["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        self.assertEqual(evidence["changes"], [])
        self.assertEqual(evidence["affected_modules"], [])

    def test_root_owner_commit_only_churn_builds_nothing_and_retains_provenance(self) -> None:
        fx = GraphFixture(self.tmp_path)
        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        owner_commit = owner.commit("owner")

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_base = root.commit("base root")
        root.write_text("docs/unrelated.md", "root-only change")
        root_candidate = root.commit("candidate root")

        for name in ("Root", "Owner"):
            fx.register_module(name)
        base_graph = dg.collect_graph(root.root, fx.identity("Root"), root_base, fx.policy)
        candidate_graph = dg.collect_graph(root.root, fx.identity("Root"), root_candidate, fx.policy)

        evidence = dg.diff_graphs(
            base_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=root_base,
            candidate=root_candidate,
            merge_base=None,
            release_eligible=True,
        )

        self.assertNotEqual(root_base, root_candidate)
        self.assertEqual(base_graph["edges"][0]["owner_commit"], root_base)
        self.assertEqual(candidate_graph["edges"][0]["owner_commit"], root_candidate)
        self.assertEqual(evidence["changes"], [])
        self.assertEqual(evidence["affected_modules"], [])

    def test_removed_depth1_edge_collapses_to_frontcomposer_root(self) -> None:
        fx, base_graph, candidate_graph = self._pointer_advance()
        removed_candidate = copy.deepcopy(candidate_graph)
        removed_candidate["edges"] = []
        removed_candidate["edge_count"] = 0
        removed_candidate["graph_digest"] = dg.canonical_digest({
            "schema": removed_candidate["schema"],
            "root": removed_candidate["root"],
            "edge_count": 0,
            "edges": [],
        })
        evidence = dg.diff_graphs(
            base_graph,
            removed_candidate,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=removed_candidate["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        self.assertEqual([row["repository"] for row in evidence["affected_modules"]], [fx.identity("Root")])

    def test_contract_tree_materializes_exact_regular_bytes(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds.write_bytes("eng/config.bin", b"\x00\x01\xff")
        commit = builds.commit()
        destination = self.tmp_path / "materialized"
        result = dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)
        self.assertEqual(result["file_count"], 2)
        self.assertEqual((destination / "eng/config.bin").read_bytes(), b"\x00\x01\xff")
        self.assertEqual((destination / "Props/Directory.Packages.props").read_bytes(), BASELINE_CATALOG)

    def test_contract_tree_omits_exact_nested_gitlink_and_materializes_regular_files(self) -> None:
        fx = GraphFixture(self.tmp_path)
        ai_tools = fx.add_repo("AI.Tools")
        ai_tools.write_text("README.md", "nested repository content")
        ai_tools_commit = ai_tools.commit("nested repository")

        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds.write_bytes("eng/config.bin", b"\x00\x01\xff")
        executable = builds.write_text("eng/build.sh", "#!/bin/sh\nexit 0\n")
        executable.chmod(0o755)
        _run(["add", "--", "eng/build.sh"], builds.root)
        builds.add_submodule(
            "Hexalith.AI.Tools",
            "references/Hexalith.AI.Tools",
            fx.url("AI.Tools"),
            ai_tools_commit,
        )
        commit = builds.commit("real-shaped Builds tree")

        destination = self.tmp_path / "materialized-with-gitlink"
        result = dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)

        self.assertEqual(result["file_count"], 4)
        self.assertEqual((destination / "eng/config.bin").read_bytes(), b"\x00\x01\xff")
        self.assertEqual((destination / "Props/Directory.Packages.props").read_bytes(), BASELINE_CATALOG)
        self.assertEqual((destination / "eng/build.sh").stat().st_mode & 0o777, 0o755)
        self.assertTrue((destination / ".gitmodules").is_file())
        self.assertFalse((destination / "references/Hexalith.AI.Tools").exists())

    def test_contract_tree_validates_gitlink_path_before_omitting_it(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.link_gitlink(r"references\Nested", "f" * 40)
        commit = builds.commit("unsafe gitlink path")
        destination = self.tmp_path / "unsafe-gitlink"

        with self.assertRaises(dg.GraphError) as ctx:
            dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)

        self.assertIn("unsafe path", str(ctx.exception))
        self.assertFalse(destination.exists())

    def test_contract_tree_rejects_symlink_and_limits_before_extraction(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        os.symlink("Props/Directory.Packages.props", builds.root / "catalog-link")
        _run(["add", "catalog-link"], builds.root)
        commit = builds.commit()
        destination = self.tmp_path / "rejected"
        with self.assertRaises(dg.GraphError) as ctx:
            dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)
        self.assertIn("catalog-link", str(ctx.exception))
        self.assertIn("unsupported mode/type", str(ctx.exception))
        self.assertFalse(destination.exists())

        fx.policy["resource_limits"]["max_contract_tree_files"] = 1
        clean = fx.add_repo("CleanBuilds")
        clean.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        clean.write_text("second.txt", "x")
        clean_commit = clean.commit()
        with self.assertRaises(dg.GraphError):
            dg.materialize_contract_tree(clean.root, clean_commit, destination, fx.policy)
        self.assertFalse(destination.exists())

    def test_module_materialization_is_detached_exact_and_does_not_initialize_gitlinks(self) -> None:
        owner = TempGitRepo(self.tmp_path / "Owner")
        owner.write_text("value.txt", "base")
        owner.link_gitlink("references/Nested", "f" * 40)
        base = owner.commit("base")
        owner.write_text("value.txt", "candidate")
        owner.commit("candidate")

        destination = self.tmp_path / "isolated-owner"
        dg.materialize_repository_tree(owner.root, base, destination)

        self.assertEqual((destination / "value.txt").read_text(), "base")
        self.assertTrue((destination / ".git").exists())
        self.assertFalse((destination / "references/Nested/.git").exists())
        observed = _run(["rev-parse", "HEAD"], destination).decode("ascii").strip()
        self.assertEqual(observed, base)

    def test_run_affected_replaces_empty_gitlink_mount_before_contract_materialization(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        owner = fx.add_repo("Owner")
        fx.register_module("Owner")
        owner_commit = "a" * 40
        builds_commit = "b" * 40
        catalog_sha256 = hashlib.sha256(BASELINE_CATALOG).hexdigest()
        selector = {
            "owner_repository": fx.identity("Owner"),
            "owner_commit": owner_commit,
            "path": "references/Builds",
            "repository": fx.identity("Builds"),
            "commit": builds_commit,
            "depth": 2,
            "catalog_sha256": catalog_sha256,
            "catalog_contract_version": None,
        }
        module = dg._module_proof(fx.identity("Owner"), owner_commit, ["test pointer advance"], fx.policy)
        evidence = {"candidate_graph": {"edges": [selector]}, "affected_modules": [module]}
        output_root = self.tmp_path / "affected-output"

        def materialize_module(_local: pathlib.Path, _commit: str, destination: pathlib.Path) -> None:
            (destination / "references/Builds").mkdir(parents=True)

        def materialize_contract(
            _local: pathlib.Path,
            _commit: str,
            destination: pathlib.Path,
            _policy: dict,
        ) -> dict:
            self.assertFalse(destination.exists())
            catalog = destination / "Props/Directory.Packages.props"
            catalog.parent.mkdir(parents=True)
            catalog.write_bytes(BASELINE_CATALOG)
            return {"file_count": 1, "total_bytes": len(BASELINE_CATALOG), "commit": builds_commit}

        completed = subprocess.CompletedProcess([], 0, stdout="", stderr="")
        with (
            mock.patch.object(dg, "validate_diff_evidence", return_value=(fx.policy, evidence)),
            mock.patch.object(dg, "materialize_repository_tree", side_effect=materialize_module),
            mock.patch.object(dg, "materialize_contract_tree", side_effect=materialize_contract),
            mock.patch.object(dg.subprocess, "run", return_value=completed) as run,
        ):
            result = dg.run_affected_builds(owner.root, evidence, output_root)

        self.assertEqual(result["modules"][0]["disposition"], "build")
        self.assertEqual(run.call_count, 2)

    def test_run_affected_build_command_failure_remains_blocking(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        owner = fx.add_repo("Owner")
        fx.register_module("Owner")
        owner_commit = "a" * 40
        builds_commit = "b" * 40
        catalog_sha256 = hashlib.sha256(BASELINE_CATALOG).hexdigest()
        selector = {
            "owner_repository": fx.identity("Owner"),
            "owner_commit": owner_commit,
            "path": "references/Builds",
            "repository": fx.identity("Builds"),
            "commit": builds_commit,
            "depth": 2,
            "catalog_sha256": catalog_sha256,
            "catalog_contract_version": None,
        }
        module = dg._module_proof(
            fx.identity("Owner"),
            owner_commit,
            ["test pointer advance"],
            fx.policy,
        )
        evidence = {"candidate_graph": {"edges": [selector]}, "affected_modules": [module]}
        output_root = self.tmp_path / "affected-failure-output"

        def materialize_module(_local: pathlib.Path, _commit: str, destination: pathlib.Path) -> None:
            (destination / "references/Builds").mkdir(parents=True)

        def materialize_contract(
            _local: pathlib.Path,
            _commit: str,
            destination: pathlib.Path,
            _policy: dict,
        ) -> dict:
            catalog = destination / "Props/Directory.Packages.props"
            catalog.parent.mkdir(parents=True)
            catalog.write_bytes(BASELINE_CATALOG)
            return {"file_count": 1, "total_bytes": len(BASELINE_CATALOG), "commit": builds_commit}

        restore_succeeded = subprocess.CompletedProcess([], 0, stdout="restored", stderr="")
        build_failed = subprocess.CompletedProcess([], 23, stdout="", stderr="synthetic build failure")
        with (
            mock.patch.object(dg, "validate_diff_evidence", return_value=(fx.policy, evidence)),
            mock.patch.object(dg, "materialize_repository_tree", side_effect=materialize_module),
            mock.patch.object(dg, "materialize_contract_tree", side_effect=materialize_contract),
            mock.patch.object(
                dg.subprocess,
                "run",
                side_effect=(restore_succeeded, build_failed),
            ) as run,
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.run_affected_builds(owner.root, evidence, output_root)

        message = str(ctx.exception)
        self.assertIn(f"affected module {fx.identity('Owner')}@{owner_commit}", message)
        self.assertIn("build_argv failed with exit 23", message)
        self.assertIn("synthetic build failure", message)
        self.assertEqual(run.call_count, 2)

    def test_offline_graph_validation_rejects_drift_and_out_of_order_edges(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        drifted = copy.deepcopy(candidate_graph)
        drifted["edge_count"] += 1
        with self.assertRaises(dg.GraphError):
            dg.validate_graph_envelope(drifted)

        out_of_order = copy.deepcopy(candidate_graph)
        out_of_order["edges"].reverse()
        out_of_order["graph_digest"] = dg.canonical_digest({
            "schema": out_of_order["schema"],
            "root": out_of_order["root"],
            "edge_count": out_of_order["edge_count"],
            "edges": out_of_order["edges"],
        })
        with self.assertRaises(dg.GraphError):
            dg.validate_graph_envelope(out_of_order)

    def _reseal(self, graph: dict) -> None:
        graph["edges"].sort(key=lambda edge: (
            edge["depth"], edge["owner_repository"], edge["owner_commit"],
            edge["path"], edge["repository"], edge["commit"],
        ))
        graph["edge_count"] = len(graph["edges"])
        graph["graph_digest"] = dg.canonical_digest({
            "schema": graph["schema"],
            "root": graph["root"],
            "edge_count": graph["edge_count"],
            "edges": graph["edges"],
        })

    @staticmethod
    def _reseal_evidence(evidence: dict) -> None:
        evidence["evidence_digest"] = dg.canonical_digest({
            name: value for name, value in evidence.items() if name != "evidence_digest"
        })

    def _live_evidence(self) -> tuple[GraphFixture, dict, dict]:
        fx, base_graph, candidate_graph = self._pointer_advance()
        root_identity = fx.identity("Root")
        next(entry for entry in fx.policy["trusted_identities"] if entry["identity"] == root_identity)["local_path"] = "."
        evidence = dg.diff_graphs(
            base_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=candidate_graph["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        coordinate = {
            "schema": dg.POLICY_SCHEMA,
            "repository": root_identity,
            "path": dg.POLICY_PATH,
            "commit": base_graph["root"]["commit"],
            "sha256": "a" * 64,
        }
        evidence["dependency_policy"] = coordinate
        self._reseal_evidence(evidence)
        return fx, evidence, coordinate

    def test_affected_evidence_rejects_self_declared_untrusted_root_identity(self) -> None:
        fx, evidence, coordinate = self._live_evidence()
        hostile = copy.deepcopy(evidence)
        for graph_name in ("base_graph", "candidate_graph"):
            graph = hostile[graph_name]
            graph["root"]["repository"] = "github.com/test/forged"
            for edge in graph["edges"]:
                if edge["depth"] == 1:
                    edge["owner_repository"] = "github.com/test/forged"
            self._reseal(graph)
        self._reseal_evidence(hostile)

        with (
            mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.validate_diff_evidence(self.tmp_path, hostile)

        self.assertIn("trusted root identity", str(ctx.exception))

    def test_affected_evidence_rejects_base_graph_revision_mismatch(self) -> None:
        fx, evidence, coordinate = self._live_evidence()
        hostile = copy.deepcopy(evidence)
        hostile["base_graph"]["root"]["commit"] = "d" * 40
        for edge in hostile["base_graph"]["edges"]:
            if edge["depth"] == 1:
                edge["owner_commit"] = "d" * 40
        self._reseal(hostile["base_graph"])
        self._reseal_evidence(hostile)

        with (
            mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.validate_diff_evidence(self.tmp_path, hostile)

        self.assertIn("base graph root commit", str(ctx.exception))

    def test_affected_evidence_applies_active_policy_edge_ceiling(self) -> None:
        fx, evidence, coordinate = self._live_evidence()
        fx.policy["resource_limits"]["max_edges"] = 1

        with (
            mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.validate_diff_evidence(self.tmp_path, evidence)

        self.assertIn("active-policy 1-edge ceiling", str(ctx.exception))

    def test_offline_graph_validation_rejects_forged_depth1_root_ownership(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        for field, value, diagnostic in (
            ("owner_repository", "github.com/test/forged", "root repository"),
            ("owner_commit", "d" * 40, "root commit"),
        ):
            with self.subTest(field=field):
                hostile = copy.deepcopy(candidate_graph)
                depth1 = next(edge for edge in hostile["edges"] if edge["depth"] == 1)
                depth1[field] = value
                self._reseal(hostile)
                with self.assertRaises(dg.GraphError) as ctx:
                    dg.validate_graph_envelope(hostile)
                self.assertIn(diagnostic, str(ctx.exception))

    def test_offline_graph_validation_rejects_orphan_depth2_ancestry(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        hostile = copy.deepcopy(candidate_graph)
        depth2 = next(edge for edge in hostile["edges"] if edge["depth"] == 2)
        depth2["owner_commit"] = "d" * 40
        self._reseal(hostile)

        with self.assertRaises(dg.GraphError) as ctx:
            dg.validate_graph_envelope(hostile)

        self.assertIn("depth-1 target", str(ctx.exception))

    def test_offline_graph_validation_rejects_duplicate_logical_edges(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        hostile = copy.deepcopy(candidate_graph)
        depth1 = next(edge for edge in hostile["edges"] if edge["depth"] == 1)
        depth2 = next(edge for edge in hostile["edges"] if edge["depth"] == 2)
        alternate_owner_commit = "d" * 40

        alternate_depth1 = copy.deepcopy(depth1)
        alternate_depth1["path"] = "references/OwnerAlternate"
        alternate_depth1["commit"] = alternate_owner_commit
        alternate_depth2 = copy.deepcopy(depth2)
        alternate_depth2["owner_commit"] = alternate_owner_commit
        hostile["edges"].extend((alternate_depth1, alternate_depth2))
        self._reseal(hostile)

        with self.assertRaises(dg.GraphError) as ctx:
            dg.validate_graph_envelope(hostile)

        self.assertIn("duplicate logical edge", str(ctx.exception))

    def test_zero_push_base_forces_full_affected_and_is_never_release_eligible(self) -> None:
        # Reproduces, at the function level, the CLI `diff` zero/unavailable-push-base
        # branch (main(), "diff" command, lines ~1724-1751): the candidate graph stands
        # in for both base and candidate, release_eligible is forced False, and
        # full_affected marks every module rather than computing a real (unavailable)
        # diff. This does not invoke main()/argv parsing itself.
        fx, _base_graph, candidate_graph = self._pointer_advance()
        root_identity = fx.identity("Root")
        next(entry for entry in fx.policy["trusted_identities"] if entry["identity"] == root_identity)["local_path"] = "."
        zero_base = "0" * 40
        candidate = candidate_graph["root"]["commit"]

        evidence = dg.diff_graphs(
            candidate_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=candidate,
            candidate=candidate,
            merge_base=None,
            release_eligible=False,
            full_affected=True,
        )
        evidence["revisions"]["event_base"] = zero_base
        evidence["diagnostic"] = "zero/unavailable push base: full-affected diagnostics only; not release-eligible"

        self.assertFalse(evidence["revisions"]["release_eligible"])
        self.assertEqual(evidence["changes"], [])
        self.assertEqual(
            sorted(row["repository"] for row in evidence["affected_modules"]),
            sorted(fx.identity(name) for name in ("Root", "Owner")),
        )
        self.assertTrue(
            all("fail-closed full-affected diagnostic" in row["reasons"] for row in evidence["affected_modules"])
        )

        coordinate = {
            "schema": dg.POLICY_SCHEMA,
            "repository": root_identity,
            "path": dg.POLICY_PATH,
            "commit": candidate,
            "sha256": "a" * 64,
        }
        evidence["dependency_policy"] = coordinate
        self._reseal_evidence(evidence)

        with mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)):
            dg.validate_diff_evidence(self.tmp_path, evidence)


class NetworkAcquisitionRetryTests(unittest.TestCase):
    """Transient remote failures must retry; authoritative answers must still fail closed.

    A hosted-runner TLS blip cloning an approved remote (observed 2026-08-11) aborted the CI
    step before the graph gate ran. Retrying is safe because the requested commit is still
    verified after every fetch, but retrying a governance answer -- unknown ref, missing
    repository -- would weaken the gate, so only infrastructure-shaped stderr may retry.
    """

    def _completed(self, returncode: int, stderr: str) -> subprocess.CompletedProcess:
        return subprocess.CompletedProcess([], returncode, stdout=b"", stderr=stderr.encode("utf-8"))

    def test_transient_clone_failure_retries_and_succeeds(self) -> None:
        flake = self._completed(
            128,
            "fatal: unable to access 'https://github.com/hexalith/hexalith.tenants.git/': "
            "server certificate verification failed. CAfile: none CRLfile: none",
        )
        with (
            mock.patch.object(dg.subprocess, "run", side_effect=(flake, self._completed(0, ""))) as run,
            mock.patch.object(dg.time, "sleep") as sleep,
        ):
            proc, attempts = dg._run_git_network(["clone", "--quiet", "https://example.invalid/x.git", "dest"])

        self.assertEqual(proc.returncode, 0)
        self.assertEqual(attempts, 2)
        self.assertEqual(run.call_count, 2)
        sleep.assert_called_once_with(dg.NETWORK_RETRY_BACKOFF_SECONDS)

    def test_transient_failure_stops_at_the_attempt_ceiling(self) -> None:
        flake = self._completed(128, "fatal: unable to access: Could not resolve host: github.com")
        with (
            mock.patch.object(dg.subprocess, "run", return_value=flake) as run,
            mock.patch.object(dg.time, "sleep"),
        ):
            proc, attempts = dg._run_git_network(["fetch", "--no-tags", "origin", "a" * 40])

        self.assertEqual(proc.returncode, 128)
        self.assertEqual(attempts, dg.NETWORK_RETRY_ATTEMPTS)
        self.assertEqual(run.call_count, dg.NETWORK_RETRY_ATTEMPTS)

    def test_authoritative_failure_fails_closed_without_retry(self) -> None:
        missing_ref = self._completed(128, "fatal: couldn't find remote ref " + "a" * 40)
        with (
            mock.patch.object(dg.subprocess, "run", return_value=missing_ref) as run,
            mock.patch.object(dg.time, "sleep") as sleep,
        ):
            proc, attempts = dg._run_git_network(["fetch", "--no-tags", "origin", "a" * 40])

        self.assertEqual(proc.returncode, 128)
        self.assertEqual(attempts, 1)
        self.assertEqual(run.call_count, 1)
        sleep.assert_not_called()

    def test_retry_clears_a_partial_clone_destination(self) -> None:
        destination = pathlib.Path(tempfile.mkdtemp()) / "partial-store"
        destination.mkdir(parents=True)
        (destination / "objects").mkdir()
        flake = self._completed(128, "fatal: the remote end hung up unexpectedly")

        def run_side_effect(*_args: object, **_kwargs: object) -> subprocess.CompletedProcess:
            if run.call_count == 1:
                return flake
            self.assertFalse(destination.exists())
            return self._completed(0, "")

        with (
            mock.patch.object(dg.subprocess, "run", side_effect=run_side_effect) as run,
            mock.patch.object(dg.time, "sleep"),
        ):
            proc, attempts = dg._run_git_network(
                ["clone", "--quiet", "https://example.invalid/x.git", str(destination)],
                reset_before_retry=destination,
            )

        self.assertEqual(proc.returncode, 0)
        self.assertEqual(attempts, 2)

    def test_exhausted_retries_report_the_attempt_count(self) -> None:
        flake = self._completed(128, "fatal: unable to access: Could not resolve host: github.com")
        with (
            mock.patch.object(dg.subprocess, "run", return_value=flake),
            mock.patch.object(dg.time, "sleep"),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg._ensure_commit_available(pathlib.Path("/nonexistent-store"), "a" * 40)

        message = str(ctx.exception)
        self.assertIn(f"after {dg.NETWORK_RETRY_ATTEMPTS} attempts", message)
        self.assertIn("Could not resolve host", message)


class CanonicalDigestTests(unittest.TestCase):
    def test_canonical_bytes_are_compact_sorted_ascii(self) -> None:
        payload = {"b": 1, "a": [3, 2, 1], "c": "café"}
        raw = dg.canonical_bytes(payload)
        self.assertEqual(raw, b'{"a":[3,2,1],"b":1,"c":"caf\\u00e9"}')
        self.assertFalse(raw.endswith(b"\n"))

    def test_digest_is_sha256_of_canonical_bytes(self) -> None:
        payload = {"x": 1}
        self.assertEqual(dg.canonical_digest(payload), hashlib.sha256(dg.canonical_bytes(payload)).hexdigest())


class PolicyShapeTests(unittest.TestCase):
    """A profile whose shape would make a required check silently vacuous must fail closed.

    Before these pins, `profile.get("selected_catalog_required_properties", {})` meant a
    renamed or misspelled key validated nothing while `validate` still reported ok: true.
    """

    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def _load(self, profile: object) -> dict:
        path = self.tmp_path / "policy.json"
        policy = json.loads((ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig"))
        policy["profiles"]["some-profile"] = profile
        path.write_text(json.dumps(policy), encoding="utf-8")
        return dg.load_policy(path)

    @staticmethod
    def _frontcomposer_profile() -> dict:
        policy = json.loads(
            (ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig")
        )
        return copy.deepcopy(policy["profiles"]["frontcomposer-catalog-v1"])

    @staticmethod
    def _minimal_validate_policy(root_identity: str, builds_identity: str) -> dict:
        evidence_only = {
            "disposition": "evidence-only",
            "solution": None,
            "builds_contract_source": "none",
            "restore_argv": None,
            "build_argv": None,
        }
        return {
            "schema": dg.POLICY_SCHEMA,
            "builds_identity": builds_identity,
            "trusted_identities": [
                {"identity": root_identity, "local_path": "."},
                {"identity": builds_identity, "local_path": "references/Builds"},
            ],
            "semantic_profiles": {
                root_identity: "root-policy-a",
                builds_identity: "builds-policy",
            },
            "profiles": {
                "root-policy-a": {
                    "selected_catalog_required_packages": {"Some.Package": "1.0.0"},
                },
                "builds-policy": {},
            },
            "module_build_registry": {
                root_identity: copy.deepcopy(evidence_only),
                builds_identity: copy.deepcopy(evidence_only),
            },
            "resource_limits": {
                "max_edges": 4096,
                "max_ls_tree_bytes_per_owner_commit": 67108864,
                "max_gitmodules_blob_bytes": 1048576,
                "max_catalog_blob_bytes": 4194304,
                "max_contract_tree_files": 16384,
                "max_contract_tree_blob_bytes": 16777216,
                "max_contract_tree_total_bytes": 268435456,
                "max_workflow_closure_depth": 16,
                "max_workflow_closure_sources": 256,
                "max_workflow_source_blob_bytes": 1048576,
                "max_workflow_source_total_bytes": 16777216,
            },
            "evaluator_authorizations": {"ci": [], "release": [], "post_release": []},
        }

    def test_validate_uses_policy_from_selected_commit_by_default(self) -> None:
        root_identity = GraphFixture.identity("Root")
        builds_identity = GraphFixture.identity("Builds")
        root = TempGitRepo(self.tmp_path / "Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = TempGitRepo(root.root / "references" / "Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()
        root.add_submodule("Builds", "references/Builds", GraphFixture.url("Builds"), builds_commit)

        policy_a = self._minimal_validate_policy(root_identity, builds_identity)
        root.write_text(dg.POLICY_PATH, json.dumps(policy_a))
        policy_a_commit = root.commit()

        policy_b = copy.deepcopy(policy_a)
        policy_b["semantic_profiles"][root_identity] = "root-policy-b"
        policy_b["profiles"]["root-policy-b"] = {
            "selected_catalog_required_packages": {"Some.Package": "2.0.0"},
        }
        (root.root / dg.POLICY_PATH).write_text(json.dumps(policy_b), encoding="utf-8")

        completed = subprocess.run(
            [
                sys.executable,
                str(ROOT / "eng" / "dependency_graph.py"),
                "--root",
                str(root.root),
                "validate",
                "--commit",
                policy_a_commit,
                "--root-identity",
                root_identity,
            ],
            cwd=str(root.root),
            capture_output=True,
            text=True,
        )

        self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
        payload = json.loads(completed.stdout)
        self.assertTrue(payload["ok"])
        self.assertEqual(payload["semantics"]["selectors_validated"], 1)
        self.assertIn("under profile root-policy-a", payload["semantics"]["diagnostics"][0])
        self.assertNotIn("root-policy-b", payload["semantics"]["diagnostics"][0])

    def test_validate_rejects_legacy_committed_policy_before_graph_collection(self) -> None:
        root_identity = GraphFixture.identity("Root")
        builds_identity = GraphFixture.identity("Builds")
        root = TempGitRepo(self.tmp_path / "Root")
        legacy_policy = self._minimal_validate_policy(root_identity, builds_identity)
        for row in legacy_policy["module_build_registry"].values():
            del row["restore_argv"]
            del row["build_argv"]
        for name in (
            "max_workflow_closure_depth",
            "max_workflow_closure_sources",
            "max_workflow_source_blob_bytes",
            "max_workflow_source_total_bytes",
        ):
            del legacy_policy["resource_limits"][name]
        root.write_text(dg.POLICY_PATH, json.dumps(legacy_policy))
        legacy_commit = root.commit()

        completed = subprocess.run(
            [
                sys.executable,
                str(ROOT / "eng" / "dependency_graph.py"),
                "--root",
                str(root.root),
                "validate",
                "--commit",
                legacy_commit,
                "--root-identity",
                root_identity,
            ],
            cwd=str(root.root),
            capture_output=True,
            text=True,
        )

        self.assertEqual(completed.returncode, 1, completed.stdout + completed.stderr)
        payload = json.loads(completed.stdout)
        self.assertFalse(payload["ok"])
        self.assertEqual(
            payload["error"],
            "validate requires a strict committed policy; legacy policy migration is non-executable",
        )

    def test_known_profile_keys_load(self) -> None:
        policy = self._load({
            "owner_checks": {},
            "selected_catalog_required_property_names": ["SomeVersion"],
            "selected_catalog_required_properties": {},
            "selected_catalog_required_package_names": ["Some.Package"],
            "selected_catalog_required_packages": {},
        })
        self.assertEqual(
            policy["profiles"]["some-profile"]["selected_catalog_required_property_names"],
            ["SomeVersion"],
        )
        self.assertEqual(
            policy["profiles"]["some-profile"]["selected_catalog_required_package_names"],
            ["Some.Package"],
        )

    def test_misspelled_required_properties_key_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_propertys": {"SomeVersion": "1.0.0"}})
        self.assertIn("unknown keys", str(ctx.exception))

    def test_required_property_names_must_be_non_empty_sorted_unique_names(self) -> None:
        for value in (
            [],
            "SomeVersion",
            ["SomeVersion", "SomeVersion"],
            ["ZVersion", "AVersion"],
            ["not-a-name"],
            [1],
        ):
            with self.subTest(value=value):
                with self.assertRaises(dg.GraphError):
                    self._load({"selected_catalog_required_property_names": value})

    def test_required_property_names_cannot_duplicate_literal_requirements(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "selected_catalog_required_property_names": ["SomeVersion"],
                "selected_catalog_required_properties": {"SomeVersion": "1.0.0"},
            })
        self.assertIn("cannot also carry literal requirements", str(ctx.exception))

    def test_required_package_names_must_be_non_empty_sorted_unique_ids(self) -> None:
        for value in (
            [],
            "Some.Package",
            ["Some.Package", "Some.Package"],
            ["Z.Package", "A.Package"],
            ["not a package"],
            [1],
        ):
            with self.subTest(value=value):
                with self.assertRaises(dg.GraphError):
                    self._load({"selected_catalog_required_package_names": value})

    def test_required_package_names_cannot_duplicate_literal_requirements(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "selected_catalog_required_package_names": ["Some.Package"],
                "selected_catalog_required_packages": {"Some.Package": "1.0.0"},
            })
        self.assertIn("cannot also carry literal requirements", str(ctx.exception))

    def test_non_dict_required_properties_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_properties": ["SomeVersion"]})
        self.assertIn("must be an object", str(ctx.exception))

    def test_explicit_null_required_catalog_controls_fail_closed(self) -> None:
        for key in (
            "selected_catalog_required_property_names",
            "selected_catalog_required_properties",
            "selected_catalog_required_package_names",
            "selected_catalog_required_packages",
        ):
            with self.subTest(key=key):
                with self.assertRaises(dg.GraphError) as ctx:
                    self._load({key: None})
                self.assertIn(key, str(ctx.exception))

    def test_non_string_expected_value_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_properties": {"SomeVersion": 1.0}})
        self.assertIn("must be a string", str(ctx.exception))

    def test_canonical_source_rule_rejects_unbounded_or_malformed_enrollment(self) -> None:
        for files in ({}, {"../escape.cs": "a" * 64}, {"DaprSelfHostedMtls.cs": "not-a-digest"}, True):
            with self.subTest(files=files), self.assertRaises(dg.GraphError):
                self._load({"owner_checks": {"canonical_source_files": files}})

    def test_unknown_owner_checks_key_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_package_version_row": True},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("owner_checks has unknown keys", str(ctx.exception))

    def test_non_object_owner_checks_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": ["no_minver"],
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("owner_checks must be an object", str(ctx.exception))

    def test_owner_checks_boolean_value_must_be_bool(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_minver": "yes"},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("must be a boolean", str(ctx.exception))

    def test_owner_checks_inline_versions_must_be_object_with_extensions(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_inline_versions_in_tracked_files": True},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("must be an object", str(ctx.exception))

        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_inline_versions_in_tracked_files": {}},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("extensions", str(ctx.exception))

    def test_owner_checks_guarded_imports_must_be_non_empty_object(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"guarded_imports": {}},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("must be a non-empty object", str(ctx.exception))

    def test_landed_task2_seed_is_non_executable_delayed_activation_only(self) -> None:
        policy = json.loads((ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig"))
        for row in policy["module_build_registry"].values():
            del row["restore_argv"]
            del row["build_argv"]
        for name in (
            "max_workflow_closure_depth",
            "max_workflow_closure_sources",
            "max_workflow_source_blob_bytes",
            "max_workflow_source_total_bytes",
        ):
            del policy["resource_limits"][name]
        raw = json.dumps(policy).encode("utf-8")

        with self.assertRaises(dg.GraphError):
            dg.load_policy_bytes(raw, "strict candidate")

        migrated = dg.load_policy_bytes(raw, "immutable base", allow_legacy_registry=True)
        self.assertIs(migrated["__legacy_registry_migration__"], True)
        self.assertEqual(
            migrated["module_build_registry"]["github.com/hexalith/hexalith.frontcomposer"]["restore_argv"][:2],
            ["dotnet", "restore"],
        )

    def test_landed_dependency_build_targets_are_complete_owned_surfaces(self) -> None:
        policy = dg.load_policy(ROOT / "eng" / "dependency-graph-policy.json")
        expected = {
            "github.com/hexalith/hexalith.frontcomposer": "Hexalith.FrontComposer.slnx",
            "github.com/hexalith/hexalith.eventstore": "Hexalith.EventStore.slnx",
            "github.com/hexalith/hexalith.tenants": "Hexalith.Tenants.Standalone.slnx",
            "github.com/hexalith/hexalith.commons": "Hexalith.Commons.Standalone.slnx",
            "github.com/hexalith/hexalith.builds": "Hexalith.Builds.slnx",
            "github.com/hexalith/hexalith.polymorphicserializations": "Hexalith.PolymorphicSerializations.slnx",
            "github.com/hexalith/hexalith.memories": "Hexalith.Memories.slnx",
            "github.com/hexalith/hexalith.parties": "Hexalith.Parties.Standalone.slnx",
        }

        for identity, target in expected.items():
            row = policy["module_build_registry"][identity]
            self.assertEqual(row["solution"], target)
            self.assertEqual(row["restore_argv"], [
                "dotnet", "restore", target, "-p:Configuration=Release", "-p:UseNuGetDeps=true",
            ])
            self.assertEqual(row["build_argv"], [
                "dotnet", "build", target, "--configuration", "Release", "--no-restore",
                "-p:UseNuGetDeps=true",
            ])

    @staticmethod
    def _catalog_property_default_start(catalog: bytes, property_name: str) -> int:
        openings = list(re.finditer(rb"<" + re.escape(property_name.encode("ascii")) + rb"(?=[\s>])[^>]*>", catalog))
        defaults = []
        for opening in openings:
            element = ET.fromstring(opening.group() + f"</{property_name}>".encode("ascii"))
            condition = element.get("Condition")
            self_default = dg._SELF_DEFAULT_CONDITION.match(condition) if condition is not None else None
            if condition is None or (self_default is not None and self_default.group("name") == property_name):
                defaults.append(opening)
        if len(defaults) == 1:
            return defaults[0].start()
        if len(openings) == 1:
            return openings[0].start()
        raise AssertionError(f"Fixture requires one canonical default for {property_name}")

    @classmethod
    def _replace_catalog_property_element(cls, catalog: bytes, property_name: str, replacement: bytes) -> bytes:
        start = cls._catalog_property_default_start(catalog, property_name)
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        return catalog[:start] + replacement + catalog[end:]

    @classmethod
    def _replace_catalog_property_value(cls, catalog: bytes, property_name: str, value: str) -> bytes:
        start = cls._catalog_property_default_start(catalog, property_name)
        opening_end = catalog.index(b">", start) + 1
        closing_start = catalog.index(f"</{property_name}>".encode("ascii"), opening_end)
        return catalog[:opening_end] + value.encode("ascii") + catalog[closing_start:]

    @classmethod
    def _frontcomposer_catalog(cls) -> bytes:
        # These synthetic tests isolate the unchanged owner policy from external
        # catalog version drift before mutating each intended check. They retain
        # the raw optional logging import and do not assert actual catalog acceptance.
        catalog = (ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props").read_bytes()
        for package_id, expected in cls._frontcomposer_profile()["selected_catalog_required_packages"].items():
            if package_id == "System.Collections.Immutable" and b'Version="$(HexalithImmutableVersion)"' in catalog:
                continue  # Exercise the consumer view as authored, without normalizing it away.
            pattern = rb'(<PackageVersion Include="' + re.escape(package_id.encode()) + rb'" Version=")[^"]+(" />)'
            catalog, count = re.subn(pattern, lambda match: match[1] + expected.encode() + match[2], catalog)
            if count != 1:
                raise AssertionError(f"Fixture requires one unconditional package row for {package_id}")
        return catalog

    def _evaluate_frontcomposer_profile(self, catalog: bytes, profile_name: str = "frontcomposer-catalog-v1", *, logging_target: bytes | None = None) -> dict:
        with tempfile.TemporaryDirectory(dir=self.tmp_path) as fixture_directory:
            fx = GraphFixture(pathlib.Path(fixture_directory))

            frontcomposer = fx.add_repo("FrontComposer")
            frontcomposer.write_text("Directory.Packages.props", OWNER_SHIM)

            builds = fx.add_repo("Builds")
            builds.write_text(".gitattributes", "Props/Directory.Packages.props text eol=crlf\n")
            builds.write_bytes("Props/Directory.Packages.props", catalog)
            if logging_target is not None:
                builds.write_bytes("Props/Logging.Build.targets", logging_target)
            builds_commit = builds.commit()

            fx.link("FrontComposer", "references/Hexalith.Builds", "Builds", builds_commit)
            frontcomposer_commit = frontcomposer.commit()
            frontcomposer_identity = fx.identity("FrontComposer")
            fx.policy["semantic_profiles"][frontcomposer_identity] = profile_name
            fx.policy["profiles"][profile_name] = self._frontcomposer_profile()

            envelope = dg.collect_graph(
                frontcomposer.root,
                frontcomposer_identity,
                frontcomposer_commit,
                fx.policy,
            )
            return dg.evaluate_semantics(frontcomposer.root, fx.policy, envelope)

    def _assert_frontcomposer_profile_failure(self, catalog: bytes, expected_diagnostic: str, *, logging_target: bytes | None = None) -> str:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_frontcomposer_profile(catalog, logging_target=logging_target)

        message = str(ctx.exception)
        self.assertIn("github.com/test/frontcomposer@", message)
        self.assertIn("references/Hexalith.Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn(expected_diagnostic, message)
        return message

    def test_frontcomposer_profile_shape_contract_is_closed_and_value_independent(self) -> None:
        profile = self._frontcomposer_profile()
        property_names = [
            "HexalithCommonsVersion",
            "HexalithEventStoreVersion",
            "HexalithMemoriesVersion",
            "HexalithPartiesVersion",
            "HexalithPolymorphicSerializationsVersion",
            "HexalithTenantsVersion",
        ]
        self.assertEqual(
            set(profile),
            {
                "owner_checks",
                "selected_catalog_required_property_names",
                "selected_catalog_required_properties",
                "selected_catalog_required_packages",
            },
        )
        self.assertEqual(profile["selected_catalog_required_property_names"], property_names)
        self.assertEqual(profile["selected_catalog_required_properties"], {})
        dg.assert_profiles_well_formed({"profiles": {"frontcomposer-catalog-v1": profile}})

    @staticmethod
    def _catalog_xml_bytes(root: ET.Element) -> bytes:
        return b"\xef\xbb\xbf" + ET.tostring(root, encoding="utf-8").replace(b"\n", b"\r\n")

    def _eventstore_qualification_catalog(self) -> bytes:
        # Preserve the temporary upstream shape as a regression fixture after its removal.
        profile = self._frontcomposer_profile()
        root = ET.Element("Project")
        properties = ET.SubElement(root, "PropertyGroup")
        for name in profile["selected_catalog_required_property_names"]:
            ET.SubElement(properties, name, {"Condition": f"'$({name})' == ''"}).text = "1.2.3"
        condition = (
            "'$(CatalogCandidateTenantsVersion)' != '' and '$(UseHexalithProjectReferences)' != 'true' and ("
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.CatalogConstruction' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.Server.Tests' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.QueryRouting.Tests' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.Server.LiveSidecar.Tests' or "
            "'$(MSBuildProjectName)' == 'routing-catalog.cs')"
        )
        ET.SubElement(properties, "HexalithTenantsVersion", {"Condition": condition}).text = "$(CatalogCandidateTenantsVersion)"
        packages = ET.SubElement(root, "ItemGroup")
        for name, version in profile["selected_catalog_required_packages"].items():
            ET.SubElement(packages, "PackageVersion", {"Include": name, "Version": version})
        ET.indent(root)
        return self._catalog_xml_bytes(root)

    def _ordinary_frontcomposer_property_catalog(self) -> bytes:
        # These property-shape controls exercise ordinary single defaults, separate
        # from the bounded retained-role SDK/Memories qualification fixtures.
        root = ET.fromstring(self._eventstore_qualification_catalog())
        qualification = list(root.iter("HexalithTenantsVersion"))[1]
        root.find("PropertyGroup").remove(qualification)
        return self._catalog_xml_bytes(root)

    def test_frontcomposer_profile_accepts_exact_inactive_eventstore_qualification(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        self.assertEqual(self._evaluate_frontcomposer_profile(catalog)["selectors_validated"], 1)
        root = ET.fromstring(catalog)
        default, qualification = list(root.iter("HexalithTenantsVersion"))
        next(group for group in root if qualification in list(group)).remove(qualification)
        self.assertEqual(self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))["selectors_validated"], 1)

    def test_eventstore_qualification_remains_strict_for_other_profiles(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        with self.assertRaisesRegex(dg.GraphError, "must define exactly one version property"):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        with self.assertRaisesRegex(dg.GraphError, "must define exactly one version property"):
            dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithTenantsVersion", "strict helper")

    def test_frontcomposer_profile_rejects_changed_qualification_scope_or_value(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        mutations = [
            (b"'$(UseHexalithProjectReferences)' != 'true' and ", b""),
            (b"'$(CatalogCandidateTenantsVersion)' != ''", b"'$(OtherCandidate)' != ''"),
            (b"'routing-catalog.cs')", b"'routing-catalog.cs' or '$(MSBuildProjectName)' == 'Hexalith.FrontComposer.Shell')"),
            (b">$(CatalogCandidateTenantsVersion)</HexalithTenantsVersion>", b">5.10.2</HexalithTenantsVersion>"),
            (b">$(CatalogCandidateTenantsVersion)</HexalithTenantsVersion>", b">$(OtherVersion)</HexalithTenantsVersion>"),
            (b">$(CatalogCandidateTenantsVersion)</HexalithTenantsVersion>", b">$(CatalogCandidateTenantsVersion)<Unexpected /></HexalithTenantsVersion>"),
        ]
        for original, replacement in mutations:
            with self.subTest(mutation=replacement):
                self.assertEqual(catalog.count(original), 1)
                self._assert_frontcomposer_profile_failure(
                    catalog.replace(original, replacement, 1),
                    "HexalithTenantsVersion must define exactly one version property",
                )

    def test_frontcomposer_profile_rejects_scoped_or_duplicate_qualification(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        for placement in ("conditional", "target", "choose", "duplicate"):
            with self.subTest(placement=placement):
                root = ET.fromstring(catalog)
                default, qualification = list(root.iter("HexalithTenantsVersion"))
                parent = next(group for group in root if qualification in list(group))
                if placement == "duplicate":
                    parent.append(copy.deepcopy(qualification))
                else:
                    parent.remove(qualification)
                    if placement == "conditional":
                        group = ET.SubElement(root, "PropertyGroup", {"Condition": "'$(Other)' == 'true'"})
                    elif placement == "target":
                        group = ET.SubElement(ET.SubElement(root, "Target", {"Name": "Later"}), "PropertyGroup")
                    else:
                        group = ET.SubElement(ET.SubElement(ET.SubElement(root, "Choose"), "When", {"Condition": "'$(Other)' == 'true'"}), "PropertyGroup")
                    group.append(qualification)
                self._assert_frontcomposer_profile_failure(
                    self._catalog_xml_bytes(root),
                    "HexalithTenantsVersion must define exactly one version property",
                )

    def test_frontcomposer_profile_qualification_retains_literal_default_validation(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        self._assert_frontcomposer_profile_failure(
            self._replace_catalog_property_value(catalog, "HexalithTenantsVersion", "$(OtherVersion)"),
            "HexalithTenantsVersion must contain a literal NuGet version",
        )
        for duplicate in (True, False):
            with self.subTest(duplicate=duplicate):
                root = ET.fromstring(catalog)
                default, qualification = list(root.iter("HexalithTenantsVersion"))
                parent = next(group for group in root if default in list(group))
                if duplicate:
                    parent.append(copy.deepcopy(default))
                else:
                    parent.remove(default)
                self._assert_frontcomposer_profile_failure(
                    self._catalog_xml_bytes(root),
                    "HexalithTenantsVersion must define exactly one version property",
                )

    def _eventstore_sdk42_catalog(self) -> bytes:
        root = ET.fromstring(self._eventstore_qualification_catalog())
        properties = root.find("PropertyGroup")
        assert properties is not None
        declarations = []
        for selector, projects in (
            ("HexalithPartiesSdk42CatalogView", ("Hexalith.Parties", "Hexalith.Parties.Client")),
            ("HexalithBuildsPackagedHostSdk42CatalogView", ("Hexalith.Builds.Module.AppHost", "Hexalith.Builds.Module.EventStoreHost", "Hexalith.Builds.Module.UiHost")),
        ):
            default = ET.Element(selector)
            default.text = "false"
            enabled = ET.Element(selector, {"Condition": " or ".join(f"$(MSBuildProjectName.Equals('{name}'))" for name in projects)})
            enabled.text = "true"
            declarations.extend((default, enabled))
        override = ET.Element("HexalithEventStoreVersion", {"Condition": (
            "'$(HexalithEventStoreVersion)' == '' and ("
            "'$(HexalithPartiesSdk42CatalogView)' == 'true' or "
            "'$(HexalithBuildsPackagedHostSdk42CatalogView)' == 'true')"
        )})
        override.text = "4.2.0"
        declarations.append(override)
        for index, element in enumerate(declarations):
            properties.insert(index, element)
        list(root.iter("HexalithEventStoreVersion"))[1].text = "5.0.1"
        ET.indent(root)
        return self._catalog_xml_bytes(root)

    def test_frontcomposer_profile_accepts_proven_inactive_sdk42_and_current_or_future_default(self) -> None:
        catalog = self._eventstore_sdk42_catalog()
        for version in ("5.0.1", "6.0.0", "999.0.42"):
            with self.subTest(version=version):
                updated = self._replace_catalog_property_value(catalog, "HexalithEventStoreVersion", version)
                self.assertEqual(["4.2.0", version], [element.text for element in ET.fromstring(updated).iter("HexalithEventStoreVersion")])
                self.assertEqual(1, self._evaluate_frontcomposer_profile(updated)["selectors_validated"])
        # Removing an inactive compatibility row retains the ordinary one-default contract.
        root = ET.fromstring(catalog)
        properties = root.find("PropertyGroup")
        properties.remove(list(root.iter("HexalithEventStoreVersion"))[0])
        self.assertEqual(1, self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))["selectors_validated"])

    def test_sdk42_exception_is_frontcomposer_only(self) -> None:
        catalog = self._eventstore_sdk42_catalog()
        with self.assertRaisesRegex(dg.GraphError, "must define exactly one version property"):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        with self.assertRaisesRegex(dg.GraphError, "must define exactly one version property"):
            dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithEventStoreVersion", "strict helper")

    def test_sdk42_selectors_reject_changed_scope_or_nonliteral_conditions(self) -> None:
        for selector in ("HexalithPartiesSdk42CatalogView", "HexalithBuildsPackagedHostSdk42CatalogView"):
            for mutation in ("frontcomposer", "other-module", "starts-with", "dynamic-name", "and", "constant", "duplicate-clause", "empty"):
                with self.subTest(selector=selector, mutation=mutation):
                    root = ET.fromstring(self._eventstore_sdk42_catalog())
                    enabled = list(root.iter(selector))[1]
                    condition = enabled.get("Condition")
                    if mutation == "frontcomposer":
                        condition += " or $(MSBuildProjectName.Equals('Hexalith.FrontComposer.Shell'))"
                    elif mutation == "other-module":
                        condition += " or $(MSBuildProjectName.Equals('Hexalith.Tenants'))"
                    elif mutation == "starts-with":
                        condition = condition.replace(".Equals(", ".StartsWith(")
                    elif mutation == "dynamic-name":
                        condition = condition.replace("'Hexalith.", "'$(Other)Hexalith.", 1)
                    elif mutation == "and":
                        condition = condition.replace(" or ", " and ", 1)
                    elif mutation == "constant":
                        condition = "true"
                    elif mutation == "duplicate-clause":
                        condition += " or " + condition.split(" or ")[0]
                    else:
                        condition = ""
                    enabled.set("Condition", condition)
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "HexalithEventStoreVersion SDK4.2 selector")

    def test_sdk42_selectors_reject_missing_duplicate_or_reassigned_definitions(self) -> None:
        for selector in ("HexalithPartiesSdk42CatalogView", "HexalithBuildsPackagedHostSdk42CatalogView"):
            for mutation in ("missing-false", "missing-true", "duplicate-false", "duplicate-true", "reassigned", "case-alias"):
                with self.subTest(selector=selector, mutation=mutation):
                    root = ET.fromstring(self._eventstore_sdk42_catalog())
                    properties = root.find("PropertyGroup")
                    default, enabled = list(root.iter(selector))
                    if mutation.startswith("missing"):
                        properties.remove(default if mutation == "missing-false" else enabled)
                    elif mutation.startswith("duplicate"):
                        properties.append(copy.deepcopy(default if mutation == "duplicate-false" else enabled))
                    else:
                        reassigned = ET.SubElement(properties, selector if mutation == "reassigned" else selector.lower())
                        reassigned.text = "true"
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "requires exactly two canonical declarations")

    def test_sdk42_selectors_reject_changed_assignment_shape(self) -> None:
        for selector in ("HexalithPartiesSdk42CatalogView", "HexalithBuildsPackagedHostSdk42CatalogView"):
            for mutation in ("true-default", "conditional-default", "default-child", "false-enabled", "enabled-child", "extra-attribute"):
                with self.subTest(selector=selector, mutation=mutation):
                    root = ET.fromstring(self._eventstore_sdk42_catalog())
                    default, enabled = list(root.iter(selector))
                    if mutation == "true-default":
                        default.text = "true"
                    elif mutation == "conditional-default":
                        default.set("Condition", "'$(Other)' == ''")
                    elif mutation == "default-child":
                        ET.SubElement(default, "Unexpected")
                    elif mutation == "false-enabled":
                        enabled.text = "false"
                    elif mutation == "enabled-child":
                        ET.SubElement(enabled, "Unexpected")
                    else:
                        enabled.set("Label", "unknown")
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "HexalithEventStoreVersion SDK4.2 selector")

    def test_sdk42_override_rejects_changed_value_condition_or_content(self) -> None:
        for mutation in ("other-version", "dynamic-version", "remove-gate", "frontcomposer", "child", "extra-attribute", "duplicate"):
            with self.subTest(mutation=mutation):
                root = ET.fromstring(self._eventstore_sdk42_catalog())
                override = list(root.iter("HexalithEventStoreVersion"))[0]
                if mutation == "other-version":
                    override.text = "4.2.1"
                elif mutation == "dynamic-version":
                    override.text = "$(Other)"
                elif mutation == "remove-gate":
                    override.set("Condition", "'$(HexalithEventStoreVersion)' == ''")
                elif mutation == "frontcomposer":
                    override.set("Condition", override.get("Condition") + " or '$(MSBuildProjectName)' == 'Hexalith.FrontComposer.Shell'")
                elif mutation == "child":
                    ET.SubElement(override, "Unexpected")
                elif mutation == "extra-attribute":
                    override.set("Label", "unknown")
                else:
                    root.find("PropertyGroup").append(copy.deepcopy(override))
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "must define exactly one version property")

    def test_sdk42_selector_and_override_placement_must_be_global_unconditional_and_ordered(self) -> None:
        for name, ordinal in (("HexalithPartiesSdk42CatalogView", 0), ("HexalithPartiesSdk42CatalogView", 1),
                              ("HexalithBuildsPackagedHostSdk42CatalogView", 0), ("HexalithBuildsPackagedHostSdk42CatalogView", 1),
                              ("HexalithEventStoreVersion", 0)):
            for placement in ("conditional", "target", "choose"):
                with self.subTest(name=name, ordinal=ordinal, placement=placement):
                    root = ET.fromstring(self._eventstore_sdk42_catalog())
                    element = list(root.iter(name))[ordinal]
                    root.find("PropertyGroup").remove(element)
                    if placement == "conditional":
                        group = ET.SubElement(root, "PropertyGroup", {"Condition": "'$(Other)' == 'true'"})
                    elif placement == "target":
                        group = ET.SubElement(ET.SubElement(root, "Target", {"Name": "Later"}), "PropertyGroup")
                    else:
                        group = ET.SubElement(ET.SubElement(ET.SubElement(root, "Choose"), "When", {"Condition": "'$(Other)' == 'true'"}), "PropertyGroup")
                    group.append(element)
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "unconditional top-level PropertyGroup")
        for mutation in ("false-after-true", "selector-after-override", "default-before-override"):
            with self.subTest(order=mutation):
                root = ET.fromstring(self._eventstore_sdk42_catalog())
                properties = root.find("PropertyGroup")
                if mutation == "false-after-true":
                    element = list(root.iter("HexalithPartiesSdk42CatalogView"))[0]
                    properties.remove(element)
                    properties.insert(2, element)
                elif mutation == "selector-after-override":
                    element = list(root.iter("HexalithBuildsPackagedHostSdk42CatalogView"))[1]
                    properties.remove(element)
                    properties.append(element)
                else:
                    element = list(root.iter("HexalithEventStoreVersion"))[1]
                    properties.remove(element)
                    properties.insert(0, element)
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "HexalithEventStoreVersion")

    def test_sdk42_selector_proof_rejects_imports_anywhere_in_catalog(self) -> None:
        for import_group in (False, True):
            for placement in ("before", "between", "after-override", "after-default"):
                with self.subTest(import_group=import_group, placement=placement):
                    root = ET.fromstring(self._eventstore_sdk42_catalog())
                    properties = root.find("PropertyGroup")
                    children = list(properties)
                    split = {"before": 0, "between": 2, "after-override": 5, "after-default": len(children)}[placement]
                    root.remove(properties)
                    before, after = ET.Element("PropertyGroup"), ET.Element("PropertyGroup")
                    before.extend(children[:split])
                    after.extend(children[split:])
                    imported = ET.Element("Import", {"Project": "untrusted-selectors.props"})
                    if import_group:
                        group = ET.Element("ImportGroup")
                        group.append(imported)
                        imported = group
                    root.insert(0, before)
                    root.insert(1, imported)
                    root.insert(2, after)
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "rejects unsupported, grouped or misplaced catalog imports")
        # The retained-row exception adds no new rule to an ordinary single default.
        root = ET.fromstring(self._eventstore_sdk42_catalog())
        root.find("PropertyGroup").remove(list(root.iter("HexalithEventStoreVersion"))[0])
        ET.SubElement(root, "Import", {"Project": "ordinary-catalog.props"})
        self.assertEqual(1, self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))["selectors_validated"])

    @staticmethod
    def _analyzer_logging_target() -> bytes:
        return b"""<Project>
  <Target Name="HexalithRemoveReplacedLoggingSourceGenerator" AfterTargets="ResolveReferences" Condition="'$(DisableMicrosoftExtensionsLoggingSourceGenerator)' == 'true'">
    <ItemGroup>
      <_HexalithTelemetryLoggingGenerator Include="@(Analyzer)" Condition="'%(Analyzer.Filename)' == 'Microsoft.Gen.Logging'" />
      <Analyzer Remove="@(Analyzer)" Condition="'@(_HexalithTelemetryLoggingGenerator)' != '' and '%(Analyzer.Filename)' == 'Microsoft.Extensions.Logging.Generators'" />
    </ItemGroup>
  </Target>
</Project>
"""

    def _sdk42_logging_import_catalog(self) -> bytes:
        root = ET.fromstring(self._eventstore_sdk42_catalog())
        ET.SubElement(root, "Import", {
            "Project": "$(MSBuildThisFileDirectory)Logging.Build.targets",
            "Condition": "Exists('$(MSBuildThisFileDirectory)Logging.Build.targets')",
        })
        return self._catalog_xml_bytes(root)

    def test_sdk42_optional_logging_import_accepts_absence_and_committed_analyzer_target(self) -> None:
        catalog = self._sdk42_logging_import_catalog()
        target = self._analyzer_logging_target()
        for content in (None, target, target.replace(b"<Project>", b"<Project><!-- formatting is not identity -->")):
            with self.subTest(content=content):
                self.assertEqual(1, self._evaluate_frontcomposer_profile(catalog, logging_target=content)["selectors_validated"])
        with self.assertRaisesRegex(dg.GraphError, "requires its exact committed Builds source"):
            dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithEventStoreVersion", "offline import", allow_eventstore_qualification=True)

    def test_sdk42_optional_logging_import_rejects_changed_shape_or_placement(self) -> None:
        for mutation in ("duplicate", "grouped", "before-default", "inside-properties", "unknown-project", "changed-condition", "missing-condition", "extra-attribute", "child"):
            with self.subTest(mutation=mutation):
                root = ET.fromstring(self._sdk42_logging_import_catalog())
                imported = root.find("Import")
                if mutation == "duplicate":
                    root.append(copy.deepcopy(imported))
                elif mutation in {"grouped", "before-default", "inside-properties"}:
                    root.remove(imported)
                    if mutation == "grouped":
                        ET.SubElement(root, "ImportGroup").append(imported)
                    elif mutation == "before-default":
                        root.insert(0, imported)
                    else:
                        root.find("PropertyGroup").append(imported)
                elif mutation == "unknown-project":
                    imported.set("Project", "$(MSBuildThisFileDirectory)Other.targets")
                elif mutation == "changed-condition":
                    imported.set("Condition", "true")
                elif mutation == "missing-condition":
                    imported.attrib.pop("Condition")
                elif mutation == "extra-attribute":
                    imported.set("Label", "unknown")
                else:
                    ET.SubElement(imported, "Unexpected")
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "rejects unsupported, grouped or misplaced catalog imports")

    def test_sdk42_committed_logging_target_rejects_properties_imports_tasks_or_shape_drift(self) -> None:
        catalog = self._sdk42_logging_import_catalog()
        for mutation in ("property", "case-alias", "import", "task", "extra-target", "target-attribute", "changed-item", "text", "deep-content", "malformed"):
            with self.subTest(mutation=mutation):
                root = ET.fromstring(self._analyzer_logging_target())
                target = root.find("Target")
                if mutation in {"property", "case-alias"}:
                    name = "HexalithEventStoreVersion" if mutation == "property" else "hexalitheventstoreversion"
                    ET.SubElement(ET.SubElement(root, "PropertyGroup"), name).text = "4.2.0"
                elif mutation == "import":
                    ET.SubElement(root, "Import", {"Project": "other.targets"})
                elif mutation == "task":
                    ET.SubElement(target, "Exec", {"Command": "must-never-execute"})
                elif mutation == "extra-target":
                    root.append(copy.deepcopy(target))
                elif mutation == "target-attribute":
                    target.set("BeforeTargets", "Restore")
                elif mutation == "changed-item":
                    target.find("ItemGroup/Analyzer").set("Remove", "@(PackageReference)")
                elif mutation == "text":
                    target.text = "unknown-content"
                if mutation == "deep-content":
                    content = b"<Project>" + b"<Unexpected>" * 1500 + b"</Unexpected>" * 1500 + b"</Project>"
                else:
                    content = b"<Project>" if mutation == "malformed" else ET.tostring(root, encoding="utf-8")
                self._assert_frontcomposer_profile_failure(catalog, "malformed XML" if mutation == "malformed" else "closed analyzer-only XML shape", logging_target=content)

    def test_sdk42_logging_target_modes_and_read_budget_are_enforced(self) -> None:
        catalog = ET.fromstring(self._sdk42_logging_import_catalog())
        for mode in ("100644", "100755", "120000", "160000"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory(dir=self.tmp_path) as temporary:
                repository = TempGitRepo(pathlib.Path(temporary))
                repository.write_bytes("Props/Directory.Packages.props", self._sdk42_logging_import_catalog())
                repository.write_bytes("Props/Logging.Build.targets", self._analyzer_logging_target())
                first = repository.commit("test: seed committed logging source")
                blob_id = _run(["rev-parse", f"{first}:Props/Logging.Build.targets"], repository.root).decode().strip()
                if mode != "100644":
                    _run(["update-index", "--cacheinfo", f"{mode},{first if mode == '160000' else blob_id},Props/Logging.Build.targets"], repository.root)
                    selected = repository.commit("test: select logging source mode")
                else:
                    selected = first
                source = (repository.root, selected, 4_194_304)
                if mode in {"100644", "100755"}:
                    dg.assert_selected_catalog_property_shape(catalog, "HexalithEventStoreVersion", "mode proof", allow_eventstore_qualification=True, catalog_source=source)
                    with self.assertRaisesRegex(dg.GraphError, "exceeds the 64-byte ceiling"):
                        dg.assert_selected_catalog_property_shape(catalog, "HexalithEventStoreVersion", "bounded proof", allow_eventstore_qualification=True, catalog_source=(repository.root, selected, 64))
                else:
                    with self.assertRaisesRegex(dg.GraphError, "must be a regular committed blob"):
                        dg.assert_selected_catalog_property_shape(catalog, "HexalithEventStoreVersion", "mode proof", allow_eventstore_qualification=True, catalog_source=source)

    def test_sdk42_logging_target_uses_exact_commit_and_ignores_head_index_and_worktree(self) -> None:
        catalog = self._sdk42_logging_import_catalog()
        with tempfile.TemporaryDirectory(dir=self.tmp_path) as temporary:
            fx = GraphFixture(pathlib.Path(temporary))
            frontcomposer = fx.add_repo("FrontComposer")
            frontcomposer.write_text("Directory.Packages.props", OWNER_SHIM)
            builds = fx.add_repo("Builds")
            builds.write_text(".gitattributes", "Props/Directory.Packages.props text eol=crlf\n")
            builds.write_bytes("Props/Directory.Packages.props", catalog)
            absent = builds.commit("test: record absent optional logging target")
            hostile = b"<Project><PropertyGroup><HexalithEventStoreVersion>4.2.0</HexalithEventStoreVersion></PropertyGroup></Project>"
            builds.write_bytes("Props/Logging.Build.targets", hostile)
            dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithEventStoreVersion", "absent committed source", allow_eventstore_qualification=True, catalog_source=(builds.root, absent, 4_194_304))
            bad_commit = builds.commit("test: commit hostile logging property")
            builds.write_bytes("Props/Logging.Build.targets", self._analyzer_logging_target())
            with self.assertRaisesRegex(dg.GraphError, "closed analyzer-only XML shape"):
                dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithEventStoreVersion", "committed hostile source", allow_eventstore_qualification=True, catalog_source=(builds.root, bad_commit, 4_194_304))
            good_commit = builds.commit("test: commit closed logging analyzer target")
            fx.link("FrontComposer", "references/Hexalith.Builds", "Builds", good_commit)
            owner_commit = frontcomposer.commit("test: select closed logging source")
            owner = fx.identity("FrontComposer")
            fx.policy["semantic_profiles"][owner] = "frontcomposer-catalog-v1"
            fx.policy["profiles"]["frontcomposer-catalog-v1"] = self._frontcomposer_profile()
            graph = dg.collect_graph(frontcomposer.root, owner, owner_commit, fx.policy)
            builds.write_bytes("Props/Logging.Build.targets", hostile)
            builds.commit("test: advance ambient logging source")
            self.assertEqual(1, dg.evaluate_semantics(frontcomposer.root, fx.policy, graph)["selectors_validated"])
            (builds.root / "Props/Logging.Build.targets").write_bytes(self._analyzer_logging_target())
            with self.assertRaisesRegex(dg.GraphError, "closed analyzer-only XML shape"):
                dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithEventStoreVersion", "hostile head with clean worktree", allow_eventstore_qualification=True, catalog_source=(builds.root, builds.head(), 4_194_304))

    def test_sdk42_proof_rejects_case_insensitive_version_aliases(self) -> None:
        for spelling in ("hexalitheventstoreversion", "HexalithEventstoreVersion", "HEXALITHEVENTSTOREVERSION"):
            for placement in ("before", "after", "replace-default", "rename-override", "unconditional-override"):
                with self.subTest(spelling=spelling, placement=placement):
                    root = ET.fromstring(self._eventstore_sdk42_catalog())
                    properties = root.find("PropertyGroup")
                    if placement == "replace-default":
                        list(root.iter("HexalithEventStoreVersion"))[1].tag = spelling
                    elif placement in {"rename-override", "unconditional-override"}:
                        override = list(root.iter("HexalithEventStoreVersion"))[0]
                        override.tag = spelling
                        if placement == "unconditional-override":
                            override.attrib.clear()
                    else:
                        alias = ET.Element(spelling)
                        alias.text = "4.2.0"
                        properties.insert(0, alias) if placement == "before" else properties.append(alias)
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "rejects case-insensitive version aliases")

    def test_sdk42_mutations_target_unconditional_and_whitespace_self_defaults(self) -> None:
        for condition in (None, "  '$(HexalithEventStoreVersion)'   ==   ''  ", "\t'$(HexalithEventStoreVersion)'\t==\t''\t"):
            with self.subTest(condition=condition):
                root = ET.fromstring(self._eventstore_sdk42_catalog())
                default = list(root.iter("HexalithEventStoreVersion"))[1]
                default.attrib.clear()
                if condition is not None:
                    default.set("Condition", condition)
                catalog = self._catalog_xml_bytes(root)
                self.assertEqual(1, self._evaluate_frontcomposer_profile(catalog)["selectors_validated"])
                updated = self._replace_catalog_property_value(catalog, "HexalithEventStoreVersion", "6.0.0")
                self.assertEqual(["4.2.0", "6.0.0"], [element.text for element in ET.fromstring(updated).iter("HexalithEventStoreVersion")])
                self.assertEqual(1, self._evaluate_frontcomposer_profile(updated)["selectors_validated"])
                removed = self._replace_catalog_property_element(catalog, "HexalithEventStoreVersion", b"")
                self.assertEqual(["4.2.0"], [element.text for element in ET.fromstring(removed).iter("HexalithEventStoreVersion")])
                self._assert_frontcomposer_profile_failure(removed, "must define exactly one version property")
                invalid = self._replace_catalog_property_value(catalog, "HexalithEventStoreVersion", "$(Other)")
                self._assert_frontcomposer_profile_failure(invalid, "must contain a literal NuGet version")

    def test_sdk42_override_retains_canonical_default_validation_and_mutation_target(self) -> None:
        catalog = self._eventstore_sdk42_catalog()
        missing = self._replace_catalog_property_element(catalog, "HexalithEventStoreVersion", b"")
        self.assertEqual(["4.2.0"], [element.text for element in ET.fromstring(missing).iter("HexalithEventStoreVersion")])
        self._assert_frontcomposer_profile_failure(missing, "must define exactly one version property")
        for value in ("", "$(OtherVersion)", "1.2.3<Unexpected />"):
            with self.subTest(value=value):
                self._assert_frontcomposer_profile_failure(
                    self._replace_catalog_property_value(catalog, "HexalithEventStoreVersion", value),
                    "must contain a literal NuGet version",
                )
        duplicate = self._replace_catalog_property_element(catalog, "HexalithEventStoreVersion",
            b"<HexalithEventStoreVersion>5.0.1</HexalithEventStoreVersion>" * 2)
        self._assert_frontcomposer_profile_failure(duplicate, "must define exactly one version property")

    def _current_sdk42_catalog(self) -> bytes:
        root = ET.fromstring(self._eventstore_qualification_catalog())
        properties = root.find("PropertyGroup")
        assert properties is not None
        for name in ("HexalithEventStoreVersion", "HexalithMemoriesVersion"):
            for element in list(root.iter(name)):
                properties.remove(element)
        fixture = ET.fromstring((ROOT / "tests/ci-governance/fixtures/builds-sdk42-current-qualification.props").read_bytes())
        for index, element in enumerate(fixture.find("PropertyGroup")):
            properties.insert(index, copy.deepcopy(element))
        ET.indent(root)
        return self._catalog_xml_bytes(root)

    def test_current_sdk42_accepts_exact_subjects_and_defaults(self) -> None:
        catalog = self._current_sdk42_catalog()
        self.assertEqual(1, self._evaluate_frontcomposer_profile(catalog)["selectors_validated"])
        root = ET.fromstring(catalog)
        self.assertEqual(["4.2.0", "5.1.0"], [element.text for element in root.iter("HexalithEventStoreVersion")])
        self.assertEqual(["3.0.27", "4.0.0"], [element.text for element in root.iter("HexalithMemoriesVersion")])
        # Without compatibility rows, the existing ordinary default contract still applies.
        for name in ("HexalithEventStoreVersion", "HexalithMemoriesVersion"):
            root.find("PropertyGroup").remove(list(root.iter(name))[0])
        self.assertEqual(1, self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))["selectors_validated"])

    def test_current_sdk42_remains_strict_for_other_profiles_and_properties(self) -> None:
        catalog = self._current_sdk42_catalog()
        with self.assertRaises(dg.GraphError):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        for name in ("HexalithEventStoreVersion", "HexalithMemoriesVersion"):
            with self.subTest(property=name), self.assertRaises(dg.GraphError):
                dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), name, "strict helper")
        root = ET.fromstring(catalog)
        ET.SubElement(root.find("PropertyGroup"), "HexalithCommonsVersion").text = "3.0.0"
        self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "HexalithCommonsVersion must define exactly one")

    def test_current_sdk42_rejects_unknown_future_case_or_expression_subjects(self) -> None:
        names = ("HexalithPartiesSdk42CatalogView", "HexalithPlatformSdk42CatalogView", "HexalithBuildsPackagedHostSdk42CatalogView")
        for name in names:
            for change in ("future", "remove", "lowercase", "duplicate", "frontcomposer", "other-module", "prefix", "dynamic", "constant"):
                with self.subTest(selector=name, change=change):
                    root = ET.fromstring(self._current_sdk42_catalog())
                    enabled = list(root.iter(name))[1]
                    clauses = re.split(r"\s+or\s+", enabled.attrib["Condition"].strip())
                    if change == "future":
                        clauses.append("$(MSBuildProjectName.Equals('Hexalith.Platform.Future'))")
                    elif change == "remove":
                        clauses.pop()
                    elif change == "lowercase":
                        clauses[0] = clauses[0].lower()
                    elif change == "duplicate":
                        clauses.append(clauses[0])
                    elif change == "frontcomposer":
                        clauses[0] = "$(MSBuildProjectName.Equals('Hexalith.FrontComposer.Shell'))"
                    elif change == "other-module":
                        clauses[0] = "$(MSBuildProjectName.Equals('Hexalith.EventStore.Host'))"
                    elif change == "prefix":
                        clauses = ["$(MSBuildProjectName.StartsWith('Hexalith.Platform'))"]
                    elif change == "dynamic":
                        clauses = ["$(MSBuildProjectName.Equals('$(SelectedProject)'))"]
                    else:
                        clauses = ["true"]
                    enabled.set("Condition", " or ".join(clauses))
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "current SDK4.2/Memories3.0.27 qualification selector")

    def test_current_sdk42_rejects_selector_writer_shape_and_placement_changes(self) -> None:
        for name in ("HexalithPartiesSdk42CatalogView", "HexalithPlatformSdk42CatalogView", "HexalithBuildsPackagedHostSdk42CatalogView"):
            for change in ("missing-default", "duplicate", "true-default", "false-enabled", "case-alias", "conditional", "target", "order"):
                with self.subTest(selector=name, change=change):
                    root = ET.fromstring(self._current_sdk42_catalog())
                    properties = root.find("PropertyGroup")
                    default, enabled = list(root.iter(name))
                    if change == "missing-default":
                        properties.remove(default)
                    elif change == "duplicate":
                        properties.append(copy.deepcopy(enabled))
                    elif change == "true-default":
                        default.text = "true"
                    elif change == "false-enabled":
                        enabled.text = "false"
                    elif change == "case-alias":
                        enabled.tag = name.lower()
                    elif change == "conditional":
                        properties.remove(enabled)
                        ET.SubElement(root, "PropertyGroup", {"Condition": "true"}).append(enabled)
                    elif change == "target":
                        properties.remove(enabled)
                        ET.SubElement(root, "Target", {"Name": "HiddenWriter"}).append(enabled)
                    else:
                        properties.remove(default)
                        properties.append(default)
                    self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "current SDK4.2/Memories3.0.27 qualification")

    def test_current_sdk42_rejects_partial_cohorts_versions_aliases_and_gates(self) -> None:
        for name in ("HexalithEventStoreVersion", "HexalithMemoriesVersion"):
            for change in ("missing-override", "duplicate", "wrong-retained", "wrong-default", "dynamic", "case-alias", "remove-gate", "order"):
                with self.subTest(property=name, change=change):
                    root = ET.fromstring(self._current_sdk42_catalog())
                    properties = root.find("PropertyGroup")
                    override, default = list(root.iter(name))
                    if change == "missing-override":
                        properties.remove(override)
                    elif change == "duplicate":
                        properties.append(copy.deepcopy(override))
                    elif change == "wrong-retained":
                        override.text = "4.2.1" if name == "HexalithEventStoreVersion" else "3.0.28"
                    elif change == "wrong-default":
                        default.text = "4.2.0" if name == "HexalithEventStoreVersion" else "3.0.27"
                    elif change == "dynamic":
                        override.text = "$(UnreviewedVersion)"
                    elif change == "case-alias":
                        default.tag = name.lower()
                    elif change == "remove-gate":
                        override.set("Condition", override.attrib["Condition"].replace("'$(HexalithPlatformSdk42CatalogView)' == 'true' or ", ""))
                    else:
                        properties.remove(default)
                        properties.insert(0, default)
                    with self.assertRaises(dg.GraphError) as failure:
                        self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))
                    self.assertRegex(str(failure.exception), "SDK4.2|HexalithEventStoreVersion|HexalithMemoriesVersion")

    def test_current_sdk42_rejects_hidden_imports_and_root_overrides(self) -> None:
        for change in ("root-condition", "local-property", "import", "grouped-import",
                       "target", "sdk-output", "selector-output", "using-task", "unparented-output",
                       "target-alias", "using-task-alias", "output-alias",
                       "nested-target", "nested-using-task", "nested-output"):
            with self.subTest(change=change):
                root = ET.fromstring(self._current_sdk42_catalog())
                if change == "root-condition":
                    root.set("Condition", "true")
                elif change == "local-property":
                    root.set("TreatAsLocalProperty", "HexalithEventStoreVersion")
                elif change == "import":
                    ET.SubElement(root, "Import", {"Project": "unreviewed.props"})
                elif change == "grouped-import":
                    ET.SubElement(ET.SubElement(root, "ImportGroup"), "Import", {"Project": "unreviewed.props"})
                elif change in ("using-task", "using-task-alias", "nested-using-task"):
                    parent = ET.SubElement(root, "PropertyGroup") if change == "nested-using-task" else root
                    tag = "usingtask" if change == "using-task-alias" else "UsingTask"
                    ET.SubElement(parent, tag, {"TaskName": "RewriteVersion", "AssemblyFile": "unreviewed.dll"})
                elif change in ("unparented-output", "output-alias", "nested-output"):
                    parent = ET.SubElement(root, "PropertyGroup") if change == "nested-output" else root
                    tag = "output" if change == "output-alias" else "Output"
                    ET.SubElement(parent, tag, {"TaskParameter": "Value", "PropertyName": "HexalithEventStoreVersion"})
                else:
                    parent = ET.SubElement(root, "ItemGroup") if change == "nested-target" else root
                    tag = "target" if change == "target-alias" else "Target"
                    target = ET.SubElement(parent, tag, {"Name": "RewriteVersion", "BeforeTargets": "CollectPackageReferences"})
                    if change in ("sdk-output", "selector-output"):
                        task = ET.SubElement(target, "CreateProperty", {"Value": "4.2.0" if change == "sdk-output" else "true"})
                        name = "HexalithEventStoreVersion" if change == "sdk-output" else "HexalithPlatformSdk42CatalogView"
                        ET.SubElement(task, "Output", {"TaskParameter": "Value", "PropertyName": name})
                expected = "conditional group" if change == "root-condition" else "current SDK4.2/Memories3.0.27 qualification"
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), expected)

    def _current_tenants8_catalog(self) -> bytes:
        root = ET.fromstring(self._current_sdk42_catalog())
        properties = root.find("PropertyGroup")
        for element in list(root.iter("HexalithTenantsVersion")):
            properties.remove(element)
        fixture = ET.fromstring((ROOT / "tests/ci-governance/fixtures/builds-sdk42-tenants8-qualification.props").read_bytes())
        for element in fixture.find("PropertyGroup"):
            properties.append(copy.deepcopy(element))
        ET.indent(root)
        return self._catalog_xml_bytes(root)

    def test_current_tenants8_accepts_exact_complementary_rows_only_in_frontcomposer_profile(self) -> None:
        catalog = self._current_tenants8_catalog()
        self.assertEqual(1, self._evaluate_frontcomposer_profile(catalog)["selectors_validated"])
        self.assertEqual(["8.0.0", "7.0.7"], [element.text for element in ET.fromstring(catalog).iter("HexalithTenantsVersion")])
        with self.assertRaises(dg.GraphError):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        with self.assertRaises(dg.GraphError):
            dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithTenantsVersion", "strict helper")

    def test_current_tenants8_rejects_changed_or_broadened_conditions(self) -> None:
        for index in (0, 1):
            for change in ("sdk", "selector", "unset", "unconditional", "broaden", "attribute-case"):
                with self.subTest(row=index, change=change):
                    root = ET.fromstring(self._current_tenants8_catalog())
                    row = list(root.iter("HexalithTenantsVersion"))[index]
                    condition = row.get("Condition")
                    if change == "sdk":
                        row.set("Condition", condition.replace("4.2.0", "4.2.1"))
                    elif change == "selector":
                        row.set("Condition", condition.replace("HexalithPlatformSdk42CatalogView", "UnreviewedSdk42CatalogView"))
                    elif change == "unset":
                        row.set("Condition", condition.replace("== ''", "!= ''", 1))
                    elif change == "unconditional":
                        row.attrib.clear()
                    elif change == "attribute-case":
                        row.attrib = {"condition": condition}
                    else:
                        row.set("Condition", condition + " or '$(MSBuildProjectName)' == 'Hexalith.FrontComposer.Shell'")
                    with self.assertRaises(dg.GraphError):
                        self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_current_tenants8_rejects_version_declaration_placement_or_import_drift(self) -> None:
        for change in ("missing-current", "missing-retained", "duplicate", "current-version", "retained-version", "dynamic", "alias", "nested", "conditional", "target", "reversed", "before-memories", "import-before-tenants"):
            with self.subTest(change=change):
                root = ET.fromstring(self._current_tenants8_catalog())
                group = root.find("PropertyGroup")
                current, retained = list(root.iter("HexalithTenantsVersion"))
                if change == "missing-current": group.remove(current)
                elif change == "missing-retained": group.remove(retained)
                elif change == "duplicate": group.append(copy.deepcopy(retained))
                elif change == "current-version": current.text = "8.0.1"
                elif change == "retained-version": retained.text = "7.0.8"
                elif change == "dynamic": current.text = "$(UnreviewedVersion)"
                elif change == "alias": retained.tag = "hexalithtenantsversion"
                elif change == "nested": ET.SubElement(current, "HiddenValue")
                elif change == "conditional":
                    group.remove(current)
                    ET.SubElement(root, "PropertyGroup", {"Condition": "true"}).append(current)
                elif change == "target":
                    group.remove(retained)
                    ET.SubElement(ET.SubElement(root, "Target", {"Name": "Later"}), "PropertyGroup").append(retained)
                elif change == "reversed":
                    group.remove(current)
                    group.append(current)
                elif change == "before-memories":
                    group.remove(current)
                    group.insert(0, current)
                else:
                    group.remove(current)
                    group.remove(retained)
                    ET.SubElement(root, "Import", {"Project": "$(MSBuildThisFileDirectory)Logging.Build.targets", "Condition": "Exists('$(MSBuildThisFileDirectory)Logging.Build.targets')"})
                    later = ET.SubElement(root, "PropertyGroup")
                    later.extend([current, retained])
                with self.assertRaises(dg.GraphError):
                    self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_current_tenants8_requires_same_closed_sdk42_and_memories_proof(self) -> None:
        for change in ("sdk-default", "missing-sdk", "missing-memories", "frontcomposer-enrollment", "output"):
            with self.subTest(change=change):
                root = ET.fromstring(self._current_tenants8_catalog())
                if change == "sdk-default": list(root.iter("HexalithEventStoreVersion"))[1].text = "6.0.0"
                elif change == "missing-sdk": root.find("PropertyGroup").remove(list(root.iter("HexalithEventStoreVersion"))[0])
                elif change == "missing-memories": root.find("PropertyGroup").remove(list(root.iter("HexalithMemoriesVersion"))[0])
                elif change == "frontcomposer-enrollment":
                    enabled = list(root.iter("HexalithPlatformSdk42CatalogView"))[1]
                    enabled.set("Condition", enabled.get("Condition") + " or $(MSBuildProjectName.Equals('Hexalith.FrontComposer.Shell'))")
                else:
                    ET.SubElement(root, "Output", {"TaskParameter": "Value", "PropertyName": "HexalithTenantsVersion"})
                with self.assertRaises(dg.GraphError):
                    self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))
                with self.assertRaises(dg.GraphError):
                    dg.assert_selected_catalog_property_shape(root, "HexalithTenantsVersion", "shared qualification control", allow_eventstore_qualification=True)

    def _immutable_consumer_catalog(self) -> bytes:
        root = ET.fromstring(self._eventstore_qualification_catalog())
        properties = root.find("PropertyGroup")
        properties.remove(list(root.iter("HexalithTenantsVersion"))[1])  # Isolate this view from the unrelated qualification override.
        # Keep the legacy selector's hostile-shape cases independent of whether the
        # current shared catalog still needs that compatibility view.
        selector = "HexalithFrontComposerImmutable10CatalogView"
        ET.SubElement(properties, selector).text = "false"
        condition = " or ".join("$(MSBuildProjectName.Equals('" + name + "'))"
                                for name in sorted(dg._FRONTCOMPOSER_IMMUTABLE_PROJECTS))
        ET.SubElement(properties, selector, {"Condition": condition}).text = "true"
        ET.SubElement(properties, "HexalithImmutableVersion").text = "11.0.0"
        ET.SubElement(properties, "HexalithImmutableVersion", {"Condition": "'$(" + selector + ")' == 'true'"}).text = (
            self._frontcomposer_profile()["selected_catalog_required_packages"]["System.Collections.Immutable"])
        next(row for row in root.iter("PackageVersion") if row.get("Include") == "System.Collections.Immutable").set("Version", "$(HexalithImmutableVersion)")
        return self._catalog_xml_bytes(root)

    def test_immutable_consumer_view_satisfies_existing_policy_without_normalization(self) -> None:
        catalog = self._immutable_consumer_catalog()
        self.assertEqual(1, self._evaluate_frontcomposer_profile(catalog)["selectors_validated"])
        self.assertEqual("$(HexalithImmutableVersion)", next(row for row in ET.fromstring(catalog).iter("PackageVersion") if row.get("Include") == "System.Collections.Immutable").get("Version"))
        # Other profiles still reject dynamic package authority.
        with self.assertRaisesRegex(dg.GraphError, "literal NuGet version"):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        # The authored candidate's real package pins/import are checked without a rewrite.
        actual = (ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props").read_bytes()
        self.assertEqual(1, self._evaluate_frontcomposer_profile(actual, logging_target=self._analyzer_logging_target())["selectors_validated"])

    def test_immutable_consumer_scope_matches_tracked_root_project_inventory(self) -> None:
        tracked = set(_run(["ls-files", "-z", "--", "*.csproj"], ROOT).decode("utf-8").rstrip("\0").split("\0"))
        archived = {
            "samples/AdopterProofKit/v1/AdopterProofKit.Domain/AdopterProofKit.Domain.csproj",
            "samples/AdopterProofKit/v1/AdopterProofKit.Web/AdopterProofKit.Web.csproj",
        }
        self.assertTrue(archived <= tracked)
        admitted_paths = tracked - archived
        inventory = {pathlib.PurePosixPath(path).stem for path in admitted_paths}
        self.assertEqual(len(admitted_paths), len(inventory), "Project names must identify a single tracked consumer")
        self.assertEqual(inventory, dg._FRONTCOMPOSER_IMMUTABLE_PROJECTS)
        catalog = ET.fromstring((ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props").read_bytes())
        selectors = list(catalog.iter("HexalithFrontComposerImmutable10CatalogView"))
        if not selectors:
            expected = self._frontcomposer_profile()["selected_catalog_required_packages"]["System.Collections.Immutable"]
            dg.assert_frontcomposer_immutable_version(catalog, expected, "current literal consumer catalog", None)
            self.assertEqual([], list(catalog.iter("HexalithImmutableVersion")))
            return
        self.assertEqual(2, len(selectors))
        names = re.findall(r"\$\(MSBuildProjectName\.Equals\('([A-Za-z0-9._-]+)'\)\)", selectors[1].get("Condition", ""))
        self.assertEqual(len(inventory), len(names))
        self.assertEqual(inventory, set(names))
        owner_matrix = ast.parse((ROOT / "references/Hexalith.Builds/Tools/test-frontcomposer-immutable-view.py").read_text())
        assignments = [node for node in owner_matrix.body if isinstance(node, ast.Assign)
                       and any(isinstance(target, ast.Name) and target.id == "PROJECTS" for target in node.targets)]
        self.assertEqual(1, len(assignments))
        matrix_names = ast.literal_eval(assignments[0].value)
        self.assertEqual(len(inventory), len(matrix_names))
        self.assertEqual(inventory, set(matrix_names))

    def test_package_matching_detects_wildcard_masking_in_strict_profiles(self) -> None:
        for attribute, identity in (("Remove", "System.*"), ("Update", "*.Immutable"), ("Include", "Other.*;system.collections.*")):
            with self.subTest(attribute=attribute, identity=identity):
                root = ET.fromstring('<Project><ItemGroup><PackageVersion Include="System.Collections.Immutable" Version="10.0.15" /></ItemGroup></Project>')
                attributes = {attribute: identity}
                if attribute != "Remove":
                    attributes["Version"] = "11.0.0"
                ET.SubElement(root.find("ItemGroup"), "PackageVersion", attributes)
                with self.assertRaisesRegex(dg.GraphError, "exactly one unmasked shared operation"):
                    dg.assert_authoritative_package_version(root, "System.Collections.Immutable", "10.0.15", "strict catalog")
        self.assertFalse(dg._simple_match("Other.*", "System.Collections.Immutable"))
        self.assertFalse(dg._simple_match("System.Collections.Immutable.Future", "System.Collections.Immutable"))

    def test_immutable_consumer_view_rejects_wildcard_package_masking(self) -> None:
        for attribute, identity in (("Remove", "System.*"), ("Remove", "*Immutable"), ("Update", "System.Collections.*"), ("Update", "Other.*;System.*"), ("Include", "System.*")):
            with self.subTest(attribute=attribute, identity=identity):
                root = ET.fromstring(self._immutable_consumer_catalog())
                attributes = {attribute: identity}
                if attribute != "Remove":
                    attributes["Version"] = "11.0.0"
                ET.SubElement(ET.SubElement(root, "ItemGroup"), "PackageVersion", attributes)
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "requires one unmasked package operation")
        root = ET.fromstring(self._immutable_consumer_catalog())
        ET.SubElement(ET.SubElement(root, "ItemGroup"), "PackageVersion", {"Remove": "Other.*"})
        self.assertEqual(1, self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))["selectors_validated"])

    def test_immutable_consumer_view_rejects_percent_escaped_update_dots(self) -> None:
        root = ET.fromstring(self._immutable_consumer_catalog())
        ET.SubElement(root.find("ItemGroup"), "PackageVersion", {"Update": "System%2ECollections%2EImmutable", "Version": "99.0.0"})
        self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "dynamic or escaped shared operations")

    def test_immutable_consumer_view_rejects_percent_escaped_update_letter(self) -> None:
        root = ET.fromstring(self._immutable_consumer_catalog())
        ET.SubElement(root.find("ItemGroup"), "PackageVersion", {"Update": "System.Collections.%49mmutable", "Version": "99.0.0"})
        self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "dynamic or escaped shared operations")

    def test_immutable_consumer_view_rejects_executable_package_output(self) -> None:
        root = ET.fromstring(self._immutable_consumer_catalog())
        target = ET.SubElement(root, "Target", {"Name": "ForgeImmutable", "BeforeTargets": "CollectPackageReferences"})
        create_item = ET.SubElement(target, "CreateItem", {"Include": "System.Collections.Immutable", "AdditionalMetadata": "Version=11.0.0"})
        ET.SubElement(create_item, "Output", {"TaskParameter": "Include", "ItemName": "PackageVersion"})
        self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "cannot prove package authority with executable targets, tasks or outputs")

    def test_immutable_consumer_view_rejects_broadened_missing_or_dynamic_scope(self) -> None:
        for mutation in ("broadened", "missing-project", "duplicate-project", "dynamic", "starts-with", "missing-selector", "duplicate-selector", "reassigned", "alias", "true-default"):
            with self.subTest(mutation=mutation):
                root = ET.fromstring(self._immutable_consumer_catalog())
                properties = root.find("PropertyGroup")
                default, enabled = list(root.iter("HexalithFrontComposerImmutable10CatalogView"))
                condition = enabled.get("Condition")
                if mutation == "broadened":
                    enabled.set("Condition", condition + " or $(MSBuildProjectName.Equals('Hexalith.Builds.Tooling'))")
                elif mutation == "missing-project":
                    enabled.set("Condition", " or ".join(condition.split(" or ")[1:]))
                elif mutation == "duplicate-project":
                    enabled.set("Condition", condition + " or " + condition.split(" or ")[0])
                elif mutation == "dynamic":
                    enabled.set("Condition", condition.replace("Hexalith.FrontComposer.Contracts", "$(Other)", 1))
                elif mutation == "starts-with":
                    enabled.set("Condition", condition.replace(".Equals(", ".StartsWith("))
                elif mutation == "missing-selector":
                    properties.remove(default)
                elif mutation == "duplicate-selector":
                    properties.append(copy.deepcopy(enabled))
                elif mutation == "reassigned":
                    ET.SubElement(properties, default.tag).text = "true"
                elif mutation == "alias":
                    enabled.tag = enabled.tag.lower()
                else:
                    default.text = "true"
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "System.Collections.Immutable FrontComposer compatibility view")

    def test_immutable_consumer_view_rejects_unproven_values_aliases_and_package_authority(self) -> None:
        for mutation in ("wrong-selected", "dynamic-selected", "dynamic-default", "version-alias", "duplicate-value", "missing-value", "package-literal", "package-dynamic", "package-update", "package-duplicate", "package-case-alias", "package-attribute-alias", "dynamic-package-identity", "package-version-override", "package-child"):
            with self.subTest(mutation=mutation):
                root = ET.fromstring(self._immutable_consumer_catalog())
                properties = root.find("PropertyGroup")
                default, selected = list(root.iter("HexalithImmutableVersion"))
                row = next(row for row in root.iter("PackageVersion") if row.get("Include") == "System.Collections.Immutable")
                if mutation == "wrong-selected":
                    selected.text = "10.0.16"
                elif mutation == "dynamic-selected":
                    selected.text = "$(Other)"
                elif mutation == "dynamic-default":
                    default.text = "$(Other)"
                elif mutation == "version-alias":
                    selected.tag = selected.tag.lower()
                elif mutation == "duplicate-value":
                    properties.append(copy.deepcopy(selected))
                elif mutation == "missing-value":
                    properties.remove(selected)
                elif mutation == "package-literal":
                    row.set("Version", "10.0.15")
                elif mutation == "package-dynamic":
                    row.set("Version", "$(Other)")
                elif mutation == "package-update":
                    row.set("Update", row.attrib.pop("Include"))
                elif mutation == "package-duplicate":
                    root.find("ItemGroup").append(copy.deepcopy(row))
                elif mutation == "package-case-alias":
                    alias = copy.deepcopy(row)
                    alias.tag = "packageversion"
                    root.find("ItemGroup").append(alias)
                elif mutation == "package-attribute-alias":
                    alias = copy.deepcopy(row)
                    alias.set("include", alias.attrib.pop("Include"))
                    root.find("ItemGroup").append(alias)
                elif mutation == "dynamic-package-identity":
                    ET.SubElement(root.find("ItemGroup"), "PackageVersion", {"Include": "$(UnknownPackage)", "Version": "11.0.0"})
                elif mutation == "package-version-override":
                    row.set("VersionOverride", "10.0.15")
                else:
                    ET.SubElement(row, "Version").text = "10.0.15"
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "System.Collections.Immutable FrontComposer compatibility view")

    def test_immutable_consumer_view_rejects_invalid_placement_order_and_imports(self) -> None:
        for mutation in ("conditional-selector", "target-value", "choose-value", "selector-after-value", "selected-before-default", "conditional-package", "target-package", "import"):
            with self.subTest(mutation=mutation):
                root = ET.fromstring(self._immutable_consumer_catalog())
                properties = root.find("PropertyGroup")
                enabled = list(root.iter("HexalithFrontComposerImmutable10CatalogView"))[1]
                default, selected = list(root.iter("HexalithImmutableVersion"))
                row = next(row for row in root.iter("PackageVersion") if row.get("Include") == "System.Collections.Immutable")
                if mutation == "conditional-selector":
                    properties.remove(enabled)
                    ET.SubElement(root, "PropertyGroup", {"Condition": "'$(Other)' == 'true'"}).append(enabled)
                elif mutation in {"target-value", "choose-value"}:
                    properties.remove(selected)
                    if mutation == "target-value":
                        group = ET.SubElement(ET.SubElement(root, "Target", {"Name": "Later"}), "PropertyGroup")
                    else:
                        group = ET.SubElement(ET.SubElement(ET.SubElement(root, "Choose"), "When", {"Condition": "true"}), "PropertyGroup")
                    group.append(selected)
                elif mutation == "selector-after-value":
                    properties.remove(enabled)
                    properties.append(enabled)
                elif mutation == "selected-before-default":
                    properties.remove(selected)
                    properties.insert(0, selected)
                elif mutation == "conditional-package":
                    row.set("Condition", "true")
                elif mutation == "target-package":
                    root.find("ItemGroup").remove(row)
                    ET.SubElement(ET.SubElement(root, "Target", {"Name": "Later"}), "ItemGroup").append(row)
                else:
                    ET.SubElement(root, "Import", {"Project": "unknown.props"})
                self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "System.Collections.Immutable")

    def test_frontcomposer_profile_cross_module_versions_change_semantic_validation_passes(self) -> None:
        property_names = (
            "HexalithCommonsVersion",
            "HexalithEventStoreVersion",
            "HexalithMemoriesVersion",
            "HexalithPartiesVersion",
            "HexalithPolymorphicSerializationsVersion",
            "HexalithTenantsVersion",
        )
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._ordinary_frontcomposer_property_catalog()
        for index, name in enumerate(property_names, start=1):
            with self.subTest(property_name=name):
                mutated_catalog = self._replace_catalog_property_value(
                    catalog,
                    name,
                    f"999.0.{index}",
                )
                semantics = self._evaluate_frontcomposer_profile(mutated_catalog)
                self.assertEqual(semantics["selectors_validated"], 1)

    def test_frontcomposer_profile_missing_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_names = self._frontcomposer_profile()["selected_catalog_required_property_names"]
        self.assertEqual(len(property_names), 6)
        for property_name in property_names:
            with self.subTest(property_name=property_name):
                mutated_catalog = self._replace_catalog_property_element(catalog, property_name, b"")
                self._assert_frontcomposer_profile_failure(
                    mutated_catalog,
                    f"{property_name} must define exactly one version property",
                )

    def test_frontcomposer_profile_duplicate_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        mutated_catalog = self._replace_catalog_property_element(
            catalog,
            property_name,
            element + b"\r\n    " + element,
        )
        message = self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must define exactly one version property",
        )
        self.assertIn("found 2 values", message)

    def test_frontcomposer_profile_empty_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        mutated_catalog = self._replace_catalog_property_value(catalog, property_name, "")
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must contain a literal NuGet version, found ''",
        )

    def test_frontcomposer_profile_malformed_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        mutated_catalog = self._replace_catalog_property_value(catalog, property_name, "$(OtherVersion)")
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must contain a literal NuGet version",
        )

    def test_frontcomposer_profile_nested_module_property_value_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        opening_end = catalog.index(b">", start) + 1
        closing = f"</{property_name}>".encode("ascii")
        replacement = catalog[start:opening_end] + b"1.2.3<Unexpected />" + closing
        mutated_catalog = self._replace_catalog_property_element(catalog, property_name, replacement)
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must contain a literal NuGet version",
        )

    def test_frontcomposer_profile_noncanonical_condition_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        canonical = f"Condition=\"'$({property_name})' == ''\"".encode("ascii")
        noncanonical = b"Condition=\"'$(OtherVersion)' == ''\""
        self.assertEqual(catalog.count(canonical), 1)
        mutated_catalog = catalog.replace(canonical, noncanonical, 1)
        self._assert_frontcomposer_profile_failure(mutated_catalog, "canonical self-default condition")

    def test_frontcomposer_profile_unconditional_module_property_passes(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        canonical = f" Condition=\"'$({property_name})' == ''\"".encode("ascii")
        self.assertEqual(catalog.count(canonical), 1)
        mutated_catalog = catalog.replace(canonical, b"", 1)
        semantics = self._evaluate_frontcomposer_profile(mutated_catalog)
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_frontcomposer_profile_conditional_ancestor_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        without_property = self._replace_catalog_property_element(catalog, property_name, b"")
        conditional_group = (
            b"  <PropertyGroup Condition=\"'$(Configuration)' == 'Release'\">\r\n"
            b"    " + element + b"\r\n"
            b"  </PropertyGroup>\r\n"
        )
        mutated_catalog = without_property.replace(b"</Project>", conditional_group + b"</Project>", 1)
        self._assert_frontcomposer_profile_failure(mutated_catalog, "conditional group")

    def test_frontcomposer_profile_choose_selected_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        without_property = self._replace_catalog_property_element(catalog, property_name, b"")
        choose = (
            b"  <Choose>\r\n"
            b"    <When Condition=\"'$(Configuration)' == 'Release'\">\r\n"
            b"      <PropertyGroup>\r\n"
            b"        " + element + b"\r\n"
            b"      </PropertyGroup>\r\n"
            b"    </When>\r\n"
            b"  </Choose>\r\n"
        )
        mutated_catalog = without_property.replace(b"</Project>", choose + b"</Project>", 1)
        self._assert_frontcomposer_profile_failure(mutated_catalog, "Choose branch")

    def test_frontcomposer_profile_target_scoped_property_fails_as_nonglobal(self) -> None:
        catalog = self._ordinary_frontcomposer_property_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        without_property = self._replace_catalog_property_element(catalog, property_name, b"")
        target = (
            b'  <Target Name="SetCatalogVersion">\r\n'
            b"    <PropertyGroup>\r\n"
            b"      " + element + b"\r\n"
            b"    </PropertyGroup>\r\n"
            b"  </Target>\r\n"
        )
        mutated_catalog = without_property.replace(b"</Project>", target + b"</Project>", 1)
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            "must be a global property declared directly in a top-level PropertyGroup",
        )

    def test_frontcomposer_profile_required_package_changes_semantic_validation_fails_closed(self) -> None:
        profile = self._frontcomposer_profile()
        package_id = "ModelContextProtocol.AspNetCore"
        expected_version = profile["selected_catalog_required_packages"][package_id]
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        expected_row = f'<PackageVersion Include="{package_id}" Version="{expected_version}" />'.encode("utf-8")
        changed_row = f'<PackageVersion Include="{package_id}" Version="999.0.0" />'.encode("utf-8")
        self.assertEqual(catalog.count(expected_row), 1)

        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_frontcomposer_profile(catalog.replace(expected_row, changed_row, 1))

        message = str(ctx.exception)
        self.assertIn(package_id, message)
        self.assertIn(f"expected version {expected_version!r}", message)
        self.assertIn("found '999.0.0'", message)

    @staticmethod
    def _sibling_profile(profile_name: str) -> dict:
        policy = json.loads(
            (ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig")
        )
        return copy.deepcopy(policy["profiles"][profile_name])

    def test_sibling_profiles_are_presence_only_for_required_packages(self) -> None:
        expected = {
            "eventstore-catalog-v1": [
                "Microsoft.Extensions.TimeProvider.Testing",
                "ModelContextProtocol",
                "System.CommandLine",
            ],
            "memories-catalog-v1": [
                "Microsoft.Extensions.TimeProvider.Testing",
                "ModelContextProtocol.AspNetCore",
            ],
            "parties-catalog-v1": [
                "Microsoft.AspNetCore.Components.CustomElements",
                "ModelContextProtocol",
                "ModelContextProtocol.AspNetCore",
            ],
        }
        for profile_name, package_names in expected.items():
            with self.subTest(profile=profile_name):
                profile = self._sibling_profile(profile_name)
                self.assertIn("selected_catalog_required_package_names", profile)
                self.assertEqual(profile["selected_catalog_required_package_names"], package_names)
                self.assertEqual(profile["selected_catalog_required_packages"], {})
                dg.assert_profiles_well_formed({"profiles": {profile_name: profile}})

    def _evaluate_eventstore_profile(self, catalog: bytes) -> dict:
        profile_name = "eventstore-catalog-v1"
        with tempfile.TemporaryDirectory(dir=self.tmp_path) as fixture_directory:
            fx = GraphFixture(pathlib.Path(fixture_directory))

            eventstore = fx.add_repo("EventStore")
            eventstore.write_text("Directory.Packages.props", OWNER_SHIM)

            builds = fx.add_repo("Builds")
            builds.write_bytes("Props/Directory.Packages.props", catalog)
            builds_commit = builds.commit()

            fx.link("EventStore", "references/Hexalith.Builds", "Builds", builds_commit)
            eventstore_commit = eventstore.commit()
            eventstore_identity = fx.identity("EventStore")
            fx.policy["semantic_profiles"][eventstore_identity] = profile_name
            fx.policy["profiles"][profile_name] = self._sibling_profile(profile_name)

            envelope = dg.collect_graph(
                eventstore.root,
                eventstore_identity,
                eventstore_commit,
                fx.policy,
            )
            return dg.evaluate_semantics(eventstore.root, fx.policy, envelope)

    def test_eventstore_profile_system_commandline_version_may_move(self) -> None:
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        current_version = self._catalog_package_version(catalog, "System.CommandLine")
        current_row = f'<PackageVersion Include="System.CommandLine" Version="{current_version}" />'.encode("ascii")
        moved_row = b'<PackageVersion Include="System.CommandLine" Version="999.0.0" />'
        self.assertEqual(catalog.count(current_row), 1)
        semantics = self._evaluate_eventstore_profile(catalog.replace(current_row, moved_row, 1))
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_eventstore_profile_missing_system_commandline_fails_with_coordinates(self) -> None:
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        current_version = self._catalog_package_version(catalog, "System.CommandLine")
        current_row = f'<PackageVersion Include="System.CommandLine" Version="{current_version}" />'.encode("ascii")
        self.assertEqual(catalog.count(current_row), 1)
        catalog = catalog.replace(current_row, b"", 1)

        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_eventstore_profile(catalog)
        message = str(ctx.exception)
        self.assertIn("github.com/test/eventstore@", message)
        self.assertIn("references/Hexalith.Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn("System.CommandLine", message)

    def _catalog_package_version(self, catalog: bytes, package_id: str) -> str:
        document = ET.fromstring(catalog.decode("utf-8-sig"))
        versions = [
            element.attrib["Version"]
            for element in document.iter("PackageVersion")
            if element.attrib.get("Include") == package_id
        ]
        self.assertEqual(len(versions), 1, f"{package_id} PackageVersion row missing or duplicated in selected catalog fixture")
        return versions[0]


    def _package_sdk511_catalog(self) -> bytes:
        root = ET.fromstring(self._current_tenants8_catalog())
        properties = root.find("PropertyGroup")
        fixture = ET.fromstring((ROOT / "tests/ci-governance/fixtures/builds-sdk42-package-511-qualification.xml").read_bytes())
        names = {element.tag for element in fixture.find("PropertyGroup")}
        for element in list(properties):
            if element.tag in names:
                properties.remove(element)
        for index, element in enumerate(fixture.find("PropertyGroup")):
            properties.insert(index, copy.deepcopy(element))
        ET.indent(root)
        return self._catalog_xml_bytes(root)

    def test_package_sdk511_accepts_only_the_exact_frontcomposer_cohort(self) -> None:
        catalog = self._package_sdk511_catalog()
        self.assertEqual(1, self._evaluate_frontcomposer_profile(catalog)["selectors_validated"])
        root = ET.fromstring(catalog)
        for name, values in (("HexalithEventStoreVersion", ["4.2.0", "5.1.1"]),
                             ("HexalithMemoriesVersion", ["3.0.27", "4.0.0"]),
                             ("HexalithTenantsVersion", ["8.0.0", "7.0.7"])):
            self.assertEqual(values, [element.text for element in root.iter(name)])
            with self.subTest(property=name), self.assertRaises(dg.GraphError):
                dg.assert_selected_catalog_property_shape(root, name, "strict helper")
        with self.assertRaises(dg.GraphError):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        ET.SubElement(root.find("PropertyGroup"), "HexalithCommonsVersion").text = "3.0.0"
        self._assert_frontcomposer_profile_failure(self._catalog_xml_bytes(root), "HexalithCommonsVersion must define exactly one")

    def test_package_sdk511_rejects_unknown_defaults_and_mixed_guard_shapes(self) -> None:
        for value in ("5.1.0", "5.1.2", "5.2.0", "5.1.*", "$(OtherVersion)", ""):
            with self.subTest(default=value), self.assertRaises(dg.GraphError):
                root = ET.fromstring(self._package_sdk511_catalog())
                list(root.iter("HexalithEventStoreVersion"))[1].text = value
                self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))
        for change in ("old-guard", "wrong-mode", "wrong-optout", "unknown-property", "missing-mode",
                       "mode-and-optout", "broaden", "unset", "attribute-case"):
            with self.subTest(guard=change), self.assertRaises(dg.GraphError):
                root = ET.fromstring(self._package_sdk511_catalog())
                row = list(root.iter("HexalithEventStoreVersion"))[0]
                condition = row.get("Condition")
                if change == "old-guard":
                    condition = dg._SDK42_CURRENT_CONDITION
                elif change == "wrong-mode":
                    condition = condition.replace("'$(UseHexalithProjectReferences)' != 'true'", "'$(UseHexalithProjectReferences)' == 'true'")
                elif change == "wrong-optout":
                    condition = condition.replace("'$(HexalithEventStoreFromSource)' == 'false'", "'$(HexalithEventStoreFromSource)' != 'true'")
                elif change == "unknown-property":
                    condition = condition.replace("HexalithEventStoreFromSource", "UnreviewedFromSource")
                elif change == "missing-mode":
                    condition = condition.replace("'$(UseHexalithProjectReferences)' != 'true' or ", "")
                elif change == "mode-and-optout":
                    condition = condition.replace("'true' or '$(HexalithEventStoreFromSource)'", "'true' and '$(HexalithEventStoreFromSource)'")
                elif change == "broaden":
                    condition += " or true"
                elif change == "unset":
                    condition = condition.replace("== ''", "!= ''", 1)
                if change == "attribute-case":
                    row.attrib = {"condition": condition}
                else:
                    row.set("Condition", condition)
                self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_package_sdk511_rejects_missing_duplicate_aliased_or_nested_rows(self) -> None:
        for name in ("HexalithEventStoreVersion", "HexalithMemoriesVersion", "HexalithTenantsVersion"):
            for ordinal in (0, 1):
                for change in ("missing", "duplicate", "alias", "child", "attribute", "conditional", "choose", "target"):
                    with self.subTest(property=name, row=ordinal, change=change), self.assertRaises(dg.GraphError):
                        root = ET.fromstring(self._package_sdk511_catalog())
                        group = root.find("PropertyGroup")
                        row = list(root.iter(name))[ordinal]
                        if change == "missing":
                            group.remove(row)
                        elif change == "duplicate":
                            group.append(copy.deepcopy(row))
                        elif change == "alias":
                            row.tag = name.lower()
                        elif change == "child":
                            ET.SubElement(row, "HiddenValue")
                        elif change == "attribute":
                            row.set("Label", "unreviewed")
                        else:
                            group.remove(row)
                            if change == "conditional":
                                holder = ET.SubElement(root, "PropertyGroup", {"Condition": "true"})
                            elif change == "choose":
                                holder = ET.SubElement(ET.SubElement(ET.SubElement(root, "Choose"), "When", {"Condition": "true"}), "PropertyGroup")
                            else:
                                holder = ET.SubElement(ET.SubElement(root, "Target", {"Name": "RewriteVersion"}), "PropertyGroup")
                            holder.append(row)
                        self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_package_sdk511_rejects_changed_declaration_order(self) -> None:
        for name, ordinal in (("HexalithEventStoreVersion", 1), ("HexalithMemoriesVersion", 1),
                              ("HexalithTenantsVersion", 0), ("HexalithTenantsVersion", 1),
                              ("HexalithPartiesSdk42CatalogView", 1), ("HexalithPlatformSdk42CatalogView", 1),
                              ("HexalithBuildsPackagedHostSdk42CatalogView", 1)):
            with self.subTest(property=name, row=ordinal), self.assertRaises(dg.GraphError):
                root = ET.fromstring(self._package_sdk511_catalog())
                group = root.find("PropertyGroup")
                row = list(root.iter(name))[ordinal]
                group.remove(row)
                group.insert(0, row)
                self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_package_sdk511_rejects_changed_frozen_role_subjects_and_writers(self) -> None:
        for name in ("HexalithPartiesSdk42CatalogView", "HexalithPlatformSdk42CatalogView", "HexalithBuildsPackagedHostSdk42CatalogView"):
            for change in ("future", "duplicate", "dynamic", "frontcomposer", "lowercase", "remove", "prefix", "constant",
                           "true-default", "conditional-default", "extra-attribute", "missing-true", "duplicate-false"):
                with self.subTest(selector=name, change=change), self.assertRaises(dg.GraphError):
                    root = ET.fromstring(self._package_sdk511_catalog())
                    group = root.find("PropertyGroup")
                    default, enabled = list(root.iter(name))
                    clauses = re.split(r"\s+or\s+", enabled.attrib["Condition"].strip())
                    if change == "future":
                        clauses.append("$(MSBuildProjectName.Equals('Hexalith.Platform.Future'))")
                    elif change == "duplicate":
                        clauses.append(clauses[0])
                    elif change == "dynamic":
                        clauses[0] = "$(MSBuildProjectName.Equals('$(SelectedProject)'))"
                    elif change == "frontcomposer":
                        clauses[0] = "$(MSBuildProjectName.Equals('Hexalith.FrontComposer.Shell'))"
                    elif change == "lowercase":
                        clauses[0] = clauses[0].lower()
                    elif change == "remove":
                        clauses.pop()
                    elif change == "prefix":
                        clauses = ["$(MSBuildProjectName.StartsWith('Hexalith.Platform'))"]
                    elif change == "constant":
                        clauses = ["true"]
                    elif change == "true-default":
                        default.text = "true"
                    elif change == "conditional-default":
                        default.set("Condition", "true")
                    elif change == "extra-attribute":
                        enabled.set("Label", "unreviewed")
                    elif change == "missing-true":
                        group.remove(enabled)
                    else:
                        group.append(copy.deepcopy(default))
                    enabled.set("Condition", " or ".join(clauses))
                    self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_package_sdk511_rejects_changed_retained_cohorts(self) -> None:
        for name in ("HexalithEventStoreVersion", "HexalithMemoriesVersion", "HexalithTenantsVersion"):
            for ordinal in (0, 1):
                for change in ("version", "dynamic", "condition"):
                    with self.subTest(property=name, row=ordinal, change=change), self.assertRaises(dg.GraphError):
                        root = ET.fromstring(self._package_sdk511_catalog())
                        row = list(root.iter(name))[ordinal]
                        if change == "version":
                            row.text = "99.0.0"
                        elif change == "dynamic":
                            row.text = "$(UnreviewedVersion)"
                        else:
                            row.set("Condition", row.get("Condition") + " or true")
                        self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_package_sdk511_rejects_hidden_imports_outputs_and_root_overrides(self) -> None:
        for change in ("root-condition", "local-property", "import", "grouped-import", "target", "using-task", "output-alias"):
            with self.subTest(change=change), self.assertRaises(dg.GraphError):
                root = ET.fromstring(self._package_sdk511_catalog())
                if change == "root-condition":
                    root.set("Condition", "true")
                elif change == "local-property":
                    root.set("TreatAsLocalProperty", "HexalithEventStoreVersion")
                elif change == "import":
                    ET.SubElement(root, "Import", {"Project": "unreviewed.props"})
                elif change == "grouped-import":
                    ET.SubElement(ET.SubElement(root, "ImportGroup"), "Import", {"Project": "unreviewed.props"})
                else:
                    tag = {"target": "Target", "using-task": "UsingTask", "output-alias": "output"}[change]
                    ET.SubElement(root, tag, {"Name": "Unreviewed"})
                self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))

    def test_package_sdk511_standalone_memories_proof_requires_the_complete_pair(self) -> None:
        root = ET.fromstring(self._package_sdk511_catalog())
        root.find("PropertyGroup").remove(list(root.iter("HexalithMemoriesVersion"))[0])
        with self.assertRaisesRegex(dg.GraphError, "requires exactly two canonical declarations"):
            dg.assert_selected_catalog_property_shape(root, "HexalithMemoriesVersion", "standalone current proof", allow_eventstore_qualification=True)


class ConsumerProjectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.fixture = GraphFixture(pathlib.Path(self.temp.name))
        fx = self.fixture
        identities = {"Root": "github.com/hexalith/hexalith.frontcomposer",
                      "Builds": "github.com/hexalith/hexalith.builds",
                      "Source": "github.com/hexalith/hexalith.platform"}
        fx.identity = lambda name: identities[name]
        fx.url = lambda name: "https://" + identities[name] + ".git"
        fx.policy["builds_identity"] = identities["Builds"]
        self.root = fx.add_repo("Root")
        fx.policy["trusted_identities"][0]["local_path"] = "."
        self.root.write_text(".gitignore", "references/\n")
        for name in ("Builds", "Source"):
            repo = TempGitRepo(self.root.root / "references" / name)
            fx.repos[name] = repo
            fx.policy["trusted_identities"].append({"identity": fx.identity(name), "local_path": "references/" + name})
        self.builds = fx.repos["Builds"]
        self.builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        self.builds.write_text("src/Tooling.csproj", "<Project />\n")
        self.nested_builds = self.builds.commit()
        self.builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG.replace(b'1.0.0', b'2.0.0'))
        self.root_builds = self.builds.commit()
        self.source = fx.repos["Source"]
        self.source.write_text("Directory.Packages.props", OWNER_SHIM)
        for name in ("One.cs", "Two.cs"):
            self.source.write_text(name, "// unchanged canonical " + name + "\n")
        fx.link("Source", "references/Builds", "Builds", self.nested_builds)
        self.untrusted = []
        for number in range(7):
            identity = f"github.com/untrusted/inventory{number}"
            self.untrusted.append(identity)
            self.source.add_submodule("Inventory" + str(number), "references/Inventory" + str(number),
                                      "https://" + identity + ".git", f"{number + 1:040x}")
        self.owner = self.source.commit()
        self.root.write_text("Directory.Packages.props", OWNER_SHIM)
        self.root.write_text("Root.slnx", "<Solution />\n")
        self.root.write_text("global.json", '{"sdk":{"version":"10.0.402"}}\n')
        self.root.write_text("nuget.config", "<configuration />\n")
        self.project = "src/AppHost.csproj"
        self.project_xml = ('<Project><ItemGroup>'
            '<Compile Include="$(CanonicalSourceRoot)/One.cs" Link="Canonical/One.cs" />'
            '<Compile Include="$(CanonicalSourceRoot)/Two.cs" Link="Canonical/Two.cs" />'
            '<ProjectReference Include="$(HexalithBuildsRoot)/src/Tooling.csproj" IsAspireProjectResource="false" />'
            '</ItemGroup></Project>\n')
        self.root.write_text(self.project, self.project_xml)
        fx.link("Root", "references/Source", "Source", self.owner)
        fx.link("Root", "references/Builds", "Builds", self.root_builds)
        self.commit = self.root.commit()
        for name in ("Root", "Builds", "Source"):
            fx.policy["semantic_profiles"][fx.identity(name)] = "fixture"
            fx.register_module(name, evidence_only=name == "Source")
        fx.policy["profiles"]["fixture"] = {"owner_checks": {}, "selected_catalog_required_properties": {},
                                               "selected_catalog_required_packages": {}}
        raw = dg.read_gitmodules(self.source.root, self.owner, 1048576)
        links = dg.list_gitlinks(self.source.root, self.owner, 67108864)
        by_path = {entry["path"]: dg.normalize_identity(entry["url"], "fixture") for entry in raw.values()}
        inventory = [{"owner_repository": fx.identity("Source"), "owner_commit": self.owner,
                      "path": path, "repository": by_path[path], "commit": sha, "depth": 2}
                     for path, sha in sorted(links.items())]
        direct = dg._owner_edges(self.root.root, self.commit, fx.identity("Root"), 1, fx.policy, self.root.root)
        row = {"owner_commit": self.owner,
               "gitmodules_sha256": hashlib.sha256(dg.read_blob(self.source.root, self.owner, ".gitmodules", 1048576, "fixture")).hexdigest(),
               "inventory_sha256": dg.canonical_digest(inventory),
               "inventory_only_identities": self.untrusted,
               "source_files": {name: hashlib.sha256(dg.read_blob(self.source.root, self.owner, name, 1048576, "fixture")).hexdigest()
                                for name in ("One.cs", "Two.cs")},
               "consumer_repository": fx.identity("Root"), "consumer_project": self.project,
               "source_root_property": "CanonicalSourceRoot",
               "compile_links": {name: "Canonical/" + name for name in ("One.cs", "Two.cs")},
               "builds_commit": self.root_builds, "builds_project": "src/Tooling.csproj",
               "root_selectors_sha256": dg.canonical_digest([{key: edge[key] for key in ("path", "repository", "commit")} for edge in direct]),
               "consumer_configuration_sha256": dg.canonical_digest(dg.consumer_configuration(self.root.root, self.commit, fx.policy))}
        fx.policy["consumer_source_projections"] = {fx.identity("Source"): row}
        self.row = row
        dg.assert_policy_well_formed(fx.policy)

    def graph(self, commit: str | None = None) -> dict:
        return dg.collect_graph(self.root.root, self.fixture.identity("Root"), commit or self.commit, self.fixture.policy)

    @staticmethod
    def resign(graph: dict) -> None:
        graph["edge_count"] = len(graph["edges"])
        graph["edges"].sort(key=lambda edge: tuple(edge[key] for key in ("depth", "owner_repository", "owner_commit", "path", "repository", "commit")))
        graph["graph_digest"] = dg.canonical_digest({key: graph[key] for key in ("schema", "root", "edge_count", "edges")})

    def test_raw_inventory_and_historical_catalog_stay_visible_without_global_trust(self) -> None:
        graph = self.graph()
        self.assertEqual(graph["edge_count"], 10)
        self.assertEqual(dg.validate_graph_envelope(graph), graph)
        self.assertEqual(sum(edge["repository"] in self.untrusted for edge in graph["edges"]), 7)
        nested = next(edge for edge in graph["edges"] if edge["depth"] == 2 and edge["repository"] == self.fixture.identity("Builds"))
        self.assertEqual(nested["commit"], self.nested_builds)
        self.assertEqual(nested["catalog_sha256"], hashlib.sha256(BASELINE_CATALOG).hexdigest())
        report = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)["consumer_source_projections"][self.fixture.identity("Source")]
        self.assertEqual(report["root_builds_commit"], self.root_builds)
        self.assertNotEqual(report["root_builds_commit"], nested["commit"])
        for identity in self.untrusted:
            with self.assertRaisesRegex(dg.GraphError, "unknown/untrusted"):
                dg.resolve_local_path(self.fixture.policy, identity, self.root.root)

    def test_projection_policy_has_closed_shapes_and_cannot_promote_global_trust(self) -> None:
        for field, value in (("inventory_sha256", "bad"), ("inventory_only_identities", [True]),
                             ("inventory_only_identities", [self.fixture.identity("Builds")]),
                             ("source_files", {"../outside.cs": "0" * 64, "Two.cs": "0" * 64}),
                             ("unexpected", True)):
            policy = copy.deepcopy(self.fixture.policy)
            policy["consumer_source_projections"][self.fixture.identity("Source")][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(dg.GraphError):
                dg.assert_policy_well_formed(policy)

    def test_seal_changes_and_unknown_root_identity_refuse(self) -> None:
        for field in ("gitmodules_sha256", "inventory_sha256"):
            with self.subTest(field=field), mock.patch.dict(self.row, {field: "0" * 64}):
                with self.assertRaises(dg.GraphError): self.graph()
        with mock.patch.dict(self.row["source_files"], {"One.cs": "0" * 64}):
            with self.assertRaisesRegex(dg.GraphError, "canonical source"):
                self.graph()
        self.root.add_submodule("Untrusted", "references/Outside", "https://" + self.untrusted[0] + ".git", "1" * 40)
        changed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "unknown/untrusted"):
            self.graph(changed)

    def test_projection_owner_cannot_become_a_buildable_umbrella(self) -> None:
        registry = self.fixture.policy["module_build_registry"][self.fixture.identity("Source")]
        with mock.patch.dict(registry, {"disposition": "build"}), self.assertRaisesRegex(dg.GraphError, "whole-owner execution"):
            dg.assert_policy_well_formed(self.fixture.policy)
        with mock.patch.dict(registry, {"disposition": "build"}), self.assertRaisesRegex(dg.GraphError, "whole-owner execution"):
            self.graph()

    def test_materialization_contains_exactly_the_two_canonical_compile_files(self) -> None:
        destination = pathlib.Path(self.temp.name) / "consumer"
        destination.mkdir()
        files = dg.materialize_consumer_sources(self.root.root, self.fixture.policy, self.graph(), destination,
                                               self.fixture.identity("Root"), self.commit)
        self.assertEqual(files, ["references/Source/One.cs", "references/Source/Two.cs"])
        self.assertEqual(sorted(str(path.relative_to(destination)) for path in destination.rglob("*") if path.is_file()), files)
        self.assertFalse((destination / "references/Source/references").exists())

    def test_changed_owner_pin_refuses_before_source_consumption(self) -> None:
        self.source.write_text("README.md", "different owner tree\n")
        changed_owner = self.source.commit()
        self.root.link_gitlink("references/Source", changed_owner)
        changed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "owner pin changed"):
            self.graph(changed)

    def test_reviewed_helper_and_configuration_change_requires_each_exact_seal(self) -> None:
        # A new selected helper and reviewed root configuration are separate inputs.
        # Updating either seal alone must not authorize the other changed input.
        original_sources = dict(self.row["source_files"])
        self.builds.write_text("src/Tooling.csproj", "<Project><PropertyGroup><ReviewedHelper>true</ReviewedHelper></PropertyGroup></Project>\n")
        selected_helper = self.builds.commit()
        self.root.link_gitlink("references/Builds", selected_helper)
        self.root.write_text("deps.local.props", "<Project><PropertyGroup><ReviewedRoot>true</ReviewedRoot></PropertyGroup></Project>\n")
        candidate = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "root source selectors changed"):
            self.graph(candidate)
        direct = dg._owner_edges(self.root.root, candidate, self.fixture.identity("Root"), 1, self.fixture.policy, self.root.root)
        self.row["root_selectors_sha256"] = dg.canonical_digest([
            {key: edge[key] for key in ("path", "repository", "commit")} for edge in direct])
        with self.assertRaisesRegex(dg.GraphError, "exact approved root Builds source"):
            self.graph(candidate)
        self.row["builds_commit"] = selected_helper
        with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
            self.graph(candidate)
        self.row["consumer_configuration_sha256"] = dg.canonical_digest(
            dg.consumer_configuration(self.root.root, candidate, self.fixture.policy))
        projection = dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(candidate))[self.fixture.identity("Source")]
        self.assertEqual(projection["root_builds_commit"], selected_helper)
        self.assertEqual(projection["source_files"], original_sources)
        self.root.write_text("deps.local.props", "<Project><Import Project=\"unreviewed.targets\" /></Project>\n")
        unreviewed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
            self.graph(unreviewed)

    def test_added_or_indirect_compile_and_project_reference_consumption_refuse(self) -> None:
        for item in ('<Compile Include="$(CanonicalSourceRoot)/Other.cs" />',
                     '<ProjectReference Include="$(CanonicalSourceRoot)/Other.csproj" />'):
            with self.subTest(item=item):
                self.root.write_text(self.project, self.project_xml.replace('</ItemGroup>', item + '</ItemGroup>'))
                changed = self.root.commit()
                with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
                    dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(changed))
        self.root.write_text(self.project, self.project_xml)
        restored = self.root.commit()
        dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(restored))
        self.root.write_text("another.csproj", '<Project><ItemGroup><Compile Include="$(Alias)/**/*.cs" /></ItemGroup></Project>')
        changed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
            dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(changed))

    def test_automatic_user_imports_and_response_inputs_refuse_before_execution(self) -> None:
        # Microsoft.Common.CurrentVersion.targets imports $(MSBuildProjectFullPath).user;
        # MSBuild also consumes automatic directory/tool response files. None of these
        # requires a change to the already sealed consumer project.
        inputs = {
            self.project + ".user": '<Project><ItemGroup><Compile Include="$(CanonicalSourceRoot)/Other.cs" /></ItemGroup></Project>\n',
            "Directory.Build.rsp": '-p:CustomAfterMicrosoftCommonTargets=references/Source/Other.targets\n',
            "MSBuild.rsp": '-p:CustomAfterMicrosoftCommonTargets=references/Source/Other.targets\n',
            "src/Directory.Build.rsp": '-p:CustomAfterMicrosoftCommonTargets=references/Source/Other.targets\n',
        }
        original_project = dg.read_blob(self.root.root, self.commit, self.project, 1048576, "fixture")
        for path, content in inputs.items():
            with self.subTest(path=path):
                self.root.write_text(path, content)
                changed = self.root.commit()
                self.assertEqual(dg.read_blob(self.root.root, changed, self.project, 1048576, "fixture"), original_project)
                self.assertIn(path, dg.consumer_configuration(self.root.root, changed, self.fixture.policy))
                with mock.patch.object(dg.subprocess, "run", wraps=dg.subprocess.run) as invoked:
                    with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
                        self.graph(changed)
                    self.assertTrue(all(call.args[0][0] == "git" for call in invoked.call_args_list))
                _run(["rm", "--", path], self.root.root)
                restored = self.root.commit()
                self.graph(restored)

    def test_committed_sdk_and_restore_configuration_changes_refuse(self) -> None:
        cases = {"global.json": '{"sdk":{"version":"10.0.999"}}\n',
                 "nuget.config": '<configuration><packageSources><clear /></packageSources></configuration>\n',
                 "NuGet.Config": '<configuration />\n', "src/NuGet.config": '<configuration />\n'}
        for path, content in cases.items():
            with self.subTest(path=path):
                original = dg.read_blob(self.root.root, self.commit, path, 1048576, "fixture")
                self.root.write_text(path, content)
                changed = self.root.commit()
                with self.assertRaisesRegex(dg.GraphError, "configuration changed"):
                    self.graph(changed)
                if original is None: _run(["rm", "--", path], self.root.root)
                else: self.root.write_bytes(path, original)
                restored = self.root.commit()
                self.graph(restored)
        for path in ("global.json", "nuget.config"):
            with self.subTest(removed=path):
                original = dg.read_blob(self.root.root, self.commit, path, 1048576, "fixture")
                _run(["rm", "--", path], self.root.root)
                removed = self.root.commit()
                with self.assertRaisesRegex(dg.GraphError, "configuration changed"):
                    self.graph(removed)
                self.root.write_bytes(path, original)
                self.graph(self.root.commit())

    def test_affected_build_commands_require_both_materialized_canonical_sources(self) -> None:
        # Execute the actual orchestration/materialization path; only the final
        # statically authorized command is simulated, after inspecting its files.
        self.fixture.register_module("Builds", evidence_only=True)
        commit = self.write_policy()
        graph = self.graph(commit)
        semantics = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)
        evidence = dg.diff_graphs(graph, graph, self.fixture.policy, event="push", event_base=commit,
                                  candidate=commit, merge_base=None, release_eligible=False, full_affected=True)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, commit)
        evidence.update({"dependency_policy": coordinate, "base_semantics": semantics, "candidate_semantics": semantics,
                         "diagnostic": "owned synthetic full-affected materialization regression"})
        evidence["evidence_digest"] = dg.canonical_digest({key: value for key, value in evidence.items() if key != "evidence_digest"})
        expected = {path: dg.read_blob(self.source.root, self.owner, path, 1048576, "fixture") for path in self.row["source_files"]}
        actual_run = dg.subprocess.run
        commands = []
        def execute(argv, **kwargs):
            if argv[0] == "git": return actual_run(argv, **kwargs)
            self.assertEqual(argv[0], "dotnet")
            source = pathlib.Path(kwargs["cwd"]) / "references/Source"
            self.assertEqual(sorted(path.name for path in source.iterdir()), sorted(expected))
            for path, content in expected.items(): self.assertEqual((source / path).read_bytes(), content)
            commands.append(argv)
            return subprocess.CompletedProcess(argv, 0, stdout="fixture verified canonical sources", stderr="")
        with mock.patch.object(dg.subprocess, "run", side_effect=execute):
            result = dg.run_affected_builds(self.root.root, evidence, self.root.root / "positive-output")
        self.assertEqual(len(commands), 2)
        self.assertEqual([argv[1] for argv in commands], ["restore", "build"])
        self.assertTrue(any(module["repository"] == self.fixture.identity("Root") and module["disposition"] == "build" for module in result["modules"]))

    def test_worktree_changes_never_replace_committed_consumer_configuration(self) -> None:
        graph = self.graph()
        self.root.write_text(self.project, self.project_xml.replace('One.cs', 'Unapproved.cs'))
        self.source.write_text("One.cs", "uncommitted foreign content\n")
        dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, graph)

    def test_rehashed_forged_depth2_inventory_and_catalog_cannot_authenticate(self) -> None:
        for kind in ("target", "omitted", "extra", "catalog"):
            graph = copy.deepcopy(self.graph())
            edge = next(edge for edge in graph["edges"] if edge["depth"] == 2 and
                        (edge["repository"] == self.fixture.identity("Builds") if kind == "catalog" else edge["repository"] in self.untrusted))
            if kind == "target": edge["commit"] = "f" * 40
            elif kind == "omitted": graph["edges"].remove(edge)
            elif kind == "extra": graph["edges"].append({**edge, "path": "references/Extra"})
            else: edge["catalog_sha256"] = "0" * 64
            self.resign(graph)
            dg.validate_graph_envelope(graph)
            with self.subTest(kind=kind), self.assertRaises(dg.GraphError):
                dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, graph)

    def test_rehashed_root_inventory_and_catalog_forgery_refuse_before_materialization(self) -> None:
        base = self.write_policy()
        graph = self.graph(base)
        semantics = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, base)
        for kind in ("omitted-owner", "root-catalog", "actual-root-target", "actual-root-mapping"):
            with self.subTest(kind=kind):
                forged = copy.deepcopy(graph)
                candidate = base
                if kind == "omitted-owner":
                    forged["edges"] = [edge for edge in forged["edges"] if edge["repository"] != self.fixture.identity("Source")
                                       and edge["owner_repository"] != self.fixture.identity("Source")]
                elif kind == "root-catalog":
                    next(edge for edge in forged["edges"] if edge["depth"] == 1 and
                         edge["repository"] == self.fixture.identity("Builds"))["catalog_sha256"] = "0" * 64
                else:
                    if kind == "actual-root-target":
                        self.root.link_gitlink("references/Builds", self.nested_builds)
                    else:
                        self.root.link_gitlink("references/Builds", self.root_builds)
                        modules = dg.read_blob(self.root.root, base, ".gitmodules", 1048576, "fixture").decode()
                        self.root.write_text(".gitmodules", modules.replace(self.fixture.url("Builds"), self.fixture.url("Root")))
                    candidate = self.root.commit()
                    forged["root"]["commit"] = candidate
                    for edge in forged["edges"]:
                        if edge["depth"] == 1: edge["owner_commit"] = candidate
                self.resign(forged)
                dg.validate_graph_envelope(forged)
                evidence = dg.diff_graphs(graph, forged, self.fixture.policy, event="push", event_base=base,
                                          candidate=candidate, merge_base=None, release_eligible=True)
                claimed = copy.deepcopy(semantics)
                if kind == "omitted-owner":
                    claimed["consumer_source_projections"] = {}
                else:
                    projection = claimed["consumer_source_projections"][self.fixture.identity("Source")]
                    projection["consumer_commit"] = candidate
                    projection["projection_digest"] = dg.canonical_digest({key: value for key, value in projection.items() if key != "projection_digest"})
                evidence.update({"dependency_policy": coordinate, "base_semantics": semantics, "candidate_semantics": claimed})
                evidence["evidence_digest"] = dg.canonical_digest({key: value for key, value in evidence.items() if key != "evidence_digest"})
                non_git_calls = []
                actual_run = dg.subprocess.run
                def inspect_or_record(argv, **kwargs):
                    if argv[0] == "git": return actual_run(argv, **kwargs)
                    non_git_calls.append(argv)
                    return subprocess.CompletedProcess(argv, 0, stdout="", stderr="")
                with mock.patch.object(dg.subprocess, "run", side_effect=inspect_or_record), \
                        mock.patch.object(dg, "materialize_repository_tree") as materialize, \
                        mock.patch.object(dg, "materialize_contract_tree") as contract, \
                        mock.patch.object(dg, "materialize_consumer_sources") as sources, \
                        self.assertRaisesRegex(dg.GraphError, "committed root edges|root Builds catalog"):
                    dg.run_affected_builds(self.root.root, evidence, self.root.root / "output")
                materialize.assert_not_called()
                contract.assert_not_called()
                sources.assert_not_called()
                self.assertEqual(non_git_calls, [])

    def test_committed_older_root_without_projection_owner_receives_no_projection_authority(self) -> None:
        self.root.write_text(".gitmodules", '[submodule "Builds"]\n\tpath = references/Builds\n\turl = ' + self.fixture.url("Builds") + '\n')
        self.root.write_text(self.project, '<Project />\n')
        _run(["update-index", "--force-remove", "references/Source"], self.root.root)
        earlier = self.root.commit()
        graph = self.graph(earlier)
        self.assertEqual(dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, graph), {})
        self.assertFalse(any(edge["repository"] == self.fixture.identity("Source") for edge in graph["edges"]))

    def test_missing_trusted_historical_catalog_is_not_a_projection_skip(self) -> None:
        original = dg.read_blob
        def read(local, commit, path, *args):
            if commit == self.nested_builds and path == "Props/Directory.Packages.props": return None
            return original(local, commit, path, *args)
        with mock.patch.object(dg, "read_blob", side_effect=read), self.assertRaisesRegex(dg.GraphError, "missing Props/Directory.Packages.props"):
            self.graph()

    def write_policy(self) -> str:
        self.root.write_text(dg.POLICY_PATH, json.dumps(self.fixture.policy, indent=2) + "\n")
        return self.root.commit()

    def _make_catalog_object_unavailable(self) -> pathlib.Path:
        remote = pathlib.Path(self.temp.name) / "catalog-remote"
        self.builds.root.rename(remote)
        _run(["clone", "--quiet", "--depth=1", remote.as_uri(), str(self.builds.root)], self.root.root)
        self.assertNotEqual(dg._run_git(["cat-file", "-e", self.nested_builds], self.builds.root).returncode, 0)
        return remote

    def test_catalog_preparation_acquires_exact_object_without_checkout_or_nested_initialization(self) -> None:
        self.root.link_gitlink("references/Builds", self.nested_builds)
        base = self.write_policy()
        self.root.link_gitlink("references/Builds", self.root_builds)
        commit = self.root.commit()
        remote = self._make_catalog_object_unavailable()
        _run(["remote", "set-url", "origin", "https://untrusted.invalid/builds.git"], self.builds.root)
        before_head = self.builds.head()
        before_catalog = (self.builds.root / "Props/Directory.Packages.props").read_bytes()
        before_status = _run(["status", "--porcelain"], self.builds.root)
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaises(dg.GraphError):
            self.graph(commit)
        network.assert_not_called()

        def fetch(args, cwd):
            self.assertEqual(args, ["-c", "submodule.recurse=false", "fetch", "--no-recurse-submodules",
                                    "--no-tags", dg._canonical_clone_url(self.fixture.identity("Builds")), self.nested_builds])
            self.assertEqual(cwd, self.builds.root)
            return dg._run_git([*args[:-2], remote.as_uri(), args[-1]], cwd), 1

        with mock.patch.object(dg, "_run_git_network", side_effect=fetch) as network:
            result = dg.acquire_catalog_objects(self.root.root, base, commit)
            network.assert_called_once()
        self.assertEqual(result["catalog_commits"], sorted([self.root_builds, self.nested_builds]))
        self.assertEqual(self.builds.head(), before_head)
        self.assertEqual((self.builds.root / "Props/Directory.Packages.props").read_bytes(), before_catalog)
        self.assertEqual(_run(["status", "--porcelain"], self.builds.root), before_status)
        self.assertFalse((self.source.root / "references/Builds/.git").exists())
        self.assertEqual(self.graph(commit)["edge_count"], 10)
        with mock.patch.object(dg, "_run_git_network") as network:
            dg.acquire_catalog_objects(self.root.root, commit, commit)
        network.assert_not_called()

    def test_catalog_preparation_acquires_missing_base_owner_without_checkout_or_nested_initialization(self) -> None:
        identity = "github.com/hexalith/hexalith.eventstore"
        owner = TempGitRepo(self.root.root / "references" / "EventStore")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        owner.write_text("README.md", "old owner inventory\n")
        old = owner.commit()
        owner.write_text("README.md", "current owner inventory\n")
        current = owner.commit()
        self.fixture.policy["trusted_identities"].append({"identity": identity, "local_path": "references/EventStore"})
        self.fixture.policy["semantic_profiles"][identity] = "fixture"
        self.fixture.policy["module_build_registry"][identity] = copy.deepcopy(self.fixture.policy["module_build_registry"][self.fixture.identity("Root")])
        self.root.add_submodule("EventStore", "references/EventStore", "https://github.com/hexalith/hexalith.eventstore.git", old)
        base = self.write_policy()
        self.root.link_gitlink("references/EventStore", current)
        candidate = self.root.commit()
        remote = pathlib.Path(self.temp.name) / "eventstore-remote"
        owner.root.rename(remote)
        _run(["clone", "--quiet", "--depth=1", remote.as_uri(), str(owner.root)], self.root.root)
        _run(["remote", "set-url", "origin", "https://untrusted.invalid/eventstore.git"], owner.root)
        self.assertNotEqual(dg._run_git(["cat-file", "-e", old], owner.root).returncode, 0)
        before = (owner.head(), _run(["status", "--porcelain"], owner.root), (owner.root / "README.md").read_bytes())
        def fetch(args, cwd):
            self.assertEqual(args, ["-c", "submodule.recurse=false", "fetch", "--no-recurse-submodules", "--no-tags", dg._canonical_clone_url(identity), old])
            self.assertEqual(cwd, owner.root)
            return dg._run_git([*args[:-2], remote.as_uri(), old], cwd), 1
        original = dg._collect_edges
        def collect(*args):
            self.assertEqual(dg._run_git(["cat-file", "-e", old + "^{commit}"], owner.root).returncode, 0)
            return original(*args)
        for status in (128, 0):
            with self.subTest(refused_fetch_status=status), mock.patch.object(dg, "_run_git_network", return_value=(subprocess.CompletedProcess([], status, b"", b"not our ref"), 1)):
                with self.assertRaises(dg.GraphError):
                    dg.acquire_catalog_objects(self.root.root, base, candidate)
        with mock.patch.object(dg, "_run_git_network", side_effect=fetch) as network, mock.patch.object(dg, "_collect_edges", side_effect=collect):
            dg.acquire_catalog_objects(self.root.root, base, candidate)
            network.assert_called_once()
        self.assertEqual((owner.head(), _run(["status", "--porcelain"], owner.root), (owner.root / "README.md").read_bytes()), before)
        self.assertFalse((self.source.root / "references/Builds/.git").exists())
        self.assertTrue(all(not (self.source.root / "references" / ("Inventory" + str(number)) / ".git").exists() for number in range(7)))

    def test_catalog_preparation_refuses_candidate_self_authorization_before_network(self) -> None:
        base = self.write_policy()
        self._make_catalog_object_unavailable()
        self.root.add_submodule("Outside", "references/Outside", "https://github.com/outside/catalog.git", "1" * 40)
        self.fixture.policy["trusted_identities"].append({"identity": "github.com/outside/catalog", "local_path": "references/Outside"})
        candidate = self.write_policy()
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "unknown/untrusted"):
            dg.acquire_catalog_objects(self.root.root, base, candidate)
        network.assert_not_called()

    def test_catalog_preparation_refuses_uninitialized_store_before_network(self) -> None:
        commit = self.write_policy()
        (self.builds.root / ".git").rename(self.builds.root / "git-store-backup")
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "initialized root Builds"):
            dg.acquire_catalog_objects(self.root.root, commit, commit)
        network.assert_not_called()

    def test_catalog_preparation_refuses_mismatched_root_mapping_before_network(self) -> None:
        base = self.write_policy()
        self._make_catalog_object_unavailable()
        modules = (self.root.root / ".gitmodules").read_text()
        self.root.write_text(".gitmodules", modules.replace("path = references/Builds", "path = references/OtherBuilds"))
        _run(["update-index", "--force-remove", "references/Builds"], self.root.root)
        self.root.link_gitlink("references/OtherBuilds", self.root_builds)
        candidate = self.root.commit()
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "policy-mapped root Builds"):
            dg.acquire_catalog_objects(self.root.root, base, candidate)
        network.assert_not_called()

    def test_catalog_preparation_rejects_absent_non_blob_and_oversized_catalogs(self) -> None:
        limit = len(BASELINE_CATALOG) + 100
        self.fixture.policy["resource_limits"]["max_catalog_blob_bytes"] = limit
        base = self.write_policy()
        for kind, error in (("absent", "missing Props"), ("tree", "not a blob"), ("oversized", "exceeds")):
            with self.subTest(kind=kind):
                _run(["rm", "-r", "-f", "Props/Directory.Packages.props"], self.builds.root)
                if kind == "tree":
                    self.builds.write_text("Props/Directory.Packages.props/entry", "unexpected directory")
                elif kind == "oversized":
                    self.builds.write_bytes("Props/Directory.Packages.props", b"x" * (limit + 1))
                selected = self.builds.commit()
                self.root.link_gitlink("references/Builds", selected)
                candidate = self.root.commit()
                with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, error):
                    dg.acquire_catalog_objects(self.root.root, base, candidate)
                network.assert_not_called()
                if kind == "absent":
                    self.builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
                    self.builds.commit()

    def test_catalog_preparation_refuses_zero_base_before_network(self) -> None:
        commit = self.write_policy()
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "active event base"):
            dg.acquire_catalog_objects(self.root.root, "0" * 40, commit)
        network.assert_not_called()

    def test_catalog_preparation_keeps_authoritative_fetch_failure(self) -> None:
        commit = self.write_policy()
        self._make_catalog_object_unavailable()
        failure = subprocess.CompletedProcess([], 128, b"", b"fatal: not our ref")
        with mock.patch.object(dg, "_run_git_network", return_value=(failure, 1)) as network:
            with self.assertRaisesRegex(dg.GraphError, "failed to acquire exact approved catalog.*not our ref"):
                dg.acquire_catalog_objects(self.root.root, commit, commit)
        network.assert_called_once()
        with self.assertRaises(dg.GraphError):
            self.graph(commit)

    def test_catalog_preparation_verifies_fetched_object_even_after_successful_fetch(self) -> None:
        commit = self.write_policy()
        self._make_catalog_object_unavailable()
        success = subprocess.CompletedProcess([], 0, b"", b"")
        with mock.patch.object(dg, "_run_git_network", return_value=(success, 1)):
            with self.assertRaisesRegex(dg.GraphError, "cat-file"):
                dg.acquire_catalog_objects(self.root.root, commit, commit)

    def test_catalog_preparation_enforces_edge_ceiling_before_network(self) -> None:
        self.fixture.policy["resource_limits"]["max_edges"] = 1
        commit = self.write_policy()
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "edge ceiling"):
            dg.acquire_catalog_objects(self.root.root, commit, commit)
        network.assert_not_called()

    def test_candidate_cannot_authorize_its_own_projection_or_expectations(self) -> None:
        row = self.fixture.policy.pop("consumer_source_projections")
        base = self.write_policy()
        self.fixture.policy["consumer_source_projections"] = row
        active = self.write_policy()
        with mock.patch("sys.stdout"):
            self.assertEqual(dg.main(["--root", str(self.root.root), "diff", "--event", "push", "--event-base", base, "--candidate", active]), 1)
        self.root.write_text("README.md", "after delayed activation\n")
        candidate = self.root.commit()
        with mock.patch("sys.stdout"):
            self.assertEqual(dg.main(["--root", str(self.root.root), "diff", "--event", "push", "--event-base", active, "--candidate", candidate]), 0)

    def test_forged_affected_evidence_refuses_before_materialization_or_execution(self) -> None:
        commit = self.write_policy()
        graph = self.graph(commit)
        semantics = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)
        forged = copy.deepcopy(graph)
        next(edge for edge in forged["edges"] if edge["repository"] in self.untrusted)["commit"] = "f" * 40
        self.resign(forged)
        evidence = dg.diff_graphs(graph, forged, self.fixture.policy, event="push", event_base=commit,
                                  candidate=commit, merge_base=None, release_eligible=True)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, commit)
        evidence.update({"dependency_policy": coordinate, "base_semantics": semantics, "candidate_semantics": semantics})
        evidence["evidence_digest"] = dg.canonical_digest({key: value for key, value in evidence.items() if key != "evidence_digest"})
        with mock.patch.object(dg, "materialize_repository_tree") as materialize, \
                mock.patch.object(dg, "materialize_contract_tree") as contract, self.assertRaisesRegex(dg.GraphError, "raw inventory"):
            dg.run_affected_builds(self.root.root, evidence, self.root.root / "output")
        materialize.assert_not_called()
        contract.assert_not_called()

    def test_live_source_handoff_and_release_consumer_recheck_the_projection(self) -> None:
        import dependency_handoff as handoff
        import release_evidence as release
        base = self.write_policy()
        graph = self.graph(base)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, base)
        evidence = {"revisions": {"event": "push", "release_eligible": True, "event_base": base, "candidate": base},
                    "dependency_policy": coordinate, "candidate_graph": graph}
        proof = handoff.create_source_proof(run_id=7, run_attempt=1, evidence=evidence)
        handoff.validate_source_proof(proof, root=self.root.root)
        self.root.write_text(self.project, self.project_xml.replace('</ItemGroup>', '<Compile Include="$(CanonicalSourceRoot)/Other.cs" /></ItemGroup>'))
        changed = self.root.commit()
        raw = dg._collect_raw_graph(self.root.root, self.fixture.identity("Root"), changed, self.fixture.policy)
        evidence["revisions"]["candidate"] = changed
        evidence["candidate_graph"] = raw
        proof = handoff.create_source_proof(run_id=8, run_attempt=1, evidence=evidence)
        with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
            handoff.validate_source_proof(proof, root=self.root.root)
        manifest = {"dependency_graph": raw, "dependency_policy": coordinate, "workflow_provenance": {}, "commit_sha": changed}
        with mock.patch.object(release, "_load_dependency_graph_engine", return_value=dg):
            diagnostics = release._live_manifest_v2_diagnostics(manifest, self.root.root)
        self.assertTrue(any("Compile/ProjectReference configuration changed" in value for value in diagnostics), diagnostics)

    def test_zero_base_projection_diagnostics_cannot_acquire_or_execute_sources(self) -> None:
        commit = self.write_policy()
        destination = pathlib.Path(self.temp.name) / "unavailable-base"
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "active base"):
            dg.acquire_object_stores(self.root.root, destination, "0" * 40, commit)
        network.assert_not_called()
        self.assertFalse(destination.exists())

    def test_acquisition_uses_only_trusted_metadata_and_never_the_seven_inventory_sources(self) -> None:
        commit = self.write_policy()
        original = dg._ensure_commit_available
        calls = []
        def ensure(local, sha):
            calls.append(sha)
            return original(local, sha)
        by_identity = {self.fixture.identity(name): repo.root for name, repo in self.fixture.repos.items()}
        def clone(args, **kwargs):
            identity = dg.normalize_identity(args[-2], "fixture remote")
            self.assertNotIn(identity, self.untrusted)
            result = subprocess.run(["git", "clone", "--quiet", "--no-checkout", str(by_identity[identity]), args[-1]], capture_output=True)
            return result, 1
        destination = pathlib.Path(self.temp.name) / "acquired"
        with mock.patch.object(dg, "_run_git_network", side_effect=clone), mock.patch.object(dg, "_ensure_commit_available", side_effect=ensure):
            result = dg.acquire_object_stores(self.root.root, destination, commit, commit)
        self.assertEqual(result["direct_edges"], 16)
        self.assertTrue(all(f"{number + 1:040x}" not in calls for number in range(7)))
        self.assertIn(self.nested_builds, calls)
        self.assertTrue(all(not (destination / "references" / ("Inventory" + str(number))).exists() for number in range(7)))


class QualityCatalogPreparationWorkflowTests(unittest.TestCase):
    def setUp(self) -> None:
        workflow = (ROOT / ".github/workflows/quality.yml").read_text()
        step = workflow.split("      - name: Prepare exact governance catalog objects\n", 1)[1]
        self.step = step.split("\n      # Gate 2b:", 1)[0]
        self.script = "\n".join(line[10:] for line in self.step.split("        run: |\n", 1)[1].splitlines())
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = pathlib.Path(self.temp.name)
        self.capture = self.root / "argv.txt"
        stub = self.root / "python3"
        stub.write_text('#!/bin/sh\nprintf "%s\\n" "$@" > "$CAPTURE"\n')
        stub.chmod(0o755)

    def run_event(self, event: str, selected_ref: str = "refs/heads/main") -> subprocess.CompletedProcess:
        env = os.environ.copy()
        env.update(EVENT_NAME=event, PULL_REQUEST_BASE="1" * 40, PUSH_BEFORE="2" * 40,
                   CANDIDATE="3" * 40, SELECTED_REF=selected_ref, CAPTURE=str(self.capture))
        env["PATH"] = str(self.root) + os.pathsep + os.defpath
        return subprocess.run(["bash", "-c", self.script], env=env, capture_output=True, text=True)

    def test_supported_events_select_the_exact_base_and_candidate(self) -> None:
        for event, base in (("pull_request", "1" * 40), ("push", "2" * 40), ("workflow_dispatch", "3" * 40)):
            with self.subTest(event=event):
                result = self.run_event(event)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual(self.capture.read_text().splitlines(), ["eng/dependency_graph.py", "--root", ".",
                    "acquire-catalogs", "--event-base", base, "--candidate", "3" * 40])

    def test_unlanded_manual_refs_and_unknown_events_refuse_before_acquisition(self) -> None:
        for event, selected_ref in (("workflow_dispatch", "refs/heads/unmerged"), ("workflow_dispatch", "refs/tags/unreviewed"),
                                    ("unsupported", "refs/heads/main")):
            with self.subTest(event=event, selected_ref=selected_ref):
                result = self.run_event(event, selected_ref)
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse(self.capture.exists())

    def test_revision_environment_bindings_are_exact(self) -> None:
        for name, expression in (("EVENT_NAME", "github.event_name"), ("PULL_REQUEST_BASE", "github.event.pull_request.base.sha"),
                                 ("PUSH_BEFORE", "github.event.before"), ("CANDIDATE", "github.sha"), ("SELECTED_REF", "github.ref")):
            self.assertIn(f"          {name}: ${{{{ {expression} }}}}", self.step)


class ApprovedProjectionExpectationTests(unittest.TestCase):
    def test_compiler_immutable_profile_accepts_exact_literal_and_refuses_other_versions(self) -> None:
        policy = dg.load_policy(ROOT / "eng/dependency-graph-policy.json")
        expected = policy["profiles"][policy["semantic_profiles"]["github.com/hexalith/hexalith.frontcomposer"]]["selected_catalog_required_packages"]["System.Collections.Immutable"]
        self.assertEqual("10.0.15", expected)
        for version in ("10.0.15", "10.0.14", "11.0.0", "11.0.1"):
            catalog = ET.fromstring('<Project><ItemGroup><PackageVersion Include="System.Collections.Immutable" Version="'
                                   + version + '" /></ItemGroup></Project>')
            with self.subTest(version=version):
                if version == expected:
                    dg.assert_frontcomposer_immutable_version(catalog, expected, "compiler compatibility profile", None)
                else:
                    with self.assertRaisesRegex(dg.GraphError, "expected version"):
                        dg.assert_frontcomposer_immutable_version(catalog, expected, "compiler compatibility profile", None)

    def test_historical_immutable_view_retains_its_exact_policy_value(self) -> None:
        fixture = PolicyShapeTests("test_immutable_consumer_view_satisfies_existing_policy_without_normalization")
        catalog = ET.fromstring(fixture._immutable_consumer_catalog())
        selected = list(catalog.iter("HexalithImmutableVersion"))[1]
        selected.text = "11.0.0"
        # A historical policy can still prove its own exact view. The current
        # compiler compatibility policy refuses those bytes without normalizing them.
        dg.assert_frontcomposer_immutable_version(catalog, "11.0.0", "historical source profile", None)
        with self.assertRaisesRegex(dg.GraphError, "must select policy version"):
            dg.assert_frontcomposer_immutable_version(catalog, "10.0.15", "compiler compatibility profile", None)

    def test_exact_two_policy_expectations_and_older_wrong_missing_catalog_negatives(self) -> None:
        policy = dg.load_policy(ROOT / "eng/dependency-graph-policy.json")
        requirements = policy["profiles"]["frontcomposer-catalog-v1"]["selected_catalog_required_packages"]
        for name, approved, newer in (("Microsoft.Extensions.Localization", "11.0.0", "11.0.1"),
                                      ("System.Collections.Immutable", "10.0.15", "11.0.0")):
            self.assertEqual(requirements[name], approved)
            for version in (approved, "10.0.14", newer, None):
                catalog = ET.fromstring('<Project><ItemGroup>' + (
                    f'<PackageVersion Include="{name}" Version="{version}" />' if version else '') + '</ItemGroup></Project>')
                with self.subTest(package=name, version=version):
                    if version == approved:
                        dg.assert_authoritative_package_version(catalog, name, requirements[name], "approved expectation")
                    else:
                        with self.assertRaises(dg.GraphError):
                            dg.assert_authoritative_package_version(catalog, name, requirements[name], "approved expectation")


if __name__ == "__main__":
    unittest.main()
