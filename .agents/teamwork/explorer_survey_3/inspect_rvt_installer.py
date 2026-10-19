import re
import sys
import os

sys.stdout.reconfigure(encoding='utf-8')

exe_path = r"C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe"
with open(exe_path, "rb") as f:
    data = f.read()

print(f"File: {exe_path}, size: {len(data):,} bytes")

# Search for filenames inside the installer binary (Inno setup file records)
# Look for strings ending in .dll, .addin, etc.
dll_pattern = re.compile(rb'[A-Za-z0-9_\-\.\\]+\.(?:dll|addin|exe|txt|xml|png|ico)', re.IGNORECASE)
matches = set()
for m in dll_pattern.finditer(data):
    s = m.group(0).decode('ascii', errors='ignore')
    if len(s) > 4:
        matches.add(s)

print("\n--- Discovered Filenames in DBIM_RVT_26.exe ---")
for m in sorted(matches):
    if any(ext in m.lower() for ext in ['.dll', '.addin']):
        print(" ", m)

# Search for IExternalCommand or Revit command class names
cmd_pattern = re.compile(rb'[A-Za-z0-9_\.]*(?:Command|Cmd|ExternalCommand|App|Application)[A-Za-z0-9_]*', re.IGNORECASE)
cmd_matches = set()
for m in cmd_pattern.finditer(data):
    s = m.group(0).decode('ascii', errors='ignore')
    if 5 < len(s) < 60 and any(kw in s.lower() for kw in ['dbim', 'dce', 'revit', 'rebar', 'dam', 'cot', 'san']):
        cmd_matches.add(s)

print("\n--- Discovered Command/Type hints in DBIM_RVT_26.exe ---")
for m in sorted(cmd_matches):
    print(" ", m)
