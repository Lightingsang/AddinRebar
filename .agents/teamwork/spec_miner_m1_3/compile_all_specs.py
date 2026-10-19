import sys
import json
import struct

sys.stdout.reconfigure(encoding='utf-8')

# 1. Load Excel Actions
with open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json', 'r', encoding='utf-8') as f:
    excel_data = json.load(f)['muc']

# 2. Load DceSteel Functions
with open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json', 'r', encoding='utf-8') as f:
    thep_data = json.load(f)['muc']

# 3. Load MBKC Functions
with open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_mbkc.json', 'r', encoding='utf-8') as f:
    mbkc_data = json.load(f)['muc']

# 4. Extract Native Exports
def get_native_exports(dll_path):
    with open(dll_path, 'rb') as f:
        data = f.read()
    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]
    opt_header_offset = e_lfanew + 24
    export_rva = struct.unpack('<I', data[opt_header_offset+112:opt_header_offset+116])[0]
    num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
    size_opt_header = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
    section_headers_offset = opt_header_offset + size_opt_header
    sections = []
    for i in range(num_sections):
        sec_offset = section_headers_offset + i * 40
        sec_name = data[sec_offset:sec_offset+8].rstrip(b'\x00').decode('latin1')
        vsize, vrva, rsize, roffset = struct.unpack('<IIII', data[sec_offset+8:sec_offset+24])
        sections.append({'name': sec_name, 'vrva': vrva, 'vsize': vsize, 'roffset': roffset, 'rsize': rsize})

    def rva_to_offset(rva):
        for sec in sections:
            if sec['vrva'] <= rva < sec['vrva'] + max(sec['vsize'], sec['rsize']):
                return sec['roffset'] + (rva - sec['vrva'])
        return None

    exp_offset = rva_to_offset(export_rva)
    base = struct.unpack('<I', data[exp_offset+0x10:exp_offset+0x14])[0]
    num_names = struct.unpack('<I', data[exp_offset+0x18:exp_offset+0x1C])[0]
    addr_funcs_offset = rva_to_offset(struct.unpack('<I', data[exp_offset+0x1C:exp_offset+0x20])[0])
    names_offset = rva_to_offset(struct.unpack('<I', data[exp_offset+0x20:exp_offset+0x24])[0])
    ordinals_offset = rva_to_offset(struct.unpack('<I', data[exp_offset+0x24:exp_offset+0x28])[0])

    exports = []
    for i in range(num_names):
        name_rva = struct.unpack('<I', data[names_offset + i*4 : names_offset + (i+1)*4])[0]
        name_off = rva_to_offset(name_rva)
        name = data[name_off:data.find(b'\x00', name_off)].decode('ascii', errors='ignore')
        ordinal_idx = struct.unpack('<H', data[ordinals_offset + i*2 : ordinals_offset + (i+1)*2])[0]
        func_rva = struct.unpack('<I', data[addr_funcs_offset + ordinal_idx*4 : addr_funcs_offset + (ordinal_idx+1)*4])[0]
        ordinal = base + ordinal_idx
        exports.append({'ordinal': ordinal, 'rva': f"0x{func_rva:08X}", 'name': name})
    exports.sort(key=lambda x: x['ordinal'])
    return exports

native_exports = get_native_exports(r'C:\Dce Pro\DceAnalyssiss64.dll')

# 5. Load Revit Commands
with open('.agents/teamwork/spec_miner_m1_3/revit_decompiled_summary.json', 'r', encoding='utf-8') as f:
    revit_data = json.load(f)

print(f"Loaded: Excel={len(excel_data)}, Steel={len(thep_data)}, MBKC={len(mbkc_data)}, Native={len(native_exports)}, Revit={len(revit_data)}")
