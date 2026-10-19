import os
import re

seed_dir = r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary"
seeds = []
for root, dirs, files in os.walk(seed_dir):
    for f in files:
        if f == "code.cs":
            seeds.append(os.path.join(root, f))

print(f"Found {len(seeds)} embedded seeds:")
all_pass = True
for s in seeds:
    rel = os.path.relpath(s, seed_dir)
    with open(s, "r", encoding="utf-8") as fp:
        lines = fp.readlines()
        count = len(lines)
        has_commented_code = False
        has_empty_catch = False
        for idx, line in enumerate(lines, 1):
            st = line.strip()
            if st.startswith("//") and not st.startswith("///"):
                comment = st[2:].strip()
                if re.search(r';\s*$', comment) and any(kw in comment for kw in ['var ', 'int ', 'return ', 'new ', 'if (']):
                    has_commented_code = True
            if re.match(r'^\s*catch\s*(\([^)]*\))?\s*\{\s*\}\s*$', st):
                has_empty_catch = True
        
        status = "PASS" if count <= 300 and not has_commented_code and not has_empty_catch else "FAIL"
        if status == "FAIL":
            all_pass = False
        print(f"  [{status}] {rel}: {count} lines, commented_code={has_commented_code}, empty_catch={has_empty_catch}")

print(f"\nALL 12 SEEDS ADR-0007 PASS: {all_pass}")
