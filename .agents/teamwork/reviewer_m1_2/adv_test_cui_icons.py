import os
import xml.etree.ElementTree as ET

print("=" * 60)
print("ADVERSARIAL STRESS TEST 1: CUI Icon Target Verification")
cui_path = r"C:\HPKata\menukata.cui"
tree = ET.parse(cui_path)
root = tree.getroot()

missing_icons = []
checked_icons = 0
for elem in root.iter():
    # Check attributes and text for icon paths
    vals = list(elem.attrib.values())
    if elem.text:
        vals.append(elem.text)
    for v in vals:
        if isinstance(v, str) and ("C:\\HPKata\\Icon\\" in v or "Icon\\" in v):
            checked_icons += 1
            icon_path = v.strip()
            if not os.path.isabs(icon_path):
                icon_path = os.path.join(r"C:\HPKata", icon_path)
            if not os.path.exists(icon_path):
                missing_icons.append(icon_path)

print(f"Total icon paths referenced in menukata.cui: {checked_icons}")
print(f"Missing icon files on disk: {len(missing_icons)}")
if missing_icons:
    for m in missing_icons:
        print("  Missing:", m)
else:
    print("PASS: Every single icon path referenced in menukata.cui exists on disk in C:\\HPKata\\Icon\\!")

print("\n" + "=" * 60)
print("ADVERSARIAL STRESS TEST 2: Inspect Goi lenh.lsp Rewritten Commands")
with open(r"C:\HPKata\Goi lenh.lsp", "r", encoding="latin1") as f:
    lines = f.readlines()

print("Lines 1-25 of C:\\HPKata\\Goi lenh.lsp:")
for idx, l in enumerate(lines[:25]):
    print(f"{idx+1:3d}: {l.rstrip()}")

print("\n" + "=" * 60)
print("ADVERSARIAL STRESS TEST 3: Inspect GoiLenh_HPKata.lsp Rewritten Commands")
with open(r"C:\HPKata\GoiLenh_HPKata.lsp", "r", encoding="latin1") as f:
    hplines = f.readlines()

print("Lines 1-25 of C:\\HPKata\\GoiLenh_HPKata.lsp:")
for idx, l in enumerate(hplines[:25]):
    print(f"{idx+1:3d}: {l.rstrip()}")
