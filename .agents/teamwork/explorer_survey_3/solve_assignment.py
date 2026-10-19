import json, sys, math
from collections import defaultdict

sys.stdout.reconfigure(encoding="utf-8")

# 1. Load piles
with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json", "r", encoding="utf-8") as f:
    piles_data = json.load(f)
piles = piles_data.get("piles", [])
pile_by_stt = {p["stt"]: p for p in piles}

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

print(f"Footings: {len(footings)}, Total Cap: {sum(f['cap'] for f in footings)}, Total Piles: {len(piles)}")

# Expand footings into 196 slots
slots = []
for f_idx, ft in enumerate(footings):
    for s in range(ft["cap"]):
        slots.append((f_idx, ft["x"], ft["y"]))

N = len(piles) # 196
# Cost matrix: C[i][j] = dist^2 from pile i to slot j
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

# Solve min-cost assignment using simple auction / Successive Shortest Path (Hungarian)
# Since N=196 is small, standard O(V^3) Hungarian algorithm runs in < 0.2s!
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

print("Solving 196x196 assignment with Hungarian algorithm...")
assignment = hungarian(cost)
print("Solved!")

# Group assigned piles by footing
footing_assigned_piles = defaultdict(list)
max_dist = 0
total_dist = 0
for pile_idx, slot_idx in enumerate(assignment):
    f_idx, fx, fy = slots[slot_idx]
    stt = piles[pile_idx]["stt"]
    footing_assigned_piles[f_idx].append(stt)
    d = math.hypot(piles[pile_idx]["revit_x"] - fx, piles[pile_idx]["revit_y"] - fy)
    if d > max_dist: max_dist = d
    total_dist += d

print(f"Max dist pile to text: {max_dist:.1f}mm, Avg dist: {total_dist / N:.1f}mm")

# Build final 72 footings with complete geometry
final_footings = []
for f_idx, ft in enumerate(footings):
    spec = ft["spec"]
    assigned_stts = sorted(footing_assigned_piles[f_idx])
    p_objs = [pile_by_stt[s] for s in assigned_stts]
    cx = sum(p["revit_x"] for p in p_objs) / len(p_objs)
    cy = sum(p["revit_y"] for p in p_objs) / len(p_objs)

    min_px = min(p["revit_x"] for p in p_objs)
    max_px = max(p["revit_x"] for p in p_objs)
    min_py = min(p["revit_y"] for p in p_objs)
    max_py = max(p["revit_y"] for p in p_objs)

    # Orientation
    if (max_px - min_px) > (max_py - min_py):
        hx = max(spec["L"] / 2.0, (max_px - min_px) / 2.0 + 350.0)
        hy = max(spec["B"] / 2.0, (max_py - min_py) / 2.0 + 350.0)
    else:
        hx = max(spec["B"] / 2.0, (max_px - min_px) / 2.0 + 350.0)
        hy = max(spec["L"] / 2.0, (max_py - min_py) / 2.0 + 350.0)

    min_x = round((cx - hx) / 10.0) * 10.0
    max_x = round((cx + hx) / 10.0) * 10.0
    min_y = round((cy - hy) / 10.0) * 10.0
    max_y = round((cy + hy) / 10.0) * 10.0

    width_x = max_x - min_x
    width_y = max_y - min_y

    vertices = [
        {"x": round(min_x, 1), "y": round(min_y, 1)},
        {"x": round(max_x, 1), "y": round(min_y, 1)},
        {"x": round(max_x, 1), "y": round(max_y, 1)},
        {"x": round(min_x, 1), "y": round(max_y, 1)}
    ]

    final_footings.append({
        "id": ft["id"],
        "type": ft["type"],
        "pileCount": len(p_objs),
        "pileStts": assigned_stts,
        "topElevation": spec["topZ"],
        "bottomElevation": spec["botZ"],
        "thicknessMm": spec["H"],
        "pileTopElevation": spec["pileTopZ"],
        "concreteGrade": f"Mác {spec['mac']}",
        "centerX": round(cx, 1),
        "centerY": round(cy, 1),
        "widthX": round(width_x, 1),
        "widthY": round(width_y, 1),
        "areaM2": round((width_x * width_y) / 1e6, 3),
        "vertices": vertices
    })

out_file = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json"
with open(out_file, "w", encoding="utf-8") as f:
    json.dump({
        "totalFootings": len(final_footings),
        "totalPiles": sum(f["pileCount"] for f in final_footings),
        "classification": "NHOM_1",
        "footings": final_footings
    }, f, indent=2, ensure_ascii=False)

print(f"\nSuccessfully wrote {out_file} with {len(final_footings)} footings and {sum(f['pileCount'] for f in final_footings)} piles!")

by_type = defaultdict(list)
for f in final_footings:
    by_type[f["type"]].append(f)

print("\n--- SUMMARY OF 72 VERIFIED FOOTINGS (NHÓM 1) ---")
print(f"{'Type':<8} | {'Count':<5} | {'Piles/Cap':<9} | {'Total Piles':<11} | {'Top Z':<7} | {'Bot Z':<7} | {'Pile Top Z':<10} | {'Dim BxL (mm)':<15} | {'Mác BT'}")
print("-" * 105)
for t in ["ĐC-1", "ĐC-2", "ĐC-2'", "ĐC-3", "ĐC-3'", "ĐC-4", "ĐC-5", "ĐC-6"]:
    flist = by_type[t]
    sample = flist[0]
    total_p = sum(x["pileCount"] for x in flist)
    dim_str = f"{sample['widthX']}x{sample['widthY']}"
    print(f"{t:<8} | {len(flist):<5} | {sample['pileCount']:<9} | {total_p:<11} | {sample['topElevation']:<7.3f} | {sample['bottomElevation']:<7.3f} | {sample['pileTopElevation']:<10.3f} | {dim_str:<15} | {sample['concreteGrade']}")

print("-" * 105)
print(f"TOTAL:   | {len(final_footings):<5} |           | {sum(f['pileCount'] for f in final_footings):<11} |         |         |            |                 |")
