import os
import re
import sys

# Force UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

def audit_solution(root_dir):
    c2_violations = []
    m3_violations_prod = []
    m3_violations_test = []
    qb2_violations = []
    all_catches = []
    qb1_candidates = []
    kata_pro_matches = []

    code_in_comment_pattern = re.compile(r'^\s*//\s*(?:(?:var|int|string|bool|double|float)\s+\w+\s*=|return\s+[^;]+;|\w+\.\w+\([^)]*\);|if\s*\(.*\)\s*\{?|for\s*\(.*\)\s*\{?|while\s*\(.*\)\s*\{?|throw\s+new\s+\w+)')

    for root, dirs, files in os.walk(root_dir):
        for b in ['bin', 'obj', '.vs', '.git']:
            if b in dirs:
                dirs.remove(b)

        is_test_dir = 'Test' in root or 'Tests' in root

        for file in files:
            if not file.endswith('.cs'):
                continue

            filepath = os.path.join(root, file)
            relpath = os.path.relpath(filepath, root_dir)

            with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                lines = content.splitlines()

            # Rule C2
            if len(lines) > 300:
                c2_violations.append((relpath, len(lines)))

            # Catch blocks analysis
            # Look for catch blocks
            catch_matches = re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
            for cm in catch_matches:
                body = cm.group(1).strip()
                start_char = cm.start()
                lineno = content[:start_char].count('\n') + 1
                
                # Check if catch has statements
                # Strip comments from body first to see if there are statements
                body_no_comments = re.sub(r'//.*', '', body)
                body_no_comments = re.sub(r'/\*.*?\*/', '', body_no_comments, flags=re.DOTALL).strip()
                
                has_statements = len(body_no_comments) > 0
                has_comment = '//' in body or '/*' in body
                
                all_catches.append((relpath, lineno, has_statements, has_comment, cm.group(0).replace('\n', ' ')))

                # Q-B2: a catch body with no statement and no comment (i.e. empty catch)
                if not has_statements and not has_comment:
                    qb2_violations.append((relpath, lineno, cm.group(0).replace('\n', ' ')))

            # Rule Q-B1
            for idx, line in enumerate(lines, 1):
                s = line.strip()
                if s.startswith('///'):
                    continue
                if 'e.g.' in s.lower() or 'example' in s.lower() or 'todo' in s.lower() or 'such as' in s.lower():
                    continue
                if code_in_comment_pattern.search(line):
                    qb1_candidates.append((relpath, idx, s))

            # kata_pro
            for idx, line in enumerate(lines, 1):
                if 'kata_pro' in line.lower():
                    if any(k in line.lower() for k in ['doesnotcontain', 'legacybaseline', 'detected', 'assert', 'purge', 'audit', 'occurrences']):
                        continue
                    kata_pro_matches.append((relpath, idx, line.strip()))

            # Rule M3
            in_method = False
            method_name = ''
            start_line = 0
            brace_depth = 0
            class_brace_depth = 0
            started_braces = False

            for idx, line in enumerate(lines, 1):
                stripped = line.strip()
                clean = re.sub(r'//.*', '', stripped)
                clean = re.sub(r'"[^"]*"', '', clean)

                open_b = clean.count('{')
                close_b = clean.count('}')

                if class_brace_depth >= 1 and not in_method:
                    m = re.match(r'(?:public|private|protected|internal|static|async|override|sealed|virtual|\s)+\s+[\w<>\[\]\?,\s]+\s+(\w+)\s*\([^;]*\)', stripped)
                    if m and not stripped.endswith(';') and not stripped.startswith('public event ') and not stripped.startswith('public interface'):
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
                            if is_test_dir:
                                m3_violations_test.append((relpath, start_line, idx, method_name, length))
                            else:
                                m3_violations_prod.append((relpath, start_line, idx, method_name, length))
                        in_method = False
                        started_braces = False
                else:
                    class_brace_depth += open_b - close_b

    print("=== FORENSIC AUDIT RESULTS ===")
    print(f"Rule C2 (>300 lines) Violations: {len(c2_violations)}")
    for v in c2_violations:
        print(f"  [C2] {v[0]}: {v[1]} lines")

    print(f"\nRule M3 (>50 lines) Production Violations: {len(m3_violations_prod)}")
    for v in m3_violations_prod:
        print(f"  [M3 PROD] {v[0]}:{v[1]}-{v[2]} {v[3]} ({v[4]} lines)")

    print(f"\nRule M3 (>50 lines) Test Methods: {len(m3_violations_test)}")
    for v in m3_violations_test:
        print(f"  [M3 TEST] {v[0]}:{v[1]}-{v[2]} {v[3]} ({v[4]} lines)")

    print(f"\nRule Q-B2 (Unannotated Empty Catch) Violations: {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  [Q-B2] {v[0]}:{v[1]} -> {v[2]}")

    print(f"\nTotal Catch Blocks in solution: {len(all_catches)}")
    empty_catches_with_comments = [c for c in all_catches if not c[2] and c[3]]
    print(f"Empty Catch Blocks with Justification Comments: {len(empty_catches_with_comments)}")
    for c in empty_catches_with_comments:
        print(f"  [COMMENTED CATCH] {c[0]}:{c[1]} -> {c[4]}")

    print(f"\nRule Q-B1 (Commented-out code) Candidates: {len(qb1_candidates)}")
    for v in qb1_candidates:
        print(f"  [Q-B1] {v[0]}:{v[1]} -> {v[2]}")

    print(f"\nkata_pro Occurrences (non-assertion): {len(kata_pro_matches)}")
    for v in kata_pro_matches:
        print(f"  [KATA_PRO] {v[0]}:{v[1]} -> {v[2]}")

if __name__ == '__main__':
    audit_solution('HPKata')
