# DW-390 reproducible visual qualification

Run the retained pixel proof with Python 3 (no third-party Python packages):

```bash
python3 _bmad-output/implementation-artifacts/evidence/dw-154-387-390-568-625/browser/compare-pixels.py
```

The comparator decodes and unfilters PNGs, normalizes RGB/alpha and includes crop dimensions. It applies one exact pixel-distinction check to all five identities in each of eight positive groups, then applies that **same check** to the five actual literal-mutation crops. The mutation fails visually because crops 0/1 collide; the independent retained initial pair also compares equal. PNG compression and metadata do not determine equality. See `pixel-comparison.json` for decoded pixel hashes and collision pairs.

For a fresh executable reproduction, start from the initialized root dependencies and restored .NET 10.0.402 Debug source graph. Python 3, `playwright-cli`, and maintained Chrome must be available. From the repository root:

```bash
python3 _bmad-output/implementation-artifacts/evidence/dw-154-387-390-568-625/browser/reproduce-browser.py --output /tmp/tenants-dw390-reproduction
```

Choose a fresh empty output directory; omitting `--output` creates a unique temporary directory. Existing populated output is rejected so stale screenshots cannot qualify a failed run.

Use `--use-built` only when current Debug UI/test output is already built. The script exports the actual EN/FR component fixtures with the focused xUnit method, resolves Fluent 5.0.1 assets from `project.assets.json` and NuGet package folders, verifies their retained hashes, and copies them only into the temporary output directory. It serves that directory on a loopback ephemeral port, invokes Playwright in a separate Chrome session, captures all crops including the same-layout literal mutation, records CDP accessible text/fonts, and runs the normalized pixel comparator. It always closes its session and server. Build, export and browser stdout remain in portable `.txt` files under the output directory. No Fluent bundle is vendored.

The retained evidence used Chromium 154/Linux, prescribed Segoe UI/sans-serif with actual Liberation Sans, EN/FR, 390/1440 px, and normal/forced colors. These are controlled production-markup fixtures, with no authenticated command egress, Windows Segoe UI, human AT, NVDA, Firefox or WebKit claim.

The fresh review reproduction produced byte-identical PNGs to the retained crops. `review-reproduction/deduplicated-crops.json` maps those fresh files to their existing byte-identical parent artifacts; the fresh run metadata, raw output and normalized pixel report are retained separately.
