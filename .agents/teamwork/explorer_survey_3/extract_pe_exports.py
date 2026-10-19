import struct
import sys
import os

sys.stdout.reconfigure(encoding='utf-8')

def get_exports(filepath):
    with open(filepath, 'rb') as f:
        data = f.read()

    # DOS Header
    if data[:2] != b'MZ':
        return []
    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]

    # PE Signature
    if data[e_lfanew:e_lfanew+4] != b'PE\0\0':
        return []

    # File Header
    coff_offset = e_lfanew + 4
    num_sections = struct.unpack('<H', data[coff_offset+2:coff_offset+4])[0]
    opt_header_size = struct.unpack('<H', data[coff_offset+16:coff_offset+18])[0]
    opt_offset = coff_offset + 20

    # Optional Header
    magic = struct.unpack('<H', data[opt_offset:opt_offset+2])[0]
    is_64 = (magic == 0x20B)

    # Data Directories: RVA of Export Table is at offset 96 (PE32) or 112 (PE32+)
    data_dir_offset = opt_offset + (112 if is_64 else 96)
    export_rva, export_size = struct.unpack('<II', data[data_dir_offset:data_dir_offset+8])

    if export_rva == 0 or export_size == 0:
        return []

    # Section Headers
    section_headers_offset = opt_offset + opt_header_size
    sections = []
    for i in range(num_sections):
        sec_off = section_headers_offset + i * 40
        sec_name = data[sec_off:sec_off+8].rstrip(b'\0')
        v_size, v_addr, raw_size, raw_ptr = struct.unpack('<IIII', data[sec_off+8:sec_off+24])
        sections.append((v_addr, v_size, raw_ptr, raw_size))

    def rva_to_offset(rva):
        for v_addr, v_size, raw_ptr, raw_size in sections:
            if v_addr <= rva < v_addr + max(v_size, raw_size):
                return raw_ptr + (rva - v_addr)
        return None

    exp_file_off = rva_to_offset(export_rva)
    if not exp_file_off:
        return []

    # IMAGE_EXPORT_DIRECTORY
    # DWORD Characteristics, DWORD TimeDateStamp, WORD MajorVersion, WORD MinorVersion,
    # DWORD Name, DWORD Base, DWORD NumberOfFunctions, DWORD NumberOfNames,
    # DWORD AddressOfFunctions, DWORD AddressOfNames, DWORD AddressOfNameOrdinals
    num_funcs, num_names, addr_funcs, addr_names, addr_ordinals = struct.unpack(
        '<IIIII', data[exp_file_off+20:exp_file_off+40]
    )

    names_off = rva_to_offset(addr_names)
    ordinals_off = rva_to_offset(addr_ordinals)
    funcs_off = rva_to_offset(addr_funcs)

    exports = []
    for i in range(num_names):
        name_rva = struct.unpack('<I', data[names_off + i*4:names_off + i*4 + 4])[0]
        name_off = rva_to_offset(name_rva)
        if name_off:
            end = data.find(b'\0', name_off)
            name = data[name_off:end].decode('ascii', errors='ignore')
            ordinal = struct.unpack('<H', data[ordinals_off + i*2:ordinals_off + i*2 + 2])[0]
            func_rva = struct.unpack('<I', data[funcs_off + ordinal*4:funcs_off + ordinal*4 + 4])[0]
            exports.append((ordinal, name, hex(func_rva)))

    return exports

dll_path = r"C:\Dce Pro\DceAnalyssiss64.dll"
exports = get_exports(dll_path)
print(f"File: {dll_path}")
print(f"Total exports found: {len(exports)}")
for ord_val, name, rva in exports:
    print(f"  [{ord_val:3d}] RVA: {rva:8s}  Name: {name}")

# Also check DceAnalyssiss32.dll for comparison
dll32_path = r"C:\Dce Pro\DceAnalyssiss32.dll"
if os.path.exists(dll32_path):
    exp32 = get_exports(dll32_path)
    print(f"\nFile: {dll32_path}")
    print(f"Total exports found in 32-bit: {len(exp32)}")
