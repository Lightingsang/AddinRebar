import os, re

prod_dirs = [
    r"HPGrasshopper/HPGrasshopper.McpBridge",
    r"HPGrasshopper/HPGrasshopper.Mcp.Server"
]

prod_files = []
for d in prod_dirs:
    for root, dirs, files in os.walk(d):
        if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
            continue
        for f in files:
            if f.endswith(".cs"):
                prod_files.append(os.path.join(root, f))

print(f"Total production files: {len(prod_files)}")

nie_findings = []
for p in prod_files:
    with open(p, "r", encoding="utf-8", errors="ignore") as fp:
        for idx, line in enumerate(fp, 1):
            if "NotImplementedException" in line:
                nie_findings.append((p, idx, line.strip()))

print(f"NotImplementedException findings: {len(nie_findings)}")
for p, idx, l in nie_findings:
    print(f"  {p}:{idx}: {l}")
