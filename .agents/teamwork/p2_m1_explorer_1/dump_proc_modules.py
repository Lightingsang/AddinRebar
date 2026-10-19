import zipfile, io, olefile
from oletools.olevba import VBA_Project

path = r'C:\kata_pro\Kata.xlsm'
with zipfile.ZipFile(path, 'r') as z:
    bin_data = z.read('xl/vbaProject.bin')

ole = olefile.OleFileIO(io.BytesIO(bin_data))
p = VBA_Project(ole, '', 'PROJECT', 'VBA/dir')
p.parse_project_stream()
list(p.parse_modules())

proc_modules = ['Ve_dam', 'Tinh_dam', 'to_hop', 'save_info', 'Module1']

with open('proc_modules_dump.txt', 'w', encoding='utf-8') as out:
    for name in proc_modules:
        for mod in p.modules:
            if mod.name == name:
                out.write(f"============================================================\n")
                out.write(f"MODULE: {mod.name} ({mod.filename_str})\n")
                out.write(f"Decompressed Size: {len(mod.code_raw)} bytes, Lines: {len(mod.code.splitlines())}\n")
                out.write(f"============================================================\n")
                lines = mod.code.splitlines()
                for idx, line in enumerate(lines):
                    out.write(f"{idx+1:4d}: {line}\n")
                out.write("\n\n")
