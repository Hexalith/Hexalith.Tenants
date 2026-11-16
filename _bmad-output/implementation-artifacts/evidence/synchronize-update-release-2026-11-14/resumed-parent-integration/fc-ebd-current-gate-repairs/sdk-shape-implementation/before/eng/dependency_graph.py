#!/usr/bin/env python3
"""Committed-object dependency-graph engine (GOV-1 / FC-DEP-1).

Collects the bounded depth-1/depth-2 `hexalith.dependency-graph.v1` gitlink graph from
an explicit FrontComposer root commit, computes its AD-5 canonical digest, evaluates
every Builds-selector edge under its AD-6 semantic profile, and produces the deterministic
AD-8 graph diff and affected-module proof used by CI. This is the single canonical
semantic and affected-module policy implementation; callers consume its machine-readable
results rather than reimplementing policy.

The exact-source CI proof is FrontComposer's approved replacement for the unrealized AD-16
evaluator handoff. This helper implements the repository-owned graph, policy, diff,
materialization, and proof surfaces without fabricating an immutable identity for the shared
CI workflow.
"""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import re
import shutil
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

__version__ = "2.0.0"

SCHEMA = "hexalith.dependency-graph.v1"
POLICY_SCHEMA = "hexalith.dependency-graph-policy.v1"
DIFF_SCHEMA = "hexalith.dependency-graph-diff.v1"
POLICY_PATH = "eng/dependency-graph-policy.json"

# A shared-catalog property may carry only the canonical "default it if unset" condition.
_SELF_DEFAULT_CONDITION = re.compile(r"^\s*'\$\((?P<name>[A-Za-z_][A-Za-z0-9_]*)\)'\s*==\s*''\s*$")
_NUGET_VERSION = re.compile(
    r"^(0|[1-9][0-9]*)(?:\.(0|[1-9][0-9]*)){0,3}"
    r"(?:-(?:0|[1-9][0-9]*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)"
    r"(?:\.(?:0|[1-9][0-9]*|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*))*)?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)

# Every key a semantic profile may declare. An unknown key -- a rename or a typo in
# selected_catalog_required_properties, say -- would otherwise silently validate nothing,
# so load_policy rejects it instead of defaulting the missing key to an empty mapping.
_PROFILE_KEYS = frozenset({
    "owner_checks",
    "selected_catalog_required_property_names",
    "selected_catalog_required_properties",
    "selected_catalog_required_package_names",
    "selected_catalog_required_packages",
})

# Every owner_checks key evaluate_semantics understands. A typo such as
# `no_package_version_row` would otherwise leave `.get(...)` returning None while
# validate still reports ok: true — the same vacuous-check defect profile keys close.
_OWNER_CHECK_KEYS = frozenset({
    "canonical_source_files",
    "bom_crlf_on_selected_catalog",
    "guarded_imports",
    "no_inline_versions_in_tracked_files",
    "no_local_override_for_selected_catalog_packages",
    "no_minver",
    "no_package_version_rows",
    "override_not_enabled",
    "well_formed_project_root",
})

# Boolean owner_checks are truthy flags. Object-shaped checks carry nested configuration
# and must not accept `true` / `{}` / wrong types that evaluate would TypeError or skip.
_OWNER_CHECK_BOOLEAN_KEYS = frozenset({
    "bom_crlf_on_selected_catalog",
    "no_local_override_for_selected_catalog_packages",
    "no_minver",
    "no_package_version_rows",
    "override_not_enabled",
    "well_formed_project_root",
})
_OWNER_CHECK_OBJECT_KEYS = frozenset({
    "canonical_source_files",
    "guarded_imports",
    "no_inline_versions_in_tracked_files",
})


class GraphError(Exception):
    """Raised for any fail-closed condition during collection or semantic evaluation."""


# ---------------------------------------------------------------------------
# Git plumbing — argv-only subprocess calls, never shell interpolation.
# `.gitmodules` and nested repository content are untrusted candidate data.
# ---------------------------------------------------------------------------


def _run_git(args: list[str], cwd: Path) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(["git", *args], cwd=str(cwd), capture_output=True, check=False)


def _git_ok(args: list[str], cwd: Path) -> bytes:
    proc = _run_git(args, cwd)
    if proc.returncode != 0:
        raise GraphError(
            f"git {' '.join(args)} failed in {cwd}: {proc.stderr.decode('utf-8', 'replace').strip()}"
        )
    return proc.stdout


# Remote acquisition is this engine's only network-bound surface. A hosted-runner TLS, DNS,
# or HTTP blip against an approved remote is transient infrastructure noise, not a governance
# verdict, yet it aborts the CI step before the diff gate ever runs (observed 2026-08-11:
# `server certificate verification failed` while cloning hexalith.tenants). Retrying the exact
# same argv cannot widen what is accepted -- the requested commit is still verified by
# `cat-file -e` after every fetch, and clone URLs are still reconstructed from policy identity
# -- so a bounded retry keeps the fail-closed contract while surviving the blip.
NETWORK_RETRY_ATTEMPTS = 3
NETWORK_RETRY_BACKOFF_SECONDS = 2.0

# Only infrastructure-shaped failures retry. Authoritative answers -- a missing repository, an
# unknown ref, a rejected credential -- are governance facts and must fail closed immediately.
_TRANSIENT_NETWORK_MARKERS = (
    "could not resolve host",
    "could not resolve proxy",
    "connection reset by peer",
    "connection timed out",
    "early eof",
    "failed to connect to",
    "gnutls_handshake() failed",
    "operation timed out",
    "remote end hung up unexpectedly",
    "rpc failed",
    "server certificate verification failed",
    "ssl connect error",
    "ssl_error_syscall",
    "the requested url returned error: 5",
    "unexpected disconnect",
)


def _is_transient_network_failure(stderr: str) -> bool:
    lowered = stderr.lower()
    return any(marker in lowered for marker in _TRANSIENT_NETWORK_MARKERS)


def _run_git_network(
    args: list[str],
    cwd: Path | None = None,
    *,
    reset_before_retry: Path | None = None,
) -> tuple[subprocess.CompletedProcess[bytes], int]:
    """Run one network-bound git argv, retrying only transient acquisition failures.

    `reset_before_retry` names a clone destination to remove between attempts so a partially
    written tree cannot turn a retryable blip into a hard "destination already exists" error.
    """
    attempt = 1
    while True:
        proc = subprocess.run(
            ["git", *args],
            cwd=None if cwd is None else str(cwd),
            capture_output=True,
            check=False,
        )
        if proc.returncode == 0 or attempt >= NETWORK_RETRY_ATTEMPTS:
            return proc, attempt
        if not _is_transient_network_failure(proc.stderr.decode("utf-8", "replace")):
            return proc, attempt
        if reset_before_retry is not None:
            shutil.rmtree(reset_before_retry, ignore_errors=True)
        time.sleep(NETWORK_RETRY_BACKOFF_SECONDS * attempt)
        attempt += 1


def _attempts_note(attempts: int) -> str:
    return "" if attempts <= 1 else f" after {attempts} attempts"


_COMMIT_RE = re.compile(r"^[0-9a-f]{40}$")


def require_commit(value: str, context: str) -> str:
    if not isinstance(value, str) or not _COMMIT_RE.match(value):
        raise GraphError(f"{context}: expected a strict lowercase 40-hex commit, got {value!r}")
    return value


def _tree_entry(local_path: Path, commit: str, path: str) -> tuple[str, str, str] | None:
    """Return (mode, type, sha) for an exact path at commit, or None if absent."""
    out = _git_ok(["ls-tree", commit, "--", path], local_path)
    line = out.decode("utf-8").strip()
    if not line:
        return None
    meta, _, entry_path = line.partition("\t")
    mode, obj_type, sha = meta.split()
    if entry_path != path:
        return None
    return mode, obj_type, sha


def _blob_size(local_path: Path, blob_sha: str) -> int:
    return int(_git_ok(["cat-file", "-s", blob_sha], local_path).decode("ascii").strip())


def read_blob(local_path: Path, commit: str, path: str, max_bytes: int, context: str) -> bytes | None:
    entry = _tree_entry(local_path, commit, path)
    if entry is None:
        return None
    _mode, obj_type, sha = entry
    if obj_type != "blob":
        raise GraphError(f"{context}: {path} at {commit} is a {obj_type}, not a blob")
    size = _blob_size(local_path, sha)
    if size > max_bytes:
        raise GraphError(f"{context}: {path} at {commit} is {size} bytes, exceeds the {max_bytes}-byte ceiling")
    return _git_ok(["cat-file", "blob", sha], local_path)


def list_gitlinks(local_path: Path, commit: str, max_ls_tree_bytes: int) -> dict[str, str]:
    """Every mode-160000 (gitlink) tree entry at commit: path -> commit sha."""
    out = _git_ok(["ls-tree", "-r", "-z", "--full-tree", commit], local_path)
    if len(out) > max_ls_tree_bytes:
        raise GraphError(
            f"ls-tree output for {commit} in {local_path} is {len(out)} bytes, "
            f"exceeds the {max_ls_tree_bytes}-byte ceiling"
        )
    gitlinks: dict[str, str] = {}
    for record in out.split(b"\x00"):
        if not record:
            continue
        meta, _, path_bytes = record.partition(b"\t")
        _mode, obj_type, sha = meta.split()
        if obj_type == b"commit":
            gitlinks[path_bytes.decode("utf-8")] = sha.decode("ascii")
    return gitlinks


_GITMODULES_KEY_RE = re.compile(r"^submodule\.(?P<name>.+)\.(?P<field>path|url)$")


def read_gitmodules(local_path: Path, commit: str, max_bytes: int) -> dict[str, dict[str, str]]:
    """Parse committed `.gitmodules` at commit via `git config --blob`. name -> {path, url}."""
    entry = _tree_entry(local_path, commit, ".gitmodules")
    if entry is None:
        return {}
    _mode, obj_type, sha = entry
    if obj_type != "blob":
        raise GraphError(f".gitmodules at {commit} in {local_path} is a {obj_type}, not a blob")
    size = _blob_size(local_path, sha)
    if size > max_bytes:
        raise GraphError(f".gitmodules at {commit} in {local_path} is {size} bytes, exceeds the {max_bytes}-byte ceiling")
    out = _git_ok(["config", "--blob", f"{commit}:.gitmodules", "--list"], local_path)
    entries: dict[str, dict[str, str]] = {}
    for line in out.decode("utf-8").splitlines():
        if not line:
            continue
        key, _, value = line.partition("=")
        m = _GITMODULES_KEY_RE.match(key)
        if not m:
            continue
        entries.setdefault(m.group("name"), {})[m.group("field")] = value
    return entries


# ---------------------------------------------------------------------------
# AD-3 identity/path normalization — closed-world, no clones, no candidate URLs.
# ---------------------------------------------------------------------------

_HTTPS_RE = re.compile(r"^https://github\.com/(?P<owner>[A-Za-z0-9._-]+)/(?P<repo>[A-Za-z0-9._-]+?)(?:\.git)?/?$")
_SSH_SCP_RE = re.compile(r"^git@github\.com:(?P<owner>[A-Za-z0-9._-]+)/(?P<repo>[A-Za-z0-9._-]+?)(?:\.git)?/?$")
_SSH_URL_RE = re.compile(r"^ssh://git@github\.com/(?P<owner>[A-Za-z0-9._-]+)/(?P<repo>[A-Za-z0-9._-]+?)(?:\.git)?/?$")
_CONTROL_RE = re.compile(r"[\x00-\x1f\x7f]")


def normalize_identity(url: str, context: str) -> str:
    if not isinstance(url, str) or not url:
        raise GraphError(f"{context}: empty repository URL")
    if _CONTROL_RE.search(url):
        raise GraphError(f"{context}: control characters in repository URL {url!r}")
    if "%" in url:
        raise GraphError(f"{context}: percent-escapes are not permitted in repository URL {url!r}")

    match = _HTTPS_RE.match(url) or _SSH_SCP_RE.match(url) or _SSH_URL_RE.match(url)
    if not match:
        raise GraphError(f"{context}: unrecognized or unsafe repository URL {url!r}")

    owner, repo = match.group("owner"), match.group("repo")
    for segment, label in ((owner, "owner"), (repo, "repository")):
        if segment in (".", ".."):
            raise GraphError(f"{context}: unsafe {label} segment in repository URL {url!r}")

    return f"github.com/{owner.lower()}/{repo.lower()}"


def normalize_path(path: str, context: str) -> str:
    if not isinstance(path, str) or not path:
        raise GraphError(f"{context}: empty path")
    if _CONTROL_RE.search(path) or "\\" in path:
        raise GraphError(f"{context}: unsafe path {path!r}")
    if path.startswith("/"):
        raise GraphError(f"{context}: absolute path not permitted: {path!r}")
    for segment in path.split("/"):
        if segment in ("", ".", ".."):
            raise GraphError(f"{context}: unsafe path segment in {path!r}")
    return path


def resolve_local_path(policy: dict[str, Any], identity: str, root_dir: Path) -> Path:
    for entry in policy["trusted_identities"]:
        if entry["identity"] == identity:
            return (root_dir / entry["local_path"]).resolve()
    raise GraphError(f"unknown/untrusted repository identity {identity!r}")


# A consumer projection never promotes an inventory identity into global trust.
# It seals one already-trusted owner tree and permits its raw, non-executable
# inventory to remain visible without resolving those unconsumed repositories.
def _consumer_projection(policy: dict[str, Any], identity: str, commit: str) -> dict[str, Any] | None:
    row = policy.get("consumer_source_projections", {}).get(identity)
    if row is not None and policy.get("module_build_registry", {}).get(identity, {}).get("disposition") != "evidence-only":
        raise GraphError("consumer projection owner must remain evidence-only; whole-owner execution is forbidden")
    if row is not None and row["owner_commit"] != commit:
        raise GraphError(f"{identity}@{commit}: consumer source projection owner pin changed")
    return row


def _validate_projection_inventory(
    local_path: Path, commit: str, owner_identity: str, edges: list[dict[str, Any]],
    policy: dict[str, Any],
) -> dict[str, Any] | None:
    row = _consumer_projection(policy, owner_identity, commit)
    if row is None:
        return None
    raw = read_blob(local_path, commit, ".gitmodules", policy["resource_limits"]["max_gitmodules_blob_bytes"], owner_identity)
    if raw is None or hashlib.sha256(raw).hexdigest() != row["gitmodules_sha256"]:
        raise GraphError(f"{owner_identity}@{commit}: sealed consumer inventory .gitmodules changed")
    if canonical_digest(edges) != row["inventory_sha256"]:
        raise GraphError(f"{owner_identity}@{commit}: sealed consumer raw inventory changed")
    trusted = {entry["identity"] for entry in policy["trusted_identities"]}
    untrusted = sorted({edge["repository"] for edge in edges} - trusted)
    if untrusted != row["inventory_only_identities"]:
        raise GraphError(f"{owner_identity}@{commit}: inventory-only identity set changed")
    for path, digest in row["source_files"].items():
        blob = read_blob(local_path, commit, path, policy["resource_limits"]["max_contract_tree_blob_bytes"], owner_identity)
        if blob is None or hashlib.sha256(blob).hexdigest() != digest:
            raise GraphError(f"{owner_identity}@{commit}: consumer canonical source {path!r} changed")
    return row


def consumer_configuration(root_dir: Path, commit: str, policy: dict[str, Any]) -> dict[str, str]:
    """Seal committed MSBuild configuration, not an evaluated/mutable worktree.

    Together with every root selector, this closes indirect/aliased/wildcard
    Compile/ProjectReference changes without executing candidate MSBuild code.
    Automatic project .user imports and directory/MSBuild .rsp inputs are sealed
    too, even when the project itself has not changed.
    """
    limits = policy["resource_limits"]
    paths = [path for path in list_tracked_files(root_dir, commit, [""], limits["max_ls_tree_bytes_per_owner_commit"])
             if path.endswith((".csproj", ".props", ".targets", ".proj", ".slnx", ".user", ".rsp"))
             or Path(path).name == "global.json" or Path(path).name.lower() == "nuget.config"]
    if len(paths) > limits["max_contract_tree_files"]:
        raise GraphError("consumer configuration exceeds the file ceiling")
    result = {}
    total_bytes = 0
    for path in paths:
        blob = read_blob(root_dir, commit, path, limits["max_contract_tree_blob_bytes"], "consumer configuration")
        if blob is None:
            raise GraphError(f"missing committed consumer configuration {path!r}")
        total_bytes += len(blob)
        if total_bytes > limits["max_contract_tree_total_bytes"]:
            raise GraphError("consumer configuration exceeds the aggregate byte ceiling")
        result[path] = hashlib.sha256(blob).hexdigest()
    return result


def evaluate_consumer_projections(
    root_dir: Path, policy: dict[str, Any], envelope: dict[str, Any],
) -> dict[str, Any]:
    if not policy.get("consumer_source_projections"):
        return {}
    validate_graph_envelope(envelope)
    reports = {}
    root = envelope["root"]
    direct = [edge for edge in envelope["edges"] if edge["depth"] == 1]
    actual_direct = _owner_edges(root_dir, root["commit"], root["repository"], 1, policy, root_dir)
    supplied_direct = [{key: edge[key] for key in ("owner_repository", "owner_commit", "path", "repository", "commit", "depth")}
                       for edge in direct]
    supplied_direct.sort(key=lambda edge: edge["path"])
    if supplied_direct != actual_direct:
        raise GraphError("consumer root selectors do not match committed root edges")
    # Authenticate raw catalog evidence even when no projection owner is selected.
    # A valid envelope/digest alone does not prove its root selectors or catalogs.
    for edge in direct:
        if edge["repository"] != policy["builds_identity"]:
            continue
        catalog = read_blob(resolve_local_path(policy, policy["builds_identity"], root_dir), edge["commit"],
                            "Props/Directory.Packages.props", policy["resource_limits"]["max_catalog_blob_bytes"], "raw root Builds metadata")
        if catalog is None or edge.get("catalog_sha256") != hashlib.sha256(catalog).hexdigest() or edge.get("catalog_contract_version") is not None:
            raise GraphError("consumer raw root Builds catalog evidence changed or unavailable")
    selector_projection = [{key: edge[key] for key in ("path", "repository", "commit")} for edge in direct]
    for identity, row in policy.get("consumer_source_projections", {}).items():
        selected = [edge for edge in direct if edge["repository"] == identity]
        if not selected:
            continue  # Earlier roots without this dependency gain no authority.
        if len(selected) != 1 or selected[0]["commit"] != row["owner_commit"]:
            raise GraphError("consumer projection requires one exact root-selected owner")
        owner_local = resolve_local_path(policy, identity, root_dir)
        actual_inventory = _owner_edges(owner_local, row["owner_commit"], identity, 2, policy, root_dir)
        supplied_inventory = [{key: edge[key] for key in ("owner_repository", "owner_commit", "path", "repository", "commit", "depth")}
                              for edge in envelope["edges"] if edge["depth"] == 2 and edge["owner_repository"] == identity]
        supplied_inventory.sort(key=lambda edge: edge["path"])
        if supplied_inventory != actual_inventory:
            raise GraphError("consumer raw inventory does not match committed owner edges")
        for edge in envelope["edges"]:
            if edge["depth"] != 2 or edge["owner_repository"] != identity or edge["repository"] != policy["builds_identity"]:
                continue
            catalog = read_blob(resolve_local_path(policy, policy["builds_identity"], root_dir), edge["commit"],
                                "Props/Directory.Packages.props", policy["resource_limits"]["max_catalog_blob_bytes"], "raw nested Builds metadata")
            if catalog is None or edge.get("catalog_sha256") != hashlib.sha256(catalog).hexdigest() or edge.get("catalog_contract_version") is not None:
                raise GraphError("consumer raw nested Builds catalog evidence changed or unavailable")
        if root["repository"] != row["consumer_repository"]:
            raise GraphError("consumer projection repository does not match explicit root")
        if canonical_digest(selector_projection) != row["root_selectors_sha256"]:
            raise GraphError("consumer projection root source selectors changed")
        builds = [edge for edge in direct if edge["repository"] == policy["builds_identity"]]
        if len(builds) != 1 or builds[0]["commit"] != row["builds_commit"]:
            raise GraphError("consumer helper must use the exact approved root Builds source")
        configuration = consumer_configuration(root_dir, root["commit"], policy)
        configuration_hash = canonical_digest(configuration)
        if configuration_hash != row["consumer_configuration_sha256"]:
            raise GraphError("consumer Compile/ProjectReference configuration changed from its sealed source projection")
        project_blob = read_blob(root_dir, root["commit"], row["consumer_project"],
                                 policy["resource_limits"]["max_contract_tree_blob_bytes"], "consumer project")
        if project_blob is None:
            raise GraphError("consumer source project is missing")
        project = parse_project_xml(project_blob, "consumer source project")
        expected = {"$(" + row["source_root_property"] + ")/" + path: link
                    for path, link in row["compile_links"].items()}
        observed = {}
        for item in project.iter("Compile"):
            include = item.get("Include", "").replace("\\", "/")
            if row["source_root_property"] not in include:
                continue
            if include not in expected or dict(item.attrib) != {"Include": include, "Link": expected[include]} or include in observed:
                raise GraphError("consumer canonical Compile links are not the exact sealed pair")
            observed[include] = expected[include]
        if observed != expected:
            raise GraphError("consumer canonical Compile pair is missing")
        tooling = "$(HexalithBuildsRoot)/" + row["builds_project"]
        references = [item for item in project.iter("ProjectReference") if item.get("Include", "").replace("\\", "/") == tooling]
        if len(references) != 1 or references[0].get("IsAspireProjectResource") != "false":
            raise GraphError("consumer helper requires the existing root Builds.Tooling source reference")
        tooling_blob = read_blob(resolve_local_path(policy, policy["builds_identity"], root_dir), row["builds_commit"],
                                 row["builds_project"], policy["resource_limits"]["max_contract_tree_blob_bytes"], "root Builds tooling")
        if tooling_blob is None:
            raise GraphError("approved root Builds.Tooling project object is missing")
        report = {"schema": "hexalith.consumer-source-projection.v1", "owner_repository": identity,
                  "owner_commit": row["owner_commit"], "consumer_repository": root["repository"],
                  "consumer_commit": root["commit"], "consumer_project": row["consumer_project"],
                  "source_files": row["source_files"], "root_builds_commit": row["builds_commit"],
                  "inventory_sha256": row["inventory_sha256"], "root_selectors_sha256": row["root_selectors_sha256"],
                  "consumer_configuration_sha256": configuration_hash}
        report["projection_digest"] = canonical_digest(report)
        reports[identity] = report
    return reports


def _inventory_only_edge(edge: dict[str, Any], policy: dict[str, Any]) -> bool:
    row = policy.get("consumer_source_projections", {}).get(edge["owner_repository"])
    return (row is not None and edge["depth"] == 2 and edge["owner_commit"] == row["owner_commit"]
            and edge["repository"] in row["inventory_only_identities"])


# ---------------------------------------------------------------------------
# AD-4/AD-5 — edge collection, canonical envelope, and digest.
# ---------------------------------------------------------------------------


def canonical_bytes(obj: Any) -> bytes:
    return json.dumps(obj, ensure_ascii=True, allow_nan=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def canonical_digest(obj: Any) -> str:
    return hashlib.sha256(canonical_bytes(obj)).hexdigest()


def _owner_edges(
    local_path: Path,
    commit: str,
    owner_identity: str,
    depth: int,
    policy: dict[str, Any],
    root_dir: Path,
) -> list[dict[str, Any]]:
    limits = policy["resource_limits"]
    modules = read_gitmodules(local_path, commit, limits["max_gitmodules_blob_bytes"])
    gitlinks = list_gitlinks(local_path, commit, limits["max_ls_tree_bytes_per_owner_commit"])

    path_to_identity: dict[str, str] = {}
    for name, fields in modules.items():
        if "path" not in fields or "url" not in fields:
            raise GraphError(f"{owner_identity}@{commit}: .gitmodules entry {name!r} missing path/url")
        path = normalize_path(fields["path"], f"{owner_identity}@{commit} .gitmodules[{name}]")
        identity = normalize_identity(fields["url"], f"{owner_identity}@{commit} .gitmodules[{name}]")
        # Unknown identities remain unresolved. Only an exact sealed depth-two
        # owner inventory can record them; root and every other context refuse.
        if not (depth == 2 and _consumer_projection(policy, owner_identity, commit) is not None):
            resolve_local_path(policy, identity, root_dir)
        if path in path_to_identity:
            raise GraphError(
                f"{owner_identity}@{commit}: duplicate .gitmodules path {path!r} "
                f"(entries map to {path_to_identity[path]!r} and {identity!r})"
            )
        path_to_identity[path] = identity

    declared_paths = {fields["path"] for fields in modules.values() if "path" in fields}
    missing = declared_paths - set(gitlinks.keys())
    if missing:
        raise GraphError(f"{owner_identity}@{commit}: declared .gitmodules paths missing a gitlink: {sorted(missing)}")

    edges: list[dict[str, Any]] = []
    for path, target_commit in sorted(gitlinks.items()):
        identity = path_to_identity.get(path)
        if identity is None:
            raise GraphError(f"{owner_identity}@{commit}: gitlink at {path!r} has no matching .gitmodules mapping")
        edges.append(
            {
                "owner_repository": owner_identity,
                "owner_commit": commit,
                "path": path,
                "repository": identity,
                "commit": target_commit,
                "depth": depth,
            }
        )
    if depth == 2:
        row = _validate_projection_inventory(local_path, commit, owner_identity, edges, policy)
        for edge in edges:
            if row is None or edge["repository"] not in row["inventory_only_identities"]:
                resolve_local_path(policy, edge["repository"], root_dir)
    return edges


def _collect_edges(root_dir: Path, root_identity: str, root_commit: str, policy: dict[str, Any]) -> list[dict[str, Any]]:
    """Read the bounded committed inventory without requiring catalog objects."""
    require_commit(root_commit, "root commit")
    limits = policy["resource_limits"]

    edges = _owner_edges(root_dir, root_commit, root_identity, 1, policy, root_dir)
    for depth1_edge in list(edges):
        owner_local = resolve_local_path(policy, depth1_edge["repository"], root_dir)
        edges.extend(
            _owner_edges(owner_local, depth1_edge["commit"], depth1_edge["repository"], 2, policy, root_dir)
        )

    if len(edges) > limits["max_edges"]:
        raise GraphError(f"graph has {len(edges)} edges, exceeds the {limits['max_edges']}-edge ceiling")
    return edges


def _collect_raw_graph(root_dir: Path, root_identity: str, root_commit: str, policy: dict[str, Any]) -> dict[str, Any]:
    limits = policy["resource_limits"]
    builds_identity = policy["builds_identity"]
    edges = _collect_edges(root_dir, root_identity, root_commit, policy)

    catalog_cache: dict[str, str] = {}
    for edge in edges:
        if edge["repository"] != builds_identity:
            continue
        if edge["commit"] not in catalog_cache:
            builds_local = resolve_local_path(policy, builds_identity, root_dir)
            blob = read_blob(
                builds_local,
                edge["commit"],
                "Props/Directory.Packages.props",
                limits["max_catalog_blob_bytes"],
                f"{builds_identity}@{edge['commit']}",
            )
            if blob is None:
                raise GraphError(f"{builds_identity}@{edge['commit']}: missing Props/Directory.Packages.props")
            catalog_cache[edge["commit"]] = hashlib.sha256(blob).hexdigest()
        edge["catalog_sha256"] = catalog_cache[edge["commit"]]
        # nullable until Hexalith.Builds supplies a marker (BUILD-CAT-1); see AD-6
        edge["catalog_contract_version"] = None

    edges.sort(key=lambda e: (e["depth"], e["owner_repository"], e["owner_commit"], e["path"], e["repository"], e["commit"]))

    envelope: dict[str, Any] = {
        "schema": SCHEMA,
        "root": {"repository": root_identity, "commit": root_commit},
        "edge_count": len(edges),
        "edges": edges,
    }
    envelope["graph_digest"] = canonical_digest(
        {"schema": envelope["schema"], "root": envelope["root"], "edge_count": envelope["edge_count"], "edges": envelope["edges"]}
    )
    return envelope


def collect_graph(root_dir: Path, root_identity: str, root_commit: str, policy: dict[str, Any]) -> dict[str, Any]:
    """Collect raw v1 provenance and enforce any active consumer projection.

    Every live handoff/release consumer shares this gate. The private raw collector
    is diagnostic metadata only and never authorizes materialization or execution.
    """
    envelope = _collect_raw_graph(root_dir, root_identity, root_commit, policy)
    evaluate_consumer_projections(root_dir, policy, envelope)
    return envelope


def validate_graph_envelope(envelope: Any) -> dict[str, Any]:
    """Validate and return one closed AD-5 graph envelope.

    This verifier is intentionally checkout-free. Live verification is performed by
    collecting the graph again from its sealed root commit and comparing the complete
    envelope.
    """
    if not isinstance(envelope, dict):
        raise GraphError("dependency graph must be a JSON object")
    expected_envelope = {"schema", "root", "edge_count", "edges", "graph_digest"}
    if set(envelope) != expected_envelope:
        raise GraphError(
            f"dependency graph members must be exactly {sorted(expected_envelope)}, found {sorted(envelope)}"
        )
    if envelope["schema"] != SCHEMA:
        raise GraphError(f"dependency graph schema must be {SCHEMA!r}")
    root = envelope["root"]
    if not isinstance(root, dict) or set(root) != {"repository", "commit"}:
        raise GraphError("dependency graph root must contain exactly repository and commit")
    if not isinstance(root["repository"], str) or re.fullmatch(
        r"github\.com/[a-z0-9._-]+/[a-z0-9._-]+", root["repository"]
    ) is None:
        raise GraphError("dependency graph root repository must be a normalized ASCII identity")
    require_commit(root["commit"], "dependency graph root commit")
    edges = envelope["edges"]
    if not isinstance(edges, list):
        raise GraphError("dependency graph edges must be an array")
    edge_count = envelope["edge_count"]
    if isinstance(edge_count, bool) or not isinstance(edge_count, int) or edge_count != len(edges):
        raise GraphError("dependency graph edge_count must be an integer equal to len(edges)")

    normalized: list[dict[str, Any]] = []
    identities: set[tuple[str, str, str]] = set()
    logical_identities: set[tuple[str, str, str]] = set()
    for index, edge in enumerate(edges):
        if not isinstance(edge, dict):
            raise GraphError(f"dependency graph edge[{index}] must be an object")
        base_members = {
            "owner_repository", "owner_commit", "path", "repository", "commit", "depth"
        }
        builds_members = base_members | {"catalog_sha256", "catalog_contract_version"}
        is_builds_edge = isinstance(edge.get("repository"), str) and edge["repository"].rsplit("/", 1)[-1] in (
            "builds", "hexalith.builds"
        )
        expected_members = builds_members if is_builds_edge else base_members
        if set(edge) != expected_members:
            raise GraphError(f"dependency graph edge[{index}] has an invalid closed member set")
        for name in ("owner_repository", "repository"):
            if not isinstance(edge[name], str) or re.fullmatch(
                r"github\.com/[a-z0-9._-]+/[a-z0-9._-]+", edge[name]
            ) is None:
                raise GraphError(f"dependency graph edge[{index}].{name} must be a normalized ASCII identity")
        require_commit(edge["owner_commit"], f"dependency graph edge[{index}] owner_commit")
        require_commit(edge["commit"], f"dependency graph edge[{index}] commit")
        normalize_path(edge["path"], f"dependency graph edge[{index}] path")
        if isinstance(edge["depth"], bool) or edge["depth"] not in (1, 2):
            raise GraphError(f"dependency graph edge[{index}] depth must be integer 1 or 2")
        if edge["depth"] == 1:
            if edge["owner_repository"] != root["repository"]:
                raise GraphError(
                    f"dependency graph edge[{index}] depth-1 owner_repository must equal the root repository"
                )
            if edge["owner_commit"] != root["commit"]:
                raise GraphError(
                    f"dependency graph edge[{index}] depth-1 owner_commit must equal the root commit"
                )
        if "catalog_sha256" in edge:
            if not isinstance(edge["catalog_sha256"], str) or not re.fullmatch(
                r"[0-9a-f]{64}", edge["catalog_sha256"]
            ):
                raise GraphError(f"dependency graph edge[{index}] catalog_sha256 must be lowercase 64-hex")
            marker = edge["catalog_contract_version"]
            if marker is not None and (
                not isinstance(marker, str)
                or re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", marker) is None
            ):
                raise GraphError(f"dependency graph edge[{index}] catalog marker is invalid")
        identity = (edge["owner_repository"], edge["owner_commit"], edge["path"])
        if identity in identities:
            raise GraphError(f"dependency graph contains duplicate edge identity {identity!r}")
        identities.add(identity)
        logical_identity = (edge["owner_repository"], edge["path"], edge["repository"])
        if logical_identity in logical_identities:
            raise GraphError(f"dependency graph contains duplicate logical edge {logical_identity!r}")
        logical_identities.add(logical_identity)
        normalized.append(edge)

    depth1_targets = {
        (edge["repository"], edge["commit"])
        for edge in normalized
        if edge["depth"] == 1
    }
    for index, edge in enumerate(normalized):
        if edge["depth"] == 2 and (edge["owner_repository"], edge["owner_commit"]) not in depth1_targets:
            raise GraphError(
                f"dependency graph edge[{index}] depth-2 owner must equal a depth-1 target repository and commit"
            )

    expected_order = sorted(
        normalized,
        key=lambda item: (
            item["depth"], item["owner_repository"], item["owner_commit"],
            item["path"], item["repository"], item["commit"],
        ),
    )
    if normalized != expected_order:
        raise GraphError("dependency graph edges are out of AD-4 ordinal order")
    material = {
        "schema": envelope["schema"],
        "root": envelope["root"],
        "edge_count": edge_count,
        "edges": edges,
    }
    expected_digest = canonical_digest(material)
    if envelope["graph_digest"] != expected_digest:
        raise GraphError(
            f"dependency graph digest mismatch: expected {expected_digest}, found {envelope['graph_digest']!r}"
        )
    return envelope


def policy_projection(policy: dict[str, Any], commit: str, raw_bytes: bytes) -> dict[str, Any]:
    """Return the closed AD-14 coordinate for an exact active-policy blob."""
    return {
        "schema": POLICY_SCHEMA,
        "repository": "github.com/hexalith/hexalith.frontcomposer",
        "path": POLICY_PATH,
        "commit": require_commit(commit, "dependency policy commit"),
        "sha256": hashlib.sha256(raw_bytes).hexdigest(),
    }


def load_policy_at_commit(root_dir: Path, commit: str) -> tuple[dict[str, Any], bytes, dict[str, Any]]:
    """Load the active policy from an immutable FrontComposer commit."""
    commit = require_commit(commit, "dependency policy commit")
    raw = read_blob(root_dir, commit, POLICY_PATH, 4 * 1024 * 1024, "dependency policy")
    if raw is None:
        raise GraphError(f"dependency policy is absent at {commit}:{POLICY_PATH}")
    policy = load_policy_bytes(raw, f"{commit}:{POLICY_PATH}", allow_legacy_registry=True)
    return policy, raw, policy_projection(policy, commit, raw)


def _logical_edge_map(graph: dict[str, Any]) -> dict[tuple[str, str, str], dict[str, Any]]:
    return {
        (edge["owner_repository"], edge["path"], edge["repository"]): edge
        for edge in graph["edges"]
    }


def _edge_change_projection(edge: dict[str, Any]) -> dict[str, Any]:
    """Project one edge for dependency-meaning equality without losing evidence."""
    if edge["depth"] != 1:
        return edge
    return {name: value for name, value in edge.items() if name != "owner_commit"}


def _candidate_module_commits(graph: dict[str, Any]) -> dict[str, str]:
    commits = {graph["root"]["repository"]: graph["root"]["commit"]}
    for edge in graph["edges"]:
        if edge["depth"] != 1:
            continue
        existing = commits.get(edge["repository"])
        if existing is not None and existing != edge["commit"]:
            raise GraphError(
                f"candidate graph selects multiple commits for module {edge['repository']}: "
                f"{existing} and {edge['commit']}"
            )
        commits[edge["repository"]] = edge["commit"]
    return commits


def _module_proof(
    repository: str,
    commit: str,
    reasons: list[str],
    policy: dict[str, Any],
) -> dict[str, Any]:
    registry = policy["module_build_registry"]
    if repository not in registry:
        raise GraphError(f"affected module {repository!r} has no active-policy disposition")
    row = registry[repository]
    proof: dict[str, Any] = {
        "repository": repository,
        "commit": require_commit(commit, f"affected module {repository} commit"),
        "disposition": row["disposition"],
        "solution": row["solution"],
        "builds_contract_source": row["builds_contract_source"],
        "restore_argv": row["restore_argv"],
        "build_argv": row["build_argv"],
        "reasons": sorted(set(reasons)),
    }
    return proof


def diff_graphs(
    base_graph: dict[str, Any],
    candidate_graph: dict[str, Any],
    policy: dict[str, Any],
    *,
    event: str,
    event_base: str,
    candidate: str,
    merge_base: str | None,
    release_eligible: bool,
    full_affected: bool = False,
) -> dict[str, Any]:
    """Return deterministic AD-8 edge changes and root-subsumed module proof."""
    validate_graph_envelope(base_graph)
    validate_graph_envelope(candidate_graph)
    if event not in ("pull_request", "push"):
        raise GraphError(f"unsupported dependency graph event {event!r}")
    require_commit(event_base, "event base")
    require_commit(candidate, "candidate")
    if merge_base is not None:
        require_commit(merge_base, "merge base")

    before_map = _logical_edge_map(base_graph)
    after_map = _logical_edge_map(candidate_graph)
    changes: list[dict[str, Any]] = []
    for key in sorted(set(before_map) | set(after_map)):
        before = before_map.get(key)
        after = after_map.get(key)
        if (
            before is not None
            and after is not None
            and _edge_change_projection(before) == _edge_change_projection(after)
        ):
            continue
        status = "added" if before is None else "removed" if after is None else "changed"
        changes.append({
            "status": status,
            "key": {"owner_repository": key[0], "path": key[1], "repository": key[2]},
            "before": before,
            "after": after,
            "subsumed_by": None,
        })

    root_repository = candidate_graph["root"]["repository"]
    module_commits = _candidate_module_commits(candidate_graph)
    affected_reasons: dict[str, list[str]] = {}

    def affect(repository: str, reason: str) -> None:
        affected_reasons.setdefault(repository, []).append(reason)

    depth1 = [
        change for change in changes
        if (change["after"] or change["before"])["depth"] == 1
    ]
    subsumed_owners: dict[str, str] = {}
    for change in depth1:
        status = change["status"]
        selected = change["after"] or change["before"]
        target = root_repository if status == "removed" else selected["repository"]
        reason = f"depth-1 {status}: {selected['path']} -> {selected['repository']}"
        affect(target, reason)
        before_repository = change["before"]["repository"] if change["before"] else None
        after_repository = change["after"]["repository"] if change["after"] else None
        for owner in (before_repository, after_repository):
            if owner:
                subsumed_owners[owner] = target

    for change in changes:
        selected = change["after"] or change["before"]
        if selected["depth"] != 2:
            continue
        owner = selected["owner_repository"]
        if owner in subsumed_owners:
            change["subsumed_by"] = subsumed_owners[owner]
            continue
        target = owner if owner in module_commits else root_repository
        affect(target, f"depth-2 {change['status']}: {selected['path']} -> {selected['repository']}")

    if full_affected:
        for repository in sorted(module_commits):
            affect(repository, "fail-closed full-affected diagnostic")

    affected_modules = [
        _module_proof(repository, module_commits.get(repository, candidate), reasons, policy)
        for repository, reasons in sorted(affected_reasons.items())
    ]
    evidence: dict[str, Any] = {
        "schema": DIFF_SCHEMA,
        "revisions": {
            "event": event,
            "event_base": event_base,
            "candidate": candidate,
            "merge_base": merge_base,
            "release_eligible": release_eligible,
        },
        "base_graph": base_graph,
        "candidate_graph": candidate_graph,
        "changes": changes,
        "affected_modules": affected_modules,
    }
    evidence["evidence_digest"] = canonical_digest(evidence)
    return evidence


def materialize_contract_tree(
    builds_local: Path,
    commit: str,
    destination: Path,
    policy: dict[str, Any],
) -> dict[str, Any]:
    """Extract the bounded regular-file Builds contract tree from one exact commit."""
    commit = require_commit(commit, "Builds contract-tree commit")
    limits = policy["resource_limits"]
    raw = _git_ok(["ls-tree", "-r", "-z", "--full-tree", commit], builds_local)
    if len(raw) > limits["max_ls_tree_bytes_per_owner_commit"]:
        raise GraphError("Builds contract-tree ls-tree output exceeds the active-policy ceiling")
    entries: list[tuple[str, str, int]] = []
    total = 0
    for record in raw.split(b"\x00"):
        if not record:
            continue
        meta, separator, path_bytes = record.partition(b"\t")
        if not separator:
            raise GraphError("malformed Builds contract-tree ls-tree record")
        try:
            mode_bytes, obj_type, sha_bytes = meta.split()
            path = path_bytes.decode("ascii")
        except (ValueError, UnicodeDecodeError) as exc:
            raise GraphError("non-ASCII or malformed Builds contract-tree entry") from exc
        normalize_path(path, "Builds contract-tree path")
        if mode_bytes == b"160000" and obj_type == b"commit":
            continue
        if obj_type != b"blob" or mode_bytes not in (b"100644", b"100755"):
            raise GraphError(f"Builds contract tree contains unsupported mode/type at {path!r}")
        size = _blob_size(builds_local, sha_bytes.decode("ascii"))
        if size > limits["max_contract_tree_blob_bytes"]:
            raise GraphError(f"Builds contract-tree blob {path!r} exceeds the per-blob ceiling")
        total += size
        if total > limits["max_contract_tree_total_bytes"]:
            raise GraphError("Builds contract tree exceeds the total-byte ceiling")
        entries.append((path, sha_bytes.decode("ascii"), int(mode_bytes, 8)))
        if len(entries) > limits["max_contract_tree_files"]:
            raise GraphError("Builds contract tree exceeds the file-count ceiling")

    destination.mkdir(parents=True, exist_ok=False)
    for path, sha, mode in entries:
        target = destination.joinpath(*path.split("/"))
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(_git_ok(["cat-file", "blob", sha], builds_local))
        target.chmod(0o755 if mode == 0o100755 else 0o644)
    return {"file_count": len(entries), "total_bytes": total, "commit": commit}


def materialize_repository_tree(local_path: Path, commit: str, destination: Path) -> None:
    """Clone and detach one exact repository commit without initializing gitlinks."""
    commit = require_commit(commit, "module materialization commit")
    if destination.exists():
        raise GraphError(f"module materialization destination already exists: {destination}")
    clone = subprocess.run(
        ["git", "clone", "--quiet", "--no-checkout", "--no-hardlinks", str(local_path), str(destination)],
        capture_output=True,
        check=False,
    )
    if clone.returncode != 0:
        raise GraphError(f"failed to clone isolated module tree: {clone.stderr.decode('utf-8', 'replace').strip()}")
    checkout = _run_git(["checkout", "--quiet", "--detach", commit], destination)
    if checkout.returncode != 0:
        raise GraphError(
            f"failed to checkout exact module commit {commit}: {checkout.stderr.decode('utf-8', 'replace').strip()}"
        )


def _canonical_clone_url(identity: str) -> str:
    match = re.fullmatch(r"github\.com/([a-z0-9._-]+)/([a-z0-9._-]+)", identity)
    if match is None:
        raise GraphError(f"cannot acquire non-canonical repository identity {identity!r}")
    return f"https://github.com/{match.group(1)}/{match.group(2)}.git"


def _ensure_commit_available(local_path: Path, commit: str) -> None:
    commit = require_commit(commit, "acquisition commit")
    probe = _run_git(["cat-file", "-e", f"{commit}^{{commit}}"], local_path)
    if probe.returncode == 0:
        return
    fetch, attempts = _run_git_network(["fetch", "--no-tags", "origin", commit], local_path)
    if fetch.returncode != 0:
        raise GraphError(
            f"failed to acquire exact approved commit {commit} in {local_path}{_attempts_note(attempts)}: "
            f"{fetch.stderr.decode('utf-8', 'replace').strip()}"
        )
    _git_ok(["cat-file", "-e", f"{commit}^{{commit}}"], local_path)


def acquire_catalog_objects(root_dir: Path, event_base: str, candidate: str) -> dict[str, Any]:
    """Prepare exact selected catalogs in the root Builds store, without checkouts.

    The committed event-base policy authorizes the identity. Both root mappings
    must be valid before networking; neither candidate URLs nor origin are used.
    Missing root Builds objects are prerequisites for reading their inventories.
    Collection and validation remain offline and retain missing-object failures.
    """
    event_base = require_commit(event_base, "catalog acquisition event base")
    candidate = require_commit(candidate, "catalog acquisition candidate")
    if event_base == "0" * 40:
        raise GraphError("catalog acquisition requires a nonzero active event base")
    policy, _raw, _projection = load_policy_at_commit(root_dir, event_base)
    builds_identity = policy["builds_identity"]
    builds_local = resolve_local_path(policy, builds_identity, root_dir)
    if not builds_local.is_dir() or Path(
        _git_ok(["rev-parse", "--show-toplevel"], builds_local).decode("utf-8").strip()
    ).resolve() != builds_local:
        raise GraphError("catalog acquisition requires an initialized root Builds object store")

    remote = _canonical_clone_url(builds_identity)
    def ensure_catalog_commit(commit: str) -> None:
        require_commit(commit, "catalog acquisition selector")
        if _run_git(["cat-file", "-e", f"{commit}^{{commit}}"], builds_local).returncode != 0:
            fetch, attempts = _run_git_network(
                ["-c", "submodule.recurse=false", "fetch", "--no-recurse-submodules", "--no-tags", remote, commit],
                builds_local,
            )
            if fetch.returncode != 0:
                raise GraphError(
                    f"failed to acquire exact approved catalog {commit}{_attempts_note(attempts)}: "
                    f"{fetch.stderr.decode('utf-8', 'replace').strip()}"
                )
        _git_ok(["cat-file", "-e", f"{commit}^{{commit}}"], builds_local)

    root_commits: set[str] = set()
    owner_requirements: set[tuple[str, str]] = set()
    for revision in dict.fromkeys((event_base, candidate)):
        edges = _owner_edges(root_dir, revision, "github.com/hexalith/hexalith.frontcomposer", 1, policy, root_dir)
        if len(edges) > policy["resource_limits"]["max_edges"]:
            raise GraphError("catalog acquisition exceeds the root edge ceiling")
        root_builds = [edge for edge in edges if edge["repository"] == builds_identity]
        if len(root_builds) != 1 or (root_dir / root_builds[0]["path"]).resolve() != builds_local:
            raise GraphError("catalog acquisition requires the policy-mapped root Builds gitlink")
        root_commits.add(root_builds[0]["commit"])
        for edge in edges:
            owner_requirements.add((edge["repository"], edge["commit"]))
    # Verify every approved top-level object store before the first network call.
    stores: dict[str, Path] = {}
    for identity, _commit in sorted(owner_requirements):
        local = resolve_local_path(policy, identity, root_dir)
        if not local.is_dir() or Path(_git_ok(["rev-parse", "--show-toplevel"], local).decode("utf-8").strip()).resolve() != local:
            raise GraphError(f"catalog acquisition requires an initialized root owner object store: {identity}")
        stores[identity] = local
    # Root-only initialization may lack a base-only/off-main Builds commit.
    # Fetch only these approved exact prerequisites before reading depth-two data.
    for commit in sorted(root_commits):
        ensure_catalog_commit(commit)

    # A candidate-only shallow checkout can lack the event-base owner's inventory.
    # Fetch its exact approved object without moving HEAD or initializing descendants.
    for identity, commit in sorted(owner_requirements):
        if identity == builds_identity:
            continue
        local = stores[identity]
        if _run_git(["cat-file", "-e", f"{commit}^{{commit}}"], local).returncode != 0:
            fetched, attempts = _run_git_network(
                ["-c", "submodule.recurse=false", "fetch", "--no-recurse-submodules", "--no-tags", _canonical_clone_url(identity), commit], local)
            if fetched.returncode != 0:
                raise GraphError(f"failed to acquire exact approved owner {identity}@{commit}{_attempts_note(attempts)}: "
                                 f"{fetched.stderr.decode('utf-8', 'replace').strip()}")
        _git_ok(["cat-file", "-e", f"{commit}^{{commit}}"], local)

    commits: set[str] = set()
    for revision in dict.fromkeys((event_base, candidate)):
        edges = _collect_edges(root_dir, "github.com/hexalith/hexalith.frontcomposer", revision, policy)
        commits.update(edge["commit"] for edge in edges if edge["repository"] == builds_identity)

    for commit in sorted(commits):
        ensure_catalog_commit(commit)
        if read_blob(builds_local, commit, "Props/Directory.Packages.props",
                     policy["resource_limits"]["max_catalog_blob_bytes"], f"{builds_identity}@{commit}") is None:
            raise GraphError(f"{builds_identity}@{commit}: missing Props/Directory.Packages.props")
    return {"event_base": event_base, "candidate": candidate, "repository": builds_identity,
            "catalog_commits": sorted(commits)}


def acquire_object_stores(
    root_dir: Path,
    destination: Path,
    event_base: str,
    candidate: str,
) -> dict[str, Any]:
    """Acquire the exact bounded graph into isolated stores from policy-approved remotes.

    Candidate `.gitmodules` URLs are validated but never used as clone/fetch endpoints.
    Every remote URL is reconstructed from the active policy's canonical identity.
    """
    zero_base = event_base == "0" * 40
    if not zero_base:
        event_base = require_commit(event_base, "acquisition event base")
    candidate = require_commit(candidate, "acquisition candidate")
    if destination.exists():
        raise GraphError(f"acquisition destination already exists: {destination}")
    policy, _raw, _projection = load_policy_at_commit(root_dir, candidate if zero_base else event_base)
    if zero_base and policy.get("consumer_source_projections"):
        raise GraphError("consumer source projection requires an active base; candidate diagnostics cannot authorize acquisition")

    # Validate both root mappings before any network operation.
    root_identity = "github.com/hexalith/hexalith.frontcomposer"
    base_root_edges = [] if zero_base else _owner_edges(root_dir, event_base, root_identity, 1, policy, root_dir)
    candidate_root_edges = _owner_edges(root_dir, candidate, root_identity, 1, policy, root_dir)

    clone_root = subprocess.run(
        ["git", "clone", "--quiet", "--no-checkout", "--no-hardlinks", str(root_dir), str(destination)],
        capture_output=True,
        check=False,
    )
    if clone_root.returncode != 0:
        raise GraphError(f"failed to create isolated root object store: {clone_root.stderr.decode('utf-8', 'replace').strip()}")
    if not zero_base:
        _ensure_commit_available(destination, event_base)
    _ensure_commit_available(destination, candidate)

    for trusted in policy["trusted_identities"]:
        identity = trusted["identity"]
        if identity == root_identity:
            continue
        local = destination / trusted["local_path"]
        local.parent.mkdir(parents=True, exist_ok=True)
        clone, attempts = _run_git_network(
            ["clone", "--quiet", "--no-checkout", _canonical_clone_url(identity), str(local)],
            reset_before_retry=local,
        )
        if clone.returncode != 0:
            raise GraphError(
                f"failed to acquire approved repository {identity}{_attempts_note(attempts)}: "
                f"{clone.stderr.decode('utf-8', 'replace').strip()}"
            )

    root_edges = base_root_edges + candidate_root_edges
    for edge in root_edges:
        local = resolve_local_path(policy, edge["repository"], destination)
        _ensure_commit_available(local, edge["commit"])

    direct_edges: list[dict[str, Any]] = []
    for edge in root_edges:
        owner_local = resolve_local_path(policy, edge["repository"], destination)
        direct_edges.extend(
            _owner_edges(owner_local, edge["commit"], edge["repository"], 2, policy, destination)
        )
    if len(root_edges) + len(direct_edges) > policy["resource_limits"]["max_edges"] * 2:
        raise GraphError("base/candidate acquisition exceeds the doubled AD-7 graph-edge ceiling")
    for edge in direct_edges:
        if _inventory_only_edge(edge, policy):
            continue
        local = resolve_local_path(policy, edge["repository"], destination)
        _ensure_commit_available(local, edge["commit"])

    if policy.get("consumer_source_projections"):
        for revision in ([event_base] if not zero_base else []) + [candidate]:
            collect_graph(destination, root_identity, revision, policy)

    candidate_builds = [edge for edge in candidate_root_edges if edge["repository"] == policy["builds_identity"]]
    if len(candidate_builds) != 1:
        raise GraphError("candidate root must select exactly one Builds commit for checkout-format governance")
    builds_local = resolve_local_path(policy, policy["builds_identity"], destination)
    checkout = _run_git(["checkout", "--quiet", "--detach", candidate_builds[0]["commit"]], builds_local)
    if checkout.returncode != 0:
        raise GraphError(
            "failed to materialize the isolated candidate Builds checkout for the root-only format guard: "
            f"{checkout.stderr.decode('utf-8', 'replace').strip()}"
        )

    return {
        "schema": "hexalith.dependency-object-acquisition.v1",
        "event_base": event_base,
        "candidate": candidate,
        "stores": [entry["identity"] for entry in policy["trusted_identities"]],
        "root_edges": len(root_edges),
        "direct_edges": len(direct_edges),
    }


def validate_diff_evidence(root_dir: Path, evidence: Any) -> tuple[dict[str, Any], dict[str, Any]]:
    """Validate a live AD-8 evidence document against its immutable active policy."""
    if not isinstance(evidence, dict) or evidence.get("schema") != DIFF_SCHEMA:
        raise GraphError(f"affected-module evidence schema must be {DIFF_SCHEMA!r}")
    supplied_digest = evidence.get("evidence_digest")
    expected_digest = canonical_digest({
        name: value for name, value in evidence.items() if name != "evidence_digest"
    })
    if supplied_digest != expected_digest:
        raise GraphError(f"affected-module evidence digest mismatch: expected {expected_digest}")
    revisions = _require_closed_object(
        evidence.get("revisions"),
        {"event", "event_base", "candidate", "merge_base", "release_eligible"},
        "affected-module evidence revisions",
    )
    candidate = require_commit(revisions["candidate"], "affected-module candidate")
    policy_coordinate = _require_closed_object(
        evidence.get("dependency_policy"), {"schema", "repository", "path", "commit", "sha256"},
        "affected-module dependency_policy",
    )
    policy_commit = require_commit(policy_coordinate["commit"], "affected-module policy commit")
    policy, _raw, expected_coordinate = load_policy_at_commit(root_dir, policy_commit)
    if policy_coordinate != expected_coordinate:
        raise GraphError("affected-module evidence policy coordinate/hash drift")
    root_identities = [
        entry["identity"] for entry in policy["trusted_identities"]
        if entry["local_path"] == "."
    ]
    if len(root_identities) != 1:
        raise GraphError("active policy must declare exactly one trusted root identity at local path '.'")
    trusted_root_identity = root_identities[0]
    max_edges = policy["resource_limits"]["max_edges"]
    base_graph = validate_graph_envelope(evidence.get("base_graph"))
    candidate_graph = validate_graph_envelope(evidence.get("candidate_graph"))
    expected_base_commit = candidate if revisions["event_base"] == "0" * 40 else revisions["event_base"]
    for label, graph, expected_commit in (
        ("base", base_graph, expected_base_commit),
        ("candidate", candidate_graph, candidate),
    ):
        if graph["root"]["repository"] != trusted_root_identity:
            raise GraphError(
                f"affected-module {label} graph root repository does not match active-policy trusted root identity"
            )
        if graph["root"]["commit"] != expected_commit:
            raise GraphError(
                f"affected-module {label} graph root commit does not match its sealed revision"
            )
        if graph["edge_count"] > max_edges:
            raise GraphError(
                f"affected-module {label} graph has {graph['edge_count']} edges, "
                f"exceeds the active-policy {max_edges}-edge ceiling"
            )
    if policy_commit != revisions["event_base"] and revisions["event_base"] != "0" * 40:
        raise GraphError("affected-module active policy commit does not match event base")
    if policy.get("consumer_source_projections"):
        for label, graph in (("base", base_graph), ("candidate", candidate_graph)):
            actual_projection = evaluate_consumer_projections(root_dir, policy, graph)
            semantic_document = evidence.get(label + "_semantics")
            if not isinstance(semantic_document, dict):
                raise GraphError("affected-module consumer source projection semantics are missing")
            claimed = semantic_document.get("consumer_source_projections", {})
            if actual_projection != claimed:
                raise GraphError("affected-module consumer source projection does not match committed objects")

    expected = diff_graphs(
        evidence["base_graph"],
        evidence["candidate_graph"],
        policy,
        event=revisions["event"],
        event_base=candidate if revisions["event_base"] == "0" * 40 else revisions["event_base"],
        candidate=candidate,
        merge_base=revisions["merge_base"],
        release_eligible=revisions["release_eligible"],
        full_affected=evidence.get("diagnostic") is not None,
    )
    if expected["changes"] != evidence.get("changes") or expected["affected_modules"] != evidence.get("affected_modules"):
        raise GraphError("affected-module changes/proof do not match canonical AD-8 projection")
    return policy, evidence


def materialize_consumer_sources(
    root_dir: Path, policy: dict[str, Any], graph: dict[str, Any], destination: Path,
    repository: str, commit: str,
) -> list[str]:
    """Materialize only the two approved Compile blobs, never the umbrella tree."""
    reports = evaluate_consumer_projections(root_dir, policy, graph)
    written = []
    for identity, report in reports.items():
        if report["consumer_repository"] != repository or report["consumer_commit"] != commit:
            continue
        row = policy["consumer_source_projections"][identity]
        selected = next(edge for edge in graph["edges"] if edge["depth"] == 1 and edge["repository"] == identity)
        target = destination / selected["path"]
        if not target.resolve().is_relative_to(destination.resolve()):
            raise GraphError("consumer source materialization path escapes its isolated module")
        if target.is_symlink() or (target.exists() and (not target.is_dir() or any(target.iterdir()))):
            raise GraphError("consumer source destination is unexpectedly populated")
        target.mkdir(parents=True, exist_ok=True)
        owner_local = resolve_local_path(policy, identity, root_dir)
        for relative, digest in row["source_files"].items():
            blob = read_blob(owner_local, row["owner_commit"], relative,
                             policy["resource_limits"]["max_contract_tree_blob_bytes"], "consumer Compile source")
            if blob is None or hashlib.sha256(blob).hexdigest() != digest:
                raise GraphError("consumer source changed before materialization")
            output = target / relative
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_bytes(blob)
            written.append(str(output.relative_to(destination)))
    return sorted(written)


def run_affected_builds(root_dir: Path, evidence: dict[str, Any], output_root: Path) -> dict[str, Any]:
    """Materialize and execute only active-policy static argv for affected build targets."""
    policy, evidence = validate_diff_evidence(root_dir, evidence)
    if output_root.exists():
        raise GraphError(f"affected-module output root already exists: {output_root}")
    output_root.mkdir(parents=True)
    graph = evidence["candidate_graph"]
    builds_identity = policy["builds_identity"]
    builds_local = resolve_local_path(policy, builds_identity, root_dir)
    results: list[dict[str, Any]] = []
    for index, module in enumerate(evidence["affected_modules"]):
        repository = module["repository"]
        commit = module["commit"]
        row = policy["module_build_registry"].get(repository)
        if row is None:
            raise GraphError(f"affected module {repository} is absent from active policy")
        expected = _module_proof(repository, commit, module["reasons"], policy)
        if module != expected:
            raise GraphError(f"affected module {repository} does not match active-policy proof")
        if row["disposition"] == "evidence-only":
            results.append({"repository": repository, "commit": commit, "disposition": "evidence-only"})
            continue

        destination = output_root / f"module-{index:03d}"
        module_local = resolve_local_path(policy, repository, root_dir)
        materialize_repository_tree(module_local, commit, destination)
        source = row["builds_contract_source"]
        if source.startswith("edge-tree:"):
            contract_path = source.removeprefix("edge-tree:")
            selectors = [
                edge for edge in graph["edges"]
                if edge["owner_repository"] == repository
                and edge["owner_commit"] == commit
                and edge["path"] == contract_path
                and edge["repository"] == builds_identity
            ]
            if len(selectors) != 1:
                raise GraphError(
                    f"affected module {repository}@{commit} requires exactly one Builds edge at {contract_path}"
                )
            selector = selectors[0]
            contract_destination = destination / contract_path
            if contract_destination.exists():
                if not contract_destination.is_dir() or any(contract_destination.iterdir()):
                    raise GraphError(
                        f"affected module {repository} archive already materialized non-empty contract path {contract_path}"
                    )
                contract_destination.rmdir()
            materialize_contract_tree(builds_local, selector["commit"], contract_destination, policy)
            catalog = destination / contract_path / "Props" / "Directory.Packages.props"
            observed_hash = hashlib.sha256(catalog.read_bytes()).hexdigest()
            if observed_hash != selector["catalog_sha256"]:
                raise GraphError(f"affected module {repository} materialized catalog hash drift")
        elif source != "self":
            raise GraphError(f"affected module {repository} has unsupported contract source {source!r}")

        materialize_consumer_sources(root_dir, policy, graph, destination, repository, commit)
        command_results = []
        for name in ("restore_argv", "build_argv"):
            argv = row[name]
            proc = subprocess.run(argv, cwd=str(destination), capture_output=True, text=True, check=False)
            command_results.append({
                "name": name,
                "argv": argv,
                "exit_code": proc.returncode,
                "stdout": proc.stdout,
                "stderr": proc.stderr,
            })
            if proc.returncode != 0:
                raise GraphError(
                    f"affected module {repository}@{commit} {name} failed with exit {proc.returncode}: "
                    f"{proc.stderr.strip()}"
                )
        results.append({
            "repository": repository,
            "commit": commit,
            "disposition": "build",
            "commands": command_results,
        })
    result = {"schema": "hexalith.affected-module-build-results.v1", "modules": results}
    result["result_digest"] = canonical_digest(result)
    return result


# ---------------------------------------------------------------------------
# AD-6 — semantic catalog evaluation (MSBuild XML introspection, not evaluation).
# ---------------------------------------------------------------------------


def parse_project_xml(blob: bytes, context: str) -> ET.Element:
    try:
        return ET.fromstring(blob)
    except ET.ParseError as exc:
        raise GraphError(f"{context}: malformed XML ({exc})") from exc


def _simple_match(item_spec: str | None, package_id: str) -> bool:
    if not item_spec:
        return False
    for spec in item_spec.split(";"):
        spec = spec.strip()
        if spec and fnmatch.fnmatchcase(package_id.lower(), spec.lower()):
            return True
    return False


def find_package_version_ops(root: ET.Element, package_id: str) -> list[ET.Element]:
    ops = []
    for el in root.iter("PackageVersion"):
        if any(_simple_match(el.get(attr), package_id) for attr in ("Include", "Update", "Remove")):
            ops.append(el)
    return ops


def _parent_map(root: ET.Element) -> dict[ET.Element, ET.Element]:
    return {child: parent for parent in root.iter() for child in parent}


def _ancestors(el: ET.Element, parents: dict[ET.Element, ET.Element]) -> list[ET.Element]:
    chain = []
    cur = el
    while cur in parents:
        cur = parents[cur]
        chain.append(cur)
    return chain


def assert_selected_catalog_property(root: ET.Element, prop_name: str, expected_value: str, context: str) -> None:
    """Assert the selected shared catalog defines prop_name exactly once, with expected_value.

    The shared catalog declares every Hexalith version property in the canonical
    self-default form Condition="'$(Name)' == ''", which resolves to the literal value
    whenever the property is not already set. That exact shape is accepted. Any other
    condition, a conditional ancestor, or selection through an MSBuild Choose branch is
    rejected, because the effective value would then depend on evaluation state this
    validator cannot observe -- the same rule assert_authoritative_package_version
    applies to package rows.
    """
    matches = list(root.iter(prop_name))
    if len(matches) != 1:
        observed_values = [match.text or "" for match in matches]
        observed = "<missing>" if not observed_values else repr(observed_values)
        raise GraphError(
            f"{context}: {prop_name} expected exactly one value {expected_value!r}, "
            f"found {len(matches)} values {observed}"
        )
    element = matches[0]
    parents = _parent_map(root)
    ancestors = _ancestors(element, parents)
    if any(node.tag in ("Choose", "When", "Otherwise") for node in ancestors):
        raise GraphError(f"{context}: {prop_name} must not be selected through an MSBuild Choose branch")
    if any(node.get("Condition") is not None for node in ancestors):
        raise GraphError(f"{context}: {prop_name} must not be declared under a conditional group in the shared catalog")
    condition = element.get("Condition")
    if condition is not None:
        match = _SELF_DEFAULT_CONDITION.match(condition)
        if match is None or match.group("name") != prop_name:
            raise GraphError(
                f"{context}: {prop_name} must be unconditional or use the canonical self-default "
                f"condition \"'$({prop_name})' == ''\", found {condition!r}"
            )
    observed_text = element.text or ""
    if observed_text != expected_value:
        raise GraphError(f"{context}: {prop_name} expected {expected_value!r}, found {observed_text!r}")


def _validate_catalog_logging_import(
    root: ET.Element, default: ET.Element, parents: dict[ET.Element, ET.Element],
    catalog_source: tuple[Path, str, int] | None, context: str,
    contract: str = "HexalithEventStoreVersion SDK4.2",
) -> None:
    """Prove the sole optional analyzer import from the catalog's exact Builds commit."""
    imports = [element for element in root.iter() if element.tag in {"Import", "ImportGroup", "Sdk"}]
    if root.get("Sdk") is not None:
        raise GraphError(f"{context}: {contract} proof rejects implicit SDK imports")
    if not imports:
        return
    expected_import = {
        "Project": "$(MSBuildThisFileDirectory)Logging.Build.targets",
        "Condition": "Exists('$(MSBuildThisFileDirectory)Logging.Build.targets')",
    }
    positions = {element: index for index, element in enumerate(root.iter())}
    if (len(imports) != 1 or imports[0].tag != "Import" or imports[0].attrib != expected_import
            or parents.get(imports[0]) is not root or positions[imports[0]] <= positions[default]
            or list(imports[0]) or (imports[0].text or "").strip()):
        raise GraphError(f"{context}: {contract} proof rejects unsupported, grouped or misplaced catalog imports")
    if catalog_source is None:
        raise GraphError(f"{context}: {contract} logging import requires its exact committed Builds source")
    local_path, commit, max_bytes = catalog_source
    commit = require_commit(commit, f"{context}: logging import source commit")
    path = "Props/Logging.Build.targets"
    entry = _tree_entry(local_path, commit, path)
    if entry is None:
        return  # The exact Exists condition is false in this committed contract tree.
    mode, object_type, _object_id = entry
    if mode not in {"100644", "100755"} or object_type != "blob":
        raise GraphError(f"{context}: {path} at {commit} must be a regular committed blob, found {mode} {object_type}")
    blob = read_blob(local_path, commit, path, max_bytes, context)
    if blob is None:
        raise GraphError(f"{context}: {path} at {commit} disappeared during committed import inspection")
    target = parse_project_xml(blob, f"{context}: {path}@{commit}")

    expected = ("Project", {}, (("Target", {
        "Name": "HexalithRemoveReplacedLoggingSourceGenerator",
        "AfterTargets": "ResolveReferences",
        "Condition": "'$(DisableMicrosoftExtensionsLoggingSourceGenerator)' == 'true'",
    }, (("ItemGroup", {}, (
        ("_HexalithTelemetryLoggingGenerator", {
            "Include": "@(Analyzer)", "Condition": "'%(Analyzer.Filename)' == 'Microsoft.Gen.Logging'",
        }, ()),
        ("Analyzer", {
            "Remove": "@(Analyzer)",
            "Condition": "'@(_HexalithTelemetryLoggingGenerator)' != '' and '%(Analyzer.Filename)' == 'Microsoft.Extensions.Logging.Generators'",
        }, ()),
    )),)),))

    def require_shape(element: ET.Element, expected_node: tuple) -> None:
        tag, attributes, children = expected_node
        if (element.tag != tag or element.attrib != attributes or len(element) != len(children)
                or (element.text or "").strip() or (element.tail or "").strip()):
            raise GraphError(f"{context}: {path} must contain only the closed analyzer-only XML shape")
        for child, expected_child in zip(element, children, strict=True):
            require_shape(child, expected_child)

    require_shape(target, expected)


# Exact inactive SDK4.2 subjects in the qualified current catalog; no prefix admission.
_SDK42_CURRENT_PROJECTS = {
    "HexalithPartiesSdk42CatalogView": frozenset({
        "Hexalith.Parties",
        "Hexalith.Parties.AdminPortal",
        "Hexalith.Parties.AdminPortal.Tests",
        "Hexalith.Parties.AppHost",
        "Hexalith.Parties.Aspire",
        "Hexalith.Parties.Aspire.Tests",
        "Hexalith.Parties.Authentication",
        "Hexalith.Parties.Authentication.Tests",
        "Hexalith.Parties.Catalog.Tests",
        "Hexalith.Parties.Ci.Tests",
        "Hexalith.Parties.Client",
        "Hexalith.Parties.Client.Tests",
        "Hexalith.Parties.ConsumerPortal",
        "Hexalith.Parties.ConsumerPortal.Tests",
        "Hexalith.Parties.Contracts",
        "Hexalith.Parties.Contracts.Tests",
        "Hexalith.Parties.EventStoreAdminUI.TestHost",
        "Hexalith.Parties.EventStoreExtensions",
        "Hexalith.Parties.EventStoreGateway.TestHost",
        "Hexalith.Parties.EventStoreSample.Contracts.TestArtifact",
        "Hexalith.Parties.EventStoreSample.TestHost",
        "Hexalith.Parties.IntegrationTests",
        "Hexalith.Parties.Mcp",
        "Hexalith.Parties.Mcp.Tests",
        "Hexalith.Parties.Picker",
        "Hexalith.Parties.Picker.Tests",
        "Hexalith.Parties.Platform.AttachedLifecycleTests",
        "Hexalith.Parties.Platform.IntegrationTests",
        "Hexalith.Parties.Platform.LifecycleTests",
        "Hexalith.Parties.Projections",
        "Hexalith.Parties.Projections.Tests",
        "Hexalith.Parties.Sample",
        "Hexalith.Parties.Sample.Tests",
        "Hexalith.Parties.Security",
        "Hexalith.Parties.Security.Tests",
        "Hexalith.Parties.Server.Tests",
        "Hexalith.Parties.Testing",
        "Hexalith.Parties.Tests",
        "Hexalith.Parties.UI",
        "Hexalith.Parties.UI.Tests",
    }),
    "HexalithPlatformSdk42CatalogView": frozenset({
        "Hexalith.Platform.CatalogBuilder",
        "Hexalith.Platform.Composition",
        "Hexalith.Platform.Composition.Tests",
        "Hexalith.Platform.Custody",
        "Hexalith.Platform.Custody.Component",
        "Hexalith.Platform.Custody.Component.Tests",
        "Hexalith.Platform.Custody.Tests",
        "Hexalith.Platform.EventStoreHost",
        "Hexalith.Platform.Identity",
        "Hexalith.Platform.Identity.Tests",
        "Hexalith.Platform.OwnerServices.Tests",
        "Hexalith.Platform.Protection",
        "Hexalith.Platform.Protection.Qualification.Enrollment",
        "Hexalith.Platform.Protection.Qualification.Executor",
        "Hexalith.Platform.Protection.Qualification.Observer",
        "Hexalith.Platform.Protection.Tests",
        "Hexalith.Platform.Secrets.ArchiveProcessFixture",
        "Hexalith.Platform.Secrets.Authority",
        "Hexalith.Platform.Secrets.Authority.Tests",
        "Hexalith.Platform.Secrets.Custody",
        "Hexalith.Platform.Secrets.Custody.Tests",
        "Hexalith.Platform.Secrets.Inventory",
        "Hexalith.Platform.Secrets.Inventory.Tests",
        "Hexalith.Platform.Secrets.Qualification.Enrollment",
        "Hexalith.Platform.Secrets.Qualification.Executor",
        "Hexalith.Platform.Secrets.Qualification.Observer",
        "Hexalith.Platform.Secrets.Runtime",
        "Hexalith.Platform.Secrets.Runtime.Tests",
        "Hexalith.Platform.TenantRegistry.Host",
        "Hexalith.Platform.TestExtension",
        "Hexalith.Platform.TestExtension.Qualification.Tests",
        "Hexalith.Platform.Works.Runtime",
        "Hexalith.Platform.Works.Tests",
    }),
    "HexalithBuildsPackagedHostSdk42CatalogView": frozenset({
        "Hexalith.Builds.Module.AppHost",
        "Hexalith.Builds.Module.EventStoreHost",
        "Hexalith.Builds.Module.UiHost",
    }),
}
_SDK42_CURRENT_GATES = " or ".join(f"'$({name})' == 'true'" for name in _SDK42_CURRENT_PROJECTS)
_SDK42_CURRENT_CONDITION = "'$(HexalithEventStoreVersion)' == '' and (" + _SDK42_CURRENT_GATES + ")"
_MEMORIES327_CURRENT_CONDITION = (
    "'$(HexalithMemoriesVersion)' == '' and '$(HexalithEventStoreVersion)' == '4.2.0' and ("
    + _SDK42_CURRENT_GATES + ")"
)


_TENANTS8_CURRENT_CONDITION = (
    "'$(HexalithTenantsVersion)' == '' and ('$(HexalithEventStoreVersion)' != '4.2.0' or ("
    + " and ".join(f"'$({name})' != 'true'" for name in _SDK42_CURRENT_PROJECTS) + "))"
)
_TENANTS707_CURRENT_CONDITION = (
    "'$(HexalithTenantsVersion)' == '' and '$(HexalithEventStoreVersion)' == '4.2.0' and ("
    + _SDK42_CURRENT_GATES + ")"
)


def _current_sdk42_qualification(
    root: ET.Element, parents: dict[ET.Element, ET.Element], context: str,
    catalog_source: tuple[Path, str, int] | None,
) -> dict[str, ET.Element]:
    """Prove the closed current SDK4.2/Memories3.0.27 cohort is inactive for FrontComposer."""
    contract = "current SDK4.2/Memories3.0.27 qualification"
    if root.tag != "Project" or root.attrib:
        raise GraphError(f"{context}: {contract} requires an ordinary unconditional Project root")
    if any(element.tag.casefold() in {"target", "usingtask", "output"} for element in root.iter()):
        raise GraphError(f"{context}: {contract} rejects executable catalog targets, tasks or outputs")
    positions = {element: index for index, element in enumerate(root.iter())}

    def require_global(element: ET.Element, name: str) -> None:
        group = parents.get(element)
        if (group is None or group.tag != "PropertyGroup" or group.attrib or parents.get(group) is not root):
            raise GraphError(f"{context}: {contract} {name} must be in an unconditional top-level PropertyGroup")

    overrides: dict[str, ET.Element] = {}
    defaults: dict[str, ET.Element] = {}
    for name, condition, retained, current in (
        ("HexalithEventStoreVersion", _SDK42_CURRENT_CONDITION, "4.2.0", "5.1.0"),
        ("HexalithMemoriesVersion", _MEMORIES327_CURRENT_CONDITION, "3.0.27", "4.0.0"),
    ):
        definitions = [element for element in root.iter() if element.tag.casefold() == name.casefold()]
        if len(definitions) != 2 or any(element.tag != name for element in definitions):
            raise GraphError(f"{context}: {contract} {name} requires exactly two canonical declarations")
        selected = [element for element in definitions if element.attrib == {"Condition": condition}
                    and element.text == retained and not list(element)]
        if len(selected) != 1:
            raise GraphError(f"{context}: {contract} {name} retained row must have its exact literal condition and version")
        override = selected[0]
        default = next(element for element in definitions if element is not override)
        for element in definitions:
            require_global(element, name)
        if (default.attrib != {"Condition": f"'$({name})' == ''"}
                or default.text != current or list(default)):
            raise GraphError(f"{context}: {contract} {name} must retain canonical default {current}")
        overrides[name] = override
        defaults[name] = default

    clause = re.compile(r"\$\(MSBuildProjectName\.Equals\('(?P<project>[A-Za-z0-9._-]+)'\)\)")
    for name, allowed_projects in _SDK42_CURRENT_PROJECTS.items():
        definitions = [element for element in root.iter() if element.tag.casefold() == name.casefold()]
        if len(definitions) != 2 or any(element.tag != name for element in definitions):
            raise GraphError(f"{context}: {contract} selector {name} requires exactly two canonical declarations")
        default, enabled = definitions
        for element in definitions:
            require_global(element, name)
        if default.attrib or default.text != "false" or list(default):
            raise GraphError(f"{context}: {contract} selector {name} requires an unconditional literal false default")
        if set(enabled.attrib) != {"Condition"} or enabled.text != "true" or list(enabled):
            raise GraphError(f"{context}: {contract} selector {name} requires one conditional literal true assignment")
        clauses = re.split(r"\s+or\s+", enabled.attrib["Condition"].strip())
        parsed = [clause.fullmatch(value) for value in clauses]
        projects = [match.group("project") for match in parsed if match is not None]
        if len(projects) != len(clauses) or len(projects) != len(set(projects)) or set(projects) != allowed_projects:
            raise GraphError(f"{context}: {contract} selector {name} must contain exactly its qualified literal project-name Equals subjects")
        if not positions[default] < positions[enabled] < positions[overrides["HexalithEventStoreVersion"]]:
            raise GraphError(f"{context}: {contract} selector {name} must default false then select subjects before the override")
    if not (positions[overrides["HexalithEventStoreVersion"]] < positions[defaults["HexalithEventStoreVersion"]]
            < positions[overrides["HexalithMemoriesVersion"]] < positions[defaults["HexalithMemoriesVersion"]]):
        raise GraphError(f"{context}: {contract} SDK selection must precede the retained Memories row and its default")
    _validate_catalog_logging_import(root, defaults["HexalithMemoriesVersion"], parents, catalog_source, context, contract)
    return overrides



def _current_tenants8_qualification(
    root: ET.Element, parents: dict[ET.Element, ET.Element], context: str,
    catalog_source: tuple[Path, str, int] | None,
) -> None:
    """Prove the exact complementary current8/retained7.0.7 rows under the same SDK4.2 scope."""
    _current_sdk42_qualification(root, parents, context, catalog_source)
    contract = "HexalithTenantsVersion SDK4.2/current8 qualification"
    definitions = [element for element in root.iter() if element.tag.casefold() == "hexalithtenantsversion"]
    if len(definitions) != 2 or any(element.tag != "HexalithTenantsVersion" for element in definitions):
        raise GraphError(f"{context}: {contract} requires exactly two canonical declarations")
    current, retained = definitions
    for element, condition, version in (
        (current, _TENANTS8_CURRENT_CONDITION, "8.0.0"),
        (retained, _TENANTS707_CURRENT_CONDITION, "7.0.7"),
    ):
        group = parents.get(element)
        if (group is None or group.tag != "PropertyGroup" or group.attrib or parents.get(group) is not root
                or element.attrib != {"Condition": condition} or element.text != version or list(element)):
            raise GraphError(f"{context}: {contract} requires exact global complementary conditions and versions 8.0.0/7.0.7")
    positions = {element: index for index, element in enumerate(root.iter())}
    memories_default = list(root.iter("HexalithMemoriesVersion"))[1]  # Already proved by the shared qualification.
    if not positions[memories_default] < positions[current] < positions[retained]:
        raise GraphError(f"{context}: {contract} current then retained rows must follow SDK/Memories selection")
    _validate_catalog_logging_import(root, retained, parents, catalog_source, context, contract)


def _inactive_eventstore_sdk42_override(
    root: ET.Element, matches: list[ET.Element], parents: dict[ET.Element, ET.Element], context: str,
    catalog_source: tuple[Path, str, int] | None,
) -> ET.Element | None:
    """Recognize the SDK4.2 row outside FrontComposer's catalog-default scope.

    Invocation-level global properties retain MSBuild precedence. This static
    default proof does not authorize them; governed build argv is separately
    bound by the active policy.
    """
    if any(element.tag.casefold() == "hexalitheventstoreversion" and element.tag != "HexalithEventStoreVersion"
           for element in root.iter()):
        raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 proof rejects case-insensitive version aliases")
    if any(element.get("Condition") == _SDK42_CURRENT_CONDITION for element in matches):
        return _current_sdk42_qualification(root, parents, context, catalog_source)["HexalithEventStoreVersion"]
    condition = (
        "'$(HexalithEventStoreVersion)' == '' and ("
        "'$(HexalithPartiesSdk42CatalogView)' == 'true' or "
        "'$(HexalithBuildsPackagedHostSdk42CatalogView)' == 'true')"
    )
    overrides = [element for element in matches
                 if element.attrib == {"Condition": condition} and element.text == "4.2.0" and not list(element)]
    if len(overrides) != 1:
        return None
    override = overrides[0]
    positions = {element: index for index, element in enumerate(root.iter())}

    def require_global(element: ET.Element, name: str) -> None:
        group = parents.get(element)
        if (root.get("Condition") is not None or group is None or group.tag != "PropertyGroup"
                or group.get("Condition") is not None or parents.get(group) is not root):
            raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 {name} must be in an unconditional top-level PropertyGroup")

    require_global(override, "override")
    selectors = {
        "HexalithPartiesSdk42CatalogView": None,
        "HexalithBuildsPackagedHostSdk42CatalogView": {
            "Hexalith.Builds.Module.AppHost", "Hexalith.Builds.Module.EventStoreHost", "Hexalith.Builds.Module.UiHost",
        },
    }
    clause = re.compile(r"\$\(MSBuildProjectName\.Equals\('(?P<project>[A-Za-z0-9._-]+)'\)\)")
    for name, allowed_projects in selectors.items():
        definitions = [element for element in root.iter() if element.tag.casefold() == name.casefold()]
        if len(definitions) != 2 or any(element.tag != name for element in definitions):
            raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 selector {name} requires exactly two canonical declarations")
        default, enabled = definitions
        for element in definitions:
            require_global(element, name)
        if default.attrib or default.text != "false" or list(default):
            raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 selector {name} requires an unconditional literal false default")
        if set(enabled.attrib) != {"Condition"} or enabled.text != "true" or list(enabled):
            raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 selector {name} requires one conditional literal true assignment")
        clauses = re.split(r"\s+or\s+", enabled.attrib["Condition"].strip())
        parsed = [clause.fullmatch(value) for value in clauses]
        projects = [match.group("project") for match in parsed if match is not None]
        if (len(projects) != len(clauses) or not projects or len(projects) != len(set(projects))
                or (allowed_projects is not None and set(projects) != allowed_projects)
                or (allowed_projects is None and any(re.fullmatch(r"Hexalith\.Parties(?:\.[A-Za-z0-9_-]+)*", project) is None for project in projects))):
            raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 selector {name} must contain only its literal project-name Equals clauses")
        if not positions[default] < positions[enabled] < positions[override]:
            raise GraphError(f"{context}: HexalithEventStoreVersion SDK4.2 selector {name} must default false then select its projects before the override")
    defaults = [element for element in matches if element is not override]
    if len(defaults) == 1:
        if positions[defaults[0]] < positions[override]:
            raise GraphError(f"{context}: HexalithEventStoreVersion canonical default must follow the SDK4.2 override")
        _validate_catalog_logging_import(root, defaults[0], parents, catalog_source, context)
    return override


def assert_selected_catalog_property_shape(
    root: ET.Element,
    prop_name: str,
    context: str,
    *,
    allow_eventstore_qualification: bool = False,
    catalog_source: tuple[Path, str, int] | None = None,
) -> None:
    """Validate the unique literal default, excluding only a known inactive qualification override.

    The FrontComposer profile may exclude the exact Story 5.12 package-only
    Tenants override and the retained EventStore SDK4.2 row after proving its
    catalog-default selector scope excludes FrontComposer. Invocation-level
    global overrides remain separate MSBuild inputs, governed by policy-bound
    build argv. Other profiles, properties, conditions and shapes retain the
    strict unique-property rule.
    """
    matches = list(root.iter(prop_name))
    parents = _parent_map(root)
    if allow_eventstore_qualification and prop_name == "HexalithTenantsVersion":
        if any(element.get("Condition") in {_TENANTS8_CURRENT_CONDITION, _TENANTS707_CURRENT_CONDITION} for element in matches):
            _current_tenants8_qualification(root, parents, context, catalog_source)
            return
        qualification_condition = (
            "'$(CatalogCandidateTenantsVersion)' != '' and '$(UseHexalithProjectReferences)' != 'true' and ("
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.CatalogConstruction' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.Server.Tests' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.QueryRouting.Tests' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.Server.LiveSidecar.Tests' or "
            "'$(MSBuildProjectName)' == 'routing-catalog.cs')"
        )
        qualification_overrides = [
            element for element in matches
            if element.attrib == {"Condition": qualification_condition}
            and element.text == "$(CatalogCandidateTenantsVersion)"
            and not list(element)
            and (group := parents.get(element)) is not None
            and group.tag == "PropertyGroup"
            and parents.get(group) is root
            and group.get("Condition") is None
            and root.get("Condition") is None
        ]
        if len(qualification_overrides) == 1:
            matches.remove(qualification_overrides[0])
    if allow_eventstore_qualification and prop_name == "HexalithEventStoreVersion":
        override = _inactive_eventstore_sdk42_override(root, matches, parents, context, catalog_source)
        if override is not None:
            matches.remove(override)
    if allow_eventstore_qualification and prop_name == "HexalithMemoriesVersion":
        if (any(element.get("Condition") == _SDK42_CURRENT_CONDITION for element in root.iter("HexalithEventStoreVersion"))
                or any(element.get("Condition") == _MEMORIES327_CURRENT_CONDITION for element in matches)):
            matches.remove(_current_sdk42_qualification(root, parents, context, catalog_source)[prop_name])
    if len(matches) != 1:
        observed_values = [match.text or "" for match in matches]
        observed = "<missing>" if not observed_values else repr(observed_values)
        raise GraphError(
            f"{context}: {prop_name} must define exactly one version property, "
            f"found {len(matches)} values {observed}"
        )

    element = matches[0]
    ancestors = _ancestors(element, parents)
    if any(node.tag in ("Choose", "When", "Otherwise") for node in ancestors):
        raise GraphError(f"{context}: {prop_name} must not be selected through an MSBuild Choose branch")
    if any(node.get("Condition") is not None for node in ancestors):
        raise GraphError(f"{context}: {prop_name} must not be declared under a conditional group in the shared catalog")
    property_group = parents.get(element)
    if property_group is None or property_group.tag != "PropertyGroup" or parents.get(property_group) is not root:
        raise GraphError(
            f"{context}: {prop_name} must be a global property declared directly in a top-level PropertyGroup"
        )
    condition = element.get("Condition")
    if condition is not None:
        match = _SELF_DEFAULT_CONDITION.match(condition)
        if match is None or match.group("name") != prop_name:
            raise GraphError(
                f"{context}: {prop_name} must be unconditional or use the canonical self-default "
                f"condition \"'$({prop_name})' == ''\", found {condition!r}"
            )

    observed_text = element.text or ""
    if list(element) or _NUGET_VERSION.fullmatch(observed_text) is None:
        raise GraphError(
            f"{context}: {prop_name} must contain a literal NuGet version, found {observed_text!r}"
        )


def assert_authoritative_package_presence(root: ET.Element, package_id: str, context: str) -> ET.Element:
    """Assert the selected catalog has exactly one authoritative Include row with a literal NuGet version."""
    ops = find_package_version_ops(root, package_id)
    if len(ops) != 1:
        raise GraphError(f"{context}: {package_id} must have exactly one unmasked shared operation (found {len(ops)})")
    el = ops[0]
    include = el.get("Include")
    if not include or include.strip().lower() != package_id.lower():
        raise GraphError(f"{context}: {package_id} must be an authoritative Include item, not Update")
    if el.get("Update") is not None:
        raise GraphError(f"{context}: {package_id} must not use Update in the shared catalog")
    if el.get("Exclude") is not None:
        raise GraphError(f"{context}: {package_id} must not use Exclude in the shared catalog")
    parents = _parent_map(root)
    ancestors = _ancestors(el, parents)
    if el.get("Condition") is not None or any(node.get("Condition") is not None for node in ancestors):
        raise GraphError(f"{context}: {package_id} must be unconditional in the shared catalog")
    if any(node.tag in ("Choose", "When", "Otherwise") for node in ancestors):
        raise GraphError(f"{context}: {package_id} must not be selected through an MSBuild Choose branch")
    version = el.get("Version")
    if not version or _NUGET_VERSION.fullmatch(version) is None:
        raise GraphError(
            f"{context}: {package_id} must declare a nonempty literal NuGet version, found {version!r}"
        )
    return el


_FRONTCOMPOSER_IMMUTABLE_PROJECTS = frozenset({
    "Counter.Domain",
    "Counter.Specimens",
    "Counter.Specimens.Domain",
    "Counter.Web",
    "Hexalith.FrontComposer.AppHost",
    "Hexalith.FrontComposer.Cli",
    "Hexalith.FrontComposer.Cli.Tests",
    "Hexalith.FrontComposer.Contracts",
    "Hexalith.FrontComposer.Contracts.Tests",
    "Hexalith.FrontComposer.Contracts.UI",
    "Hexalith.FrontComposer.Contracts.UI.Tests",
    "Hexalith.FrontComposer.CounterFixture",
    "Hexalith.FrontComposer.Mcp",
    "Hexalith.FrontComposer.Mcp.Tests",
    "Hexalith.FrontComposer.Schema",
    "Hexalith.FrontComposer.Shell",
    "Hexalith.FrontComposer.Shell.Tests",
    "Hexalith.FrontComposer.Shell.Tests.Bench",
    "Hexalith.FrontComposer.SourceTools",
    "Hexalith.FrontComposer.SourceTools.Tests",
    "Hexalith.FrontComposer.Testing",
    "Hexalith.FrontComposer.Testing.Tests",
    "Hexalith.FrontComposer.UI",
    "Hexalith.FrontComposer.UI.Tests",
    "IdeParityCounter",
})


def assert_frontcomposer_immutable_version(
    root: ET.Element, expected_version: str, context: str,
    catalog_source: tuple[Path, str, int] | None,
) -> None:
    """Prove the named net10 consumer view without evaluating candidate MSBuild."""
    selector = "HexalithFrontComposerImmutable10CatalogView"
    version_property = "HexalithImmutableVersion"
    package = "System.Collections.Immutable"
    nodes = list(root.iter())
    if not any(element.tag.casefold() in {selector.casefold(), version_property.casefold()} for element in nodes):
        assert_authoritative_package_version(root, package, expected_version, context)
        return
    parents = _parent_map(root)
    positions = {element: index for index, element in enumerate(nodes)}

    def reject(reason: str) -> None:
        raise GraphError(f"{context}: {package} FrontComposer compatibility view {reason}")

    if any(element.tag.casefold() in {"target", "usingtask", "output"} for element in nodes):
        reject("cannot prove package authority with executable targets, tasks or outputs")

    def declarations(name: str) -> list[ET.Element]:
        found = [element for element in nodes if element.tag.casefold() == name.casefold()]
        if len(found) != 2 or any(element.tag != name for element in found):
            reject(f"requires exactly two canonical {name} declarations")
        for element in found:
            group = parents.get(element)
            if (group is None or group.tag != "PropertyGroup" or parents.get(group) is not root
                    or group.get("Condition") is not None or root.get("Condition") is not None or list(element)):
                reject(f"requires global unconditional placement and scalar {name} values")
        return found

    disabled, enabled = declarations(selector)
    if disabled.attrib or disabled.text != "false" or set(enabled.attrib) != {"Condition"} or enabled.text != "true":
        reject("requires a literal false default and conditional true assignment")
    clauses = re.split(r"\s+or\s+", enabled.attrib["Condition"].strip())
    pattern = re.compile(r"\$\(MSBuildProjectName\.Equals\('(?P<project>[A-Za-z0-9._-]+)'\)\)")
    parsed = [pattern.fullmatch(value) for value in clauses]
    projects = [match.group("project") for match in parsed if match is not None]
    if len(projects) != len(clauses) or len(projects) != len(set(projects)) or set(projects) != _FRONTCOMPOSER_IMMUTABLE_PROJECTS:
        reject("requires the exact admitted literal project-name Equals scope")
    default, selected = declarations(version_property)
    if default.attrib or _NUGET_VERSION.fullmatch(default.text or "") is None:
        reject("requires an unconditional literal shared default")
    if selected.attrib != {"Condition": f"'$({selector})' == 'true'"} or selected.text != expected_version:
        reject(f"must select policy version {expected_version!r}, found {selected.text!r}")
    package_nodes = [element for element in nodes if element.tag.casefold() == "packageversion"]
    identity_fields = {"include", "update", "remove"}
    if any(key.casefold() in identity_fields and any(token in value for token in ("$(", "@(", "%"))
           for element in package_nodes for key, value in element.attrib.items()):
        reject("cannot prove package identity from dynamic or escaped shared operations")
    operations = [element for element in package_nodes if any(
        key.casefold() in identity_fields and _simple_match(value, package) for key, value in element.attrib.items()
    )]
    if len(operations) != 1:
        reject("requires one unmasked package operation")
    row = operations[0]
    group = parents.get(row)
    if (row.tag != "PackageVersion" or row.attrib != {"Include": package, "Version": f"$({version_property})"} or list(row)
            or group is None or group.tag != "ItemGroup" or parents.get(group) is not root or group.get("Condition") is not None):
        reject("requires one unconditional authoritative package row bound to its version property")
    if not positions[disabled] < positions[enabled] < positions[default] < positions[selected] < positions[row]:
        reject("requires selector, default, selected value and package declaration order")
    _validate_catalog_logging_import(root, selected, parents, catalog_source, context, package)


def assert_authoritative_package_version(root: ET.Element, package_id: str, expected_version: str, context: str) -> None:
    el = assert_authoritative_package_presence(root, package_id, context)
    version = el.get("Version")
    if version != expected_version:
        raise GraphError(f"{context}: {package_id} expected version {expected_version!r}, found {version!r}")


_UTF8_BOM = b"\xef\xbb\xbf"


def assert_utf8_bom_and_crlf(data: bytes, context: str) -> None:
    if not data.startswith(_UTF8_BOM):
        raise GraphError(f"{context}: must start with a UTF-8 BOM")
    body = data[len(_UTF8_BOM):]
    try:
        body.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise GraphError(f"{context}: invalid UTF-8 after BOM ({exc})") from exc
    for i, byte in enumerate(body):
        if byte == 0x0A and (i == 0 or body[i - 1] != 0x0D):
            raise GraphError(f"{context}: bare LF at byte offset {i + len(_UTF8_BOM)}")
        if byte == 0x0D and (i + 1 >= len(body) or body[i + 1] != 0x0A):
            raise GraphError(f"{context}: bare CR at byte offset {i + len(_UTF8_BOM)}")


def _check_attr(local_path: Path, relative_path: str, attribute: str) -> str:
    out = _git_ok(["check-attr", attribute, "--", relative_path], local_path).decode("utf-8")
    prefix = f"{relative_path}: {attribute}: "
    if not out.startswith(prefix):
        raise GraphError(f"git check-attr produced unexpected output for {relative_path}: {out!r}")
    return out[len(prefix):].strip()


def assert_builds_checkout_format_policy(builds_local: Path, context: str) -> None:
    """Root-only BOM/CRLF + gitattributes format policy on the local Builds checkout.

    Deliberately reads the checked-out working tree rather than a committed-object blob.
    `eol=crlf` only rewrites bytes on checkout, so the raw commit object for a catalog can
    legitimately carry bare LF (a known, separately tracked upstream Hexalith.Builds
    formatting issue) while every local checkout still renders CRLF. Property and package
    asserts on the same Builds-selector edge use `read_blob(..., edge["commit"], ...)` for
    provenance; this check answers a different question — “is this machine’s Builds
    checkout safe to consume under the catalog-only format policy?” — and must not be
    read as blob provenance. Edge context strings that name `Builds@<commit>` therefore
    describe which selector triggered the checkout check, not which blob was byte-scanned.
    Dev Notes call this out as remaining "a local format policy unless separately
    generalized"; it stays scoped to the local checkout, not graph provenance.
    """
    catalog_relative = "Props/Directory.Packages.props"
    if _check_attr(builds_local, catalog_relative, "text") != "set":
        raise GraphError(f"{context}: {catalog_relative} must declare text=set")
    if _check_attr(builds_local, catalog_relative, "eol") != "crlf":
        raise GraphError(f"{context}: {catalog_relative} must declare eol=crlf")
    if _check_attr(builds_local, "Directory.Build.props", "eol") != "unspecified":
        raise GraphError(f"{context}: Directory.Build.props eol must remain unspecified (the catalog-only checkout policy must not broaden to unrelated Builds files)")
    catalog_path = builds_local / "Props" / "Directory.Packages.props"
    if not catalog_path.is_file():
        raise GraphError(f"{context}: missing checked-out {catalog_relative}")
    assert_utf8_bom_and_crlf(catalog_path.read_bytes(), context)


def assert_override_not_enabled(root: ET.Element, context: str) -> None:
    for el in root.iter("CentralPackageVersionOverrideEnabled"):
        if (el.text or "").strip().lower() == "true":
            raise GraphError(f"{context}: CentralPackageVersionOverrideEnabled must not be enabled")


def assert_no_minver(root: ET.Element, context: str) -> None:
    for el in root.iter("PackageReference"):
        if _simple_match(el.get("Include"), "MinVer"):
            raise GraphError(f"{context}: release versioning is owned by semantic-release, not MinVer")
    for el in root.iter():
        if el.tag.startswith("MinVer"):
            raise GraphError(f"{context}: must not retain MinVer configuration after semantic-release ownership")


def assert_guarded_imports(root: ET.Element, spec: dict[str, Any], context: str) -> None:
    imports = list(root.iter("Import"))
    expected_projects = spec["import_projects"]
    expected_conditions = spec["import_conditions"]
    if len(imports) != len(expected_projects):
        raise GraphError(f"{context}: must preserve exactly {len(expected_projects)} guarded shared-catalog import paths (found {len(imports)})")
    for idx, el in enumerate(imports):
        if el.get("Project") != expected_projects[idx]:
            raise GraphError(f"{context}: import[{idx}] Project mismatch (found {el.get('Project')!r})")
        if el.get("Condition") != expected_conditions[idx]:
            raise GraphError(f"{context}: import[{idx}] Condition mismatch (found {el.get('Condition')!r})")
    for prop_name, expected_value in spec["required_properties"].items():
        matches = list(root.iter(prop_name))
        if len(matches) != 1:
            raise GraphError(f"{context}: expected exactly one {prop_name} property (found {len(matches)})")
        if (matches[0].text or "") != expected_value:
            raise GraphError(f"{context}: {prop_name} expected {expected_value!r}, found {matches[0].text!r}")


def list_tracked_files(local_path: Path, commit: str, extensions: list[str], max_ls_tree_bytes: int) -> list[str]:
    out = _git_ok(["ls-tree", "-r", "-z", "--full-tree", commit], local_path)
    if len(out) > max_ls_tree_bytes:
        raise GraphError(f"ls-tree output for {commit} in {local_path} exceeds the {max_ls_tree_bytes}-byte ceiling")
    files = []
    for record in out.split(b"\x00"):
        if not record:
            continue
        meta, _, path_bytes = record.partition(b"\t")
        _mode, obj_type, _sha = meta.split()
        if obj_type != b"blob":
            continue
        path = path_bytes.decode("utf-8")
        if any(path.endswith(ext) for ext in extensions):
            files.append(path)
    return sorted(files)


def assert_no_inline_versions(local_path: Path, commit: str, extensions: list[str], limits: dict[str, int], context: str) -> None:
    for rel_path in list_tracked_files(local_path, commit, extensions, limits["max_ls_tree_bytes_per_owner_commit"]):
        blob = read_blob(local_path, commit, rel_path, limits["max_catalog_blob_bytes"], f"{context}:{rel_path}")
        if blob is None:
            continue
        root = parse_project_xml(blob, f"{context}:{rel_path}")
        parents = _parent_map(root)
        for el in root.iter("PackageVersion"):
            parent = parents.get(el)
            if parent is not None and parent.tag == "ItemGroup":
                raise GraphError(f"{context}:{rel_path} must inherit every package version from the pinned Builds catalog")
        for el in root.iter():
            if el.tag in ("PackageReference", "GlobalPackageReference"):
                if el.get("Version") is not None:
                    raise GraphError(f"{context}:{rel_path} must not declare an inline Version")
                if el.get("VersionOverride") is not None:
                    raise GraphError(f"{context}:{rel_path} must not declare VersionOverride")
                for child in el:
                    if child.tag in ("Version", "VersionOverride"):
                        raise GraphError(f"{context}:{rel_path} must not declare inline package-version metadata")


def evaluate_semantics(root_dir: Path, policy: dict[str, Any], envelope: dict[str, Any]) -> dict[str, Any]:
    """Evaluate every Builds-selector edge under its owner's explicit semantic profile (AD-6)."""
    limits = policy["resource_limits"]
    profiles = policy["profiles"]
    semantic_profiles = policy["semantic_profiles"]
    builds_identity = policy["builds_identity"]

    by_owner: dict[str, list[dict[str, Any]]] = {}
    for edge in envelope["edges"]:
        if edge["repository"] == builds_identity:
            by_owner.setdefault(edge["owner_repository"], []).append(edge)

    catalog_cache: dict[str, tuple[ET.Element, bytes]] = {}

    def load_catalog(commit: str) -> tuple[ET.Element, bytes]:
        if commit not in catalog_cache:
            builds_local = resolve_local_path(policy, builds_identity, root_dir)
            blob = read_blob(
                builds_local, commit, "Props/Directory.Packages.props", limits["max_catalog_blob_bytes"], f"{builds_identity}@{commit}"
            )
            if blob is None:
                raise GraphError(f"{builds_identity}@{commit}: missing Props/Directory.Packages.props")
            catalog_cache[commit] = (parse_project_xml(blob, f"{builds_identity}@{commit}"), blob)
        return catalog_cache[commit]

    diagnostics: list[str] = []
    for owner_identity, owner_edges in sorted(by_owner.items()):
        profile_name = semantic_profiles.get(owner_identity)
        if profile_name is None:
            raise GraphError(f"{owner_identity}: no semantic profile mapping in policy (fails closed)")
        profile = profiles.get(profile_name)
        if profile is None:
            raise GraphError(f"{owner_identity}: unknown semantic profile {profile_name!r}")

        owner_local = resolve_local_path(policy, owner_identity, root_dir)
        owner_checks = profile.get("owner_checks", {})
        if not isinstance(owner_checks, dict):
            raise GraphError(f"{owner_identity}: owner_checks must be an object")

        required_property_names = profile.get("selected_catalog_required_property_names", [])
        required_props = profile.get("selected_catalog_required_properties", {})
        required_package_names = profile.get("selected_catalog_required_package_names", [])
        required_packages = profile.get("selected_catalog_required_packages", {})

        # Each distinct owner_commit pin of this identity must run owner_checks against
        # its own tree. Using only owner_edges[0] left later pins' shim/override/minver/
        # inline state unchecked.
        for owner_commit in sorted({edge["owner_commit"] for edge in owner_edges}):
            commit_edges = [edge for edge in owner_edges if edge["owner_commit"] == owner_commit]

            for relative_path, expected_hash in owner_checks.get("canonical_source_files", {}).items():
                source = read_blob(owner_local, owner_commit, relative_path, limits["max_catalog_blob_bytes"], f"{owner_identity}@{owner_commit}")
                if source is None or hashlib.sha256(source).hexdigest() != expected_hash:
                    raise GraphError(f"{owner_identity}@{owner_commit}: canonical source {relative_path!r} is missing or differs from its enrolled digest")

            own_blob = read_blob(owner_local, owner_commit, "Directory.Packages.props", limits["max_catalog_blob_bytes"], f"{owner_identity}@{owner_commit}")
            own_xml = parse_project_xml(own_blob, f"{owner_identity}@{owner_commit} Directory.Packages.props") if own_blob is not None else None

            if owner_checks.get("no_package_version_rows"):
                if own_xml is None:
                    raise GraphError(f"{owner_identity}@{owner_commit}: missing Directory.Packages.props")
                if list(own_xml.iter("PackageVersion")):
                    raise GraphError(f"{owner_identity}@{owner_commit}: root Directory.Packages.props must be an import shim owning no PackageVersion rows")

            if owner_checks.get("well_formed_project_root"):
                if own_xml is None:
                    raise GraphError(f"{owner_identity}@{owner_commit}: missing Directory.Packages.props")
                if own_xml.tag != "Project":
                    raise GraphError(f"{owner_identity}@{owner_commit}: Directory.Packages.props must have a Project root")

            if owner_checks.get("override_not_enabled") and own_xml is not None:
                assert_override_not_enabled(own_xml, f"{owner_identity}@{owner_commit}")

            if owner_checks.get("no_minver"):
                build_props_blob = read_blob(owner_local, owner_commit, "Directory.Build.props", limits["max_catalog_blob_bytes"], f"{owner_identity}@{owner_commit}")
                if build_props_blob is None:
                    raise GraphError(
                        f"{owner_identity}@{owner_commit}: missing Directory.Build.props while no_minver is required"
                    )
                build_props_xml = parse_project_xml(build_props_blob, f"{owner_identity}@{owner_commit} Directory.Build.props")
                assert_no_minver(build_props_xml, f"{owner_identity}@{owner_commit} Directory.Build.props")

            guarded = owner_checks.get("guarded_imports")
            if guarded:
                if own_xml is None:
                    raise GraphError(f"{owner_identity}@{owner_commit}: missing Directory.Packages.props")
                assert_guarded_imports(own_xml, guarded, f"{owner_identity}@{owner_commit} Directory.Packages.props")

            inline = owner_checks.get("no_inline_versions_in_tracked_files")
            if inline:
                assert_no_inline_versions(owner_local, owner_commit, inline["extensions"], limits, f"{owner_identity}@{owner_commit}")

            for edge in commit_edges:
                catalog_xml, catalog_blob = load_catalog(edge["commit"])
                edge_context = f"{owner_identity}@{owner_commit} -> {edge['path']} -> {builds_identity}@{edge['commit']}"

                if owner_checks.get("bom_crlf_on_selected_catalog"):
                    builds_local = resolve_local_path(policy, builds_identity, root_dir)
                    assert_builds_checkout_format_policy(
                        builds_local,
                        f"{edge_context} [Builds checkout format; not blob provenance]",
                    )

                for prop_name in required_property_names:
                    assert_selected_catalog_property_shape(
                        catalog_xml,
                        prop_name,
                        edge_context,
                        allow_eventstore_qualification=profile_name == "frontcomposer-catalog-v1",
                        catalog_source=(resolve_local_path(policy, builds_identity, root_dir), edge["commit"], limits["max_catalog_blob_bytes"]),
                    )

                for prop_name, expected_value in required_props.items():
                    assert_selected_catalog_property(catalog_xml, prop_name, expected_value, edge_context)

                for package_id in required_package_names:
                    assert_authoritative_package_presence(catalog_xml, package_id, edge_context)
                    if owner_checks.get("no_local_override_for_selected_catalog_packages") and own_xml is not None:
                        if find_package_version_ops(own_xml, package_id):
                            raise GraphError(
                                f"{edge_context}: must inherit {package_id} from the shared catalog without local override"
                            )

                for package_id, expected_version in required_packages.items():
                    if profile_name == "frontcomposer-catalog-v1" and package_id == "System.Collections.Immutable":
                        assert_frontcomposer_immutable_version(
                            catalog_xml, expected_version, edge_context,
                            (resolve_local_path(policy, builds_identity, root_dir), edge["commit"], limits["max_catalog_blob_bytes"]),
                        )
                    else:
                        assert_authoritative_package_version(catalog_xml, package_id, expected_version, edge_context)
                    if owner_checks.get("no_local_override_for_selected_catalog_packages") and own_xml is not None:
                        if find_package_version_ops(own_xml, package_id):
                            raise GraphError(
                                f"{owner_identity}@{owner_commit}: must inherit {package_id} {expected_version} from the shared catalog without local override"
                            )

                diagnostics.append(f"validated {edge_context} under profile {profile_name}")

    selectors_validated = sum(len(v) for v in by_owner.values())
    if selectors_validated == 0:
        raise GraphError(
            "validate found no Builds-selector edges; semantic profiles were not exercised"
        )
    result = {"selectors_validated": selectors_validated, "diagnostics": diagnostics}
    projections = evaluate_consumer_projections(root_dir, policy, envelope)
    if projections:
        result["consumer_source_projections"] = projections
    return result


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def _reject_duplicate_members(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for name, value in pairs:
        if name in result:
            raise GraphError(f"duplicate JSON member {name!r}")
        result[name] = value
    return result


def load_json_bytes(raw: bytes, context: str) -> Any:
    try:
        return json.loads(raw.decode("utf-8"), object_pairs_hook=_reject_duplicate_members)
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise GraphError(f"{context}: malformed UTF-8 JSON ({exc})") from exc


def load_policy_bytes(raw: bytes, context: str, *, allow_legacy_registry: bool = False) -> dict[str, Any]:
    policy = load_json_bytes(raw, context)
    if not isinstance(policy, dict):
        raise GraphError(f"{context}: policy must be a JSON object")
    if policy.get("schema") != POLICY_SCHEMA:
        raise GraphError(f"policy schema mismatch: expected {POLICY_SCHEMA!r}, found {policy.get('schema')!r}")
    if allow_legacy_registry and _upgrade_legacy_module_registry(policy):
        policy["__legacy_registry_migration__"] = True
    assert_policy_well_formed(policy, allow_migration_marker=allow_legacy_registry)
    return policy


def load_policy(path: Path) -> dict[str, Any]:
    with open(path, "rb") as handle:
        raw = handle.read()
    return load_policy_bytes(raw, str(path))


def _require_closed_object(value: Any, members: set[str], context: str) -> dict[str, Any]:
    if not isinstance(value, dict) or set(value) != members:
        found = sorted(value) if isinstance(value, dict) else type(value).__name__
        raise GraphError(f"{context} members must be exactly {sorted(members)}, found {found}")
    return value


def _require_sha256(value: Any, context: str) -> str:
    if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
        raise GraphError(f"{context} must be lowercase 64-hex")
    return value


def _upgrade_legacy_module_registry(policy: dict[str, Any]) -> bool:
    """Upgrade the already-landed Task-2 seed only for delayed-policy activation.

    The migration is deliberately non-executable: diff rejects any affected module while
    this marker is present. It exists so the policy-only correction can prove an unchanged
    graph under the immutable prior policy; the strict candidate policy activates only on
    the following change, as AD-12 requires.
    """
    registry = policy.get("module_build_registry")
    if not isinstance(registry, dict) or not registry:
        return False
    legacy = False
    for row in registry.values():
        if not isinstance(row, dict):
            return False
        if set(row) == {"disposition", "solution", "builds_contract_source"}:
            legacy = True
            if row.get("disposition") == "build" and isinstance(row.get("solution"), str):
                solution = row["solution"]
                row["restore_argv"] = [
                    "dotnet", "restore", solution, "-p:Configuration=Release", "-p:UseNuGetDeps=true",
                ]
                row["build_argv"] = [
                    "dotnet", "build", solution, "--configuration", "Release", "--no-restore",
                    "-p:UseNuGetDeps=true",
                ]
            elif row.get("disposition") == "evidence-only":
                row["restore_argv"] = None
                row["build_argv"] = None
            else:
                return False
        elif set(row) != {"disposition", "solution", "builds_contract_source", "restore_argv", "build_argv"}:
            return False
    limits = policy.get("resource_limits")
    if not isinstance(limits, dict):
        return False
    workflow_defaults = {
        "max_workflow_closure_depth": 16,
        "max_workflow_closure_sources": 256,
        "max_workflow_source_blob_bytes": 1_048_576,
        "max_workflow_source_total_bytes": 16_777_216,
    }
    missing_limits = set(workflow_defaults) - set(limits)
    if missing_limits:
        if set(limits) & set(workflow_defaults):
            return False
        limits.update(workflow_defaults)
        legacy = True
    return legacy


def assert_policy_well_formed(policy: dict[str, Any], *, allow_migration_marker: bool = False) -> None:
    expected = {
        "schema", "builds_identity", "trusted_identities", "semantic_profiles", "profiles",
        "module_build_registry", "resource_limits", "evaluator_authorizations",
    }
    if allow_migration_marker and policy.get("__legacy_registry_migration__") is True:
        expected.add("__legacy_registry_migration__")
    if "consumer_source_projections" in policy:
        expected.add("consumer_source_projections")
    _require_closed_object(policy, expected, "dependency policy")
    assert_profiles_well_formed(policy)

    trusted = policy["trusted_identities"]
    if not isinstance(trusted, list) or not trusted:
        raise GraphError("policy trusted_identities must be a non-empty array")
    identities: set[str] = set()
    paths: set[str] = set()
    for index, entry_value in enumerate(trusted):
        entry = _require_closed_object(entry_value, {"identity", "local_path"}, f"trusted identity[{index}]")
        identity = entry["identity"]
        local_path = entry["local_path"]
        if not isinstance(identity, str) or not identity.isascii() or not identity.startswith("github.com/"):
            raise GraphError(f"trusted identity[{index}] identity is invalid")
        if identity in identities:
            raise GraphError(f"duplicate trusted identity {identity!r}")
        if not isinstance(local_path, str) or (local_path != "." and normalize_path(local_path, "trusted local_path") != local_path):
            raise GraphError(f"trusted identity[{index}] local_path is invalid")
        if local_path in paths:
            raise GraphError(f"duplicate trusted local_path {local_path!r}")
        identities.add(identity)
        paths.add(local_path)
    if policy["builds_identity"] not in identities:
        raise GraphError("policy builds_identity must name one trusted identity")

    semantic_profiles = policy["semantic_profiles"]
    if not isinstance(semantic_profiles, dict) or set(semantic_profiles) != identities:
        raise GraphError("policy semantic_profiles must cover every trusted identity exactly")
    for identity, profile_name in semantic_profiles.items():
        if profile_name not in policy["profiles"]:
            raise GraphError(f"semantic profile {profile_name!r} for {identity} is undefined")

    projections = policy.get("consumer_source_projections", {})
    if not isinstance(projections, dict):
        raise GraphError("consumer_source_projections must be an object")
    projection_members = {"owner_commit", "gitmodules_sha256", "inventory_sha256", "inventory_only_identities",
                          "source_files", "consumer_repository", "consumer_project", "source_root_property",
                          "compile_links", "builds_commit", "builds_project", "root_selectors_sha256", "consumer_configuration_sha256"}
    for identity, value in projections.items():
        if identity not in identities or identity == policy["builds_identity"]:
            raise GraphError("consumer projection owner must be an existing trusted non-Builds identity")
        row = _require_closed_object(value, projection_members, f"consumer projection {identity}")
        require_commit(row["owner_commit"], "consumer projection owner")
        require_commit(row["builds_commit"], "consumer projection root Builds")
        for field in ("gitmodules_sha256", "inventory_sha256", "root_selectors_sha256", "consumer_configuration_sha256"):
            _require_sha256(row[field], f"consumer projection {field}")
        if row["consumer_repository"] not in identities:
            raise GraphError("consumer projection consumer must already be trusted")
        for field in ("consumer_project", "builds_project"):
            normalize_path(row[field], f"consumer projection {field}")
        if not isinstance(row["source_root_property"], str) or re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", row["source_root_property"]) is None:
            raise GraphError("consumer projection source root property is invalid")
        files = row["source_files"]
        links = row["compile_links"]
        if not isinstance(files, dict) or len(files) != 2 or not isinstance(links, dict) or set(links) != set(files):
            raise GraphError("consumer projection must seal exactly two sources and their Compile links")
        for path, digest in files.items():
            normalize_path(path, "consumer canonical source")
            if not path.endswith(".cs"):
                raise GraphError("consumer canonical source must be C# Compile data")
            _require_sha256(digest, "consumer canonical source hash")
            normalize_path(links[path], "consumer Compile link")
        inventory_only = row["inventory_only_identities"]
        if (not isinstance(inventory_only, list) or not inventory_only
                or not all(isinstance(item, str) for item in inventory_only)
                or inventory_only != sorted(set(inventory_only))):
            raise GraphError("consumer inventory-only identities must be unique and sorted")
        for untrusted in inventory_only:
            if not isinstance(untrusted, str) or normalize_identity("https://" + untrusted, "inventory-only identity") != untrusted or untrusted in identities:
                raise GraphError("inventory-only identity must remain globally untrusted")

    registry = policy["module_build_registry"]
    if not isinstance(registry, dict) or set(registry) != identities:
        raise GraphError("policy module_build_registry must cover every trusted identity exactly")
    build_members = {
        "disposition", "solution", "builds_contract_source", "restore_argv", "build_argv"
    }
    for identity, row_value in registry.items():
        row = _require_closed_object(row_value, build_members, f"module registry[{identity}]")
        disposition = row["disposition"]
        if identity in projections and disposition != "evidence-only":
            raise GraphError("consumer projection owner must remain evidence-only; whole-owner execution is forbidden")
        if disposition not in ("build", "evidence-only"):
            raise GraphError(f"module registry[{identity}] has invalid disposition {disposition!r}")
        if disposition == "evidence-only":
            if any(row[name] is not None for name in ("solution", "restore_argv", "build_argv")):
                raise GraphError(f"evidence-only module registry[{identity}] must not declare executable argv")
            if row["builds_contract_source"] != "none":
                raise GraphError(f"evidence-only module registry[{identity}] must use builds_contract_source 'none'")
            continue
        solution = row["solution"]
        if not isinstance(solution, str) or normalize_path(solution, f"module registry[{identity}] solution") != solution:
            raise GraphError(f"module registry[{identity}] solution is invalid")
        source = row["builds_contract_source"]
        if source != "self" and not (isinstance(source, str) and source.startswith("edge-tree:") and normalize_path(
            source.removeprefix("edge-tree:"), f"module registry[{identity}] contract source"
        )):
            raise GraphError(f"module registry[{identity}] builds_contract_source is invalid")
        expected_restore = [
            "dotnet", "restore", solution, "-p:Configuration=Release", "-p:UseNuGetDeps=true"
        ]
        expected_build = [
            "dotnet", "build", solution, "--configuration", "Release", "--no-restore",
            "-p:UseNuGetDeps=true",
        ]
        if row["restore_argv"] != expected_restore or row["build_argv"] != expected_build:
            raise GraphError(f"module registry[{identity}] argv must equal the closed standalone Release/NuGet commands")

    required_limits = {
        "max_edges", "max_ls_tree_bytes_per_owner_commit", "max_gitmodules_blob_bytes",
        "max_catalog_blob_bytes", "max_contract_tree_files", "max_contract_tree_blob_bytes",
        "max_contract_tree_total_bytes", "max_workflow_closure_depth", "max_workflow_closure_sources",
        "max_workflow_source_blob_bytes", "max_workflow_source_total_bytes",
    }
    limits = _require_closed_object(policy["resource_limits"], required_limits, "policy resource_limits")
    if any(isinstance(value, bool) or not isinstance(value, int) or value < 1 for value in limits.values()):
        raise GraphError("every policy resource limit must be a positive JSON integer")

    authorizations = _require_closed_object(
        policy["evaluator_authorizations"], {"ci", "release", "post_release"},
        "policy evaluator_authorizations",
    )
    for stage in ("ci", "release", "post_release"):
        rows = authorizations[stage]
        if not isinstance(rows, list):
            raise GraphError(f"policy evaluator_authorizations.{stage} must be an array")
        previous: tuple[str, ...] | None = None
        for index, value in enumerate(rows):
            row = _require_closed_object(
                value, {"stage", "caller", "reusable", "actions", "closure_digest"},
                f"evaluator authorization {stage}[{index}]",
            )
            if row["stage"] != stage:
                raise GraphError(f"evaluator authorization {stage}[{index}] stage mismatch")
            caller = _require_closed_object(
                row["caller"], {"repository", "workflow_path", "blob_sha256"},
                f"evaluator authorization {stage}[{index}].caller",
            )
            _validate_source(caller, workflow=True, commit_required=False)
            reusable = _require_closed_object(
                row["reusable"], {"repository", "workflow_path", "commit", "blob_sha256"},
                f"evaluator authorization {stage}[{index}].reusable",
            )
            _validate_source(reusable, workflow=True, commit_required=True)
            actions = row["actions"]
            if not isinstance(actions, list):
                raise GraphError(f"evaluator authorization {stage}[{index}].actions must be an array")
            sorted_actions = sorted(
                actions,
                key=lambda item: (item["repository"], item["path"], item["commit"], item["blob_sha256"]),
            ) if all(isinstance(item, dict) for item in actions) else []
            if actions != sorted_actions or len({canonical_bytes(item) for item in actions}) != len(actions):
                raise GraphError(f"evaluator authorization {stage}[{index}].actions must be sorted and unique")
            for action_index, action_value in enumerate(actions):
                action = _require_closed_object(
                    action_value, {"repository", "path", "commit", "blob_sha256"},
                    f"evaluator authorization {stage}[{index}].actions[{action_index}]",
                )
                _validate_source(action, workflow=False, commit_required=True)
            projection = {name: row[name] for name in ("stage", "caller", "reusable", "actions")}
            if row["closure_digest"] != canonical_digest(projection):
                raise GraphError(f"evaluator authorization {stage}[{index}] closure_digest mismatch")
            sort_key = _authorization_sort_key(row)
            if previous is not None and sort_key <= previous:
                raise GraphError(f"policy evaluator_authorizations.{stage} must be ordinally sorted and unique")
            previous = sort_key


def _validate_source(source: dict[str, Any], *, workflow: bool, commit_required: bool) -> None:
    repository = source["repository"]
    if not isinstance(repository, str) or re.fullmatch(
        r"github\.com/[a-z0-9._-]+/[a-z0-9._-]+", repository
    ) is None:
        raise GraphError("evaluator source repository must be a normalized ASCII identity")
    path_name = "workflow_path" if workflow else "path"
    source_path = normalize_path(source[path_name], f"evaluator source {path_name}")
    if workflow and (
        not source_path.startswith(".github/workflows/")
        or not source_path.endswith((".yml", ".yaml"))
    ):
        raise GraphError("evaluator workflow source must be a .github/workflows/*.yml or *.yaml path")
    _require_sha256(source["blob_sha256"], "evaluator source blob_sha256")
    if commit_required:
        require_commit(source["commit"], "evaluator source commit")


def _authorization_sort_key(value: dict[str, Any]) -> tuple[str, ...]:
    caller = value["caller"]
    reusable = value["reusable"]
    return (
        caller["repository"], caller["workflow_path"], caller["blob_sha256"],
        reusable["repository"], reusable["workflow_path"], reusable["commit"],
        reusable["blob_sha256"], value["closure_digest"],
    )


def assert_profiles_well_formed(policy: dict[str, Any]) -> None:
    """Reject a profile whose shape would make a required check silently vacuous."""
    profiles = policy.get("profiles")
    if not isinstance(profiles, dict):
        raise GraphError("policy profiles must be an object")
    for profile_name, profile in profiles.items():
        if not isinstance(profile, dict):
            raise GraphError(f"policy profile {profile_name!r} must be an object")
        unknown = sorted(set(profile) - _PROFILE_KEYS)
        if unknown:
            raise GraphError(
                f"policy profile {profile_name!r} has unknown keys {unknown}; "
                f"expected only {sorted(_PROFILE_KEYS)}"
            )
        owner_checks = profile.get("owner_checks")
        if owner_checks is not None:
            if not isinstance(owner_checks, dict):
                raise GraphError(f"policy profile {profile_name!r}: owner_checks must be an object")
            unknown_checks = sorted(set(owner_checks) - _OWNER_CHECK_KEYS)
            if unknown_checks:
                raise GraphError(
                    f"policy profile {profile_name!r}: owner_checks has unknown keys {unknown_checks}; "
                    f"expected only {sorted(_OWNER_CHECK_KEYS)}"
                )
            for check_name, check_value in owner_checks.items():
                if check_name in _OWNER_CHECK_BOOLEAN_KEYS and not isinstance(check_value, bool):
                    raise GraphError(
                        f"policy profile {profile_name!r}: owner_checks[{check_name!r}] must be a boolean, "
                        f"found {check_value!r}"
                    )
                if check_name in _OWNER_CHECK_OBJECT_KEYS:
                    if not isinstance(check_value, dict):
                        raise GraphError(
                            f"policy profile {profile_name!r}: owner_checks[{check_name!r}] must be an object, "
                            f"found {check_value!r}"
                        )
                    if check_name == "no_inline_versions_in_tracked_files":
                        extensions = check_value.get("extensions")
                        if not isinstance(extensions, list) or not extensions or not all(
                            isinstance(item, str) and item for item in extensions
                        ):
                            raise GraphError(
                                f"policy profile {profile_name!r}: owner_checks[{check_name!r}].extensions "
                                f"must be a non-empty list of strings, found {extensions!r}"
                            )
                    if check_name == "canonical_source_files":
                        if not check_value:
                            raise GraphError(f"policy profile {profile_name!r}: canonical_source_files must not be empty")
                        for relative_path, digest in check_value.items():
                            normalize_path(relative_path, f"policy profile {profile_name!r} canonical source path")
                            _require_sha256(digest, f"policy profile {profile_name!r} canonical source digest")
                    if check_name == "guarded_imports" and not check_value:
                        raise GraphError(
                            f"policy profile {profile_name!r}: owner_checks[{check_name!r}] must be a non-empty object"
                        )
        has_required_property_names = "selected_catalog_required_property_names" in profile
        required_property_names = profile.get("selected_catalog_required_property_names")
        if has_required_property_names:
            if not isinstance(required_property_names, list) or not required_property_names:
                raise GraphError(
                    f"policy profile {profile_name!r}: selected_catalog_required_property_names "
                    "must be a non-empty list"
                )
            if not all(
                isinstance(name, str) and re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", name)
                for name in required_property_names
            ):
                raise GraphError(
                    f"policy profile {profile_name!r}: selected_catalog_required_property_names "
                    "must contain only MSBuild property names"
                )
            if required_property_names != sorted(set(required_property_names)):
                raise GraphError(
                    f"policy profile {profile_name!r}: selected_catalog_required_property_names "
                    "must be ordinally sorted and unique"
                )

        has_required_package_names = "selected_catalog_required_package_names" in profile
        required_package_names = profile.get("selected_catalog_required_package_names")
        if has_required_package_names:
            if not isinstance(required_package_names, list) or not required_package_names:
                raise GraphError(
                    f"policy profile {profile_name!r}: selected_catalog_required_package_names "
                    "must be a non-empty list"
                )
            if not all(
                isinstance(name, str) and re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]*", name)
                for name in required_package_names
            ):
                raise GraphError(
                    f"policy profile {profile_name!r}: selected_catalog_required_package_names "
                    "must contain only NuGet package ids"
                )
            if required_package_names != sorted(set(required_package_names)):
                raise GraphError(
                    f"policy profile {profile_name!r}: selected_catalog_required_package_names "
                    "must be ordinally sorted and unique"
                )

        for key in ("selected_catalog_required_properties", "selected_catalog_required_packages"):
            if key not in profile:
                continue
            required = profile.get(key)
            if not isinstance(required, dict):
                raise GraphError(f"policy profile {profile_name!r}: {key} must be an object")
            for name, expected in required.items():
                if not isinstance(expected, str):
                    raise GraphError(
                        f"policy profile {profile_name!r}: {key}[{name!r}] must be a string, found {expected!r}"
                    )

        if has_required_property_names:
            literal_properties = profile.get("selected_catalog_required_properties", {})
            overlap = sorted(set(required_property_names) & set(literal_properties))
            if overlap:
                raise GraphError(
                    f"policy profile {profile_name!r}: required property names cannot also carry "
                    f"literal requirements {overlap}"
                )

        if has_required_package_names:
            literal_packages = profile.get("selected_catalog_required_packages", {})
            package_overlap = sorted(set(required_package_names) & set(literal_packages))
            if package_overlap:
                raise GraphError(
                    f"policy profile {profile_name!r}: required package names cannot also carry "
                    f"literal requirements {package_overlap}"
                )


def build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="FrontComposer root working directory")
    parser.add_argument(
        "--policy",
        default=None,
        help=(
            "Explicit dependency policy path for graph/validate; validate otherwise reads "
            "eng/dependency-graph-policy.json from --commit, while graph uses the worktree file"
        ),
    )
    sub = parser.add_subparsers(dest="command", required=True)

    graph_cmd = sub.add_parser("graph", help="Collect and emit the canonical v1 graph envelope")
    graph_cmd.add_argument("--commit", required=True)
    graph_cmd.add_argument("--root-identity", default="github.com/hexalith/hexalith.frontcomposer")

    validate_cmd = sub.add_parser("validate", help="Collect the graph and evaluate every selector's semantic profile")
    validate_cmd.add_argument("--commit", required=True)
    validate_cmd.add_argument("--root-identity", default="github.com/hexalith/hexalith.frontcomposer")

    diff_cmd = sub.add_parser("diff", help="Collect exact base/candidate graphs and emit AD-8 affected-module evidence")
    diff_cmd.add_argument("--event", required=True, choices=("pull_request", "push"))
    diff_cmd.add_argument("--event-base", required=True)
    diff_cmd.add_argument("--candidate", required=True)
    diff_cmd.add_argument("--root-identity", default="github.com/hexalith/hexalith.frontcomposer")

    verify_cmd = sub.add_parser("verify-graph", help="Verify one AD-5 graph envelope without consulting a checkout")
    verify_cmd.add_argument("--input", required=True)

    run_cmd = sub.add_parser("run-affected", help="Materialize affected exact commits and run active-policy static argv")
    run_cmd.add_argument("--evidence", required=True)
    run_cmd.add_argument("--output-root", required=True)

    acquire_cmd = sub.add_parser("acquire", help="Acquire exact base/candidate graph objects into isolated approved stores")
    acquire_cmd.add_argument("--event-base", required=True)
    acquire_cmd.add_argument("--candidate", required=True)
    acquire_cmd.add_argument("--destination", required=True)

    catalogs_cmd = sub.add_parser("acquire-catalogs", help="Prepare exact selected catalogs in the initialized root Builds store")
    catalogs_cmd.add_argument("--event-base", required=True)
    catalogs_cmd.add_argument("--candidate", required=True)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_arg_parser()
    args = parser.parse_args(argv)
    root_dir = Path(args.root).resolve()
    policy_path = Path(args.policy).resolve() if args.policy else root_dir / "eng" / "dependency-graph-policy.json"

    try:
        if args.command == "verify-graph":
            with open(args.input, "rb") as handle:
                envelope = load_json_bytes(handle.read(), args.input)
            validate_graph_envelope(envelope)
            print(json.dumps({"ok": True, "graph_digest": envelope["graph_digest"]}, indent=2, sort_keys=True))
            return 0

        if args.command == "run-affected":
            with open(args.evidence, "rb") as handle:
                evidence_document = load_json_bytes(handle.read(), args.evidence)
            if not isinstance(evidence_document, dict):
                raise GraphError("affected-module evidence file must contain a JSON object")
            evidence = evidence_document.get("evidence", evidence_document)
            results = run_affected_builds(root_dir, evidence, Path(args.output_root).resolve())
            print(json.dumps({"ok": True, "results": results}, indent=2, sort_keys=True))
            return 0

        if args.command == "acquire-catalogs":
            result = acquire_catalog_objects(root_dir, args.event_base, args.candidate)
            print(json.dumps({"ok": True, "acquisition": result}, indent=2, sort_keys=True))
            return 0

        if args.command == "acquire":
            result = acquire_object_stores(
                root_dir,
                Path(args.destination).resolve(),
                args.event_base,
                args.candidate,
            )
            print(json.dumps({"ok": True, "acquisition": result}, indent=2, sort_keys=True))
            return 0

        if args.command == "diff":
            candidate = require_commit(args.candidate, "--candidate")
            zero_base = args.event_base == "0" * 40
            if zero_base:
                policy, policy_raw, policy_coordinates = load_policy_at_commit(root_dir, candidate)
                candidate_graph = collect_graph(root_dir, args.root_identity, candidate, policy)
                candidate_semantics = evaluate_semantics(root_dir, policy, candidate_graph)
                evidence = diff_graphs(
                    candidate_graph,
                    candidate_graph,
                    policy,
                    event=args.event,
                    event_base=candidate,
                    candidate=candidate,
                    merge_base=None,
                    release_eligible=False,
                    full_affected=True,
                )
                evidence["revisions"]["event_base"] = "0" * 40
                evidence["dependency_policy"] = policy_coordinates
                evidence["base_semantics"] = None
                evidence["candidate_semantics"] = candidate_semantics
                evidence["diagnostic"] = "zero/unavailable push base: full-affected diagnostics only; not release-eligible"
                evidence["evidence_digest"] = canonical_digest({
                    name: value for name, value in evidence.items() if name != "evidence_digest"
                })
                print(json.dumps({"ok": False, "evidence": evidence}, indent=2, sort_keys=True))
                return 2

            event_base = require_commit(args.event_base, "--event-base")
            policy, policy_raw, policy_coordinates = load_policy_at_commit(root_dir, event_base)
            base_graph = collect_graph(root_dir, args.root_identity, event_base, policy)
            candidate_graph = collect_graph(root_dir, args.root_identity, candidate, policy)
            base_semantics = evaluate_semantics(root_dir, policy, base_graph)
            candidate_semantics = evaluate_semantics(root_dir, policy, candidate_graph)
            merge_base: str | None = None
            if args.event == "pull_request":
                merge_base = _git_ok(["merge-base", event_base, candidate], root_dir).decode("ascii").strip()
                require_commit(merge_base, "computed merge base")
                if merge_base != event_base:
                    raise GraphError(
                        f"pull-request event base {event_base} does not equal computed merge-base {merge_base}"
                    )
            evidence = diff_graphs(
                base_graph,
                candidate_graph,
                policy,
                event=args.event,
                event_base=event_base,
                candidate=candidate,
                merge_base=merge_base,
                release_eligible=args.event == "push",
            )
            if policy.get("__legacy_registry_migration__") is True and evidence["affected_modules"]:
                raise GraphError(
                    "immutable base policy is the non-executable Task-2 seed: only an unchanged graph may "
                    "land the strict static-argv/workflow-limit policy correction; affected modules require "
                    "a later change governed by that landed policy"
                )
            evidence["dependency_policy"] = policy_coordinates
            evidence["base_semantics"] = base_semantics
            evidence["candidate_semantics"] = candidate_semantics
            evidence["evidence_digest"] = canonical_digest({
                name: value for name, value in evidence.items() if name != "evidence_digest"
            })
            print(json.dumps({"ok": True, "evidence": evidence}, indent=2, sort_keys=True))
            return 0

        commit = require_commit(args.commit, "--commit")
        if args.command == "validate" and args.policy is None:
            policy, _policy_raw, _policy_coordinates = load_policy_at_commit(root_dir, commit)
        else:
            policy = load_policy(policy_path)
        if args.command == "validate" and policy.get("__legacy_registry_migration__") is True:
            raise GraphError(
                "validate requires a strict committed policy; legacy policy migration is non-executable"
            )
        envelope = collect_graph(root_dir, args.root_identity, commit, policy)
        if args.command == "graph":
            print(json.dumps({"ok": True, "envelope": envelope}, indent=2, sort_keys=True))
            return 0
        if args.command == "validate":
            semantics = evaluate_semantics(root_dir, policy, envelope)
            print(json.dumps({"ok": True, "envelope": envelope, "semantics": semantics}, indent=2, sort_keys=True))
            return 0
        raise GraphError(f"unknown command {args.command!r}")
    except GraphError as exc:
        print(json.dumps({"ok": False, "error": str(exc)}, indent=2, sort_keys=True))
        return 1


if __name__ == "__main__":
    sys.exit(main())
