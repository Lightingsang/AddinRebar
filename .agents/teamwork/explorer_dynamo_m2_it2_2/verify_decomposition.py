import re

path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m2_it2_2\proposed_CanvasGraphRunner.cs"
with open(path, "r", encoding="utf-8") as fp:
    lines = fp.readlines()

print(f"Total file lines: {len(lines)}")

i = 0
all_pass = True
methods = []
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
                if status != "PASS":
                    all_pass = False
                methods.append((method_name, start_line, end_line, total_lines, body_lines, status))
                print(f"  {method_name} (L{start_line}-L{end_line}): total={total_lines}, body={body_lines} [{status}]")
                i = end_line
                continue
    i += 1

print(f"Total methods found: {len(methods)}")
print(f"ALL METHODS PURE PASS: {all_pass}")
