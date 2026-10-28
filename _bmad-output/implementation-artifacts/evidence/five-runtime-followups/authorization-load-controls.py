"""Verify the qualification runner rejects missing tests and absent CPU load."""
import importlib.util
from pathlib import Path
import tempfile

spec = importlib.util.spec_from_file_location("authorization_load", Path(__file__).with_name("authorization-load.py"))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


def rejects(operation):
    try:
        operation()
    except ValueError:
        return
    raise RuntimeError("Invalid qualification was accepted")


with tempfile.TemporaryDirectory() as directory:
    path = Path(directory) / "case.xml"
    valid = ('<assemblies><assembly total="1" passed="1" failed="0" skipped="0" not-run="0" errors="0">'
             f'<collection><test method="{runner.METHOD}" result="Pass"/></collection></assembly></assemblies>')
    path.write_text(valid)
    runner.validate_case(path)
    for original, replacement in [("total=\"1\"", "total=\"0\""), ("skipped=\"0\"", "skipped=\"1\""),
                                  ("not-run=\"0\"", "not-run=\"1\""), (runner.METHOD, "AnotherMethod"),
                                  ("result=\"Pass\"", "result=\"Fail\"")]:
        path.write_text(valid.replace(original, replacement))
        rejects(lambda: runner.validate_case(path))

before = [{"pid": index, "alive": True, "iterations": 1, "cpuSeconds": 0.1} for index in (1, 2)]
after = [{**item, "iterations": 2, "cpuSeconds": 0.2} for item in before]
runner.validate_progress(before, after)
rejects(lambda: runner.validate_progress(before, before))
rejects(lambda: runner.validate_progress(before, [{**item, "alive": False} for item in after]))
rejects(lambda: runner.validate_progress(before, after[:1]))
if runner.complete([]) or runner.complete([{"exitCode": 0, "case": {"passed": 1}}] * 4):
    raise RuntimeError("Incomplete qualification was accepted")
print("PASS: valid controls accepted; empty, skipped, aborted, wrong, failed or incomplete tests and dead/inactive workers rejected.")
