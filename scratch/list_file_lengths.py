import os

target = r"HPGrasshopper/HPGrasshopper.McpBridge"
files = []
for r, _, fs in os.walk(target):
    if "bin" in r or "obj" in r: continue
    for f in fs:
        if f.endswith(".cs"):
            files.append(os.path.join(r, f))

files.sort()
print(f"Total C# files: {len(files)}")
print("-" * 60)
for p in files:
    with open(p, "r", encoding="utf-8") as f:
        count = len(f.readlines())
    rel = os.path.relpath(p, target)
    flag = " [FAIL > 300]" if count > 300 else " [OK]"
    print(f"{count:3d} lines: {rel:<45}{flag}")
print("-" * 60)
max_file = max(files, key=lambda p: len(open(p, "r", encoding="utf-8").readlines()))
print(f"Max file: {max_file} ({len(open(max_file, 'r', encoding='utf-8').readlines())} lines)")
