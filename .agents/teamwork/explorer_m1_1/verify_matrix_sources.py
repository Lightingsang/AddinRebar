import json
import os
import struct
import subprocess
import sys

# Ensure UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

print("Starting Master Truth Matrix Schema & Verification Engine...")

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
DCE_DIR = r"C:\Dce Pro"
DANHBA_DIR = os.path.join(DCE_DIR, "AiAgent", "DanhBa")
HPDCEPRO_DIR = os.path.join(BASE_DIR, "HPDcePro")
SCRATCH_RVT_DIR = os.path.join(BASE_DIR, "scratch", "dce_rvt_unpacked")

# 1. Verify Catalogs
catalogs = {
    "lenh": os.path.join(DANHBA_DIR, "danhba_lenh.json"),
    "excel": os.path.join(DANHBA_DIR, "danhba_dce_excel.json"),
    "thep": os.path.join(DANHBA_DIR, "danhba_dce_thep.json"),
    "mbkc": os.path.join(DANHBA_DIR, "danhba_mbkc.json")
}

for k, v in catalogs.items():
    if not os.path.exists(v):
        raise FileNotFoundError(f"Missing catalog: {v}")
    with open(v, 'r', encoding='utf-8') as f:
        d = json.load(f)
        print(f"Loaded {k}: {len(d.get('muc', []))} entries")

# 2. Extract 69 Native Exports from DceAnalyssiss64.dll
def get_pe_exports(dll_path):
    with open(dll_path, 'rb') as f:
        data = f.read()
    if data[:2] != b'MZ':
        return []
    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]
    if data[e_lfanew:e_lfanew+4] != b'PE\x00\x00':
        return []
    opt_offset = e_lfanew + 24
    export_rva = struct.unpack('<I', data[opt_offset+112:opt_offset+116])[0]
    num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
    size_opt = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
    sec_offset = opt_offset + size_opt
    sections = []
    for i in range(num_sections):
        s_off = sec_offset + i*40
        s_name = data[s_off:s_off+8].rstrip(b'\x00').decode('latin1')
        vsize = struct.unpack('<I', data[s_off+8:s_off+12])[0]
        vrva = struct.unpack('<I', data[s_off+12:s_off+16])[0]
        rsize = struct.unpack('<I', data[s_off+16:s_off+20])[0]
        roffset = struct.unpack('<I', data[s_off+20:s_off+24])[0]
        sections.append({'name': s_name, 'vrva': vrva, 'vsize': vsize, 'roffset': roffset, 'rsize': rsize})
    def rva_to_off(rva):
        for s in sections:
            if s['vrva'] <= rva < s['vrva'] + max(s['vsize'], s['rsize']):
                return s['roffset'] + (rva - s['vrva'])
        return None
    exp_off = rva_to_off(export_rva)
    if not exp_off:
        return []
    num_names = struct.unpack('<I', data[exp_off+0x18:exp_off+0x1C])[0]
    addr_funcs_rva = struct.unpack('<I', data[exp_off+0x1C:exp_off+0x20])[0]
    addr_names_rva = struct.unpack('<I', data[exp_off+0x20:exp_off+0x24])[0]
    addr_ord_rva = struct.unpack('<I', data[exp_off+0x24:exp_off+0x28])[0]
    
    names_off = rva_to_off(addr_names_rva)
    funcs_off = rva_to_off(addr_funcs_rva)
    ord_off = rva_to_off(addr_ord_rva)
    
    exports = []
    for i in range(num_names):
        name_rva = struct.unpack('<I', data[names_off + i*4 : names_off + (i+1)*4])[0]
        n_off = rva_to_off(name_rva)
        fn_name = data[n_off:data.find(b'\x00', n_off)].decode('ascii')
        ord_idx = struct.unpack('<H', data[ord_off + i*2 : ord_off + (i+1)*2])[0]
        fn_rva = struct.unpack('<I', data[funcs_off + ord_idx*4 : funcs_off + (ord_idx+1)*4])[0]
        exports.append((i, fn_name, f"0x{fn_rva:04x}"))
    return exports

native_dll = os.path.join(DCE_DIR, "DceAnalyssiss64.dll")
native_exports = get_pe_exports(native_dll)
print(f"Extracted {len(native_exports)} native exports from {native_dll}")

print("Master Verification Script Validation Complete.")
