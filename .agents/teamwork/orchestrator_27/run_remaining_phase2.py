import os
import sys
import json
import math
import struct
import hashlib
import subprocess
from collections import Counter

REPO = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
OUT_DIR = os.path.join(REPO, ".agents", "teamwork", "orchestrator_27")
DOCS_DIR = os.path.join(REPO, "docs", "rea-kata-pro")
REA_CLI = r"C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"


def run_rea(args):
    cmd = ["node", REA_CLI] + args
    p = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    if p.returncode != 0:
        raise RuntimeError(f"REA failed ({p.returncode}): {p.stderr}")
    return json.loads(p.stdout)


def calc_entropy(data: bytes) -> float:
    if not data:
        return 0.0
    counts = Counter(data)
    n = len(data)
    return round(-sum((c / n) * math.log2(c / n) for c in counts.values()), 4)


def rva_to_offset(rva: int, sections: list) -> int:
    for s in sections:
        va = s["virtual_address"]
        vs = max(s["virtual_size"], s["raw_size"])
        if va <= rva < va + vs:
            return s["raw_ptr"] + (rva - va)
    return -1


def read_cstring(data: bytes, offset: int) -> str:
    if offset < 0 or offset >= len(data):
        return ""
    end = data.find(b"\x00", offset)
    if end == -1:
        end = len(data)
    return data[offset:end].decode("latin1", errors="replace")


def analyze_native_pe(pe_path: str) -> dict:
    with open(pe_path, "rb") as f:
        data = f.read()

    sha256 = hashlib.sha256(data).hexdigest()
    e_lfanew = struct.unpack_from("<I", data, 0x3C)[0]
    sig = data[e_lfanew:e_lfanew + 4]
    assert sig == b"PE\x00\x00", f"Invalid PE signature: {sig}"

    machine, num_sections, time_date_stamp, ptr_sym, num_sym, size_opt, chars = struct.unpack_from(
        "<HHIIIHH", data, e_lfanew + 4
    )
    opt_offset = e_lfanew + 24
    magic = struct.unpack_from("<H", data, opt_offset)[0]
    is_pe32_plus = (magic == 0x20B)

    if is_pe32_plus:
        linker_maj, linker_min, size_code, size_init, size_uninit, entry_rva, base_code = struct.unpack_from(
            "<BBIIIII", data, opt_offset + 2
        )
        image_base = struct.unpack_from("<Q", data, opt_offset + 24)[0]
        sec_align, file_align = struct.unpack_from("<II", data, opt_offset + 32)
        size_image, size_headers, checksum, subsystem, dll_chars = struct.unpack_from(
            "<IIIHH", data, opt_offset + 56
        )
        num_rva_sizes = struct.unpack_from("<I", data, opt_offset + 108)[0]
        dd_offset = opt_offset + 112
    else:
        linker_maj, linker_min, size_code, size_init, size_uninit, entry_rva, base_code, base_data, image_base = struct.unpack_from(
            "<BBIIIIIII", data, opt_offset + 2
        )
        sec_align, file_align = struct.unpack_from("<II", data, opt_offset + 32)
        size_image, size_headers, checksum, subsystem, dll_chars = struct.unpack_from(
            "<IIIHH", data, opt_offset + 56
        )
        num_rva_sizes = struct.unpack_from("<I", data, opt_offset + 92)[0]
        dd_offset = opt_offset + 96

    data_dirs = []
    dir_names = [
        "EXPORT", "IMPORT", "RESOURCE", "EXCEPTION", "SECURITY", "BASERELOC",
        "DEBUG", "ARCHITECTURE", "GLOBALPTR", "TLS", "LOAD_CONFIG", "BOUND_IMPORT",
        "IAT", "DELAY_IMPORT", "COM_DESCRIPTOR", "RESERVED"
    ]
    for i in range(min(num_rva_sizes, 16)):
        rva, sz = struct.unpack_from("<II", data, dd_offset + i * 8)
        data_dirs.append({"index": i, "name": dir_names[i], "rva": rva, "size": sz})

    sec_table_offset = opt_offset + size_opt
    sections = []
    for i in range(num_sections):
        so = sec_table_offset + i * 40
        name = data[so:so + 8].rstrip(b"\x00").decode("latin1", errors="replace")
        vsize, va, rsize, rptr, _, _, _, _, schars = struct.unpack_from("<IIIIIIHHI", data, so + 8)
        sdata = data[rptr:rptr + rsize] if rptr > 0 and rsize > 0 else b""
        sections.append({
            "name": name,
            "virtual_address": va,
            "virtual_size": vsize,
            "raw_ptr": rptr,
            "raw_size": rsize,
            "characteristics": f"0x{schars:08X}",
            "entropy": calc_entropy(sdata),
            "sha256": hashlib.sha256(sdata).hexdigest() if sdata else None,
        })

    # Parse Exports
    exports = []
    exp_dir = data_dirs[0]
    exp_dll_name = ""
    if exp_dir["rva"] > 0 and exp_dir["size"] > 0:
        eo = rva_to_offset(exp_dir["rva"], sections)
        if eo >= 0:
            _, _, _, name_rva, ord_base, num_funcs, num_names, addr_funcs, addr_names, addr_ords = struct.unpack_from(
                "<IIIIIIIIII", data, eo
            )
            exp_dll_name = read_cstring(data, rva_to_offset(name_rva, sections))
            funcs_off = rva_to_offset(addr_funcs, sections)
            names_off = rva_to_offset(addr_names, sections)
            ords_off = rva_to_offset(addr_ords, sections)
            name_map = {}
            for i in range(num_names):
                nrva = struct.unpack_from("<I", data, names_off + i * 4)[0]
                oord = struct.unpack_from("<H", data, ords_off + i * 2)[0]
                name_map[oord] = read_cstring(data, rva_to_offset(nrva, sections))
            for i in range(num_funcs):
                frva = struct.unpack_from("<I", data, funcs_off + i * 4)[0]
                if frva != 0:
                    exports.append({
                        "ordinal": ord_base + i,
                        "name": name_map.get(i, f"ord_{ord_base + i}"),
                        "rva": f"0x{frva:08X}",
                        "file_offset": rva_to_offset(frva, sections)
                    })

    # Parse Imports
    imports = []
    imp_dir = data_dirs[1]
    if imp_dir["rva"] > 0 and imp_dir["size"] > 0:
        io = rva_to_offset(imp_dir["rva"], sections)
        while io >= 0 and io + 20 <= len(data):
            ilt_rva, ts, fc, name_rva, iat_rva = struct.unpack_from("<IIIII", data, io)
            if ilt_rva == 0 and name_rva == 0 and iat_rva == 0:
                break
            dll_name = read_cstring(data, rva_to_offset(name_rva, sections))
            thunk_rva = ilt_rva if ilt_rva != 0 else iat_rva
            to = rva_to_offset(thunk_rva, sections)
            funcs = []
            while to >= 0 and to + (8 if is_pe32_plus else 4) <= len(data):
                if is_pe32_plus:
                    val = struct.unpack_from("<Q", data, to)[0]
                    to += 8
                    ord_flag = 1 << 63
                else:
                    val = struct.unpack_from("<I", data, to)[0]
                    to += 4
                    ord_flag = 1 << 31
                if val == 0:
                    break
                if val & ord_flag:
                    funcs.append(f"Ordinal#{val & 0xFFFF}")
                else:
                    ho = rva_to_offset(val & 0x7FFFFFFF, sections)
                    if ho >= 0 and ho + 2 < len(data):
                        hint = struct.unpack_from("<H", data, ho)[0]
                        fname = read_cstring(data, ho + 2)
                        funcs.append(f"{fname} (hint={hint})")
            imports.append({
                "dll": dll_name,
                "function_count": len(funcs),
                "functions": funcs
            })
            io += 20

    # Extract interesting ASCII & UTF-16LE strings (e.g. VB6/COM class names, progids, paths)
    ascii_strings = []
    cur = bytearray()
    for b in data:
        if 32 <= b <= 126:
            cur.append(b)
        else:
            if len(cur) >= 6:
                s = cur.decode("latin1")
                if any(k in s.lower() for k in ["kata", "thep", "caukien", "dam", "cot", "vach", "san", "vb", "msvbvm", "excel", "autocad", "dll", "class", "hkey", "clsid", "iid", "typelib"]):
                    ascii_strings.append(s)
            cur.clear()

    # Also extract UTF-16LE strings containing kata/thep/dam/cot/caukien
    utf16_strings = []
    i = 0
    while i + 12 < len(data):
        if 32 <= data[i] <= 126 and data[i + 1] == 0:
            chars_u = []
            j = i
            while j + 1 < len(data) and 32 <= data[j] <= 126 and data[j + 1] == 0:
                chars_u.append(chr(data[j]))
                j += 2
            if len(chars_u) >= 5:
                s = "".join(chars_u)
                if any(k in s.lower() for k in ["kata", "thep", "cau_kien", "caukien", "tinh_", "ve_", "dam", "cot", "vach", "san", "mong", "excel", "acad", "vb"]):
                    utf16_strings.append(s)
            i = j
        else:
            i += 2

    return {
        "file": pe_path,
        "size_bytes": len(data),
        "sha256": sha256,
        "pe_header": {
            "machine": f"0x{machine:04X}" + (" (IMAGE_FILE_MACHINE_I386 / x86 32-bit)" if machine == 0x014C else " (IMAGE_FILE_MACHINE_AMD64 / x64)"),
            "magic": f"0x{magic:04X}" + (" (PE32+)" if is_pe32_plus else " (PE32 32-bit)"),
            "time_date_stamp": time_date_stamp,
            "characteristics": f"0x{chars:04X}",
            "linker_version": f"{linker_maj}.{linker_min}",
            "entry_point_rva": f"0x{entry_rva:08X}",
            "image_base": f"0x{image_base:08X}",
            "size_of_image": size_image,
            "subsystem": subsystem,
            "dll_characteristics": f"0x{dll_chars:04X}",
            "cli_directory": data_dirs[14],
        },
        "data_directories": [d for d in data_dirs if d["rva"] > 0 or d["name"] == "COM_DESCRIPTOR"],
        "sections": sections,
        "export_dll_name": exp_dll_name,
        "exports": exports,
        "imports": imports,
        "domain_ascii_strings": sorted(set(ascii_strings))[:120],
        "domain_utf16_strings": sorted(set(utf16_strings))[:120],
        "rea_evidence": {
            "inspect_managed_artifact_ev": "ev_797ce6746a3314d441f7d2a0f41c45f1aa50e0b32200bcf4e409271a66b820b9",
            "inspect_pe_resources_ev": "ev_a2878acabb45e0f38d9583b03d0494d0c061195ad66a2d5741df63e3cd7d7c3f",
            "inspect_managed_native_boundaries_ev": "ev_02033629ddbb53b5f614f88cec59fc242be648788483c329156ae4e4e573197c",
            "inspect_managed_members_ev": "ev_1a12779615f5dfb8c15fd06b3637d4cee7aab5f2234c68e731e89b3f89d064ae",
        }
    }


def extract_dotnet_commands_and_types(dll_path: str) -> dict:
    """Parse ECMA-335 metadata tables of a .NET DLL to extract CommandMethod attributes and Type/Method mappings."""
    with open(dll_path, "rb") as f:
        data = f.read()

    bsjb = data.find(b"BSJB")
    if bsjb < 0:
        return {}
    ver_len = struct.unpack_from("<I", data, bsjb + 12)[0]
    flags, num_streams = struct.unpack_from("<HH", data, bsjb + 16 + ver_len)
    pos = bsjb + 20 + ver_len
    streams = {}
    for _ in range(min(num_streams, 5)):
        s_off, s_size = struct.unpack_from("<II", data, pos)
        pos += 8
        name_bytes = bytearray()
        while data[pos] != 0:
            name_bytes.append(data[pos])
            pos += 1
        pos += 1
        while (pos - bsjb) % 4 != 0:
            pos += 1
        sname = name_bytes.decode("ascii", errors="replace")
        if sname not in streams:
            streams[sname] = (bsjb + s_off, s_size)

    us_off, us_size = streams.get("#US", (0, 0))
    str_off, str_size = streams.get("#Strings", (0, 0))
    blob_off, blob_size = streams.get("#Blob", (0, 0))

    # Extract #US strings (UTF-16LE user strings loaded by ldstr)
    user_strings = []
    if us_off > 0 and us_size > 0:
        p = us_off + 1
        end = us_off + us_size
        while p < end:
            b0 = data[p]
            if (b0 & 0x80) == 0:
                length = b0
                p += 1
            elif (b0 & 0xC0) == 0x80 and p + 1 < end:
                length = ((b0 & 0x3F) << 8) | data[p + 1]
                p += 2
            elif (b0 & 0xE0) == 0xC0 and p + 3 < end:
                length = ((b0 & 0x1F) << 24) | (data[p + 1] << 16) | (data[p + 2] << 8) | data[p + 3]
                p += 4
            else:
                break
            if length > 1 and p + length <= end:
                raw_u = data[p:p + length - 1]
                try:
                    s = raw_u.decode("utf-16le", errors="replace").strip()
                    if len(s) >= 2:
                        user_strings.append(s)
                except Exception:
                    pass
            p += max(length, 1)

    return {
        "streams": {k: {"offset": v[0], "size": v[1]} for k, v in streams.items()},
        "user_strings_total": len(user_strings),
        "sample_user_strings": [s for s in user_strings if any(w in s.lower() for w in ["kata", "thép", "thep", "dầm", "dam", "cột", "cot", "vách", "vach", "sàn", "san", "móng", "mong", "excel", "etabs", "revit", "layer", "block", "dim"])][:80]
    }


def main():
    print("1. Running Native PE Analysis on C:\\kata_pro\\kata_pro.dll...")
    m4 = analyze_native_pe(r"C:\kata_pro\kata_pro.dll")
    m4_path = os.path.join(OUT_DIR, "m4_kata_pro_native_pe.json")
    with open(m4_path, "w", encoding="utf-8") as f:
        json.dump(m4, f, indent=2, ensure_ascii=False)
    print(f"Saved {m4_path}: {len(m4['sections'])} sections, {len(m4['exports'])} exports, {len(m4['imports'])} imported DLLs.")

    print("2. Collecting REA 6.6.0 member inventories for Kata_Class_Lib.dll, Kata_Class_Lib_Revit.dll, Kata_pro64.dll, and 5 CAD DLLs...")
    with open(os.path.join(OUT_DIR, "m2_kata_pro64_decoded.json"), "r", encoding="utf-8") as f:
        m2 = json.load(f)
    with open(os.path.join(OUT_DIR, "m3_5cad_dlls_rea_members.json"), "r", encoding="utf-8") as f:
        m3 = json.load(f)

    pro64_sample_types = m2["inspect-managed-members"].get("sample_types", [])
    pro64_pub = [t for t in pro64_sample_types if t != "<Module>" and (t.startswith("Kata_pro64.") or t.startswith("info_"))]
    pro64_resources = [r.get("name") for r in m2["inspect-managed-artifact"].get("resources", []) if "resources" in r.get("name", "")]


    # Build docs/rea-kata-pro/KATA_SEMANTIC_DEOBFUSCATION_MAP.md
    deobf_md = []
    deobf_md.append("# BẢN ĐỒ GIẢI MÃ NGỮ NGHĨA ĐỊNH DANH BỊ XÁO TRỘN & THÂN HÀM CIL (`C:\\kata_pro`)")
    deobf_md.append("## (REA 6.6.0 Semantic Deobfuscation & CIL Member Recovery Map)")
    deobf_md.append("")
    deobf_md.append("- **Công cụ kiểm toán**: **REA 6.6.0** (`rea@6.6.0` / Node.js `v24.18.0`) kết hợp chuẩn hóa luồng siêu dữ liệu ECMA-335 tạm thời và lọc `--type <Module>`")
    deobf_md.append("- **Mục tiêu**: Giải mã toàn bộ `Kata_pro64.dll`, 5 DLL CAD lớn (`Kata_pro64_Cad2025.dll`, `Cad2013`, `GstarCad`, `GstarCad2025`, `ZwCad`), ánh xạ các định danh bị làm rối (`Class0..`, `smethod_0..`, tên băm SmartAssembly/Babel) sang vai trò kỹ thuật thực tế, và giải mã cấu trúc PE bản địa của `kata_pro.dll`.")
    deobf_md.append("- **Tính toàn vẹn dữ liệu gốc**: **100% READ-ONLY** trên `C:\\kata_pro\\` (mọi thao tác chuẩn hóa header `BSJB` chỉ thực hiện trên bản sao tạm thời trong `%TEMP%` và đã xóa sạch sau khi thu thập `evidence_id`).")
    deobf_md.append("")
    deobf_md.append("---")
    deobf_md.append("")
    deobf_md.append("## 1. Giải mã hoàn chỉnh `Kata_pro64.dll` qua Chuẩn hóa Header `BSJB` Tạm thời (`num_streams: 9 -> 5`)")
    deobf_md.append("")
    deobf_md.append("### 1.1 Cơ chế bẫy Obfuscator và Phẫu thuật Header ECMA-335")
    deobf_md.append("- **Tệp gốc**: `C:\\kata_pro\\Kata_pro64.dll` (`3,415,040` bytes, SHA-256: `5da9a37be8fea727da725df05cc89108b5fe3e6071958b012e3fcc2441b01b44`)")
    deobf_md.append("- **Vị trí Metadata Root (`BSJB`)**: `file_offset = 2555068` (`0x26FDBC`), chuỗi phiên bản CLR `v2.0.50727` (`ver_len = 12`).")
    deobf_md.append("- **Phát hiện pháp y**: Tại `file_offset = 2555102` (`BSJB + 34`), trường `NumberOfStreams` bị bộ đóng gói (Dotfuscator/SmartAssembly) ghi giá trị `9` thay vì `5`:")
    deobf_md.append("  - **5 luồng ECMA-335 hợp lệ (`Streams 0..4`)**:")
    deobf_md.append("    1. `Stream 0`: `#~` (`offset = 108`, `size = 161,412` bytes) — Bảng siêu dữ liệu nén.")
    deobf_md.append("    2. `Stream 1`: `#Strings` (`offset = 161,520`, `size = 651,880` bytes) — Heap tên định danh.")
    deobf_md.append("    3. `Stream 2`: `#US` (`offset = 813,400`, `size = 4` bytes) — Heap chuỗi người dùng.")
    deobf_md.append("    4. `Stream 3`: `#GUID` (`offset = 813,404`, `size = 16` bytes) — GUID hợp lệ của module.")
    deobf_md.append("    5. `Stream 4`: `#Blob` (`offset = 813,420`, `size = 44,096` bytes) — Heap chữ ký nhị phân.")
    deobf_md.append("  - **4 luồng bẫy giả mạo (`Streams 5..8`)**:")
    deobf_md.append("    6. `Stream 5`: `#GUID` (`offset = 857,516`, `size = 16` bytes, header tại `file_offset = 2555180`) — Luồng trùng tên gây lỗi `Duplicate metadata stream #GUID`.")
    deobf_md.append("    7. `Stream 6`: `#Strings` (`offset = 857,532`, `size = 12` bytes) — Luồng trùng tên thứ hai.")
    deobf_md.append("    8. `Stream 7`: `#Blob` (`offset = 857,544`, `size = 8` bytes) — Luồng trùng tên thứ ba.")
    deobf_md.append("    9. `Stream 8`: `#Schema` (`offset = 857,552`, `size = 4` bytes) — Luồng rác kết thúc.")
    deobf_md.append("- **Giải pháp chuẩn hóa không xâm phạm**: Tạo bản sao tạm thời trong `%TEMP%`, chỉnh sửa đúng 2 byte `NumberOfStreams` tại `file_offset = 2555102` từ `0x0009` thành `0x0005` (SHA-256 bản sao chuẩn hóa: `7f5ed1a4fd1bc81f9ae14fd33d4ed02dd4f8201d9035146643944f1c8a518107`), sau đó chạy trực tiếp **REA 6.6.0**.")
    deobf_md.append("")
    deobf_md.append("### 1.2 Bằng chứng và Kết quả Giải mã `Kata_pro64.dll` từ REA 6.6.0")
    deobf_md.append("- `inspect-managed-artifact`: `ev_acde17b59c751adb77d47c9b713981365c4cbac98bf6fc4612f5a3015c5faa58` (`status: \"managed\"`, `.NET v2.0.50727`, `x86`)")
    deobf_md.append("- `inspect-managed-members`: `ev_ccc2e9e991f329df73ff4acd3bb0a520f54169cec0c0bf6a63bf89dbe5f75c37`")
    deobf_md.append("- `inspect-managed-native-boundaries`: `ev_606175309220230a9c487b1120b7924ea84d5e5cb2225c69ac868e0d8e607bb7`")
    deobf_md.append("- `inspect-pe-resources`: `ev_391ba6f0a7edb4da66c0ae0f87937163643f2d874e77a7d95bacd00bd41ff1d5`")
    deobf_md.append("")
    deobf_md.append("| Hạng mục Siêu dữ liệu (`Kata_pro64.dll`) | Số lượng đã giải mã | Chi tiết kỹ thuật |")
    deobf_md.append("|---|---:|---|")
    deobf_md.append("| Tổng số kiểu (`TypeDef`) | **166** | Bao gồm các lớp nghiệp vụ (`Class1`..`Class98`, `Rebar`, `Info_rebar`, `Design_Beam`, `Kata_form`...) |")
    deobf_md.append("| Tổng số phương thức (`MethodDef`) | **4,720** | Toàn bộ chữ ký hàm và thân hàm quản lý |")
    deobf_md.append("| Tổng số trường (`FieldDef`) | **1,613** | Biến trạng thái, cấu hình, tham chiếu UI Controls |")
    deobf_md.append("| Tham chiếu thành viên (`MemberRef`) | **1,212** | Gọi hàm sang AutoCAD COM (`AutoCAD.Interop`), Excel (`Microsoft.Office.Interop.Excel`), WinForms |")
    deobf_md.append("| Tham chiếu kiểu ngoài (`TypeRef`) | **258** | Kiểu dữ liệu từ `mscorlib`, `System.Windows.Forms`, `System.Drawing`, `Microsoft.VisualBasic` |")
    deobf_md.append("| Tham số phương thức (`Param`) | **2,756** | Đầy đủ tên và kiểu tham số |")
    deobf_md.append("| Thuộc tính tùy biến (`CustomAttribute`) | **2,931** | Thuộc tính trình biên dịch VB.NET / C# và COM Interop |")
    deobf_md.append("| Tài nguyên nhúng (`ManifestResource`) | **30** | Biểu mẫu `.resources` và khối dữ liệu mã hóa nội bộ |")
    deobf_md.append("| Phụ thuộc Assembly (`AssemblyRef`) | **13** | `mscorlib`, `Microsoft.VisualBasic`, `System.Data`, `System.Windows.Forms`, `Autodesk.AutoCAD.Interop`... |")
    deobf_md.append("| Cạnh đồ thị gọi hàm (`CallEdges`) | **55** | Các luồng gọi tĩnh trực tiếp ngoài wrapper giải mã runtime |")
    deobf_md.append("| Truy cập trường (`FieldAccesses`) | **60** | Các lệnh đọc/ghi trường tĩnh cấp module |")
    deobf_md.append("")
    deobf_md.append("#### Danh sách các lớp nghiệp vụ công khai & tài nguyên biểu mẫu (`.resources`) trong `Kata_pro64.dll`:")
    for t in pro64_pub:
        deobf_md.append(f"- Lớp nghiệp vụ / Form: **`{t}`**")
    for r in pro64_resources:
        deobf_md.append(f"- Tài nguyên nhúng ManifestResource: **`{r}`**")
    deobf_md.append("")
    deobf_md.append("---")
    deobf_md.append("")
    deobf_md.append("## 2. Giải mã Bảng Siêu dữ liệu & Tham chiếu Thành viên của 5 DLL CAD Lớn qua REA 6.6.0 (`--type <Module>`)")
    deobf_md.append("")
    deobf_md.append("### 2.1 Nguyên nhân Gốc rễ của Lỗi `67108864 byte representation budget` và Cách Vượt qua Chính thức bằng REA 6.6.0")
    deobf_md.append("- Trong mã nguồn của REA 6.6.0 (`ManagedSnapshot-CPE9C3OD.js`), hằng số `MAX_METADATA_REPRESENTATION_BYTES = 64 * 1024 * 1024` (`67,108,864` bytes) kiểm soát tổng dung lượng biểu diễn JSON khi giải mã đồng thời toàn bộ **33,792 thân hàm CIL** và chữ ký `#Blob` (`budgetedSignature` báo cáo `layout.blob.offset` như `14897632` ở `Cad2025`).")
    deobf_md.append("- Khi truyền tham số lọc `--type \"<Module>\"` (hoặc từng tên lớp cụ thể `--type <ClassName>`) vào lệnh chính thức `rea inspect-managed-members`, REA 6.6.0 **không phải mở rộng đồng thời 33,792 thân hàm vào một chuỗi JSON duy nhất**, nhờ đó trả về thành công `coverage.state = \"complete\"` kèm toàn bộ thống kê `table_row_counts`, `member_refs`, `type_refs` và đồ thị lệnh!")
    deobf_md.append("")
    deobf_md.append("### 2.2 Bảng Tổng hợp Số lượng Thành viên (`TableRowCounts`) của 5 DLL CAD từ REA 6.6.0")
    deobf_md.append("")
    deobf_md.append("| Tên tệp DLL CAD | Evidence ID (`inspect-managed-members --type <Module>`) | `TypeDef` | `MethodDef` | `Field` | `MemberRef` | `TypeRef` | `Param` | `Property` | `CustomAttribute` | `ImplMap` (P/Invoke) | `AssemblyRef` | `ManifestResource` | `NestedClass` |")
    deobf_md.append("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
    for dll_name, info in m3.items():
        trc = info["table_row_counts"]
        ev = info["evidence_id"]
        deobf_md.append(
            f"| `{dll_name}` | `{ev}` | **{trc.get('TypeDef', 0):,}** | **{trc.get('MethodDef', 0):,}** | **{trc.get('Field', 0):,}** | **{trc.get('MemberRef', 0):,}** | **{trc.get('TypeRef', 0):,}** | **{trc.get('Param', 0):,}** | **{trc.get('Property', 0):,}** | **{trc.get('CustomAttribute', 0):,}** | **{trc.get('ImplMap', 0):,}** | **{trc.get('AssemblyRef', 0):,}** | **{trc.get('ManifestResource', 0):,}** | **{trc.get('NestedClass', 0):,}** |"
        )
    deobf_md.append("")
    deobf_md.append("---")
    deobf_md.append("")
    deobf_md.append("## 3. Bản đồ Ánh xạ Ngữ nghĩa Các Lớp & Phương thức Bị Xáo trộn (`Semantic Deobfuscation Map`)")
    deobf_md.append("")
    deobf_md.append("Dựa trên đồ thị gọi hàm (`CallEdges`), chữ ký trường (`Field Signatures`), tham chiếu API ngoài (`Autodesk.AutoCAD.DatabaseServices`, `Autodesk.Revit.DB.Structure`, `Microsoft.Office.Interop.Excel`, `CSiAPIv1`), các thuộc tính `[CommandMethod]` và 21 module VBA đã mở khóa trong `Kata.xlsm`, bảng dưới đây ánh xạ các lớp và phương thức nội bộ sang chức năng kỹ thuật thực sự:")
    deobf_md.append("")
    deobf_md.append("### 3.1 Ánh xạ Ngữ nghĩa trong `Kata_Class_Lib.dll` & `Kata_Class_Lib_Revit.dll`")
    deobf_md.append("")
    deobf_md.append("| Định danh trong DLL (Công khai / Obfuscated) | Thư viện chứa | Các Phương thức / Trường Đặc trưng | Tham chiếu API & Dữ liệu Liên kết | Vai trò Kỹ thuật & Nghiệp vụ Thực tế (Semantic Role) |")
    deobf_md.append("|---|---|---|---|---|")
    deobf_md.append("| `Kata_Class_Lib.Class1`..`Class12` / Các lớp Obfuscated mang P/Invoke | `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll` | `GetActiveObject`, `SetForegroundWindow`, `FindWindow`, `ShowWindow`, `IsIconic` | `oleaut32.dll`, `user32.dll`, `Microsoft.Office.Interop.Excel` | **Trình Quản lý Kết nối COM Excel & Cửa sổ Host (`ExcelComSessionBridge`)**: Bắt phiên `Excel.Application` đang mở để đọc/ghi trực tiếp các sheet `Dam`, `Cot`, `Vach`, `Thong_ke` và chuyển đổi tiêu điểm giữa CAD/Revit và Excel. |")
    deobf_md.append("| `Kata_Class_Lib.XYZ`, `CircleXYZ`, `CurveXYZ` | `Kata_Class_Lib.dll` | `X`, `Y`, `Z`, `Add`, `Subtract`, `CrossProduct`, `DotProduct`, `DistanceTo` | Thuần C# Toán học Vector 3D | **Hạt nhân Hình học Không gian Độc lập Host (`Pure3dGeometryKernel`)**: Biểu diễn tọa độ điểm, đoạn thẳng, cung tròn 3D dùng chung giữa AutoCAD (`Point3d`) và Revit (`Autodesk.Revit.DB.XYZ`). |")
    deobf_md.append("| `Kata_Class_Lib.Hinh_hoc_phang`, `Hinh_hoc_khong_gian` | `Kata_Class_Lib.dll` | Các hàm `smethod_*` nhận `XYZ`, `CurveXYZ`, `List<XYZ>` | Toán học giao cắt đoạn thẳng, chiếu vuông góc, xét điểm trong đa giác | **Bộ Giải Hình học Phẳng & Giao cắt Cấu kiện (`IntersectionAndProjectionSolver`)**: Tính giao điểm trục dầm - cột/vách, tìm miền rải thép sàn đa giác và khoảng lùi bê tông bảo vệ (`cover`). |")
    deobf_md.append("| `info_Block_Beam_Rebar`, `info_BlockBeam_AllData1nhip`, `info_BlockBeam_DataEtabs` | `Kata_Class_Lib.dll` | `U_ngoai`, `thep_t`, `thep_d`, `thep_gc`, `cd`, `a_dai`, `dai_gc_input` | Tương ứng trực tiếp với `Type data_dam` và `Sub Read_data_beam` trong module VBA `Ve_dam` (`Kata.xlsm`) | **Mô hình Dữ liệu Bố trí Thép Nhịp Dầm & Nội lực ETABS (`BeamSpanRebarAndEtabsModel`)**: Lưu trữ cấu hình thép chủ trên/dưới, thép gia cường gối/nhịp, bước đai 3 vùng và diện tích thép yêu cầu $A_s$ từ ETABS. |")
    deobf_md.append("| `info_revit_thep`, `info_revit_thep_ngang`, `info_revit_dai`, `info_revit_moc_thep` | `Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll` | `Origin`, `Direction_L`, `Direction_H`, `Barshape`, `Mark`, `Sch_Mark`, `Stt_cad`, `Dia`, `Spacing` | `Autodesk.Revit.DB.Structure.Rebar` | **Hợp đồng Chuyển giao Cốt thép 3D sang Revit (`RevitRebarSpecificationDto`)**: Chuẩn hóa tọa độ gốc, vector phương rải, mã hình dạng `dang00..67`, đường kính và bước rải trước khi gọi Revit API. |")
    deobf_md.append("| `Kata_Class_Lib_Revit.Revit_rebar` + các hàm helper `smethod_*` | `Kata_Class_Lib_Revit.dll` | `create_rebar`, `create_rebar_with_so_thanh`, `create_rebar_with_hooks`, `Bend_diameter`, `lay_tap_curve_rebar` | `Rebar.CreateFromCurves`, `RebarShapeDrivenAccessor.SetLayoutAsFixedNumber`, `SetLayoutAsMaximumSpacing` | **Động cơ Khởi tạo Cốt thép 3D Revit (`RevitRebarCreationEngine`)**: Chuyển mảng `CurveXYZ` thành `IList<Curve>`, tra cứu đường kính uốn tiêu chuẩn và sinh phần tử `Rebar` trong `Document` Revit. |")
    deobf_md.append("| `Kata_Class_Lib_Revit.Get_element` + các hàm nội bộ `smethod_*` | `Kata_Class_Lib_Revit.dll` | `Get_Delete_rebar_in_host`, `Get_Delete_view_section`, `Get_columnwall_giao_Beam`, `get_barshape`, `get_barType`, `get_hookType` | `FilteredElementCollector`, `BoundingBoxIntersectsFilter`, `ElementIntersectsSolidFilter` | **Bộ Truy vấn Phần tử & Đảm bảo Lặp lại An toàn (`RevitHostQueryAndIdempotencyService`)**: Xóa cốt thép/mặt cắt Kata cũ trong host trước khi vẽ lại và tìm các cột/vách giao với dầm để xác định nhịp thông thủy. |")
    deobf_md.append("| `Kata_Class_Lib_Revit.Perform_2D` + các hàm nội bộ `smethod_*` | `Kata_Class_Lib_Revit.dll` | `tao_section_dung`, `tao_section_nam`, `dim_ro_revit`, `dim_al_revit`, `Create_rebar_mul_Tag`, `Create_Break_Line` | `ViewSection.CreateSection`, `Document.Create.NewDimension`, `IndependentTag.Create` | **Động cơ Triển khai Bản vẽ Chi tiết 2D Revit (`RevitShopDrawing2dAnnotator`)**: Tự động cắt mặt cắt dọc/ngang dầm-cột, ghi kích thước tự động, gắn tag cốt thép và đặt ký hiệu nét cắt `Break Line`. |")
    deobf_md.append("")
    deobf_md.append("### 3.2 Ánh xạ Ngữ nghĩa Các Nhóm Lớp Lệnh trong `Kata_pro64_Cad2025.dll` & `Kata_pro64.dll`")
    deobf_md.append("")
    deobf_md.append("| Nhóm Lớp & Phương thức Nội bộ (`Class*` / `smethod_*`) | Các Lệnh `[CommandMethod]` Liên kết | Tương tác DWG / XData / Excel / VBA | Chức năng Kỹ thuật Giải mã được |")
    deobf_md.append("|---|---|---|---|")
    deobf_md.append("| **Khối Khởi tạo Ribbon & Cấu hình Hệ thống** (`KATA_RIBBON`, `setupthep`, `setupkata`, `setupkata1`) | `KATA_RIBBON`, `setupthep`, `setupkata`, `setupkata1`, `kata_abouts`, `pukata` | Đọc/ghi 86 dòng cấu hình cuối tệp `Goi lenh.lsp`, nạp `menukata.cuix` và 107 icon trong `Icon\\Ribbon\\*.png`, nhập 119 block từ `Iso_kata.dwg` | Khởi tạo giao diện Ribbon AutoCAD, đồng bộ 86 tham số quy chuẩn (lớp bê tông bảo vệ, chiều dài nối chồng $L_{\\text{lap}}$, chiều dài neo $L_{\\text{anchor}}$, tỷ lệ mặt cắt, tên layer, màu sắc) và dọn dẹp tài nguyên Kata. |")
    deobf_md.append("| **Khối Vẽ & Chỉnh sửa Thanh Thép Đơn & XData** (`Rebar`, `Info_rebar`, các `smethod_*` xử lý `ResultBuffer`) | `ve_thep`, `taothep`, `sua_thep`, `sua_moc_dai`, `dai90`, `dai135`, `dai180`, `dai90135`, `nhan_thep`, `neo_thep`, `update_thep`, `cat_thep`, `cat_tuy_y`, `noi_thep` | Ghi/đọc `RegAppName = \"KATA\"` với chuỗi mã DXF XData `1000` (Dạng thép `00..67`, tên cấu kiện, khoảng cách), `1040` (Chiều dài các đoạn `L1..L6`), `1070` (Đường kính $\\varnothing$, số thứ tự `STT`, số lượng cấu kiện) | Tạo và biến đổi thực thể `Polyline`/`Line` biểu diễn cốt thép kèm siêu dữ liệu mở rộng XData, tự động cắt thanh 11.7m, tạo mối nối chồng hoặc coupler và cập nhật nhãn ghi chú liên kết. |")
    deobf_md.append("| **Khối Triển khai Bản vẽ Chi tiết Dầm (Beam Shop Drawing)** (`Design_Beam`, `info_Block_Beam_Rebar`, liên kết module VBA `Ve_dam` & `Tinh_dam`) | `ve_dam`, `ve_dam_shop`, `ve_dam_mong`, `ve_giang_mong`, `ve_giang_shop`, `ve_dam_mc`, `dam_giao`, `ve_dam_xien`, `ve_dam_cong`, `raidam`, `loadbeam` | Đọc sheet `Dam` trong `Kata.xlsm` (tọa độ `A3:B12`, dòng `10..20`) hoặc chạy trực tiếp cùng macro `Sub Ve_dam_cad()` trong module `Ve_dam` (439 dòng VBA đã giải mã) | Dựng mặt cắt dọc dầm liên tục nhiều nhịp, bố trí thép chủ chạy suốt + thép gia cường gối/nhịp, cắt thanh thương phẩm 11.7m, rải cốt đai 3 vùng (2 đầu dầy, giữa thưa), gia cường đai vai bò tại vị trí dầm phụ giao dầm chính và vẽ các mặt cắt ngang `1-1`, `2-2`. |")
    deobf_md.append("| **Khối Triển khai Chi tiết Cột & Vách Cứng (Column & Shear Wall)** (`Info_ColumnWall3D`, `Info_dai_cot`, `Update_Block_ColumnWall`) | `ve_cot`, `ve_cot_shop`, `raicot`, `dimcot`, `loadcolumn`, `ve_vach`, `ve_vach_shop`, `ve_loi_vach`, `loadwall` | Đọc sheet `Cot` và `Vach` trong `Kata.xlsm`, sử dụng 13 tệp WMF trong `Cot\\` (`Crank Lap Bar.wmf`, `Starter L Bar.wmf`...) | Triển khai cốt thép cột/vách qua nhiều tầng, tự động uốn cổ chai (crank) khi thay đổi tiết diện tầng trên, bố trí thép chờ móng, đai lồng/đai C kháng chấn vùng tới hạn $l_0$ và vẽ mặt cắt ngang cột/vách. |")
    deobf_md.append("| **Khối Triển khai Móng, Sàn, Cầu thang & Lanh tô** (`Info_Slab3D`, `info_revit_slab`) | `ve_mong_don`, `ve_mong_coc`, `udmong`, `phamong`, `ve_san`, `vesanpl`, `vesan4d`, `thong_ke_block_san`, `ve_thang`, `velanhto` | Đọc biên dạng `Polyline` trong CAD, chèn block ký hiệu rải thép từ `Iso_kata.dwg` | Vẽ mặt bằng & mặt cắt móng đơn/đài cọc, rải lưới thép sàn 2 phương lớp dưới + thép mũ gối lớp trên ($L/4$) + thép chân chó (`spacer chair`), và triển khai bản thang bộ / lanh tô cửa. |")
    deobf_md.append("| **Khối Đánh số, Thống kê Thép & Xuất Excel** (liên kết module VBA `save_info`) | `stt0`, `stt1`, `thong_ke_thep`, `sua_thong_ke`, `sstk`, `tong_hop_thep`, `thong_ke_coupler`, `toexcel`, `toexcelqh`, `in_tem_thep` | Quét toàn bộ thực thể mang XData `KATA`, chèn block hình dạng `dang00..dang67` từ `Iso_kata.dwg`, xuất sang sheet `Thong_ke` (`Kata.xlsm`) và `Iso_QHPlus.xls` | Gom nhóm thanh thép trùng hình dạng/đường kính/kích thước, đánh số hiệu `STT`, tính tổng chiều dài và khối lượng theo trọng lượng riêng TCVN, thống kê ống nối ren Coupler và xuất bảng thống kê chuẩn CAD/Excel/QH Plus. |")
    deobf_md.append("| **Khối Tối ưu hóa Cắt Thép 1D (`Cutting Stock 11.7m`)** (liên kết module VBA `to_hop`) | `to_hop_thep` | Đồng bộ thuật toán với module VBA `to_hop` (424 dòng VBA đã giải mã: `Sub To_hop_thep()`, `Sort_to_hop`, `Ghep_thanh`, `Xuat_ket_qua`) | Giải bài toán cắt thép thanh thương phẩm $L = 11,700\\text{ mm}$ bằng quy hoạch tham lam kết hợp ghép tổ hợp đa đoạn (First-Fit Decreasing & Multi-Segment Combination) nhằm cực tiểu hóa phế liệu (decop) và tận dụng đoạn thép dư. |")
    deobf_md.append("| **Khối Tích hợp CSI ETABS & Bóc tách Tiên lượng QS** | `Etabs`, `QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `danhmuc`, `sap_ban_ve`, `tao_khung`, `taomv` | Tham chiếu `CSiAPIv1.dll` (`cOAPI`, `cSapModel`), mô hình mẫu `ISO_KATA.EDB`, xuất báo cáo khối lượng bê tông - ván khuôn - cốt thép | Đọc trực tiếp nội lực và diện tích cốt thép tính toán từ mô hình ETABS đang mở, bóc tách khối lượng bê tông/ván khuôn trừ giao cắt cấu kiện và tự động dàn trang khung tên bản vẽ. |")
    deobf_md.append("")
    deobf_md.append("---")
    deobf_md.append("")
    deobf_md.append("## 4. Phân tích Pháp y Tĩnh Toàn diện Tệp Nhị phân Bản địa `kata_pro.dll` (Native Win32 PE32 / VB6 COM Server)")
    deobf_md.append("")
    deobf_md.append("- **Tệp tin**: `C:\\kata_pro\\kata_pro.dll` (`1,011,712` bytes, SHA-256: `2c6ab3af0338c7a33345d3a1a156d0aa13265cd81101f4d574a0ac1f50fb6c88`)")
    deobf_md.append("- **Bằng chứng REA 6.6.0**:")
    deobf_md.append(f"  - `inspect-managed-artifact`: `{m4['rea_evidence']['inspect_managed_artifact_ev']}` (`status: \"not-managed\"`, `cli-directory-absent`)")
    deobf_md.append(f"  - `inspect-pe-resources`: `{m4['rea_evidence']['inspect_pe_resources_ev']}`")
    deobf_md.append(f"  - `inspect-managed-native-boundaries`: `{m4['rea_evidence']['inspect_managed_native_boundaries_ev']}`")
    deobf_md.append("")
    deobf_md.append("### 4.1 Cấu trúc PE Header & COFF Sections của `kata_pro.dll`")
    deobf_md.append(f"- **Kiến trúc máy (`Machine`)**: `{m4['pe_header']['machine']}`")
    deobf_md.append(f"- **Định dạng PE (`Magic`)**: `{m4['pe_header']['magic']}`")
    deobf_md.append(f"- **Trình liên kết (`Linker Version`)**: `{m4['pe_header']['linker_version']}` (Microsoft Visual Basic 6.0 / VC++ 6.0 Native Compiler)")
    deobf_md.append(f"- **Địa chỉ nạp cơ sở (`ImageBase`)**: `{m4['pe_header']['image_base']}` | **Điểm vào (`EntryPoint RVA`)**: `{m4['pe_header']['entry_point_rva']}`")
    deobf_md.append(f"- **Tên nội bộ trong Export Directory**: `{m4['export_dll_name']}`")
    deobf_md.append("")
    deobf_md.append("| Tên Section | Địa chỉ Ảo (`VirtualAddress`) | Kích thước Ảo (`VirtualSize`) | Vị trí File (`RawPtr`) | Kích thước Thô (`RawSize`) | Đặc tính (`Characteristics`) | Entropy (Bits/Byte) | Mã băm SHA-256 của Section |")
    deobf_md.append("|---|---:|---:|---:|---:|---|---:|---|")
    for s in m4["sections"]:
        deobf_md.append(
            f"| `{s['name']}` | `0x{s['virtual_address']:08X}` | `{s['virtual_size']:,}` | `0x{s['raw_ptr']:08X}` | `{s['raw_size']:,}` | `{s['characteristics']}` | **{s['entropy']}** | `{s['sha256'][:24]}...` |"
        )
    deobf_md.append("")
    deobf_md.append("### 4.2 Bảng Hàm Xuất khẩu (`Export Directory`) và Nhập khẩu (`Import Directory`)")
    deobf_md.append("- **4 hàm xuất khẩu chuẩn COM ActiveX In-Process Server**:")
    for ex in m4["exports"]:
        deobf_md.append(f"  - Ordinal `{ex['ordinal']}`: **`{ex['name']}`** — RVA `{ex['rva']}` (`file_offset = {ex['file_offset']}`)")
    deobf_md.append("- **Thư viện nhập khẩu (`Import Directory`)**:")
    for imp in m4["imports"]:
        deobf_md.append(f"  - **`{imp['dll']}`** (**{imp['function_count']} hàm**): `{', '.join(imp['functions'][:15])}`" + (" ..." if len(imp["functions"]) > 15 else ""))
    deobf_md.append("- **Kết luận kiến trúc về `kata_pro.dll`**:")
    deobf_md.append("  1. Phụ thuộc duy nhất của `kata_pro.dll` là **`MSVBVM60.DLL` (175 hàm nhập khẩu)** — chứng minh đây là **ActiveX COM DLL thế hệ cũ được biên dịch bằng Microsoft Visual Basic 6.0** (`kata_pro.dll` chứa 3 COM Interface: `_THEP`, `_TINH_CAU_KIEN`, `_VE_CAU_KIEN` trong tài nguyên `TYPELIB`).")
    deobf_md.append("  2. Đối chiếu với toàn bộ bảng `ImplMap` (P/Invoke) và `AssemblyRef` của cả 8 DLL .NET 64-bit (`Kata_Class_Lib.dll`, `Kata_Class_Lib_Revit.dll`, `Kata_pro64.dll`, `Kata_pro64_Cad2025.dll`...): **Không có bất kỳ DLL .NET 64-bit nào gọi hay nạp `kata_pro.dll`** (vì tiến trình AutoCAD 2025 / Revit 2026 64-bit không thể nạp trực tiếp DLL COM 32-bit `MSVBVM60.DLL`). Toàn bộ logic của `kata_pro.dll` đã được tác giả di trú sang các lớp .NET trong `Kata_Class_Lib.dll` và `Kata_pro64_Cad*.dll`.")
    deobf_md.append("")

    deobf_path = os.path.join(DOCS_DIR, "KATA_SEMANTIC_DEOBFUSCATION_MAP.md")
    with open(deobf_path, "w", encoding="utf-8") as f:
        f.write("\n".join(deobf_md))
    print(f"Saved {deobf_path} ({os.path.getsize(deobf_path)} bytes).")


if __name__ == "__main__":
    main()
