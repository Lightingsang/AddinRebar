import json, math, sys
from collections import defaultdict

sys.stdout.reconfigure(encoding="utf-8")

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)
piles = piles_data.get("piles", [])

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_true_foundation_elements.json", "r", encoding="utf-8") as f:
    cad_el = json.load(f)
footing_texts = cad_el.get("footingTexts", [])

# For each footing text, find the piles that are within 1800mm of the text
ft_piles = []
for ft in footing_texts:
    fx = ft["x"]
    fy = ft["y"]
    # find piles within 1800mm of text
    near_piles = [p for p in piles if math.hypot(p["revit_x"] - fx, p["revit_y"] - fy) <= 2200]
    ft_piles.append({
        "text": ft["text"],
        "x": fx,
        "y": fy,
        "piles": [p["stt"] for p in near_piles],
        "count": len(near_piles)
    })

counts = defaultdict(int)
for f in ft_piles:
    counts[(f["text"], f["count"])] += 1

print("Footing type and pile count within 2200mm:")
for k, v in sorted(counts.items()):
    print(f"  {k[0]} with {k[1]} piles: {v} footings")
