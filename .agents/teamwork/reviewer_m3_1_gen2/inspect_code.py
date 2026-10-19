import os
import re

client_src = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client"
test_src = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests"

def get_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        if "obj" in dirs:
            dirs.remove("obj")
        if "bin" in dirs:
            dirs.remove("bin")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

print("=== HPBim.Client Files ===")
for path in get_cs_files(client_src):
    rel = os.path.relpath(path, client_src)
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    print(f"{rel}: {len(lines)} lines")

print("\n=== Check Catch Blocks in Client ===")
for path in get_cs_files(client_src):
    rel = os.path.relpath(path, client_src)
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()
    # Find catch blocks
    for match in re.finditer(r'catch\s*(\([^)]*\))?\s*(when\s*\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL):
        body = match.group(3).strip()
        has_comment = "//" in body or "/*" in body
        has_statement = any(stmt for stmt in body.split(";") if stmt.strip() and not stmt.strip().startswith("//"))
        print(f"[{rel}] catch: has_statement={has_statement}, has_comment={has_comment}")
        if not has_statement and not has_comment:
            print(f"  VIOLATION: Empty catch without comment: {match.group(0)}")
        elif not has_statement:
            print(f"  Justified empty catch: {body}")

print("\n=== Method Lengths in HPBim.Client ===")
# Basic parser for C# methods: look for method signatures followed by { ... }
method_regex = re.compile(
    r'(?:public|private|protected|internal|static|async|override|virtual|sealed)\s+'
    r'(?:[\w<>\[\],\?]+\s+)+'
    r'(\w+)\s*\([^;{]*\)\s*(?:where\s+[\w\s:<>,]+)?\s*\{',
    re.MULTILINE
)

for path in get_cs_files(client_src):
    rel = os.path.relpath(path, client_src)
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
        lines = text.splitlines()

    # Track open braces to find method boundaries
    for match in method_regex.finditer(text):
        method_name = match.group(1)
        if method_name in ("if", "while", "for", "foreach", "switch", "catch", "using", "lock"):
            continue
        start_pos = match.start()
        # Find start line
        start_line = text.count('\n', 0, start_pos) + 1
        # Track braces from {
        brace_start = match.end() - 1
        brace_count = 1
        i = brace_start + 1
        in_string = False
        in_char = False
        in_line_comment = False
        in_block_comment = False
        is_raw_string = False

        while i < len(text) and brace_count > 0:
            c = text[i]
            prev = text[i-1] if i > 0 else ''
            
            # Simplified brace matching handling raw strings or comments
            if text[i:i+3] == '"""':
                is_raw_string = not is_raw_string
                i += 3
                continue
            if is_raw_string:
                i += 1
                continue

            if not in_string and not in_char:
                if text[i:i+2] == '//':
                    in_line_comment = True
                    i += 2
                    continue
                if text[i:i+2] == '/*':
                    in_block_comment = True
                    i += 2
                    continue
            if in_line_comment:
                if c == '\n':
                    in_line_comment = False
                i += 1
                continue
            if in_block_comment:
                if text[i:i+2] == '*/':
                    in_block_comment = False
                    i += 2
                    continue
                i += 1
                continue
            if c == '"' and prev != '\\':
                in_string = not in_string
            elif c == "'" and prev != '\\':
                in_char = not in_char
            elif not in_string and not in_char:
                if c == '{':
                    brace_count += 1
                elif c == '}':
                    brace_count -= 1
            i += 1

        end_line = text.count('\n', 0, i) + 1
        method_lines = end_line - start_line + 1
        if method_lines > 50:
            print(f"[{rel}] Method '{method_name}' is {method_lines} lines (L{start_line}-L{end_line})")
        else:
            # print ok for longish methods
            if method_lines > 35:
                print(f"[{rel}] Method '{method_name}' = {method_lines} lines (OK <= 50)")

print("\nDone script inspection.")
