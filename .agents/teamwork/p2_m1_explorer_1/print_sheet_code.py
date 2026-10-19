import zipfile, io, olefile
from oletools.olevba import VBA_Project

path = r'C:\kata_pro\Kata.xlsm'
with zipfile.ZipFile(path, 'r') as z:
    bin_data = z.read('xl/vbaProject.bin')

ole = olefile.OleFileIO(io.BytesIO(bin_data))
p = VBA_Project(ole, '', 'PROJECT', 'VBA/dir')
p.parse_project_stream()
list(p.parse_modules())

for mod in p.modules:
    if mod.name.startswith('Sheet') or mod.name == 'ThisWorkbook':
        print(f"=== {mod.name} ({mod.filename_str}) ===")
        print(mod.code)
