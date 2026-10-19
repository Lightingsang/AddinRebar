import os
import re

views_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI\Views"
themes_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI\Themes"

# 1. Collect keys from themes
theme_keys = set()
for tfile in os.listdir(themes_dir):
    if tfile.endswith(".xaml"):
        tpath = os.path.join(themes_dir, tfile)
        with open(tpath, "r", encoding="utf-8") as f:
            content = f.read()
            for m in re.finditer(r'x:Key="([^"]+)"', content):
                theme_keys.add(m.group(1))

print(f"Total theme keys collected: {len(theme_keys)}")
for tk in sorted(theme_keys):
    print(f"  Theme Key: {tk}")

# 2. Inspect views
static_res_regex = re.compile(r"\{StaticResource\s+([^}]+)\}")
local_key_regex = re.compile(r'x:Key="([^"]+)"')
forbidden_patterns = ["materialdesigninxaml.net", "MaterialDesignThemes", "MaterialDesignColors"]

mahapps_prefixes = ("MahApps.", "MetroWindow.", "{x:Type")

unresolved_total = 0
forbidden_found = 0

for vfile in sorted(os.listdir(views_dir)):
    if vfile.endswith(".xaml"):
        vpath = os.path.join(views_dir, vfile)
        with open(vpath, "r", encoding="utf-8") as f:
            content = f.read()

        # Check forbidden
        for pat in forbidden_patterns:
            if pat.lower() in content.lower():
                print(f"FORBIDDEN MATERIAL DESIGN FOUND in {vfile}: {pat}")
                forbidden_found += 1

        local_keys = set(m.group(1) for m in local_key_regex.finditer(content))
        static_keys = [m.group(1).split(",")[0].strip() for m in static_res_regex.finditer(content)]

        unresolved = []
        for k in static_keys:
            if k in local_keys or k in theme_keys or any(k.startswith(p) for p in mahapps_prefixes):
                continue
            unresolved.append(k)

        if unresolved:
            print(f"FAIL: {vfile} unresolved: {unresolved}")
            unresolved_total += len(unresolved)
        else:
            print(f"PASS: {vfile} (Static: {len(static_keys)}, Local: {len(local_keys)})")

print(f"\nSummary:")
print(f"Total unresolved keys: {unresolved_total}")
print(f"Total forbidden MaterialDesign references: {forbidden_found}")
