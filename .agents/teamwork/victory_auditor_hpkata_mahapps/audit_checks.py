import os
import re

hpkata_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

cs_files = []
for root, dirs, files in os.walk(hpkata_dir):
    if "bin" in dirs:
        dirs.remove("bin")
    if "obj" in dirs:
        dirs.remove("obj")
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

# Rule M3: Every method <= 50 lines
# Let's parse C# methods accurately
def strip_comments_and_strings(code):
    # Replace comments and strings with placeholders preserving line count
    result = []
    in_block_comment = False
    for line in code.splitlines():
        cleaned = ""
        i = 0
        in_string = False
        str_char = None
        while i < len(line):
            if in_block_comment:
                if line[i:i+2] == "*/":
                    in_block_comment = False
                    i += 2
                else:
                    i += 1
            elif in_string:
                if line[i] == "\\":
                    i += 2
                elif line[i] == str_char:
                    in_string = False
                    i += 1
                else:
                    i += 1
            else:
                if line[i:i+2] == "/*":
                    in_block_comment = True
                    i += 2
                elif line[i:i+2] == "//":
                    break
                elif line[i] in ('"', "'"):
                    in_string = True
                    str_char = line[i]
                    i += 1
                else:
                    cleaned += line[i]
                    i += 1
        result.append(cleaned)
    return result

method_decl_regex = re.compile(
    r"^\s*(?:(?:public|protected|private|internal|static|async|override|virtual|sealed|partial)\s+)+[A-Za-z0-9_<>,\.\?\[\]\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$"
)

m3_violations = []

for f in cs_files:
    with open(f, "r", encoding="utf-8", errors="ignore") as fp:
        raw_code = fp.read()
    
    cleaned_lines = strip_comments_and_strings(raw_code)
    
    # Simple brace depth tracking
    i = 0
    n = len(cleaned_lines)
    while i < n:
        line = cleaned_lines[i]
        # Check if line or combination of lines matches method decl
        m = method_decl_regex.match(line)
        if m:
            method_name = m.group(1)
            # Find opening brace
            brace_line = i
            found_open = False
            while brace_line < n and not found_open:
                if "{" in cleaned_lines[brace_line]:
                    found_open = True
                    break
                elif ";" in cleaned_lines[brace_line]:
                    # Interface or abstract method declaration
                    break
                brace_line += 1
            
            if found_open:
                # Track braces until matched
                depth = 0
                end_line = brace_line
                for k in range(brace_line, n):
                    depth += cleaned_lines[k].count("{") - cleaned_lines[k].count("}")
                    if depth <= 0:
                        end_line = k
                        break
                
                method_len = end_line - i + 1
                if method_len > 50:
                    m3_violations.append((f, i + 1, end_line + 1, method_name, method_len))
                i = end_line
        i += 1

print(f"Rule M3 (method lines <= 50) violations: {len(m3_violations)}")
for f, s, e, name, length in m3_violations:
    print(f"  VIOLATION M3: {f}:{s}-{e} {name}() is {length} lines")
