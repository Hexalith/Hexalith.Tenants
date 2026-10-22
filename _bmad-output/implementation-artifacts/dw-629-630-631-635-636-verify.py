"""Reproduce this fixed inventory qualification; run from the repository root.

Requires Python 3 and jsonschema (validated with 4.26.0). Uses only local Git
objects and tracked files. Future unrelated ledger changes intentionally fail.
"""

from collections import Counter
from copy import deepcopy
import hashlib
import json
from pathlib import Path
import re
import subprocess

from jsonschema import Draft202012Validator, ValidationError


ROOT = Path(__file__).resolve().parents[2]
BASE = "205cbc5019343d344643b530fb03aee82af2840d"
BUILDS = "c0cbbbd4366d512cc1731397f14dd53a7a01580d"
PLATFORM = "c65d87a6f8f4a05fa3bea4e24ac778bf23f12cd6"
ART = "_bmad-output/implementation-artifacts/"
SELECTED = {629, 630, 631, 635, 636}
MANIFEST = "module/hexalith.module-manifest.v2.json"
POLICY = "authenticated-current-global-administrator"


def git(*args, cwd=ROOT):
    return subprocess.check_output(["git", *args], cwd=cwd, text=True)


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def records(content):
    matches = list(re.finditer(r"^### DW-(\d+): [^\n]*\n.*?(?=^##|\Z)", content, re.M | re.S))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches) == 657, "657 unique ledger IDs required")
    return result


def parse_contract(source, label, prefixes):
    declarations = re.findall(r'\[\s*RestRoute(?:Attribute)?\s*\(', source)
    routes = re.findall(r'\[\s*RestRoute(?:Attribute)?\s*\(\s*RestVerb\.\w+,\s*"([^"]*)",\s*ApiScope\s*=\s*"([^"]+)"\s*\)\s*\]', source)
    require(len(routes) == len(declarations), f"Unparsed RestRoute declaration: {label}")
    result = []
    for route, scope in routes:
        require(scope == "tenants", f"Uninventoried API scope: {label}")
        members = re.findall(r'public static string (CommandType|QueryType)\s*=>\s*"([^"]+)"', source)
        require(len(members) == 1, f"Expected one literal command/query type: {label}")
        member, name = members[0]
        kind = "command" if member == "CommandType" else "query"
        effective = route[1:] if route.startswith("~/") else prefixes["tenant-api"] + ("/" + route if route else "")
        matches = [surface for surface, prefix in prefixes.items()
                   if effective == prefix or effective.startswith(prefix.rstrip("/") + "/")]
        require(len(matches) == 1, f"REST route outside declared routePrefix boundaries: {label}")
        surface = matches[0]
        if surface == "global-administrators-api":
            name = {"get-global-administrators": "list-global-administrators"}.get(name, name)
        result.append((surface, (name, kind)))
    return result


def contract_inventory(prefixes):
    result = {name: [] for name in prefixes}
    for path in sorted((ROOT / "src/Hexalith.Tenants.Contracts").rglob("*.cs")):
        if {"obj", "bin"} & set(path.relative_to(ROOT / "src/Hexalith.Tenants.Contracts").parts):
            continue
        for surface, operation in parse_contract(path.read_text(), path, prefixes):
            result[surface].append(operation)
    require([len(result[name]) for name in result] == [13, 3, 1], "Unexpected public route counts")
    return {name: sorted(items) for name, items in result.items()}


def qualify(manifest, validator, expected, before):
    validator.validate(manifest)
    module = manifest["modules"][0]
    surfaces = {item["name"]: item for item in module["surfaces"]["interfaces"]}
    for name, operations in expected.items():
        actual = sorted((op["name"], op["kind"]) for op in surfaces[name]["operations"])
        require(actual == operations, f"Contract inventory mismatch: {name}")
    admin = surfaces["global-administrators-api"]
    require(admin["authorization"] == {"policy": POLICY, "permissions": []}, "Administrator surface authority")
    require(all(op["authorizationPolicy"] == POLICY for op in admin["operations"]), "Administrator operation authority")
    lifecycle = module["lifecycle"]
    require(sum(flow["name"] == "global-administrator-management" for flow in lifecycle["criticalFlows"]) == 1,
            "Exactly one administrator management flow required")
    for flow in lifecycle["criticalFlows"]:
        require(flow["surface"] in surfaces, "Unknown critical flow surface")
        known = {op["name"] for op in surfaces[flow["surface"]]["operations"]}
        require(set(flow["operations"]) <= known, "Unknown critical flow operation")
        if flow["name"] == "global-administrator-management":
            require(set(flow["operations"]) == known and len(flow["operations"]) == 3, "Incomplete administrator flow")
    inventory = lifecycle["recoveryInventory"]
    require(len(inventory) == 3 and {item["name"] for item in inventory} == {
        "global-administrator-authority", "global-administrator-audit", "system-tenant-audit"}, "Recovery owner inventory")
    require(all(item == {"name": item["name"], "recoveryClass": "rebuild-only", "owner": "tenants", "dependencies": ["eventstore"]}
                for item in inventory), "Recovery inventory must not claim executable recovery")
    require(not lifecycle["tasks"] and not lifecycle["recoveryHooks"], "No executable recovery declared")
    require(module["integration"]["topics"] == [{"name": "tenants-events", "role": "events", "deadLetter": {"strategy": "none"}}],
            "Topic inventory must not add a runtime subscription")

    restored = deepcopy(manifest)
    restored_module = restored["modules"][0]
    old_module = before["modules"][0]
    restored_admin = next(item for item in restored_module["surfaces"]["interfaces"] if item["name"] == "global-administrators-api")
    old_admin = next(item for item in old_module["surfaces"]["interfaces"] if item["name"] == "global-administrators-api")
    restored_admin["authorization"] = old_admin["authorization"]
    for op, old_op in zip(restored_admin["operations"], old_admin["operations"], strict=True):
        op["authorizationPolicy"] = old_op["authorizationPolicy"]
    restored_flow = next(flow for flow in restored_module["lifecycle"]["criticalFlows"] if flow["name"] == "global-administrator-management")
    restored_flow["operations"].remove("remove-global-administrator")
    restored_module["lifecycle"]["recoveryInventory"] = old_module["lifecycle"]["recoveryInventory"]
    require(restored == before, "Unrelated manifest fields changed")


def main():
    schema_text = git("show", f"{BUILDS}:schemas/hexalith.module-manifest.v2.json", cwd=ROOT / "references/Hexalith.Builds")
    validator = Draft202012Validator(json.loads(schema_text))
    manifest = json.loads(read(MANIFEST))
    before = json.loads(git("show", f"{BASE}:{MANIFEST}"))
    prefixes = {item["name"]: item["routePrefix"] for item in before["modules"][0]["surfaces"]["interfaces"]
                if item["exposure"] == "public"}
    expected = contract_inventory(prefixes)
    qualify(manifest, validator, expected, before)
    print("PASS pinned v2 schema and public contract inventory: 13/3/1 operations")
    print("Schema SHA-256:", hashlib.sha256(schema_text.encode()).hexdigest())
    emitter = git("show", f"{PLATFORM}:src/Hexalith.Platform.Composition/PlatformDaprBindings.cs", cwd=ROOT / "references/Hexalith.Platform")
    require('if (!topic.TryGetProperty("subscription", out JsonElement subscription)) continue;' in emitter,
            "Platform must skip unsubscribed inventory")
    require('subscription.TryGetProperty("topic", out JsonElement physical)' in emitter, "Physical topic override missing")
    print("PASS Platform source-presence checks; control flow interpretation requires source inspection")
    publisher = read("src/Hexalith.Tenants.AppHost/Program.cs")
    require(re.search(r'\.WithEnvironment\(\s*"EventStore__Publisher__TopicOverrides__global-administrators",\s*"tenants\.events"\s*\)', publisher),
            "Actual AppHost publisher override must remain tenants.events")
    print("PASS actual AppHost publisher override: tenants.events (static configuration)")

    sample = read("src/Hexalith.Tenants.Contracts/Queries/GetGlobalAdministratorsQuery.cs")
    for label, source in (("unsupported attribute syntax", sample.replace("RestVerb.Get", "verb: RestVerb.Get")),
                          ("lookalike absolute route", sample.replace("~/api/global-administrators", "~/api/global-administrators-other"))):
        try: parse_contract(source, label, prefixes)
        except ValueError: print("PASS rejected parser control:", label)
        else: raise ValueError("Accepted unsupported parser control: " + label)

    mutants = []
    for label in ("dotted inventory name", "missing public operation", "weakened administrator policy", "missing removal flow", "absent administrator flow", "invented restore hook", "added subscription", "unrelated tenant flow change"):
        value = deepcopy(manifest)
        module = value["modules"][0]
        admin = next(item for item in module["surfaces"]["interfaces"] if item["name"] == "global-administrators-api")
        if label == "dotted inventory name": module["integration"]["topics"][0]["name"] = "tenants.events"
        elif label == "missing public operation": module["surfaces"]["interfaces"][0]["operations"].pop()
        elif label == "weakened administrator policy": admin["operations"][0]["authorizationPolicy"] = "authenticated"
        elif label == "missing removal flow": module["lifecycle"]["criticalFlows"][1]["operations"].pop()
        elif label == "absent administrator flow": module["lifecycle"]["criticalFlows"].pop()
        elif label == "invented restore hook": module["lifecycle"]["recoveryInventory"][0]["restoreHook"] = "unimplemented"
        elif label == "added subscription": module["integration"]["topics"][0]["subscription"] = {"server": "domain", "route": "/project", "topic": "tenants.events"}
        elif label == "unrelated tenant flow change": module["lifecycle"]["criticalFlows"][0]["operations"].pop()
        mutants.append((label, value))
    for label, value in mutants:
        try: qualify(value, validator, expected, before)
        except (ValueError, ValidationError): print("PASS rejected control:", label)
        else: raise ValueError("Accepted harmful control: " + label)

    ledger = ART + "deferred-work.md"
    old, new = records(git("show", f"{BASE}:{ledger}")), records(read(ledger))
    require(old.keys() == new.keys() and {n for n in new if old[n] != new[n]} == SELECTED, "Unexpected changed ledger records")
    for number in SELECTED:
        require(re.findall(r"^status: (.+)$", new[number], re.M) == ["done 2026-10-22"], "Selected status")
        require(re.findall(r"^disposition: (.+)$", new[number], re.M) == ["done"], "Selected disposition")
        for line in old[number].splitlines():
            require(not line or line in new[number].splitlines() or "pre-manifest-reconciliation-" + line in new[number].splitlines(), "Lost original ledger field")
    counts = Counter(re.search(r"^status: (\w+)", body, re.M)[1] for body in new.values())
    require(counts == {"done": 470, "open": 187}, "Unexpected ledger counts")
    index = "docs/maintenance-backlog.md"
    rows = lambda text: [line for line in text.splitlines() if line.startswith("| DW-")]
    old_rows, new_rows = rows(git("show", f"{BASE}:{index}")), rows(read(index))
    row_ids = lambda row: {int(n) for n in re.findall(r"DW-(\d+)", row.split("|")[1])}
    require(new_rows == [row for row in old_rows if not row_ids(row) & SELECTED], "Unrelated maintenance rows changed")
    covered = [n for row in new_rows for n in row_ids(row)]
    opened = {n for n, body in new.items() if re.search(r"^status: open$", body, re.M)}
    require(len(new_rows) == 173 and len(covered) == len(set(covered)) and set(covered) == opened, "Maintenance exact coverage")
    disposition = Counter(re.search(r"^disposition: (.+)$", body, re.M)[1] if re.search(r"^status: open$", body, re.M) else "done" for body in new.values())
    for key, count in disposition.items(): require(f"| {key} | {count} |" in read(index), "Stale disposition summary")
    print("PASS conservation: exactly five closures, 657 IDs, 470 done/187 open, 173 index rows")
    print("PASS dispositions:", json.dumps(dict(sorted(disposition.items()))))
    print("PASS all unrelated manifest fields, ledger records and maintenance rows preserved")


if __name__ == "__main__":
    main()
