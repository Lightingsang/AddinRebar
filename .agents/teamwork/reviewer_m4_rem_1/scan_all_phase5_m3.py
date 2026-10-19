import os
import re

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'

phase5_patterns = [
    r'QuantitySurveying',
    r'DrawingLayout',
    r'CadUtilities',
    r'DimensionUtility',
    r'LayoutCommands',
    r'QsCommands',
    r'QsTakeoff',
    r'SheetArranger',
    r'Phase5'
]

def is_phase5(filepath):
    return any(re.search(pat, filepath, re.IGNORECASE) for pat in phase5_patterns)

def analyze_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    methods = []
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        trimmed = line.strip()
        is_candidate = False
        if re.match(r'^\s*(public|private|protected|internal|static|async|virtual|override|sealed)', line):
            if not re.search(r'\b(class|struct|interface|enum|namespace)\b', line):
                if '(' in line and not line.strip().endswith(';'):
                    is_candidate = True

        if is_candidate:
            start_line = i + 1
            header = trimmed
            brace_count = 0
            found_open = False
            j = i
            while j < n:
                l = lines[j]
                open_b = l.count('{')
                close_b = l.count('}')
                if open_b > 0 and not found_open:
                    found_open = True
                if found_open:
                    brace_count += open_b - close_b
                    if brace_count == 0:
                        end_line = j + 1
                        total_lines = end_line - start_line + 1
                        sig_match = re.search(r'([A-Za-z0-9_<>]+)\s*\(', header)
                        name = sig_match.group(1) if sig_match else header
                        methods.append((name, start_line, end_line, total_lines))
                        i = j
                        break
                j += 1
        i += 1
    return methods

hpkata_root = os.path.join(repo_root, "HPKata")
violations = []
for root, dirs, files in os.walk(hpkata_root):
    if "obj" in root or "bin" in root:
        continue
    for file in files:
        if file.endswith(".cs"):
            fpath = os.path.join(root, file)
            if is_phase5(fpath):
                meths = analyze_methods(fpath)
                for name, s, e, length in meths:
                    if length > 50:
                        rel = os.path.relpath(fpath, repo_root)
                        violations.append((rel, name, s, e, length))

print(f"Phase 5 Method Length (> 50 lines) Violations: {len(violations)}")
for rel, name, s, e, length in violations:
    print(f"  {rel} -> {name}: lines {s}-{e} ({length} lines)")
