import os
import re
import sys
import json

REPO_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
HPDYNAMO_DIR = os.path.join(REPO_ROOT, "HPDynamo")

def get_all_cs_files():
    prod_files = []
    test_files = []
    for root, dirs, files in os.walk(HPDYNAMO_DIR):
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                full_path = os.path.join(root, f)
                if "Tests" in full_path:
                    test_files.append(full_path)
                else:
                    prod_files.append(full_path)
    return prod_files, test_files

def check_file_line_counts(files):
    violations = []
    for f in files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        if len(lines) > 300:
            violations.append((f, len(lines)))
    return violations

def strip_strings_and_comments(content):
    # Strips comments and string literals to leave raw code structure.
    # Handles raw string literals, verbatim strings, regular strings, and comments.
    res = []
    i = 0
    n = len(content)
    while i < n:
        # Check raw string literal: """
        if content[i:i+3] == '"""':
            # find end """
            end = content.find('"""', i + 3)
            if end != -1:
                i = end + 3
                continue
            else:
                break
        # Verbatim string @"..."
        if content[i:i+2] == '@"' or content[i:i+3] == '$@"' or content[i:i+3] == '@$"':
            quote_start = content.find('"', i)
            j = quote_start + 1
            while j < n:
                if content[j] == '"':
                    if j + 1 < n and content[j+1] == '"':
                        j += 2
                        continue
                    else:
                        j += 1
                        break
                j += 1
            i = j
            continue
        # Regular string "..."
        if content[i] == '"':
            j = i + 1
            while j < n:
                if content[j] == '\\':
                    j += 2
                    continue
                if content[j] == '"':
                    j += 1
                    break
                j += 1
            i = j
            continue
        # Char literal '.'
        if content[i] == "'":
            j = i + 1
            while j < n:
                if content[j] == '\\':
                    j += 2
                    continue
                if content[j] == "'":
                    j += 1
                    break
                j += 1
            i = j
            continue
        # Single-line comment //
        if content[i:i+2] == '//':
            end = content.find('\n', i)
            if end != -1:
                res.append('\n')
                i = end + 1
                continue
            else:
                break
        # Multi-line comment /* ... */
        if content[i:i+2] == '/*':
            end = content.find('*/', i + 2)
            if end != -1:
                i = end + 2
                continue
            else:
                break
        res.append(content[i])
        i += 1
    return "".join(res)

def check_empty_catches(files):
    violations = []
    for f in files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            content = fp.read()
        # Find raw catch statements in file
        # We need to find real C# catch blocks, not string literals
        # Find all catch occurrences
        for m in re.finditer(r'\bcatch\s*(\([^\)]*\))?\s*\{', content):
            start = m.end() - 1
            # Check if this occurrence was inside a string
            prefix = content[:m.start()]
            # Quick parity check: if odd number of quotes on current line, might be in string
            last_nl = prefix.rfind('\n')
            line_prefix = prefix[last_nl+1:] if last_nl != -1 else prefix
            if line_prefix.count('"') % 2 == 1:
                continue
            
            # Find matching closing brace
            depth = 1
            j = start + 1
            body_chars = []
            while j < len(content) and depth > 0:
                if content[j] == '{':
                    depth += 1
                elif content[j] == '}':
                    depth -= 1
                if depth > 0:
                    body_chars.append(content[j])
                j += 1
            body = "".join(body_chars)
            # Check if body has code or comment
            has_comment = '//' in body or '/*' in body
            has_code = bool(re.sub(r'[\s;]', '', body))
            if not has_code and not has_comment:
                violations.append((f, f"catch {body}"))
    return violations

def check_commented_code(files):
    violations = []
    code_pattern = re.compile(r'^\s*//\s*(var\s+|int\s+|string\s+|Assert\.|if\s*\(|for\s*\(|while\s*\(|return\s+|new\s+[A-Z]|throw\s+new\s+)', re.IGNORECASE)
    for f in files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            for idx, line in enumerate(fp, 1):
                stripped = line.strip()
                if code_pattern.match(line):
                    if not stripped.startswith("///"):
                        violations.append((f, idx, stripped))
    return violations

def parse_methods_accurately(filepath):
    with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
        lines = content.splitlines(keepends=True)

    # Tokenize characters and track line numbers
    # Line indices for char offsets
    line_starts = [0]
    for idx, c in enumerate(content):
        if c == '\n':
            line_starts.append(idx + 1)

    def get_line_num(char_offset):
        # binary search or linear
        import bisect
        return bisect.bisect_right(line_starts, char_offset)

    methods = []
    # Find method signatures
    method_sig = re.compile(r'(?:^\s*(?:\[[^\]]+\]\s*)*\s*(?:public|private|internal|protected|static|async|\s+)+\s+(?:void|Task|Task<[A-Za-z0-9_,\s<>]+>|[A-Za-z0-9_<>\[\],\?]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+[^{]+)?)\{', re.MULTILINE)

    for m in method_sig.finditer(content):
        method_name = m.group(1)
        brace_pos = m.end() - 1
        start_char = m.start()
        start_line = get_line_num(start_char)

        # Find matching closing brace, skipping strings and comments
        depth = 1
        i = brace_pos + 1
        n = len(content)
        while i < n and depth > 0:
            if content[i:i+3] == '"""':
                end = content.find('"""', i + 3)
                i = end + 3 if end != -1 else n
                continue
            if content[i:i+2] == '@"' or content[i:i+3] == '$@"' or content[i:i+3] == '@$"':
                quote_start = content.find('"', i)
                j = quote_start + 1
                while j < n:
                    if content[j] == '"':
                        if j + 1 < n and content[j+1] == '"':
                            j += 2
                            continue
                        else:
                            j += 1
                            break
                    j += 1
                i = j
                continue
            if content[i] == '"':
                j = i + 1
                while j < n:
                    if content[j] == '\\':
                        j += 2
                        continue
                    if content[j] == '"':
                        j += 1
                        break
                    j += 1
                i = j
                continue
            if content[i:i+2] == '//':
                end = content.find('\n', i)
                i = end + 1 if end != -1 else n
                continue
            if content[i:i+2] == '/*':
                end = content.find('*/', i + 2)
                i = end + 2 if end != -1 else n
                continue
            if content[i] == '{':
                depth += 1
            elif content[i] == '}':
                depth -= 1
            i += 1

        end_line = get_line_num(i - 1)
        span = end_line - start_line + 1
        methods.append({
            "file": filepath,
            "name": method_name,
            "start_line": start_line,
            "end_line": end_line,
            "span": span
        })
    return methods

def main():
    print("=" * 80)
    print("ACCURATE HPDYNAMO FORENSIC AUDIT SCANNER")
    print("=" * 80)

    prod_files, test_files = get_all_cs_files()
    all_files = prod_files + test_files
    print(f"Total C# files: {len(all_files)} (Production: {len(prod_files)}, Test: {len(test_files)})")

    # 1. Rule C2
    c2_v = check_file_line_counts(all_files)
    print(f"\n[Check 1] Rule C2 (File <= 300 lines): {'FAIL (' + str(len(c2_v)) + ')' if c2_v else 'PASS (0 violations)'}")

    # 2. Rule Q-B1 (Empty catch blocks)
    qb1_v = check_empty_catches(all_files)
    print(f"[Check 2] Rule Q-B1 (0 Unannotated Empty Catches): {'FAIL (' + str(len(qb1_v)) + ')' if qb1_v else 'PASS (0 violations)'}")
    for f, c in qb1_v:
        print(f"  - {f}: {c}")

    # 3. Rule Q-B2 (Commented code)
    qb2_v = check_commented_code(all_files)
    print(f"[Check 3] Rule Q-B2 (0 Commented Code Statements): {'FAIL (' + str(len(qb2_v)) + ')' if qb2_v else 'PASS (0 violations)'}")

    # 4. Method spans in Production Code
    prod_methods = []
    for f in prod_files:
        prod_methods.extend(parse_methods_accurately(f))
    prod_m3_v = [m for m in prod_methods if m["span"] > 50 and not (m["name"] == "ExecuteCore" and m["span"] <= 100)]
    print(f"[Check 4] Production Method Lengths (M3 <= 50 lines / justified <= 100): {'FAIL (' + str(len(prod_m3_v)) + ')' if prod_m3_v else 'PASS (0 violations)'}")
    for m in prod_m3_v:
        print(f"  - {os.path.basename(m['file'])}::{m['name']}: {m['span']} lines (L{m['start_line']}-L{m['end_line']})")

    # 5. Method spans in Test Code
    test_methods = []
    for f in test_files:
        test_methods.extend(parse_methods_accurately(f))
    # In test code, test methods or test helpers > 50 lines:
    test_m3_v = [m for m in test_methods if m["span"] > 50]
    print(f"[Check 5] Test Method Lengths: {len(test_m3_v)} methods > 50 lines")
    for m in test_m3_v:
        print(f"  - {os.path.basename(m['file'])}::{m['name']}: {m['span']} lines (L{m['start_line']}-L{m['end_line']})")

    # 6. Embedded Seeds Check
    seed_files = [f for f in prod_files if "SeedLibrary" in f and f.endswith("code.cs")]
    print(f"\n[Check 6] Embedded Seeds Audit ({len(seed_files)} seeds):")
    seed_issues = []
    for sf in seed_files:
        with open(sf, "r", encoding="utf-8") as fp:
            s_lines = fp.readlines()
        s_count = len(s_lines)
        # Check forbidden namespaces
        s_content = "".join(s_lines)
        forbidden = []
        if re.search(r'\bSystem\.IO\b', s_content) and not re.search(r'//.*System\.IO', s_content):
            forbidden.append("System.IO")
        if re.search(r'\bSystem\.Net\b', s_content):
            forbidden.append("System.Net")
        if re.search(r'\bSystem\.Diagnostics\.Process\b', s_content):
            forbidden.append("Process")
        if s_count > 300 or forbidden:
            seed_issues.append((sf, s_count, forbidden))
        print(f"  - {os.path.basename(os.path.dirname(sf))}: {s_count} lines, forbidden: {forbidden or 'NONE'}")

    clean = (len(c2_v) == 0 and len(qb1_v) == 0 and len(qb2_v) == 0 and len(prod_m3_v) == 0 and len(seed_issues) == 0)
    print("\n" + "=" * 80)
    print(f"STRICT PRODUCTION & SEEDS AUDIT VERDICT: {'CLEAN' if clean else 'INTEGRITY VIOLATION'}")
    print("=" * 80)

if __name__ == "__main__":
    main()
