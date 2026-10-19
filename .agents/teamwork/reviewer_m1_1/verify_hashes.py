import os
import hashlib

files = [
    r'C:\Dce Pro\DCE Pro.dll',
    r'C:\Dce Pro\DceAnalyssiss64.dll',
    r'C:\Dce Pro\DCE Pro.lsp',
    r'C:\Dce Pro\AutoStr\DuAnMau.autostr',
    r'C:\Dce Pro\MayHoc\mauhoc.db',
    r'C:\Dce Pro\AddinSteel\DceSteel.dll',
    r'C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll',
    r'C:\Dce Pro\V.2020\DCE Excel.dll',
    r'C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe',
    r'C:\Dce Pro\DceAnalyssiss32.dll',
    r'C:\Dce Pro\ZwDCE Pro.dll',
    r'C:\Dce Pro\IcDCE Pro.dll',
    r'C:\Dce Pro\DceUpdate\DceUpdate.exe'
]

print(f"{'Filename':<35} | {'Size (bytes)':<12} | {'SHA-256'}")
print("-" * 115)
for f in files:
    if os.path.exists(f):
        size = os.path.getsize(f)
        with open(f, 'rb') as fp:
            data = fp.read()
            sha256 = hashlib.sha256(data).hexdigest()
        print(f"{os.path.basename(f):<35} | {size:<12} | {sha256}")
    else:
        print(f"{os.path.basename(f):<35} | {'NOT FOUND':<12} | -")
