with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt", "r", encoding="utf-8") as f:
    lines = [l.strip() for l in f if l.strip()]

autostr_types = [l for l in lines if "ClassLibrary1.AutoStr" in l]
print(f"Total AutoStr types: {len(autostr_types)}")

# Look for top-level non-closure classes in AutoStr
non_closure = [t for t in autostr_types if not "+_" in t and not "Closure" in t]
print(f"Non-closure AutoStr classes: {len(non_closure)}")
for t in sorted(non_closure)[:60]:
    print("  " + t)
