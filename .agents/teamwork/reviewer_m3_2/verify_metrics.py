import os

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge"

over_300 = []
all_files = []

for root, dirs, files in os.walk(bridge_dir):
    if "obj" in root or "bin" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            full_path = os.path.join(root, f)
            with open(full_path, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
            all_files.append((f, len(lines), full_path))
            if len(lines) > 300:
                over_300.append((f, len(lines), full_path))

print(f"Total C# files checked: {len(all_files)}")
print(f"Files > 300 lines: {len(over_300)}")
for f, count, p in over_300:
    print(f"  {f}: {count} lines")

# Check longest files
all_files.sort(key=lambda x: x[1], reverse=True)
print("\nTop 5 longest files:")
for f, count, _ in all_files[:5]:
    print(f"  {f}: {count} lines")
