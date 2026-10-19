import struct

def get_exports(filepath):
    with open(filepath, 'rb') as f:
        data = f.read()

    # DOS Header
    if data[:2] != b'MZ':
        return []
    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]

    # PE Signature
    if data[e_lfanew:e_lfanew+4] != b'PE\x00\x00':
        return []

    # Optional Header
    magic = struct.unpack('<H', data[e_lfanew+24:e_lfanew+26])[0]
    if magic == 0x10b: # PE32
        export_rva = struct.unpack('<I', data[e_lfanew+120:e_lfanew+124])[0]
        export_size = struct.unpack('<I', data[e_lfanew+124:e_lfanew+128])[0]
        num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
        opt_hdr_size = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
        sec_offset = e_lfanew + 24 + opt_hdr_size
    elif magic == 0x20b: # PE32+
        export_rva = struct.unpack('<I', data[e_lfanew+136:e_lfanew+140])[0]
        export_size = struct.unpack('<I', data[e_lfanew+140:e_lfanew+144])[0]
        num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
        opt_hdr_size = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
        sec_offset = e_lfanew + 24 + opt_hdr_size
    else:
        return []

    if export_rva == 0:
        return []

    # Section Headers
    sections = []
    for i in range(num_sections):
        s_data = data[sec_offset + i*40 : sec_offset + (i+1)*40]
        s_name = s_data[:8].rstrip(b'\x00').decode('latin1')
        s_vsize, s_va, s_rawsize, s_rawptr = struct.unpack('<IIII', s_data[8:24])
        sections.append((s_va, s_vsize, s_rawptr, s_rawsize))

    def rva_to_offset(rva):
        for s_va, s_vsize, s_rawptr, s_rawsize in sections:
            if s_va <= rva < s_va + max(s_vsize, s_rawsize):
                return s_rawptr + (rva - s_va)
        return None

    exp_offset = rva_to_offset(export_rva)
    if not exp_offset:
        return []

    num_funcs, num_names, funcs_rva, names_rva, ords_rva = struct.unpack('<IIIII', data[exp_offset+20:exp_offset+40])
    names_offset = rva_to_offset(names_rva)

    exports = []
    for i in range(num_names):
        name_rva = struct.unpack('<I', data[names_offset + i*4: names_offset + (i+1)*4])[0]
        name_off = rva_to_offset(name_rva)
        name_end = data.find(b'\x00', name_off)
        name = data[name_off:name_end].decode('utf-8', errors='ignore')
        exports.append(name)
    return exports

exports = get_exports(r'C:\Dce Pro\DceAnalyssiss64.dll')
print(f'Total exports in DceAnalyssiss64.dll: {len(exports)}')
for e in sorted(exports):
    print(f'  {e}')
