import json
from pathlib import Path

danhba_dir = Path(r"C:\Dce Pro\AiAgent\DanhBa")

for json_file in sorted(danhba_dir.glob("*.json")):
    try:
        with open(json_file, "r", encoding="utf-8") as f:
            data = json.load(f)
            if isinstance(data, list):
                print(f"{json_file.name}: list of {len(data)} items")
                if len(data) > 0 and isinstance(data[0], dict):
                    print("  Sample keys:", list(data[0].keys())[:8])
            elif isinstance(data, dict):
                print(f"{json_file.name}: dict with {len(data)} top-level keys")
                sample_k = list(data.keys())[:5]
                print("  Sample keys:", sample_k)
                first_val = data[sample_k[0]]
                if isinstance(first_val, list):
                    print(f"  Sample value length: {len(first_val)}")
    except Exception as e:
        print(f"Error reading {json_file.name}: {e}")
