import os
import sys
import hashlib
import struct
import json
import subprocess
from pathlib import Path

def compute_sha256(filepath):
    h = hashlib.sha256()
    with open(filepath, 'rb') as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()

def inspect_pe(filepath):
    try:
        with open(filepath, 'rb') as f:
            data = f.read()
    except Exception as e:
        return {"error": str(e)}

    if len(data) < 64 or data[:2] != b'MZ':
        return {"is_pe": False}

    e_lfanew = struct.unpack('<I', data[0x3C:0x40])[0]
    if len(data) < e_lfanew + 24 or data[e_lfanew:e_lfanew+4] != b'PE\x00\x00':
        return {"is_pe": False}

    machine = struct.unpack('<H', data[e_lfanew+4:e_lfanew+6])[0]
    num_sections = struct.unpack('<H', data[e_lfanew+6:e_lfanew+8])[0]
    size_opt = struct.unpack('<H', data[e_lfanew+20:e_lfanew+22])[0]
    characteristics = struct.unpack('<H', data[e_lfanew+22:e_lfanew+24])[0]

    is_64 = (machine == 0x8664)
    arch_str = "x64" if is_64 else ("x86" if machine == 0x14C else f"0x{machine:X}")

    opt_header_offset = e_lfanew + 24
    if is_64:
        # 64-bit opt header: Data directories start at offset 112
        clr_rva = struct.unpack('<I', data[opt_header_offset+208:opt_header_offset+212])[0] if size_opt >= 216 else 0
        export_rva = struct.unpack('<I', data[opt_header_offset+112:opt_header_offset+116])[0] if size_opt >= 120 else 0
    else:
        # 32-bit opt header: Data directories start at offset 96
        clr_rva = struct.unpack('<I', data[opt_header_offset+208:opt_header_offset+212])[0] if size_opt >= 216 else 0
        export_rva = struct.unpack('<I', data[opt_header_offset+96:opt_header_offset+100])[0] if size_opt >= 104 else 0

    is_managed = (clr_rva != 0)
    has_exports = (export_rva != 0)

    # Search for CLR metadata string like v4.0.30319 or v2.0.50727
    clr_version = None
    if is_managed:
        for v in [b"v4.0.30319", b"v2.0.50727", b"v1.1.4322", b"v1.0.3705"]:
            if v in data:
                clr_version = v.decode('ascii')
                break

    return {
        "is_pe": True,
        "machine": arch_str,
        "is_managed": is_managed,
        "has_exports": has_exports,
        "clr_version": clr_version,
    }

def main():
    roots = [
        ("C:\\Dce Pro", Path("C:/Dce Pro")),
        ("Revit Unpacked", Path("g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/scratch/dce_rvt_unpacked"))
    ]

    all_dlls = []

    for label, root_path in roots:
        if not root_path.exists():
            continue
        for p in root_path.rglob("*.dll"):
            rel = str(p.relative_to(root_path))
            size = p.stat().st_size
            sha = compute_sha256(str(p))
            pe_info = inspect_pe(str(p))
            all_dlls.append({
                "scope": label,
                "relative_path": rel,
                "absolute_path": str(p),
                "size_bytes": size,
                "size_mb": round(size / (1024 * 1024), 2),
                "sha256": sha,
                "pe": pe_info
            })

    output_path = Path(".agents/teamwork/explorer_survey_1/catalog_dlls.json")
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(all_dlls, f, indent=2)

    print(f"Cataloged {len(all_dlls)} DLLs.")

if __name__ == "__main__":
    main()
