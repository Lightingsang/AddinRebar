import os
import re

hpdynamo_dir = r"HPDynamo"

all_cs_files = []
for root, dirs, files in os.walk(hpdynamo_dir):
    if "bin" in dirs:
        dirs.remove("bin")
    if "obj" in dirs:
        dirs.remove("obj")
    for file in files:
        if file.endswith(".cs"):
            all_cs_files.append(os.path.join(root, file))

print(f"Total C# files found in HPDynamo (excluding bin/obj): {len(all_cs_files)}")

failed_c2 = []
failed_m3 = []
failed_qb1 = []
failed_qb2 = []

method_regex = re.compile(r'^\s*(?:public|private|protected|internal|static|async|sealed|\s)+[\w<>\[\],\s\?]+\s+(\w+)\s*\([^)]*\)\s*(?:where[^{]+)?\s*\{')

for cs_file in all_cs_files:
    with open(cs_file, "r", encoding="utf-8", errors="replace") as f:
        content = f.read()
    lines = content.splitlines()

    # C2: File <= 300 lines
    if len(lines) > 300:
        failed_c2.append((cs_file, len(lines)))

    # M3: Method <= 50 lines
    for i, line in enumerate(lines):
        m = method_regex.match(line)
        if m:
            method_name = m.group(1)
            open_braces = 0
            start_line = i + 1
            end_line = start_line
            for j in range(i, len(lines)):
                open_braces += lines[j].count('{')
                open_braces -= lines[j].count('}')
                if open_braces == 0 and '{' in ''.join(lines[i:j+1]):
                    end_line = j + 1
                    break
            span = end_line - start_line + 1
            if span > 50:
                failed_m3.append((cs_file, method_name, span, start_line, end_line))

    # Q-B2: Empty catch
    catch_matches = re.finditer(r'catch\s*(\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
    for m in catch_matches:
        body = m.group(2).strip()
        if not body:
            failed_qb2.append((cs_file, "Empty catch block"))

print("\n=== RESULTS FOR ALL HPDYNAMO C# FILES ===")
if failed_c2:
    print(f"FAILED C2 (>300 lines) in {len(failed_c2)} files:")
    for f, count in failed_c2:
        print(f"  {f}: {count} lines")
else:
    print("PASS C2: All C# files in HPDynamo are <= 300 lines.")

if failed_m3:
    print(f"FAILED M3 (>50 lines method) in {len(failed_m3)} methods:")
    for f, m, span, s, e in failed_m3:
        print(f"  {f} :: {m} ({s}-{e}) -> {span} lines")
else:
    print("PASS M3: All methods in HPDynamo C# files are <= 50 lines.")

if failed_qb2:
    print(f"FAILED Q-B2 (empty catch) in {len(failed_qb2)} occurrences:")
    for f, msg in failed_qb2:
        print(f"  {f} :: {msg}")
else:
    print("PASS Q-B2: No empty catch blocks in HPDynamo C# files.")
