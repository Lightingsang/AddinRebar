import zipfile
import io
import re
import hashlib
from oletools.olevba import VBA_Parser

xlsm_path = r'C:\kata_pro\Kata.xlsm'

with open(xlsm_path, 'rb') as f:
    raw_data = f.read()

sha256 = hashlib.sha256(raw_data).hexdigest().upper()
print(f"File size: {len(raw_data)} bytes")
print(f"File SHA256: {sha256}")

with zipfile.ZipFile(io.BytesIO(raw_data), 'r') as z:
    vba_bin = z.read('xl/vbaProject.bin')

parser = VBA_Parser('vbaProject.bin', data=vba_bin)
macros = list(parser.extract_macros())
print(f"Total modules: {len(macros)}")

for subfilename, stream_path, vba_filename, vba_code in macros:
    print(f"\n--- {vba_filename} ({stream_path}) ---")
    ranges = sorted(set(re.findall(r'Range\(\"([^\"]+)\"\)', vba_code, re.IGNORECASE)))
    cells = sorted(set(re.findall(r'Cells\(([^)]+)\)', vba_code, re.IGNORECASE)))
    print(f"Lines: {len(vba_code.splitlines())}, Bytes: {len(vba_code)}")
    if ranges:
        print(f"  Ranges: {ranges}")
    if cells:
        print(f"  Cells samples: {cells[:10]}")
