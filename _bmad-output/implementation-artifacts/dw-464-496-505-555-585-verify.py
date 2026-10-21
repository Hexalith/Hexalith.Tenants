"""Replay the fixed CI-record closure snapshot offline from the repository root.

This checks retained evidence, not a fresh browser run or live GitHub status.
Future ledger changes intentionally fail; replay at this file's introducing commit.
"""

from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import subprocess
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parents[2]
ARTIFACTS = "_bmad-output/implementation-artifacts/"
BASE = "b02452a16869f265c21b1ae6c15f2f299677f139"
SUCCESS = "3ae9db014dfe52bdee4f6731465cc2a7b65701b7"
SELECTED = {464, 496, 505, 555, 585}
EVENT_BASE = "0c10ef637171f9ac2639d4b5ac42f513ea9ef133"
IMPLEMENTATION = {
    ".github/workflows/story-guards.yml": "6cfa604231c58dc9e507e5b4c24c88e2d6628855",
    "tests/Hexalith.Tenants.UI.Tests/Browser/dump-validation-dom.mjs": "5487b9b5d647755430185341244a20d06b96686c",
    "tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh": "fd76f0d1e7c2b8963b53785c736231b811c2fc75",
}
RUNS = {
    "first-success-39167197905.json": ("39167197905", "579ec157ac38eb30593e736042aedac4d1acd08d"),
    "last-success-39426910956.json": ("39426910956", SUCCESS),
    "current-39659580488.json": ("39659580488", BASE),
}
LOG_JOBS = {
    "first-success-browser-117792500177.log": (117792500177, "39167197905"),
    "last-success-browser-118641663695.log": (118641663695, "39426910956"),
    "current-browser-119428775389.log": (119428775389, "39659580488"),
    "current-gitlink-119428776078.log": (119428776078, "39659580488"),
}
AUDITS = [
    (ARTIFACTS + "spec-2-4b-wp-2a-removal-proof-and-audit-available.md", "b2b80941df874c2ee6772ca316841c480e0e493b"),
    (ARTIFACTS + "spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md", "2b91b05be9c927319fd207b02552d1213997776b"),
    (ARTIFACTS + "spec-dw-297-462-533-549-558-evidence-reconciliation.md", BASE),
]


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def git(*arguments):
    return subprocess.check_output(["git", *arguments], cwd=ROOT, text=True).strip()


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def records(text):
    matches = list(re.finditer(r"^### DW-(\d+): [^\n]*\n.*?(?=^##|\Z)", text, re.M | re.S))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches) == 657, "Expected 657 unique ledger records")
    return result


def main():
    path = ARTIFACTS + "deferred-work.md"
    original = subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT, text=True)
    before, after = records(original), records(read(path))
    require(before.keys() == after.keys(), "Ledger IDs changed")
    require({n for n in after if before[n] != after[n]} == SELECTED,
            "Expected exactly the five selected record changes")
    for number in SELECTED:
        require(re.search(r"^status: done 2026-10-21$", after[number], re.M),
                f"DW-{number} is not closed")
        require(re.findall(r"^disposition: ([^\n]+)$", after[number], re.M) == ["done"],
                f"DW-{number} has an inconsistent active disposition")
        active_lines = set(after[number].splitlines())
        for line in before[number].splitlines():
            require(not line or line in active_lines
                    or "pre-ci-reconciliation-" + line in active_lines,
                    f"Lost original DW-{number} field: {line}")
    counts = Counter()
    opened = set()
    for number, body in after.items():
        status = re.search(r"^status: (\w+)", body, re.M)[1]
        if status == "open":
            opened.add(number)
        counts["done" if status == "done" else re.search(r"^disposition: ([^\n]+)", body, re.M)[1]] += 1
    require(counts == {"done": 465, "actionable": 109, "blocked": 15,
                       "awaiting-decision": 25, "accepted-limitation": 9, "uncertain": 34},
            f"Wrong disposition counts: {counts}")
    backlog = read("docs/maintenance-backlog.md")
    rows = [line for line in backlog.splitlines() if line.startswith("| DW-")]
    prior_backlog = subprocess.check_output(
        ["git", "show", f"{BASE}:docs/maintenance-backlog.md"], cwd=ROOT, text=True)
    expected_rows = [line for line in prior_backlog.splitlines() if line.startswith("| DW-")
                     and not SELECTED.intersection(map(int, re.findall(r"DW-(\d+)", line.split("|")[1])))]
    require(rows == expected_rows, "Unrelated maintenance rows changed")
    covered = [int(value) for row in rows for value in re.findall(r"DW-(\d+)", row.split("|")[1])]
    require(len(rows) == 178 and len(covered) == len(set(covered)) == 192
            and set(covered) == opened, "Maintenance rows must exactly cover open records")
    for disposition, count in counts.items():
        require(f"| {disposition} | {count} |" in backlog, "Stale disposition table")
    require("**Remaining: 192 open records across 178 index rows.**" in backlog,
            "Stale remaining count")
    print("PASS 657 IDs; exactly five closures; 465 done/192 open; 178 exact-covering rows")

    data = json.loads(read(ARTIFACTS + "dw-464-496-505-555-585-hosted-evidence.json"))
    require(data["entry_commit"] == BASE, "Wrong entry revision")
    require(data["entry_event"] == {"before": EVENT_BASE, "after": BASE}, "Wrong retained event")
    require(Counter(item["file"] for item in data["runs"]) == Counter(RUNS.keys())
            and Counter(item["file"] for item in data["logs"]) == Counter(LOG_JOBS.keys()),
            "Missing, duplicate or unexpected evidence entries")
    require(all(item["retrieval_exit_code"] == 0 for item in data["runs"] + data["logs"]),
            "A hosted evidence retrieval failed")
    with zipfile.ZipFile(ROOT / (ARTIFACTS + "dw-464-496-505-555-585-raw-hosted-evidence.zip")) as archive:
        expected_members = set(RUNS) | set(LOG_JOBS)
        require(len(archive.namelist()) == len(expected_members)
                and set(archive.namelist()) == expected_members, "Wrong raw evidence archive members")
        for item in data["runs"] + data["logs"]:
            require(item["raw_member"] == item["file"], "Wrong raw member binding")
            raw = archive.read(item["raw_member"])
            require(len(raw) == item["raw_bytes"]
                    and hashlib.sha256(raw).hexdigest() == item["raw_sha256"], "Raw evidence digest/size mismatch")
            if item["file"] in RUNS:
                require(json.loads(raw) == item["response"], "Run response differs from raw evidence")
            else:
                lines = raw.decode("utf-8").splitlines()
                numbers = [excerpt["line"] for excerpt in item["excerpts"]]
                require(numbers == sorted(set(numbers)), "Repeated or unordered excerpt lines")
                for excerpt in item["excerpts"]:
                    require(0 < excerpt["line"] <= len(lines)
                            and lines[excerpt["line"] - 1] == excerpt["text"], "Excerpt differs from raw log position")
    jobs = {}
    for run in data["runs"]:
        run_id, revision = RUNS[run["file"]]
        require(run["retrieval_command"] == ["gh", "run", "view", run_id, "--json", "headSha,jobs,url"]
                and run["response"]["headSha"] == revision
                and run["response"]["url"] == f"https://github.com/Hexalith/Hexalith.Tenants/actions/runs/{run_id}",
                "Wrong run retrieval/revision binding")
        for job in run["response"]["jobs"]:
            require(job["databaseId"] not in jobs, "Repeated hosted job ID")
            jobs[job["databaseId"]] = (run["response"]["headSha"], job)
    for log in data["logs"]:
        job_id, run_id = LOG_JOBS[log["file"]]
        require(log["retrieval_command"] == ["gh", "api", f"repos/Hexalith/Hexalith.Tenants/actions/jobs/{job_id}/logs"]
                and jobs[job_id][1]["url"] == f"https://github.com/Hexalith/Hexalith.Tenants/actions/runs/{run_id}/job/{job_id}",
                "Wrong log repository/job binding")
    for job_id, revision in [(117792500177, "579ec157ac38eb30593e736042aedac4d1acd08d"),
                             (118641663695, SUCCESS), (119428776078, BASE)]:
        actual_revision, job = jobs[job_id]
        require(actual_revision == revision and job["status"] == "completed"
                and job["conclusion"] == "success", f"Wrong successful hosted job {job_id}")
        step = ("Validate changed story gitlinks" if job_id == 119428776078
                else "Validate the shipped removal focus module in Chromium")
        require(any(s["name"] == step and s["conclusion"] == "success" for s in job["steps"]),
                f"Missing executed hosted step: {step}")
    actual_revision, job = jobs[119428775389]
    require(actual_revision == BASE and job["conclusion"] == "failure", "Lost current failure")
    steps = {s["name"]: s["conclusion"] for s in job["steps"]}
    require(steps["Build the UI browser-validation graph"] == "failure"
            and steps["Install Chrome for the removal focus validator"] == "skipped"
            and steps["Install the matching Chrome headless shell"] == "skipped"
            and steps["Validate the shipped removal focus module in Chromium"] == "skipped",
            "Current browser skip/build failure was obscured")
    print("PASS hosted successes and separate current pre-browser build failure retained")

    logs = {log["file"]: "\n".join(item["text"] for item in log["excerpts"]) for log in data["logs"]}
    for name in ["first-success-browser-117792500177.log", "last-success-browser-118641663695.log"]:
        for marker in ["153.0.8010.52", "TENANTS_BROWSER_BUILD_CONFIGURATION: Release",
                       "Rendered tenant start/preview/recovery EN/FR:",
                       "missing-tabindex control rejected", "Return-true mutation: correctly rejected",
                       "Removal-dialog in-flow and hidden CSS mutations: correctly rejected",
                       "Unstacked availability recoveries at 390px: correctly rejected"]:
            require(marker in logs[name], f"Missing browser evidence: {name}: {marker}")
    require("NativeSourceProof" in logs["current-browser-119428775389.log"]
            and "error CS0246" in logs["current-browser-119428775389.log"], "Lost build diagnostic")
    require("Changed stories: 3; failed: 0" in logs["current-gitlink-119428776078.log"],
            "Missing actual changed-story audit")
    require(logs["current-gitlink-119428776078.log"].count("RESULT: PASS") == 3,
            "Missing actual-story pass reports")
    audit_log = logs["current-gitlink-119428776078.log"]
    require(f"Event range: {EVENT_BASE}..{BASE}" in audit_log, "Wrong actual-story event range")
    require(re.findall(r"Audit: (\S+) at ([0-9a-f]{40})", audit_log) == AUDITS,
            "Wrong actual-story artifact/endpoint pairs")
    comparison = data["source_blob_comparison"]
    require(comparison["identical"] and comparison["diff_bytes"] == 0, "Wrong blob comparison")
    require(set(comparison["trees"]) == {BASE, SUCCESS}, "Wrong comparison revisions")
    recorded = {}
    for line in comparison["trees"][BASE].splitlines():
        metadata, path = line.split("\t")
        blob = metadata.split()[2]
        require(path not in recorded, "Repeated implementation path")
        recorded[path] = blob
    require(recorded == IMPLEMENTATION, "Wrong implementation paths/blobs")
    require(comparison["trees"][BASE] == comparison["trees"][SUCCESS], "Recorded trees differ")
    for path, blob in IMPLEMENTATION.items():
        require(git("rev-parse", f"{BASE}:{path}") == blob
                and git("rev-parse", f"{SUCCESS}:{path}") == blob,
                f"Historical implementation differs: {path}")
        require(subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT)
                == (ROOT / path).read_bytes(), f"Current implementation changed: {path}")
    print("PASS numbered scenario evidence and exact historical/current implementation equality")
    with tempfile.TemporaryDirectory(prefix="dw-ci-event-") as directory:
        event = Path(directory) / "entry-event.json"
        event.write_text(json.dumps(data["entry_event"]) + "\n", encoding="utf-8")
        replay = subprocess.run(["python3", str(ROOT / "scripts/validate-changed-story-gitlinks.py"),
                                 "--event-name", "push", "--event-path", str(event)],
                                cwd=ROOT, capture_output=True, text=True, check=False)
        require(replay.returncode == 0 and "Changed stories: 3; failed: 0" in replay.stdout,
                f"Actual-story replay failed: {replay.stdout} {replay.stderr}")
    print("PASS actual-story replay from retained event input")


if __name__ == "__main__":
    main()
