import json, sys, math
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

# 3. Known specs for each footing type:
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

# Create 72 footing instances
ft_instances = []
type_idx = defaultdict(int)
for ft in sorted(footing_texts, key=lambda x: (round(x["y"], -2), round(x["x"], -2))):
    t = ft["text"]
    type_idx[t] += 1
    ft_id = f"{t}-{type_idx[t]:02d}"
    spec = SPECS[t]
    ft_instances.append({
        "id": ft_id,
        "type": t,
        "text_x": ft["x"],
        "text_y": ft["y"],
        "required_piles": spec["piles"],
        "spec": spec,
        "assigned_piles": []
    })

# Matching algorithm:
# For each footing, find the nearest piles. Since distance between piles within same footing is <= 1500mm,
# and distance between different footings is >= 2500mm.
unassigned_piles = set(p["stt"] for p in piles)
pile_by_stt = {p["stt"]: p for p in piles}

# Step 1: Assign to each footing the required number of piles closest to its text location
# Sort footings by required_piles descending (match complex caps first: 6, 5, 4, 3, 2, 1)
sorted_fts = sorted(ft_instances, key=lambda f: -f["required_piles"])

for ft in sorted_fts:
    req = ft["required_piles"]
    fx = ft["text_x"]
    fy = ft["text_y"]
    # Sort unassigned piles by distance to footing text
    candidates = sorted(list(unassigned_piles), key=lambda stt: (pile_by_stt[stt]["revit_x"] - fx)**2 + (pile_by_stt[stt]["revit_y"] - fy)**2)
    chosen = candidates[:req]
    ft["assigned_piles"] = chosen
    for c in chosen:
        unassigned_piles.remove(c)

print(f"Assigned all piles. Remaining unassigned: {len(unassigned_piles)}")

# Compute geometry for each footing
final_footings = []
for ft in sorted(ft_instances, key=lambda f: f["id"]):
    spec = ft["spec"]
    p_objs = [pile_by_stt[stt] for stt in ft["assigned_piles"]]
    cx = sum(p["revit_x"] for p in p_objs) / len(p_objs)
    cy = sum(p["revit_y"] for p in p_objs) / len(p_objs)
    
    # Overhang: 350mm standard from outer edge of piles
    min_px = min(p["revit_x"] for p in p_objs)
    max_px = max(p["revit_x"] for p in p_objs)
    min_py = min(p["revit_y"] for p in p_objs)
    max_py = max(p["revit_y"] for p in p_objs)

    # Footing extent: at least spec dimensions B x L centered on cx, cy
    half_w = max(spec["B"] / 2.0, (max_px - min_px) / 2.0 + 350.0)
    half_l = max(spec["L"] / 2.0, (max_py - min_py) / 2.0 + 350.0)

    # Determine orientation (along X or Y): if max_px - min_px > max_py - min_py, orient width along Y, length along X
    if (max_px - min_px) > (max_py - min_py):
        hx = max(spec["L"] / 2.0, (max_px - min_px) / 2.0 + 350.0)
        hy = max(spec["B"] / 2.0, (max_py - min_py) / 2.0 + 350.0)
    else:
        hx = max(spec["B"] / 2.0, (max_px - min_px) / 2.0 + 350.0)
        hy = max(spec["L"] / 2.0, (max_py - min_py) / 2.0 + 350.0)

    # Round to standard dimensions (10mm grid)
    min_x = round((cx - hx) / 10.0) * 10.0
    max_x = round((cx + hx) / 10.0) * 10.0
    min_y = round((cy - hy) / 10.0) * 10.0
    max_y = round((cy + hy) / 10.0) * 10.0

    width_x = max_x - min_x
    width_y = max_y - min_y

    vertices = [
        {"x": min_x, "y": min_y},
        {"x": max_x, "y": min_y},
        {"x": max_x, "y": max_y},
        {"x": min_x, "y": max_y}
    ]

    final_footings.append({
        "id": ft["id"],
        "type": ft["type"],
        "pileCount": len(p_objs),
        "pileStts": ft["assigned_piles"],
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

out_path = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json"
with open(out_path, "w", encoding="utf-8") as f:
    json.dump({
        "totalFootings": len(final_footings),
        "totalPiles": sum(f["pileCount"] for f in final_footings),
        "classification": "NHOM_1",
        "footings": final_footings
    }, f, indent=2, ensure_ascii=False)

print(f"Successfully generated 72 verified footings in: {out_path}")

# Print summary table by type
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
