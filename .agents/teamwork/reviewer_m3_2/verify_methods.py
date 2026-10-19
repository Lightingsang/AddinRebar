import os
import re

PHASE4_DIRS = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\Foundations",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\Slabs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\Stairs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI\ViewModels\FoundationDetailingViewModel.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI\ViewModels\SlabDetailingViewModel.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.UI\ViewModels\StairLintelDetailingViewModel.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\FoundationDrawController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\FoundationShopController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\FoundationTakeoffController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\SlabDrawController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\SlabShopController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\SlabTakeoffController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\StairDrawController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\LintelDrawController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\Controllers\SectionCutController.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Services\FoundationRebarGenerator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Services\SlabRebarGenerator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Services\StairRebarGenerator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Services\FoundationSlabStairScheduleService.cs",
]

def get_files():
    all_files = []
    for item in PHASE4_DIRS:
        if os.path.isfile(item):
            all_files.append(item)
        elif os.path.isdir(item):
            for root, dirs, files in os.walk(item):
                for f in files:
                    if f.endswith(".cs"):
                        all_files.append(os.path.join(root, f))
    return all_files

def check_m3_accurate():
    violations = []
    files = get_files()
    
    for path in files:
        rel_path = os.path.relpath(path, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")
        with open(path, "r", encoding="utf-8", errors="replace") as f:
            lines = f.readlines()
        
        # Track brace levels
        brace_level = 0
        method_name = None
        method_start = 0
        method_brace_level = 0
        
        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            # Ignore single line comments
            code_line = re.sub(r"//.*$", "", line)
            
            # Check method declaration when not already in a method
            if method_name is None:
                # heuristic for method header
                m = re.search(r"(?:public|private|protected|internal)(?:\s+static)?(?:\s+async)?\s+[\w\<\>\[\],\s\?\(\)]+\s+([A-Z]\w*)\s*\([^\)]*\)", stripped)
                if m and not stripped.endswith(";") and not "class " in stripped and not "interface " in stripped:
                    potential_name = m.group(1)
                    # Check if next or this line has {
                    method_name = potential_name
                    method_start = idx
                    method_brace_level = brace_level + 1
            
            # Count braces on code_line (ignoring string literals for simplicity)
            in_str = False
            for i, ch in enumerate(code_line):
                if ch == '"' and (i == 0 or code_line[i-1] != '\\'):
                    in_str = not in_str
                if not in_str:
                    if ch == '{':
                        brace_level += 1
                    elif ch == '}':
                        if method_name is not None and brace_level == method_brace_level:
                            # Method closed
                            length = idx - method_start + 1
                            if length > 50:
                                violations.append((rel_path, method_name, method_start, idx, length))
                            method_name = None
                        brace_level -= 1

    print(f"Accurate M3 Violations (>50 lines) across {len(files)} Phase 4 files: {len(violations)}")
    for v in violations:
        print(f"  {v[0]}:{v[2]} '{v[1]}' is {v[4]} lines ({v[2]}-{v[3]})")

if __name__ == "__main__":
    check_m3_accurate()
