import os
import re

ROOT_DIR = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim"

def analyze_csharp_files():
    c2_violations = []
    m3_violations = []
    qb1_violations = []
    qb2_violations = []
    total_files = 0
    total_lines = 0

    # Pattern for empty catch without comment
    empty_catch_pattern = re.compile(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', re.MULTILINE)
    
    # Pattern for commented out code (simple heuristic: // followed by C# keywords like var, if, for, return, throw, class, using)
    commented_code_pattern = re.compile(r'^\s*//\s*(var|if|for|foreach|while|return|throw|using|public|private|protected|internal|class|void|int|string|bool)\s+[A-Za-z0-9_]', re.MULTILINE)

    for root, dirs, files in os.walk(ROOT_DIR):
        # Skip bin and obj directories
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        if ".vs" in dirs:
            dirs.remove(".vs")
            
        for file in files:
            if file.endswith(".cs"):
                total_files += 1
                full_path = os.path.join(root, file)
                rel_path = os.path.relpath(full_path, ROOT_DIR)
                
                with open(full_path, "r", encoding="utf-8", errors="ignore") as f:
                    lines = f.readlines()
                
                num_lines = len(lines)
                total_lines += num_lines
                
                # Rule C2: <= 300 lines
                if num_lines > 300:
                    c2_violations.append((rel_path, num_lines))
                
                content = "".join(lines)
                
                # Rule Q-B2: Empty catch
                for m in empty_catch_pattern.finditer(content):
                    qb2_violations.append((rel_path, m.group(0)))
                    
                # Rule Q-B1: Commented out code
                for line_idx, line in enumerate(lines, 1):
                    # Check if line looks like commented out code
                    if commented_code_pattern.search(line):
                        # Filter out legitimate comment patterns like XML doc or examples in comments
                        if not line.strip().startswith("///") and "e.g." not in line and "example" not in line.lower():
                            qb1_violations.append((rel_path, line_idx, line.strip()))

    print(f"Total C# files scanned: {total_files}")
    print(f"Total lines scanned: {total_lines}")
    print(f"\nRule C2 Violations (> 300 lines): {len(c2_violations)}")
    for v in c2_violations:
        print(f"  {v[0]}: {v[1]} lines")
        
    print(f"\nRule Q-B2 Violations (empty catch without comment): {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  {v[0]}: {v[1]}")
        
    print(f"\nRule Q-B1 Candidate Violations (commented code): {len(qb1_violations)}")
    for v in qb1_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

if __name__ == "__main__":
    analyze_csharp_files()
