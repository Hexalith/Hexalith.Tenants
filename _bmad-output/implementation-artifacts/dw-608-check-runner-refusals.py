"""Check that stale XML and unrelated fixture failures cannot qualify DW-608 replay."""

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


def main():
    """Seed old reports, then require both deliberately broken replays to fail."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed-directory", required=True, type=Path)
    parser.add_argument("--output-directory", type=Path, default=Path(tempfile.gettempdir()) / "dw-608-runner-refusals")
    args = parser.parse_args()
    if not (args.seed_directory / "results.json").is_file():
        raise ValueError("Run the normal variant checker successfully before seeding stale reports.")
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=True)
    repo = Path(__file__).resolve().parents[2]
    runner = repo / "_bmad-output/implementation-artifacts/dw-608-check-variants.py"
    with tempfile.TemporaryDirectory(prefix="dw-608-runner-refusals-") as temporary:
        root = Path(temporary)
        stale = root / "stale"
        shutil.copytree(args.seed_directory, stale)
        tool_directory = root / "tools"
        tool_directory.mkdir()
        wrapper = tool_directory / "dotnet"
        wrapper.write_bytes(b'#!/usr/bin/env bash\ncase "$*" in build\\ *|*production-control.xml*) exec "$DW608_REAL_DOTNET" "$@";; *) exit 1;; esac\n')
        wrapper.chmod(0o700)
        env = dict(os.environ, DW608_REAL_DOTNET=shutil.which("dotnet"), PATH=str(tool_directory) + os.pathsep + os.environ["PATH"])
        with (output / "stale-refusal.log").open("w") as log:
            result = subprocess.run([sys.executable, str(runner), "--output-directory", str(stale)],
                                    env=env, stdout=log, stderr=subprocess.STDOUT, timeout=180, check=False)
        if result.returncode == 0 or (stale / "results.json").exists() or "FileNotFoundError" not in (output / "stale-refusal.log").read_text():
            raise RuntimeError("Stale reports or summary were accepted.")
        print("Stale XML refused; prior summary invalidated.", flush=True)

        source = runner.read_text()
        before = '("bypass-comparison", replace_once(publisher, comparison, ""), 19)'
        after = '("bypass-comparison", replace_once(publisher, comparison, \'  if [ "$actual_hash" != "$expected_hash" ]; then FIXTURE_RESPONSE=hash-mismatch; fi\\n\'), 19)'
        if source.count(before) != 1:
            raise ValueError("Expected one bypass mutation anchor.")
        source = "__file__ = " + repr(str(runner)) + "\n" + source.replace(before, after)
        with (output / "mechanism-refusal.log").open("w") as log:
            result = subprocess.run([sys.executable, "-c", source, "--output-directory", str(root / "mechanism")],
                                    stdout=log, stderr=subprocess.STDOUT, timeout=180, check=False)
        evidence = (output / "mechanism-refusal.log").read_text()
        if result.returncode == 0 or '"failed": 19' not in evidence or "Unrelated fixture failure" not in evidence or (root / "mechanism/results.json").exists():
            raise RuntimeError("Unrelated fixture failures qualified mutation evidence.")
        print("Expected aggregate totals with unrelated fixture failures refused.", flush=True)


if __name__ == "__main__":
    main()
