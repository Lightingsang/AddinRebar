import os
import re

HPDYNAMO_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo"

def check_clean_code():
    c2_violations = [] # files > 300 lines
    m3_violations = [] # methods > 50 lines
    qb1_violations = [] # unannotated empty catch
    qb2_violations = [] # commented-out code

    csharp_files = []
    for root, dirs, files in os.walk(HPDYNAMO_DIR):
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                csharp_files.append(os.path.join(root, f))

    print(f"Total C# files scanned: {len(csharp_files)}")

    for filepath in csharp_files:
        rel_path = os.path.relpath(filepath, HPDYNAMO_DIR)
        with open(filepath, "r", encoding="utf-8", errors="replace") as file:
            lines = file.readlines()

        # Rule C2: File length <= 300
        if len(lines) > 300:
            c2_violations.append((rel_path, len(lines)))

        # Rule Q-B1: Empty catch blocks
        # Looking for catch (...) { } without comment inside
        content = "".join(lines)
        empty_catch_pattern = re.compile(r'catch\s*\([^\)]*\)\s*\{\s*\}', re.MULTILINE)
        for m in empty_catch_pattern.finditer(content):
            # find line number
            lineno = content[:m.start()].count('\n') + 1
            qb1_violations.append((rel_path, lineno, m.group(0)))

        # Rule Q-B2: Commented-out code
        # Look for single line comments that look like code statements (ending with ;, { or }, or var/return/if/for/while etc.)
        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith("//"):
                comment_text = stripped[2:].strip()
                # Check if comment looks like commented out code
                # Exclude standard doc or regular comments
                if re.match(r'^(var|return|if|for|while|await|using|throw|public|private|internal|protected|class|interface|record)\s+[a-zA-Z0-9_]', comment_text) or \
                   re.match(r'^[a-zA-Z0-9_]+\s*\([^)]*\)\s*;', comment_text) or \
                   (comment_text.endswith(";") and not comment_text.startswith("TODO") and not comment_text.startswith("NOTE") and not comment_text.startswith("e.g.") and not comment_text.startswith("Multi-version:")):
                    # check if it's descriptive text
                    if not any(word in comment_text.lower() for word in ["example", "sample", "see", "rule", "todo", "note", "multi-version", "reason", "ref:", "url", "http"]):
                        qb2_violations.append((rel_path, idx, stripped))

        # Rule M3: Method length <= 50 lines
        # Simple heuristic: scan method blocks by brace counting
        in_method = False
        method_name = ""
        method_start = 0
        brace_depth = 0
        method_brace_start = 0

        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            # check method declaration
            # pattern for method header
            if not in_method:
                if re.match(r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|\s+)*\s+([A-Za-z0-9_<>?,\[\]]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(where\s+.*)?$', line):
                    # candidate method
                    method_name = line.strip()
                    method_start = idx
                elif '{' in line and method_name and method_brace_start == 0:
                    in_method = True
                    method_brace_start = brace_depth + 1
            
            brace_depth += line.count('{') - line.count('}')

            if in_method and brace_depth < method_brace_start:
                method_len = idx - method_start + 1
                if method_len > 50:
                    m3_violations.append((rel_path, method_name, method_start, idx, method_len))
                in_method = False
                method_name = ""
                method_brace_start = 0

    print(f"\n--- Clean Code Scan Results ---")
    print(f"Rule C2 (File <= 300 lines) Violations: {len(c2_violations)}")
    for v in c2_violations:
        print(f"  [C2] {v[0]}: {v[1]} lines")

    print(f"\nRule M3 (Method <= 50 lines) Violations: {len(m3_violations)}")
    for v in m3_violations:
        print(f"  [M3] {v[0]}: {v[1]} ({v[2]}-{v[3]}, {v[4]} lines)")

    print(f"\nRule Q-B1 (Unannotated empty catch) Violations: {len(qb1_violations)}")
    for v in qb1_violations:
        print(f"  [Q-B1] {v[0]}:{v[1]}: {v[2]}")

    print(f"\nRule Q-B2 (Commented-out code) Violations: {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  [Q-B2] {v[0]}:{v[1]}: {v[2]}")

if __name__ == "__main__":
    check_clean_code()
