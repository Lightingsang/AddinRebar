with open(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DCE_Pro_entities.txt", "r", encoding="utf-8") as f:
    lines = [l.strip() for l in f if l.strip()]

ns_map = {}
for l in lines:
    parts = l.split(" ")
    if len(parts) >= 2:
        fullname = parts[1]
        ns = ".".join(fullname.split(".")[:-1]) if "." in fullname else "<global>"
        ns_map[ns] = ns_map.get(ns, 0) + 1

print(f"Total entities: {len(lines)}")
print(f"Total namespaces: {len(ns_map)}")
print("\nTop 40 Namespaces in DCE Pro.dll:")
for ns, count in sorted(ns_map.items(), key=lambda x: -x[1])[:40]:
    print(f"  {ns:50} : {count}")
