import os
import re
import sys

CORE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core"

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(CORE_DIR):
        if "obj" in root or "bin" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return sorted(cs_files)

def check_c2(files):
    print("=" * 80)
    print("CHECK C2: FILE LINE COUNTS (<= 300 LINES)")
    print("=" * 80)
    violations = []
    for f in files:
        rel = os.path.relpath(f, CORE_DIR)
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        cnt = len(lines)
        status = "PASS" if cnt <= 300 else "FAIL"
        if cnt > 300:
            violations.append((rel, cnt))
        print(f"[{status}] {cnt:3d} lines: {rel}")
    print(f"\nTotal files: {len(files)}, C2 Violations: {len(violations)}")
    return violations

def check_m3(files):
    print("\n" + "=" * 80)
    print("CHECK M3: METHOD LINE COUNTS (<= 50 LINES)")
    print("=" * 80)
    violations = []
    
    # Method detection heuristic with brace counting
    for fpath in files:
        rel = os.path.relpath(fpath, CORE_DIR)
        with open(fpath, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
            
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            # Look for method definition
            m_decl = re.search(r'\b(public|private|protected|internal|static|async|virtual|override|sealed)\b.*?\b([A-Za-z0-9_]+)\s*\(', line)
            if m_decl and not any(kw in line for kw in ["class ", "interface ", "struct ", "enum ", "record ", "namespace ", "delegate "]):
                method_name = m_decl.group(2)
                start_line = i + 1
                
                # Check for expression-bodied method
                if "=>" in line and ";" in line:
                    length = 1
                    i += 1
                    continue
                
                # Find opening brace
                brace_found = False
                brace_depth = 0
                k = i
                while k < len(lines):
                    l_str = lines[k]
                    # strip comments/strings roughly
                    cleaned = re.sub(r'".*?"', '', re.sub(r'//.*', '', l_str))
                    for ch in cleaned:
                        if ch == '{':
                            brace_depth += 1
                            brace_found = True
                        elif ch == '}':
                            brace_depth -= 1
                            if brace_found and brace_depth == 0:
                                end_line = k + 1
                                length = end_line - start_line + 1
                                if length > 50:
                                    violations.append((rel, method_name, start_line, end_line, length))
                                    print(f"[FAIL] {length:3d} lines: {rel} -> {method_name} (L{start_line}-L{end_line})")
                                i = k
                                break
                    if brace_found and brace_depth == 0:
                        break
                    # If semicolon found before open brace, might be abstract or interface
                    if not brace_found and ";" in cleaned:
                        break
                    k += 1
            i += 1
            
    print(f"\nM3 Violations: {len(violations)}")
    if len(violations) == 0:
        print("[ALL METHODS <= 50 LINES PASS]")
    return violations

def check_q_b1(files):
    print("\n" + "=" * 80)
    print("CHECK Q-B1: ZERO DEAD CODE / COMMENTED-OUT CODE")
    print("=" * 80)
    suspicious = []
    patterns = [
        re.compile(r'//\s*(var|int|string|double|bool|if|for|foreach|while|return|new)\s+[A-Za-z0-9_]+.*;'),
        re.compile(r'//\s*[A-Za-z0-9_]+\s*=\s*.*;'),
        re.compile(r'//\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\(.*\);')
    ]
    for f in files:
        rel = os.path.relpath(f, CORE_DIR)
        with open(f, "r", encoding="utf-8") as fp:
            for lno, line in enumerate(fp, 1):
                s = line.strip()
                if s.startswith("//"):
                    for p in patterns:
                        if p.search(s):
                            suspicious.append((rel, lno, s))
                            print(f"[SUSPICIOUS] {rel}:{lno}: {s}")
                            break
    print(f"\nQ-B1 Suspicious count: {len(suspicious)}")
    return suspicious

def check_q_b2(files):
    print("\n" + "=" * 80)
    print("CHECK Q-B2: ZERO UNANNOTATED CATCH BLOCKS")
    print("=" * 80)
    violations = []
    catch_blocks = []
    for f in files:
        rel = os.path.relpath(f, CORE_DIR)
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        for idx, line in enumerate(lines):
            if re.search(r'\bcatch\b', line):
                # find body
                body_lines = []
                depth = 0
                started = False
                for k in range(idx, min(idx + 20, len(lines))):
                    l_str = lines[k]
                    body_lines.append(l_str)
                    if "{" in l_str:
                        depth += l_str.count("{")
                        started = True
                    if "}" in l_str:
                        depth -= l_str.count("}")
                    if started and depth <= 0:
                        break
                body_text = "".join(body_lines)
                has_comment = "//" in body_text or "/*" in body_text
                catch_blocks.append((rel, idx + 1, line.strip(), has_comment, body_text))
                status = "PASS" if has_comment else "FAIL"
                if not has_comment:
                    violations.append((rel, idx + 1, line.strip()))
                print(f"[{status}] {rel}:{idx+1}: {line.strip()} (Has annotation comment: {has_comment})")
    print(f"\nTotal catch blocks: {len(catch_blocks)}, Q-B2 Violations: {len(violations)}")
    return violations

def check_d1_d4(files):
    print("\n" + "=" * 80)
    print("CHECK D1/D4: ZERO AUTODESK REFERENCES")
    print("=" * 80)
    violations = []
    for f in files:
        rel = os.path.relpath(f, CORE_DIR)
        with open(f, "r", encoding="utf-8") as fp:
            for lno, line in enumerate(fp, 1):
                if re.search(r'\busing\s+Autodesk\b', line) or re.search(r'\bAutodesk\.(AutoCAD|Revit)\b', line):
                    # Check if in comment
                    if line.strip().startswith("//") or line.strip().startswith("///"):
                        continue
                    violations.append((rel, lno, line.strip()))
                    print(f"[FAIL] {rel}:{lno}: {line.strip()}")
    print(f"\nD1/D4 Violations: {len(violations)}")
    if len(violations) == 0:
        print("[ZERO AUTODESK REFERENCES PASS]")
    return violations

def check_facades(files):
    print("\n" + "=" * 80)
    print("CHECK FACADES / STUB METHODS / HARDCODED RESULTS")
    print("=" * 80)
    suspicious = []
    not_impl = []
    for f in files:
        rel = os.path.relpath(f, CORE_DIR)
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        for lno, line in enumerate(lines, 1):
            if "NotImplementedException" in line:
                not_impl.append((rel, lno, line.strip()))
                print(f"[NOT IMPLEMENTED] {rel}:{lno}: {line.strip()}")
            if re.search(r'=>\s*(true|false|null|0|-1|"");\s*$', line.strip()):
                # Check what kind of member this is
                suspicious.append((rel, lno, line.strip()))
                print(f"[EXPR STUB?] {rel}:{lno}: {line.strip()}")
    print(f"\nNotImplementedExceptions: {len(not_impl)}")
    print(f"Expression body stubs: {len(suspicious)}")
    return not_impl, suspicious

if __name__ == "__main__":
    files = get_cs_files()
    print(f"Found {len(files)} C# files in HPKata.Core\n")
    check_c2(files)
    check_m3(files)
    check_q_b1(files)
    check_q_b2(files)
    check_d1_d4(files)
    check_facades(files)
