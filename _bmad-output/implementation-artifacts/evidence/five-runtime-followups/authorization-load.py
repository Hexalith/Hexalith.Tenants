"""Qualify one named test under two observed CPU workers; retain machine-readable evidence."""
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

METHOD = "Detail_lifecycle_actions_fail_closed_while_authorization_is_pending"


def validate_case(xml_path):
    assemblies = ET.parse(xml_path).getroot().findall("assembly")
    if len(assemblies) != 1:
        raise ValueError("Exactly one test assembly must execute")
    assembly = assemblies[0]
    expected = {"total": "1", "passed": "1", "failed": "0", "skipped": "0", "not-run": "0", "errors": "0"}
    if any(assembly.get(key) != value for key, value in expected.items()):
        raise ValueError("Exactly one passing, unskipped, non-aborted case is required")
    cases = assembly.findall(".//test")
    if len(cases) != 1 or cases[0].get("result") != "Pass" or cases[0].get("method") != METHOD:
        raise ValueError("The exact named authorization case must pass")
    return {"executed": 1, "passed": 1, "skipped": 0, "method": METHOD}


def validate_progress(before, after):
    if len(before) != 2 or len(after) != 2:
        raise ValueError("Exactly two observed workers are required")
    for previous, current in zip(before, after):
        if not previous["alive"] or not current["alive"] or previous["pid"] != current["pid"]:
            raise ValueError("Both workers must remain alive across the iteration")
        if current["iterations"] <= previous["iterations"] or current["cpuSeconds"] <= previous["cpuSeconds"]:
            raise ValueError("Both workers must show actual CPU activity during the iteration")


def complete(results):
    return len(results) == 5 and all(result["exitCode"] == 0 and result["case"]["passed"] == 1 for result in results)


def worker(index, core, directory):
    os.sched_setaffinity(0, {core})
    path = directory / f"worker-{index}.json"
    temporary = directory / f"worker-{index}.tmp"
    iterations = 0
    previous = 0
    while True:
        sum(range(10000))
        iterations += 1
        if time.monotonic() - previous >= 0.1:
            data = {"pid": os.getpid(), "affinity": sorted(os.sched_getaffinity(0)),
                    "iterations": iterations, "cpuSeconds": time.process_time()}
            temporary.write_text(json.dumps(data) + "\n")
            temporary.replace(path)
            previous = time.monotonic()


def main():
    root = Path(__file__).resolve().parents[4]
    output = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent
    output.mkdir(parents=True, exist_ok=True)
    core = min(os.sched_getaffinity(0))
    workers = []
    results = []
    failure = None

    def snapshots():
        values = []
        for index, process in enumerate(workers, 1):
            data = json.loads((output / f"worker-{index}.json").read_text())
            if data["pid"] != process.pid or data["affinity"] != [core]:
                raise ValueError("Worker identity and single-CPU affinity must match")
            values.append({**data, "alive": process.poll() is None})
        return values

    try:
        for index in (1, 2):
            workers.append(subprocess.Popen([sys.executable, str(Path(__file__).resolve()), "--worker", str(index), str(core), str(output)],
                                            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
        deadline = time.monotonic() + 5
        while True:
            try:
                ready = snapshots()
                if len(ready) == 2 and all(item["alive"] and item["iterations"] > 0 for item in ready):
                    break
            except (FileNotFoundError, ValueError):
                pass
            if time.monotonic() >= deadline:
                raise TimeoutError("Both CPU workers must report ready and active before qualification")
            time.sleep(0.05)
        for iteration in range(1, 6):
            before = snapshots()
            xml_path = output / f"authorization-load-{iteration}.xml"
            xml_path.unlink(missing_ok=True)
            command = ["taskset", "-c", str(core), str(root / "tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests"),
                       "-noLogo", "-parallelMode", "none", "-method", f"*{METHOD}", "-xml", str(xml_path)]
            started = time.monotonic()
            result = subprocess.run(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, timeout=60)
            (output / f"authorization-load-{iteration}.log").write_text(result.stdout)
            case = validate_case(xml_path)
            after = snapshots()
            validate_progress(before, after)
            record = {"iteration": iteration, "command": command, "exitCode": result.returncode, "case": case,
                      "workersBefore": before, "workersAfter": after, "elapsedSeconds": round(time.monotonic() - started, 3)}
            results.append(record)
            print(json.dumps(record), flush=True)
            if result.returncode:
                raise RuntimeError("The named test process failed")
    except Exception as error:
        failure = f"{type(error).__name__}: {error}"
    finally:
        for process in workers:
            if process.poll() is None:
                process.terminate()
        for process in workers:
            process.wait()
        passed = failure is None and complete(results)
        (output / "authorization-load.json").write_text(json.dumps({"passed": passed, "failure": failure, "core": core,
            "cpuWorkers": 2, "bound": "same single allowed CPU for two observed workers and test process", "results": results}, indent=2) + "\n")
    return 0 if passed else 1


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--worker":
        worker(int(sys.argv[2]), int(sys.argv[3]), Path(sys.argv[4]))
    else:
        sys.exit(main())
