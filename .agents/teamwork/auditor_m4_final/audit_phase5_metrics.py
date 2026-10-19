import os
import re

PHASE5_PATHS = [
    # Core
    r"HPKata.Core\QuantitySurveying",
    r"HPKata.Core\DrawingLayout",
    r"HPKata.Core\CadUtilities",
    # UI
    r"HPKata.UI\ViewModels\QsTakeoffViewModel.cs",
    r"HPKata.UI\ViewModels\DrawingUtilityViewModel.cs",
    r"HPKata.UI\Views\QsTakeoffWindow.xaml.cs",
    r"HPKata.UI\Views\DrawingUtilityWindow.xaml.cs",
    # AutoCAD
    r"HPKata.AutoCad\Commands\QsCommands.cs",
    r"HPKata.AutoCad\Commands\LayoutCommands.cs",
    r"HPKata.AutoCad\Commands\DimensionUtilityCommands.cs",
    r"HPKata.AutoCad\Controllers\QsTakeoffController.cs",
    r"HPKata.AutoCad\Controllers\DimensionUtilityController.cs",
    r"HPKata.AutoCad\Controllers\GridDrawController.cs",
    r"HPKata.AutoCad\Controllers\SheetLayoutController.cs",
    r"HPKata.AutoCad\Controllers\DrawingIndexController.cs",
    r"HPKata.AutoCad\Controllers\ViewportController.cs",
    # Revit
    r"HPKata.Revit\Commands\QsTakeoffCommand.cs",
    r"HPKata.Revit\Commands\SheetArrangementCommand.cs",
    r"HPKata.Revit\Services\RevitQsTakeoffService.cs",
    r"HPKata.Revit\Services\RevitSheetArrangementService.cs",
]

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def is_phase5(rel_path):
    for p in PHASE5_PATHS:
        if p.lower() in rel_path.lower():
            return True
    return False

def audit_phase5():
    phase5_files = []
    for root, dirs, files in os.walk(BASE_DIR):
        if "bin" in dirs: dirs.remove("bin")
        if "obj" in dirs: dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                full = os.path.join(root, f)
                rel = os.path.relpath(full, BASE_DIR)
                if is_phase5(rel):
                    phase5_files.append((full, rel))

    print(f"Total Phase 5 production files identified: {len(phase5_files)}")
    
    # Check C2
    c2_violations = []
    for full, rel in phase5_files:
        with open(full, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        if len(lines) > 300:
            c2_violations.append((rel, len(lines)))
    print(f"\n--- Check C2 (<= 300 lines) ---")
    print(f"Violations: {len(c2_violations)}")
    for rel, count in c2_violations:
        print(f"  {rel}: {count} lines")

    # Check M3 on Phase 5 production files
    method_sig = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w<>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where[^{]+)?$')
    m3_violations = []
    for full, rel in phase5_files:
        with open(full, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        i = 0
        n = len(lines)
        while i < n:
            line = lines[i].strip()
            if not line.startswith("//") and not line.startswith("/*") and not line.startswith("["):
                m = method_sig.match(lines[i])
                if m and not line.endswith(";"):
                    name = m.group(1)
                    if name not in ["class", "struct", "interface", "enum", "record", "namespace"]:
                        start_line = i + 1
                        brace_count = 0
                        found_open = False
                        j = i
                        while j < n:
                            for ch in lines[j]:
                                if ch == '{':
                                    brace_count += 1
                                    found_open = True
                                elif ch == '}':
                                    brace_count -= 1
                            if found_open and brace_count == 0:
                                end_line = j + 1
                                length = end_line - start_line + 1
                                if length > 50:
                                    m3_violations.append((rel, name, start_line, end_line, length))
                                i = j
                                break
                            j += 1
            i += 1

    print(f"\n--- Check M3 (<= 50 lines) in Phase 5 ---")
    print(f"Violations: {len(m3_violations)}")
    for rel, name, s, e, l in m3_violations:
        print(f"  {rel} :: {name} ({s}-{e}): {l} lines")

if __name__ == "__main__":
    audit_phase5()
