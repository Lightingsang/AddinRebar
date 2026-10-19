import os
import re
import datetime

HPGRASSHOPPER_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper"
MCPSHARED_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared"

print("==================================================")
print("1. RULE C2 AUDIT: FILE LENGTHS IN HPGRASSHOPPER")
print("==================================================")
c2_violations = []
total_cs_files = 0
for root, _, files in os.walk(HPGRASSHOPPER_DIR):
    for f in files:
        if f.endswith(".cs") and not f.endswith(".g.cs") and not f.endswith(".AssemblyInfo.cs"):
            total_cs_files += 1
            fpath = os.path.join(root, f)
            with open(fpath, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
            line_count = len(lines)
            if line_count > 300:
                c2_violations.append((fpath, line_count))

print(f"Total C# source files checked: {total_cs_files}")
if c2_violations:
    print(f"C2 VIOLATIONS FOUND: {len(c2_violations)}")
    for fpath, lc in c2_violations:
        print(f"  VIOLATION: {os.path.relpath(fpath, HPGRASSHOPPER_DIR)} has {lc} lines (> 300)")
else:
    print("PASS: All C# files in HPGrasshopper are <= 300 lines.")

print("\n==================================================")
print("2. RULE M3 AUDIT: METHOD LENGTHS (AST / BRACE SPAN)")
print("==================================================")
def analyze_methods(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    total = len(lines)
    methods = []
    i = 0
    while i < total:
        line = lines[i].strip()
        if (not line or line.startswith("//") or line.startswith("/*") or line.startswith("*") or
            any(re.search(rf"\b{kw}\b", line) for kw in ["class", "interface", "struct", "record", "enum", "namespace"])):
            i += 1
            continue
            
        if "(" in line and not any(re.match(rf"^\b{kw}\b", line) for kw in ["if", "for", "foreach", "while", "switch", "using", "catch", "lock"]):
            sig_start = i
            sig_lines = []
            curr = i
            found_body = False
            is_expr = False
            has_semi = False
            while curr < total and curr - sig_start < 25:
                cl = lines[curr].strip()
                if "//" in cl:
                    cl = cl[:cl.index("//")].strip()
                sig_lines.append(cl)
                if "{" in cl:
                    found_body = True
                    break
                if "=>" in cl:
                    found_body = True
                    is_expr = True
                    break
                if ";" in cl:
                    has_semi = True
                    break
                curr += 1
                
            if found_body and not has_semi:
                sig_str = " ".join(sig_lines)
                if "(" in sig_str and ")" in sig_str:
                    body_start = curr
                    if is_expr:
                        end_line = body_start
                        while end_line < total and ";" not in lines[end_line]:
                            end_line += 1
                        span = end_line - sig_start + 1
                        methods.append((sig_start + 1, end_line + 1, span, sig_str.split("=>")[0].strip()))
                        i = end_line
                    else:
                        brace = 0
                        found_first = False
                        end_line = body_start
                        for scan in range(body_start, total):
                            clean = re.sub(r'//.*$', '', lines[scan])
                            clean = re.sub(r'"(?:\\.|[^"\\])*"', '', clean)
                            for ch in clean:
                                if ch == '{':
                                    brace += 1
                                    found_first = True
                                elif ch == '}':
                                    brace -= 1
                                    if found_first and brace == 0:
                                        end_line = scan
                                        break
                            if found_first and brace == 0:
                                break
                        span = end_line - sig_start + 1
                        methods.append((sig_start + 1, end_line + 1, span, sig_str.split("{")[0].strip()))
                        i = end_line
        i += 1
    return methods

m3_violations = []
near_limit = []
for root, _, files in os.walk(HPGRASSHOPPER_DIR):
    for f in files:
        if f.endswith(".cs") and not f.endswith(".g.cs") and not f.endswith(".AssemblyInfo.cs"):
            fpath = os.path.join(root, f)
            rel_path = os.path.relpath(fpath, HPGRASSHOPPER_DIR)
            res = analyze_methods(fpath)
            for start_l, end_l, span, sig in res:
                if span > 50:
                    m3_violations.append((rel_path, start_l, end_l, span, sig))
                elif span >= 45:
                    near_limit.append((rel_path, start_l, end_l, span, sig))

if m3_violations:
    print(f"M3 VIOLATIONS FOUND: {len(m3_violations)}")
    for rel, start, end, span, sig in m3_violations:
        print(f"  VIOLATION: {rel}:{start}-{end} ({span} lines) -> {sig}")
else:
    print(f"PASS: 0 methods > 50 lines across ALL {total_cs_files} files in HPGrasshopper.")

print(f"Near-limit methods (45-50 lines): {len(near_limit)}")
for rel, start, end, span, sig in near_limit:
    print(f"  Near limit: {rel}:{start}-{end} ({span} lines) -> {sig[:60]}")

print("\n==================================================")
print("3. RULE Q-B1 (COMMENTED CODE) & Q-B2 (EMPTY CATCH)")
print("==================================================")
# Check production files
prod_dirs = [
    os.path.join(HPGRASSHOPPER_DIR, "HPGrasshopper.McpBridge"),
    os.path.join(HPGRASSHOPPER_DIR, "HPGrasshopper.Mcp.Server"),
]
empty_catches = []
commented_code = []
for pdir in prod_dirs:
    for root, _, files in os.walk(pdir):
        for f in files:
            if f.endswith(".cs"):
                fpath = os.path.join(root, f)
                rel = os.path.relpath(fpath, HPGRASSHOPPER_DIR)
                with open(fpath, "r", encoding="utf-8") as fp:
                    content = fp.read()
                    lines = content.splitlines()
                # Empty catch
                catch_matches = re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{\s*\}', content)
                for m in catch_matches:
                    empty_catches.append((rel, m.group(0)))
                # Commented out code pattern
                for idx, line in enumerate(lines, 1):
                    s = line.strip()
                    if s.startswith("//") and any(s.endswith(term) for term in [";", "{", "}"]):
                        # exclude pure comments like // Multi-version: foo;
                        if not s.startswith("// Multi-version") and not s.startswith("// see:") and not s.startswith("// e.g."):
                            commented_code.append((rel, idx, s))

print(f"Empty catch blocks in production: {len(empty_catches)}")
for rel, m in empty_catches:
    print(f"  Empty catch: {rel} -> {m}")
print(f"Suspicious commented out code in production: {len(commented_code)}")
for rel, idx, s in commented_code:
    print(f"  Commented code: {rel}:{idx} -> {s}")

print("\n==================================================")
print("4. MCPSHARED ISOLATION CHECK")
print("==================================================")
cutoff = datetime.datetime(2026, 10, 12, 0, 0, 0, tzinfo=datetime.timezone.utc)
mcp_modified = []
for root, _, files in os.walk(MCPSHARED_DIR):
    for f in files:
        fpath = os.path.join(root, f)
        mtime = datetime.datetime.fromtimestamp(os.path.getmtime(fpath), tz=datetime.timezone.utc)
        if mtime > cutoff:
            mcp_modified.append((os.path.relpath(fpath, MCPSHARED_DIR), mtime))

print(f"Files modified in McpShared since 2026-10-12T00:00:00Z: {len(mcp_modified)}")
for f, mt in mcp_modified:
    print(f"  Modified: {f} at {mt}")

