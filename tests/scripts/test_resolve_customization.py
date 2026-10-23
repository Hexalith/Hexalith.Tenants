"""Run the actual resolver under conflicting temporary project installations."""

import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]


class ResolveCustomizationTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="bmad roots ")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.cwd = self.project("working", "cwd")
        self.installed = self.project("installed", "script")
        self.skill_root = self.project("skills", "skill")
        self.explicit = self.project("explicit", "explicit")
        self.skill = self.skill_root / ".agents/skills/example"
        self.skill.mkdir(parents=True)
        (self.skill / "customize.toml").write_text('value = "default"\ndefault_only = true\n')
        self.script = self.installed / "_bmad/scripts/resolve_customization.py"
        self.script.parent.mkdir()
        for name in ("resolve_customization.py", "config_utils.py"):
            shutil.copyfile(ROOT / "_bmad/scripts" / name, self.script.parent / name)

    def project(self, name, value):
        root = self.root / name
        custom = root / "_bmad/custom"
        custom.mkdir(parents=True)
        (custom / "example.toml").write_text(f'value = "{value}"\nteam_only = true\n')
        return root

    def resolve(self, cwd=None, explicit=None, script=None):
        args = [sys.executable, str(script or self.script), "--skill", str(self.skill)]
        if explicit is not None:
            args += ["--project-root", str(explicit)]
        result = subprocess.run(args, cwd=cwd or self.cwd, capture_output=True,
                                text=True, check=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        return json.loads(result.stdout), result.stderr

    def test_explicit_root_overrides_conflicting_cwd_script_and_skill(self):
        (self.explicit / "_bmad/custom/example.user.toml").write_text('value = "user"\n')
        data, stderr = self.resolve(explicit=self.explicit)
        self.assertEqual(data, {"value": "user", "default_only": True, "team_only": True})
        self.assertEqual(stderr, "")

    def test_cwd_root_precedes_script_and_skill(self):
        data, stderr = self.resolve()
        self.assertEqual(data["value"], "cwd")
        self.assertEqual(stderr, "")

    def test_ancestor_bmad_root_precedes_nearest_nested_git(self):
        nested = self.cwd / "nested/submodule"
        nested.mkdir(parents=True)
        (nested / ".git").write_text("gitdir: unused-fixture\n")
        data, _ = self.resolve(cwd=nested)
        self.assertEqual(data["value"], "cwd")

    def test_script_root_precedes_skill_when_cwd_has_no_project(self):
        data, _ = self.resolve(cwd=self.root)
        self.assertEqual(data["value"], "script")

    def test_nearest_git_root_is_used_without_an_ancestor_bmad_root(self):
        git_root = self.root / "git-only"
        nested = git_root / "nested"
        nested.mkdir(parents=True)
        (git_root / ".git").write_text("gitdir: unused-fixture\n")
        data, stderr = self.resolve(cwd=nested)
        self.assertEqual(data, {"value": "default", "default_only": True})
        self.assertIn(f"resolved project root {git_root}", stderr)

    def test_no_project_uses_only_skill_defaults(self):
        standalone = self.root / "uninstalled"
        standalone.mkdir()
        for name in ("resolve_customization.py", "config_utils.py"):
            shutil.copyfile(self.script.parent / name, standalone / name)
        orphan_skill = self.root / "orphan-skill"
        orphan_skill.mkdir()
        shutil.copyfile(self.skill / "customize.toml", orphan_skill / "customize.toml")
        self.skill = orphan_skill
        data, stderr = self.resolve(cwd=standalone, script=standalone / self.script.name)
        self.assertEqual(data, {"value": "default", "default_only": True})
        self.assertEqual(stderr, "")

    def test_skill_root_is_final_fallback_for_standalone_script(self):
        standalone = self.root / "standalone"
        standalone.mkdir()
        for file in self.script.parent.iterdir():
            if file.is_file():
                shutil.copyfile(file, standalone / file.name)
        data, _ = self.resolve(cwd=self.root, script=standalone / self.script.name)
        self.assertEqual(data["value"], "skill")

    def test_masked_override_warns_without_using_rejected_root(self):
        (self.cwd / "_bmad/custom/example.toml").unlink()
        data, stderr = self.resolve()
        self.assertEqual(data, {"value": "default", "default_only": True})
        self.assertIn(f"resolved project root {self.cwd}", stderr)
        self.assertIn(str(self.installed), stderr)
        self.assertIn("--project-root", stderr)

    def test_explicit_empty_root_does_not_emit_masking_warning(self):
        (self.explicit / "_bmad/custom/example.toml").unlink()
        data, stderr = self.resolve(explicit=self.explicit)
        self.assertEqual(data["value"], "default")
        self.assertEqual(stderr, "")

    def test_user_only_selected_override_suppresses_warning(self):
        (self.cwd / "_bmad/custom/example.toml").unlink()
        (self.cwd / "_bmad/custom/example.user.toml").write_text('value = "user-only"\n')
        data, stderr = self.resolve()
        self.assertEqual(data["value"], "user-only")
        self.assertEqual(stderr, "")


if __name__ == "__main__":
    unittest.main(verbosity=2)
