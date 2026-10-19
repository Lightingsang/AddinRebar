#!/usr/bin/env python3
import os
import re
from pathlib import Path

assets_dir = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\assets")

# 1. Normalize DuAnMau.autostr
autostr_file = assets_dir / "AutoStr" / "DuAnMau.autostr"
if autostr_file.exists():
    content = autostr_file.read_text(encoding="utf-8")
    content = content.replace(r"C:\\Dce Pro\\AutoStr", "AutoStr")
    content = content.replace(r"C:\\Dce Pro\\Excel", "Excel")
    content = content.replace(r"C:\\Dce Pro\\", "")
    content = content.replace(r"C:\Dce Pro\\", "")
    content = content.replace(r"C:\Dce Pro", "")
    autostr_file.write_text(content, encoding="utf-8")
    print(f"Normalized {autostr_file}")

# 2. Normalize System/*.txt
system_dir = assets_dir / "System"
for f in system_dir.glob("*.txt"):
    try:
        content = f.read_text(encoding="utf-8", errors="ignore")
        if re.search(r'C:(\\)?\\Dce Pro', content, re.IGNORECASE):
            content = re.sub(r'C:(\\)?\\Dce Pro(\\)?', '', content, flags=re.IGNORECASE)
            f.write_text(content, encoding="utf-8")
            print(f"Normalized {f.name}")
    except Exception as ex:
        print(f"Error normalizing {f}: {ex}")

# 3. Normalize Language/**/*.txt
lang_dir = assets_dir / "Language"
for f in lang_dir.glob("**/*.txt"):
    try:
        content = f.read_text(encoding="utf-8", errors="ignore")
        if re.search(r'C:(\\)?\\Dce Pro', content, re.IGNORECASE):
            content = re.sub(r'C:(\\)?\\Dce Pro(\\)?', '', content, flags=re.IGNORECASE)
            f.write_text(content, encoding="utf-8")
            print(f"Normalized {f.name}")
    except Exception as ex:
        print(f"Error normalizing {f}: {ex}")

print("Asset normalization complete.")
