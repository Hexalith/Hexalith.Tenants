"""Check retained policy bytes without launching the live Dapr qualification."""

import ast
import hashlib
import json
from pathlib import Path
import types
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'tests/integration/qualify-dapr-read-retries.py'


class DaprReadPolicyEvidenceTests(unittest.TestCase):
    def test_normal_and_retry_mutation_reports_reconstruct_the_exact_loaded_policy(self):
        tree = ast.parse(SCRIPT.read_text())
        report = next(node for node in ast.walk(tree) if isinstance(node, ast.Assign)
                      and any(isinstance(target, ast.Name) and target.id == 'report' for target in node.targets))
        code = compile(ast.Module(body=[report], type_ignores=[]), str(SCRIPT), 'exec')
        for mutation in (False, True):
            with self.subTest(mutation=mutation):
                policy = ('# café\r\nendpoints:\r\n  tenants-api-random-qualification-id:\r\n'
                          f'    maxRetries: {3 if mutation else 0}\r\n').encode()
                scope = {'hashlib': hashlib, 'version': '1.18.7',
                         'args': types.SimpleNamespace(mutate_retries=mutation),
                         'original_results': 13, 'namespace': 'isolated-qualification',
                         'original': 'public ACL', 'original_resiliency': b'original policy',
                         'loaded_resiliency': policy, 'results': [], 'counts': {},
                         'private_key': 'must-not-appear-in-the-report'}
                exec(code, scope)
                serialized = json.dumps(scope['report'])
                retained = json.loads(serialized)
                reconstructed = retained['loadedResiliencyPolicy'].encode('utf-8')
                self.assertEqual(reconstructed, policy)
                self.assertEqual(hashlib.sha256(reconstructed).hexdigest(), retained['loadedResiliencySha256'])
                self.assertNotIn(scope['private_key'], serialized)
                self.assertEqual(retained['originalControlCount'], 13)
                self.assertEqual(retained['mutation'], mutation)


if __name__ == '__main__':
    unittest.main()
