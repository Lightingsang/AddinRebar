import os
import re

target = r"C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe"
print(f"Inspecting: {target}")

with open(target, "rb") as f:
    data = f.read()

print(f"Size: {len(data)} bytes")

# Extract ASCII strings
ascii_strings = re.findall(b"[\x20-\x7e]{4,}", data)
# Extract UTF-16 strings
utf16_strings = re.findall(b"(?:[\x20-\x7e]\x00){4,}", data)

def decode_utf16(b):
    try:
        return b.decode('utf-16le')
    except:
        return ""

decoded_u16 = [decode_utf16(b) for b in utf16_strings]
decoded_ascii = [b.decode('ascii', errors='ignore') for b in ascii_strings]

all_strings = set(decoded_ascii + decoded_u16)

keywords = ["Inno", "Nullsoft", "InstallShield", "Wise", "AutoIt", "Extract", "Revit", "addin", "dll", "DBIM", "DCE", "Setup", "Version", "Product"]

print("\n--- Key string detections ---")
for kw in keywords:
    matches = [s for s in all_strings if kw.lower() in s.lower()]
    print(f"Keyword '{kw}': {len(matches)} matches")
    for m in matches[:5]:
        print(f"  - {m[:100]}")

print("\n--- Looking for embedded paths, dlls, addins ---")
interesting = [s for s in all_strings if any(ext in s.lower() for ext in [".dll", ".addin", ".exe", "revit", "c:\\", "autodesk"])]
print(f"Total interesting paths/files: {len(interesting)}")
for s in sorted(interesting)[:30]:
    print(f"  {s}")
