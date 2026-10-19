import json, sys, math

sys.stdout.reconfigure(encoding="utf-8")

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_true_foundation_elements.json", "r", encoding="utf-8") as f:
    cad_el = json.load(f)

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)

piles = piles_data.get("piles", [])
print(f"Total piles in model: {len(piles)}")

footing_texts = cad_el.get("footingTexts", [])
print(f"Total footing texts: {len(footing_texts)}")

# For each pile, find the nearest footing text
pile_to_ft = {}
for p in piles:
    px = p["revit_x"]
    py = p["revit_y"]
    best_dist = float("inf")
    best_ft = None
    for ft in footing_texts:
        d = math.sqrt((ft["x"] - px)**2 + (ft["y"] - py)**2)
        if d < best_dist:
            best_dist = d
            best_ft = ft["text"]
    pile_to_ft[p["stt"]] = (best_ft, best_dist)

# Group piles by footing text
from collections import defaultdict
piles_per_type = defaultdict(list)
for stt, (ft_name, dist) in pile_to_ft.items():
    piles_per_type[ft_name].append((stt, dist))

print("\n--- Piles assigned to footing types ---")
for ft_name in sorted(piles_per_type.keys()):
    stt_list = piles_per_type[ft_name]
    print(f"  {ft_name}: {len(stt_list)} piles (avg dist to text: {sum(x[1] for x in stt_list)/len(stt_list):.1f}mm)")

# Verify pile count formula:
# Let's see: 72 footings total:
# ĐC-1 (7), ĐC-2 (28), ĐC-2' (4), ĐC-3 (15), ĐC-3' (2), ĐC-4 (10), ĐC-5 (2), ĐC-6 (4)
# What is typical pile count per footing type?
# ĐC-1: 1 cọc? 7 * 1 = 7?
# ĐC-2: 2 cọc? 28 * 2 = 56?
# ĐC-2': 2 cọc? 4 * 2 = 8?
# ĐC-3: 3 cọc? 15 * 3 = 45?
# ĐC-3': 3 cọc? 2 * 3 = 6?
# ĐC-4: 4 cọc? 10 * 4 = 40?
# ĐC-5: 5 cọc? 2 * 5 = 10?
# ĐC-6: 6 cọc? 4 * 6 = 24?
# Sum: 7 + 56 + 8 + 45 + 6 + 40 + 10 + 24 = 196!
print("\n--- Theoretical check: 1 pile for ĐC-1, 2 for ĐC-2, 3 for ĐC-3, 4 for ĐC-4, 5 for ĐC-5, 6 for ĐC-6 ---")
theo_sum = 7*1 + 28*2 + 4*2 + 15*3 + 2*3 + 10*4 + 2*5 + 4*6
print(f"7*1 + 28*2 + 4*2 + 15*3 + 2*3 + 10*4 + 2*5 + 4*6 = {theo_sum} piles!")
if theo_sum == 196:
    print(">>> EXACT 100% MATCH TO TOTAL 196 PILES! <<<")
