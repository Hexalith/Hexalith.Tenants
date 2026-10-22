#!/usr/bin/env python3
"""Replay harmful publisher mutations only in disposable offline fixture roots."""
from __future__ import annotations

import importlib.util
import io
import json
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("recovery_fixture", ROOT / "tests/scripts/test_partial_release_recovery.py")
fixture = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fixture)

CONTROLS = [
    ("omit-prepared-proof", "test_changed_or_missing_prepared_bytes_refuse",
     lambda text: text.replace("recovery_require_prepared\n", "")),
    ("omit-final-proof", "test_preparation_evidence_is_rechecked_after_asset_reads",
     lambda text: text[:text.rindex("recovery_require_prepared\n")] + text[text.rindex("recovery_require_prepared\n") + len("recovery_require_prepared\n"):]),
    ("rebuild-with-live-key", "test_publication_never_rebuilds_prepared_packages",
     lambda text: text.replace("recovery_require_prepared\n", "recovery_require_prepared\ndotnet build Hexalith.Tenants.slnx --configuration Release -p:Version=\"$version\"\n", 1)),
    ("omit-key-admission", "test_missing_empty_or_whitespace_key_refuses_before_reads",
     lambda text: text.replace("recovery_fail 'NUGET_API_KEY is required for missing packages.'", ":")),
]

results = []
for name, method, mutate in CONTROLS:
    with tempfile.TemporaryDirectory(prefix="tenants-credential-mutation-") as directory:
        mirror = Path(directory)
        shutil.copytree(ROOT / "scripts", mirror / "scripts")
        (mirror / "tools").mkdir()
        shutil.copyfile(ROOT / "tools/release-packages.json", mirror / "tools/release-packages.json")
        for package in json.loads((mirror / "tools/release-packages.json").read_text())["packages"]:
            project = mirror / package["project"]
            project.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / package["project"], project)
        fixture.ROOT = mirror
        publisher = mirror / "scripts/publish-partial-release.sh"
        original = publisher.read_text()
        baseline = unittest.TextTestRunner(stream=io.StringIO()).run(
            unittest.TestSuite([fixture.PartialReleaseRecoveryTests(method)]))
        if not baseline.wasSuccessful():
            raise SystemExit("Baseline failed for " + name)
        changed = mutate(original)
        assert changed != original, name
        publisher.write_text(changed)
        output = io.StringIO()
        result = unittest.TextTestRunner(stream=output).run(
            unittest.TestSuite([fixture.PartialReleaseRecoveryTests(method)]))
        if result.wasSuccessful() or result.errors:
            raise SystemExit("Mutation did not reject through behavioral assertions: " + name + "\n" + output.getvalue())
        results.append({"mutation": name, "baseline_passed": True, "rejected": True,
                        "assertion_failures": len(result.failures)})
        print(name + ": rejected")
print(json.dumps(results, indent=2))
