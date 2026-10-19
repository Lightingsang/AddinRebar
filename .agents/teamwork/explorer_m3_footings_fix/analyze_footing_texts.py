import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
from collections import Counter

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_true_foundation_elements.json', 'r', encoding='utf-8') as f:
    cad_el = json.load(f)

texts = cad_el['footingTexts']
print(f"Total footing texts: {len(texts)}")

counts = Counter(t['text'] for t in texts)
print("\nFooting text counts by type:")
for k, v in sorted(counts.items()):
    print(f"  {k}: {v}")

SPECS = {
    "ĐC-1":  {"piles": 1, "B": 700,  "L": 700,  "H": 1000},
    "ĐC-2":  {"piles": 2, "B": 900,  "L": 1600, "H": 1000},
    "ĐC-2'": {"piles": 2, "B": 900,  "L": 1600, "H": 1000},
    "ĐC-3":  {"piles": 3, "B": 1000, "L": 2200, "H": 1000},
    "ĐC-3'": {"piles": 3, "B": 1000, "L": 2200, "H": 1000},
    "ĐC-4":  {"piles": 4, "B": 1600, "L": 1600, "H": 1200},
    "ĐC-5":  {"piles": 5, "B": 1600, "L": 2500, "H": 1200},
    "ĐC-6":  {"piles": 6, "B": 1600, "L": 2500, "H": 1300},
}

total_expected_piles = sum(counts[k] * SPECS[k]["piles"] for k in counts)
print(f"\nTotal expected piles across all 72 footings: {total_expected_piles}")
