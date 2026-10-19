import os
import re
import sys

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

phase4_files = []
for root, dirs, files in os.walk(HPKATA_DIR):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in files:
        if f.endswith(".cs"):
            full = os.path.join(root, f)
            rel = os.path.relpath(full, HPKATA_DIR).replace("\\", "/")
            rel_lower = rel.lower()
            if any(k in rel_lower for k in [
                "foundation", "slab", "stair", "lintel", "sectioncut",
                "pile", "footing", "padfooting", "pilecap", "phase4"
            ]):
                phase4_files.append((full, rel))

print(f"Found {len(phase4_files)} Phase 4 files.")

# Check 1: File lines and method lines in Phase 4
long_methods = []
long_files = []

method_sig_pattern = re.compile(
    r"^\s*(?:public|private|protected|internal|static|virtual|override|async|sealed|\s)+\s+"
    r"(?:(?:[a-zA-Z0-9_<>,\[\]\?]+)\s+)+([a-zA-Z0-9_]+)\s*\([^;]*\)\s*(?:where\s+.*)?$"
)

for full, rel in phase4_files:
    with open(full, "r", encoding="utf-8-sig", errors="replace") as f:
        lines = f.readlines()
    
    if len(lines) > 300:
        long_files.append((rel, len(lines)))
        
    brace_depth = 0
    in_method = False
    m_name = ""
    m_start = 0
    m_brace_depth = 0
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if not in_method and brace_depth >= 1:
            match = method_sig_pattern.match(stripped)
            if match and not stripped.endswith(";"):
                pname = match.group(1)
                if pname not in ("class", "interface", "record", "struct", "enum", "namespace"):
                    m_name = pname
                    m_start = idx
        
        in_str = False
        in_char = False
        in_line_comment = False
        for c_idx, c in enumerate(line):
            if in_line_comment: break
            if c == '/' and c_idx + 1 < len(line) and line[c_idx+1] == '/':
                in_line_comment = True
                break
            if c == '"' and not in_char:
                if c_idx > 0 and line[c_idx-1] == '\\': pass
                else: in_str = not in_str
            elif c == "'" and not in_str:
                if c_idx > 0 and line[c_idx-1] == '\\': pass
                else: in_char = not in_char
            elif not in_str and not in_char:
                if c == '{':
                    brace_depth += 1
                    if m_name and not in_method:
                        in_method = True
                        m_brace_depth = brace_depth
                elif c == '}':
                    if in_method and brace_depth == m_brace_depth:
                        m_len = idx - m_start + 1
                        if m_len > 50:
                            long_methods.append((rel, m_name, m_start, idx, m_len))
                        in_method = False
                        m_name = ""
                    brace_depth -= 1

print("\n--- Long Files in Phase 4 (>300 lines) ---")
if long_files:
    for r, cnt in long_files:
        print(f"  {r}: {cnt} lines")
else:
    print("  ZERO files > 300 lines in Phase 4 (Rule C2 100% compliant)!")

print("\n--- Long Methods in Phase 4 (>50 lines) ---")
if long_methods:
    for r, name, st, end, cnt in long_methods:
        print(f"  {r}::{name} ({st}-{end}) -> {cnt} lines")
else:
    print("  ZERO methods > 50 lines in Phase 4 (Rule M3 100% compliant)!")

# Check 2: Facades, Stubs, NotImplemented, Dummy returns in Phase 4 production code
print("\n--- Facade / Stub / Simulation scan in Phase 4 production code ---")
suspicious = []
for full, rel in phase4_files:
    if "Tests" in rel: continue # skip test projects for this check
    with open(full, "r", encoding="utf-8-sig", errors="replace") as f:
        lines = f.readlines()
    for idx, line in enumerate(lines, 1):
        if re.search(r"\bNotImplementedException\b", line):
            suspicious.append((rel, idx, "NotImplementedException", line.strip()))
        if re.search(r"\b(mock|fake|dummy|stub|simulation)\b", line, re.IGNORECASE):
            suspicious.append((rel, idx, "Mock/Fake/Simulation term", line.strip()))
        if re.search(r"return\s+(true|false|null|0|\"\")\s*;\s*//\s*dummy", line, re.IGNORECASE):
            suspicious.append((rel, idx, "Dummy return", line.strip()))

if suspicious:
    print(f"Found {len(suspicious)} suspicious items in Phase 4 production code:")
    for r, idx, kind, l in suspicious:
        print(f"  {r}:{idx} [{kind}] -> {l}")
else:
    print("  ZERO facades, stubs, mocks, or dummy returns in Phase 4 production code!")

# Check 3: Truly empty catch blocks in all HPKata .cs files
print("\n--- Truly empty catch blocks scan across all HPKata ---")
all_cs = []
for root, dirs, files in os.walk(HPKATA_DIR):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in files:
        if f.endswith(".cs"):
            all_cs.append(os.path.join(root, f))

empty_catches = []
for f in all_cs:
    rel = os.path.relpath(f, HPKATA_DIR).replace("\\", "/")
    with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
        content = fp.read()
    # Match catch (...) { } where inside { } is only whitespace
    m = re.finditer(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}", content)
    for match in m:
        line_num = content[:match.start()].count("\n") + 1
        empty_catches.append((rel, line_num, match.group(0)))

if empty_catches:
    print(f"Found {len(empty_catches)} truly empty catch blocks:")
    for r, idx, code in empty_catches:
        print(f"  {r}:{idx} -> {code}")
else:
    print("  ZERO truly empty catch blocks found across HPKata!")
