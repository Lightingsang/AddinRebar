import os
import glob
from pathlib import Path
import sys

sys.stdout.reconfigure(encoding='utf-8')

dce_root = Path(r"C:\Dce Pro")

if not dce_root.exists():
    print(f"Warning: {dce_root} does not exist on this machine!")
    sys.exit(0)

print(f"Checking C:\\Dce Pro assets:")

# 1. DWG files in Cad and AutoStr
cad_dwgs = list(dce_root.glob("Cad/*.dwg")) + list(dce_root.glob("Cad/**/*.dwg"))
autostr_dwgs = list(dce_root.glob("AutoStr/**/00.FileDungChung/*.dwg")) + list(dce_root.glob("AutoStr/**/*.dwg"))
all_dwgs = set(list(dce_root.glob("**/*.dwg")))
print(f"  Cad DWGs: {len(cad_dwgs)}, AutoStr DWGs: {len(autostr_dwgs)}, Total DWGs in C:\\Dce Pro: {len(all_dwgs)}")

# 2. XLSM workbooks
all_xlsms = list(dce_root.glob("**/*.xlsm"))
print(f"  Total XLSM workbooks: {len(all_xlsms)}")

# 3. MayHoc
mayhoc_db = dce_root / "MayHoc" / "mauhoc.db"
mayhoc_zip = dce_root / "MayHoc" / "model.zip"
print(f"  MayHoc mauhoc.db exists: {mayhoc_db.exists()} (size: {mayhoc_db.stat().st_size if mayhoc_db.exists() else 0} bytes)")
print(f"  MayHoc model.zip exists: {mayhoc_zip.exists()} (size: {mayhoc_zip.stat().st_size if mayhoc_zip.exists() else 0} bytes)")

# 4. System txt files
system_txts = list((dce_root / "System").glob("*.txt"))
system_all = list((dce_root / "System").glob("*"))
print(f"  System/*.txt: {len(system_txts)}, System/* total: {len(system_all)}")

# 5. Language txt files
lang_txts = list((dce_root / "Language").glob("**/*.txt"))
print(f"  Language/**/*.txt: {len(lang_txts)}")

# 6. Icons
icon_pngs = list((dce_root / "Icon").glob("**/*.png"))
icon_jpgs = list((dce_root / "Icon").glob("**/*.jpg"))
icon_icos = list((dce_root / "Icon").glob("**/*.ico"))
print(f"  Icon PNGs: {len(icon_pngs)}, JPGs: {len(icon_jpgs)}, ICOs: {len(icon_icos)}, Total icons: {len(icon_pngs)+len(icon_jpgs)+len(icon_icos)}")

# 7. CUIX menus
cuix_files = list(dce_root.glob("*.cuix")) + list(dce_root.glob("**/*.cuix"))
print(f"  CUIX menus: {len(cuix_files)}")
