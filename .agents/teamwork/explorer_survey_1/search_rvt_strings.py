import re

path = r"C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe"
with open(path, "rb") as f:
    data = f.read()

# Look for Inno Setup script strings or file lists
# Inno Setup stores file entries in its setup header/data
# Often strings in Inno Setup contain filenames like {app}\..., {userappdata}\..., etc.
patterns = [
    rb"\{app\}[^\x00\r\n]+",
    rb"\{code:[^\x00\r\n]+\}",
    rb"\{tmp\}[^\x00\r\n]+",
    rb"\{userappdata\}[^\x00\r\n]+",
    rb"\{commonappdata\}[^\x00\r\n]+",
    rb"[A-Za-z0-9_\-\\]+\.(?:dll|addin|exe|rvt|rfa|txt|png|ico)",
]

found = set()
for p in patterns:
    matches = re.findall(p, data)
    for m in matches:
        try:
            s = m.decode('ascii')
            if len(s) > 3 and not s.startswith("PADDING"):
                found.add(s)
        except:
            pass

print(f"Total matched file/path references in DBIM_RVT_26.exe: {len(found)}")
for s in sorted(found):
    if any(ext in s.lower() for ext in [".dll", ".addin", "revit", "dbim"]):
        print(f"  {s}")
