import json

with open("scratch/revit_tang1_beams.json", encoding="utf-8") as f:
    beams = json.load(f)

updates = []

for b in beams:
    mark = b["mark"]
    b_id = b["id"]
    current_type = b["typeName"]
    
    target_type = None
    target_z_offset = 0.0 # mm relative to Level Tầng 1 (-100mm)
    
    if "DK.a'" in mark:
        target_type = "B_100x300"
        # COTE -1.000m relative to Level -0.100m -> -900mm
        target_z_offset = -900.0
    elif "DK.a" in mark:
        target_type = "B_100x300"
        target_z_offset = 0.0
    elif "DK.b" in mark:
        target_type = "B_200x300"
        target_z_offset = 0.0

    if target_type:
        updates.append({
            "id": b_id,
            "mark": mark,
            "currentType": current_type,
            "targetType": target_type,
            "targetZOffsetMm": target_z_offset,
            "typeChanged": current_type != target_type,
            "offsetChanged": target_z_offset != 0.0
        })

print(f"Total beams to update: {len(updates)}")
type_changes = [u for u in updates if u["typeChanged"]]
offset_changes = [u for u in updates if u["offsetChanged"]]

print(f"Beams requiring Type change: {len(type_changes)}")
print(f"Beams requiring Z-Offset change: {len(offset_changes)}")

with open("scratch/beam_updates_plan.json", "w", encoding="utf-8") as f:
    json.dump(updates, f, indent=2, ensure_ascii=False)

print("\nBreakdown by Mark:")
by_mark = {}
for u in updates:
    m = "DK.a'" if "DK.a'" in u["mark"] else ("DK.a" if "DK.a" in u["mark"] else "DK.b")
    by_mark.setdefault(m, []).append(u)

for k, items in by_mark.items():
    print(f"  {k} ({len(items)} beams): -> {items[0]['targetType']}, zOffset={items[0]['targetZOffsetMm']}mm")
    types_before = set(i["currentType"] for i in items)
    print(f"    Types before: {types_before}")
