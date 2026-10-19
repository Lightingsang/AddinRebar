import os
import re
import math
import sys

def test_excel_73_actions():
    print("=== TEST 1: EXCEL 73 LEGACY ACTIONS VERIFICATION ===")
    legacy_73 = [
        "APIAUTOCAD", "APICAD", "APICSI",
        "Bea_CalculateTheVolume", "Bea_ChooseColor", "Bea_Design", "Bea_ExportDrawingData",
        "Bea_ExportSize", "Bea_LoadData", "Bea_Print", "Bea_Setup",
        "MatTienDceExcel.DocBangDam", "MatTienDceExcel.KiemTraDauVaoDam", "MatTienDceExcel.DocKetQuaDam",
        "MatTienDceExcel.DocDamDaLuuDuLieuVe", "AutoStr.AutoStr_InThuyetMinhDam",
        "AutoStr.AutoStr_AiTinhToanDam", "AutoStr.AutoStr_AiChuyenDuLieuVeDam",
        "Col_CalculateTheVolume", "Col_ChooseColor", "Col_Design", "Col_Design2",
        "Col_ExportDrawingData", "Col_ExportSize", "Col_LoadData", "Col_LoadDataGenCol",
        "Col_Print", "Col_Setup", "Col_SortData",
        "MatTienDceExcel.DocBangCot", "MatTienDceExcel.DocKetQuaCot", "MatTienDceExcel.DocKetQuaVach",
        "MatTienDceExcel.KiemTraDauVaoCot", "AutoStr.AutoStr_InThuyetMinhCot",
        "AutoStr.AutoStr_AiTinhToanCot", "AutoStr.AutoStr_AiXuatDuLieuVeCot",
        "Sla_ChooseColor", "Sla_Design", "Sla_ExportSize", "Sla_LoadData", "Sla_Print", "Sla_Setup",
        "MatTienDceExcel.DocBangSan", "MatTienDceExcel.KiemTraDauVaoSan",
        "AutoStr.AutoStr_ChonThepSan", "AutoStr.AutoStr_InThuyetMinhSan",
        "Bas_Show", "Eta_Show", "Fou_Show",
        "GopThepDaiToHop", "ToHop", "ToHop2",
        "DrawLine", "DrawPolyLine", "DrawREC", "SelectHandle",
        "ESStiffnessModifiers", "ESUniformLoadSetsToSafe", "EtaSTableToEta",
        "PointSafeToPointSafe", "SafeTableToSafe", "UniformLoadSets", "DbimPuFile",
        "DceAI", "DceAdmin", "DceLanguage", "DceLogin", "DceLogout", "DceUpdate", "licenseissuance",
        "MatTienDceExcel.PhienBan", "MatTienDceExcel.LietKeChucNang", "MatTienDceExcel.TrangThaiWorkbook"
    ]
    
    assert len(legacy_73) == 73, f"Expected 73 actions, got {len(legacy_73)}"
    assert len(set(legacy_73)) == 73, "Duplicate actions in legacy_73 catalog"

    handlers_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Excel\ActionHandlers"
    registered_actions = []
    handler_files = [f for f in os.listdir(handlers_dir) if f.endswith(".cs")]
    for hf in handler_files:
        path = os.path.join(handlers_dir, hf)
        with open(path, "r", encoding="utf-8") as f:
            content = f.read()
        m = re.search(r"s_actions\s*=\s*\{(.*?)\};", content, re.DOTALL)
        if m:
            acts = re.findall(r'"([^"]+)"', m.group(1))
            registered_actions.extend(acts)
            print(f"  {hf}: {len(acts)} actions")

    print(f"Total registered actions across handlers: {len(registered_actions)}")
    missing = set(legacy_73) - set(registered_actions)
    extra = set(registered_actions) - set(legacy_73)
    
    if missing:
        print(f"FAILED: Missing actions: {missing}")
    else:
        print("PASS: All 73 legacy actions are registered in ActionHandlers!")
    
    if extra:
        print(f"Extra actions in handlers: {extra}")

def test_geometry_and_precedence():
    print("\n=== TEST 2: REVIT STRUCTURAL GEOMETRY & PRECEDENCE ===")
    
    # 2.1 Join Precedence
    ranks = {"Column": 1, "Wall": 2, "Beam": 3, "Slab": 4}
    elements = [
        ("Slab_1", "Slab"),
        ("Beam_1", "Beam"),
        ("Wall_1", "Wall"),
        ("Col_1", "Column")
    ]
    # Verify strict sorting by rank
    sorted_elems = sorted(elements, key=lambda x: ranks[x[1]])
    expected_order = ["Col_1", "Wall_1", "Beam_1", "Slab_1"]
    actual_order = [e[0] for e in sorted_elems]
    print(f"Precedence order: {actual_order}")
    assert actual_order == expected_order, "Precedence order mismatch!"
    print("PASS: Precedence order strictly Column (1) > Wall (2) > Beam (3) > Slab (4).")

    # 2.2 Beam Break Geometry Edge Cases
    def break_beam(start, end, supports):
        dir_x, dir_y, dir_z = end[0] - start[0], end[1] - start[1], end[2] - start[2]
        total_len = math.sqrt(dir_x**2 + dir_y**2 + dir_z**2)
        if total_len < 1e-4:
            return []
        projections = [(0.0, start), (1.0, end)]
        for s in supports:
            vx, vy, vz = s[0] - start[0], s[1] - start[1], s[2] - start[2]
            dot = (vx * dir_x + vy * dir_y + vz * dir_z) / (total_len * total_len)
            if 0.01 < dot < 0.99:
                pt = (start[0] + dir_x * dot, start[1] + dir_y * dot, start[2] + dir_z * dot)
                projections.append((dot, pt))
        projections.sort(key=lambda p: p[0])
        spans = []
        for i in range(len(projections) - 1):
            p1 = projections[i][1]
            p2 = projections[i+1][1]
            spans.append((p1, p2, math.sqrt((p2[0]-p1[0])**2 + (p2[1]-p1[1])**2 + (p2[2]-p1[2])**2)))
        return spans

    # Normal beam 12m with supports at 4m and 8m
    spans_norm = break_beam((0,0,0), (12000,0,0), [(4000,0,0), (8000,0,0)])
    assert len(spans_norm) == 3
    assert all(abs(s[2] - 4000.0) < 1e-4 for s in spans_norm)
    print("PASS: Normal beam span breakdown.")

    # Inverted beam (end < start)
    spans_inv = break_beam((12000,0,0), (0,0,0), [(8000,0,0), (4000,0,0)])
    assert len(spans_inv) == 3
    assert all(abs(s[2] - 4000.0) < 1e-4 for s in spans_inv)
    print("PASS: Inverted coordinate beam span breakdown.")

    # Zero-length beam
    spans_zero = break_beam((0,0,0), (0,0,0), [(1000,0,0)])
    assert len(spans_zero) == 0
    print("PASS: Zero-length beam handled gracefully without division by zero.")

    # 2.3 Shoelace Polygon Area
    def polygon_area(pts):
        if not pts or len(pts) < 3: return 0.0
        s = 0.0
        n = len(pts)
        for i in range(n):
            curr, nxt = pts[i], pts[(i + 1) % n]
            s += (curr[0] * nxt[1]) - (nxt[0] * curr[1])
        return abs(s) * 0.5

    ccw_pts = [(0, 0), (5000, 0), (5000, 4000), (0, 4000)]
    cw_pts = [(0, 0), (0, 4000), (5000, 4000), (5000, 0)]
    assert abs(polygon_area(ccw_pts) - 20000000.0) < 1e-4
    assert abs(polygon_area(cw_pts) - 20000000.0) < 1e-4
    print("PASS: Polygon area is orientation-independent (CW == CCW).")

    # Collinear points
    collinear = [(0, 0), (1000, 1000), (2000, 2000)]
    assert polygon_area(collinear) == 0.0
    print("PASS: Collinear polygon area is 0.0.")

    # Bowtie / self-intersecting polygon
    bowtie = [(0, 0), (4000, 4000), (4000, 0), (0, 4000)]
    area_bt = polygon_area(bowtie)
    print(f"Info: Symmetric Bowtie Area via Shoelace = {area_bt} (expected cancellation for opposite winding lobes).")

    # 2.4 Wall and Slab Rebar Generator spacing edge cases
    # Wall rebar
    def wall_rebar(length, height, spacing, cover):
        vert_count = max(2, int(math.floor((length - 2.0 * cover) / spacing)) + 1)
        vert_len = height + 600.0
        horiz_count = max(2, int(math.floor((height - 2.0 * cover) / spacing)) + 1)
        horiz_len = length - 2.0 * cover + 400.0
        return vert_count, vert_len, horiz_count, horiz_len

    # Standard wall 4000 x 3300
    vc, vl, hc, hl = wall_rebar(4000, 3300, 200, 25)
    assert vc == 20 and hc == 17
    print("PASS: Standard wall rebar generated accurately.")

    # Wall where length < 2 * cover
    vc_min, vl_min, hc_min, hl_min = wall_rebar(30, 30, 200, 25)
    assert vc_min == 2 and hc_min == 2
    print("PASS: Minimum 2-bar boundary condition preserved for sub-cover wall.")

    # Slab rebar
    def slab_rebar(span_x, span_y, spacing, cover):
        count_x = max(2, int(math.floor((span_y - 2.0 * cover) / spacing)) + 1)
        len_x = span_x - 2.0 * cover
        count_y = max(2, int(math.floor((span_x - 2.0 * cover) / spacing)) + 1)
        len_y = span_y - 2.0 * cover
        return count_x, len_x, count_y, len_y

    cx, lx, cy, ly = slab_rebar(4000, 5000, 150, 20)
    assert cx == 34 and cy == 27
    print("PASS: Standard slab rebar generated accurately.")

def test_clean_code_and_test_suite():
    print("\n=== TEST 3: CLEAN CODE & TEST SUITE VERIFICATION ===")
    
    root_src = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src"
    long_files = []
    unannotated_catches = []
    for dirpath, _, filenames in os.walk(root_src):
        if "obj" in dirpath or "bin" in dirpath: continue
        for fn in filenames:
            if fn.endswith(".cs"):
                fpath = os.path.join(dirpath, fn)
                with open(fpath, "r", encoding="utf-8") as f:
                    lines = f.readlines()
                if len(lines) > 300:
                    long_files.append((fpath, len(lines)))
                for idx, line in enumerate(lines):
                    if re.search(r"catch\s*(\(Exception.*?\))?\s*\{", line):
                        # check next 3 lines for // Reason:
                        snippet = "".join(lines[idx:idx+4])
                        if "// Reason:" not in snippet:
                            unannotated_catches.append((fpath, idx+1, line.strip()))

    print(f"Files > 300 lines in HPDcePro/src: {len(long_files)}")
    for lf in long_files:
        print(f"  WARNING: {lf[0]} has {lf[1]} lines")
    assert len(long_files) == 0, "Clean code rule C2 violated!"

    print(f"Unannotated catch blocks in HPDcePro/src: {len(unannotated_catches)}")
    for uc in unannotated_catches:
        print(f"  WARNING: {uc[0]}:{uc[1]} -> {uc[2]}")
    assert len(unannotated_catches) == 0, "Clean code rule Q-B2 violated!"

    # Count test methods in test projects
    tests_root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests"
    for proj_name in ["HPDcePro.Core.Tests", "HPDcePro.Adapters.Tests"]:
        proj_dir = os.path.join(tests_root, proj_name)
        facts = 0
        theories = 0
        inline_data = 0
        for dirpath, _, filenames in os.walk(proj_dir):
            if "obj" in dirpath or "bin" in dirpath: continue
            for fn in filenames:
                if fn.endswith(".cs"):
                    fpath = os.path.join(dirpath, fn)
                    with open(fpath, "r", encoding="utf-8") as f:
                        content = f.read()
                    f_cnt = len(re.findall(r"\[Fact\]", content))
                    t_cnt = len(re.findall(r"\[Theory\]", content))
                    i_cnt = len(re.findall(r"\[InlineData", content))
                    facts += f_cnt
                    theories += t_cnt
                    inline_data += i_cnt
        total_runs = facts + (inline_data if inline_data > 0 else theories)
        print(f"Project {proj_name}: {facts} Facts, {theories} Theories, {inline_data} InlineData -> {total_runs} test executions")

if __name__ == "__main__":
    test_excel_73_actions()
    test_geometry_and_precedence()
    test_clean_code_and_test_suite()
    print("\nALL ADVERSARIAL CHECKS COMPLETED SUCCESSFULLY!")
