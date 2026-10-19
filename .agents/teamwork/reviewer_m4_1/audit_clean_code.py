import os
import re

ui_dir = os.path.abspath("HPKata/HPKata.UI")
tests_dir = os.path.abspath("HPKata/HPKata.UI.Tests")

cs_files = []
for base in [ui_dir, tests_dir]:
    for root, dirs, files in os.walk(base):
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))

# Check Rule M3 (<= 50 physical lines per method)
# A method in C# can be approximated by detecting signature and matching braces { ... }
method_sig_pattern = re.compile(
    r'^\s*(?:(?:public|private|protected|internal|static|override|virtual|async|sealed|partial)\s+)+[^\s=;(]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
)

m3_violations = []
q_b1_suspects = []
all_catches = []

for path in sorted(cs_files):
    rel_path = os.path.relpath(path, os.path.abspath("."))
    with open(path, "r", encoding="utf-8") as fp:
        lines = fp.readlines()

    # Check for commented-out code (Rule Q-B1)
    # Looking for lines starting with // that look like code statements (ending in ;, containing var, new, if, return, etc.)
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//"):
            comment_content = stripped[2:].strip()
            # If it looks like code: e.g. var x = ...; or methodCall(); or return ...;
            if re.match(r'^(?:var\s+[a-zA-Z0-9_]+\s*=|if\s*\(|return\s+|[a-zA-Z0-9_]+\.[a-zA-Z0-9_]+\(|new\s+[a-zA-Z0-9_]+|[a-zA-Z0-9_]+\s*=\s*).*;$', comment_content):
                q_b1_suspects.append((rel_path, idx, line.strip()))

    # Check catch blocks
    for idx, line in enumerate(lines, 1):
        if "catch" in line:
            # check next 5 lines
            snippet = "".join(lines[idx-1:min(len(lines), idx+5)])
            all_catches.append((rel_path, idx, snippet))

    # Parse method boundaries using brace depth
    in_method = False
    method_name = ""
    method_start = 0
    brace_depth = 0
    start_depth = 0

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        # Check if line or preceding lines match method signature
        if not in_method and ("(" in line and ")" in line and not line.strip().startswith("//") and not line.strip().startswith("*")):
            # Check if it looks like a method
            match = method_sig_pattern.match(stripped)
            if not match:
                # Could be multi-line signature or attributes
                # Also check common patterns like `public void Foo()`
                m = re.search(r'(?:public|private|protected|internal)\s+(?:async\s+)?(?:static\s+)?[A-Za-z0-9_<>,\[\]\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)', stripped)
                if m and not stripped.endswith(";") and not "class " in stripped and not "interface " in stripped:
                    match = m
            
            if match:
                # Verify if next line or this line has '{'
                has_brace = "{" in stripped
                if not has_brace and i + 1 < len(lines) and "{" in lines[i+1]:
                    has_brace = True
                if has_brace:
                    in_method = True
                    method_name = match.group(1)
                    method_start = i + 1
                    start_depth = brace_depth

        # Update brace depth
        # ignore braces in strings/comments for simple counting
        clean_line = re.sub(r'//.*$', '', stripped)
        clean_line = re.sub(r'"(?:\\.|[^"\\])*"', '', clean_line)
        open_braces = clean_line.count("{")
        close_braces = clean_line.count("}")

        brace_depth += open_braces - close_braces

        if in_method and brace_depth <= start_depth and open_braces != 0 or (in_method and brace_depth == start_depth and close_braces > 0):
            method_end = i + 1
            length = method_end - method_start + 1
            if length > 50:
                m3_violations.append((rel_path, method_name, method_start, method_end, length))
            in_method = False
            method_name = ""

        i += 1

print(f"Rule M3 (>50 lines per method) potential violations: {len(m3_violations)}")
for rel_path, name, start, end, length in m3_violations:
    print(f"  {rel_path}:{start}-{end} {name} ({length} lines)")

print(f"\nRule Q-B1 (Commented-out code suspects): {len(q_b1_suspects)}")
for rel_path, idx, text in q_b1_suspects:
    print(f"  {rel_path}:{idx}: {text}")

print(f"\nCatch blocks found: {len(all_catches)}")
for rel_path, idx, snip in all_catches:
    print(f"  {rel_path}:{idx}")
    print("    " + "\n    ".join(snip.splitlines()[:3]))
