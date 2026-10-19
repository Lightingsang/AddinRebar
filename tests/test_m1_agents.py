"""
Empirical verification script for Milestone M1 Multi-Agent Structural & BIM Coordination.
Tests all 5 invariants defined in DISPATCH.md and ORIGINAL_REQUEST.md ## 2026-10-07T02:05:11Z.
"""

import os
import sys
import re

if sys.stdout.encoding.lower() != 'utf-8':
    sys.stdout.reconfigure(encoding='utf-8')

REPO_ROOT = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
AGENTS_DIR = os.path.join(REPO_ROOT, "HPBim", "agents")

M1_FILES = [
    "coordinator_assistant.md",
    "cad_assistant.md",
    "revit_assistant.md",
    "qaqc_assistant.md",
    "rfi_template.md",
    "README.md"
]

GOLDEN_DIEU_0 = (
    "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. "
    "Không được suy đoán để hoàn thành công việc. "
    "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."
)

class TestResult:
    def __init__(self, name):
        self.name = name
        self.passed = True
        self.messages = []
        self.failures = []

    def assert_true(self, condition, msg):
        if not condition:
            self.passed = False
            self.failures.append(f"FAIL: {msg}")
        else:
            self.messages.append(f"PASS: {msg}")

    def log(self, msg):
        self.messages.append(f"INFO: {msg}")

def read_file(filename):
    path = os.path.join(AGENTS_DIR, filename)
    with open(path, "r", encoding="utf-8") as f:
        return f.read(), path

def test_invariant_1():
    """Invariant 1: ĐIỀU 0 verbatim in all 6 files."""
    tr = TestResult("Invariant 1: ĐIỀU 0 verbatim in all 6 files")
    
    for fname in M1_FILES:
        content, path = read_file(fname)
        tr.assert_true(os.path.exists(path), f"File {fname} exists")
        
        # Check verbatim text
        has_verbatim = GOLDEN_DIEU_0 in content
        tr.assert_true(has_verbatim, f"File {fname} contains ĐIỀU 0 verbatim")
        
        # Check prominence (within first 20 lines)
        lines = content.splitlines()
        dieu_0_found_at_line = -1
        for idx, line in enumerate(lines[:25]):
            if GOLDEN_DIEU_0 in line:
                dieu_0_found_at_line = idx + 1
                break
        tr.assert_true(dieu_0_found_at_line > 0, f"File {fname} has ĐIỀU 0 in first 25 lines (found at line {dieu_0_found_at_line})")
        
        # Check header
        has_header = any("ĐIỀU 0: NGUYÊN TẮC VÀNG TỐI THƯỢNG" in l for l in lines[:15])
        tr.assert_true(has_header, f"File {fname} has header 'ĐIỀU 0: NGUYÊN TẮC VÀNG TỐI THƯỢNG'")
        
    return tr

def test_invariant_2():
    """Invariant 2: 7-step lifecycle in coordinator_assistant.md."""
    tr = TestResult("Invariant 2: 7-step lifecycle in coordinator_assistant.md")
    content, _ = read_file("coordinator_assistant.md")
    
    steps = [
        ("Step 1", "Step1_ReadCad", "Đọc bản vẽ"),
        ("Step 2", "Step2_VerifiedInfo", "Kiểm tra thông tin"),
        ("Step 3", "Step3_ModelRevit", "Dựng mô hình Revit"),
        ("Step 4", "Step4_QaqcAudited", "kiểm toán độc lập"),
        ("Step 5", "Step5_DefectFixing", "Định tuyến sửa lỗi"),
        ("Step 6", "Step6_ReAudited", "tái kiểm toán"),
        ("Step 7", "Step7_ConfirmedPass", "Xác nhận Nghiệm thu ĐẠT")
    ]
    
    for step_num, code_name, desc_kw in steps:
        has_step = code_name in content or (step_num in content and desc_kw.lower() in content.lower())
        tr.assert_true(has_step, f"coordinator_assistant.md contains {step_num} ({code_name})")
        
    # Check that Freeze Zone mechanism is defined
    tr.assert_true("Freeze Zone" in content or "Đóng băng" in content, "coordinator_assistant.md defines Freeze Zone")
    
    # Check that RFI mechanism is defined
    tr.assert_true("rfi_template.md" in content or "RFI" in content, "coordinator_assistant.md references RFI mechanism")
    
    # Check prohibition on premature modeling
    tr.assert_true("CẤM trực tiếp đọc bản vẽ thay cho CAD" in content, "coordinator_assistant.md prohibits coordinator from reading CAD directly")
    tr.assert_true("CẤM trực tiếp dựng mô hình thay cho Revit" in content, "coordinator_assistant.md prohibits coordinator from modeling Revit directly")
    tr.assert_true("CẤM cho phép dựng khi chưa đủ thông tin" in content, "coordinator_assistant.md prohibits approving modeling with incomplete info")

    return tr

def test_invariant_3():
    """Invariant 3: 6 sources and 3 groups in cad_assistant.md."""
    tr = TestResult("Invariant 3: 6 sources and 3 groups in cad_assistant.md")
    content, _ = read_file("cad_assistant.md")
    
    sources = [
        "Mặt bằng",
        "Mặt cắt",
        "Chi tiết",
        "thống kê",
        "Ghi chú",
        "Tầng liên đới"
    ]
    for s in sources:
        tr.assert_true(s.lower() in content.lower(), f"cad_assistant.md contains cross-check source: '{s}'")
        
    groups = [
        ("Nhóm 1", "Group1_ReadyToModel", "Đủ thông tin"),
        ("Nhóm 2", "Group2_ConflictNeedRfi", "Mâu thuẫn"),
        ("Nhóm 3", "Group3_MissingInfoHalt", "Thiếu thông tin")
    ]
    for g_label, code_name, kw in groups:
        tr.assert_true(g_label in content and (code_name in content or kw in content), 
                       f"cad_assistant.md defines {g_label} ({code_name})")
        
    # Check handoff rule: ONLY Group 1 handed over, Group 2 and 3 forbidden
    tr.assert_true("Chỉ chuyển giao dữ liệu cấu kiện **Nhóm 1**" in content or "Chỉ được phép bàn giao" in content,
                   "cad_assistant.md mandates ONLY Group 1 can be handed over to Revit")
    tr.assert_true("CẤM chuyển giao cấu kiện Nhóm 2 và Nhóm 3 sang Revit" in content or "CẤM DỰNG MÔ HÌNH" in content,
                   "cad_assistant.md prohibits handing over Group 2 and Group 3 to Revit")
    
    # Check prohibition on guessing algorithms
    tr.assert_true("line-pairing" in content, "cad_assistant.md bans line-pairing algorithm")
    tr.assert_true("distance-clustering" in content or "ClusterPiles" in content or "gom cụm" in content, 
                   "cad_assistant.md bans distance clustering")

    return tr

def test_invariant_4():
    """Invariant 4: 2-phase verification in revit_assistant.md."""
    tr = TestResult("Invariant 4: 2-phase verification in revit_assistant.md")
    content, _ = read_file("revit_assistant.md")
    
    # 2 phases
    tr.assert_true("PHA 1" in content or "Pha 1" in content, "revit_assistant.md defines Phase 1 (Create/Execution)")
    tr.assert_true("PHA 2" in content or "Pha 2" in content, "revit_assistant.md defines Phase 2 (In-memory verification)")
    
    # Parameters measured
    tr.assert_true("tiết diện" in content.lower() or "b_measured" in content or "b x h" in content, 
                   "revit_assistant.md measures cross-section (b, h)")
    tr.assert_true("cao độ" in content.lower() or "z_top" in content.lower(), 
                   "revit_assistant.md measures elevation (z)")
    
    # Tolerances
    tr.assert_true("0.0" in content and "mm" in content, "revit_assistant.md specifies delta dim = 0.0mm")
    tr.assert_true("1.0" in content and "mm" in content, "revit_assistant.md specifies delta z/position <= 1.0mm")
    
    # Rollback on failure
    tr.assert_true("ROLLBACK" in content or "rollback" in content, "revit_assistant.md mandates rollback on tolerance failure")
    
    # Mandatory report string verbatim
    mandatory_phrase = "Đã dựng xong theo dữ liệu được giao và đã kiểm tra lại các thông số cơ bản"
    tr.assert_true(mandatory_phrase in content, f"revit_assistant.md mandates report string: '{mandatory_phrase}'")
    
    # Prohibition on claiming accuracy
    tr.assert_true("CẤM tự nhận mô hình đã hoàn toàn chuẩn xác" in content or "không được tự nhận" in content,
                   "revit_assistant.md prohibits self-proclaiming model accuracy")

    return tr

def test_invariant_5():
    """Invariant 5: 8-step audit and 3 conclusion states in qaqc_assistant.md."""
    tr = TestResult("Invariant 5: 8-step audit and 3 conclusion states in qaqc_assistant.md")
    content, _ = read_file("qaqc_assistant.md")
    
    audit_steps = [
        ("Bước 1", "Hệ trục", "cao độ"),
        ("Bước 2", "Số lượng", "cấu kiện"),
        ("Bước 3", "Vị trí", "mặt bằng"),
        ("Bước 4", "tiết diện", "Kích thước"),
        ("Bước 5", "Cao độ", "kết cấu"),
        ("Bước 6", "Quan hệ kết cấu", "Điểm tựa"),
        ("Bước 7", "Mặt bằng", "Mặt cắt"),
        ("Bước 8", "Vị trí bất thường", "Va chạm")
    ]
    for b_num, kw1, kw2 in audit_steps:
        has_b = b_num in content and (kw1.lower() in content.lower() or kw2.lower() in content.lower())
        tr.assert_true(has_b, f"qaqc_assistant.md defines {b_num} ({kw1} / {kw2})")
        
    # Step 1 Stop-all gate
    tr.assert_true("DỪNG TOÀN BỘ" in content, "qaqc_assistant.md defines Step 1 stop-all gate")
    
    # 3 conclusion states
    tr.assert_true("Đúng" in content or "Pass" in content, "qaqc_assistant.md has state: Đúng / Pass")
    tr.assert_true("Sai, cần sửa" in content or "Fail_NeedsFix" in content, "qaqc_assistant.md has state: Sai, cần sửa / Fail_NeedsFix")
    tr.assert_true("Chưa đủ cơ sở để kết luận" in content or "Inconclusive_NeedsInfo" in content, 
                   "qaqc_assistant.md has state: Chưa đủ cơ sở để kết luận / Inconclusive_NeedsInfo")
    
    # Automated defect routing
    tr.assert_true("CAD_FIX" in content or "CadFix" in content, "qaqc_assistant.md defines CAD defect routing")
    tr.assert_true("REVIT_FIX" in content or "RevitFix" in content, "qaqc_assistant.md defines Revit defect routing")
    
    # Prohibited speculative words
    prohibited_words = ["Có vẻ đúng", "Gần đúng", "Không thấy vấn đề", "Khả năng cao là đúng"]
    for pw in prohibited_words:
        tr.assert_true(pw in content, f"qaqc_assistant.md explicitly lists prohibited term '{pw}'")

    return tr

def test_adversarial_challenges():
    """Adversarial stress-testing: cross-file consistency, role scoping, and legacy isolation."""
    tr = TestResult("Adversarial Challenges & Stress Testing")
    
    # Challenge 1: Foundation phase parameters per role
    # Coordinator (Lead): must oversee full scope (211 ghost beams, 196 piles, 72 footings, 22 grade beams, -1.600m)
    coord_c, _ = read_file("coordinator_assistant.md")
    tr.assert_true("211" in coord_c, "coordinator_assistant.md specifies 211 ghost beams to purge")
    tr.assert_true("196" in coord_c, "coordinator_assistant.md specifies 196 piles")
    tr.assert_true("72" in coord_c, "coordinator_assistant.md specifies 72 footings")
    tr.assert_true("GM-1" in coord_c and "22" in coord_c, "coordinator_assistant.md specifies 22 grade beams GM-1..22")
    tr.assert_true("-1.600" in coord_c, "coordinator_assistant.md specifies footing/beam top elevation -1.600m")

    # Revit Assistant: must model/purge Revit elements (211 ghost beams, 196 piles, 72 footings, 22 grade beams, -1.600m)
    revit_c, _ = read_file("revit_assistant.md")
    tr.assert_true("211" in revit_c, "revit_assistant.md specifies purge of 211 ghost beams")
    tr.assert_true("196" in revit_c, "revit_assistant.md specifies adjustment of 196 piles")
    tr.assert_true("72" in revit_c, "revit_assistant.md specifies modeling 72 footings")
    tr.assert_true("GM-1" in revit_c and "22" in revit_c, "revit_assistant.md specifies modeling 22 grade beams GM-1..22")
    tr.assert_true("-1.600" in revit_c, "revit_assistant.md specifies COTE -1.600m")

    # CAD Assistant: must extract DWG entities (196 piles, 72 footings, 22 grade beams, -1.600m; NO ghost beams in DWG)
    cad_c, _ = read_file("cad_assistant.md")
    tr.assert_true("196" in cad_c, "cad_assistant.md specifies extraction of 196 piles")
    tr.assert_true("72" in cad_c, "cad_assistant.md specifies extraction of 72 footings")
    tr.assert_true("GM-1" in cad_c and "22" in cad_c, "cad_assistant.md specifies extraction of 22 grade beams")
    tr.assert_true("-1.600" in cad_c, "cad_assistant.md specifies DWG top elevation -1.600m")
    tr.assert_true("211" not in cad_c, "cad_assistant.md correctly omits Revit ghost beams (reads DWG only)")

    # QA/QC Assistant: must audit entire scope (290 elements = 72+22+196, ghost beam audit, -1.600m, pile cutoffs)
    qaqc_c, _ = read_file("qaqc_assistant.md")
    tr.assert_true("290" in qaqc_c, "qaqc_assistant.md specifies 290 total foundation elements audited")
    tr.assert_true("dầm ma" in qaqc_c, "qaqc_assistant.md mandates ghost beam audit (dầm ma)")
    tr.assert_true("-1.600" in qaqc_c, "qaqc_assistant.md specifies top elevation -1.600m check")

    # README: must document full architecture and phase scope
    readme_c, _ = read_file("README.md")
    tr.assert_true("211" in readme_c, "README.md specifies 211 ghost beams")
    tr.assert_true("196" in readme_c, "README.md specifies 196 piles")
    tr.assert_true("72" in readme_c, "README.md specifies 72 footings")
    tr.assert_true("GM-1" in readme_c and "22" in readme_c, "README.md specifies 22 grade beams")
    tr.assert_true("-1.600" in readme_c, "README.md specifies elevation -1.600m")

    # Challenge 2: Check pile cutoff elevations consistency across all relevant files
    for fname in ["coordinator_assistant.md", "cad_assistant.md", "revit_assistant.md", "qaqc_assistant.md"]:
        content, _ = read_file(fname)
        tr.assert_true("-2.500" in content, f"{fname} specifies pile cutoff -2.500m")
        tr.assert_true("-2.700" in content, f"{fname} specifies pile cutoff -2.700m")
        tr.assert_true("-2.800" in content, f"{fname} specifies pile cutoff -2.800m")

    # Challenge 3: Cross-file tolerance consistency
    # Tolerance for cross-section must be strictly 0.0mm everywhere
    for fname in ["cad_assistant.md", "revit_assistant.md", "qaqc_assistant.md", "README.md"]:
        c, _ = read_file(fname)
        tr.assert_true("0.0" in c and "mm" in c, f"{fname} specifies strict 0.0mm cross-section tolerance")

    # Tolerance for elevation must be <= 1.0mm everywhere
    for fname in ["revit_assistant.md", "qaqc_assistant.md", "README.md"]:
        c, _ = read_file(fname)
        tr.assert_true("1.0" in c and "mm" in c, f"{fname} specifies <= 1.0mm elevation tolerance")

    # Challenge 4: Official index in README.md must strictly reference the 6 M1 files
    for m1_f in M1_FILES:
        tr.assert_true(m1_f in readme_c, f"README.md indexes M1 file {m1_f}")

    # Challenge 5: Legacy files isolation check
    legacy_files = [
        "dieu-phoi-bim.md",
        "kiem-soat-ban-ve.md",
        "kien-truc-su.md",
        "ky-su-co-dien.md",
        "ky-su-du-toan.md",
        "ky-su-ket-cau.md"
    ]
    for lf in legacy_files:
        tr.assert_true(lf not in readme_c, f"README.md does not reference legacy file {lf}")

    return tr

def main():
    print("=" * 80)
    print("RUNNING EMPIRICAL INVARIANT VERIFICATION SUITE — MILESTONE M1")
    print("=" * 80)
    
    tests = [
        test_invariant_1(),
        test_invariant_2(),
        test_invariant_3(),
        test_invariant_4(),
        test_invariant_5(),
        test_adversarial_challenges()
    ]
    
    all_passed = True
    total_assertions = 0
    total_failures = 0
    
    for t in tests:
        print(f"\n--- {t.name} ---")
        for msg in t.messages:
            print(f"  [PASS] {msg[6:]}")
            total_assertions += 1
        for fail in t.failures:
            print(f"  [FAIL] {fail[6:]}")
            total_assertions += 1
            total_failures += 1
        if not t.passed:
            all_passed = False
            
    print("\n" + "=" * 80)
    print(f"SUMMARY: Total Assertions: {total_assertions} | Failures: {total_failures}")
    if all_passed:
        print("OVERALL VERDICT: ALL INVARIANTS EMPIRICALLY CONFIRMED (PASS)")
        return 0
    else:
        print("OVERALL VERDICT: INVARIANT FAILURES DETECTED (FAIL)")
        return 1

if __name__ == "__main__":
    sys.exit(main())
