import os, re

root_dir = r"HPGrasshopper/HPGrasshopper.E2E.Tests"
cs_files = []
for root, dirs, files in os.walk(root_dir):
    if "obj" in root or "bin" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

cs_files.sort()
print("All actual catch blocks in source code (excluding string literals):")
for p in cs_files:
    rel = os.path.relpath(p, root_dir)
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
    for i, line in enumerate(lines, 1):
        if re.search(r"\bcatch\b", line) and '"' not in line:
            # print line and next 3 lines
            snippet = "".join([lines[j] for j in range(i-1, min(len(lines), i+4))])
            print(f"--- {rel}:{i} ---")
            print(snippet.strip())
