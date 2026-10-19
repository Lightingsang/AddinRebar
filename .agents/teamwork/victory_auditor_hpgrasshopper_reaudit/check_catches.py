import os, re

cs_files = []
for r, d, files in os.walk('HPGrasshopper'):
    if 'bin' in r.split(os.sep) or 'obj' in r.split(os.sep):
        continue
    for f in files:
        if f.endswith('.cs'):
            cs_files.append(os.path.join(r, f))

print(f"Checking all catch blocks across {len(cs_files)} files...")
empty_catches = []
annotated_empty_catches = []
executable_catches = []

for fp in cs_files:
    with open(fp, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Strip string literals carefully using state machine
    clean = []
    i = 0
    n = len(content)
    while i < n:
        if content[i:i+2] == '@"' or content[i:i+3] == '$@"' or content[i:i+3] == '@$"':
            # Verbatim string
            if content[i:i+3] in ('$@"', '@$"'):
                i += 3
            else:
                i += 2
            clean.append('""')
            while i < n:
                if content[i] == '"':
                    if i + 1 < n and content[i+1] == '"':
                        i += 2
                    else:
                        i += 1
                        break
                else:
                    i += 1
        elif content[i] == '"' or (content[i] == '$' and i + 1 < n and content[i+1] == '"'):
            # Regular or interpolated string
            if content[i] == '$':
                i += 2
            else:
                i += 1
            clean.append('""')
            escaped = False
            while i < n:
                if escaped:
                    escaped = False
                    i += 1
                elif content[i] == '\\':
                    escaped = True
                    i += 1
                elif content[i] == '"':
                    i += 1
                    break
                else:
                    i += 1
        else:
            clean.append(content[i])
            i += 1

    clean_content = "".join(clean)

    # Now find catch blocks in clean_content
    # Matches: catch (...) { ... } or catch { ... }
    # To properly handle nested braces in catch body, find 'catch' keyword then find opening '{' and matching '}'
    pos = 0
    while True:
        m = re.search(r'\bcatch\b(?:\s*\([^)]*\))?\s*\{', clean_content[pos:])
        if not m:
            break
        catch_start = pos + m.start()
        open_brace = pos + m.end() - 1
        
        # Match braces
        depth = 1
        curr = open_brace + 1
        while curr < len(clean_content) and depth > 0:
            if clean_content[curr] == '{':
                depth += 1
            elif clean_content[curr] == '}':
                depth -= 1
            curr += 1
        
        close_brace = curr - 1
        body = clean_content[open_brace+1:close_brace]
        
        # Check executable code
        no_comm = re.sub(r'//.*', '', body)
        no_comm = re.sub(r'/\*.*?\*/', '', no_comm, flags=re.DOTALL).strip()
        line_no = clean_content[:catch_start].count('\n') + 1

        if not no_comm:
            # Body has no executable code
            has_comment = ('//' in body) or ('/*' in body)
            if has_comment:
                annotated_empty_catches.append((fp, line_no, body.strip()))
            else:
                empty_catches.append((fp, line_no, clean_content[catch_start:close_brace+1]))
        else:
            executable_catches.append((fp, line_no))
            
        pos = close_brace + 1

print(f"Total catch blocks: {len(executable_catches) + len(annotated_empty_catches) + len(empty_catches)}")
print(f"Executable catch blocks: {len(executable_catches)}")
print(f"Annotated empty catch blocks: {len(annotated_empty_catches)}")
for fp, l, b in annotated_empty_catches:
    print(f"  [OK-Annotated] {fp}:{l} -> {repr(b)}")
print(f"Unannotated empty catch blocks (VIOLATIONS): {len(empty_catches)}")
for fp, l, snip in empty_catches:
    print(f"  [VIOLATION Q-B2] {fp}:{l} -> {repr(snip)}")
