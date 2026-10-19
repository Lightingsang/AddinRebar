import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'r', encoding='utf-8') as f:
    spans = json.load(f)

print(f"Total spans: {len(spans)}")
zero_lens = [s for s in spans if s['len'] < 100.0]
print(f"Zero or short spans (<100mm): {len(zero_lens)}")
for s in spans[:5]:
    print(f"  {s['name']}: ({s['x1']}, {s['y1']}) -> ({s['x2']}, {s['y2']}), len={s['len']}mm, b={s['bMm']}, h={s['hMm']}")

sections = {}
for s in spans:
    sec = f"{s['bMm']}x{s['hMm']}"
    sections[sec] = sections.get(sec, 0) + 1
print("Spans by cross section:", sections)
