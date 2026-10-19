import io
import os
import zipfile
import hashlib
import olefile
from oletools.olevba import VBA_Project, decompress_stream

SRC_XLSM = r'C:\kata_pro\Kata.xlsm'

def verify_source():
    assert os.path.exists(SRC_XLSM), f"File not found: {SRC_XLSM}"
    size = os.path.getsize(SRC_XLSM)
    with open(SRC_XLSM, 'rb') as f:
        data = f.read()
    sha = hashlib.sha256(data).hexdigest()
    print(f"Source: {SRC_XLSM}")
    print(f"  Size: {size} bytes")
    print(f"  SHA-256: {sha}")
    return sha

def extract_vba_in_memory():
    with zipfile.ZipFile(SRC_XLSM, 'r') as z:
        bin_data = z.read('xl/vbaProject.bin')
    print(f"Read xl/vbaProject.bin in-memory: {len(bin_data)} bytes")
    
    ole = olefile.OleFileIO(io.BytesIO(bin_data))
    project = VBA_Project(ole, '', 'PROJECT', 'VBA/dir')
    project.parse_project_stream()
    list(project.parse_modules())
    
    print(f"Extracted modules count: {len(project.modules)}")
    modules_info = []
    for mod in project.modules:
        modules_info.append({
            'name': mod.name,
            'filename': mod.filename_str,
            'stream': mod.streamname,
            'offset': mod.textoffset,
            'size': len(mod.code_raw) if mod.code_raw else 0,
            'lines': len(mod.code.splitlines()) if mod.code else 0,
            'code': mod.code
        })
    return modules_info

if __name__ == '__main__':
    sha = verify_source()
    mods = extract_vba_in_memory()
    for m in mods:
        print(f"  [{m['filename']}] stream: VBA/{m['stream']}, offset: {m['offset']}, size: {m['size']}, lines: {m['lines']}")
