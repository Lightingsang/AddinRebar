import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', encoding='utf-8') as f:
    data = json.load(f)

footings = data['footings']
all_stts = []
for ft in footings:
    all_stts.extend(ft['pileStts'])

print(f"Total pile entries in all footings: {len(all_stts)}")
print(f"Unique pile entries: {len(set(all_stts))}")
from collections import Counter
counts = Counter(all_stts)
duplicates = {k: v for k, v in counts.items() if v > 1}
print(f"Duplicates: {duplicates}")

missing_1_to_196 = set(range(1, 197)) - set(all_stts)
print(f"Missing from 1..196: {sorted(missing_1_to_196)}")
