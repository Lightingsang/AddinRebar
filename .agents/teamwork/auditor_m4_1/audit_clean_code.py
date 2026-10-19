import os
import re
import sys

BASE_DIRS = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI.Tests"
]

def get_cs_files():
    cs_files = []
    for base in BASE_DIRS:
        for root, dirs, files in os.walk(base):
            if "obj" in root.split(os.sep) or "bin" in root.split(os.sep):
                continue
            for f in files:
                if f.endswith(".cs"):
                    cs_files.append(os.path.join(root, f))
    return sorted(cs_files)

def audit_file(path):
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    total_lines = len(lines)
    rel_path = os.path.relpath(path, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")
    
    # Check D1/D4: Autodesk.* references
    autodesk_refs = []
    for idx, line in enumerate(lines, 1):
        if "Autodesk" in line:
            autodesk_refs.append((idx, line.strip()))

    # Check Q-B1: commented-out code
    # Heuristics: lines starting with // followed by code patterns like var, if, for, while, return, ;, {
    commented_code = []
    code_patterns = [
        re.compile(r'^\s*//\s*(var|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|return\s+|throw\s+|new\s+|Console\.|Debug\.)'),
        re.compile(r'^\s*//\s*(\w+\s*=\s*.+;|[A-Z]\w+\(.*\);)'),
        re.compile(r'^\s*/\*.*(var|if|return|;).*\*/', re.DOTALL)
    ]
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        # ignore comments that are documentation or section headers
        for cp in code_patterns:
            if cp.match(line):
                # check if it's descriptive text
                if not any(w in stripped.lower() for w in ["note", "example", "e.g.", "todo", "fixme", "see", "multi-version"]):
                    commented_code.append((idx, stripped))

    # Check Q-B2: unannotated catch blocks
    # e.g., catch { } or catch (Exception) { } with no body and no comment
    catch_blocks = []
    for idx, line in enumerate(lines, 1):
        m = re.search(r'catch(\s*\([^\)]*\))?\s*\{([^\}]*)\}', line)
        if m:
            body = m.group(2).strip()
            if not body or (len(body) == 0):
                catch_blocks.append((idx, "empty single-line catch"))
        elif re.search(r'catch(\s*\([^\)]*\))?\s*$', line.strip()):
            # multi-line catch
            # look ahead
            body_lines = []
            for j in range(idx, min(idx + 10, len(lines))):
                body_lines.append(lines[j])
                if "}" in lines[j]:
                    break
            joined = "".join(body_lines)
            if re.search(r'catch[^{]*\{\s*\}', joined):
                catch_blocks.append((idx, "empty multi-line catch without comment"))

    # Check M3: Method lengths
    # Parse simple method bodies using brace matching
    methods = []
    # Simple regex to find method headers
    method_header_re = re.compile(
        r'^\s*(?:(?:public|private|protected|internal|static|async|override|virtual|partial)\s+)+[\w\<\>\[\],\s\?]+\s+([A-Z]\w*)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
    )
    
    i = 0
    while i < len(lines):
        line = lines[i]
        match = method_header_re.match(line)
        if match:
            method_name = match.group(1)
            # Find the opening brace
            brace_count = 0
            start_line = i + 1
            j = i
            found_open = False
            while j < len(lines):
                for ch in lines[j]:
                    if ch == '{':
                        brace_count += 1
                        found_open = True
                    elif ch == '}':
                        brace_count -= 1
                if found_open and brace_count == 0:
                    end_line = j + 1
                    method_len = end_line - start_line + 1
                    methods.append((method_name, start_line, end_line, method_len))
                    i = j
                    break
                j += 1
        i += 1

    return {
        "file": rel_path,
        "total_lines": total_lines,
        "c2_pass": total_lines <= 300,
        "autodesk_refs": autodesk_refs,
        "commented_code": commented_code,
        "catch_blocks": catch_blocks,
        "methods": methods
    }

def main():
    files = get_cs_files()
    print(f"Total C# files found: {len(files)}")
    print("=" * 80)
    
    all_c2_pass = True
    all_m3_pass = True
    max_file_lines = 0
    max_file_name = ""
    max_method_lines = 0
    max_method_name = ""
    total_autodesk = 0
    total_commented_code = 0
    total_catch_issues = 0

    print(f"{'Lines':<6} | {'C2':<4} | {'Max M3':<6} | {'File'}")
    print("-" * 80)
    for p in files:
        res = audit_file(p)
        if res["total_lines"] > max_file_lines:
            max_file_lines = res["total_lines"]
            max_file_name = res["file"]
        if not res["c2_pass"]:
            all_c2_pass = False
        
        max_m = 0
        for m_name, s, e, m_len in res["methods"]:
            if m_len > max_m:
                max_m = m_len
            if m_len > max_method_lines:
                max_method_lines = m_len
                max_method_name = f"{res['file']}::{m_name} ({m_len} lines, L{s}-{e})"
            if m_len > 50:
                all_m3_pass = False
                print(f"  [M3 VIOLATION] {res['file']}::{m_name} is {m_len} lines (L{s}-{e})")

        total_autodesk += len(res["autodesk_refs"])
        total_commented_code += len(res["commented_code"])
        total_catch_issues += len(res["catch_blocks"])

        c2_str = "PASS" if res["c2_pass"] else "FAIL"
        print(f"{res['total_lines']:<6} | {c2_str:<4} | {max_m:<6} | {res['file']}")

        if res["autodesk_refs"]:
            print(f"  [AUTODESK REF]: {res['autodesk_refs']}")
        if res["commented_code"]:
            print(f"  [COMMENTED CODE]: {res['commented_code']}")
        if res["catch_blocks"]:
            print(f"  [CATCH ISSUE]: {res['catch_blocks']}")

    print("=" * 80)
    print(f"Total files audited: {len(files)}")
    print(f"Rule C2 (<= 300 lines): {'PASS' if all_c2_pass else 'FAIL'} (Max: {max_file_lines} in {max_file_name})")
    print(f"Rule M3 (<= 50 lines): {'PASS' if all_m3_pass else 'FAIL'} (Max: {max_method_lines} in {max_method_name})")
    print(f"Rule Q-B1 (0 dead code): {'PASS' if total_commented_code == 0 else 'FAIL'} ({total_commented_code} issues)")
    print(f"Rule Q-B2 (0 unannotated catch): {'PASS' if total_catch_issues == 0 else 'FAIL'} ({total_catch_issues} issues)")
    print(f"Rule D1/D4 (0 Autodesk refs): {'PASS' if total_autodesk == 0 else 'FAIL'} ({total_autodesk} refs)")

if __name__ == "__main__":
    main()
