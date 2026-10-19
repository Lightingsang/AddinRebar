import os
import re
import sys

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def get_all_cs_files(root_dir):
    cs_files = []
    for dirpath, dirnames, filenames in os.walk(root_dir):
        # Skip bin and obj directories
        if "bin" in dirnames:
            dirnames.remove("bin")
        if "obj" in dirnames:
            dirnames.remove("obj")
        for f in filenames:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(dirpath, f))
    return cs_files

def check_c2(cs_files):
    print("=== CHECK RULE C2: EVERY .CS FILE <= 300 LINES ===")
    violations = []
    max_lines = 0
    max_file = ""
    for fpath in cs_files:
        with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
            lines = len(f.readlines())
        if lines > max_lines:
            max_lines = lines
            max_file = fpath
        if lines > 300:
            rel = os.path.relpath(fpath, HPKATA_DIR)
            violations.append((rel, lines))
    
    print(f"Total .cs files checked: {len(cs_files)}")
    print(f"Max file length: {max_lines} lines ({os.path.relpath(max_file, HPKATA_DIR)})")
    if violations:
        print(f"FAIL: {len(violations)} files exceed 300 lines:")
        for rel, l in violations:
            print(f"  - {rel}: {l} lines")
        return False
    else:
        print("PASS: All .cs files are <= 300 lines.")
        return True

def check_host_neutrality(cs_files):
    print("\n=== CHECK HOST NEUTRALITY: HPKata.Core HAS ZERO REFERENCES TO Autodesk.* ===")
    core_files = [f for f in cs_files if "HPKata.Core" in f and "Tests" not in f]
    violations = []
    autodesk_re = re.compile(r"\bAutodesk\b", re.IGNORECASE)
    for fpath in core_files:
        with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
        matches = autodesk_re.findall(content)
        if matches:
            rel = os.path.relpath(fpath, HPKATA_DIR)
            violations.append((rel, len(matches)))
    
    print(f"Total HPKata.Core files checked: {len(core_files)}")
    if violations:
        print(f"FAIL: {len(violations)} files in HPKata.Core reference Autodesk:")
        for rel, count in violations:
            print(f"  - {rel}: {count} occurrences")
        return False
    else:
        print("PASS: Zero references to Autodesk in HPKata.Core.")
        return True

def check_r5(cs_files):
    print("\n=== CHECK RULE R5: 304.8 STRICTLY AND EXCLUSIVELY IN RevitUnitConverter.cs ===")
    violations = []
    target_name = "RevitUnitConverter.cs"
    pattern = re.compile(r"\b304\.8\b")
    found_in_target = False
    
    for fpath in cs_files:
        fname = os.path.basename(fpath)
        with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
            for lno, line in enumerate(f, 1):
                if pattern.search(line):
                    if fname == target_name:
                        found_in_target = True
                    else:
                        rel = os.path.relpath(fpath, HPKATA_DIR)
                        violations.append((rel, lno, line.strip()))
    
    print(f"Found in {target_name}: {found_in_target}")
    if violations:
        print(f"FAIL: 304.8 found in {len(violations)} other locations:")
        for rel, lno, line in violations:
            print(f"  - {rel}:{lno}: {line}")
        return False
    elif not found_in_target:
        print(f"WARNING: 304.8 not found even in {target_name}?")
        return False
    else:
        print(f"PASS: 304.8 appears strictly and exclusively in {target_name}.")
        return True

def check_qb2(cs_files):
    print("\n=== CHECK RULE Q-B2: ZERO UNANNOTATED EMPTY CATCH BLOCKS ===")
    empty_catch_re = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}")
    violations = []
    for fpath in cs_files:
        with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
        if empty_catch_re.search(content):
            rel = os.path.relpath(fpath, HPKATA_DIR)
            violations.append(rel)
    
    if violations:
        print(f"FAIL: Unannotated empty catch found in {len(violations)} files:")
        for rel in violations:
            print(f"  - {rel}")
        return False
    else:
        print("PASS: Zero unannotated empty catch blocks.")
        return True

def check_qb1(cs_files):
    print("\n=== CHECK RULE Q-B1: ZERO COMMENTED-OUT CODE ===")
    code_patterns = [
        re.compile(r"^\s*//\s*(?:var|if|for|foreach|while|return|using|new|throw|try|catch)\b"),
        re.compile(r"^\s*//\s*[A-Z][a-zA-Z0-9_]*\s*\(.*?\);"),
        re.compile(r"^\s*//\s*[a-z][a-zA-Z0-9_]*\.[a-zA-Z0-9_]+\s*=")
    ]
    violations = []
    for fpath in cs_files:
        with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
            for lno, line in enumerate(f, 1):
                clean = line.strip()
                if clean.startswith("///") or clean.startswith("// Multi-version") or clean.startswith("// Rule") or clean.startswith("// ADR") or clean.startswith("// Phase") or clean.startswith("// Case") or clean.startswith("// Arrange") or clean.startswith("// Act") or clean.startswith("// Assert") or clean.startswith("// Note") or clean.startswith("// <summary>"):
                    continue
                for cp in code_patterns:
                    if cp.match(line):
                        rel = os.path.relpath(fpath, HPKATA_DIR)
                        violations.append((rel, lno, clean))
                        break
    if violations:
        print(f"WARNING/FAIL: Found {len(violations)} suspicious commented code lines:")
        for rel, lno, text in violations[:20]:
            print(f"  - {rel}:{lno}: {text}")
        if len(violations) > 20:
            print(f"  ... and {len(violations)-20} more.")
        return False
    else:
        print("PASS: Zero commented-out code detected.")
        return True

def check_bad_keywords(cs_files):
    print("\n=== CHECK SUSPICIOUS KEYWORDS: TODO, FIXME, HACK, DUMMY, NOTIMPLEMENTED ===")
    keywords = ["TODO", "FIXME", "HACK", "NotImplementedException"]
    found = {}
    for kw in keywords:
        found[kw] = []
        kw_re = re.compile(rf"\b{kw}\b")
        for fpath in cs_files:
            with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
                for lno, line in enumerate(f, 1):
                    if kw_re.search(line):
                        rel = os.path.relpath(fpath, HPKATA_DIR)
                        found[kw].append((rel, lno, line.strip()))
    
    has_issues = False
    for kw, items in found.items():
        if items:
            print(f"Found {len(items)} instances of {kw}:")
            for rel, lno, text in items[:10]:
                print(f"  - {rel}:{lno}: {text}")
            if len(items) > 10:
                print(f"  ... and {len(items)-10} more.")
            has_issues = True
        else:
            print(f"PASS: 0 instances of {kw}")
    return not has_issues

if __name__ == "__main__":
    files = get_all_cs_files(HPKATA_DIR)
    c2 = check_c2(files)
    hn = check_host_neutrality(files)
    r5 = check_r5(files)
    qb2 = check_qb2(files)
    qb1 = check_qb1(files)
    kw = check_bad_keywords(files)
    print("\n=== SUMMARY ===")
    print(f"C2: {'PASS' if c2 else 'FAIL'}")
    print(f"Host Neutrality: {'PASS' if hn else 'FAIL'}")
    print(f"R5 (304.8): {'PASS' if r5 else 'FAIL'}")
    print(f"Q-B2: {'PASS' if qb2 else 'FAIL'}")
    print(f"Q-B1: {'PASS' if qb1 else 'FAIL'}")
    print(f"Keywords: {'PASS' if kw else 'ISSUES_FOUND'}")
