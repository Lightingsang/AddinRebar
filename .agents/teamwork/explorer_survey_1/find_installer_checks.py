with open(r"C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe", "rb") as f:
    data = f.read()

import re
# Find all ASCII and UTF-16 strings with length >= 5
ascii_strs = [s.decode('ascii', errors='ignore') for s in re.findall(b"[\x20-\x7e]{5,}", data)]
utf16_strs = [s.decode('utf-16le', errors='ignore') for s in re.findall(b"(?:[\x20-\x7e]\x00){5,}", data)]
all_strs = ascii_strs + utf16_strs

for s in all_strs:
    low = s.lower()
    if any(k in low for k in ["reg", "software\\autodesk", "revit", "2026", "addin", "install", "exit", "abort", "not found"]):
        if len(s) < 200:
            print(s)
