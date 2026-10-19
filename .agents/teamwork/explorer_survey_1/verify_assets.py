import sys
import hashlib
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')
base = Path(r"C:\Dce Pro")

def count_and_size(pattern, desc):
    files = list(base.glob(pattern))
    total_size = sum(f.stat().st_size for f in files)
    print(f"{desc}: {len(files)} files ({total_size:,} bytes, {round(total_size/(1024*1024), 2)} MB)")
    return files

print("=== Inventory of Static Assets in C:\\Dce Pro ===")
dwg_files = count_and_size("**/*.dwg", "AutoCAD DWG templates")
xlsm_files = count_and_size("Excel/**/*.xlsm", "Excel XLSM workbooks")
sys_files = count_and_size("System/**/*.txt", "System TXT configuration files")
lang_files = count_and_size("Language/**/*.txt", "Language TXT dictionaries")
icon_files = count_and_size("Icon/**/*.*", "Icon files")
cuix_files = count_and_size("**/*.cuix", "CUIX menu files")
lsp_files = count_and_size("**/*.lsp", "AutoLISP files")

mauhoc_db = base / "MayHoc" / "mauhoc.db"
model_zip = base / "MayHoc" / "model.zip"

print("\n--- MayHoc (Machine Learning) Files ---")
if mauhoc_db.exists():
    print(f"mauhoc.db: {mauhoc_db.stat().st_size:,} bytes")
if model_zip.exists():
    print(f"model.zip: {model_zip.stat().st_size:,} bytes")
