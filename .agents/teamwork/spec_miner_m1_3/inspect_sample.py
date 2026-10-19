import sys
import json

sys.stdout.reconfigure(encoding='utf-8')

def inspect_sample(path, count=3):
    with open(path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    items = data.get('muc', [])
    print(f"File {path}: Total items = {len(items)}")
    for i in range(min(count, len(items))):
        print(f"--- Item {i+1} ---")
        for k, v in items[i].items():
            print(f"  {k}: {repr(v)[:100]}")

inspect_sample(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json')
inspect_sample(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json')
inspect_sample(r'C:\Dce Pro\AiAgent\DanhBa\danhba_mbkc.json')
