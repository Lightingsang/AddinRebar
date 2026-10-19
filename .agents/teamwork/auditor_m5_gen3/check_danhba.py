import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

for fname in ['danhba_lenh.json', 'danhba_cad.json', 'danhba_dce_thep.json', 'danhba_mbkc.json', 'danhba_cad_api.json']:
    p = rf'C:\Dce Pro\AiAgent\DanhBa\{fname}'
    with open(p, 'r', encoding='utf-8') as f:
        d = json.load(f)
    print(f"\n--- {fname} ---")
    if isinstance(d, dict):
        print(f"dict with keys: {list(d.keys())}")
        for k in d:
            if isinstance(d[k], (list, dict)):
                print(f"  {k}: len {len(d[k])}")
    elif isinstance(d, list):
        print(f"list with {len(d)} items")
        if d:
            print(f"  sample: {list(d[0].keys()) if isinstance(d[0], dict) else type(d[0])}")
