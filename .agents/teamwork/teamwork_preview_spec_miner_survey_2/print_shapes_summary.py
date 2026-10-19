import json

with open('full_shapes_analysis.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

print(f"{'Shape':10} | {'Legs':20} | {'WMF Texts':30} | {'Geometry Summary'}")
print("-" * 85)
for k in sorted(data.keys()):
    v = data[k]
    legs = ', '.join(v['legs']) if v['legs'] else 'NONE'
    wmf_txt = ', '.join(v['wmf_texts']) if v['wmf_texts'] else 'NONE'
    print(f"{k:10} | {legs:20} | {wmf_txt:30} | {v['wmf_summary']}")
