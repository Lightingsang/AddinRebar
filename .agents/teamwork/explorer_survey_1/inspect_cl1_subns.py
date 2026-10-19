with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt", "r", encoding="utf-8") as f:
    lines = [l.strip() for l in f if l.strip()]

cl1_types = [l for l in lines if "ClassLibrary1" in l]
print(f"Total ClassLibrary1 types: {len(cl1_types)}")

cl1_subns = set()
for l in cl1_types:
    parts = l.split(" ")
    if len(parts) >= 2:
        fullname = parts[1]
        ns = ".".join(fullname.split(".")[:-1])
        cl1_subns.add(ns)

for ns in sorted(cl1_subns):
    count = sum(1 for l in cl1_types if ns in l)
    print(f"  {ns:45} : {count} types")
