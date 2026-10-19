import os
import re
import sys

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def get_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_rule_c2(cs_files):
    violations = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        if len(lines) > 300:
            violations.append((f, len(lines)))
    return violations

def check_rule_r5(cs_files):
    violations = []
    for f in cs_files:
        filename = os.path.basename(f)
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            content = fp.read()
        if "304.8" in content:
            if filename != "RevitUnitConverter.cs":
                violations.append(f)
    return violations

def check_rule_q_b1(cs_files):
    todo_violations = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            for idx, line in enumerate(fp, 1):
                clean = line.strip()
                if "TODO" in clean or "FIXME" in clean or "HACK" in clean:
                    todo_violations.append((f, idx, clean))
    return todo_violations

def check_rule_q_b2(cs_files):
    empty_catch_violations = []
    catch_pattern = re.compile(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', re.DOTALL)
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            content = fp.read()
        for match in catch_pattern.finditer(content):
            empty_catch_violations.append((f, match.group(0)))
    return empty_catch_violations

def count_method_lines(f):
    # Heuristic method line counter for C#
    with open(f, "r", encoding="utf-8", errors="ignore") as fp:
        lines = fp.readlines()
    
    # We look for method declarations followed by {
    violations = []
    # simplified brace matching
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # method signature detection (access modifier, return type, name, parens)
        # excludes class, struct, interface, enum, namespace, property get/set
        is_method_sig = False
        if any(line.startswith(kw) for kw in ["public ", "private ", "protected ", "internal ", "static ", "async "]):
            if "(" in line and not line.startswith("public class") and not line.startswith("public record") and not line.startswith("public struct"):
                if not line.endswith(";") and not "=>" in line:
                    is_method_sig = True
        
        if is_method_sig:
            sig_start = i + 1
            # find opening brace
            brace_idx = i
            while brace_idx < len(lines) and "{" not in lines[brace_idx]:
                brace_idx += 1
            if brace_idx < len(lines) and "{" in lines[brace_idx]:
                brace_count = 0
                method_end = brace_idx
                for j in range(brace_idx, len(lines)):
                    brace_count += lines[j].count("{") - lines[j].count("}")
                    if brace_count == 0:
                        method_end = j + 1
                        break
                method_len = method_end - sig_start + 1
                if method_len > 50:
                    violations.append((sig_start, method_end, method_len, lines[i].strip()))
                i = method_end
                continue
        i += 1
    return violations

if __name__ == "__main__":
    all_cs = get_cs_files(HPKATA_DIR)
    print(f"Total C# files scanned in HPKata: {len(all_cs)}")
    
    # C2
    c2 = check_rule_c2(all_cs)
    print(f"--- Rule C2 (<= 300 lines) Violations: {len(c2)} ---")
    for f, count in c2:
        print(f"  {f}: {count} lines")
        
    # R5
    r5 = check_rule_r5(all_cs)
    print(f"--- Rule R5 (304.8 only in RevitUnitConverter.cs) Violations: {len(r5)} ---")
    for f in r5:
        print(f"  {f}")
        
    # Q-B1
    # Check beam & Phase 2 files specifically
    beam_cs = [f for f in all_cs if any(k in f for k in ["Beam", "Etabs", "TieBeam", "Framing"])]
    print(f"Scanning {len(beam_cs)} Phase 2 files for Q-B1 and Q-B2...")
    qb1 = check_rule_q_b1(beam_cs)
    print(f"--- Rule Q-B1 (TODO/FIXME/HACK) Violations: {len(qb1)} ---")
    for f, line_no, text in qb1:
        print(f"  {os.path.basename(f)}:{line_no} -> {text}")
        
    # Q-B2
    qb2 = check_rule_q_b2(beam_cs)
    print(f"--- Rule Q-B2 (Empty catch) Violations: {len(qb2)} ---")
    for f, snippet in qb2:
        print(f"  {os.path.basename(f)} -> {snippet}")
        
    # M3
    print("--- Rule M3 (<= 50 physical lines per method) in Phase 2 files ---")
    m3_violations = []
    for f in beam_cs:
        v = count_method_lines(f)
        if v:
            for s, e, l, sig in v:
                m3_violations.append((f, s, e, l, sig))
                print(f"  {os.path.basename(f)} lines {s}-{e} ({l} lines): {sig}")
    print(f"Total Rule M3 violations found: {len(m3_violations)}")
