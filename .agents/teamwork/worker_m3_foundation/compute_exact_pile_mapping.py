import json
import sys
import math
from collections import defaultdict

sys.stdout.reconfigure(encoding="utf-8")

# 1. Load piles
with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)
piles = piles_data.get("piles", [])

# 2. Load footing texts
with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\cad_true_foundation_elements.json", "r", encoding="utf-8") as f:
    cad_el = json.load(f)
footing_texts = cad_el.get("footingTexts", [])

SPECS = {
    "ĐC-1":  {"piles": 1, "B": 700,  "L": 700,  "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-2":  {"piles": 2, "B": 900,  "L": 1600, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-2'": {"piles": 2, "B": 900,  "L": 1600, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-3":  {"piles": 3, "B": 1000, "L": 2200, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 300},
    "ĐC-3'": {"piles": 3, "B": 1000, "L": 2200, "H": 1000, "topZ": -1.600, "botZ": -2.600, "pileTopZ": -2.500, "mac": 400},
    "ĐC-4":  {"piles": 4, "B": 1600, "L": 1600, "H": 1200, "topZ": -1.600, "botZ": -2.800, "pileTopZ": -2.700, "mac": 300},
    "ĐC-5":  {"piles": 5, "B": 1600, "L": 2500, "H": 1200, "topZ": -1.600, "botZ": -2.800, "pileTopZ": -2.700, "mac": 300},
    "ĐC-6":  {"piles": 6, "B": 1600, "L": 2500, "H": 1300, "topZ": -1.600, "botZ": -2.900, "pileTopZ": -2.800, "mac": 400},
}

# Footings
footings = []
type_idx = defaultdict(int)
for ft in sorted(footing_texts, key=lambda f: (f["y"], f["x"])):
    t = ft["text"]
    type_idx[t] += 1
    ft_id = f"{t}-{type_idx[t]:02d}"
    spec = SPECS[t]
    footings.append({
        "id": ft_id,
        "type": t,
        "x": ft["x"],
        "y": ft["y"],
        "cap": spec["piles"],
        "spec": spec
    })

slots = []
for f_idx, ft in enumerate(footings):
    for s in range(ft["cap"]):
        slots.append((f_idx, ft["x"], ft["y"]))

N = len(piles) # 196
cost = []
for i in range(N):
    px = piles[i]["revit_x"]
    py = piles[i]["revit_y"]
    row = []
    for j in range(N):
        f_idx, fx, fy = slots[j]
        d2 = (px - fx)**2 + (py - fy)**2
        row.append(d2)
    cost.append(row)

def hungarian(cost_matrix):
    n = len(cost_matrix)
    u = [0] * (n + 1)
    v = [0] * (n + 1)
    p = [0] * (n + 1)
    way = [0] * (n + 1)
    for i in range(1, n + 1):
        p[0] = i
        j0 = 0
        minv = [float('inf')] * (n + 1)
        used = [False] * (n + 1)
        while True:
            used[j0] = True
            i0 = p[j0]
            delta = float('inf')
            j1 = 0
            for j in range(1, n + 1):
                if not used[j]:
                    cur = cost_matrix[i0 - 1][j - 1] - u[i0] - v[j]
                    if cur < minv[j]:
                        minv[j] = cur
                        way[j] = j0
                    if minv[j] < delta:
                        delta = minv[j]
                        j1 = j
            for j in range(n + 1):
                if used[j]:
                    u[p[j]] += delta
                    v[j] -= delta
                else:
                    minv[j] -= delta
            j0 = j1
            if p[j0] == 0:
                break
        while True:
            j1 = way[j0]
            p[j0] = p[j1]
            j0 = j1
            if j0 == 0:
                break
    ans = [0] * n
    for j in range(1, n + 1):
        ans[p[j] - 1] = j - 1
    return ans

assignment = hungarian(cost)

pile_mapping = []
elev_counts = defaultdict(int)
type_counts = defaultdict(int)

for pile_idx, slot_idx in enumerate(assignment):
    f_idx, fx, fy = slots[slot_idx]
    ft = footings[f_idx]
    p = piles[pile_idx]
    pile_top_z = ft["spec"]["pileTopZ"]
    elev_counts[pile_top_z] += 1
    type_counts[ft["type"]] += 1
    d = math.hypot(p["revit_x"] - fx, p["revit_y"] - fy)
    pile_mapping.append({
        "pile_idx": pile_idx,
        "x": p["revit_x"],
        "y": p["revit_y"],
        "footing_id": ft["id"],
        "footing_type": ft["type"],
        "pile_top_z": pile_top_z,
        "dist_to_text": round(d, 1)
    })

print(f"Total piles mapped: {len(pile_mapping)}")
print("Pile counts by Footing Type:")
for t, cnt in sorted(type_counts.items()):
    print(f"  {t}: {cnt}")
print("Pile counts by Pile Top Elevation:")
for el, cnt in sorted(elev_counts.items()):
    print(f"  {el}m: {cnt}")

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\exact_196_pile_mapping.json", "w", encoding="utf-8") as f:
    json.dump(pile_mapping, f, indent=2, ensure_ascii=False)
print("Saved exact_196_pile_mapping.json")
