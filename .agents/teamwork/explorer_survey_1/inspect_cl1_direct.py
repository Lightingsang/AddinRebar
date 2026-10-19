with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt", "r", encoding="utf-8") as f:
    lines = [l.strip() for l in f if l.strip()]

cl1_direct = []
for l in lines:
    parts = l.split(" ")
    if len(parts) >= 2:
        fullname = parts[1]
        tokens = fullname.split(".")
        if len(tokens) == 2 and tokens[0] == "ClassLibrary1":
            cl1_direct.append(l)

print(f"Direct types in ClassLibrary1: {len(cl1_direct)}")
for t in sorted(cl1_direct)[:100]:
    print("  " + t)
