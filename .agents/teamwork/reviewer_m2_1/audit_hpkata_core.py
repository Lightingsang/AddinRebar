import os
import re
import sys

CORE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core"

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(CORE_DIR):
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return sorted(cs_files)

def audit_c2(cs_files):
    print("=== Rule C2: File Length <= 300 lines ===")
    violations = []
    file_stats = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        rel = os.path.relpath(f, CORE_DIR)
        count = len(lines)
        file_stats.append((count, rel))
        if count > 300:
            violations.append((rel, count))
    
    file_stats.sort(reverse=True)
    for count, rel in file_stats:
        status = "FAIL" if count > 300 else "PASS"
        print(f"[{status}] {count:3d} lines : {rel}")
    
    return violations

def audit_m3(cs_files):
    print("\n=== Rule M3: Method Length <= 50 lines ===")
    violations = []
    all_methods = []

    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        rel = os.path.relpath(f, CORE_DIR)

        # Basic state machine to find method boundaries
        # We look for lines containing method signatures followed by {
        i = 0
        n = len(lines)
        while i < n:
            line = lines[i]
            stripped = line.strip()

            # Ignore comments or directives
            if stripped.startswith("//") or stripped.startswith("/*") or stripped.startswith("#"):
                i += 1
                continue

            # Method signature pattern heuristic:
            # (public|private|protected|internal|static|async|virtual|override|sealed) ... ( ... )
            # Not class, interface, struct, enum, record, namespace
            is_decl = False
            decl_keywords = ["public", "private", "protected", "internal", "static", "async", "virtual", "override", "sealed"]
            type_keywords = ["class", "interface", "struct", "enum", "record", "namespace"]

            if any(stripped.startswith(k + " ") or (" " + k + " ") in stripped for k in decl_keywords):
                if not any((" " + tk + " ") in stripped or stripped.startswith(tk + " ") for tk in type_keywords):
                    # Check if there is '(' in this line or subsequent lines up to '{' or ';'
                    sig_lines = [stripped]
                    j = i
                    has_paren = "(" in stripped
                    ends_with_semi = stripped.endswith(";")
                    while not has_paren and not ends_with_semi and j + 1 < n and "{" not in lines[j]:
                        j += 1
                        sig_lines.append(lines[j].strip())
                        if "(" in lines[j]:
                            has_paren = True
                        if lines[j].strip().endswith(";"):
                            ends_with_semi = True
                            break

                    combined_sig = " ".join(sig_lines)
                    # Exclude property getters/setters or abstract/interface declarations ending with ;
                    if has_paren and not ends_with_semi and not any((" " + tk + " ") in combined_sig for tk in type_keywords):
                        # Look for opening brace '{'
                        brace_idx = j
                        found_brace = False
                        while brace_idx < n and brace_idx < j + 5:
                            if "{" in lines[brace_idx]:
                                found_brace = True
                                break
                            brace_idx += 1

                        if found_brace:
                            # Now track brace depth until 0
                            start_line = i + 1
                            brace_depth = 0
                            curr = brace_idx
                            while curr < n:
                                for ch in lines[curr]:
                                    if ch == '{':
                                        brace_depth += 1
                                    elif ch == '}':
                                        brace_depth -= 1
                                if brace_depth == 0:
                                    end_line = curr + 1
                                    length = end_line - start_line + 1
                                    all_methods.append((length, rel, combined_sig[:60], start_line, end_line))
                                    if length > 50:
                                        violations.append((rel, combined_sig[:60], start_line, end_line, length))
                                    i = curr
                                    break
                                curr += 1
            i += 1

    all_methods.sort(reverse=True)
    print(f"Total methods analyzed: {len(all_methods)}")
    print("Top 10 longest methods:")
    for length, rel, sig, start, end in all_methods[:10]:
        status = "FAIL" if length > 50 else "PASS"
        print(f"[{status}] {length:2d} lines (L{start:3d}-L{end:3d}) in {rel} : {sig}")

    return violations

def audit_qb1(cs_files):
    print("\n=== Rule Q-B1: 0 Dead / Commented-out Code ===")
    violations = []
    code_pattern = re.compile(r"^\s*//\s*(if\s*\(|for\s*\(|while\s*\(|var\s+\w+\s*=|return\s+|throw\s+|new\s+\w+|Console\.|Debug\.|;\s*$|else\s*\{)")
    
    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        rel = os.path.relpath(f, CORE_DIR)
        for idx, line in enumerate(lines, 1):
            if code_pattern.search(line):
                # Check if it's documentation or legitimate comment
                stripped = line.strip()
                if "///" in stripped or "e.g." in stripped or "example" in stripped.lower():
                    continue
                violations.append((rel, idx, stripped))
    
    if not violations:
        print("PASS: 0 dead / commented-out code detected.")
    else:
        for rel, idx, text in violations:
            print(f"FAIL: {rel}:{idx} -> {text}")
    return violations

def audit_qb2(cs_files):
    print("\n=== Rule Q-B2: 0 Unannotated Catch Blocks ===")
    violations = []
    catch_pattern = re.compile(r"catch(\s*\([^\)]*\))?\s*\{([^}]*)\}", re.DOTALL)

    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            content = fp.read()
        rel = os.path.relpath(f, CORE_DIR)

        matches = re.finditer(r"catch(\s*\([^\)]*\))?\s*\{", content)
        for m in matches:
            # find matching closing brace
            start = m.start()
            brace_pos = content.find('{', start)
            depth = 1
            idx = brace_pos + 1
            while idx < len(content) and depth > 0:
                if content[idx] == '{':
                    depth += 1
                elif content[idx] == '}':
                    depth -= 1
                idx += 1
            body = content[brace_pos+1 : idx-1].strip()
            # check if body is empty or has no comment / no rethrow / no logging
            has_comment = "//" in body or "/*" in body
            has_action = len(body) > 0 and not body.isspace()
            line_no = content[:start].count("\n") + 1
            if not body:
                violations.append((rel, line_no, "Empty catch block"))
            elif not has_comment and "throw" not in body and "log" not in body.lower():
                violations.append((rel, line_no, f"Catch without comment/rethrow: {body[:50]}"))
            else:
                print(f"PASS (Annotated catch): {rel}:{line_no} -> {body.splitlines()[0][:60]}")

    if not violations:
        print("PASS: All catch blocks are properly annotated or handled.")
    else:
        for rel, line_no, desc in violations:
            print(f"FAIL: {rel}:{line_no} -> {desc}")
    return violations

if __name__ == "__main__":
    files = get_cs_files()
    print(f"Found {len(files)} C# files in HPKata.Core")
    c2 = audit_c2(files)
    m3 = audit_m3(files)
    qb1 = audit_qb1(files)
    qb2 = audit_qb2(files)

    print("\n================ FINAL SUMMARY ================")
    print(f"Rule C2 (<= 300 lines): {'PASS' if not c2 else f'FAIL ({len(c2)} violations)'}")
    print(f"Rule M3 (<= 50 lines) : {'PASS' if not m3 else f'FAIL ({len(m3)} violations)'}")
    print(f"Rule Q-B1 (0 dead code): {'PASS' if not qb1 else f'FAIL ({len(qb1)} violations)'}")
    print(f"Rule Q-B2 (catch rules): {'PASS' if not qb2 else f'FAIL ({len(qb2)} violations)'}")
