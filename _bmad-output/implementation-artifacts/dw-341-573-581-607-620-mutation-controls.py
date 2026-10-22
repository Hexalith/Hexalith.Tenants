import contextlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import tempfile
import unittest

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('recovery_controls', root/'tests/scripts/test_partial_release_recovery.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
mutations = [
    ('eligibility-guard-removed', 'validate-partial-release-recovery.sh',
     'if [ "$existing" -eq 0 ] && [ "$container_status" = 404 ] && [ "$tag_status" = 404 ] && [ "$release_status" = 404 ]; then',
     'if false; then', 'test_entirely_absent_release_refuses_and_preserves_previous_state'),
    ('manifest-binding-removed', 'partial-release-recovery-common.sh',
     '[ "$actual_hash" = "$expected_hash" ] || recovery_fail \'The package manifest changed after recovery validation.\'',
     ':', 'test_changed_inventory_refuses_before_build_or_publication'),
    ('asset-hash-check-removed', 'partial-release-recovery-common.sh',
     '[ "$actual_hash" = "$expected_hash" ] || recovery_fail "Release asset $asset_name does not match the reviewed package."',
     ':', 'test_conflicting_asset_refuses_before_every_publication'),
    ('default-verification-inventory-restored', 'verify-partial-release.sh',
     "for package_id in $(jq -r '.packages[].id' \"$manifest\"); do",
     "for package_id in $(jq -r '.packages[].id' tools/release-packages.json); do", 'test_override_inventory_drives_full_pipeline'),
    ('release-metadata-check-removed', 'partial-release-recovery-common.sh',
     '.tag_name == $tag and .draft == false and .prerelease == $prerelease',
     'true', 'test_release_metadata_is_rechecked_before_publication'),
]
results = []
for name, filename, old, new, test in mutations:
    with tempfile.TemporaryDirectory(prefix='tenants-recovery-control-') as temporary:
        staged = Path(temporary)
        (staged/'scripts').mkdir()
        for script in (root/'scripts').iterdir():
            if script.is_file(): shutil.copyfile(script, staged/'scripts'/script.name)
        (staged/'tools').mkdir()
        shutil.copyfile(root/'tools/release-packages.json', staged/'tools/release-packages.json')
        for package in json.loads((staged/'tools/release-packages.json').read_text())['packages']:
            project = staged/package['project']
            project.parent.mkdir(parents=True)
            project.write_text('<Project />')
        file = staged/'scripts'/filename
        content = file.read_text()
        assert content.count(old) == 1, name
        file.write_text(content.replace(old, new))
        module.ROOT = staged
        stream = io.StringIO()
        result = unittest.TextTestRunner(stream=stream).run(unittest.TestSuite([module.PartialReleaseRecoveryTests(test)]))
        assert not result.wasSuccessful(), f'{name}: wrong behavior was accepted'
        results.append({'control': name, 'tests': result.testsRun, 'failures': len(result.failures),
                        'errors': len(result.errors), 'result': 'rejected'})
        (Path('/tmp')/('tenants-dw-recovery-'+name+'.txt')).write_text(stream.getvalue())
print(json.dumps(results, indent=2))
