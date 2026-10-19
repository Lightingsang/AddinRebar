# -*- coding: utf-8 -*-
"""
Verification script for docs/rea-dce-pro/TRUTH_MATRIX.md
"""

import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

MATRIX_PATH = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md"

if not os.path.exists(MATRIX_PATH):
    print("FAIL: TRUTH_MATRIX.md does not exist!")
    sys.exit(1)

with open(MATRIX_PATH, 'r', encoding='utf-8') as f:
    text = f.read()
    lines = text.split('\n')

print(f"File size: {os.path.getsize(MATRIX_PATH)} bytes")
print(f"Total lines: {len(lines)}")

# 1. Verify Headings
headings = [line for line in lines if line.startswith('#')]
print(f"Total headings: {len(headings)}")

required_sections = [
    "## 1. Executive Summary & Forensic Parity Statistics",
    "## 2. Subsystem 1: AutoCAD <CommandMethod> Endpoints (311 Entries)",
    "## 3. Subsystem 2: Excel Ribbon Commands & Dispatch Actions (73 Entries)",
    "## 4. Subsystem 3: DceSteel Detailing & Editing Facade (49 Entries)",
    "## 5. Subsystem 4: MBKC Structural Framing Plan Facade (29 Entries)",
    "## 6. Subsystem 5: Native C++ Mathematical & Geometric Core (69 Exports)",
    "## 7. Subsystem 6: Revit 2026 Structural Commands & Application (24 Entries)",
    "## 8. Subsystem 7: Interactive AutoCAD Jigs (11 Jigs)",
    "## 9. Subsystem 8: Specialized UI Forms, Dialogs & Palettes (12 Entries)",
    "## 10. Subsystem 9: AutoStr 24-Step Automation Pipeline (24 Steps)",
    "## 11. Static Engineering Assets & Template Cross-Reference",
    "## 12. Forensic Evidence & Technical Causes of Blockers"
]

missing_sections = []
for req in required_sections:
    if not any(req in h for h in headings):
        missing_sections.append(req)

if missing_sections:
    print(f"FAIL: Missing sections: {missing_sections}")
    sys.exit(1)
else:
    print("PASS: All 12 required sections are present.")

# 2. Verify Table Rows and Column Counts
table_rows = [l for l in lines if l.startswith('|') and not l.startswith('|---') and not l.startswith('| Legacy Identifier') and not l.startswith('| Subsystem Index') and not l.startswith('| Asset Category')]

subsystem_rows = []
for r in table_rows:
    cols = [c.strip() for c in r.split('|')[1:-1]]
    if len(cols) == 6:
        subsystem_rows.append(cols)

print(f"Total 6-column endpoint rows: {len(subsystem_rows)}")
if len(subsystem_rows) != 602:
    print(f"FAIL: Expected 602 endpoint rows, found {len(subsystem_rows)}")
    sys.exit(1)
else:
    print("PASS: Exactly 602 endpoint rows found.")

# 3. Status Breakdown
statuses = {}
for r in subsystem_rows:
    st = r[5]
    statuses[st] = statuses.get(st, 0) + 1

print(f"Statuses found: {statuses}")
expected_statuses = {"HOÀN THÀNH": 130, "ĐANG LÀ MOCK": 102, "CHƯA GIẢI MÃ ĐƯỢC": 370}
for k, v in expected_statuses.items():
    if statuses.get(k, 0) != v:
        print(f"FAIL: Expected {k} = {v}, found {statuses.get(k, 0)}")
        sys.exit(1)

print("PASS: Parity classification distribution matches exact expected totals (130 HOÀN THÀNH, 102 ĐANG LÀ MOCK, 370 CHƯA GIẢI MÃ ĐƯỢC).")

# 4. Verify No Unescaped Pipes or Malformed Lines
malformed = 0
for idx, l in enumerate(lines):
    if l.startswith('|') and not l.endswith('|'):
        print(f"Line {idx+1} malformed: does not end with |")
        malformed += 1

if malformed == 0:
    print("PASS: Zero malformed table rows detected.")
else:
    print(f"FAIL: {malformed} malformed lines detected.")
    sys.exit(1)

# 5. Verify Static Assets in Section 11
sec11_text = text[text.find("## 11."):text.find("## 12.")]
asset_checks = [
    ("DWG Block & Titleblock Templates", "17"),
    ("XLSM Engineering Calculation Workbooks", "132"),
    ("Machine Learning Database & Weights", "2"),
    ("System Configuration Defaults", "178"),
    ("Language Localization Dictionaries", "8"),
    ("Icons & Ribbon Visual Assets", "133"),
    ("AutoCAD Ribbon Customization Menus", "7")
]
for cat, count in asset_checks:
    if cat in sec11_text and f"| {count} |" in sec11_text:
        print(f"PASS: Asset category '{cat}' verified with count {count}.")
    else:
        print(f"FAIL: Asset category '{cat}' missing or count mismatch.")
        sys.exit(1)

# 6. Verify Section 12 Blocker Details
sec12_text = text[text.find("## 12."):]
blocker_keywords = ["Babel .NET", "getJit", "VirtualProtect", "out-of-bounds", "clean-room"]
for kw in blocker_keywords:
    if kw.lower() in sec12_text.lower():
        print(f"PASS: Blocker keyword '{kw}' present in Section 12.")
    else:
        print(f"FAIL: Blocker keyword '{kw}' missing from Section 12.")
        sys.exit(1)

print("\nALL VERIFICATIONS PASSED CLEANLY (100% SUCCESS).")
