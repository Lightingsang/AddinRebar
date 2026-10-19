import os
import sys

HPDYNAMO_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo"

def run_checks():
    c2_violations = [] # > 300 lines
    m3_violations = [] # > 50 lines
    qb1_violations = [] # unannotated empty catch
    qb2_violations = [] # commented out code
    
    cs_files = []
    for root, dirs, files in os.walk(HPDYNAMO_DIR):
        if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
            continue
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))

    print(f"Total C# files scanned: {len(cs_files)}", flush=True)

    for path in cs_files:
        rel = os.path.relpath(path, HPDYNAMO_DIR)
        with open(path, "r", encoding="utf-8", errors="replace") as fp:
            lines = fp.readlines()

        # Rule C2: File lines <= 300
        if len(lines) > 300:
            c2_violations.append((rel, len(lines)))

        # Rule Q-B1: Empty catch
        for i, line in enumerate(lines):
            s = line.strip()
            if s.startswith("catch") and "{" in s and "}" in s:
                # check if there is comment inside { }
                inside = s[s.find("{")+1:s.rfind("}")].strip()
                if not inside or not (inside.startswith("//") or inside.startswith("/*")):
                    qb1_violations.append((rel, i+1, s))

        # Rule Q-B2: Commented out code
        for i, line in enumerate(lines):
            s = line.strip()
            if s.startswith("//"):
                comment = s[2:].strip()
                # Check if it looks like commented-out statements
                keywords = ["var ", "return ", "if (", "for (", "while (", "await ", "throw new ", "public class ", "private void "]
                if any(comment.startswith(kw) for kw in keywords):
                    # Check if it's documentation/notes
                    if not any(token in comment.lower() for token in ["example", "sample", "note", "rule", "todo", "multi-version", "ref:"]):
                        qb2_violations.append((rel, i+1, s))

        # Rule M3: Simple method scan
        # Detect methods by tracking braces and declaration lines
        in_body = False
        brace_level = 0
        method_name = ""
        method_start = 0

        for i, line in enumerate(lines):
            stripped = line.strip()
            
            # Simple header detection (has parentheses, no semicolon, access modifier)
            if not in_body and brace_level == 1:
                if any(stripped.startswith(acc) for acc in ["public ", "private ", "protected ", "internal "]) and "(" in stripped and not stripped.endswith(";"):
                    method_name = stripped
                    method_start = i + 1

            if "{" in line:
                brace_level += line.count("{")
                if method_name and not in_body and brace_level >= 2:
                    in_body = True

            if "}" in line:
                brace_level -= line.count("}")
                if in_body and brace_level <= 1:
                    length = (i + 1) - method_start + 1
                    if length > 50:
                        m3_violations.append((rel, method_name[:40], method_start, i+1, length))
                    in_body = False
                    method_name = ""

    print(f"\n--- Clean Code Scan Results ---", flush=True)
    print(f"Rule C2 (>300 lines): {len(c2_violations)}")
    for v in c2_violations:
        print(f"  [C2] {v[0]}: {v[1]} lines")

    print(f"Rule M3 (>50 lines): {len(m3_violations)}")
    for v in m3_violations:
        print(f"  [M3] {v[0]}: {v[1]} (lines {v[2]}-{v[3]}, total {v[4]})")

    print(f"Rule Q-B1 (Unannotated empty catch): {len(qb1_violations)}")
    for v in qb1_violations:
        print(f"  [Q-B1] {v[0]}:{v[1]} -> {v[2]}")

    print(f"Rule Q-B2 (Commented out code): {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  [Q-B2] {v[0]}:{v[1]} -> {v[2]}")

if __name__ == "__main__":
    run_checks()
