import os
import re

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge"

cs_files = []
for root, dirs, files in os.walk(bridge_dir):
    if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

print(f"Total .cs files examined: {len(cs_files)}")

# 1. Rule C2: File line count <= 300
files_over_300 = []
all_file_counts = []
for p in cs_files:
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
        count = len(lines)
        rel = os.path.relpath(p, bridge_dir)
        all_file_counts.append((rel, count))
        if count > 300:
            files_over_300.append((rel, count))

all_file_counts.sort(key=lambda x: -x[1])
print("\n--- Top 10 longest files ---")
for rel, count in all_file_counts[:10]:
    print(f"{count:4d} lines: {rel}")

print(f"\nRule C2 Violations (> 300 lines): {len(files_over_300)}")
for rel, count in files_over_300:
    print(f"  VIOLATION: {rel} ({count} lines)")

# 2. Rule Q-B1: Dead / commented-out code
# Comments like // ... ; or /* ... ; */ or lines containing C# code constructs commented out
commented_code_findings = []
code_pattern = re.compile(r"^\s*//\s*(?:(?:var|int|string|bool|return|if|else|for|foreach|while|using|new|class|public|private|protected|internal|void|throw|async|await)\s+|[a-zA-Z0-9_.]+\s*\(.*?\)\s*;|[a-zA-Z0-9_.]+\s*=\s*[^;]+;)")

for p in cs_files:
    rel = os.path.relpath(p, bridge_dir)
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
        for idx, line in enumerate(lines):
            # check for single-line commented out code
            m = code_pattern.match(line)
            if m:
                # exclude typical explanatory comments that might start with a word like 'using' or 'return'
                cleaned = line.strip().lstrip("/").strip()
                if cleaned.endswith(";") or " = " in cleaned or cleaned.startswith("return "):
                    commented_code_findings.append((rel, idx + 1, line.strip()))

print(f"\nRule Q-B1 Potential Commented Code Findings: {len(commented_code_findings)}")
for rel, line_no, content in commented_code_findings:
    print(f"  {rel}:{line_no}: {content}")

# 3. Rule Q-B2: Unannotated empty catch blocks
# catch (...) {} or catch {} without explanation comment inside or preceding
empty_catch_findings = []
catch_pattern = re.compile(r"catch\s*(?:\([^\)]*\))?\s*\{([^\}]*)\}", re.DOTALL)

for p in cs_files:
    rel = os.path.relpath(p, bridge_dir)
    with open(p, "r", encoding="utf-8") as f:
        content = f.read()
        # Find all catch blocks
        for m in re.finditer(r"catch\s*(?:\([^\)]*\))?\s*\{([^\}]*)\}", content):
            body = m.group(1).strip()
            # If body has no non-whitespace or no comment explaining why
            if not body:
                # Find line number
                line_no = content[:m.start()].count("\n") + 1
                empty_catch_findings.append((rel, line_no, "Empty catch block with no content/comment"))
            elif not re.search(r"[a-zA-Z0-9]", body):
                # Only whitespace or symbols
                line_no = content[:m.start()].count("\n") + 1
                empty_catch_findings.append((rel, line_no, "Catch block without code or word comment"))

print(f"\nRule Q-B2 Empty Catch Findings: {len(empty_catch_findings)}")
for rel, line_no, desc in empty_catch_findings:
    print(f"  {rel}:{line_no}: {desc}")

