"""Exercise the actual CI runner and production guard in disposable Git histories.

Run: python3 tests/scripts/test_validate_changed_story_gitlinks.py
"""

import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
ARTIFACTS = Path("_bmad-output/implementation-artifacts")
STORY = ARTIFACTS / "spec-7-1-ci-story.md"
DEPENDENCY = "references/Dependency"
OLD = "1" * 40
NEW = "2" * 40


class ChangedStoryGitlinkTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="changed-story-tests-")
        self.addCleanup(self.directory.cleanup)
        self.repo = Path(self.directory.name) / "repo"
        self.repo.mkdir()
        self.git("init", "--initial-branch=main")
        self.git("config", "user.name", "Fixture")
        self.git("config", "user.email", "fixture@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.hooksPath", "/dev/null")
        (self.repo / "scripts").mkdir()
        for name in ["validate-changed-story-gitlinks.py", "validate-story-gitlinks.py"]:
            shutil.copyfile(ROOT / "scripts" / name, self.repo / "scripts" / name)
        self.pointer(OLD)
        self.base = self.commit()

    def git(self, *args):
        result = subprocess.run(
            ["git", *args], cwd=self.repo, text=True, capture_output=True, check=False,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def write(self, path, text):
        destination = self.repo / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(text, encoding="utf-8")

    def pointer(self, oid):
        self.git("update-index", "--add", "--cacheinfo", f"160000,{oid},{DEPENDENCY}")

    def commit(self):
        # Do not stage a deinitialized gitlink's missing directory as a deletion.
        self.git("add", "--", "scripts")
        if (self.repo / ARTIFACTS).exists():
            self.git("add", "--", str(ARTIFACTS))
        self.git("commit", "--allow-empty", "-m", "test: Create fixture commit")
        return self.git("rev-parse", "HEAD")

    def story(self, declared=False, extra="", baseline=None):
        declaration = (
            f"- `{DEPENDENCY}`\n\n| Dependency | Before | After |\n"
            f"| --- | --- | --- |\n| `{DEPENDENCY}` | `{OLD}` | `{NEW}` |\n"
        ) if declared else ""
        return (
            f"---\nstatus: done\nbaseline_commit: '{baseline or self.base}'\n{extra}---\n"
            f"\n## File List\n{declaration}"
        )

    def run_event(self, before=None, after=None, event_name="push", event=None, repo=None):
        target = repo or self.repo
        if event is None:
            event = {"before": before or self.base, "after": after or self.git("rev-parse", "HEAD")}
        event_file = Path(self.directory.name) / "event.json"
        event_file.write_text(json.dumps(event), encoding="utf-8")
        return subprocess.run(
            [sys.executable, str(target / "scripts/validate-changed-story-gitlinks.py"),
             "--event-path", str(event_file), "--event-name", event_name],
            cwd=target, text=True, capture_output=True, check=False,
        )

    def assert_pass(self, result, count=1):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn(f"Changed stories: {count}; failed: 0", result.stdout)

    def assert_fail(self, result, diagnostic):
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn(diagnostic, result.stdout + result.stderr)

    def historical_story(self):
        self.write(STORY, self.story())
        endpoint = self.commit()
        self.pointer(NEW)
        before = self.commit()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{endpoint}'\n") + "\nHistorical repair.\n")
        return endpoint, before

    def test_new_story_without_pointer_changes_passes(self):
        self.write(STORY, self.story())
        after = self.commit()
        result = self.run_event(after=after)
        self.assert_pass(result)
        self.assertIn(f"Compared: {after}", result.stdout)
        self.assertNotIn("+ working tree", result.stdout)

    def test_declared_pointer_change_passes(self):
        self.write(STORY, self.story(declared=True))
        self.pointer(NEW)
        self.commit()
        self.assert_pass(self.run_event())

    def test_undeclared_pointer_change_fails(self):
        self.write(STORY, self.story())
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(), "UNDECLARED")

    def test_incorrect_declared_target_fails(self):
        self.write(STORY, self.story(declared=True).replace(f"`{NEW}`", f"`{'3' * 40}`"))
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(), "MISSTATED")

    def test_later_pointer_commit_in_same_push_is_checked(self):
        self.write(STORY, self.story())
        self.commit()
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(), "UNDECLARED")

    def test_replacement_baseline_cannot_hide_an_event_pointer_commit(self):
        self.write(STORY, self.story())
        before = self.commit()
        self.pointer(NEW)
        bump = self.commit()
        self.write(STORY, self.story(baseline=bump))
        self.commit()
        self.assert_fail(self.run_event(before=before), "not an ancestor")

    def test_replacement_baseline_cannot_narrow_existing_pre_event_history(self):
        self.write(STORY, self.story())
        self.commit()
        self.pointer(NEW)
        before = self.commit()
        self.write(STORY, self.story(baseline=before))
        self.commit()
        self.assert_fail(self.run_event(before=before), "not an ancestor")

    def test_new_story_baseline_cannot_exclude_earlier_event_pointer_work(self):
        self.pointer(NEW)
        bump = self.commit()
        self.write(STORY, self.story(baseline=bump))
        self.commit()
        self.assert_fail(self.run_event(), "not an ancestor")

    def test_broadening_an_existing_baseline_is_allowed(self):
        self.write(STORY, self.story())
        middle = self.commit()
        self.write(STORY, self.story(baseline=middle))
        before = self.commit()
        self.write(STORY, self.story())
        self.commit()
        self.assert_pass(self.run_event(before=before))

    def test_story_restored_before_event_head_is_still_checked(self):
        original = self.story()
        self.write(STORY, original)
        before = self.commit()
        self.write(STORY, original + "\nImplementation.\n")
        self.pointer(NEW)
        self.commit()
        self.write(STORY, original)
        self.commit()
        self.assert_fail(self.run_event(before=before), "UNDECLARED")

    def test_story_created_then_deleted_in_event_cannot_disappear(self):
        self.write(STORY, self.story())
        self.pointer(NEW)
        self.commit()
        self.git("rm", str(STORY))
        self.commit()
        self.assert_fail(self.run_event(), "Removed story")

    def test_unchanged_old_story_is_not_selected(self):
        self.write(STORY, self.story())
        before = self.commit()
        self.pointer(NEW)
        self.write(ARTIFACTS / "dw-evidence.md", "# Evidence\n")
        self.commit()
        self.assert_pass(self.run_event(before=before), count=0)

    def test_all_selected_stories_are_checked_after_failure(self):
        self.write(STORY, self.story())
        self.write(ARTIFACTS / "spec-8-1-other.md", self.story(declared=True))
        self.pointer(NEW)
        self.commit()
        result = self.run_event()
        self.assert_fail(result, "Changed stories: 2; failed: 1")
        self.assertIn("RESULT: PASS", result.stdout)

    def test_missing_baseline_fails_for_named_spec(self):
        self.write(STORY, "---\nstatus: done\n---\n")
        self.commit()
        self.assert_fail(self.run_event(), "no `baseline_commit`")

    def test_removing_baseline_from_nonstandard_story_does_not_skip_it(self):
        path = ARTIFACTS / "custom-delivery.md"
        self.write(path, self.story())
        before = self.commit()
        self.write(path, "---\nstatus: done\n---\n")
        self.commit()
        self.assert_fail(self.run_event(before=before), "no `baseline_commit`")

    def test_numeric_story_and_paths_with_spaces_are_checked(self):
        self.write(ARTIFACTS / "7-2-story with spaces.md", self.story())
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(), "UNDECLARED")

    def test_control_characters_and_pathspec_characters_are_literal(self):
        for name in ["spec-name\r.md", "spec-name\n.md", "spec-name[1]*?.md"]:
            with self.subTest(name=repr(name)):
                self.write(ARTIFACTS / name, self.story())
        self.pointer(NEW)
        self.commit()
        result = self.run_event()
        self.assert_fail(result, "Changed stories: 3; failed: 3")
        self.assertEqual(result.stdout.count("[UNDECLARED]"), 3)

    def test_rename_preserves_selection(self):
        self.write(STORY, self.story())
        before = self.commit()
        self.git("mv", str(STORY), str(ARTIFACTS / "renamed with spaces.md"))
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(before=before), "UNDECLARED")

    def test_rename_preserves_original_mid_story_baseline_check(self):
        self.write(STORY, self.story())
        self.pointer(NEW)
        middle = self.commit()
        self.write(STORY, self.story(baseline=middle))
        before = self.commit()
        self.git("mv", str(STORY), str(ARTIFACTS / "renamed.md"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "mid-story baseline")

    def test_control_character_identity_preserves_mid_story_baseline_check(self):
        path = ARTIFACTS / "spec-original\r.md"
        self.write(path, self.story())
        self.pointer(NEW)
        middle = self.commit()
        self.write(path, self.story(baseline=middle))
        before = self.commit()
        self.write(path, self.story(baseline=middle) + "\nCorrection.\n")
        self.commit()
        self.assert_fail(self.run_event(before=before), "mid-story baseline")

    def test_ambiguous_rewritten_rename_cannot_discard_story_identity(self):
        self.write(STORY, self.story() + "Original story.\n" * 30)
        before = self.commit()
        self.git("rm", str(STORY))
        self.write(ARTIFACTS / "delivery.md", "Completely rewritten delivery.\n" * 40)
        self.commit()
        self.assert_fail(self.run_event(before=before), "Removed story")

    def test_unmarked_evidence_symlink_is_excluded(self):
        path = self.repo / ARTIFACTS / "dw-evidence.md"
        path.parent.mkdir(parents=True)
        path.symlink_to("/etc/passwd")
        self.commit()
        self.assert_pass(self.run_event(), count=0)

    def test_worktree_content_and_pointers_cannot_change_snapshot(self):
        self.write(STORY, self.story())
        after = self.commit()
        self.write(STORY, "not the committed story")
        self.pointer(NEW)
        self.assert_pass(self.run_event(after=after))

    def test_pull_request_uses_merge_base_and_head_not_target_or_checkout(self):
        self.git("switch", "-c", "feature")
        self.write(STORY, self.story())
        head = self.commit()
        self.git("switch", "main")
        self.pointer(NEW)
        self.write(ARTIFACTS / "spec-9-1-target-only.md", "no baseline")
        target = self.commit()
        self.git("merge", "--no-edit", "feature")
        event = {"pull_request": {"base": {"sha": target}, "head": {"sha": head}}}
        result = self.run_event(event_name="pull_request", event=event)
        self.assert_pass(result)
        self.assertIn(f"Event range: {self.base}..{head}", result.stdout)
        self.assertNotIn("target-only", result.stdout)

    def test_retargeted_pull_request_rechecks_new_base_range(self):
        self.write(STORY, self.story())
        self.pointer(NEW)
        old_target = self.commit()
        head = self.commit()
        event = {"action": "edited", "pull_request": {"base": {"sha": old_target}, "head": {"sha": head}}}
        self.assert_pass(self.run_event(event_name="pull_request", event=event), count=0)
        event["pull_request"]["base"]["sha"] = self.base
        self.assert_fail(self.run_event(event_name="pull_request", event=event), "UNDECLARED")

    def test_historical_repair_uses_recorded_endpoint(self):
        endpoint, before = self.historical_story()
        self.commit()
        result = self.run_event(before=before)
        self.assert_pass(result)
        self.assertIn(f"Compared: {endpoint}", result.stdout)

    def test_historical_repair_without_endpoint_fails_on_later_pointers(self):
        _, before = self.historical_story()
        self.write(STORY, self.story() + "\nHistorical repair.\n")
        self.commit()
        self.assert_fail(self.run_event(before=before), "UNDECLARED")

    def test_historical_endpoint_cannot_hide_new_pointer_commit(self):
        _, before = self.historical_story()
        self.pointer("3" * 40)
        self.commit()
        self.assert_fail(self.run_event(before=before), "historical gitlink_audit_ref cannot be combined")

    def test_historical_initial_snapshot_cannot_hide_later_story_pointer_work(self):
        self.write(STORY, self.story())
        initial = self.commit()
        self.write(STORY, self.story() + "\nCompleted with dependency work.\n")
        self.pointer(NEW)
        before = self.commit()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{initial}'\n") + "\nHistorical correction.\n")
        self.commit()
        self.assert_fail(self.run_event(before=before), "omits later story-associated gitlink commit")

    def test_historical_endpoint_follows_prior_rename_and_rename_back(self):
        self.write(STORY, self.story())
        initial = self.commit()
        alternate = ARTIFACTS / "alternate.md"
        self.git("mv", str(STORY), str(alternate))
        self.commit()
        self.write(alternate, self.story() + "\nCompleted with dependency work.\n")
        self.pointer(NEW)
        self.commit()
        self.git("mv", str(alternate), str(STORY))
        before = self.commit()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{initial}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "omits later story-associated gitlink commit")

    def test_historical_endpoint_cannot_cross_deleted_and_recreated_identity(self):
        self.write(STORY, self.story())
        initial = self.commit()
        self.git("rm", str(STORY))
        self.commit()
        self.write(STORY, self.story() + "\nRecreated story.\n")
        before = self.commit()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{initial}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "identity was recreated")

    def test_historical_repair_after_rename_retains_original_endpoint_identity(self):
        self.write(STORY, self.story())
        initial = self.commit()
        alternate = ARTIFACTS / "alternate.md"
        self.git("mv", str(STORY), str(alternate))
        self.commit()
        self.pointer(NEW)
        before = self.commit()
        self.write(alternate, self.story(extra=f"gitlink_audit_ref: '{initial}'\n"))
        self.commit()
        self.assert_pass(self.run_event(before=before))

    def test_historical_endpoint_cannot_hide_move_then_revert(self):
        _, before = self.historical_story()
        self.pointer("3" * 40)
        self.commit()
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(before=before), "gitlink-bearing event commits")

    def test_historical_endpoint_cannot_hide_merge_pointer_change(self):
        _, before = self.historical_story()
        # Commit metadata on main; introduce a pointer on an independent branch.
        self.commit()
        self.git("switch", "-c", "dependency", before)
        self.pointer("3" * 40)
        self.commit()
        self.git("switch", "main")
        self.git("merge", "--no-ff", "--no-edit", "dependency")
        self.assert_fail(self.run_event(before=before), "gitlink-bearing event commits")

    def test_historical_endpoint_requires_full_commit(self):
        endpoint, before = self.historical_story()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{endpoint[:8]}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "full canonical commit ID")

    def test_unavailable_historical_endpoint_fails(self):
        _, before = self.historical_story()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{'f' * 40}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "rev-parse")

    def test_unrelated_historical_endpoint_fails(self):
        _, before = self.historical_story()
        self.commit()
        self.git("switch", "-c", "unrelated", self.base)
        unrelated = self.commit()
        self.git("switch", "main")
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{unrelated}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "not an ancestor")

    def test_historical_endpoint_cannot_be_after_event_base(self):
        _, before = self.historical_story()
        newer = self.commit()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{newer}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "not an ancestor")

    def test_historical_endpoint_must_contain_story(self):
        _, before = self.historical_story()
        self.write(STORY, self.story(extra=f"gitlink_audit_ref: '{self.base}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "identity was recreated")

    def test_historical_repair_must_retain_recorded_baseline(self):
        endpoint, before = self.historical_story()
        self.write(STORY, self.story(baseline=endpoint, extra=f"gitlink_audit_ref: '{endpoint}'\n"))
        self.commit()
        self.assert_fail(self.run_event(before=before), "mid-story baseline")

    def test_deleted_story_with_pointer_movement_fails(self):
        self.write(STORY, self.story())
        before = self.commit()
        self.git("rm", str(STORY))
        self.pointer(NEW)
        self.commit()
        self.assert_fail(self.run_event(before=before), "Removed story")

    def test_deleted_story_without_pointer_movement_cannot_erase_identity(self):
        self.write(STORY, self.story())
        before = self.commit()
        self.git("rm", str(STORY))
        self.commit()
        self.assert_fail(self.run_event(before=before), "Removed story")

    def test_symlink_type_change_cannot_hide_story(self):
        self.write(STORY, self.story())
        before = self.commit()
        (self.repo / STORY).unlink()
        (self.repo / STORY).symlink_to("/etc/passwd")
        self.commit()
        self.assert_fail(self.run_event(before=before), "regular Markdown file")

    def test_recorded_owned_chain_still_passes(self):
        self.pointer(NEW)
        owned = self.commit()
        text = self.story(declared=True) + f"\n## Story-Owned Gitlink Commits\n- `{owned}`\n"
        self.write(STORY, text)
        self.commit()
        self.assert_pass(self.run_event())

    def test_recorded_owned_chain_cannot_hide_later_unrecorded_pointer(self):
        self.pointer(NEW)
        owned = self.commit()
        self.write(STORY, self.story(declared=True) + f"\n## Story-Owned Gitlink Commits\n- `{owned}`\n")
        self.pointer("3" * 40)
        self.commit()
        self.assert_fail(self.run_event(), "UNRECORDED ENDPOINT")

    def test_invalid_event_evidence_fails_closed(self):
        for event in [{}, {"before": "0" * 40, "after": self.base},
                      {"before": "main", "after": self.base},
                      {"before": self.base, "after": "f" * 40}]:
            with self.subTest(event=event):
                self.assert_fail(self.run_event(event=event), "RESULT: FAIL")

    def test_nonancestor_push_range_fails(self):
        self.write(STORY, self.story())
        ahead = self.commit()
        self.assert_fail(self.run_event(before=ahead, after=self.base), "not an ancestor")

    def test_unsupported_event_fails(self):
        self.assert_fail(self.run_event(event_name="workflow_dispatch"), "Unsupported event")

    def test_shallow_checkout_fails_even_when_both_endpoints_exist(self):
        self.commit()
        head = self.commit()
        clone = Path(self.directory.name) / "shallow"
        result = subprocess.run(
            ["git", "clone", "--depth=2", self.repo.as_uri(), str(clone)],
            text=True, capture_output=True, check=False,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        before = self.git("rev-parse", f"{head}^")
        self.assert_fail(self.run_event(before=before, after=head, repo=clone), "Full Git history is required")


if __name__ == "__main__":
    unittest.main()
