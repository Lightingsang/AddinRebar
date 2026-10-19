import os
import sys
import glob
import hashlib

sys.stdout.reconfigure(encoding='utf-8')

dce_base = r"C:\Dce Pro"

def sha256_file(filepath):
    h = hashlib.sha256()
    with open(filepath, 'rb') as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

print("=================================================================")
print("STATIC ENGINEERING ASSETS INVENTORY IN C:\\Dce Pro (READ-ONLY)")
print("=================================================================")

# 1. DWG Templates
dwg_cad = glob.glob(os.path.join(dce_base, "Cad", "*.dwg"))
dwg_autostr = glob.glob(os.path.join(dce_base, "AutoStr", "00.FileDungChung", "*.dwg"))
all_dwgs = sorted(dwg_cad + dwg_autostr)
print(f"\n1. DWG Templates ({len(all_dwgs)} files):")
print(f"   - Cad\\*.dwg: {len(dwg_cad)} files")
print(f"   - AutoStr\\00.FileDungChung\\*.dwg: {len(dwg_autostr)} files")
for p in all_dwgs:
    rel = os.path.relpath(p, dce_base)
    sz = os.path.getsize(p)
    h = sha256_file(p)[:16]
    print(f"   * [{sz:10,d} B] {rel:45s} (SHA256: {h}...)")

# Also check if there are other DWGs anywhere in C:\Dce Pro
other_dwgs = [p for p in glob.glob(os.path.join(dce_base, "**", "*.dwg"), recursive=True) if p not in all_dwgs]
if other_dwgs:
    print(f"\n   Additional DWGs found in C:\\Dce Pro ({len(other_dwgs)} files):")
    for p in other_dwgs:
        rel = os.path.relpath(p, dce_base)
        sz = os.path.getsize(p)
        print(f"   * [{sz:10,d} B] {rel}")

# 2. XLSM Calculation Workbooks
xlsm_files = sorted(glob.glob(os.path.join(dce_base, "Excel", "**", "*.xlsm"), recursive=True))
print(f"\n2. XLSM Calculation Workbooks in Excel\\**\\*.xlsm ({len(xlsm_files)} files):")
# Subfolder breakdown
xlsm_sub = {}
for p in xlsm_files:
    rel = os.path.relpath(p, os.path.join(dce_base, "Excel"))
    folder = os.path.dirname(rel) or "Root"
    xlsm_sub[folder] = xlsm_sub.get(folder, 0) + 1
for folder, count in sorted(xlsm_sub.items()):
    print(f"   - Excel\\{folder}: {count} workbooks")

# Also check for .xls or other Excel files in C:\Dce Pro
all_excel = glob.glob(os.path.join(dce_base, "Excel", "**", "*.*"), recursive=True)
non_xlsm = [p for p in all_excel if os.path.isfile(p) and not p.lower().endswith(".xlsm")]
print(f"   Non-xlsm files in Excel\\: {len(non_xlsm)} files (e.g. {', '.join(set(os.path.splitext(p)[1] for p in non_xlsm))})")

# 3. MayHoc
print("\n3. Machine Learning (MayHoc):")
mayhoc_dir = os.path.join(dce_base, "MayHoc")
if os.path.exists(mayhoc_dir):
    for f in sorted(os.listdir(mayhoc_dir)):
        p = os.path.join(mayhoc_dir, f)
        if os.path.isfile(p):
            sz = os.path.getsize(p)
            h = sha256_file(p)[:16]
            print(f"   * [{sz:12,d} B] MayHoc\\{f:25s} (SHA256: {h}...)")

# 4. System Config Files
sys_txt = sorted(glob.glob(os.path.join(dce_base, "System", "*.txt")))
print(f"\n4. System Configuration Files in System\\*.txt ({len(sys_txt)} files):")
# Sample first 10 and last 10
for p in sys_txt[:10]:
    rel = os.path.relpath(p, dce_base)
    sz = os.path.getsize(p)
    print(f"   * [{sz:6,d} B] {rel}")
print(f"   ... ({len(sys_txt)-20} more files) ...")
for p in sys_txt[-10:]:
    rel = os.path.relpath(p, dce_base)
    sz = os.path.getsize(p)
    print(f"   * [{sz:6,d} B] {rel}")

# 5. Language Localization Dictionaries
lang_txt = sorted(glob.glob(os.path.join(dce_base, "Language", "**", "*.txt"), recursive=True))
print(f"\n5. Language Dictionaries in Language\\**\\*.txt ({len(lang_txt)} files):")
for p in lang_txt:
    rel = os.path.relpath(p, dce_base)
    sz = os.path.getsize(p)
    h = sha256_file(p)[:16]
    # count lines
    with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
        lines = len(fp.readlines())
    print(f"   * [{sz:8,d} B, {lines:4d} lines] {rel:30s} (SHA256: {h}...)")

# 6. Icons & CUIX Menus
cuix_files = sorted(glob.glob(os.path.join(dce_base, "*.cuix")) + glob.glob(os.path.join(dce_base, "*.cui*")))
print(f"\n6. CUIX / Menu Files ({len(cuix_files)} files):")
for p in cuix_files:
    rel = os.path.relpath(p, dce_base)
    sz = os.path.getsize(p)
    h = sha256_file(p)[:16]
    print(f"   * [{sz:10,d} B] {rel:25s} (SHA256: {h}...)")

icon_files = glob.glob(os.path.join(dce_base, "Icon", "**", "*.*"), recursive=True)
icon_files = [p for p in icon_files if os.path.isfile(p)]
print(f"\n   Icons in Icon\\** ({len(icon_files)} files):")
icon_exts = {}
for p in icon_files:
    ext = os.path.splitext(p)[1].lower()
    icon_exts[ext] = icon_exts.get(ext, 0) + 1
for ext, count in sorted(icon_exts.items()):
    print(f"   - {ext}: {count} files")
