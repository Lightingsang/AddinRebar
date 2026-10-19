import re

files = ["DceRebarXDataTKT_decompiled.cs", "Links_XDataObject_decompiled.cs"]
base = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\\"

for fname in files:
    p = base + fname
    with open(p, "rb") as f:
        data = f.read()
    if data.startswith(b'\xff\xfe'):
        text = data.decode('utf-16le')
    else:
        text = data.decode('utf-8', errors='replace')
    with open(p, "w", encoding="utf-8") as f:
        f.write(text)

    print(f"=== {fname} ===")
    reg_apps = re.findall(r'"([A-Za-z0-9_]{3,30})"', text)
    reg_app_candidates = [r for r in reg_apps if any(k in r.lower() for k in ['dce', 'rebar', 'xdata', 'link', 'tkt', 'thep'])]
    print(f"  RegApp candidates: {set(reg_app_candidates)}")
    methods = re.findall(r'public (?:static )?[^\(\)]+ \w+\([^\)]*\)', text)
    print(f"  Public methods ({len(methods)}):")
    for m in methods[:15]:
        print("    " + m.strip())
