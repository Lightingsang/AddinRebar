import shutil
import hashlib
import os

SRC = r'C:\kata_pro\Kata.xlsm'
DST1 = r'C:\HPKata\Kata.xlsm'
DST2 = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm'

EXPECTED_SHA = '89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D'
EXPECTED_LEN = 474272

def get_hash_and_len(path):
    with open(path, 'rb') as f:
        data = f.read()
    return hashlib.sha256(data).hexdigest().upper(), len(data)

# 1. Verify source first
src_hash, src_len = get_hash_and_len(SRC)
print(f"Source: {SRC}")
print(f"  Length: {src_len} (expected {EXPECTED_LEN})")
print(f"  SHA256: {src_hash}")
assert src_hash == EXPECTED_SHA, f"Source SHA256 mismatch: {src_hash}"
assert src_len == EXPECTED_LEN, f"Source Length mismatch: {src_len}"

# 2. Copy to DST1
os.makedirs(os.path.dirname(DST1), exist_ok=True)
shutil.copy2(SRC, DST1)

# 3. Copy to DST2
os.makedirs(os.path.dirname(DST2), exist_ok=True)
shutil.copy2(SRC, DST2)

# 4. Verify all three files
for p in [SRC, DST1, DST2]:
    h, l = get_hash_and_len(p)
    print(f"Verified {p}:")
    print(f"  Length: {l} bytes")
    print(f"  SHA256: {h}")
    assert h == EXPECTED_SHA, f"Hash mismatch on {p}: {h}"
    assert l == EXPECTED_LEN, f"Length mismatch on {p}: {l}"

print("\nALL 3 FILES VERIFIED AND IDENTICAL! C:\\kata_pro\\ remains 100% unmodified.")
