import json
import sys
from collections import defaultdict

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\gm_beams_plan_summary.json', 'r', encoding='utf-8') as f:
    items = json.load(f)

print(f"Total GM instances in gm_beams_plan_summary.json: {len(items)}")
by_name = defaultdict(list)
for it in items:
    by_name[it['name']].append(it)

print(f"Unique beam names: {len(by_name)}")
for name, lst in sorted(by_name.items()):
    sec = lst[0].get('section', '')
    print(f"  {name:<8}: {len(lst):<2} instances, section={sec}, sample pos=({lst[0]['x']}, {lst[0]['y']})")
