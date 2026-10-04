"""Compares two golden runs (or a baseline folder and a run): snapshots, input read-backs and result dialogs.

    python compare-golden.py <baseline-dir> <run-dir>

Fields that legitimately differ between runs (document path, Revit build) are dropped before comparing. Prints a unified
diff per differing file; exit 0 when every compared file is identical, 1 otherwise, 2 when a file is missing.
"""
import difflib
import json
import os
import sys

FILES = ["s0.json", "column.json", "foundation.json", "beam.json",
         "column.readback.json", "foundation.readback.json", "beam.readback.json",
         "column.dialog.txt", "foundation.dialog.txt", "beam.dialog.txt"]
IGNORED_KEYS = {"docPath", "revitBuild"}


def normalized(path):
    with open(path, encoding="utf-8-sig") as f:
        text = f.read()
    if not path.endswith(".json"):
        return text.replace("\r\n", "\n").strip().splitlines()
    data = json.loads(text)
    if isinstance(data, dict):
        data = {k: v for k, v in data.items() if k not in IGNORED_KEYS}
    return json.dumps(data, indent=1, ensure_ascii=False, sort_keys=True).splitlines()


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    left, right = sys.argv[1], sys.argv[2]
    status = 0
    for name in FILES:
        a, b = os.path.join(left, name), os.path.join(right, name)
        if not os.path.exists(a) and not os.path.exists(b):
            continue
        if not (os.path.exists(a) and os.path.exists(b)):
            print(f"MISSING {name}: {'left' if not os.path.exists(a) else 'right'}")
            status = max(status, 2)
            continue
        la, lb = normalized(a), normalized(b)
        if la == lb:
            print(f"SAME    {name}")
            continue
        status = max(status, 1)
        print(f"DIFF    {name}")
        for line in difflib.unified_diff(la, lb, fromfile=a, tofile=b, lineterm="", n=2):
            print("  " + line)
    print("identical" if status == 0 else "different")
    return status


if __name__ == "__main__":
    sys.exit(main())
