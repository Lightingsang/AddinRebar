import pefile

dll_path = r'C:\Dce Pro\DceAnalyssiss64.dll'
pe = pefile.PE(dll_path)

exports = []
if hasattr(pe, 'DIRECTORY_ENTRY_EXPORT'):
    for exp in pe.DIRECTORY_ENTRY_EXPORT.symbols:
        name = exp.name.decode('utf-8') if exp.name else f'ordinal_{exp.ordinal}'
        exports.append(name)

print(f'Total exports in DceAnalyssiss64.dll: {len(exports)}')
print('Export names:')
for e in sorted(exports):
    print(f'  {e}')
