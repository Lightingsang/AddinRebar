import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\revit_live_survey_summary.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

beams = data['beams']['items']
print(f"Total beams in backup: {len(beams)}")
sample = beams[:5]
for b in sample:
    print(f"ID {b['id']}: {b['type']}, Mark='{b['mark']}', Comments='{b['comments']}', Level='{b['level']}', Z={b['zMm']}, ({b['x1']}, {b['y1']}) -> ({b['x2']}, {b['y2']}), Len={b['lenMm']}")

z_summary = {}
for b in beams:
    z_summary[b['zMm']] = z_summary.get(b['zMm'], 0) + 1
print("Beams by Z elevation:", z_summary)

marks = [b['mark'] for b in beams if b['mark']]
print(f"Sample marks: {marks[:10]}")
comments = [b['comments'] for b in beams if b['comments']]
print(f"Sample comments: {comments[:10]}")
