import struct

def get_exports(dll_path):
    with open(dll_path, 'rb') as f:
        data = f.read()

    # DOS Header
    if data[:2] != b'MZ':
        return "Not MZ"
    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]
    
    # NT Headers
    if data[e_lfanew:e_lfanew+4] != b'PE\x00\x00':
        return "Not PE"
    
    machine = struct.unpack('<H', data[e_lfanew+4:e_lfanew+6])[0]
    is_64 = (machine == 0x8664)
    
    opt_header_offset = e_lfanew + 24
    if is_64:
        magic = struct.unpack('<H', data[opt_header_offset:opt_header_offset+2])[0]
        # Data directories start at offset 112 in 64-bit opt header
        export_rva = struct.unpack('<I', data[opt_header_offset+112:opt_header_offset+116])[0]
        export_size = struct.unpack('<I', data[opt_header_offset+116:opt_header_offset+120])[0]
    else:
        # Data directories start at offset 96 in 32-bit opt header
        export_rva = struct.unpack('<I', data[opt_header_offset+96:opt_header_offset+100])[0]
        export_size = struct.unpack('<I', data[opt_header_offset+100:opt_header_offset+104])[0]
        
    if export_rva == 0:
        return []
    
    num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
    size_opt_header = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
    section_headers_offset = opt_header_offset + size_opt_header
    
    sections = []
    for i in range(num_sections):
        sec_offset = section_headers_offset + i * 40
        sec_name = data[sec_offset:sec_offset+8].rstrip(b'\x00').decode('latin1')
        vsize = struct.unpack('<I', data[sec_offset+8:sec_offset+12])[0]
        vrva = struct.unpack('<I', data[sec_offset+12:sec_offset+16])[0]
        rsize = struct.unpack('<I', data[sec_offset+16:sec_offset+20])[0]
        roffset = struct.unpack('<I', data[sec_offset+20:sec_offset+24])[0]
        sections.append({
            'name': sec_name, 'vrva': vrva, 'vsize': vsize,
            'roffset': roffset, 'rsize': rsize
        })
        
    def rva_to_offset(rva):
        for sec in sections:
            if sec['vrva'] <= rva < sec['vrva'] + max(sec['vsize'], sec['rsize']):
                return sec['roffset'] + (rva - sec['vrva'])
        return None

    exp_offset = rva_to_offset(export_rva)
    if exp_offset is None:
        return f"Cannot find section for export RVA 0x{export_rva:X}"
        
    # Export directory table:
    # 0x10: TimeDateStamp
    # 0x14: MajorVersion / MinorVersion
    # 0x18: Name RVA
    # 0x1C: OrdinalBase
    # 0x20: NumberOfFunctions
    # 0x24: NumberOfNames
    # 0x28: AddressOfFunctions RVA
    # 0x2C: AddressOfNames RVA
    # 0x30: AddressOfNameOrdinals RVA
    num_funcs = struct.unpack('<I', data[exp_offset+0x14:exp_offset+0x18])[0] # wait, check layout
    ordinal_base = struct.unpack('<I', data[exp_offset+0x10:exp_offset+0x14])[0]
    num_funcs = struct.unpack('<I', data[exp_offset+0x14:exp_offset+0x18])[0]
    num_names = struct.unpack('<I', data[exp_offset+0x18:exp_offset+0x1C])[0]
    addr_funcs_rva = struct.unpack('<I', data[exp_offset+0x1C:exp_offset+0x20])[0]
    addr_names_rva = struct.unpack('<I', data[exp_offset+0x20:exp_offset+0x24])[0]
    addr_ordinals_rva = struct.unpack('<I', data[exp_offset+0x24:exp_offset+0x28])[0]

    # Let's adjust based on standard IMAGE_EXPORT_DIRECTORY
    # DWORD Characteristics; (0x00)
    # DWORD TimeDateStamp;   (0x04)
    # WORD MajorVersion;     (0x08)
    # WORD MinorVersion;     (0x0A)
    # DWORD Name;            (0x0C)
    # DWORD Base;            (0x10)
    # DWORD NumberOfFunctions; (0x14)
    # DWORD NumberOfNames;     (0x18)
    # DWORD AddressOfFunctions;     (0x1C)
    # DWORD AddressOfNames;         (0x20)
    # DWORD AddressOfNameOrdinals;  (0x24)
    
    num_names = struct.unpack('<I', data[exp_offset+0x18:exp_offset+0x1C])[0]
    names_offset = rva_to_offset(struct.unpack('<I', data[exp_offset+0x20:exp_offset+0x24])[0])
    ordinals_offset = rva_to_offset(struct.unpack('<I', data[exp_offset+0x24:exp_offset+0x28])[0])
    
    exports = []
    if names_offset:
        for i in range(num_names):
            name_rva = struct.unpack('<I', data[names_offset + i*4 : names_offset + (i+1)*4])[0]
            name_off = rva_to_offset(name_rva)
            if name_off:
                end = data.find(b'\x00', name_off)
                name = data[name_off:end].decode('ascii', errors='ignore')
                exports.append(name)
    return exports

for target in [r"C:\Dce Pro\DceAnalyssiss64.dll", r"C:\Dce Pro\DceAnalyssiss32.dll", r"C:\Dce Pro\V.2020\DceAnalyssiss64.dll"]:
    res = get_exports(target)
    print(f"=== Exports of {target} ===")
    if isinstance(res, list):
        print(f"Total exported functions: {len(res)}")
        for e in sorted(res):
            print(f"  {e}")
    else:
        print(f"  {res}")
