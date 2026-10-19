import json

with open("scratch/story1_grade_beams_model.json", "r", encoding="utf-8") as f:
    model = json.load(f)

updated_count = 0
for b in model["beams"]:
    name = b.get("Name", "")
    mark = b.get("Mark", "")
    
    if "DK.a'" in mark or name == "DK.a'":
        b["TypeName"] = "B_100x300"
        b["WidthMm"] = 100
        b["HeightMm"] = 300
        b["TopElevationMm"] = -1000
        b["ZOffsetMm"] = -900
        updated_count += 1
    elif "DK.a" in mark or name == "DK.a":
        b["TypeName"] = "B_100x300"
        b["WidthMm"] = 100
        b["HeightMm"] = 300
        updated_count += 1
    elif "DK.b" in mark or name == "DK.b":
        b["TypeName"] = "B_200x300"
        b["WidthMm"] = 200
        b["HeightMm"] = 300
        updated_count += 1

with open("scratch/story1_grade_beams_model.json", "w", encoding="utf-8") as f:
    json.dump(model, f, indent=2, ensure_ascii=False)

print(f"Updated {updated_count} beams in story1_grade_beams_model.json")
