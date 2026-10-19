import zipfile
import olefile
import io

xlsm_path = r'C:\kata_pro\Kata.xlsm'
with zipfile.ZipFile(xlsm_path, 'r') as z:
    vba_bin = z.read('xl/vbaProject.bin')

ole = olefile.OleFileIO(io.BytesIO(vba_bin))
print("=== vbaProject.bin OLE STREAMS ===")
streams = ole.listdir()
for s in streams:
    size = ole.get_size(s)
    print(f"Stream: {'/'.join(s):<30} | Size: {size:,} bytes")

# Read PROJECT stream
if ole.exists('PROJECT'):
    proj_text = ole.openstream('PROJECT').read().decode('latin-1', errors='ignore')
    print("\n--- PROJECT Stream Content ---")
    for line in proj_text.splitlines():
        if any(line.startswith(k) for k in ['ID=', 'Module=', 'Class=', 'BaseClass=', 'Document=', 'Name=', 'HelpFile=', 'DPB=', 'CMG=', 'GC=']):
            print(line)

# Check if dir stream exists
print("\n--- dir stream check ---")
if ole.exists('dir'):
    dir_data = ole.openstream('dir').read()
    print(f"dir stream size: {len(dir_data):,} bytes")
