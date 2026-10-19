import os
import re
import sys
from pathlib import Path

HPKATA_DIR = Path(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(HPKATA_DIR):
        # Ignore bin and obj
        dirs[:] = [d for d in dirs if d not in ("bin", "obj")]
        for file in files:
            if file.endswith(".cs"):
                cs_files.append(Path(root) / file)
    return sorted(cs_files)

def audit_rule_c2(cs_files):
    violations = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="replace") as fp:
            lines = fp.readlines()
        if len(lines) > 300:
            violations.append((f, len(lines)))
    return violations

def audit_rule_m3(cs_files):
    # Method parser using bracket counting and signature detection
    violations = []
    # Regex to detect method signature candidate
    # Typically: [access_modifier] [static/virtual/override/async/etc] ReturnType MethodName(args)
    method_sig_pattern = re.compile(
        r'^\s*(public|protected|private|internal|static|virtual|override|async|sealed)\s+.*?\([^\)]*\)(\s*where\s+.*)?\s*$'
    )

    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="replace") as fp:
            lines = fp.readlines()

        i = 0
        while i < len(lines):
            line = lines[i].strip()
            # Check if this line looks like a method signature or start of a method
            # Skip interface, class, enum, record definitions, properties (get; set;), control flow (if, for, while, switch)
            if (
                any(line.startswith(kw) for kw in ("public ", "private ", "protected ", "internal ", "static ", "async "))
                and not any(kw in line for kw in ("class ", "interface ", "enum ", "struct ", "record ", "delegate ", "namespace ", "event "))
                and "(" in line
                and not line.endswith(";")
                and not line.startswith("//")
            ):
                sig_line_num = i + 1
                sig_name = line

                # Find opening brace '{'
                brace_idx = i
                found_brace = False
                while brace_idx < len(lines) and brace_idx < i + 10:
                    if "{" in lines[brace_idx]:
                        found_brace = True
                        break
                    if ";" in lines[brace_idx] and not lines[brace_idx].strip().startswith("//"):
                        break
                    brace_idx += 1

                if found_brace:
                    # Count matching braces
                    depth = 0
                    start_method_line = brace_idx + 1
                    end_method_line = brace_idx + 1
                    method_started = False
                    j = brace_idx
                    while j < len(lines):
                        cur_line = lines[j]
                        # Crude string/comment stripping for brace counting
                        stripped = re.sub(r'".*?"', '', cur_line)
                        stripped = re.sub(r'//.*', '', stripped)
                        open_count = stripped.count('{')
                        close_count = stripped.count('}')
                        if open_count > 0:
                            method_started = True
                        depth += open_count - close_count
                        if method_started and depth <= 0:
                            end_method_line = j + 1
                            break
                        j += 1

                    length = end_method_line - start_method_line + 1
                    if length > 50:
                        violations.append((f, sig_line_num, sig_name, length))
                    i = j # skip past method
            i += 1
    return violations

def audit_rule_qb1(cs_files):
    violations = []
    # Pattern for TODO, FIXME, HACK, NotImplementedException
    pattern = re.compile(r'\b(TODO|FIXME|HACK|NotImplementedException)\b')
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="replace") as fp:
            for idx, line in enumerate(fp, start=1):
                m = pattern.search(line)
                if m:
                    # Exclude this script or testing if needed, but in HPKata all should be 0
                    violations.append((f, idx, m.group(1), line.strip()))
    return violations

def audit_rule_qb2(cs_files):
    violations = []
    # Find catch blocks that are completely empty or lack comments / logging
    # e.g., catch { } or catch (Exception) { } without any statement or comment
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="replace") as fp:
            text = fp.read()
        
        # Regex to find catch (...) { ... }
        for m in re.finditer(r'catch\s*(\([^\)]*\))?\s*\{([^}]*)\}', text, re.DOTALL):
            body = m.group(2).strip()
            # If body is empty or has no comment and no logging/throw
            if not body:
                violations.append((f, "Empty catch block with no content or explanation"))
            else:
                # Check if body has at least a comment or code
                lines_in_body = [l.strip() for l in body.splitlines() if l.strip()]
                has_comment = any(l.startswith("//") or "/*" in l for l in lines_in_body)
                has_code = any(not l.startswith("//") and not l.startswith("/*") for l in lines_in_body)
                if not has_comment and not has_code:
                    violations.append((f, f"Catch block without annotation comment: {body}"))
    return violations

def audit_rule_r5(cs_files):
    violations = []
    for f in cs_files:
        if "Tests" in str(f):
            continue
        with open(f, "r", encoding="utf-8", errors="replace") as fp:
            for idx, line in enumerate(fp, start=1):
                if "304.8" in line:
                    if f.name != "RevitUnitConverter.cs":
                        violations.append((f, idx, line.strip()))
    return violations

def audit_host_neutrality(cs_files):
    violations = []
    core_files = [f for f in cs_files if "HPKata.Core" in str(f) and "Tests" not in str(f)]
    for f in core_files:
        with open(f, "r", encoding="utf-8", errors="replace") as fp:
            for idx, line in enumerate(fp, start=1):
                if re.search(r'\bAutodesk\.', line):
                    violations.append((f, idx, line.strip()))
    return violations

def main():
    cs_files = get_cs_files()
    print(f"Total .cs files scanned in HPKata/: {len(cs_files)}")
    
    # Per-project stats
    projects = {}
    for f in cs_files:
        proj_name = f.relative_to(HPKATA_DIR).parts[0]
        projects.setdefault(proj_name, []).append(f)
    
    print("\n--- Project Breakdown ---")
    total_lines = 0
    for p, files in sorted(projects.items()):
        p_lines = sum(len(open(f, "r", encoding="utf-8", errors="replace").readlines()) for f in files)
        total_lines += p_lines
        print(f"  {p}: {len(files)} files, {p_lines:,} lines")
    print(f"Total Source Lines across HPKata: {total_lines:,}\n")

    # Audit C2
    c2 = audit_rule_c2(cs_files)
    print(f"Rule C2 (File <= 300 lines) Violations: {len(c2)}")
    for f, count in c2:
        print(f"  VIOLATION C2: {f} ({count} lines)")

    # Audit M3
    m3 = audit_rule_m3(cs_files)
    print(f"\nRule M3 (Method <= 50 lines) Violations: {len(m3)}")
    for f, line_num, sig, length in m3:
        print(f"  VIOLATION M3: {f.name}:{line_num} -> '{sig}' ({length} lines)")

    # Audit Q-B1
    qb1 = audit_rule_qb1(cs_files)
    print(f"\nRule Q-B1 (Zero TODO/FIXME/HACK/NotImplementedException) Violations: {len(qb1)}")
    for f, line_num, kw, content in qb1:
        print(f"  VIOLATION Q-B1: {f.name}:{line_num} [{kw}] -> {content}")

    # Audit Q-B2
    qb2 = audit_rule_qb2(cs_files)
    print(f"\nRule Q-B2 (Annotated Catch Blocks) Violations: {len(qb2)}")
    for f, msg in qb2:
        print(f"  VIOLATION Q-B2: {f.name} -> {msg}")

    # Audit R5
    r5 = audit_rule_r5(cs_files)
    print(f"\nRule R5 (304.8 strictly in RevitUnitConverter.cs) Violations: {len(r5)}")
    for f, line_num, content in r5:
        print(f"  VIOLATION R5: {f.name}:{line_num} -> {content}")

    # Audit Host Neutrality
    hn = audit_host_neutrality(cs_files)
    print(f"\nHost Neutrality (Zero Autodesk.* in HPKata.Core) Violations: {len(hn)}")
    for f, line_num, content in hn:
        print(f"  VIOLATION Host Neutrality: {f.name}:{line_num} -> {content}")

if __name__ == "__main__":
    main()
