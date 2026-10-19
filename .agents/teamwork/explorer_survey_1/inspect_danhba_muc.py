import sys
import json
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

danhba_dir = Path(r"C:\Dce Pro\AiAgent\DanhBa")

for json_file in sorted(danhba_dir.glob("*.json")):
    with open(json_file, "r", encoding="utf-8") as f:
        data = json.load(f)
        muc = data.get("muc", [])
        print(f"\n=== {json_file.name} ===")
        print(f"Total entries in 'muc': {len(muc)}")
        if len(muc) > 0:
            sample = muc[0]
            print("Sample entry keys:", list(sample.keys()))
            print(f"Sample 'ten': {sample.get('ten')}, 'lop': {sample.get('lop')}, 'ham': {sample.get('ham')}")
