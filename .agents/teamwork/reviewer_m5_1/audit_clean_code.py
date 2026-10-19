import os
import re

autocad_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad"
tests_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad.Tests"

def get_cs_files():
    cs_files = []
    for root_dir in [autocad_dir, tests_dir]:
        for root, dirs, files in os.walk(root_dir):
            if "bin" in dirs:
                dirs.remove("bin")
            if "obj" in dirs:
                dirs.remove("obj")
            for f in files:
                if f.endswith(".cs"):
                    cs_files.append(os.path.join(root, f))
    return cs_files

def check_methods(cs_files):
    # Method detection heuristic: find method definitions and match braces
    long_methods = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            # check if line looks like method signature
            if any(k in line for k in ["public ", "private ", "protected ", "internal ", "static "]) and "(" in line and not line.startswith("//") and not line.startswith("/*") and not ";" in line:
                if not any(x in line for x in ["class ", "interface ", "struct ", "record ", "namespace ", "enum ", "event ", "delegate "]):
                    sig = line
                    start_line = i + 1
                    # find opening brace
                    brace_count = 0
                    found_open = False
                    j = i
                    while j < len(lines):
                        for char in lines[j]:
                            if char == '{':
                                brace_count += 1
                                found_open = True
                            elif char == '}':
                                brace_count -= 1
                        if found_open and brace_count == 0:
                            end_line = j + 1
                            method_len = end_line - start_line + 1
                            if method_len > 50:
                                long_methods.append((f, sig, start_line, end_line, method_len))
                            i = j
                            break
                        j += 1
            i += 1
    return long_methods

def check_catches(cs_files):
    empty_catches = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            content = fp.read()
        
        # search for catch (...) { ... }
        matches = re.finditer(r'catch\s*(\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
        for m in matches:
            body = m.group(2).strip()
            if not body:
                empty_catches.append((f, m.start(), "completely empty body"))
            elif body:
                # check if body has only whitespace or no comment/statement
                has_comment = "//" in body or "/*" in body
                has_code = any(c in body for c in [";", "(", ")"])
                if not has_comment and not has_code:
                    empty_catches.append((f, m.start(), f"body without comment or statement: {body}"))
    return empty_catches

def check_commented_code(cs_files):
    commented_code = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8") as fp:
            lines = fp.readlines()
        for idx, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith("//"):
                comment = stripped[2:].strip()
                # Check if comment looks like C# code
                if re.match(r'^(var|int|string|bool|double|if|for|foreach|while|return|tr\.|doc\.|ed\.|pl\.)\s*[a-zA-Z0-9_\.]+\s*(=|\(|\+\+|--|;)', comment):
                    commented_code.append((f, idx + 1, stripped))
    return commented_code

files = get_cs_files()
print(f"Total C# files found: {len(files)}")

long_m = check_methods(files)
print(f"Methods > 50 lines: {len(long_m)}")
for lm in long_m:
    rel = os.path.relpath(lm[0], r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
    print(f"  {rel}:{lm[2]}-{lm[3]} ({lm[4]} lines) -> {lm[1]}")

empty_c = check_catches(files)
print(f"Empty catches: {len(empty_c)}")
for ec in empty_c:
    rel = os.path.relpath(ec[0], r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
    print(f"  {rel} -> {ec[2]}")

comm_c = check_commented_code(files)
print(f"Commented code lines: {len(comm_c)}")
for cc in comm_c:
    rel = os.path.relpath(cc[0], r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
    print(f"  {rel}:{cc[1]} -> {cc[2]}")
