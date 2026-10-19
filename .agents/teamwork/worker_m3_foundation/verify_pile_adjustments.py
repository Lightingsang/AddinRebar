import json
import sys

sys.stdout.reconfigure(encoding="utf-8")

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\exact_196_pile_mapping.json", "r", encoding="utf-8") as f:
    mapping = json.load(f)

with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\revit_live_survey_summary.json", "r", encoding="utf-8") as f:
    revit_piles = json.load(f)["piles"]["items"]

print(f"Mapping entries: {len(mapping)}")
print(f"Revit piles: {len(revit_piles)}")

matched_piles = []
for rp in revit_piles:
    rx, ry = rp["xMm"], rp["yMm"]
    best_m = None
    best_d = 1e9
    for m in mapping:
        d = ((rx - m["x"])**2 + (ry - m["y"])**2)**0.5
        if d < best_d:
            best_d = d
            best_m = m
    assert best_d < 1.0, f"Pile at ({rx}, {ry}) not matched (dist={best_d})"
    matched_piles.append({
        "id": rp["id"],
        "x": rx,
        "y": ry,
        "currentZ": rp["zMm"],
        "targetZ": int(best_m["pile_top_z"] * 1000),
        "targetOffsetMm": int(best_m["pile_top_z"] * 1000) - (-1000), # level is -1000mm
        "footingId": best_m["footing_id"],
        "footingType": best_m["footing_type"]
    })

print(f"All {len(matched_piles)} piles matched to exact target elevations!")

# Check how many piles need elevation adjustment
changes = [p for p in matched_piles if p["currentZ"] != p["targetZ"]]
print(f"Piles requiring elevation adjustment: {len(changes)}")
for c in changes[:5]:
    print(f"  Pile ID {c['id']} at ({c['x']}, {c['y']}): current {c['currentZ']}mm -> target {c['targetZ']}mm (under {c['footingType']} {c['footingId']})")

target_z_counts = {}
for p in matched_piles:
    z = p["targetZ"]
    target_z_counts[z] = target_z_counts.get(z, 0) + 1
print("Target Z counts:", target_z_counts)
