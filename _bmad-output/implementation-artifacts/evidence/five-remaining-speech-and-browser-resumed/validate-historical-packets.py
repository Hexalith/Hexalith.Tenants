"""Replay scoped exact-byte historical packet conservation against entry blobs."""
import hashlib
import json
from pathlib import Path


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]


def main():
    record = json.loads((HERE / "historical-packet-conservation.json").read_bytes())
    for item in record["files"]:
        path = ROOT / item["path"]
        if not path.resolve().is_relative_to(ROOT.resolve()):
            raise ValueError("Historical path escapes workspace")
        data = path.read_bytes()
        actual = hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
        if actual != item["baselineBlob"] or actual != item["workingBytesBlob"]:
            raise ValueError("Historical packet bytes changed: " + item["path"])
    print(json.dumps({"canonicalBaseline": record["canonicalBaseline"],
                      "scopedHistoricalFiles": len(record["files"]), "exactBytesEqual": True}, indent=2))


if __name__ == "__main__":
    main()
