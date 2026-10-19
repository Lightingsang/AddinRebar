import os
import re

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge"

print("=== LINE COUNTS SCAN (Rule C2: <= 300 lines) ===")
over_300 = []
all_files = []

for root, dirs, files in os.walk(bridge_dir):
    if "bin" in root or "obj" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, bridge_dir)
            with open(full_path, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
                count = len(lines)
                all_files.append((count, rel_path, full_path, lines))
                if count > 300:
                    over_300.append((count, rel_path))

all_files.sort(key=lambda x: x[0], reverse=True)
for count, rel_path, _, _ in all_files:
    status = "FAIL (>300)" if count > 300 else "OK"
    print(f"{count:4d} lines | {status:10s} | {rel_path}")

print("\nTotal .cs files scanned:", len(all_files))
print("Files > 300 lines:", len(over_300))

print("\n=== EMPTY CATCH BLOCK SCAN (Rule Q-B1) ===")
empty_catches = []
# Pattern looking for catch followed by braces with only whitespace or no statements
catch_pattern = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}", re.MULTILINE)

for count, rel_path, full_path, lines in all_files:
    content = "".join(lines)
    for match in catch_pattern.finditer(content):
        body = match.group(1).strip()
        # If body is empty or only whitespace
        if not body:
            empty_catches.append((rel_path, match.group(0)))
        elif not any(c.isalnum() for c in body):
            empty_catches.append((rel_path, match.group(0)))

print("Empty catch blocks found:", len(empty_catches))
for path, snippet in empty_catches:
    print(f"  {path}: {snippet}")

print("\n=== COMMENTED-OUT CODE SCAN (Rule Q-B2) ===")
commented_code = []
code_like_patterns = [
    re.compile(r"^\s*//\s*(?:var|if|for|foreach|while|return|throw|new|public|private|protected|internal|class|interface)\s+"),
    re.compile(r"^\s*//\s*[A-Za-z0-9_]+\s*=\s*"),
    re.compile(r"^\s*//\s*[A-Za-z0-9_.]+\s*\([^)]*\)\s*;"),
]

for count, rel_path, full_path, lines in all_files:
    for idx, line in enumerate(lines, 1):
        for pat in code_like_patterns:
            if pat.search(line):
                # Exclude regular explanatory comments or markdown/doc tags
                stripped = line.strip()
                if "///" in stripped or "e.g." in stripped.lower():
                    continue
                commented_code.append((rel_path, idx, stripped))

print("Potential commented-out code lines found:", len(commented_code))
for path, line_no, line in commented_code:
    print(f"  {path}:{line_no}: {line}")
