import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    d = json.load(f)

for ft in d['footings']:
    if ft['id'] in ['ĐC-2-04', 'ĐC-6-01', 'ĐC-6-04', 'ĐC-2-19', 'ĐC-2-10', 'ĐC-3-04', 'ĐC-3-03']:
        print(f"{ft['id']}: center=({ft['centerX']}, {ft['centerY']}), size=({ft['widthX']}, {ft['widthY']}), pileStts={ft['pileStts']}")
        for v in ft['vertices']:
            print(f"   v: ({v['x']}, {v['y']})")
