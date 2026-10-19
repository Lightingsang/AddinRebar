import re

file_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_3\proposed_CanvasGraphRunner.cs"

with open(file_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

print(f"Total lines in proposed file: {len(lines)}")
assert len(lines) <= 300, f"File exceeds 300 lines: {len(lines)}"

i = 0
violations = []
while i < len(lines):
    line = lines[i].strip()
    m = re.search(r'(?:public|private|internal|protected)\s+(?:static\s+|async\s+|override\s+|sealed\s+|virtual\s+)*([\w\<\>\[\],\?\.\s]+)\s+([A-Za-z0-9_]+)\s*\(', line)
    if m:
        method_name = m.group(2) if m.lastindex >= 2 else m.group(1)
        start_line = i + 1
        brace_idx = i
        while brace_idx < len(lines) and "{" not in lines[brace_idx]:
            brace_idx += 1
        if brace_idx < len(lines) and "{" in lines[brace_idx]:
            depth = 0
            end_line = -1
            for j in range(brace_idx, len(lines)):
                depth += lines[j].count("{") - lines[j].count("}")
                if depth == 0:
                    end_line = j + 1
                    break
            if end_line != -1:
                total_lines = end_line - start_line + 1
                body_lines = (end_line - 1) - (brace_idx + 1) + 1
                status = "PASS" if total_lines <= 50 else ("PASS (body<=50)" if body_lines <= 50 else "OVER_50")
                print(f"  {method_name} (L{start_line}-L{end_line}): total={total_lines}, body={body_lines} [{status}]")
                if total_lines > 50:
                    violations.append((method_name, total_lines, body_lines))
                i = end_line
                continue
    i += 1

if violations:
    print(f"FAILED: {len(violations)} methods over 50 lines: {violations}")
else:
    print("SUCCESS: ALL methods are <= 50 lines (both total and body)!")
