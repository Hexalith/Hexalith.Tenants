"""Reproduce the exact silent-return control and restore/rebuild original source."""
from pathlib import Path
import argparse
import hashlib
import subprocess

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--log-directory', type=Path, default=Path('/tmp/dw-grant-refusal-control'))
args = parser.parse_args()
args.log_directory.mkdir(parents=True, exist_ok=True)
source = ROOT / 'src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor'
original = source.read_bytes()
eol = b'\r\n' if b'\r\n' in original else b'\n'
call = b'                    SurfaceAmbiguousGrantRetryUnavailable(expectedSnapshot, expectedLease);' + eol
needle = eol.join([call.rstrip(b'\r\n'), b'                    return;', b'                }', b'',
                    b'                requestSentSnapshot = expectedSnapshot;'])
if original.count(needle) != 1:
    raise RuntimeError('Expected exactly one inner redispatch refusal call')
mutated = original.replace(needle, needle.replace(call, b''))
build = ['dotnet', 'build', 'tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj',
         '-c', 'Debug', '-p:UseNuGetDeps=false', '-m:1', '-v:q']
test = ['dotnet', 'tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll',
        '-method', '*GlobalAdministratorsPageTests.InnerGrantRedispatchRefusalSurfacesRecoveryWithoutAnEventRender']


def run(command, name):
    print(' '.join(command), flush=True)
    with (args.log_directory / name).open('w') as log:
        return subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT).returncode


source.write_bytes(mutated)
try:
    if run(build, 'mutation-build.log') != 0:
        raise RuntimeError('Mutation build failed')
    exit_code = run(test, 'mutation-test.log')
    output = (args.log_directory / 'mutation-test.log').read_text()
    if exit_code == 0 or 'after.IsDeliveryRetryWithdrawn' not in output or 'False' not in output:
        raise RuntimeError('Control did not fail its intended withdrawal assertion')
    print('Exact inner silent-return control rejected', flush=True)
finally:
    if source.read_bytes() != mutated:
        raise RuntimeError('Concurrent source edit detected; refusing overwrite')
    source.write_bytes(original)
    if run(build, 'restored-build.log') != 0:
        raise RuntimeError('Restored production build failed')
if source.read_bytes() != original:
    raise RuntimeError('Source restoration mismatch')
print('Restored original source SHA-256:', hashlib.sha256(original).hexdigest())
