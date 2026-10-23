"""Exercise both shipped elicitation catalogs without optional test dependencies."""

import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = [ROOT / entry / "skills/bmad-advanced-elicitation/scripts/pick_methods.py"
           for entry in (".agents", ".claude")]


def row(name, number=""):
    return dict(num=number, method_name=name, category="test", description="fixture",
                output_pattern="result")


class ElicitationNumberTests(unittest.TestCase):
    def modules(self):
        for script in SCRIPTS:
            spec = importlib.util.spec_from_file_location("catalog_fixture", script)
            module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)
            yield module

    def test_replacements_and_appends_remain_number_addressable(self):
        for module in self.modules():
            original = [row("First", "1"), row("Second", "2")]
            extras = [row("FIRST"), row("Second", "7"), row("New"), row("Explicit", "10")]
            before = json.dumps([original, extras], sort_keys=True)
            merged = module.merge_extra(original, extras)
            self.assertEqual([r["num"] for r in merged], ["1", "7", "11", "10"])
            for method in merged:
                found, missing = module.find(merged, [method["num"]])
                self.assertEqual(found, [method])
                self.assertEqual(missing, [])
            self.assertEqual(json.dumps([original, extras], sort_keys=True), before)

    def test_duplicate_numbers_are_rejected(self):
        scenarios = [
            [row("First", "2")],
            [row("Added", "1")],
            [row("First", "8"), row("Second", "8")],
            [row("Added", "8"), row("Other", "8")],
            [row("First", "8"), row("Added", "8")],
        ]
        for module in self.modules():
            for extras in scenarios:
                with self.subTest(script=module.__file__, extras=extras):
                    with self.assertRaisesRegex(ValueError, "duplicate method number"):
                        module.merge_extra([row("First", "1"), row("Second", "2")], extras)

    def test_automatic_allocation_preserves_unnumbered_inputs(self):
        for module in self.modules():
            original = [row("Unnumbered"), row("Second", "2")]
            extras = [row("Added")]
            before = json.dumps([original, extras], sort_keys=True)
            merged = module.merge_extra(original, extras)
            self.assertEqual([r["num"] for r in merged], ["3", "2", "4"])
            self.assertEqual(json.dumps([original, extras], sort_keys=True), before)

    def test_renumbering_can_free_a_number_for_another_method(self):
        for module in self.modules():
            merged = module.merge_extra([row("First", "1"), row("Second", "2")],
                                        [row("First", "3"), row("Added", "1")])
            self.assertEqual([r["num"] for r in merged], ["3", "2", "1"])

    def test_cli_rejects_collision_without_printing_a_partial_catalog(self):
        for script in SCRIPTS:
            result = subprocess.run(
                [sys.executable, str(script), "--extra",
                 json.dumps([row("Tree of Thoughts", "2")]), "list", "--all"],
                capture_output=True, text=True, check=False)
            self.assertEqual(result.returncode, 2, result.stderr)
            self.assertIn("duplicate method number: 2", result.stderr)
            self.assertEqual(result.stdout, "")

    def test_mirrored_sources_match(self):
        self.assertEqual(SCRIPTS[0].read_bytes(), SCRIPTS[1].read_bytes())


if __name__ == "__main__":
    unittest.main(verbosity=2)
