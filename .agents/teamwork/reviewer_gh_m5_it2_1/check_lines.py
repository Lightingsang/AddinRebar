import os

root_dir = r"HPGrasshopper/HPGrasshopper.E2E.Tests"
cs_files = []
for root, dirs, files in os.walk(root_dir):
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

cs_files.sort()
print(f"Total C# files found: {len(cs_files)}")
over_300 = []
over_230 = []

for path in cs_files:
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = len(lines)
    rel = os.path.relpath(path, root_dir)
    print(f"{count:4d} lines : {rel}")
    if count > 300:
        over_300.append((rel, count))
    elif count > 230:
        over_230.append((rel, count))

print("=" * 60)
print(f"Violations > 300 lines (Rule C2): {len(over_300)}")
for r, c in over_300:
    print(f"  FAILED: {r} has {c} lines")

print(f"Target warning > 230 lines: {len(over_230)}")
for r, c in over_230:
    print(f"  TARGET > 230: {r} has {c} lines")
