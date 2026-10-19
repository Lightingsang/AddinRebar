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

# Find pile clusters in piles_data
# Threshold for piles in same footing: d <= 1500mm
adj = defaultdict(list)
for i in range(len(piles)):
    for j in range(i + 1, len(piles)):
        p1 = piles[i]
        p2 = piles[j]
        d = math.hypot(p1["revit_x"] - p2["revit_x"], p1["revit_y"] - p2["revit_y"])
        if d <= 1500:
            adj[p1["stt"]].append(p2["stt"])
            adj[p2["stt"]].append(p1["stt"])

visited = set()
clusters = []
for p in piles:
    stt = p["stt"]
    if stt not in visited:
        cluster = []
        q = [stt]
        visited.add(stt)
        while q:
            curr = q.pop()
            cluster.append(curr)
            for neighbor in adj[curr]:
                if neighbor not in visited:
                    visited.add(neighbor)
                    q.append(neighbor)
        clusters.append(cluster)

print(f"Total pile clusters at 1500mm: {len(clusters)}")

# Calculate cluster center for each cluster
cluster_info = []
for c in clusters:
    p_objs = [pile_by_stt[s] for s in c]
    cx = sum(p["revit_x"] for p in p_objs) / len(p_objs)
    cy = sum(p["revit_y"] for p in p_objs) / len(p_objs)
    cluster_info.append({
        "piles": c,
        "count": len(c),
        "cx": cx,
        "cy": cy,
        "assigned": False
    })

# If any cluster has size > 6, split it (e.g. if two adjacent footings were within 1500mm)
# Let's check cluster counts
print("Cluster size breakdown:")
sz_counts = defaultdict(int)
for ci in cluster_info:
    sz_counts[ci["count"]] += 1
for sz in sorted(sz_counts.keys()):
    print(f"  {sz} piles: {sz_counts[sz]} clusters")

# Match each footing text to its best matching cluster of same pile count
# Sort footing texts by (Y, X)
type_idx = defaultdict(int)
final_footings = []

# For footings of count 1..6, match to nearest cluster of that size
unassigned_clusters = list(cluster_info)

for ft in sorted(footing_texts, key=lambda f: (f["text"], f["x"], f["y"])):
    t = ft["text"]
    spec = SPECS[t]
    req_piles = spec["piles"]
    fx = ft["x"]
    fy = ft["y"]

    # find best cluster with matching count
    matching_clusters = [c for c in unassigned_clusters if c["count"] == req_piles]
    if not matching_clusters:
        # fallback: find cluster of any count
        matching_clusters = unassigned_clusters

    best_c = min(matching_clusters, key=lambda c: (c["cx"] - fx)**2 + (c["cy"] - fy)**2)
    unassigned_clusters.remove(best_c)

    type_idx[t] += 1
    ft_id = f"{t}-{type_idx[t]:02d}"

    p_objs = [pile_by_stt[s] for s in best_c["piles"]]
    cx = best_c["cx"]
    cy = best_c["cy"]

    # Calculate bounding box
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
        "id": ft_id,
        "type": t,
        "pileCount": len(p_objs),
        "pileStts": sorted(best_c["piles"]),
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

print(f"\nFinal verified footings count: {len(final_footings)}")
out_file = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json"
with open(out_file, "w", encoding="utf-8") as f:
    json.dump({
        "totalFootings": len(final_footings),
        "totalPiles": sum(f["pileCount"] for f in final_footings),
        "classification": "NHOM_1",
        "footings": final_footings
    }, f, indent=2, ensure_ascii=False)

print(f"Wrote {out_file} successfully!")
