import os
import sys
import hashlib
import datetime
import re

DCE_PRO_DIR = r"C:\Dce Pro"
HPDCEPRO_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"

KNOWN_HASHES = {
    os.path.join(DCE_PRO_DIR, "DCE Pro.dll"): "8f7b56d1f916351fb91a8464628471499f59006ac3ba5e362b9df8dbc1225780",
    os.path.join(DCE_PRO_DIR, "MayHoc", "mauhoc.db"): "74eb37741c999c677df271a6c8007d2d09b704fb16c7aff884f4f11ab624e1a8",
}

def sha256_file(filepath):
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().lower()

def audit_phase1_external():
    print("=" * 60)
    print("PHASE 1: EXTERNAL TARGET VERIFICATION (C:\\Dce Pro)")
    print("=" * 60)

    if not os.path.exists(DCE_PRO_DIR):
        print(f"FAIL: {DCE_PRO_DIR} does not exist!")
        return False, ["C:\\Dce Pro does not exist"]

    total_files = 0
    modified_files = []
    # Wave 2 dispatch started after 2026-10-17 22:00 UTC
    cutoff_time = datetime.datetime(2026, 10, 17, 22, 0, 0)

    for root, dirs, files in os.walk(DCE_PRO_DIR):
        for f in files:
            total_files += 1
            full_path = os.path.join(root, f)
            try:
                mtime = datetime.datetime.fromtimestamp(os.path.getmtime(full_path))
                ctime = datetime.datetime.fromtimestamp(os.path.getctime(full_path))
                if mtime > cutoff_time or ctime > cutoff_time:
                    modified_files.append((full_path, mtime, ctime))
            except Exception as ex:
                print(f"Error accessing {full_path}: {ex}")

    print(f"Total files in C:\\Dce Pro: {total_files}")
    print(f"Files modified/created after {cutoff_time}: {len(modified_files)}")
    for mf, mt, ct in modified_files:
        print(f"  VIOLATION: {mf} modified at {mt}, created at {ct}")

    hash_failures = []
    for path, expected_hash in KNOWN_HASHES.items():
        if not os.path.exists(path):
            print(f"  FAIL: Expected file not found: {path}")
            hash_failures.append(f"Missing file: {path}")
            continue
        actual_hash = sha256_file(path)
        if actual_hash == expected_hash.lower():
            print(f"  PASS: {os.path.basename(path)} sha256={actual_hash[:16]}... matched expected")
        else:
            print(f"  FAIL: Hash mismatch for {path}\n    Expected: {expected_hash}\n    Actual:   {actual_hash}")
            hash_failures.append(f"Hash mismatch {path}")

    passed = (len(modified_files) == 0) and (len(hash_failures) == 0)
    return passed, modified_files + hash_failures

def find_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(HPDCEPRO_DIR):
        if any(part in ("bin", "obj", ".vs", ".git") for part in root.split(os.sep)):
            continue
        for f in files:
            if f.endswith(".cs") and not f.endswith(".g.cs") and not f.endswith(".AssemblyAttributes.cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def parse_methods_lines(lines):
    # Accurate brace counter to find method blocks
    methods = []
    # State tracking
    in_block_comment = False
    brace_depth = 0
    method_start_line = None
    method_name = ""
    method_brace_depth = 0

    method_sig_pattern = re.compile(
        r'^\s*(?:(?:public|private|protected|internal|static|virtual|override|async|sealed)\s+)+[\w\<\>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*$'
    )

    for idx, line in enumerate(lines):
        line_num = idx + 1
        stripped = line.strip()

        # Handle comments
        if "/*" in stripped:
            in_block_comment = True
        if in_block_comment:
            if "*/" in stripped:
                in_block_comment = False
            continue
        if stripped.startswith("//"):
            continue

        # Check for method header candidate when not inside another method
        if method_start_line is None:
            # Check if line looks like method signature or starts one
            if ("(" in stripped and ")" in stripped) and not stripped.endswith(";"):
                if any(kw in stripped for kw in ["public ", "private ", "protected ", "internal "]):
                    if not any(kw in stripped for kw in ["class ", "interface ", "struct ", "enum ", "record ", "namespace ", "delegate ", "event "]):
                        method_start_line = line_num
                        method_name = stripped
                        method_brace_depth = brace_depth

        # Count braces
        open_count = stripped.count("{")
        close_count = stripped.count("}")

        brace_depth += (open_count - close_count)

        if method_start_line is not None:
            # If we opened a brace and now we returned to method_brace_depth
            if open_count > 0:
                pass
            if brace_depth <= method_brace_depth and "}" in stripped:
                length = line_num - method_start_line + 1
                methods.append((method_name, method_start_line, line_num, length))
                method_start_line = None
                method_name = ""

    return methods

def audit_phase2_clean_code_and_anticheat():
    print("\n" + "=" * 60)
    print("PHASE 2: CHEATING & CLEAN CODE FORENSIC VERIFICATION")
    print("=" * 60)

    cs_files = find_cs_files()
    print(f"Total C# files in HPDcePro: {len(cs_files)}")

    c2_violations = [] # file length > 300
    m3_violations = [] # method length > 50
    qb1_violations = [] # commented out code
    qb2_violations = [] # empty / unannotated catch
    anticheat_violations = [] # NotImplementedException, fake returns

    code_like_comment = re.compile(
        r'^\s*//\s*(?:(?:var|int|string|bool|double|float|if|while|for|foreach|return|throw|using)\s+[A-Za-z0-9_]|.*\b[A-Za-z0-9_]+\s*=\s*[^=;]+;|\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\([^\)]*\)\s*;)',
        re.IGNORECASE
    )

    for full_path in cs_files:
        rel_path = os.path.relpath(full_path, HPDCEPRO_DIR)
        with open(full_path, "r", encoding="utf-8-sig", errors="replace") as f:
            lines = f.readlines()

        total_lines = len(lines)
        # Check C2
        if total_lines > 300:
            c2_violations.append((rel_path, total_lines))

        # Check Q-B1, Q-B2, Anti-cheat
        is_test_project = ("Tests" in rel_path)

        for line_idx, line in enumerate(lines):
            line_num = line_idx + 1
            s = line.strip()

            # Anti-cheat: Check for NotImplementedException or fake bypasses
            if not is_test_project:
                if "throw new NotImplementedException" in s or "throw new NotImplemented" in s:
                    anticheat_violations.append((rel_path, line_num, "NotImplementedException in production code"))

            # Q-B1: Commented out code
            if s.startswith("//") and not s.startswith("///"):
                comment_text = s[2:].strip()
                # Exclude annotations, documentation comments, headers
                exemptions = [
                    "Rule ", "PCC-", "Step ", "Note", "TCVN", "Multi-version", "TODO", "Phase ", 
                    "Calculate", "Check", "AutoStr", "Reason:", "Expected", "Setup", "Assert",
                    "===", "---", "1.", "2.", "3.", "4.", "5.", "6.", "7.", "8.", "9.", "10."
                ]
                if not any(comment_text.startswith(ex) for ex in exemptions):
                    if code_like_comment.match(s):
                        # Ensure it's not a natural language explanation
                        words = comment_text.split()
                        if len(words) > 0 and words[0].lower() not in ["for", "if", "while", "return", "using", "see", "in", "with", "when", "by", "as", "to"]:
                            qb1_violations.append((rel_path, line_num, s))

            # Q-B2: Catch blocks
            if re.search(r'\bcatch\b', s):
                # Check for reason comment
                has_comment = ("//" in s or "/*" in s)
                if not has_comment:
                    # Check next 3 lines
                    for offset in range(1, 4):
                        if line_idx + offset < len(lines):
                            nxt = lines[line_idx + offset].strip()
                            if "//" in nxt or "/*" in nxt:
                                has_comment = True
                                break
                            if nxt.startswith("throw;") or nxt.startswith("throw "):
                                has_comment = True
                                break
                    if not has_comment:
                        qb2_violations.append((rel_path, line_num, s))

        # Check M3 (Method length <= 50)
        methods = parse_methods_lines(lines)
        for mname, start_l, end_l, length in methods:
            if length > 50:
                m3_violations.append((rel_path, mname[:40], start_l, end_l, length))

    print(f"\nRule C2 Violations (File length > 300 lines): {len(c2_violations)}")
    for f, count in c2_violations:
        print(f"  VIOLATION: {f} has {count} lines")

    print(f"\nRule M3 Violations (Method length > 50 lines): {len(m3_violations)}")
    for f, mname, s_l, e_l, length in m3_violations:
        print(f"  VIOLATION: {f}:{s_l}-{e_l} ({length} lines) -> {mname}")

    print(f"\nRule Q-B1 Violations (Commented-out code): {len(qb1_violations)}")
    for f, l, text in qb1_violations[:10]:
        print(f"  VIOLATION: {f}:{l} -> {text}")

    print(f"\nRule Q-B2 Violations (Unannotated catch blocks): {len(qb2_violations)}")
    for f, l, text in qb2_violations:
        print(f"  VIOLATION: {f}:{l} -> {text}")

    print(f"\nAnti-Cheat Violations: {len(anticheat_violations)}")
    for f, l, text in anticheat_violations:
        print(f"  VIOLATION: {f}:{l} -> {text}")

    clean_code_passed = (
        len(c2_violations) == 0 and
        len(m3_violations) == 0 and
        len(qb1_violations) == 0 and
        len(qb2_violations) == 0 and
        len(anticheat_violations) == 0
    )

    return clean_code_passed, {
        "c2": c2_violations,
        "m3": m3_violations,
        "qb1": qb1_violations,
        "qb2": qb2_violations,
        "anticheat": anticheat_violations
    }

if __name__ == "__main__":
    p1_pass, p1_issues = audit_phase1_external()
    p2_pass, p2_issues = audit_phase2_clean_code_and_anticheat()

    print("\n" + "=" * 60)
    print("SUMMARY:")
    print(f"Phase 1: {'PASS' if p1_pass else 'FAIL'}")
    print(f"Phase 2: {'PASS' if p2_pass else 'FAIL'}")
    print("=" * 60)
    if not (p1_pass and p2_pass):
        sys.exit(1)
    sys.exit(0)
