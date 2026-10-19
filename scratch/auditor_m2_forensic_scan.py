import os
import re
import sys

def check_c2(root_dir):
    print("=== CHECK C2: File line counts <= 300 ===")
    violations = []
    for dirpath, _, filenames in os.walk(root_dir):
        if any(x in dirpath for x in ['bin', 'obj', '.git']):
            continue
        for f in filenames:
            if f.endswith('.cs'):
                full_path = os.path.join(dirpath, f)
                with open(full_path, 'r', encoding='utf-8', errors='ignore') as fp:
                    lines = fp.readlines()
                    count = len(lines)
                    # Check modified M2 files specifically, or all bridge files
                    if count > 300:
                        rel = os.path.relpath(full_path, root_dir)
                        violations.append((rel, count))
    print(f"Total C# files exceeding 300 lines in {root_dir}: {len(violations)}")
    for v in violations:
        print(f"  VIOLATION C2: {v[0]}: {v[1]} lines")
    return len(violations)

def check_m2_files_c2_m3_qb(files):
    print("\n=== CHECK M2 SPECIFIC FILES (C2, M3, Q-B1, Q-B2) ===")
    c2_violations = []
    m3_violations = []
    qb1_violations = []
    qb2_violations = []

    for path in files:
        if not os.path.exists(path):
            print(f"File not found: {path}")
            continue
        with open(path, 'r', encoding='utf-8', errors='ignore') as fp:
            lines = fp.readlines()
            total_lines = len(lines)
            if total_lines > 300:
                c2_violations.append((path, total_lines))

            # Check Q-B1: commented out code
            # Pattern: lines starting with // that look like code (semicolon at end, statements)
            for idx, line in enumerate(lines, 1):
                stripped = line.strip()
                if stripped.startswith('//') and not stripped.startswith('///'):
                    comment_text = stripped[2:].strip()
                    # Check if it looks like commented out C# code
                    if re.search(r';\s*$', comment_text) and not re.search(r'(e\.g\.|http|TODO|NOTE|Finding|Rules|Rule)', comment_text, re.IGNORECASE):
                        if any(kw in comment_text for kw in ['var ', 'int ', 'return ', 'new ', 'if (', 'foreach (', 'public ', 'private ']):
                            qb1_violations.append((path, idx, line.strip()))

                # Check Q-B2: empty catch blocks without comment
                if re.match(r'^\s*catch\s*(\([^)]*\))?\s*\{\s*\}\s*$', stripped):
                    qb2_violations.append((path, idx, "empty catch block"))

            # Check M3: method line counts <= 50
            # Parse braces to measure method lengths
            in_method = False
            method_name = ""
            start_line = 0
            brace_depth = 0
            
            # Simple heuristic for method declaration
            method_pattern = re.compile(r'^\s*(public|private|internal|protected|static|async|sealed|virtual|override).*[\w\<\>\[\]]+\s+(\w+)\s*\([^;]*\)\s*(where.*)?$')
            
            for idx, line in enumerate(lines, 1):
                stripped = line.strip()
                if not in_method:
                    m = method_pattern.match(line)
                    if m and not line.strip().endswith(';'):
                        method_name = m.group(2)
                        start_line = idx
                        in_method = True
                        brace_depth = 0
                
                if in_method:
                    brace_depth += line.count('{') - line.count('}')
                    if '{' in line and brace_depth == 0 and start_line == idx:
                        # single line method e.g. { ... }
                        in_method = False
                    elif brace_depth <= 0 and ('}' in line or line.strip().endswith(';')):
                        length = idx - start_line + 1
                        if length > 50:
                            m3_violations.append((path, method_name, start_line, idx, length))
                        in_method = False

    print(f"C2 Violations (files > 300 lines): {len(c2_violations)}")
    for v in c2_violations:
        print(f"  {v[0]}: {v[1]} lines")

    print(f"M3 Violations (methods > 50 lines): {len(m3_violations)}")
    for v in m3_violations:
        print(f"  {v[0]} :: {v[1]} (lines {v[2]}-{v[3]}): {v[4]} lines")

    print(f"Q-B1 Violations (commented code): {len(qb1_violations)}")
    for v in qb1_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

    print(f"Q-B2 Violations (unannotated empty catch): {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

    return len(c2_violations) + len(m3_violations) + len(qb1_violations) + len(qb2_violations)

if __name__ == '__main__':
    m2_files = [
        r'HPGrasshopper\HPGrasshopper.McpBridge\Execution\GrasshopperExecutionEngine.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperContextExtractor.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerOutputExtractor.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerDataTreeHelper.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasBatchValidator.cs',
        r'HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerRunner.cs',
    ]
    total_violations = check_m2_files_c2_m3_qb(m2_files)
    check_c2(r'HPGrasshopper\HPGrasshopper.McpBridge')
    sys.exit(total_violations)
