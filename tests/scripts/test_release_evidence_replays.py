"""Allocate release evidence replays without executing the retained .NET recipes."""

import ast
import datetime
from pathlib import Path
import tempfile
import unittest
import uuid

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / '_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14'


class ReleaseEvidenceReplayTests(unittest.TestCase):
    def test_each_replay_allocates_an_exclusive_directory_and_preserves_originals(self):
        for name in ('resumed-parent-root-verification', 'resumed-parent-packed-verification'):
            with self.subTest(recipe=name), tempfile.TemporaryDirectory() as temporary:
                source = EVIDENCE / name / 'verify.py'
                tree = ast.parse(source.read_text())
                function = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == 'replay_output')
                namespace = {'datetime': datetime, 'uuid': uuid}
                exec(compile(ast.Module(body=[function], type_ignores=[]), str(source), 'exec'), namespace)
                base = Path(temporary)
                historical = base / 'results.json'
                historical.write_bytes(b'original retained success\n')
                first = namespace['replay_output'](base)
                second = namespace['replay_output'](base)
                self.assertNotEqual(first, second)
                self.assertEqual(first.parent, base / 'replays')
                self.assertEqual(second.parent, base / 'replays')
                self.assertTrue(first.is_dir())
                self.assertTrue(second.is_dir())
                self.assertEqual(historical.read_bytes(), b'original retained success\n')
                allocation = next(node for node in tree.body if isinstance(node, ast.Assign)
                                  and any(isinstance(target, ast.Name) and target.id == 'OUT' for target in node.targets))
                self.assertIsInstance(allocation.value, ast.Call)
                self.assertEqual(allocation.value.func.id, 'replay_output')
                self.assertFalse(any(isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute)
                                     and node.func.attr in ('run', 'check_output') for node in ast.walk(function)))


if __name__ == '__main__':
    unittest.main()
