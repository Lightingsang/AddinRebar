import xml.etree.ElementTree as ET
import os
import re

cui_path = r"C:\HPKata\menukata.cui"
with open(cui_path, "r", encoding="utf-8", errors="ignore") as fp:
    raw_content = fp.read()

tree = ET.parse(cui_path)
root = tree.getroot()

print("Root tag:", root.tag)

# Find all paths or icon references
matches = re.findall(r'[A-Za-z]:\\[^<>\r\n\"]+', raw_content)
print(f"Total path strings found by regex: {len(matches)}")
broken = []
for m in matches:
    exists = os.path.exists(m)
    status = "EXISTS" if exists else "BROKEN"
    if not exists:
        broken.append(m)
    print(f"  [{status}] {m}")

# Check any element tags that mention image or icon
icon_elements = []
for elem in root.iter():
    if "icon" in elem.tag.lower() or "image" in elem.tag.lower() or "bmp" in (elem.text or "").lower():
        icon_elements.append((elem.tag, elem.attrib, (elem.text or "").strip()))

print(f"\nElements related to icons/images: {len(icon_elements)}")
for tag, attr, txt in icon_elements[:20]:
    print(f"  <{tag} {attr}>{txt}</{tag}>")

print(f"\nBroken paths count: {len(broken)}")
