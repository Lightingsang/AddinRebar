import json

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)

piles = piles_data.get("piles", [])
stts = [p["stt"] for p in piles]
print(f"Total piles in piles_true_bim_model.json: {len(piles)}")
print(f"Unique stts: {len(set(stts))}")
from collections import Counter
c = Counter(stts)
dups = {k: v for k, v in c.items() if v > 1}
print(f"Duplicate stts: {dups}")
