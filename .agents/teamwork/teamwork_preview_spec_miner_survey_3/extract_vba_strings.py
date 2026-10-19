import zipfile
import re

with zipfile.ZipFile(r"C:\kata_pro\Kata.xlsm") as z:
    vba_data = z.read("xl/vbaProject.bin")

print("vbaProject.bin size:", len(vba_data))

# Extract ASCII strings of length >= 4
ascii_strings = re.findall(b'[\x20-\x7E]{4,}', vba_data)
# Decode and find interesting lines
interesting = []
for b in ascii_strings:
    try:
        s = b.decode('ascii')
        if any(w in s.lower() for w in ['sub ', 'function ', 'dim ', 'kata', 'tra_bang', 'small', 'to_hop', 'coupler', 'tem', 'thep']):
            interesting.append(s)
    except:
        pass

print(f"Total interesting strings: {len(interesting)}")
for s in interesting[:60]:
    print("  ", s)
