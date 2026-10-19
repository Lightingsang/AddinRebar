import os
import json
import glob

danhba_dir = r"C:\Dce Pro\AiAgent\DanhBa"
files = glob.glob(os.path.join(danhba_dir, "*"))

print(f"Total files in DanhBa: {len(files)}")
for f in sorted(files):
    fname = os.path.basename(f)
    size = os.path.getsize(f)
    print(f"\n--- File: {fname} ({size:,} bytes) ---")
    if fname.endswith(".json"):
        try:
            with open(f, "r", encoding="utf-8") as fp:
                data = json.load(fp)
            if isinstance(data, list):
                print(f"Type: List, Count: {len(data)}")
                if len(data) > 0:
                    sample = data[0]
                    if isinstance(sample, dict):
                        print(f"Keys in sample: {list(sample.keys())}")
                        # print first 2 items
                        for i in range(min(2, len(data))):
                            print(f"Sample [{i}]: {json.dumps(data[i], ensure_ascii=False)[:200]}")
                    else:
                        print(f"Sample [0]: {sample}")
            elif isinstance(data, dict):
                print(f"Type: Dict, Keys: {list(data.keys())}")
                for k in list(data.keys())[:10]:
                    val = data[k]
                    if isinstance(val, list):
                        print(f"  Key '{k}': List with {len(val)} items")
                        if len(val) > 0 and isinstance(val[0], dict):
                            print(f"    Sample item keys: {list(val[0].keys())}")
                    elif isinstance(val, dict):
                        print(f"  Key '{k}': Dict with {len(val)} keys: {list(val.keys())[:5]}")
                    else:
                        print(f"  Key '{k}': {type(val).__name__} = {str(val)[:100]}")
        except Exception as e:
            print(f"Error reading {fname}: {e}")
    else:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        print(f"Text file, {len(lines)} lines")
        for l in lines[:5]:
            print(f"  {l.strip()}")
