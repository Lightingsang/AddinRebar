import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import os

scratch_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch"
for fname in os.listdir(scratch_dir):
    if "footing" in fname.lower():
        fpath = os.path.join(scratch_dir, fname)
        size = os.path.getsize(fpath)
        print(f"\n--- {fname} ({size} bytes) ---")
        try:
            with open(fpath, 'r', encoding='utf-8') as f:
                data = json.load(f)
            if isinstance(data, dict):
                print(f"  Keys: {list(data.keys())[:10]}")
                for k, v in data.items():
                    if isinstance(v, list):
                        print(f"    Key '{k}': list of {len(v)} items")
                    elif isinstance(v, (int, float, str)):
                        print(f"    Key '{k}': {v}")
            elif isinstance(data, list):
                print(f"  List of {len(data)} items")
        except Exception as e:
            print(f"  Error reading: {e}")
