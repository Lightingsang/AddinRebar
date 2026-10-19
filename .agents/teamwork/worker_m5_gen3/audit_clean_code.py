import os
import re

HPDCEPRO_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"

def audit():
    cs_files = []
    for root, dirs, files in os.walk(HPDCEPRO_DIR):
        if 'bin' in dirs: dirs.remove('bin')
        if 'obj' in dirs: dirs.remove('obj')
        if '.git' in dirs: dirs.remove('.git')
        for f in files:
            if f.endswith('.cs'):
                cs_files.append(os.path.join(root, f))

    print(f"Total .cs files found: {len(cs_files)}")

    c2_violations = []
    qb2_violations = []
    qb1_suspicious = []
    m3_violations = []

    for path in cs_files:
        rel_path = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()

        # Rule C2: <= 300 lines
        line_count = len(lines)
        if line_count > 300:
            c2_violations.append((rel_path, line_count))

        # Rule Q-B2: catch blocks must have `// Reason:`
        # Rule Q-B1: dead code / commented out code
        in_multiline_comment = False
        brace_depth = 0
        method_start = None
        method_name = ""

        for idx, line in enumerate(lines, 1):
            stripped = line.strip()

            # Q-B2 check
            if re.search(r'\bcatch\b', stripped):
                # Check if this line or next 3 lines contain `// Reason:`
                found_reason = False
                for lookahead in range(max(0, idx - 1), min(len(lines), idx + 3)):
                    if '// Reason:' in lines[lookahead] or '// Reason' in lines[lookahead]:
                        found_reason = True
                        break
                if not found_reason:
                    qb2_violations.append((rel_path, idx, stripped))

            # Q-B1 check: commented out statements like `// var ...;` or `// foo.Bar();`
            if stripped.startswith('//') and not stripped.startswith('///'):
                comment_content = stripped.lstrip('/ ').strip()
                # If comment ends with ';' and contains typical C# keywords/syntax
                if comment_content.endswith(';') and any(comment_content.startswith(kw) for kw in ['var ', 'int ', 'double ', 'string ', 'return ', 'if ', 'for ', 'foreach ']):
                    # Exclude doc examples
                    if 'e.g.' not in comment_content and 'example' not in comment_content.lower():
                        qb1_suspicious.append((rel_path, idx, stripped))

    print(f"Rule C2 (> 300 lines): {len(c2_violations)}")
    for v in c2_violations:
        print(f"  {v[0]}: {v[1]} lines")

    print(f"Rule Q-B2 (unannotated catch): {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

    print(f"Rule Q-B1 (commented out code): {len(qb1_suspicious)}")
    for v in qb1_suspicious:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

if __name__ == '__main__':
    audit()
