"""Export real fixtures, resolve pinned assets, serve locally, run Playwright and compare pixels."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading

here = Path(__file__).resolve().parent
root = next(path for path in here.parents if (path / 'Hexalith.Tenants.slnx').is_file())
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path)
parser.add_argument('--use-built', action='store_true', help='Use already built current Debug source/test output')
args = parser.parse_args()
output = args.output.resolve() if args.output else Path(tempfile.mkdtemp(prefix='tenants-dw390-reproduction-'))
if output.is_relative_to(root):
    raise SystemExit('Use a temporary directory outside the repository; package bundles must not be vendored.')
if output.exists() and any(output.iterdir()):
    raise SystemExit('Use a fresh empty output directory so previous screenshots/results cannot authorize this run.')
output.mkdir(parents=True, exist_ok=True)
props = ['-c', 'Debug', '-m:1', '-p:UseHexalithProjectReferences=true',
         '-p:HexalithEventStoreVersion=3.128.2', '-v:q']
assembly = root / 'tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll'
with (output / 'fixture-export.txt').open('w') as log:
    if not args.use_built:
        for project in ['src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj',
                        'tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj']:
            subprocess.run(['dotnet', 'build', project, *props], cwd=root, stdout=log, stderr=subprocess.STDOUT, check=True)
    environment = dict(os.environ, TENANTS_CORRECTION_FIXTURE_DIRECTORY=str(output))
    subprocess.run(['dotnet', str(assembly), '-method', '*WhitespaceIdentitiesExportActualAnnotatedFluentMarkup'],
                   cwd=root, env=environment, stdout=log, stderr=subprocess.STDOUT, check=True)
assets = json.loads((root / 'src/Hexalith.Tenants.UI/obj/project.assets.json').read_text())
package = next(name for name in assets['libraries'] if name.lower().startswith('microsoft.fluentui.aspnetcore.components/'))
version = package.split('/', 1)[1]
package_path = next(Path(folder) / 'microsoft.fluentui.aspnetcore.components' / version / 'staticwebassets'
                    for folder in assets['packageFolders']
                    if (Path(folder) / 'microsoft.fluentui.aspnetcore.components' / version / 'staticwebassets').is_dir())
resolved = {'fluent-ui.js': package_path / 'Microsoft.FluentUI.AspNetCore.Components.lib.module.js',
            'fluent-ui.css': package_path / 'Microsoft.FluentUI.AspNetCore.Components.bundle.scp.css',
            'correction.css': root / 'src/Hexalith.Tenants.UI/obj/Debug/net10.0/scopedcss/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor.rz.scp.css'}
expected_hashes = json.loads((here / 'asset-hashes.json').read_text())
for name, source in resolved.items():
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    if digest != expected_hashes[name]:
        raise SystemExit(f'Pinned qualification asset changed: {name}; assess and record a fresh asset baseline.')
    shutil.copyfile(source, output / name)
for name in ['annotated.html', 'literal-0.png', 'literal-1.png']:
    shutil.copyfile(here / name, output / name)
(output / 'asset-hashes.json').write_text(json.dumps(expected_hashes, indent=2) + '\n')
cli = shutil.which('playwright-cli')
if cli is None:
    raise SystemExit('Install/enable playwright-cli and maintained Chrome before reproduction.')
handler = partial(SimpleHTTPRequestHandler, directory=str(output))
server = ThreadingHTTPServer(('127.0.0.1', 0), handler)
thread = threading.Thread(target=server.serve_forever, daemon=True)
thread.start()
session = 'dw390-' + str(os.getpid())
try:
    subprocess.run([cli, '-s=' + session, 'open', '--browser=chrome'], cwd=output, check=True,
                   stdout=(output / 'playwright-open.txt').open('w'), stderr=subprocess.STDOUT)
    code = (here / 'run-annotated-playwright.js').read_text().replace(
        'http://127.0.0.1:18790', f'http://127.0.0.1:{server.server_port}').replace(
        '/tmp/tenants-dw390-browser/', str(output) + '/')
    completed = subprocess.run([cli, '-s=' + session, 'run-code', code], cwd=output, check=True,
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    (output / 'playwright-output.txt').write_text(completed.stdout)
    if '### Result\n' not in completed.stdout:
        raise RuntimeError('Playwright returned no qualification result')
    metadata, _ = json.JSONDecoder().raw_decode(completed.stdout.split('### Result\n', 1)[1])
    metadata['provenance'] = 'Fresh bUnit-exported production Fluent correction markup; pinned package/scoped CSS; CDP fonts and accessible names. No live command or human AT claim.'
    (output / 'annotated-metadata.json').write_text(json.dumps(metadata, indent=2) + '\n')
    subprocess.run([sys.executable, str(here / 'compare-pixels.py'), str(output)], check=True)
finally:
    subprocess.run([cli, '-s=' + session, 'close'], cwd=output, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    server.shutdown()
    server.server_close()
print('Reproduction artifacts:', output)
