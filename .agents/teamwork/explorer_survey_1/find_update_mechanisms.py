with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt", "r", encoding="utf-8") as f:
    lines = [l.strip() for l in f if l.strip()]

updates = [l for l in lines if "update" in l.lower()]
print(f"Total update entities in DCE Pro.dll: {len(updates)}")
for u in updates[:40]:
    print("  " + u)
