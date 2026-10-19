import os
import re

files = [
    r"HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs",
    r"HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs",
    r"HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs",
    r"HPBim/src/HPBim.Client/Verification/RevitGeometryQueryScriptTemplate.cs",
    r"HPBim/tests/HPBim.Core.Tests/Verification/EmpiricalChallengerM2FoundationTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Cli/VerifyCommandTests.cs",
    r"HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricEngineRequiredMeasurementsTests.cs"
]

def find_methods(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    print(f"\n==================================================")
    print(f"FILE: {filepath} ({len(lines)} lines)")
    print(f"==================================================")
    
    in_raw_string = False
    depth = 0
    method_start = None
    method_name = None
    
    for idx, line in enumerate(lines):
        line_num = idx + 1
        s = line.strip()
        
        # Check raw string literal toggle
        if '"""' in line:
            # count occurrences of """
            count = line.count('"""')
            if count % 2 == 1:
                in_raw_string = not in_raw_string
                
        if in_raw_string:
            continue
            
        # Detect method declaration when depth == 1 (inside class)
        if depth == 1 and method_start is None:
            # Match method signature pattern
            if re.search(r"^\s*(public|private|protected|internal|static).*\([^\)]*\)", line):
                if not re.search(r"\b(class|record|interface|struct)\b", line):
                    method_start = line_num
                    method_name = line.strip()
                    # Check expression-bodied on single line
                    if "=>" in line and line.strip().endswith(";"):
                        length = 1
                        print(f"  [EXPR METHOD] {line_num}: {method_name[:50]} (length: 1 line)")
                        method_start = None
                        method_name = None
                        continue
        
        # Track braces
        open_b = line.count("{")
        close_b = line.count("}")
        
        if method_start is not None:
            if "=>" in lines[method_start - 1] and '"""' in lines[method_start - 1]:
                # Expression bodied returning raw string
                # Ends when """ is closed and ends with ;
                pass
            elif depth == 1 and open_b > 0 and "{" in line:
                # entering method body
                pass
                
        depth += open_b - close_b
        
        if method_start is not None and depth == 1 and close_b > 0:
            # closing method body
            length = line_num - method_start + 1
            print(f"  [METHOD] Lines {method_start}-{line_num} (length: {length}): {method_name[:60]}")
            method_start = None
            method_name = None

if __name__ == "__main__":
    for f in files:
        find_methods(f)

