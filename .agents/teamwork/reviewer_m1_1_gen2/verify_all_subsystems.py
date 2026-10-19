import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open("docs/rea-dce-pro/TRUTH_MATRIX.md", "r", encoding="utf-8") as f:
    text = f.read()

subsystems = [
    ("Subsystem 1 (AutoCAD Commands)", r"## 2\. Subsystem 1: AutoCAD <CommandMethod> Endpoints.*?\n(.*?)\n## 3\. Subsystem 2:", 311, 0, 64, 247),
    ("Subsystem 2 (Excel Ribbon Actions)", r"## 3\. Subsystem 2: Excel Ribbon Commands & Dispatch Actions.*?\n(.*?)\n## 4\. Subsystem 3:", 73, 5, 0, 68),
    ("Subsystem 3 (DceSteel Facade)", r"## 4\. Subsystem 3: DceSteel Detailing & Editing Facade.*?\n(.*?)\n## 5\. Subsystem 4:", 49, 37, 6, 6),
    ("Subsystem 4 (MBKC Facade)", r"## 5\. Subsystem 4: MBKC Structural Framing Plan Facade.*?\n(.*?)\n## 6\. Subsystem 5:", 29, 6, 7, 16),
    ("Subsystem 5 (Native C++ Exports)", r"## 6\. Subsystem 5: Native C\+\+ Mathematical & Geometric Core.*?\n(.*?)\n## 7\. Subsystem 6:", 69, 65, 0, 4),
    ("Subsystem 6 (Revit 2026 Commands)", r"## 7\. Subsystem 6: Revit 2026 Structural Commands & Application.*?\n(.*?)\n## 8\. Subsystem 7:", 24, 1, 2, 21),
    ("Subsystem 7 (CAD Jigs)", r"## 8\. Subsystem 7: Interactive AutoCAD Jigs.*?\n(.*?)\n## 9\. Subsystem 8:", 11, 0, 11, 0),
    ("Subsystem 8 (UI Forms & Palettes)", r"## 9\. Subsystem 8: Specialized UI Forms, Dialogs & Palettes.*?\n(.*?)\n## 10\. Subsystem 9:", 12, 0, 12, 0),
    ("Subsystem 9 (AutoStr Steps)", r"## 10\. Subsystem 9: AutoStr 24-Step Automation Pipeline.*?\n(.*?)\n## 11\. Static Engineering Assets", 24, 16, 8, 0),
]

all_ok = True
tot_count = 0
tot_ht = 0
tot_mock = 0
tot_chua = 0

print("SUBSYSTEM VERIFICATION ACROSS ALL SECTIONS:")
for name, pattern, exp_total, exp_ht, exp_mock, exp_chua in subsystems:
    m = re.search(pattern, text, re.DOTALL)
    assert m, f"Pattern failed for {name}"
    sec_text = m.group(1)
    rows = [l.strip() for l in sec_text.splitlines() if l.strip().startswith("|") and not l.strip().startswith("|---") and not "Legacy Identifier" in l and not "Step Number" in l and not "Export Name / Ordinal" in l]
    
    ht = sum(1 for r in rows if "HOÀN THÀNH" in r)
    mock = sum(1 for r in rows if "ĐANG LÀ MOCK" in r)
    chua = sum(1 for r in rows if "CHƯA GIẢI MÃ ĐƯỢC" in r)
    count = len(rows)
    
    tot_count += count
    tot_ht += ht
    tot_mock += mock
    tot_chua += chua
    
    match = (count == exp_total and ht == exp_ht and mock == exp_mock and chua == exp_chua)
    print(f"  {name:38} | Rows: {count:3} (exp {exp_total:3}) | HT: {ht:2} (exp {exp_ht:2}) | MOCK: {mock:2} (exp {exp_mock:2}) | CHUA: {chua:3} (exp {exp_chua:3}) | Match: {match}")
    if not match:
        all_ok = False

print("-" * 80)
print(f"  {'TOTAL':38} | Rows: {tot_count:3} (exp 602) | HT: {tot_ht:2} (exp 130) | MOCK: {tot_mock:2} (exp 110) | CHUA: {tot_chua:3} (exp 362)")
print(f"ALL SUBSYSTEM TABLE ROWS MATCH EXACTLY: {all_ok}")
assert all_ok and tot_count == 602 and tot_ht == 130 and tot_mock == 110 and tot_chua == 362
