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

def find_methods_in_file(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    methods = []
    
    # We track braces
    # When encountering a method signature followed by '{'
    method_sig_pattern = re.compile(
        r'^\s*(public|private|protected|internal)?\s*(static|async|virtual|override|abstract|sealed)?\s*'
        r'(public|private|protected|internal)?\s*(static|async|virtual|override|abstract|sealed)?\s*'
        r'([\w\<\>\[\],\s\?]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(where\s+[^{]+)?$'
    )

    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # Look for method definition
        # Also include constructors: e.g. public ClassName(...)
        # Check if line matches method pattern
        if ("(" in line and ")" in line and not line.endswith(";") and not line.startswith("//") and not line.startswith("/*")):
            # Check if followed by { (maybe on same or next line)
            start_line = i + 1
            # Look backwards for attributes [Attribute]
            attr_start = start_line
            k = i - 1
            while k >= 0 and lines[k].strip().startswith("[") and lines[k].strip().endswith("]"):
                attr_start = k + 1
                k -= 1
                
            # Find the opening brace '{'
            brace_idx = i
            found_open_brace = False
            while brace_idx < len(lines) and brace_idx < i + 5:
                if "{" in lines[brace_idx]:
                    found_open_brace = True
                    break
                if lines[brace_idx].strip().endswith(";"):
                    # abstract or interface or expression
                    break
                brace_idx += 1
                
            if found_open_brace:
                open_line = brace_idx + 1
                # Now match braces to find closing brace
                brace_count = 0
                end_line = open_line
                for curr in range(brace_idx, len(lines)):
                    # simple brace counting (ignoring strings for now or basic string skipping)
                    curr_line = lines[curr]
                    # strip comments
                    c_line = re.sub(r'//.*', '', curr_line)
                    brace_count += c_line.count('{')
                    brace_count -= c_line.count('}')
                    if brace_count == 0:
                        end_line = curr + 1
                        span = end_line - attr_start + 1
                        methods.append((line[:40], attr_start, end_line, span))
                        i = curr
                        break
        i += 1
    return methods

def main():
    all_methods = []
    violations = []
    for d in DELIVERABLE_DIRS:
        for f in os.listdir(d):
            if f.endswith(".cs"):
                fpath = os.path.join(d, f)
                ms = find_methods_in_file(fpath)
                for m in ms:
                    all_methods.append((f, m))
                    if m[3] > 50:
                        violations.append((f, m))
                        
    print(f"Total methods analyzed: {len(all_methods)}")
    print(f"Rule M3 violations (> 50 lines): {len(violations)}")
    for v in violations:
        print(f"  {v[0]}: {v[1][0]} (L{v[1][1]}-L{v[1][2]}, {v[1][3]} lines)")

if __name__ == "__main__":
    main()
