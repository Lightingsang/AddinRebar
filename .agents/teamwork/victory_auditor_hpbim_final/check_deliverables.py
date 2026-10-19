import os
import re

DELIVERABLE_DIRS = [
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Validation",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Execution",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Coordination",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Verification",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Connection",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Verification",
]

def check_deliverable_files():
    files_to_check = []
    for d in DELIVERABLE_DIRS:
        for f in os.listdir(d):
            if f.endswith(".cs"):
                files_to_check.append(os.path.join(d, f))

    print(f"Total deliverable production files: {len(files_to_check)}")
    
    c2_violations = []
    qb2_violations = []
    max_lines = 0
    max_file = ""

    empty_catch_pattern = re.compile(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', re.MULTILINE)

    for path in files_to_check:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        count = len(lines)
        if count > max_lines:
            max_lines = count
            max_file = os.path.basename(path)
        if count > 300:
            c2_violations.append((path, count))
            
        content = "".join(lines)
        if empty_catch_pattern.search(content):
            qb2_violations.append(path)

    print(f"Max physical lines in deliverable files: {max_lines} ({max_file})")
    print(f"Rule C2 Violations (> 300 lines): {len(c2_violations)}")
    for v in c2_violations:
        print(f"  {v[0]}: {v[1]}")
    print(f"Rule Q-B2 Violations: {len(qb2_violations)}")

if __name__ == "__main__":
    check_deliverable_files()
