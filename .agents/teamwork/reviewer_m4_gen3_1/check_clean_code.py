import os
import re

revit_src = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Revit"

files_over_300 = []
methods_over_50 = []
unannotated_catches = []
dead_code_lines = []

for root, dirs, files in os.walk(revit_src):
    if "obj" in root or "bin" in root:
        continue
    for f in files:
        if not f.endswith(".cs"):
            continue
        path = os.path.join(root, f)
        rel_path = os.path.relpath(path, revit_src)
        with open(path, "r", encoding="utf-8") as file:
            lines = file.readlines()
        
        # Rule C2: <= 300 lines
        line_count = len(lines)
        if line_count > 300:
            files_over_300.append((rel_path, line_count))

        # Rule Q-B2: catches annotated with // Reason:
        for idx, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith("catch") or (re.search(r"\bcatch\s*\(", stripped) and not stripped.startswith("//")):
                # Check next 3 lines or same line for // Reason:
                annotated = False
                for check_idx in range(idx, min(idx + 4, len(lines))):
                    if "// Reason:" in lines[check_idx]:
                        annotated = True
                        break
                if not annotated:
                    unannotated_catches.append((rel_path, idx + 1, stripped))

            # Rule Q-B1: commented-out code heuristics
            if stripped.startswith("//") and not stripped.startswith("///") and not stripped.startswith("// Reason:") and not stripped.startswith("// Priority:"):
                comment_body = stripped[2:].strip()
                if any(comment_body.startswith(kw) for kw in ["var ", "using ", "return ", "if (", "for (", "while (", "public ", "private ", "protected ", "throw "]):
                    dead_code_lines.append((rel_path, idx + 1, stripped))

print(f"Total files checked: {sum(len([f for f in files if f.endswith('.cs')]) for root, dirs, files in os.walk(revit_src) if 'obj' not in root)}")
print(f"Files > 300 lines (Rule C2): {len(files_over_300)}")
for f, lc in files_over_300:
    print(f"  - {f}: {lc} lines")

print(f"Unannotated catches (Rule Q-B2): {len(unannotated_catches)}")
for f, ln, c in unannotated_catches:
    print(f"  - {f}:{ln} -> {c}")

print(f"Potential dead code lines (Rule Q-B1): {len(dead_code_lines)}")
for f, ln, c in dead_code_lines:
    print(f"  - {f}:{ln} -> {c}")
