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

def check_external_reference():
    print("=== CHECK 1: C:\\Dce Pro Invariance ===")
    if not os.path.exists(DCE_PRO_DIR):
        print(f"ERROR: {DCE_PRO_DIR} does not exist!")
        return False

    recent_files = []
    cutoff = datetime.datetime.now() - datetime.timedelta(days=2) # last 48 hours
    total_files = 0

    for root, dirs, files in os.walk(DCE_PRO_DIR):
        for f in files:
            total_files += 1
            full_path = os.path.join(root, f)
            try:
                mtime = datetime.datetime.fromtimestamp(os.path.getmtime(full_path))
                if mtime > cutoff:
                    recent_files.append((full_path, mtime))
            except Exception as e:
                print(f"Warning reading {full_path}: {e}")

    print(f"Total files in C:\\Dce Pro: {total_files}")
    print(f"Files modified in last 48 hours: {len(recent_files)}")
    for rf, mtime in recent_files:
        print(f"  MODIFIED: {rf} at {mtime}")

    hash_matches = True
    for path, expected_hash in KNOWN_HASHES.items():
        if os.path.exists(path):
            actual_hash = sha256_file(path)
            if actual_hash == expected_hash.lower():
                print(f"  HASH MATCH OK: {os.path.basename(path)} -> {actual_hash[:16]}...")
            else:
                print(f"  HASH MISMATCH: {path}\n    Expected: {expected_hash}\n    Actual:   {actual_hash}")
                hash_matches = False
        else:
            print(f"  MISSING FILE: {path}")
            hash_matches = False

    return len(recent_files) == 0 and hash_matches

def check_clean_code():
    print("\n=== CHECK 2: Clean Code Governance in HPDcePro ===")
    c2_violations = [] # file length > 300
    m3_violations = [] # method length > 50
    qb1_violations = [] # commented-out code
    qb2_violations = [] # unannotated catch blocks

    total_cs_files = 0
    total_lines = 0

    # Common code comment patterns
    code_in_comment_pattern = re.compile(
        r'^\s*//\s*(?:(?:var|int|string|bool|double|float|if|while|for|foreach|return|throw|try|catch|using|public|private|protected|internal|class|interface|struct)\b|.*\b(?:=>|=|==|!=|\+=|-=|\.Add\(|\.Remove\(|\.Execute\(|\.Invoke\()\s*[\w\d"\']|.*;\s*$)',
        re.IGNORECASE
    )

    for root, dirs, files in os.walk(HPDCEPRO_DIR):
        # Skip bin and obj directories
        if any(part in ("bin", "obj", ".vs", ".git") for part in root.split(os.sep)):
            continue
        for f in files:
            if not f.endswith(".cs"):
                continue
            # Skip auto-generated files
            if f.endswith(".g.cs") or f.endswith(".AssemblyAttributes.cs"):
                continue

            total_cs_files += 1
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, HPDCEPRO_DIR)

            with open(full_path, "r", encoding="utf-8-sig", errors="replace") as file_obj:
                lines = file_obj.readlines()

            line_count = len(lines)
            total_lines += line_count

            # Rule C2: File length <= 300 lines
            if line_count > 300:
                c2_violations.append((rel_path, line_count))

            # Rule Q-B1 & Q-B2 & M3
            in_block_comment = False
            for line_idx, line in enumerate(lines):
                stripped = line.strip()

                # Check for block comments
                if "/*" in stripped and "*/" not in stripped:
                    in_block_comment = True
                    continue
                if in_block_comment:
                    if "*/" in stripped:
                        in_block_comment = False
                    continue

                # Check Q-B1: Commented-out code
                # Ignore doc comments, multi-version comments, rule citations, headers
                if stripped.startswith("//"):
                    comment_text = stripped[2:].strip()
                    # Exclude documentation, XML docs, notes, references
                    if not (stripped.startswith("///") or 
                            comment_text.startswith("Rule ") or 
                            comment_text.startswith("PCC-") or 
                            comment_text.startswith("Step ") or 
                            comment_text.startswith("Note:") or 
                            comment_text.startswith("TCVN") or
                            comment_text.startswith("Multi-version:") or
                            comment_text.startswith("TODO") or
                            comment_text.startswith("Calculat") or
                            comment_text.startswith("Check ") or
                            comment_text.startswith("AutoStr") or
                            comment_text.startswith("Phase ") or
                            comment_text.startswith("Reason:") or
                            comment_text.startswith("Expected") or
                            comment_text.startswith("Setup") or
                            comment_text.startswith("Assert") or
                            comment_text.startswith("---") or
                            comment_text.startswith("===")):
                        # Test if looks like active C# statement
                        if code_in_comment_pattern.match(stripped):
                            # Make sure it's not explanatory comment containing a code keyword like "return true if valid"
                            if not any(comment_text.lower().startswith(prefix) for prefix in [
                                "return ", "returns ", "using ", "for ", "while ", "if ", "var ", "int ", "bool ", "double "
                            ]):
                                qb1_violations.append((rel_path, line_idx + 1, stripped))

                # Check Q-B2: Catch blocks must have explicit reason comment
                # Look for 'catch'
                if re.search(r'\bcatch\b', stripped):
                    # Check if there is an open brace and whether the catch body or line has a comment
                    # Scan subsequent 3 lines for a comment or reason
                    has_comment = False
                    if "//" in stripped or "/*" in stripped:
                        has_comment = True
                    else:
                        for next_offset in range(1, 4):
                            if line_idx + next_offset < len(lines):
                                next_line = lines[line_idx + next_offset].strip()
                                if "//" in next_line or "/*" in next_line:
                                    has_comment = True
                                    break
                                if next_line.startswith("throw;") or next_line.startswith("throw "):
                                    # re-throwing does not swallow
                                    has_comment = True
                                    break
                                if next_line and not next_line.startswith("{") and not next_line.startswith("}"):
                                    # Some statement inside catch, check if reason comment is absent
                                    pass
                    if not has_comment:
                        # Inspect the full catch block
                        # Let's see if catch block is empty or unannotated
                        qb2_violations.append((rel_path, line_idx + 1, stripped))

            # Rule M3: Method length <= 50 lines
            # Rough parser for method declarations and their lengths
            depth = 0
            method_start = None
            method_name = ""
            for line_idx, line in enumerate(lines):
                stripped = line.strip()
                # Simple heuristic for method declaration: ends with ) or ) { or where clause
                # and contains access modifier or return type
                is_method_header = (
                    depth == 1 and
                    not stripped.startswith("//") and
                    not stripped.startswith("/*") and
                    not stripped.startswith("[") and
                    not stripped.startswith("namespace ") and
                    not stripped.startswith("class ") and
                    not stripped.startswith("interface ") and
                    not stripped.startswith("struct ") and
                    not stripped.startswith("record ") and
                    not stripped.startswith("enum ") and
                    ("(" in stripped and ")" in stripped) and
                    any(acc in stripped for acc in ["public ", "private ", "protected ", "internal ", "static ", "async "]) and
                    not stripped.endswith(";")
                )

                if is_method_header and method_start is None:
                    method_start = line_idx + 1
                    method_name = stripped.split("(")[0].split()[-1] if "(" in stripped else "Method"

                for char in line:
                    if char == '{':
                        depth += 1
                    elif char == '}':
                        depth -= 1
                        if depth == 1 and method_start is not None:
                            length = (line_idx + 1) - method_start + 1
                            if length > 50:
                                m3_violations.append((rel_path, method_start, method_name, length))
                            method_start = None
                        elif depth == 0:
                            method_start = None

    print(f"Total C# files scanned: {total_cs_files}")
    print(f"Total lines of code: {total_lines}")
    print(f"\nRule C2 (file length <= 300) violations: {len(c2_violations)}")
    for v in c2_violations:
        print(f"  VIOLATION C2: {v[0]} ({v[1]} lines)")

    print(f"\nRule M3 (method length <= 50) potential violations: {len(m3_violations)}")
    for v in m3_violations[:10]:
        print(f"  POTENTIAL M3: {v[0]}:{v[1]} in '{v[2]}' ({v[3]} lines)")
    if len(m3_violations) > 10:
        print(f"  ... and {len(m3_violations) - 10} more.")

    print(f"\nRule Q-B1 (commented-out code) potential violations: {len(qb1_violations)}")
    for v in qb1_violations[:10]:
        print(f"  POTENTIAL Q-B1: {v[0]}:{v[1]} -> {v[2]}")
    if len(qb1_violations) > 10:
        print(f"  ... and {len(qb1_violations) - 10} more.")

    print(f"\nRule Q-B2 (unannotated catch) potential violations: {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  POTENTIAL Q-B2: {v[0]}:{v[1]} -> {v[2]}")

    return {
        "total_files": total_cs_files,
        "total_lines": total_lines,
        "c2": c2_violations,
        "m3": m3_violations,
        "qb1": qb1_violations,
        "qb2": qb2_violations
    }

if __name__ == "__main__":
    ext_ok = check_external_reference()
    code_res = check_clean_code()
