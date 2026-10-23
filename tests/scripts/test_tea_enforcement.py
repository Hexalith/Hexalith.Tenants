"""Execute TEA scanner and hook modes in disposable project roots through Node."""

import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
HOOKS = [ROOT / entry / "skills/bmad-testarch-framework/resources/hooks/tea-enforce.cjs"
         for entry in (".agents", ".claude")]
FOCUSED = "test.only('subject', () => expect(actual).toBe(expected));\n"
CLEAN = "test('subject', () => expect(actual).toBe(expected));\n"


class TeaEnforcementTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="tea hook ")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.env = dict(os.environ, CLAUDE_PROJECT_DIR=str(self.root))

    def api(self, hook, expression, *args):
        result = subprocess.run(
            ["node", "-e", "const hook = require(process.argv[1]);\n"
             f"process.stdout.write(JSON.stringify({expression}));", str(hook), *args],
            cwd=self.root, capture_output=True, text=True, check=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        return json.loads(result.stdout)

    def invoke(self, hook, mode, payload):
        return subprocess.run(["node", str(hook), mode], cwd=self.root, env=self.env,
                              input=payload if isinstance(payload, str) else json.dumps(payload),
                              capture_output=True, text=True, check=False)

    def write(self, path, content):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(content)

    def test_every_absolute_registry_row_is_classified_with_matching_severity(self):
        for hook in HOOKS:
            metadata = self.api(hook, "({rules: hook.RULES, deferred: hook.DEFERRED})")
            registry = hook.parents[3] / "bmad-testarch-test-review/steps-c/criteria-registry.md"
            rows = []
            for line in registry.read_text().splitlines():
                cells = [cell.strip() for cell in line.split("|")]
                if len(cells) >= 7 and re.fullmatch(r"[CHML]\d+", cells[1]) and cells[-2] == "Absolute":
                    rows.append((cells[1], cells[-3]))
            self.assertTrue(rows, "Registry parser must recognize Absolute criteria")
            absolute = dict(rows)
            self.assertEqual(len(rows), len(absolute))
            rules = metadata["rules"]
            implemented = {rule["id"] for rule in rules}
            deferred = set(metadata["deferred"])
            self.assertEqual(len(rules), len(implemented))
            self.assertFalse(implemented & deferred)
            self.assertEqual(implemented | deferred, set(absolute))
            for rule in rules:
                self.assertEqual(rule["severity"], absolute[rule["id"]])
                self.assertEqual(rule["gate"], "Absolute")
            self.assertTrue(all(metadata["deferred"].values()))
            partial = next(rule for rule in rules if rule["id"] == "C4")
            self.assertEqual(partial["kinds"], ["maestro"])
            self.assertIn("general", partial["partial"])

    def test_scanner_rules_have_reachable_positive_cases(self):
        cases = [
            ("sample.spec.ts", "test.skip('case', () => run());", "C1"),
            ("sample.spec.ts", FOCUSED, "C2"),
            ("sample.spec.ts", "expect(actual).toBe(actual);", "C3"),
            (".maestro/flow.yaml", "appId: fixture\n---\n- launchApp\n", "C4"),
            ("sample.spec.ts", "await page.waitForTimeout(500);", "H1"),
            ("sample.spec.ts", "\n" * 1001, "H5"),
            ("vitest.config.pact.ts", "export default {};", "H6"),
            ("vitest.config.pact.ts", "export default {fileParallelism: false, maxWorkers: 2};", "H8"),
        ]
        for hook in HOOKS:
            implemented = self.api(hook, "hook.RULES.map(rule => rule.id)")
            self.assertEqual({rule for _, _, rule in cases}, set(implemented))
            for path, source, rule in cases:
                with self.subTest(hook=hook, rule=rule):
                    found = self.api(hook,
                                     "hook.scanContent(process.argv[2], process.argv[3], hook.DEFAULT_CONFIG)",
                                     path, source)
                    self.assertIn(rule, [finding["rule"]["id"] for finding in found])

    def test_supported_language_branches_detect_violations_and_ignore_noise(self):
        cases = [
            ("test_sample.py", "time.sleep(1)\n", "# time.sleep(1)\ntext = 'time.sleep(1)'\n"),
            ("SampleTest.java", "Thread.sleep(1);\n", '// Thread.sleep(1);\nString text = "Thread.sleep(1)";\n'),
            ("sample_test.go", "time.Sleep(time.Second)\n", '// time.Sleep(time.Second)\ntext := "time.Sleep(time.Second)"\n'),
        ]
        for hook in HOOKS:
            for path, violation, noise in cases:
                with self.subTest(hook=hook, path=path):
                    found = self.api(hook,
                                     "hook.scanContent(process.argv[2], process.argv[3], hook.DEFAULT_CONFIG)",
                                     path, violation)
                    self.assertIn("H1", [finding["rule"]["id"] for finding in found])
                    found = self.api(hook,
                                     "hook.scanContent(process.argv[2], process.argv[3], hook.DEFAULT_CONFIG)",
                                     path, noise)
                    self.assertEqual(found, [])

    def test_write_enforces_whole_file_rules_while_edits_remain_fragments(self):
        cases = [(".maestro/flow.yaml", "appId: fixture\n---\n- launchApp\n", "C4"),
                 ("sample.spec.ts", "\n" * 1001, "H5")]
        for hook in HOOKS:
            for path, source, rule in cases:
                for tool, fragment, expected in (
                        ("Write", {"content": source}, 2),
                        ("Edit", {"new_string": source}, 0),
                        ("MultiEdit", {"edits": [{"new_string": source}]}, 0)):
                    with self.subTest(hook=hook, rule=rule, tool=tool):
                        result = self.invoke(hook, "--pre", {"tool_name": tool,
                                             "tool_input": {"file_path": path, **fragment}})
                        self.assertEqual(result.returncode, expected, result.stderr)
                        if tool == "Write":
                            self.assertIn(f"[{rule} ", result.stderr)
                        else:
                            self.assertEqual(result.stderr, "")

    def test_undocumented_skip_warns_and_documented_skip_is_silent(self):
        for hook in HOOKS:
            for comment in ("", "// Requires an unavailable external fixture\n"):
                result = self.invoke(hook, "--pre", {"tool_name": "Write", "tool_input": {
                    "file_path": "sample.spec.ts", "content": comment + "test.skip('subject', () => run());\n"}})
                self.assertEqual(result.returncode, 0, result.stderr)
                if comment:
                    self.assertEqual(result.stderr, "")
                else:
                    self.assertIn("[C1 CRITICAL]", result.stderr)
                    self.assertIn("advisory", result.stderr)

    def test_pre_blocks_focused_write_edit_and_multiedit(self):
        for hook in HOOKS:
            for tool, fragment in (("Write", {"content": FOCUSED}),
                                   ("Edit", {"new_string": FOCUSED}),
                                   ("MultiEdit", {"edits": [{"new_string": FOCUSED}]})):
                result = self.invoke(hook, "--pre", {"tool_name": tool,
                                     "tool_input": {"file_path": "sample.spec.ts", **fragment}})
                self.assertEqual(result.returncode, 2, result.stderr)
                self.assertIn("[C2 CRITICAL]", result.stderr)
                self.assertIn("Fix:", result.stderr)

    def test_clean_comments_strings_and_ungated_paths_are_allowed(self):
        for hook in HOOKS:
            for path, source in (("sample.spec.ts", CLEAN),
                                 ("sample.spec.ts", "// test.only('example');\nconst text = 'fit(example)';\n"),
                                 ("example.ts", FOCUSED), ("../outside.spec.ts", FOCUSED)):
                result = self.invoke(hook, "--pre", {"tool_name": "Write",
                                     "tool_input": {"file_path": path, "content": source}})
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stderr, "")

    def test_post_rereads_complete_file_for_edit_and_bash(self):
        self.write("sample.spec.ts", FOCUSED)
        for hook in HOOKS:
            for tool, payload in (("Edit", {"file_path": "sample.spec.ts", "new_string": CLEAN}),
                                  ("Bash", {"command": "cat > sample.spec.ts"})):
                result = self.invoke(hook, "--post", {"tool_name": tool, "tool_input": payload})
                self.assertEqual(result.returncode, 2, result.stderr)
                self.assertIn("sample.spec.ts:1", result.stderr)
                self.assertIn("[C2 CRITICAL]", result.stderr)

    def test_stop_finds_unnamed_recent_files_and_avoids_reentry(self):
        self.write("nested/sample.spec.ts", FOCUSED)
        for hook in HOOKS:
            result = self.invoke(hook, "--stop", {})
            self.assertEqual(result.returncode, 2, result.stderr)
            self.assertIn("nested/sample.spec.ts:1", result.stderr)
            result = self.invoke(hook, "--stop", {"stop_hook_active": True})
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stderr, "")

    def test_stop_ignores_files_outside_the_configured_window(self):
        self.write("old.spec.ts", FOCUSED)
        self.write(".tea/enforce-config.json", json.dumps({"stopScanWindowSeconds": 60}))
        os.utime(self.root / "old.spec.ts", (1, 1))
        for hook in HOOKS:
            result = self.invoke(hook, "--stop", {})
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stderr, "")

    def test_stop_catches_quoted_bash_targets_missed_by_best_effort_post_scan(self):
        # Bash token extraction is deliberately best effort; stop scans unnamed files.
        self.write("sample with space.spec.ts", FOCUSED)
        for hook in HOOKS:
            result = self.invoke(hook, "--post", {"tool_name": "Bash", "tool_input": {
                "command": 'cat > "sample with space.spec.ts"'}})
            self.assertEqual(result.returncode, 0, result.stderr)
            result = self.invoke(hook, "--stop", {})
            self.assertEqual(result.returncode, 2, result.stderr)
            self.assertIn("sample with space.spec.ts:1", result.stderr)

    def test_configured_disabled_rule_and_excluded_file_are_allowed(self):
        for config in ({"disabledRules": ["C2"]}, {"excludeGlobs": ["**/*.spec.ts"]}):
            self.write(".tea/enforce-config.json", json.dumps(config))
            for hook in HOOKS:
                result = self.invoke(hook, "--pre", {"tool_name": "Write",
                                     "tool_input": {"file_path": "sample.spec.ts", "content": FOCUSED}})
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stderr, "")

    def test_hook_failures_allow_writes_as_documented(self):
        for hook in HOOKS:
            for payload in ("{", {"tool_name": "Edit"}):
                result = self.invoke(hook, "--pre", payload)
                self.assertEqual(result.returncode, 0, result.stderr)
            result = self.invoke(hook, "--post", {"tool_name": "Edit",
                                 "tool_input": {"file_path": "missing.spec.ts"}})
            self.assertEqual(result.returncode, 0, result.stderr)
        self.write(".tea/enforce-config.json", "{")
        for hook in HOOKS:
            result = self.invoke(hook, "--pre", {"tool_name": "Write",
                                 "tool_input": {"file_path": "sample.spec.ts", "content": FOCUSED}})
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("is not valid JSON", result.stderr)

    def test_mirrored_hooks_match(self):
        self.assertEqual(HOOKS[0].read_bytes(), HOOKS[1].read_bytes())


if __name__ == "__main__":
    unittest.main(verbosity=2)
