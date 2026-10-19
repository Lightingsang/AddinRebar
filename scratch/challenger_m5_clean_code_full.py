import os
import re
import sys

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper"

PROJECTS = [
    r"HPGrasshopper.McpBridge",
    r"HPGrasshopper.Mcp.Server",
    r"HPGrasshopper.McpBridge.Tests",
    r"HPGrasshopper.Mcp.Server.Tests",
    r"HPGrasshopper.E2E.Tests",
]

def scan_files(root_dir):
    cs_files = []
    for r, d, files in os.walk(root_dir):
        if "bin" in r.split(os.sep) or "obj" in r.split(os.sep):
            continue
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(r, f))
    return sorted(cs_files)

def check_c2(filepath, lines):
    # Rule C2: File length <= 300 lines
    count = len(lines)
    return count <= 300, count

def check_qb1(filepath, lines):
    # Rule Q-B1: Zero commented-out code
    violations = []
    code_keywords = (
        "var ", "return ", "if ", "for ", "foreach ", "while ", "await ",
        "using ", "throw ", "public ", "private ", "internal ", "protected ",
        "class ", "interface ", "record ", "struct ", "new "
    )
    allowed_prefixes = (
        "TODO", "NOTE", "Multi-version:", "Multi-version", "Ref:", "See", "e.g.",
        "Rule", "PCC", "ADR", "Phase", "Step", "Tier", "Case", "Example", "Doc",
        "Expected", "Tested", "Verify", "Reason:"
    )
    for idx, line in enumerate(lines, 1):
        s = line.strip()
        if s.startswith("//"):
            c = s[2:].strip()
            if any(c.startswith(kw) for kw in code_keywords) or (c.endswith(";") and not any(c.startswith(ap) for ap in allowed_prefixes)):
                if not any(word in c.lower() for word in ["example", "sample", "note", "todo", "multi-version", "reason", "ref:", "url", "rule", "tested", "expected", "simulat"]):
                    violations.append((idx, s))
    return violations

def check_qb2(filepath, content):
    # Rule Q-B2: Catch blocks with empty body must have an explanatory comment
    # Matches catch (...) { } or catch { }
    violations = []
    # Find all catch blocks
    catch_pattern = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', re.MULTILINE | re.DOTALL)
    for m in catch_pattern.finditer(content):
        body = m.group(1).strip()
        # If body is empty or whitespace
        if not body:
            # Check if there's any comment inside the catch block
            # Calculate line number
            line_num = content[:m.start()].count('\n') + 1
            violations.append((line_num, "Unannotated empty catch block: catch { }"))
        else:
            # If body has no executable code (only whitespace), check for comment
            non_comment_body = re.sub(r'//.*', '', body)
            non_comment_body = re.sub(r'/\*.*?\*/', '', non_comment_body, flags=re.DOTALL).strip()
            if not non_comment_body:
                # Body only has comments - check that it has non-empty comment
                if not ("//" in body or "/*" in body):
                    line_num = content[:m.start()].count('\n') + 1
                    violations.append((line_num, "Catch block without code or comment"))
    return violations

def find_methods_and_check_m3(filepath, content):
    # Rule M3: Method body <= 50 lines
    # Parse methods by looking for method signatures followed by { ... }
    lines = content.splitlines()
    violations = []
    
    # Simple C# method finder avoiding classes/records with primary constructors
    # A method declaration pattern
    method_sig_pattern = re.compile(
        r'^\s*(?:(?:public|private|protected|internal|static|async|override|virtual|sealed|unsafe)\s+)+'
        r'(?:[\w\<\>\[\],\?\s]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+[^{]+)?\s*\{',
        re.MULTILINE
    )
    
    for m in method_sig_pattern.finditer(content):
        # Check if this is a class, record, struct, interface declaration
        prefix = content[max(0, m.start() - 50):m.start()]
        matched_str = m.group(0)
        if any(kw in matched_str for kw in [" class ", " record ", " struct ", " interface "]):
            continue
            
        method_name = m.group(1)
        start_pos = m.end() - 1 # at the '{'
        start_line = content[:m.start()].count('\n') + 1
        body_start_line = content[:start_pos].count('\n') + 1
        
        # Match braces to find method end
        depth = 1
        curr = start_pos + 1
        n = len(content)
        while curr < n and depth > 0:
            ch = content[curr]
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
            curr += 1
            
        end_line = content[:curr].count('\n') + 1
        body_span = end_line - body_start_line + 1
        total_span = end_line - start_line + 1
        
        # Check if method body > 50 lines
        if body_span > 50:
            violations.append((method_name, start_line, end_line, body_span, total_span))
            
    return violations

def main():
    print("=== HPGRASSHOPPER FULL CLEAN CODE COMPLIANCE AUDIT ===")
    total_files = 0
    total_c2_fails = 0
    total_m3_fails = 0
    total_qb1_fails = 0
    total_qb2_fails = 0

    for proj in PROJECTS:
        proj_dir = os.path.join(BASE_DIR, proj)
        files = scan_files(proj_dir)
        print(f"\n--- Project: {proj} ({len(files)} files) ---")
        
        proj_c2 = []
        proj_m3 = []
        proj_qb1 = []
        proj_qb2 = []
        
        for f in files:
            total_files += 1
            rel = os.path.relpath(f, BASE_DIR)
            with open(f, "r", encoding="utf-8") as fp:
                content = fp.read()
            lines = content.splitlines()
            
            # C2
            c2_ok, line_count = check_c2(f, lines)
            if not c2_ok:
                proj_c2.append((rel, line_count))
                
            # M3
            m3_viols = find_methods_and_check_m3(f, content)
            for m_name, sl, el, bspan, tspan in m3_viols:
                proj_m3.append((rel, m_name, sl, el, bspan, tspan))
                
            # Q-B1
            qb1_viols = check_qb1(f, lines)
            for lnum, text in qb1_viols:
                proj_qb1.append((rel, lnum, text))
                
            # Q-B2
            qb2_viols = check_qb2(f, content)
            for lnum, msg in qb2_viols:
                proj_qb2.append((rel, lnum, msg))

        # Project summary
        print(f"  C2 (<=300 lines):   {'PASS (0)' if not proj_c2 else f'FAIL ({len(proj_c2)})'}")
        if proj_c2:
            for r, lc in proj_c2:
                print(f"    VIOLATION C2: {r} has {lc} lines")
        print(f"  M3 (body <=50 lines): {'PASS (0)' if not proj_m3 else f'FAIL ({len(proj_m3)})'}")
        if proj_m3:
            for r, mn, sl, el, bs, ts in proj_m3:
                print(f"    VIOLATION M3: {r}:{sl}-{el} {mn}() body={bs} lines")
        print(f"  Q-B1 (no dead code): {'PASS (0)' if not proj_qb1 else f'FAIL ({len(proj_qb1)})'}")
        if proj_qb1:
            for r, ln, txt in proj_qb1:
                print(f"    VIOLATION Q-B1: {r}:{ln} -> {txt}")
        print(f"  Q-B2 (no empty catch): {'PASS (0)' if not proj_qb2 else f'FAIL ({len(proj_qb2)})'}")
        if proj_qb2:
            for r, ln, msg in proj_qb2:
                print(f"    VIOLATION Q-B2: {r}:{ln} -> {msg}")

        total_c2_fails += len(proj_c2)
        total_m3_fails += len(proj_m3)
        total_qb1_fails += len(proj_qb1)
        total_qb2_fails += len(proj_qb2)

    print("\n" + "=" * 60)
    print(f"OVERALL SUMMARY ({total_files} files scanned across 5 projects):")
    print(f"  Rule C2 violations:   {total_c2_fails}")
    print(f"  Rule M3 violations:   {total_m3_fails}")
    print(f"  Rule Q-B1 violations: {total_qb1_fails}")
    print(f"  Rule Q-B2 violations: {total_qb2_fails}")
    
    clean = (total_c2_fails == 0 and total_m3_fails == 0 and total_qb1_fails == 0 and total_qb2_fails == 0)
    if clean:
        print("VERDICT: 100% COMPLIANT WITH ALL CLEAN CODE RULES (C2, M3, Q-B1, Q-B2).")
    else:
        print("VERDICT: NON-COMPLIANT.")
        sys.exit(1)

if __name__ == "__main__":
    main()
