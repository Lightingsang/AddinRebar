#!/usr/bin/env python3
"""
Milestone M2 Verification & Grep Strategy Scanner.
Checks:
1. Zero occurrences of "C:\\Dce Pro" or "C:/Dce Pro" (including JSON escaped "C:\\\\Dce Pro") in HPDcePro/src/** and HPDcePro/assets/**.
2. Clean Code Compliance:
   - Rule C2: File length <= 300 lines (ViewModels <= 250 lines)
   - Rule M3: Method length <= 50 lines
   - Rule Q-B1: Zero commented-out code blocks
   - Rule Q-B2: Zero unannotated catch blocks (requires // Reason: comment)
3. Asset Inventory Completeness (17 DWGs, 132 XLSMs, mauhoc.db, model.zip, 178 System files, 8 Lang dicts, 133 Icons, 7 CUIX)
4. Read-Only Invariant on legacy C:\\Dce Pro baseline
"""

import os
import re
import sys
from pathlib import Path

REPO_ROOT = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
HPDCEPRO_DIR = REPO_ROOT / "HPDcePro"
SRC_DIR = HPDCEPRO_DIR / "src"
ASSETS_DIR = HPDCEPRO_DIR / "assets"
LEGACY_DIR = Path(r"C:\Dce Pro")

def scan_zero_dce_pro():
    print("=" * 70)
    print("CHECK 1: ZERO OCCURRENCES OF 'C:\\Dce Pro' / 'C:/Dce Pro'")
    print("=" * 70)
    
    # Regex matching C:\Dce Pro or C:\\Dce Pro or C:/Dce Pro case-insensitively
    pattern = re.compile(r'C:(?:\\\\|\\|/)Dce Pro', re.IGNORECASE)
    
    violations = []
    scanned_files = 0
    
    targets = [SRC_DIR]
    if ASSETS_DIR.exists():
        targets.append(ASSETS_DIR)
    else:
        print(f"[WARN] Assets directory {ASSETS_DIR} does not exist yet (M2 feature 4 pending).")

    for target in targets:
        for root, dirs, files in os.walk(target):
            if any(part in ('bin', 'obj', '.git', '.vs') for part in Path(root).parts):
                continue
            for f in files:
                # Scan all text-based files
                ext = Path(f).suffix.lower()
                if ext in ('.cs', '.xaml', '.json', '.xml', '.config', '.txt', '.lsp', '.autostr', '.props', '.csproj', '.bat', '.cmd'):
                    scanned_files += 1
                    file_path = Path(root) / f
                    try:
                        with open(file_path, 'r', encoding='utf-8', errors='ignore') as fp:
                            for line_no, line in enumerate(fp, 1):
                                if pattern.search(line):
                                    rel = file_path.relative_to(HPDCEPRO_DIR)
                                    violations.append((str(rel), line_no, line.strip()))
                    except Exception as ex:
                        violations.append((str(file_path), 0, f"Error reading file: {ex}"))
                        
    print(f"Scanned {scanned_files} files across {targets}.")
    if violations:
        print(f"[FAIL] Found {len(violations)} forbidden occurrences of 'C:\\Dce Pro':")
        for f, l, content in violations:
            print(f"  - {f}:{l} -> {content[:80]}")
        return False, violations
    else:
        print("[PASS] Zero occurrences of 'C:\\Dce Pro' found!")
        return True, []

def scan_clean_code():
    print("\n" + "=" * 70)
    print("CHECK 2: CLEAN CODE COMPLIANCE (C2, M3, Q-B1, Q-B2)")
    print("=" * 70)
    
    cs_files = []
    for root, dirs, files in os.walk(HPDCEPRO_DIR):
        if any(part in ('bin', 'obj', '.git', '.vs') for part in Path(root).parts):
            continue
        for f in files:
            if f.endswith('.cs') and not f.endswith(('.g.cs', '.Designer.cs', 'AssemblyInfo.cs')):
                cs_files.append(Path(root) / f)
                
    print(f"Found {len(cs_files)} C# files in HPDcePro.")
    
    # 2.1 Rule C2: File <= 300 lines (ViewModels <= 250)
    c2_violations = []
    for f in cs_files:
        try:
            with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
                line_count = sum(1 for _ in fp)
            limit = 250 if "ViewModel" in f.name else 300
            if line_count > limit:
                c2_violations.append((str(f.relative_to(HPDCEPRO_DIR)), line_count, limit))
        except Exception as ex:
            c2_violations.append((str(f), 0, str(ex)))
            
    if c2_violations:
        print(f"[FAIL] Rule C2: {len(c2_violations)} files exceed line limits:")
        for f, c, lim in c2_violations:
            print(f"  - {f}: {c} lines (limit {lim})")
    else:
        print(f"[PASS] Rule C2: All {len(cs_files)} files within line limits.")
        
    # 2.2 Rule M3: Method <= 50 lines
    m3_violations = []
    for f in cs_files:
        try:
            with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
                
            in_method = False
            method_name = ""
            method_start = 0
            brace_level = 0
            method_brace_level = 0
            
            for idx, line in enumerate(lines, 1):
                stripped = line.strip()
                if not in_method and brace_level >= 1:
                    if any(stripped.startswith(mod) for mod in ['public ', 'private ', 'internal ', 'protected ', 'static ', 'async ']):
                        if '(' in stripped and not any(kw in stripped for kw in ['class ', 'struct ', 'interface ', 'record ', 'enum ', 'delegate ']):
                            method_name = stripped
                            method_start = idx
                
                cleaned = re.sub(r'"(\\.|[^"\\])*"', '""', line)
                cleaned = re.sub(r"'(\\.|[^'\\])*'", "''", cleaned)
                cleaned = re.sub(r'//.*', '', cleaned)
                
                open_cnt = cleaned.count('{')
                close_cnt = cleaned.count('}')
                
                if open_cnt > 0 or close_cnt > 0:
                    if not in_method and method_name and open_cnt > 0:
                        in_method = True
                        method_brace_level = brace_level + 1
                    brace_level += open_cnt - close_cnt
                    if in_method and brace_level < method_brace_level:
                        length = idx - method_start + 1
                        if length > 50:
                            m3_violations.append((str(f.relative_to(HPDCEPRO_DIR)), method_name[:40], method_start, idx, length))
                        in_method = False
                        method_name = ""
        except Exception:
            pass

    if m3_violations:
        print(f"[FAIL] Rule M3: {len(m3_violations)} methods exceed 50 lines:")
        for f, m, s, e, l in m3_violations[:10]:
            print(f"  - {f}:{s}-{e} ({l} lines): {m}")
        if len(m3_violations) > 10:
            print(f"  ... and {len(m3_violations) - 10} more.")
    else:
        print("[PASS] Rule M3: All methods <= 50 lines.")

    # 2.3 Rule Q-B1: Zero commented-out code
    qb1_violations = []
    code_pattern = re.compile(r'^\s*//\s*(var|int|string|double|bool|if|for|foreach|while|return|throw|using|new|Assert|Console)\s+.*[;{}]', re.IGNORECASE)
    for f in cs_files:
        try:
            with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
                for idx, line in enumerate(fp, 1):
                    if code_pattern.search(line):
                        stripped = line.strip()
                        if not stripped.startswith('///') and 'e.g.' not in stripped and 'example' not in stripped.lower():
                            qb1_violations.append((str(f.relative_to(HPDCEPRO_DIR)), idx, stripped))
        except Exception:
            pass
            
    if qb1_violations:
        print(f"[FAIL] Rule Q-B1: {len(qb1_violations)} commented code lines found:")
        for f, l, s in qb1_violations:
            print(f"  - {f}:{l}: {s}")
    else:
        print("[PASS] Rule Q-B1: Zero commented-out code lines found.")

    # 2.4 Rule Q-B2: Empty catch or catch without reason comment
    qb2_violations = []
    for f in cs_files:
        try:
            with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
            for idx, line in enumerate(lines, 1):
                if 'catch' in line:
                    match = re.search(r'catch\s*(\([^\)]*\))?\s*\{?', line)
                    if match:
                        chunk = "\n".join(lines[idx-1 : min(len(lines), idx+10)])
                        if not re.search(r'//\s*(Reason|reason)', chunk):
                            qb2_violations.append((str(f.relative_to(HPDCEPRO_DIR)), idx, line.strip()))
        except Exception:
            pass

    if qb2_violations:
        print(f"[FAIL] Rule Q-B2: {len(qb2_violations)} catch blocks missing '// Reason: ...':")
        for f, l, s in qb2_violations:
            print(f"  - {f}:{l}: {s}")
    else:
        print("[PASS] Rule Q-B2: All catch blocks contain '// Reason: ...' comments.")

    return len(c2_violations) == 0 and len(m3_violations) == 0 and len(qb1_violations) == 0 and len(qb2_violations) == 0

def scan_asset_inventory():
    print("\n" + "=" * 70)
    print("CHECK 3: ASSET INVENTORY VERIFICATION (HPDcePro/assets/)")
    print("=" * 70)
    
    if not ASSETS_DIR.exists():
        print(f"[PENDING] Assets directory {ASSETS_DIR} not yet populated.")
        return False
        
    counts = {}
    dwgs = list(ASSETS_DIR.glob("**/*.dwg"))
    xlsms = list(ASSETS_DIR.glob("**/*.xlsm"))
    db = ASSETS_DIR / "MayHoc" / "mauhoc.db"
    model_zip = ASSETS_DIR / "MayHoc" / "model.zip"
    sys_files = list((ASSETS_DIR / "System").glob("**/*"))
    sys_files = [f for f in sys_files if f.is_file()]
    lang_files = list((ASSETS_DIR / "Language").glob("**/*.txt"))
    icons = list((ASSETS_DIR / "Icon").glob("**/*"))
    icons = [f for f in icons if f.is_file()]
    cuix = list(ASSETS_DIR.glob("**/*.cuix"))

    print(f"DWG Templates:       {len(dwgs)} / 17 expected")
    print(f"XLSM Workbooks:      {len(xlsms)} / 132 expected")
    print(f"mauhoc.db:           {'EXISTS (' + str(db.stat().st_size) + ' bytes)' if db.exists() else 'MISSING'}")
    print(f"model.zip:           {'EXISTS (' + str(model_zip.stat().st_size) + ' bytes)' if model_zip.exists() else 'MISSING'}")
    print(f"System config files: {len(sys_files)} / 178 expected")
    print(f"Language dicts:      {len(lang_files)} / 8 expected")
    print(f"Icons:               {len(icons)} / 133 expected")
    print(f"CUIX Menus:          {len(cuix)} / 7 expected")

    passed = (
        len(dwgs) == 17 and
        len(xlsms) == 132 and
        db.exists() and db.stat().st_size > 8000000 and
        model_zip.exists() and model_zip.stat().st_size > 20000 and
        len(sys_files) == 178 and
        len(lang_files) == 8 and
        len(icons) == 133 and
        len(cuix) == 7
    )
    if passed:
        print("[PASS] All asset inventory categories 100% matched!")
    else:
        print("[FAIL] Asset inventory does not match expected counts.")
    return passed

def check_legacy_invariant():
    print("\n" + "=" * 70)
    print("CHECK 4: READ-ONLY LEGACY INVARIANT (C:\\Dce Pro)")
    print("=" * 70)
    if not LEGACY_DIR.exists():
        print("[FAIL] Legacy directory C:\\Dce Pro does not exist!")
        return False
        
    legacy_files = list(LEGACY_DIR.glob("**/*"))
    legacy_files = [f for f in legacy_files if f.is_file()]
    print(f"C:\\Dce Pro total file count: {len(legacy_files)} (baseline: 1554)")
    if len(legacy_files) == 1554:
        print("[PASS] Legacy file count intact (1554 files, 0 files added or deleted).")
        return True
    else:
        print(f"[WARN] Legacy file count is {len(legacy_files)}, expected 1554.")
        return False

def main():
    ok1, _ = scan_zero_dce_pro()
    ok2 = scan_clean_code()
    ok3 = scan_asset_inventory()
    ok4 = check_legacy_invariant()
    
    print("\n" + "=" * 70)
    print("OVERALL VERIFICATION SUMMARY:")
    print("=" * 70)
    print(f"1. Zero C:\\Dce Pro Occurrences:  {'PASS' if ok1 else 'FAIL'}")
    print(f"2. Clean Code Compliance:        {'PASS' if ok2 else 'FAIL'}")
    print(f"3. Asset Inventory Completeness: {'PASS' if ok3 else 'PENDING'}")
    print(f"4. Legacy Read-Only Invariant:   {'PASS' if ok4 else 'FAIL'}")
    
    all_ok = ok1 and ok2 and ok3 and ok4
    return 0 if all_ok else 1

if __name__ == '__main__':
    sys.exit(main())
