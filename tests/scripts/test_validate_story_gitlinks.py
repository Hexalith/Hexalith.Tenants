"""Regression lane for `scripts/validate-story-gitlinks.py`.

Decision D-K originally accepted "no test lane, manual executable verification only"
as an owned limitation. Review loop 13 reversed it: three defects were found in the
parser at once, and two of them made the guard **pass a tree it should have failed**.
A guard whose failure mode is a silent pass cannot rest on manual checks.

Stdlib `unittest` only -- this repository has no Python test infrastructure, and
adding pytest for one file would be a dependency the project rules do not want.

Run: `python3 tests/scripts/test_validate_story_gitlinks.py`
"""

import importlib.util
import pathlib
import shutil
import subprocess
import tempfile
import unittest

_SCRIPT = pathlib.Path(__file__).resolve().parents[2] / "scripts" / "validate-story-gitlinks.py"
_spec = importlib.util.spec_from_file_location("validate_story_gitlinks", _SCRIPT)
assert _spec is not None and _spec.loader is not None
guard = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(guard)

ZWSP = "​"
NBSP = " "


class NormalizeLineTests(unittest.TestCase):
    def test_format_controls_are_removed_not_folded(self):
        self.assertEqual(guard.normalize_line(f"references/X{ZWSP}"), "references/X")

    def test_separators_are_folded_to_a_plain_space(self):
        for separator in (NBSP, " ", " ", "　", "\t"):
            with self.subTest(separator=hex(ord(separator))):
                self.assertEqual(guard.normalize_line(f"a{separator}b"), "a b")

    def test_ordinary_text_is_unchanged(self):
        self.assertEqual(guard.normalize_line("- `references/X` 1234567 -> abcdefg"),
                         "- `references/X` 1234567 -> abcdefg")


class DeclaredPathsTests(unittest.TestCase):
    def test_file_list_entries_below_a_subheading_are_still_declared(self):
        """The `File List` body must not terminate at its first `###`.

        Terminating there dropped every entry below it, so a correctly declared bump
        read as UNDECLARED and failed a correct tree.
        """
        story = (
            "## File List\n"
            "\n"
            "### Source\n"
            "- `src/Thing.cs`\n"
            "\n"
            "### Submodules\n"
            "- `references/Hexalith.Builds`\n"
            "\n"
            "## Completion Notes List\n"
        )
        self.assertIn("references/Hexalith.Builds", guard.declared_paths(story))

    def test_a_trailing_nbsp_does_not_prevent_a_declaration(self):
        story = f"## File List\n- `references/Hexalith.Builds{NBSP}`\n"
        self.assertIn("references/Hexalith.Builds", guard.declared_paths(story))

    def test_a_zero_width_space_does_not_produce_a_phantom_key(self):
        story = f"## File List\n- `references/Hexalith.Builds{ZWSP}`\n"
        self.assertEqual(guard.declared_paths(story), {"references/Hexalith.Builds"})

    def test_prose_denying_a_change_is_not_a_declaration(self):
        story = "## File List\n- `references/Hexalith.Builds` was left untouched\n"
        self.assertEqual(guard.declared_paths(story), set())

    def test_completion_notes_stop_at_the_first_subheading(self):
        """Retained historical tables under `### Review Findings` are not declarations."""
        story = (
            "## Completion Notes List\n"
            "- `references/Hexalith.Builds`\n"
            "\n"
            "### Review Findings\n"
            "- `references/Hexalith.Memories`\n"
        )
        declared = guard.declared_paths(story)
        self.assertIn("references/Hexalith.Builds", declared)
        self.assertNotIn("references/Hexalith.Memories", declared)


class StatedTargetsTests(unittest.TestCase):
    def _file_list(self, body: str) -> dict[str, str]:
        return guard.stated_targets(f"## File List\n{body}\n")

    def test_a_format_control_next_to_the_arrow_still_states_a_target(self):
        """The defect that motivated this lane.

        `ARROW_CHAIN` used to scan the raw line, and `\\s` does not match U+200B, so
        this produced no match, recorded no claim, and let a stale SHA print as
        `[declared]` with a PASS.
        """
        stated = self._file_list(f"- `references/Hexalith.Builds` 53d53ae{ZWSP} -> b529b66")
        self.assertEqual(stated.get("references/Hexalith.Builds"), "b529b66")

    def test_an_nbsp_around_the_arrow_still_states_a_target(self):
        stated = self._file_list(f"- `references/Hexalith.Builds` 53d53ae{NBSP}->{NBSP}b529b66")
        self.assertEqual(stated.get("references/Hexalith.Builds"), "b529b66")

    def test_the_last_hop_of_a_chain_wins(self):
        stated = self._file_list("- `references/Hexalith.Builds` 53d53ae -> aaaaaaa -> b529b66")
        self.assertEqual(stated.get("references/Hexalith.Builds"), "b529b66")

    def test_the_last_statement_for_a_path_wins(self):
        stated = self._file_list(
            "- `references/Hexalith.Builds` 53d53ae -> aaaaaaa\n"
            "- `references/Hexalith.Builds` 53d53ae -> b529b66")
        self.assertEqual(stated.get("references/Hexalith.Builds"), "b529b66")

    def test_a_multi_pointer_line_states_one_target_per_path(self):
        stated = self._file_list(
            "- `references/Hexalith.Builds` 53d53ae -> b529b66, "
            "`references/Hexalith.Memories` 1868c8f -> a1f64d5")
        self.assertEqual(stated.get("references/Hexalith.Builds"), "b529b66")
        self.assertEqual(stated.get("references/Hexalith.Memories"), "a1f64d5")

    def test_en_dash_and_unicode_arrows_are_accepted(self):
        for arrow in ("->", "-->", "—>", "–>", "→"):
            with self.subTest(arrow=arrow):
                stated = self._file_list(f"- `references/Hexalith.Builds` 53d53ae {arrow} b529b66")
                self.assertEqual(stated.get("references/Hexalith.Builds"), "b529b66")

    def test_superseded_tables_under_a_review_subheading_are_not_binding(self):
        story = (
            "## Completion Notes List\n"
            "- `references/Hexalith.Builds` 53d53ae -> b529b66\n"
            "\n"
            "### Review Findings\n"
            "- `references/Hexalith.Builds` 53d53ae -> deadbee\n"
        )
        self.assertEqual(guard.stated_targets(story).get("references/Hexalith.Builds"), "b529b66")


class PathTokenOffsetTests(unittest.TestCase):
    def test_reported_offset_indexes_the_reported_path(self):
        """Offsets must index the same string the arrow scan reads.

        Advancing by the *unfiltered* token length while building the path from the
        filtered one desynchronized the two, so a chain could be matched to the wrong
        path on a multi-pointer line.
        """
        for raw in (
            "- `references/Hexalith.Builds` 53d53ae -> b529b66",
            f"- `references/Hexalith.Builds{ZWSP}` 53d53ae -> b529b66",
            f"-{NBSP}`references/Hexalith.Builds` 53d53ae -> b529b66",
        ):
            with self.subTest(raw=repr(raw)):
                cleaned = guard.normalize_line(raw).replace("`", " ").replace("|", " ")
                tokens = guard._path_tokens(cleaned)
                self.assertEqual(len(tokens), 1)
                offset, path = tokens[0]
                self.assertEqual(cleaned.index(path), offset)


class GuardCommandIntegrationTests(unittest.TestCase):
    """Runs the production CLI against real gitlink changes in an isolated repository."""

    def setUp(self):
        self._temporary_directory = tempfile.TemporaryDirectory()
        self.repo = pathlib.Path(self._temporary_directory.name)
        (self.repo / "scripts").mkdir()
        (self.repo / "references").mkdir()
        shutil.copy2(_SCRIPT, self.repo / "scripts" / _SCRIPT.name)

        self._git("init", "--quiet")
        self._git("config", "user.email", "story-guard-tests@example.invalid")
        self._git("config", "user.name", "Story Guard Tests")
        (self.repo / "seed.txt").write_text("seed\n", encoding="utf-8")
        self._git("add", "seed.txt")
        self._git("commit", "--quiet", "-m", "seed")
        seed = self._git("rev-parse", "HEAD").stdout.strip()

        self._git(
            "update-index",
            "--add",
            "--cacheinfo",
            f"160000,{seed},references/Dependency",
        )
        self._git("commit", "--quiet", "-m", "add dependency pointer")
        self.baseline = self._git("rev-parse", "HEAD").stdout.strip()

        (self.repo / "seed.txt").write_text("next\n", encoding="utf-8")
        self._git("add", "seed.txt")
        self._git("commit", "--quiet", "-m", "create target commit")
        self.target = self._git("rev-parse", "HEAD").stdout.strip()
        self._git(
            "update-index",
            "--cacheinfo",
            f"160000,{self.target},references/Dependency",
        )
        self._git("commit", "--quiet", "-m", "move dependency pointer")

    def tearDown(self):
        self._temporary_directory.cleanup()

    def test_matching_declared_pointer_exits_zero_with_pass_verdict(self):
        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline[:7]} -> {self.target[:7]}",
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("[declared] references/Dependency", result.stdout)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_undeclared_pointer_exits_one_with_fail_verdict(self):
        result = self._run_guard("- `src/Thing.cs`")

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNDECLARED] references/Dependency", result.stdout)
        self.assertIn("RESULT: FAIL", result.stdout)

    def test_misstated_pointer_exits_one_with_fail_verdict(self):
        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline[:7]} -> deadbee",
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[MISSTATED] references/Dependency", result.stdout)
        self.assertIn("RESULT: FAIL", result.stdout)

    def _git(self, *args):
        return subprocess.run(
            ["git", *args],
            cwd=self.repo,
            capture_output=True,
            text=True,
            check=True,
        )

    def _run_guard(self, file_list_entry, completion_entry=None):
        story = self.repo / "story.md"
        head = self._git("rev-parse", "HEAD").stdout.strip()
        completion = (
            "\n## Completion Notes List\n\n" + completion_entry + "\n"
            if completion_entry is not None
            else ""
        )
        story.write_text(
            "---\n"
            f"baseline_commit: {self.baseline}\n"
            "---\n\n"
            "## File List\n\n"
            f"{file_list_entry}\n"
            f"{completion}",
            encoding="utf-8",
        )
        return subprocess.run(
            [
                "python3",
                "scripts/validate-story-gitlinks.py",
                str(story),
                "--ref",
                head,
            ],
            cwd=self.repo,
            capture_output=True,
            text=True,
            check=False,
        )


if __name__ == "__main__":
    unittest.main()
