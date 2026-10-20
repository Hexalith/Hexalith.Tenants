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
import contextlib
import io
import pathlib
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

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

    def test_a_dependency_table_does_not_replace_an_explicit_declaration(self):
        story = "## File List\n| references/Hexalith.Builds | 1111111 | 2222222 |\n"
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

    def test_three_column_tables_bind_short_and_full_targets(self):
        for target in ("b529b66", "b529b66" + "a" * 33):
            for row in (
                f"| references/Hexalith.Builds | 53d53ae | {target} |",
                f"references/Hexalith.Builds | 53d53ae | {target}",
                f"| `references/Hexalith.Builds` | `53D53AE` | `{target.upper()}` |",
            ):
                with self.subTest(row=row):
                    self.assertEqual(self._file_list(row), {"references/Hexalith.Builds": target})

    def test_three_column_tables_accept_absent_baselines_and_zero_removal_targets(self):
        for base, target in (("absent", "b529b66"), ("53d53ae", "0" * 40)):
            with self.subTest(base=base, target=target):
                self.assertEqual(
                    self._file_list(f"| references/Hexalith.Builds | {base} | {target} |"),
                    {"references/Hexalith.Builds": target},
                )

    def test_three_column_tables_use_existing_unicode_normalization(self):
        row = f"|{NBSP}`references/Hexalith.Builds{ZWSP}` | `53d53ae` | `b52{ZWSP}9b66`{NBSP}|"
        self.assertEqual(self._file_list(row), {"references/Hexalith.Builds": "b529b66"})

    def test_the_last_claim_wins_across_arrow_and_table_forms(self):
        arrow = "- `references/Hexalith.Builds` 53d53ae -> aaaaaaa"
        table = "| references/Hexalith.Builds | 53d53ae | b529b66 |"
        for body, target in (
            (f"{arrow}\n{table}", "b529b66"),
            (f"{table}\n{arrow}", "aaaaaaa"),
            (f"{table}\n| references/Hexalith.Builds | 53d53ae | deadbee |", "deadbee"),
        ):
            with self.subTest(body=body):
                self.assertEqual(self._file_list(body), {"references/Hexalith.Builds": target})

    def test_table_rows_bind_each_dependency_without_crossing_cells(self):
        body = (
            "| Dependency | Baseline | Target |\n| --- | --- | --- |\n"
            "| references/Hexalith.Builds | 1111111 | 2222222 |\n"
            "| references/Hexalith.Memories | 3333333 | 4444444 |"
        )
        self.assertEqual(self._file_list(body), {
            "references/Hexalith.Builds": "2222222",
            "references/Hexalith.Memories": "4444444",
        })

    def test_table_claims_obey_existing_section_and_subheading_boundaries(self):
        story = (
            "## File List\n### Dependencies\n"
            "| references/Hexalith.Builds | 1111111 | 2222222 |\n"
            "## Completion Notes List\n"
            "| references/Hexalith.Builds | 2222222 | 3333333 |\n"
            "### Review Findings\n"
            "| references/Hexalith.Builds | 3333333 | deadbee |\n"
            "## Historical Evidence\n"
            "| references/Hexalith.Builds | 3333333 | aaaaaaa |\n"
        )
        self.assertEqual(guard.stated_targets(story), {"references/Hexalith.Builds": "3333333"})

    def test_other_table_shapes_and_prose_do_not_create_table_claims(self):
        for row in (
            "| Dependency | Baseline | Target |",
            "| --- | --- | --- |",
            "| references/Hexalith.Builds | 2222222 |",
            "| references/Hexalith.Builds | 1111111 | 2222222 | explanation |",
            "| references/Hexalith.Builds was untouched | 1111111 | 2222222 |",
            "| src/Thing.cs | 1111111 | 2222222 |",
            "| references/Hexalith.Builds | unknown | 2222222 |",
            "| references/Hexalith.Builds | 1111111 | 222222 |",
            "| references/Hexalith.Builds | 1111111 | " + "2" * 41 + " |",
            "| references/Hexalith.Builds | 1111111 | not-a-sha |",
        ):
            with self.subTest(row=row):
                self.assertEqual(self._file_list(row), {})

    def test_existing_arrow_tables_remain_binding(self):
        row = "| references/Hexalith.Builds | 1111111 -> 2222222 | reviewed |"
        self.assertEqual(self._file_list(row), {"references/Hexalith.Builds": "2222222"})

    def test_code_examples_do_not_override_live_table_claims(self):
        live = "| references/Hexalith.Builds | 1111111 | deadbee |"
        example = "references/Hexalith.Builds | 1111111 | 2222222"
        for code in (
            f"```markdown\n{example}\n```",
            f"~~~markdown\n{example}\n~~~",
            f"````markdown\n```\n{example}\n````",
            f"~~~markdown\n```\n{example}\n~~~",
            f"    {example}",
            f"\t{example}",
            f" \t{example}",
            f"`{example}`",
        ):
            with self.subTest(code=code):
                self.assertEqual(self._file_list(f"{live}\n{code}"), {"references/Hexalith.Builds": "deadbee"})

    def test_table_claims_resume_after_a_matching_fence_closes(self):
        for opening, closing in (("```markdown", "```"), ("~~~markdown", "~~~~")):
            with self.subTest(opening=opening):
                body = (
                    f"{opening}\n| references/Hexalith.Builds | 1111111 | deadbee |\n{closing}\n"
                    "| references/Hexalith.Builds | 1111111 | 2222222 |"
                )
                self.assertEqual(self._file_list(body), {"references/Hexalith.Builds": "2222222"})


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


class BaselineStoryInspectionTests(unittest.TestCase):
    def test_inspection_failure_cannot_establish_a_safe_baseline(self):
        with patch.object(guard, "run_git", side_effect=guard.CheckError("inspection failed")):
            with self.assertRaisesRegex(guard.CheckError, "inspection failed"):
                guard.baseline_is_mid_story("a" * 40, "story")


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
        self.pointer_commit = self._git("rev-parse", "HEAD").stdout.strip()

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

    def test_legacy_mid_story_baseline_rejects_hidden_earlier_pointer_movement(self):
        for artifact in ("story.md", "spec-story-2.md"):
            self._commit_story_baseline(artifact)
            self.assertEqual(self._git("diff", self.baseline, "HEAD", "--", "references/").stdout, "")
            self._assert_mid_story_refusal()

    def test_legacy_mid_story_baseline_rejects_pointer_movement_in_the_baseline_commit(self):
        self._git("update-index", "--cacheinfo", f"160000,{self.baseline},references/Dependency")
        self.target = self.baseline
        self._commit_story_baseline("story.md")
        self.assertIn("references/Dependency", self._git("diff", f"{self.baseline}^", self.baseline,
                                                        "--name-only", "--", "references/").stdout)
        self._assert_mid_story_refusal()

    def test_correcting_a_legacy_mid_story_baseline_restores_the_complete_pointer_audit(self):
        true_start = self.baseline
        self._commit_story_baseline("spec-story-2.md")
        self._assert_mid_story_refusal()
        self.baseline = true_start
        for include_worktree in (False, True):
            with self.subTest(include_worktree=include_worktree):
                result = self._run_guard("- `references/Dependency`",
                                         f"- `references/Dependency` {true_start} -> {self.target}",
                                         include_worktree=include_worktree)
                self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
                self.assertIn("[declared] references/Dependency", result.stdout)
                self.assertIn("RESULT: PASS", result.stdout)
                self.assertNotIn("mid-story baseline", result.stdout)
                undeclared = self._run_guard("- `src/Thing.cs`", include_worktree=include_worktree)
                self.assertEqual(undeclared.returncode, guard.EXIT_FAIL, undeclared.stdout + undeclared.stderr)
                self.assertIn("[UNDECLARED] references/Dependency", undeclared.stdout)

    def test_unrelated_baseline_artifacts_do_not_block_a_valid_legacy_audit(self):
        self._commit_story_baseline("unrelated.md")
        for include_worktree in (False, True):
            with self.subTest(include_worktree=include_worktree):
                result = self._run_guard("- `src/Thing.cs`", include_worktree=include_worktree)
                self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
                self.assertIn("No references/ pointer changes in range.", result.stdout)
                self.assertIn("RESULT: PASS", result.stdout)
                self.assertNotIn("mid-story baseline", result.stdout)

    def test_baseline_inspection_error_produces_a_failed_command_verdict(self):
        self._run_guard("- `references/Dependency`")
        story = self.repo / "story.md"
        actual_git = guard.run_git

        def inspect_or_fail(*args):
            if args[0] == "show":
                raise guard.CheckError("git show failed: injected baseline inspection error")
            return actual_git(*args)

        output = io.StringIO()
        with patch.object(guard, "REPO_ROOT", self.repo), \
             patch.object(guard, "run_git", side_effect=inspect_or_fail), \
             patch.object(sys, "argv", [str(_SCRIPT), str(story)]), \
             contextlib.redirect_stdout(output):
            result = guard.main()
        self.assertEqual(result, guard.EXIT_FAIL, output.getvalue())
        self.assertIn("RESULT: FAIL", output.getvalue())
        self.assertIn("git show failed: injected baseline inspection error", output.getvalue())
        self.assertNotIn("RESULT: PASS", output.getvalue())

    def test_recorded_provenance_keeps_its_mid_story_warning_and_owned_commit_checks(self):
        self._commit_story_baseline("spec-story-2.md")
        self.target = self.pointer_commit
        self._commit_pointer("references/Dependency", self.target)
        owned = self._git("rev-parse", "HEAD").stdout.strip()
        for include_worktree in (False, True):
            with self.subTest(include_worktree=include_worktree):
                result = self._run_guard("- `references/Dependency`",
                                         f"- `references/Dependency` {self.baseline} -> {self.target}",
                                         owned_commits=[owned], include_worktree=include_worktree)
                self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
                self.assertIn("WARNING:", result.stdout)
                self.assertIn("mid-story baseline", result.stdout)
                self.assertIn("RESULT: PASS", result.stdout)
                undeclared = self._run_guard("- `src/Thing.cs`", owned_commits=[owned],
                                             include_worktree=include_worktree)
                self.assertEqual(undeclared.returncode, guard.EXIT_FAIL, undeclared.stdout + undeclared.stderr)
                self.assertIn("[UNDECLARED] references/Dependency", undeclared.stdout)

    def test_mid_story_recorded_provenance_retains_target_endpoint_worktree_and_chain_refusals(self):
        self._commit_story_baseline("spec-story-2.md")
        self.target = self.pointer_commit
        self._commit_pointer("references/Dependency", self.target)
        owned = self._git("rev-parse", "HEAD").stdout.strip()
        declaration = "- `references/Dependency`"
        claim = f"- `references/Dependency` {self.baseline} -> {self.target}"
        stale = self._run_guard(declaration, "- `references/Dependency` abcdef0 -> deadbee",
                                owned_commits=[owned])
        self.assertEqual(stale.returncode, guard.EXIT_FAIL, stale.stdout + stale.stderr)
        self.assertIn("[MISSTATED]", stale.stdout)

        self._commit_pointer("references/Dependency", self.baseline)
        endpoint = self._run_guard(declaration, claim, owned_commits=[owned])
        self.assertEqual(endpoint.returncode, guard.EXIT_FAIL, endpoint.stdout + endpoint.stderr)
        self.assertIn("[UNRECORDED ENDPOINT]", endpoint.stdout)
        historical = self._run_guard(declaration, claim, owned_commits=[owned], ref=owned)
        self.assertEqual(historical.returncode, guard.EXIT_PASS, historical.stdout + historical.stderr)
        self.assertIn("RESULT: PASS", historical.stdout)

        self._commit_pointer("references/Dependency", self.target)
        self._git("update-index", "--cacheinfo", f"160000,{self.baseline},references/Dependency")
        worktree = self._run_guard(declaration, claim, owned_commits=[owned], include_worktree=True)
        self.assertEqual(worktree.returncode, guard.EXIT_FAIL, worktree.stdout + worktree.stderr)
        self.assertIn("[WORKTREE MOVE]", worktree.stdout)

        self._git("commit", "--quiet", "-m", "test: add unrecorded intermediate pointer")
        self._commit_pointer("references/Dependency", self.target)
        final = self._git("rev-parse", "HEAD").stdout.strip()
        gap = self._run_guard(declaration, claim, owned_commits=[owned, final])
        self.assertEqual(gap.returncode, guard.EXIT_FAIL, gap.stdout + gap.stderr)
        self.assertIn("[CHAIN GAP]", gap.stdout)

    def _commit_story_baseline(self, artifact):
        (self.repo / "references/Dependency").mkdir(exist_ok=True)
        path = self.repo / "_bmad-output/implementation-artifacts" / artifact
        self.baseline_artifact = path.relative_to(self.repo).as_posix()
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("story baseline evidence\n", encoding="utf-8")
        self._git("add", str(path))
        self._git("commit", "--quiet", "-m", "test: add baseline artifact")
        self.baseline = self._git("rev-parse", "HEAD").stdout.strip()

    def _assert_mid_story_refusal(self):
        endpoint = self.repo / "audit-endpoint.txt"
        endpoint.write_text(self.baseline + "\n", encoding="utf-8")
        self._git("add", str(endpoint))
        self._git("commit", "--quiet", "-m", "test: advance audit endpoint")
        self.assertNotEqual(self.baseline, self._git("rev-parse", "HEAD").stdout.strip())
        for declared in (False, True):
            for ref in (None, "HEAD", self.baseline):
                with self.subTest(baseline=self.baseline, declared=declared, ref=ref):
                    result = self._run_guard(
                        "- `references/Dependency`" if declared else "- `src/Thing.cs`",
                        f"- `references/Dependency` {self.baseline} -> {self.target}" if declared else None,
                        include_worktree=ref is None, ref=ref,
                    )
                    self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                    self.assertIn("RESULT: FAIL", result.stdout)
                    self.assertIn("mid-story baseline", result.stdout)
                    self.assertIn("Filename-stem heuristic matched `story`", result.stdout)
                    self.assertIn(self.baseline_artifact, result.stdout)
                    self.assertIn("true start", result.stdout)
                    self.assertNotIn("RESULT: PASS", result.stdout)

    def test_matching_table_targets_pass_in_both_provenance_modes(self):
        self._git("clone", "--quiet", "--no-checkout", str(self.repo), str(self.repo / "references/Dependency"))
        self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", self.target)
        for owned in (None, [self.pointer_commit]):
            for include_worktree in (False, True):
                with self.subTest(owned=owned, include_worktree=include_worktree):
                    result = self._run_guard(
                        "- `references/Dependency`",
                        f"| references/Dependency | {self.baseline} | {self.target} |",
                        owned_commits=owned,
                        include_worktree=include_worktree,
                    )
                    self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
                    self.assertIn("RESULT: PASS", result.stdout)
                    self.assertNotIn("[MISSTATED]", result.stdout)

    def test_stale_table_targets_fail_in_both_provenance_modes(self):
        self._git("clone", "--quiet", "--no-checkout", str(self.repo), str(self.repo / "references/Dependency"))
        self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", self.target)
        for owned in (None, [self.pointer_commit]):
            for include_worktree in (False, True):
                with self.subTest(owned=owned, include_worktree=include_worktree):
                    result = self._run_guard(
                        "- `references/Dependency`",
                        f"| references/Dependency | {self.baseline} | deadbee |",
                        owned_commits=owned,
                        include_worktree=include_worktree,
                    )
                    self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                    self.assertIn("[MISSTATED] references/Dependency", result.stdout)
                    self.assertIn(self.target, result.stdout)
                    self.assertIn("deadbee", result.stdout)
                    self.assertIn("RESULT: FAIL", result.stdout)

    def test_table_claims_cannot_declare_a_pointer_implicitly(self):
        for owned in (None, [self.pointer_commit]):
            with self.subTest(owned=owned):
                result = self._run_guard(
                    f"| references/Dependency | {self.baseline} | {self.target} |",
                    owned_commits=owned,
                )
                self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                self.assertIn("[UNDECLARED] references/Dependency", result.stdout)

    def test_explicit_commit_table_targets_are_checked_when_the_pointer_did_not_move(self):
        self.baseline = self.pointer_commit
        for target, verdict in ((self.target, guard.EXIT_PASS), ("deadbee", guard.EXIT_FAIL)):
            with self.subTest(target=target):
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"| references/Dependency | {self.target} | {target} |",
                )
                self.assertEqual(result.returncode, verdict, result.stdout + result.stderr)
                if verdict == guard.EXIT_FAIL:
                    self.assertIn("[MISSTATED] references/Dependency", result.stdout)

    def test_default_head_empty_checkout_checks_unchanged_claims_against_the_gitlink(self):
        self.baseline = self.pointer_commit
        (self.repo / "references/Dependency").mkdir()
        parent_head = self._git("rev-parse", "HEAD").stdout.strip()
        for form in ("table", "arrow"):
            for claim, verdict in ((self.target, guard.EXIT_PASS), ("deadbee", guard.EXIT_FAIL), (parent_head, guard.EXIT_FAIL)):
                with self.subTest(form=form, claim=claim):
                    entry = (
                        f"| references/Dependency | {self.target} | {claim} |"
                        if form == "table"
                        else f"- `references/Dependency` {self.target} -> {claim}"
                    )
                    result = self._run_guard("- `references/Dependency`", entry, include_worktree=True)
                    self.assertEqual(result.returncode, verdict, result.stdout + result.stderr)
                    self.assertIn("No references/ pointer changes in range.", result.stdout)
                    if verdict == guard.EXIT_FAIL:
                        self.assertIn(f"[MISSTATED] references/Dependency is {self.target} in the tree", result.stdout)
                    else:
                        self.assertIn("RESULT: PASS", result.stdout)

    def test_default_head_empty_checkout_uses_the_staged_pointer_when_the_net_diff_is_empty(self):
        staged = self._git("rev-parse", f"{self.baseline}:references/Dependency").stdout.strip()
        self._git("update-index", "--cacheinfo", f"160000,{staged},references/Dependency")
        (self.repo / "references/Dependency").mkdir()
        for claim, verdict in ((staged, guard.EXIT_PASS), (self.target, guard.EXIT_FAIL)):
            with self.subTest(claim=claim):
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"| references/Dependency | {staged} | {claim} |",
                    include_worktree=True,
                )
                self.assertEqual(result.returncode, verdict, result.stdout + result.stderr)
                self.assertIn("No references/ pointer changes in range.", result.stdout)
                if verdict == guard.EXIT_FAIL:
                    self.assertIn(f"[MISSTATED] references/Dependency is {staged} in the tree", result.stdout)

    def test_default_head_own_checkouts_preserve_matching_claims_and_movement_refusals(self):
        self.baseline = self.pointer_commit
        for gitfile in (False, True):
            dependency = self._clone_dependency(gitfile)
            self.assertEqual((dependency / ".git").is_file(), gitfile)
            try:
                for checkout, verdict in ((self.target, guard.EXIT_PASS), (self.baseline, guard.EXIT_FAIL)):
                    self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", checkout)
                    for include_worktree in (True, False):
                        with self.subTest(gitfile=gitfile, checkout=checkout, include_worktree=include_worktree):
                            result = self._run_guard(
                                "- `references/Dependency`",
                                f"| references/Dependency | {self.target} | {self.target} |",
                                include_worktree=include_worktree,
                            )
                            expected = verdict if include_worktree else guard.EXIT_PASS
                            self.assertEqual(result.returncode, expected, result.stdout + result.stderr)
                            if expected == guard.EXIT_FAIL:
                                self.assertIn("[MISSTATED] references/Dependency", result.stdout)
                                self.assertIn(f"{self.target[:7]} -> {checkout[:7]}", result.stdout)
                # Removing the checkout leaves a deinitialized directory. Git
                # must use the index rather than ascending to the parent HEAD.
                shutil.rmtree(dependency)
                dependency.mkdir()
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"- `references/Dependency` {self.target} -> {self.target}",
                    include_worktree=True,
                )
                self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
            finally:
                shutil.rmtree(dependency)

    def test_default_head_own_checkout_wins_over_the_index_when_it_returns_to_baseline(self):
        checkout = self._git("rev-parse", f"{self.baseline}:references/Dependency").stdout.strip()
        for gitfile in (False, True):
            dependency = self._clone_dependency(gitfile)
            try:
                self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", checkout)
                with patch.object(guard, "REPO_ROOT", self.repo):
                    self.assertEqual(guard.current_pointer("references/Dependency", None), checkout)
                for claim, verdict in ((checkout, guard.EXIT_PASS), (self.target, guard.EXIT_FAIL)):
                    with self.subTest(gitfile=gitfile, claim=claim):
                        result = self._run_guard(
                            "- `references/Dependency`",
                            f"| references/Dependency | {checkout} | {claim} |",
                            include_worktree=True,
                        )
                        self.assertEqual(result.returncode, verdict, result.stdout + result.stderr)
                        if verdict == guard.EXIT_FAIL:
                            self.assertIn(f"[MISSTATED] references/Dependency is {checkout} in the tree", result.stdout)
            finally:
                shutil.rmtree(dependency)
                if gitfile:
                    shutil.rmtree(self.repo / ".git/modules/Dependency")

    def test_default_head_missing_directory_still_reports_removal(self):
        self.baseline = self.pointer_commit
        result = self._run_guard(
            "- `references/Dependency`",
            f"| references/Dependency | {self.target} | {self.target} |",
            include_worktree=True,
        )
        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn(f"{self.target[:7]} -> removed", result.stdout)
        self.assertIn("[MISSTATED] references/Dependency", result.stdout)

    def test_claims_for_empty_or_ordinary_directories_require_an_actual_gitlink(self):
        self.baseline = self.pointer_commit
        (self.repo / "references/Dependency").mkdir()
        ordinary = self.repo / "references/Typo"
        ordinary.mkdir()
        for tracked in (False, True):
            if tracked:
                (ordinary / "file.txt").write_text("ordinary tracked content\n", encoding="utf-8")
                self._git("add", "references/Typo/file.txt")
                self._git("commit", "--quiet", "-m", "test: track ordinary fixture directory")
            for include_worktree in (True, False):
                for claim, verdict in (("deadbee", guard.EXIT_FAIL), ("0" * 40, guard.EXIT_PASS)):
                    with self.subTest(tracked=tracked, include_worktree=include_worktree, claim=claim):
                        result = self._run_guard(
                            "- `references/Typo`",
                            f"| references/Typo | absent | {claim} |",
                            include_worktree=include_worktree,
                        )
                        self.assertEqual(result.returncode, verdict, result.stdout + result.stderr)
                        if verdict == guard.EXIT_FAIL:
                            self.assertIn("[MISSTATED] references/Typo", result.stdout)

    def test_default_head_unmerged_gitlink_refuses_all_claims_instead_of_treating_it_as_absent(self):
        other = self._git("rev-parse", f"{self.baseline}:references/Dependency").stdout.strip()
        self.baseline = self.pointer_commit
        (self.repo / "references/Dependency").mkdir()
        self._git("update-index", "--force-remove", "references/Dependency")
        subprocess.run(
            ["git", "update-index", "--index-info"],
            input="".join(f"160000 {sha} {stage}\treferences/Dependency\n" for stage, sha in ((1, self.target), (2, self.target), (3, other))),
            cwd=self.repo,
            capture_output=True,
            text=True,
            check=True,
        )
        for initialized in (False, True):
            if initialized:
                (self.repo / "references/Dependency").rmdir()
                self._clone_dependency(False)
                self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", self.target)
            for claim in ("deadbee", "0" * 40, self.target):
                with self.subTest(initialized=initialized, claim=claim):
                    result = self._run_guard(
                        "- `references/Dependency`",
                        f"| references/Dependency | {self.target} | {claim} |",
                        include_worktree=True,
                    )
                    self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                    self.assertIn("Unmerged gitlink", result.stdout)
                    self.assertIn("RESULT: FAIL", result.stdout)
        explicit = self._run_guard(
            "- `references/Dependency`",
            f"| references/Dependency | {self.target} | {self.target} |",
        )
        self.assertEqual(explicit.returncode, guard.EXIT_PASS, explicit.stdout + explicit.stderr)

    def test_changed_unmerged_gitlink_refuses_stage_one_claims_in_both_provenance_modes(self):
        original_baseline = self.baseline
        stage_one = self._git("rev-parse", f"{original_baseline}:references/Dependency").stdout.strip()
        (self.repo / "references/Dependency").mkdir()
        self._git("update-index", "--force-remove", "references/Dependency")
        subprocess.run(
            ["git", "update-index", "--index-info"],
            input="".join(f"160000 {sha} {stage}\treferences/Dependency\n" for stage, sha in ((1, stage_one), (2, self.target), (3, self.pointer_commit))),
            cwd=self.repo,
            capture_output=True,
            text=True,
            check=True,
        )
        for owned in (None, [self.pointer_commit]):
            self.baseline = self.pointer_commit if owned is None else original_baseline
            with self.subTest(owned=owned):
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"| references/Dependency | {self.target} | {stage_one} |",
                    owned_commits=owned,
                    include_worktree=True,
                )
                self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                self.assertIn("Unmerged gitlink", result.stdout)
                explicit = self._run_guard(
                    "- `references/Dependency`",
                    f"| references/Dependency | {self.target} | {self.target} |",
                    owned_commits=owned,
                )
                self.assertEqual(explicit.returncode, guard.EXIT_PASS, explicit.stdout + explicit.stderr)

    def test_recorded_table_claims_still_reject_an_unchanged_unowned_pointer(self):
        self._prepare_other_baseline_pointer()
        other = self._git("rev-parse", f"{self.baseline}:references/Other").stdout.strip()
        result = self._run_guard(
            "- `references/Dependency`\n- `references/Other`",
            f"| references/Dependency | {self.baseline} | {self.target} |\n"
            f"| references/Other | {other} | {other} |",
            owned_commits=[self.pointer_commit],
        )
        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNOWNED CLAIM] references/Other", result.stdout)

    def test_code_examples_cannot_exonerate_a_stale_table(self):
        for owned in (None, [self.pointer_commit]):
            for example in (
                f"```markdown\n| references/Dependency | {self.baseline} | {self.target} |\n```",
                f"~~~markdown\n| references/Dependency | {self.baseline} | {self.target} |\n~~~",
                f"`references/Dependency | {self.baseline} | {self.target}`",
                f"    references/Dependency | {self.baseline} | {self.target}",
            ):
                with self.subTest(owned=owned, example=example):
                    result = self._run_guard(
                        "- `references/Dependency`",
                        f"| references/Dependency | {self.baseline} | deadbee |\n{example}",
                        owned_commits=owned,
                    )
                    self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                    self.assertIn("[MISSTATED] references/Dependency", result.stdout)
                    self.assertIn("deadbee", result.stdout)

    def test_recorded_addition_accepts_an_absent_table_baseline(self):
        self._commit_pointer("references/Added", self.target)
        addition = self._git("rev-parse", "HEAD").stdout.strip()
        result = self._run_guard(
            "- `references/Dependency`\n- `references/Added`",
            f"| references/Dependency | {self.baseline} | {self.target} |\n"
            f"| references/Added | absent | {self.target} |",
            owned_commits=[self.pointer_commit, addition],
        )
        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_removal_accepts_a_zero_table_target(self):
        self._git("update-index", "--force-remove", "references/Dependency")
        self._git("commit", "--quiet", "-m", "test: remove fixture dependency")
        removal = self._git("rev-parse", "HEAD").stdout.strip()
        result = self._run_guard(
            "- `references/Dependency`",
            f"| references/Dependency | {self.target} | {'0' * 40} |",
            owned_commits=[self.pointer_commit, removal],
        )
        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_addition_rejects_a_stale_target_after_an_absent_baseline(self):
        self._commit_pointer("references/Added", self.target)
        addition = self._git("rev-parse", "HEAD").stdout.strip()
        result = self._run_guard(
            "- `references/Dependency`\n- `references/Added`",
            f"| references/Dependency | {self.baseline} | {self.target} |\n"
            "| references/Added | absent | deadbee |",
            owned_commits=[self.pointer_commit, addition],
        )
        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[MISSTATED] references/Added", result.stdout)

    def test_zero_table_target_is_rejected_while_the_pointer_remains_present(self):
        for owned in (None, [self.pointer_commit]):
            with self.subTest(owned=owned):
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"| references/Dependency | {self.baseline} | {'0' * 40} |",
                    owned_commits=owned,
                )
                self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                self.assertIn("[MISSTATED] references/Dependency", result.stdout)

    def test_nonzero_table_target_is_rejected_after_recorded_removal(self):
        self._git("update-index", "--force-remove", "references/Dependency")
        self._git("commit", "--quiet", "-m", "test: remove fixture dependency")
        removal = self._git("rev-parse", "HEAD").stdout.strip()
        result = self._run_guard(
            "- `references/Dependency`",
            f"| references/Dependency | {self.target} | {self.target} |",
            owned_commits=[self.pointer_commit, removal],
        )
        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[MISSTATED] references/Dependency", result.stdout)

    def test_recorded_story_commit_rejects_unrecorded_later_pointer_update(self):
        (self.repo / "seed.txt").write_text("later\n", encoding="utf-8")
        self._git("add", "seed.txt")
        self._git("commit", "--quiet", "-m", "later dependency target")
        later_target = self._git("rev-parse", "HEAD").stdout.strip()
        self._git("update-index", "--cacheinfo", f"160000,{later_target},references/Dependency")
        self._git("commit", "--quiet", "-m", "later unrelated pointer refresh")

        for include_worktree in (False, True):
            with self.subTest(include_worktree=include_worktree):
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"- `references/Dependency` {self.baseline} -> {self.target}",
                    owned_commits=[self.pointer_commit],
                    include_worktree=include_worktree,
                )

                self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                self.assertIn(self.pointer_commit, result.stdout)
                self.assertIn("[UNRECORDED ENDPOINT] references/Dependency", result.stdout)
                self.assertIn(self.target, result.stdout)
                self.assertIn(later_target, result.stdout)
                self.assertIn("RESULT: FAIL", result.stdout)
                self.assertIn("use --ref <story-endpoint>", result.stdout)

        historical = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
            ref=self.pointer_commit,
        )
        self.assertEqual(historical.returncode, guard.EXIT_PASS, historical.stdout + historical.stderr)
        self.assertIn("RESULT: PASS", historical.stdout)

    def test_recorded_story_commit_passes_matching_endpoint(self):
        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commit_rejects_pointer_reverted_to_baseline(self):
        original = self._git("rev-parse", f"{self.baseline}:references/Dependency").stdout.strip()
        self._commit_pointer("references/Dependency", original)
        self.assertEqual(self._git("diff", self.baseline, "HEAD", "--", "references/").stdout, "")

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {original} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNRECORDED ENDPOINT] references/Dependency", result.stdout)
        self.assertIn(original, result.stdout)
        self.assertIn(self.target, result.stdout)

    def test_recorded_story_commit_rejects_unrecorded_removal(self):
        self._git("update-index", "--force-remove", "references/Dependency")
        self._git("commit", "--quiet", "-m", "test: remove fixture dependency pointer")

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNRECORDED ENDPOINT] references/Dependency", result.stdout)
        self.assertIn("absent", result.stdout)

    def test_recorded_story_commit_rejects_new_unowned_path_even_when_declared(self):
        self._commit_pointer("references/Other", self.target)

        result = self._run_guard(
            "- `references/Dependency`\n- `references/Other`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNRECORDED ENDPOINT] references/Other", result.stdout)
        self.assertIn("absent", result.stdout)

    def test_recorded_story_commit_passes_after_unrecorded_move_is_reverted(self):
        self._commit_pointer("references/Dependency", self.baseline)
        self._commit_pointer("references/Dependency", self.target)

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commit_passes_later_commit_without_pointer_changes(self):
        (self.repo / "seed.txt").write_text("unrelated change\n", encoding="utf-8")
        self._git("add", "seed.txt")
        self._git("commit", "--quiet", "-m", "test: change non-pointer fixture file")

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)

    def test_recorded_story_commit_rejects_change_to_existing_unowned_path(self):
        self._prepare_other_baseline_pointer()
        original_other = self._git("rev-parse", f"{self.baseline}:references/Other").stdout.strip()
        self._commit_pointer("references/Other", self.target)

        result = self._run_guard(
            "- `references/Dependency`\n- `references/Other`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNRECORDED ENDPOINT] references/Other", result.stdout)
        self.assertIn(f"expected {original_other}", result.stdout)
        self.assertIn(f"records {self.target}", result.stdout)

    def test_recorded_story_commit_passes_unchanged_existing_unowned_path(self):
        self._prepare_other_baseline_pointer()

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commit_rejects_removal_of_existing_unowned_path(self):
        self._prepare_other_baseline_pointer()
        self._git("update-index", "--force-remove", "references/Other")
        self._git("commit", "--quiet", "-m", "test: remove unowned fixture dependency")

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNRECORDED ENDPOINT] references/Other", result.stdout)

    def test_recorded_story_commit_rejects_unrecorded_gitlink_rename(self):
        self._git("update-index", "--force-remove", "references/Dependency")
        self._commit_pointer("references/Renamed", self.target)

        result = self._run_guard(
            "- `references/Dependency`\n- `references/Renamed`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[UNRECORDED ENDPOINT] references/Renamed", result.stdout)
        self.assertIn("[UNRECORDED ENDPOINT] references/Dependency", result.stdout)

    def test_recorded_story_commits_pass_recorded_addition(self):
        self._commit_pointer("references/Other", self.target)
        addition_commit = self._git("rev-parse", "HEAD").stdout.strip()

        result = self._run_guard(
            "- `references/Dependency`\n- `references/Other`",
            f"- `references/Dependency` {self.baseline} -> {self.target}\n"
            f"- `references/Other` {'0' * 40} -> {self.target}",
            owned_commits=[self.pointer_commit, addition_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commits_pass_recorded_rename(self):
        self._git("update-index", "--force-remove", "references/Dependency")
        self._commit_pointer("references/Renamed", self.target)
        rename_commit = self._git("rev-parse", "HEAD").stdout.strip()

        result = self._run_guard(
            "- `references/Dependency`\n- `references/Renamed`",
            f"- `references/Dependency` {self.target} -> {'0' * 40}\n"
            f"- `references/Renamed` {'0' * 40} -> {self.target}",
            owned_commits=[self.pointer_commit, rename_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commits_pass_complete_multi_commit_chain(self):
        self._commit_pointer("references/Dependency", self.baseline)
        second_commit = self._git("rev-parse", "HEAD").stdout.strip()

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.target} -> {self.baseline}",
            owned_commits=[self.pointer_commit, second_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commits_still_reject_missing_intermediate_commit(self):
        self._commit_pointer("references/Dependency", self.baseline)
        self._commit_pointer("references/Dependency", self.pointer_commit)
        last_commit = self._git("rev-parse", "HEAD").stdout.strip()

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.target} -> {self.pointer_commit}",
            owned_commits=[self.pointer_commit, last_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[CHAIN GAP]", result.stdout)
        self.assertNotIn("use --ref <story-endpoint>", result.stdout)

    def test_recorded_story_commits_pass_recorded_removal(self):
        self._git("update-index", "--force-remove", "references/Dependency")
        self._git("commit", "--quiet", "-m", "test: record fixture dependency removal")
        removal_commit = self._git("rev-parse", "HEAD").stdout.strip()

        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.target} -> {'0' * 40}",
            owned_commits=[self.pointer_commit, removal_commit],
        )

        self.assertEqual(result.returncode, guard.EXIT_PASS, result.stdout + result.stderr)
        self.assertIn("RESULT: PASS", result.stdout)

    def test_recorded_story_commit_rejects_misstated_or_undeclared_pointer(self):
        for file_list, completion, expected in (
            ("- `references/Dependency`", "- `references/Dependency` deadbee -> deadbee", "[MISSTATED]"),
            ("- `src/Thing.cs`", None, "[UNDECLARED]"),
        ):
            with self.subTest(expected=expected):
                result = self._run_guard(
                    file_list,
                    completion,
                    owned_commits=[self.pointer_commit],
                )
                self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
                self.assertIn(expected, result.stdout)
                self.assertNotIn("use --ref <story-endpoint>", result.stdout)

    def test_recorded_story_commit_rejects_missing_and_short_commit_ids(self):
        for commit_ids in ([], [self.pointer_commit[:7]]):
            with self.subTest(commit_ids=commit_ids):
                result = self._run_guard(
                    "- `references/Dependency`",
                    f"- `references/Dependency` {self.baseline} -> {self.target}",
                    owned_commits=commit_ids,
                )
                self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)

    def test_recorded_story_commit_rejects_new_worktree_pointer_movement(self):
        self._git("update-index", "--cacheinfo", f"160000,{self.baseline},references/Dependency")
        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
            include_worktree=True,
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[WORKTREE MOVE]", result.stdout)
        self.assertNotIn("use --ref <story-endpoint>", result.stdout)

    def test_recorded_story_commit_rejects_unstaged_submodule_checkout_movement(self):
        dependency = self.repo / "references/Dependency"
        self._git("clone", "--quiet", "--no-checkout", str(self.repo), str(dependency))
        self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", self.target)

        unchanged = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
            include_worktree=True,
        )
        self.assertEqual(unchanged.returncode, guard.EXIT_PASS, unchanged.stdout + unchanged.stderr)

        self._git("-C", "references/Dependency", "checkout", "--quiet", "--detach", self.baseline)
        self.assertEqual(self._git("diff", "--cached", "--raw").stdout, "")
        result = self._run_guard(
            "- `references/Dependency`",
            f"- `references/Dependency` {self.baseline} -> {self.target}",
            owned_commits=[self.pointer_commit],
            include_worktree=True,
        )

        self.assertEqual(result.returncode, guard.EXIT_FAIL, result.stdout + result.stderr)
        self.assertIn("[WORKTREE MOVE] references/Dependency", result.stdout)
        self.assertIn(f"{self.target[:7]} -> {self.baseline[:7]}", result.stdout)
        self.assertNotIn("use --ref <story-endpoint>", result.stdout)

    def _clone_dependency(self, gitfile):
        dependency = self.repo / "references/Dependency"
        options = []
        if gitfile:
            git_directory = self.repo / ".git/modules/Dependency"
            git_directory.parent.mkdir(parents=True, exist_ok=True)
            options = ["--separate-git-dir", str(git_directory)]
        self._git("clone", "--quiet", "--no-checkout", *options, str(self.repo), str(dependency))
        return dependency

    def _git(self, *args):
        return subprocess.run(
            ["git", *args],
            cwd=self.repo,
            capture_output=True,
            text=True,
            check=True,
        )

    def _commit_pointer(self, path, target):
        self._git("update-index", "--add", "--cacheinfo", f"160000,{target},{path}")
        self._git("commit", "--quiet", "-m", "test: change fixture dependency pointer")

    def _prepare_other_baseline_pointer(self):
        self._commit_pointer("references/Other", self.baseline)
        self.baseline = self._git("rev-parse", "HEAD").stdout.strip()
        self.target = self.pointer_commit
        self._commit_pointer("references/Dependency", self.target)
        self.pointer_commit = self._git("rev-parse", "HEAD").stdout.strip()

    def _run_guard(self, file_list_entry, completion_entry=None, owned_commits=None, include_worktree=False, ref=None):
        story = self.repo / "story.md"
        head = ref or self._git("rev-parse", "HEAD").stdout.strip()
        completion = (
            "\n## Completion Notes List\n\n" + completion_entry + "\n"
            if completion_entry is not None
            else ""
        )
        provenance = (
            "\n## Story-Owned Gitlink Commits\n\n"
            + "".join(f"- `{commit}`\n" for commit in owned_commits)
            if owned_commits is not None
            else ""
        )
        story.write_text(
            "---\n"
            f"baseline_commit: {self.baseline}\n"
            "---\n\n"
            "## File List\n\n"
            f"{file_list_entry}\n"
            f"{completion}{provenance}",
            encoding="utf-8",
        )
        return subprocess.run(
            [
                "python3",
                "scripts/validate-story-gitlinks.py",
                str(story),
                *([] if include_worktree else ["--ref", head]),
            ],
            cwd=self.repo,
            capture_output=True,
            text=True,
            check=False,
        )


if __name__ == "__main__":
    unittest.main()
