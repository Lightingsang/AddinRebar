#!/usr/bin/env python3
"""
Forensic Integrity Audit Script for Milestone M2 (Asset Migration & Decoupling).
Auditor: auditor_m2_gen2
"""

import os
import re
import sys
import sqlite3
import zipfile
from pathlib import Path
from datetime import datetime

REPO_ROOT = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
HPDCEPRO_DIR = REPO_ROOT / "HPDcePro"
SRC_DIR = HPDCEPRO_DIR / "src"
ASSETS_DIR = HPDCEPRO_DIR / "assets"
LEGACY_DIR = Path(r"C:\Dce Pro")

def audit_legacy_invariant():
    print("=" * 80)
    print("AUDIT CHECK 1: LEGACY INVARIANT (C:\\Dce Pro)")
    print("=" * 80)
    
    if not LEGACY_DIR.exists():
        print("[FAIL] Legacy directory C:\\Dce Pro does not exist!")
        return False, {"exists": False}
        
    legacy_files = [f for f in LEGACY_DIR.glob("**/*") if f.is_file()]
    file_count = len(legacy_files)
    print(f"Total file count in C:\\Dce Pro: {file_count} (Required: 1554)")
    
    # Check if any file was modified after 2026-10-18T00:00:00 (local time)
    # The milestone started around 05:27Z on 2026-10-18
    recent_mods = []
    cutoff = datetime(2026, 10, 18, 0, 0, 0).timestamp()
    for f in legacy_files:
        try:
            mtime = f.stat().st_mtime
            if mtime > cutoff:
                recent_mods.append((str(f), mtime))
        except Exception:
            pass
            
    print(f"Files modified after 2026-10-18T00:00:00: {len(recent_mods)}")
    if recent_mods:
        for f, m in recent_mods[:5]:
            print(f"  Modified file: {f} (mtime: {datetime.fromtimestamp(m).isoformat()})")
            
    passed = (file_count == 1554) and (len(recent_mods) == 0)
    print(f"Check 1 Result: {'PASS' if passed else 'FAIL'}")
    return passed, {"file_count": file_count, "recent_mods": len(recent_mods)}

def audit_assets_genuineness():
    print("\n" + "=" * 80)
    print("AUDIT CHECK 2: GENUINE ASSET MIGRATION IN HPDcePro/assets/")
    print("=" * 80)
    
    results = {}
    
    # 2.1 mauhoc.db
    db_path = ASSETS_DIR / "MayHoc" / "mauhoc.db"
    db_ok = False
    sample_count = 0
    tables = []
    if db_path.exists():
        db_size = db_path.stat().st_size
        print(f"mauhoc.db size: {db_size} bytes (~{db_size / 1024 / 1024:.2f} MB)")
        with open(db_path, "rb") as fp:
            header = fp.read(16)
        is_sqlite = header.startswith(b"SQLite format 3\x00")
        print(f"mauhoc.db SQLite magic header: {header} -> {'VALID' if is_sqlite else 'INVALID'}")
        
        try:
            conn = sqlite3.connect(str(db_path))
            cur = conn.cursor()
            cur.execute("SELECT name FROM sqlite_master WHERE type='table';")
            tables = [r[0] for r in cur.fetchall()]
            print(f"mauhoc.db tables ({len(tables)}): {tables}")
            
            cur.execute("SELECT COUNT(*) FROM [MauHoc];")
            mauhoc_rows = cur.fetchone()[0]
            print(f"  Table [MauHoc] row count: {mauhoc_rows}")

            # Inspect first 2 rows of primary table safely with utf-8 or ascii repr
            cur.execute("SELECT * FROM [MauHoc] LIMIT 2;")
            sample_rows = cur.fetchall()
            if sample_rows:
                print(f"  Sample row from [MauHoc] (repr): {ascii(sample_rows[0])[:120]}...")
            conn.close()
            
            # Must be ~8.98 MB (8,982,528 bytes) and exactly 45,020 samples in MauHoc
            if is_sqlite and mauhoc_rows == 45020 and 8_900_000 <= db_size <= 9_100_000:
                db_ok = True
                print("[PASS] mauhoc.db is authentic SQLite DB with 45,020 samples.")
            else:
                print(f"[FAIL] mauhoc.db mismatch: MauHoc rows={mauhoc_rows}, size={db_size}")
        except Exception as ex:
            print(f"[FAIL] Error querying SQLite: {ex}")
    else:
        print("[FAIL] mauhoc.db NOT FOUND!")
    results["mauhoc_db"] = db_ok
    
    # 2.2 model.zip
    model_path = ASSETS_DIR / "MayHoc" / "model.zip"
    model_ok = False
    if model_path.exists():
        model_size = model_path.stat().st_size
        print(f"\nmodel.zip size: {model_size} bytes (~{model_size / 1024:.2f} KB)")
        try:
            with zipfile.ZipFile(model_path, 'r') as zf:
                bad_file = zf.testzip()
                entries = zf.namelist()
                print(f"model.zip entries ({len(entries)}): {entries[:10]}")
                if bad_file is None and 25_000 <= model_size <= 30_000 and len(entries) > 0:
                    model_ok = True
                    print("[PASS] model.zip is authentic valid zip archive.")
                else:
                    print(f"[FAIL] model.zip corrupted or unexpected size ({bad_file}, {model_size})")
        except Exception as ex:
            print(f"[FAIL] model.zip error: {ex}")
    else:
        print("[FAIL] model.zip NOT FOUND!")
    results["model_zip"] = model_ok
    
    # 2.3 DWG templates (17 expected)
    print("\n--- Verifying 17 DWG templates ---")
    dwgs = list(ASSETS_DIR.glob("**/*.dwg"))
    dwg_ok = True
    print(f"Found {len(dwgs)} DWG files (expected 17).")
    if len(dwgs) != 17:
        dwg_ok = False
    for dwg in dwgs:
        sz = dwg.stat().st_size
        with open(dwg, "rb") as fp:
            magic = fp.read(6)
        valid_header = magic.startswith(b"AC10")
        if sz == 0 or not valid_header:
            print(f"[FAIL] Invalid DWG: {dwg.name} (size: {sz}, magic: {magic})")
            dwg_ok = False
    if dwg_ok:
        print(f"[PASS] All {len(dwgs)} DWG files have valid AutoCAD 'AC10...' magic headers and non-zero size.")
    results["dwgs"] = dwg_ok
    
    # 2.4 XLSM workbooks (132 expected)
    print("\n--- Verifying 132 XLSM calculation workbooks ---")
    xlsms = list(ASSETS_DIR.glob("**/*.xlsm"))
    xlsm_ok = True
    print(f"Found {len(xlsms)} XLSM files (expected 132).")
    if len(xlsms) != 132:
        xlsm_ok = False
    invalid_xlsms = []
    for xl in xlsms:
        sz = xl.stat().st_size
        with open(xl, "rb") as fp:
            magic = fp.read(4)
        valid_header = magic.startswith(b"PK\x03\x04")
        if sz == 0 or not valid_header:
            invalid_xlsms.append((xl.name, sz, magic))
            xlsm_ok = False
    if invalid_xlsms:
        print(f"[FAIL] Invalid XLSM files ({len(invalid_xlsms)}): {invalid_xlsms[:5]}")
    else:
        print(f"[PASS] All {len(xlsms)} XLSM workbooks have valid zip 'PK\\x03\\x04' headers and non-zero size.")
    results["xlsms"] = xlsm_ok
    
    # 2.5 System configs (178 expected)
    print("\n--- Verifying System configs ---")
    sys_dir = ASSETS_DIR / "System"
    sys_files = [f for f in sys_dir.glob("**/*") if f.is_file()]
    sys_ok = (len(sys_files) == 178)
    print(f"System files count: {len(sys_files)} / 178 expected -> {'PASS' if sys_ok else 'FAIL'}")
    results["system_configs"] = sys_ok
    
    # 2.6 Language dicts (8 expected)
    print("\n--- Verifying Language dictionaries ---")
    lang_files = [f for f in (ASSETS_DIR / "Language").glob("**/*") if f.is_file()]
    lang_ok = (len(lang_files) == 8)
    print(f"Language files count: {len(lang_files)} / 8 expected -> {'PASS' if lang_ok else 'FAIL'}")
    results["language_dicts"] = lang_ok
    
    # 2.7 Icons (133 expected)
    print("\n--- Verifying Icons ---")
    icon_files = [f for f in (ASSETS_DIR / "Icon").glob("**/*") if f.is_file()]
    icon_ok = (len(icon_files) == 133)
    print(f"Icon files count: {len(icon_files)} / 133 expected -> {'PASS' if icon_ok else 'FAIL'}")
    results["icons"] = icon_ok
    
    # 2.8 CUIX menus (7 expected)
    print("\n--- Verifying CUIX menus ---")
    cuix_files = list(ASSETS_DIR.glob("**/*.cuix"))
    cuix_ok = (len(cuix_files) == 7)
    for c in cuix_files:
        with open(c, "rb") as fp:
            magic = fp.read(4)
        if not magic.startswith(b"PK\x03\x04"):
            cuix_ok = False
            print(f"[FAIL] Invalid CUIX header: {c.name}")
    print(f"CUIX files count: {len(cuix_files)} / 7 expected -> {'PASS' if cuix_ok else 'FAIL'}")
    results["cuix_menus"] = cuix_ok
    
    all_assets_pass = all(results.values())
    print(f"\nCheck 2 Overall Result: {'PASS' if all_assets_pass else 'FAIL'}")
    return all_assets_pass, results

def audit_code_decoupling():
    print("\n" + "=" * 80)
    print("AUDIT CHECK 3: CODE DECOUPLING & ZERO C:\\Dce Pro REFERENCES")
    print("=" * 80)
    
    pattern = re.compile(r'C:(?:\\\\|\\|/)Dce Pro', re.IGNORECASE)
    violations = []
    
    targets = [SRC_DIR, ASSETS_DIR]
    scanned_count = 0
    for target in targets:
        for root, dirs, files in os.walk(target):
            if any(part in ('bin', 'obj', '.git', '.vs') for part in Path(root).parts):
                continue
            for f in files:
                ext = Path(f).suffix.lower()
                # Scan all text-based files
                if ext in ('.cs', '.xaml', '.json', '.xml', '.config', '.txt', '.lsp', '.autostr', '.props', '.csproj', '.bat', '.cmd', '.md'):
                    scanned_count += 1
                    file_path = Path(root) / f
                    try:
                        with open(file_path, 'r', encoding='utf-8', errors='ignore') as fp:
                            for line_no, line in enumerate(fp, 1):
                                if pattern.search(line):
                                    rel = file_path.relative_to(HPDCEPRO_DIR)
                                    violations.append((str(rel), line_no, line.strip()))
                    except Exception as ex:
                        violations.append((str(file_path), 0, f"Error: {ex}"))
                        
    print(f"Scanned {scanned_count} text files in HPDcePro/src and HPDcePro/assets.")
    if violations:
        print(f"[FAIL] Found {len(violations)} occurrences of C:\\Dce Pro:")
        for rel, l, content in violations:
            print(f"  {rel}:{l}: {content[:100]}")
        return False, violations
    else:
        print("[PASS] ZERO occurrences of 'C:\\Dce Pro' found in src/ or assets/!")
        return True, []

def main():
    ok1, r1 = audit_legacy_invariant()
    ok2, r2 = audit_assets_genuineness()
    ok3, r3 = audit_code_decoupling()
    
    all_ok = ok1 and ok2 and ok3
    print("\n" + "=" * 80)
    print(f"FORENSIC SUMMARY: {'CLEAN' if all_ok else 'INTEGRITY VIOLATION'}")
    print("=" * 80)
    return 0 if all_ok else 1

if __name__ == '__main__':
    sys.exit(main())
