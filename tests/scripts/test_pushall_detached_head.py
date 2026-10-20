"""Execute the documented pushall attachment gate in disposable Git submodules.

Run: python3 tests/scripts/test_pushall_detached_head.py
Only local temporary repositories are modified; no pushall skill is invoked.
"""

import json
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
SKILLS = [ROOT / entry / "skills/pushall/SKILL.md"
          for entry in (".agents", ".claude", ".github")]
MESSAGE = "build: exercise pushall attachment safety"


def documented_gate(skill):
    body = skill.read_text(encoding="utf-8").split("## Per-repo procedure", 1)[1]
    return re.search(r"```bash\n(.*?)\n```", body, re.S)[1]


class PushallDetachedHeadTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="pushall gate ")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.real_git = shutil.which("git")
        self.env = {key: value for key, value in os.environ.items()
                    if not key.startswith("GIT_")}
        self.env.update({"GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull,
                         "GIT_TERMINAL_PROMPT": "0", "GIT_AUTHOR_NAME": "Fixture",
                         "GIT_COMMITTER_NAME": "Fixture", "GIT_AUTHOR_EMAIL": "fixture@example.invalid",
                         "GIT_COMMITTER_EMAIL": "fixture@example.invalid"})
        self.remote = self.root / "remote.git"
        self.git(self.root, "init", "--bare", "--initial-branch=main", str(self.remote))
        seed = self.root / "seed"
        self.git(self.root, "clone", str(self.remote), str(seed))
        (seed / "tracked.txt").write_bytes(b"baseline\n")
        self.git(seed, "add", "tracked.txt")
        self.git(seed, "commit", "-m", MESSAGE)
        self.git(seed, "push", "origin", "main")
        parent = self.root / "parent"
        self.parent = parent
        self.git(self.root, "init", "--initial-branch=main", str(parent))
        self.git(parent, "-c", "protocol.file.allow=always", "submodule", "add",
                 str(self.remote), "references/dependency with spaces")
        self.git(parent, "commit", "-m", MESSAGE)
        self.repo = parent / "references/dependency with spaces"
        self.git(self.repo, "checkout", "--detach")

    def git(self, repo, *args, check=True):
        return subprocess.run(["git", "--no-optional-locks", "-C", str(repo), *args],
                              env=self.env, capture_output=True, check=check)

    def snapshot(self):
        gitdir = Path(self.git(self.repo, "rev-parse", "--absolute-git-dir").stdout.decode().strip())
        files = {}
        for path in self.repo.rglob("*"):
            if path.is_symlink() or path.is_file():
                metadata = path.lstat()
                content = os.readlink(path) if path.is_symlink() else path.read_bytes()
                files[str(path.relative_to(self.repo))] = (metadata.st_mode, metadata.st_mtime_ns, content)
        fetch_head = gitdir / "FETCH_HEAD"
        return (self.git(self.repo, "rev-parse", "HEAD").stdout,
                self.git(self.repo, "symbolic-ref", "--quiet", "HEAD", check=False).stdout,
                self.git(self.repo, "show-ref").stdout,
                (gitdir / "index").read_bytes(), files,
                fetch_head.read_bytes() if fetch_head.exists() else None,
                self.git(self.repo, "status", "--porcelain=v1", "--untracked-files=all",
                         "--ignore-submodules=none").stdout,
                self.git(self.remote, "show-ref").stdout)

    def gate(self, skill, failure=None):
        shim = Path(tempfile.mkdtemp(prefix="git trace ", dir=self.root))
        trace = shim / "commands.jsonl"
        script = shim / "git"
        script.write_text(f"#!{sys.executable}\nimport json, os, sys\n"
                          f"with open({str(trace)!r}, 'a') as stream:\n"
                          "    stream.write(json.dumps(sys.argv[1:]) + '\\n')\n"
                          f"if {failure!r} in sys.argv[1:]:\n    sys.exit(128)\n"
                          f"os.execv({self.real_git!r}, ['git', *sys.argv[1:]])\n")
        script.chmod(0o755)
        env = dict(self.env, PATH=str(shim) + os.pathsep + self.env["PATH"])
        command = documented_gate(skill).replace("<dir>", shlex.quote(str(self.repo)))
        result = subprocess.run(["bash", "-e", "-c", command], env=env,
                                capture_output=True, check=False)
        for line in trace.read_text().splitlines() if trace.exists() else []:
            args = json.loads(line)
            self.assertIn("-C", args, f"Gate Git operation lacks a repository directory: {args}")
            directory = args.index("-C")
            del args[directory:directory + 2]
            args = [arg for arg in args if arg != "--no-optional-locks"]
            self.assertIn(args[0], ("status", "symbolic-ref"), f"Prohibited gate operation: {args}")
        return result

    def dirty(self, kind):
        tracked = self.repo / "tracked.txt"
        if kind in ("staged", "combined"):
            tracked.write_bytes(b"staged\n")
            self.git(self.repo, "add", "tracked.txt")
        if kind in ("unstaged", "combined"):
            tracked.write_bytes(b"unstaged\n")
        if kind in ("untracked", "combined"):
            (self.repo / "untracked.txt").write_bytes(b"untracked\n")
        if kind == "deleted":
            tracked.unlink()

    def test_dirty_detached_repositories_are_refused_without_changes(self):
        # Use a fresh submodule fixture for each independent dirty state.
        for kind in ("staged", "unstaged", "untracked", "combined", "deleted"):
            with self.subTest(kind=kind):
                fixture = PushallDetachedHeadTests()
                fixture.setUp()
                try:
                    fixture.dirty(kind)
                    before = fixture.snapshot()
                    for skill in SKILLS:
                        result = fixture.gate(skill)
                        self.assertNotEqual(result.returncode, 0, result.stdout)
                        self.assertIn(b"dirty detached HEAD", result.stderr)
                        self.assertEqual(fixture.snapshot(), before)
                finally:
                    fixture.doCleanups()

    def test_clean_detached_repository_may_proceed_without_changes(self):
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(self.snapshot(), before)

    def test_dirty_attached_commit_reaches_the_pushed_default_branch(self):
        self.git(self.repo, "checkout", "-b", "fix/fixture")
        self.dirty("combined")
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(self.snapshot(), before)
        self.git(self.repo, "add", "-A")
        self.git(self.repo, "commit", "-m", MESSAGE)
        committed = self.git(self.repo, "rev-parse", "HEAD").stdout.decode().strip()
        self.git(self.repo, "checkout", "main")
        self.git(self.repo, "merge", "--ff-only", "origin/main")
        self.git(self.repo, "merge", "--no-ff", "fix/fixture", "-m", MESSAGE)
        self.git(self.repo, "push", "origin", "main")
        self.git(self.remote, "merge-base", "--is-ancestor", committed, "refs/heads/main")
        self.assertEqual(self.git(self.remote, "show", "main:tracked.txt").stdout, b"unstaged\n")
        self.assertEqual(self.git(self.remote, "show", "main:untracked.txt").stdout, b"untracked\n")

    def test_dirty_default_branch_may_proceed(self):
        self.git(self.repo, "checkout", "main")
        self.dirty("staged")
        before = self.snapshot()
        for skill in SKILLS:
            self.assertEqual(self.gate(skill).returncode, 0)
            self.assertEqual(self.snapshot(), before)

    def test_status_inspection_failure_refuses_the_repository(self):
        self.repo = self.root / "missing repository"
        for skill in SKILLS:
            result = self.gate(skill)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"cannot inspect repository status", result.stderr)
            self.assertFalse(self.repo.exists())

    def test_branch_inspection_error_is_distinct_from_detachment(self):
        self.dirty("combined")
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill, failure="symbolic-ref")
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"cannot verify the current branch", result.stderr)
            self.assertNotIn(b"dirty detached HEAD", result.stderr)
            self.assertEqual(self.snapshot(), before)

    def test_status_error_preserves_an_existing_dirty_repository(self):
        self.dirty("combined")
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill, failure="status")
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"cannot inspect repository status", result.stderr)
            self.assertEqual(self.snapshot(), before)

    def test_untracked_files_cannot_be_hidden_by_status_configuration(self):
        self.git(self.repo, "config", "status.showUntrackedFiles", "no")
        self.dirty("untracked")
        self.assertEqual(self.git(self.repo, "status", "--porcelain").stdout, b"")
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"dirty detached HEAD", result.stderr)
            self.assertEqual(self.snapshot(), before)

    def test_dirty_submodule_cannot_be_hidden_by_ignore_configuration(self):
        self.dirty("combined")
        self.repo = self.parent
        self.git(self.repo, "checkout", "--detach")
        self.git(self.repo, "config", "diff.ignoreSubmodules", "all")
        self.assertEqual(self.git(self.repo, "status", "--porcelain").stdout, b"")
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"dirty detached HEAD", result.stderr)
            self.assertEqual(self.snapshot(), before)

    def test_symbolic_head_outside_local_branches_is_refused(self):
        self.git(self.repo, "tag", "holder")
        self.git(self.repo, "symbolic-ref", "HEAD", "refs/tags/holder")
        self.dirty("combined")
        before = self.snapshot()
        for skill in SKILLS:
            result = self.gate(skill)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"HEAD does not name a local branch", result.stderr)
            self.assertEqual(self.snapshot(), before)

    def test_refusal_preserves_executable_bits_and_symlink_identity(self):
        self.dirty("untracked")
        (self.repo / "untracked.txt").chmod(0o755)
        (self.repo / "symbolic link").symlink_to("tracked.txt")
        before = self.snapshot()
        for skill in SKILLS:
            self.assertNotEqual(self.gate(skill).returncode, 0)
            self.assertEqual(self.snapshot(), before)

    def test_legacy_order_omits_the_dirty_detached_commit_from_the_push(self):
        self.dirty("combined")
        self.git(self.repo, "add", "-A")
        self.git(self.repo, "commit", "-m", MESSAGE)
        detached = self.git(self.repo, "rev-parse", "HEAD").stdout.decode().strip()
        self.git(self.repo, "checkout", "main")
        self.git(self.repo, "merge", "--ff-only", "origin/main")
        self.git(self.repo, "push", "origin", "main")
        self.assertEqual(self.git(self.remote, "rev-parse", "main").stdout,
                         self.git(self.repo, "rev-parse", "origin/main").stdout)
        self.assertEqual(self.git(self.repo, "merge-base", "--is-ancestor", detached,
                                  "origin/main", check=False).returncode, 1)
        self.assertEqual(self.git(self.remote, "show", "main:tracked.txt").stdout, b"baseline\n")

    def test_all_tracked_skill_copies_are_identical(self):
        self.assertEqual(SKILLS[0].read_bytes(), SKILLS[1].read_bytes())
        self.assertEqual(SKILLS[0].read_bytes(), SKILLS[2].read_bytes())


if __name__ == "__main__":
    unittest.main(verbosity=2)
