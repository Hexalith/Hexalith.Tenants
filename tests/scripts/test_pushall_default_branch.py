"""Execute pushall's documented default resolution in disposable local Git repos.

Run: python3 tests/scripts/test_pushall_default_branch.py
No workspace sync or network publication is performed.
"""

import json
import os
from pathlib import Path
import re
import shlex
import subprocess
import sys
import tempfile
import unittest

import test_pushall_detached_head as attachment


MESSAGE = "build: exercise pushall default branch selection"
SKILLS = attachment.SKILLS


def documented_blocks(skill):
    body = skill.read_text(encoding="utf-8").split("## Per-repo procedure", 1)[1]
    return re.findall(r"```bash\n(.*?)\n```", body, re.S)[1:3]


class PushallDefaultBranchTests(unittest.TestCase):
    def setUp(self):
        self.fixture = attachment.PushallDetachedHeadTests()
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.repo = self.fixture.repo
        self.remote = self.fixture.remote
        self.oid = self.git(self.repo, "rev-parse", "HEAD").stdout.decode().strip()

    def git(self, repo, *args, **kwargs):
        return self.fixture.git(repo, *args, **kwargs)

    def default(self, branch, local=True):
        self.git(self.repo, "branch", branch, self.oid)
        self.git(self.repo, "push", "origin", f"refs/heads/{branch}")
        self.git(self.remote, "symbolic-ref", "HEAD", f"refs/heads/{branch}")
        if not local:
            self.git(self.repo, "branch", "-d", branch)
        self.git(self.repo, "fetch", "origin")

    def advertisement(self, branch="main", oid=None):
        return f"ref: refs/heads/{branch}\tHEAD\n{oid or self.oid}\tHEAD\n"

    def run_block(self, skill, overrides=None, switch=False, variables=None):
        shim = Path(tempfile.mkdtemp(prefix="default trace ", dir=self.fixture.root))
        trace = shim / "commands.jsonl"
        script = shim / "git"
        script.write_text(f"#!{sys.executable}\nimport json, os, sys\n"
                          "args = sys.argv[1:]\n"
                          f"with open({str(trace)!r}, 'a') as stream:\n"
                          "    stream.write(json.dumps(args) + '\\n')\n"
                          "command = args[args.index('-C') + 2]\n"
                          f"overrides = {overrides or {}!r}\n"
                          "if command in overrides:\n"
                          "    status, output = overrides[command]\n"
                          "    sys.stdout.write(output)\n"
                          "    sys.exit(status)\n"
                          f"os.execv({self.fixture.real_git!r}, ['git', *args])\n")
        script.chmod(0o755)
        env = dict(self.fixture.env, PATH=str(shim) + os.pathsep + self.fixture.env["PATH"])
        resolve, checkout = documented_blocks(skill)
        command = resolve
        if variables:
            command = "\n".join(f"{key}={shlex.quote(value)}" for key, value in variables.items())
        if switch:
            command += "\n" + checkout
        command += '\nprintf "%s\\n" "$pushall_default_branch"'
        command = command.replace("<dir>", shlex.quote(str(self.repo)))
        result = subprocess.run(["bash", "-e", "-c", command], env=env,
                                capture_output=True, check=False)
        calls = [json.loads(line) for line in trace.read_text().splitlines()]
        for args in calls:
            self.assertEqual(args[args.index("-C") + 1], str(self.repo))
            allowed = ("remote", "ls-remote", "check-ref-format", "for-each-ref", "cat-file")
            if switch:
                allowed += ("show-ref", "switch")
            self.assertIn(args[args.index("-C") + 2], allowed)
        return result, calls

    def assert_resolves(self, expected):
        before = self.fixture.snapshot()
        for skill in SKILLS:
            result, _ = self.run_block(skill)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.decode().strip(), expected)
            self.assertEqual(self.fixture.snapshot(), before)

    def assert_refuses(self, overrides=None, diagnostic=b"SKIP:"):
        self.fixture.dirty("combined")
        before = self.fixture.snapshot()
        for skill in SKILLS:
            result, _ = self.run_block(skill, overrides=overrides)
            self.assertNotEqual(result.returncode, 0, result.stdout)
            self.assertIn(diagnostic, result.stderr)
            self.assertEqual(self.fixture.snapshot(), before)

    def test_master_wins_over_local_main_and_stale_cached_origin_head(self):
        self.default("master")
        self.assertEqual(self.git(self.repo, "symbolic-ref", "refs/remotes/origin/HEAD").stdout,
                         b"refs/remotes/origin/main\n")
        self.assert_resolves("master")

    def test_main_wins_when_master_also_exists(self):
        self.default("master")
        self.git(self.remote, "symbolic-ref", "HEAD", "refs/heads/main")
        self.assert_resolves("main")

    def test_nonstandard_slash_default_is_selected(self):
        self.default("release/stable")
        self.assert_resolves("release/stable")

    def test_missing_cached_origin_head_is_irrelevant(self):
        self.git(self.repo, "symbolic-ref", "--delete", "refs/remotes/origin/HEAD")
        self.assert_resolves("main")

    def test_dangling_cached_origin_head_is_irrelevant(self):
        self.git(self.repo, "symbolic-ref", "refs/remotes/origin/HEAD", "refs/remotes/origin/gone")
        self.assert_resolves("main")

    def test_missing_local_default_is_created_at_the_remote_commit(self):
        self.default("release/stable", local=False)
        for skill in SKILLS:
            result, _ = self.run_block(skill, switch=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.strip(), b"release/stable")
            self.assertEqual(self.git(self.repo, "symbolic-ref", "HEAD").stdout,
                             b"refs/heads/release/stable\n")
            self.assertEqual(self.git(self.repo, "rev-parse", "HEAD").stdout.strip().decode(), self.oid)
            self.git(self.repo, "checkout", "--detach")
            self.git(self.repo, "branch", "-d", "release/stable")

    def test_existing_local_default_is_switched_without_reset(self):
        self.default("master")
        self.git(self.repo, "checkout", "master")
        (self.repo / "local.txt").write_bytes(b"preserve local commit\n")
        self.git(self.repo, "add", "local.txt")
        self.git(self.repo, "commit", "-m", MESSAGE)
        expected = self.git(self.repo, "rev-parse", "HEAD").stdout
        self.git(self.repo, "checkout", "--detach", self.oid)
        for skill in SKILLS:
            result, _ = self.run_block(skill, switch=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(self.git(self.repo, "rev-parse", "HEAD").stdout, expected)

    def test_missing_configured_origin_cannot_be_treated_as_a_local_path(self):
        self.git(self.repo, "remote", "rename", "origin", "upstream")
        (self.repo / "origin").symlink_to(self.remote, target_is_directory=True)
        self.assert_refuses(diagnostic=b"no configured origin")

    def test_unavailable_origin_refuses(self):
        self.git(self.repo, "remote", "set-url", "origin", str(self.fixture.root / "missing"))
        self.assert_refuses(diagnostic=b"cannot query")

    def test_unborn_remote_head_refuses(self):
        self.git(self.remote, "symbolic-ref", "HEAD", "refs/heads/missing")
        self.assert_refuses()

    def test_detached_remote_head_refuses(self):
        self.git(self.remote, "update-ref", "--no-deref", "HEAD", self.oid)
        self.assert_refuses(diagnostic=b"missing, ambiguous or malformed")

    def test_malformed_or_ambiguous_advertisements_refuse(self):
        good = self.advertisement()
        for output in ("", good + good, good + "unexpected\n", good.splitlines()[0] + "\n",
                       f"{self.oid}\tHEAD\n", good.replace("refs/heads/", "refs/tags/"),
                       good.replace(self.oid, "x" * 40), good.replace(self.oid, "a" * 39)):
            with self.subTest(output=output):
                self.assert_refuses({"ls-remote": (0, output)}, b"missing, ambiguous or malformed")

    def test_invalid_local_or_remote_branch_names_refuse(self):
        for branch in ("bad..name", "-option", "HEAD", "bad@{name", "bad.lock"):
            with self.subTest(branch=branch):
                self.assert_refuses({"ls-remote": (0, self.advertisement(branch))}, b"invalid default")

    def test_failed_git_inspection_refuses_even_with_success_shaped_stdout(self):
        evidence = f"refs/remotes/origin/main\t{self.oid}\t\n"
        for command, output in (("remote", str(self.remote)), ("ls-remote", self.advertisement()),
                                ("check-ref-format", "main\n"), ("for-each-ref", evidence),
                                ("cat-file", "commit\n")):
            with self.subTest(command=command):
                self.assert_refuses({command: (128, output)})

    def test_unfetched_default_branch_refuses(self):
        self.default("master")
        self.git(self.repo, "update-ref", "-d", "refs/remotes/origin/master")
        self.assert_refuses(diagnostic=b"fetched origin default")

    def test_concurrent_remote_advance_refuses_stale_fetched_tip(self):
        seed = self.fixture.root / "seed"
        (seed / "new.txt").write_bytes(b"new remote commit\n")
        self.git(seed, "add", "new.txt")
        self.git(seed, "commit", "-m", MESSAGE)
        self.git(seed, "push", "origin", "main")
        self.assert_refuses(diagnostic=b"cannot inspect the fetched default commit")

    def test_indirect_tracking_ref_refuses(self):
        self.git(self.repo, "symbolic-ref", "refs/remotes/origin/main", "refs/heads/main")
        self.assert_refuses(diagnostic=b"fetched origin default")

    def test_non_commit_default_refuses(self):
        blob = self.git(self.repo, "rev-parse", "HEAD:tracked.txt").stdout.decode().strip()
        self.git(self.repo, "update-ref", "refs/remotes/origin/main", blob)
        self.assert_refuses({"ls-remote": (0, self.advertisement(oid=blob))}, b"fetched origin default")

    def test_narrow_fetch_refspec_cannot_hide_missing_default_coverage(self):
        self.default("master")
        self.git(self.repo, "config", "remote.origin.fetch", "+refs/heads/main:refs/remotes/origin/other")
        self.git(self.repo, "update-ref", "-d", "refs/remotes/origin/master")
        self.git(self.repo, "fetch", "origin")
        self.assert_refuses(diagnostic=b"fetched origin default")

    def test_custom_fetch_mapping_does_not_break_missing_local_branch_creation(self):
        self.default("master", local=False)
        self.git(self.repo, "config", "remote.origin.fetch", "+refs/heads/main:refs/remotes/origin/other")
        for skill in SKILLS:
            result, _ = self.run_block(skill, switch=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(self.git(self.repo, "symbolic-ref", "HEAD").stdout, b"refs/heads/master\n")
            self.git(self.repo, "checkout", "--detach")
            self.git(self.repo, "branch", "-d", "master")

    def test_local_ref_inspection_error_does_not_create_or_switch(self):
        self.default("master", local=False)
        before = self.fixture.snapshot()
        for skill in SKILLS:
            result, calls = self.run_block(skill, overrides={"show-ref": (128, "")}, switch=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn(b"cannot inspect the local default", result.stderr)
            self.assertFalse(any("switch" in args for args in calls))
            self.assertEqual(self.fixture.snapshot(), before)

    def test_selected_master_receives_commit_while_nondefault_main_can_be_pruned(self):
        self.default("master", local=False)
        self.git(self.repo, "checkout", "-b", "fix/fixture")
        self.fixture.dirty("combined")
        result, _ = self.run_block(SKILLS[0])
        self.assertEqual(result.returncode, 0, result.stderr)
        default = result.stdout.decode().strip()
        self.assertEqual(default, "master")
        self.git(self.repo, "add", "-A")
        self.git(self.repo, "commit", "-m", MESSAGE)
        committed = self.git(self.repo, "rev-parse", "HEAD").stdout.decode().strip()
        result, _ = self.run_block(SKILLS[0], switch=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.git(self.repo, "merge", "--ff-only", f"origin/{default}")
        self.git(self.repo, "merge", "--no-ff", "fix/fixture", "-m", MESSAGE)
        self.git(self.repo, "push", "origin", default)
        self.git(self.repo, "branch", "-d", "main")
        self.git(self.repo, "push", f"--force-with-lease=refs/heads/main:{self.oid}",
                 "origin", "--delete", "main")
        self.git(self.remote, "merge-base", "--is-ancestor", committed, f"refs/heads/{default}")
        self.assertEqual(self.git(self.remote, "symbolic-ref", "HEAD").stdout, b"refs/heads/master\n")
        self.assertEqual(self.git(self.remote, "show", "master:tracked.txt").stdout, b"unstaged\n")
        self.assertEqual(self.git(self.remote, "show", "master:untracked.txt").stdout, b"untracked\n")
        self.assertNotEqual(self.git(self.remote, "show-ref", "--verify", "refs/heads/main", check=False).returncode, 0)


if __name__ == "__main__":
    unittest.main(verbosity=2)
