import re

edb_path = r'C:\kata_pro\ISO_KATA.EDB'
with open(edb_path, 'rb') as f:
    data = f.read()

print(f"File size: {len(data):,} bytes")
print(f"Header hex (64 bytes): {data[:64].hex()}")
print(f"Header ASCII: {data[:64]}")

# Look for text strings in EDB (Microsoft Access / Jet DB or CSI proprietary or SQLite?)
# Jet DB magic: \x00\x01\x00\x00Standard Jet DB... or Standard ACE DB
if b'Standard Jet DB' in data[:64] or b'Standard ACE DB' in data[:64]:
    print("Format: Microsoft Jet / ACE Database (MDB)")
elif data[:16] == b'SQLite format 3\x00':
    print("Format: SQLite Database")
else:
    print("Format: CSI ETABS binary proprietary format / OLE compound file")

# Find readable strings in EDB
strings = re.findall(rb'[\x20-\x7E]{4,}', data)
decoded_strings = [s.decode('latin-1') for s in strings]
print(f"Total ASCII strings found: {len(decoded_strings)}")

# Look for story names, materials, frame sections, load cases
stories = [s for s in decoded_strings if re.match(r'^(Story\d+|TANG\s*\d+|FL\d+|BASE|ROOF)', s, re.IGNORECASE)]
materials = [s for s in decoded_strings if re.match(r'^(B\d+|C\d+|CONC|STEEL|REBAR|ASTM|CB\d+)', s, re.IGNORECASE)]
sections = [s for s in decoded_strings if re.match(r'^[B,C,D,W]\d+[xX]\d+', s)]

print(f"Sample Stories detected: {list(set(stories))[:10]}")
print(f"Sample Materials detected: {list(set(materials))[:10]}")
print(f"Sample Sections detected: {list(set(sections))[:10]}")

# Look for ETABS version strings
version_strings = [s for s in decoded_strings if 'ETABS' in s or 'CSI' in s or 'Version' in s]
print(f"Version / CSI tags: {list(set(version_strings))[:15]}")
