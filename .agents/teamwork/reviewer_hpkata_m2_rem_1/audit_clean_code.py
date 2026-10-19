import os
import re
import sys

def check_cs_file(filepath):
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    total_lines = len(lines)
    c2 = total_lines > 300

    # Catch block checker: find empty catch blocks without comments
    qb2_issues = []
    # Method length checker
    m3_issues = []

    in_method = False
    method_name = ''
    start_line = 0
    brace_depth = 0
    class_brace_depth = 0
    started_braces = False

    for idx, line in enumerate(lines, 1):
        stripped = line.strip()

        # Check catch blocks
        if 'catch' in stripped:
            # Check if this line or next lines have empty catch
            # e.g., catch { } or catch (Exception) { }
            # Match catch ... { ... }
            if re.search(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', stripped):
                qb2_issues.append((idx, stripped))

        clean = re.sub(r'//.*', '', stripped)
        clean = re.sub(r'"(?:\\.|[^"\\])*"', '', clean)
        clean = re.sub(r"'(?:\\.|[^'\\])*'", '', clean)

        open_b = clean.count('{')
        close_b = clean.count('}')

        if class_brace_depth >= 1 and not in_method:
            m = re.match(r'(?:public|private|protected|internal|static|async|override|sealed|virtual|\s)+\s+[\w<>\[\]\?,\s]+\s+(\w+)\s*\([^;]*\)', stripped)
            if m and not stripped.endswith(';') and not stripped.startswith('return') and not stripped.startswith('throw'):
                method_name = m.group(1)
                start_line = idx
                in_method = True
                brace_depth = 0
                started_braces = False

        if in_method:
            if open_b > 0:
                started_braces = True
            brace_depth += open_b - close_b
            if started_braces and brace_depth <= 0:
                length = idx - start_line + 1
                if length > 50:
                    m3_issues.append((start_line, idx, method_name, length))
                in_method = False
                started_braces = False
        else:
            class_brace_depth += open_b - close_b

    return total_lines, c2, m3_issues, qb2_issues

def scan_project(proj_dir, is_test_project=False):
    c2_violations = []
    m3_violations = []
    qb2_violations = []
    file_count = 0

    for root, dirs, files in os.walk(proj_dir):
        for b in ['bin', 'obj', '.git']:
            if b in dirs: dirs.remove(b)
        for f in files:
            if f.endswith('.cs'):
                file_count += 1
                path = os.path.join(root, f)
                total, c2, m3, qb2 = check_cs_file(path)
                if c2:
                    c2_violations.append((path, total))
                if m3:
                    for v in m3:
                        m3_violations.append((path, v))
                if qb2:
                    for q in qb2:
                        qb2_violations.append((path, q))

    return file_count, c2_violations, m3_violations, qb2_violations

if __name__ == '__main__':
    target = r'HPKata'
    projects = [
        ('HPKata.Core', os.path.join(target, 'HPKata.Core'), False),
        ('HPKata.UI', os.path.join(target, 'HPKata.UI'), False),
        ('HPKata.AutoCad', os.path.join(target, 'HPKata.AutoCad'), False),
        ('HPKata.Revit', os.path.join(target, 'HPKata.Revit'), False),
        ('HPKata.Installer', os.path.join(target, 'HPKata.Installer'), False),
        ('HPKata.Core.Tests', os.path.join(target, 'HPKata.Core.Tests'), True),
        ('HPKata.UI.Tests', os.path.join(target, 'HPKata.UI.Tests'), True),
        ('HPKata.AutoCad.Tests', os.path.join(target, 'HPKata.AutoCad.Tests'), True),
        ('HPKata.Revit.Tests', os.path.join(target, 'HPKata.Revit.Tests'), True),
        ('HPKata.E2E.Tests', os.path.join(target, 'HPKata.E2E.Tests'), True),
    ]

    print("=== HPKata Code Audit ===")
    total_prod_files = 0
    total_test_files = 0

    for name, path, is_test in projects:
        count, c2, m3, qb2 = scan_project(path, is_test)
        if is_test:
            total_test_files += count
        else:
            total_prod_files += count
        print(f"\n--- {name} (Files: {count}, IsTest: {is_test}) ---")
        print(f"  C2 (>300 lines): {len(c2)}")
        for p, t in c2:
            print(f"    VIOLATION C2: {p} ({t} lines)")
        print(f"  M3 (>50 lines): {len(m3)}")
        for p, v in m3:
            print(f"    VIOLATION M3: {p}:{v[0]}-{v[1]} {v[2]} ({v[3]} lines)")
        print(f"  Q-B2 (empty catch): {len(qb2)}")
        for p, q in qb2:
            print(f"    VIOLATION Q-B2: {p}:{q[0]} -> {q[1]}")
