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

method_regex = re.compile(
    r'(?:public|private|protected|internal|static|async|override|virtual|sealed)\s+'
    r'(?:[\w<>\[\],\?]+\s+)+'
    r'(\w+)\s*\([^;{]*\)\s*(?:where\s+[\w\s:<>,]+)?\s*\{',
    re.MULTILINE
)

def analyze_methods(files, label):
    print(f"\n=== Analyzing Methods in {label} ===")
    for path in files:
        rel = os.path.split(path)[1]
        with open(path, "r", encoding="utf-8") as f:
            text = f.read()

        for match in method_regex.finditer(text):
            method_name = match.group(1)
            if method_name in ("if", "while", "for", "foreach", "switch", "catch", "using", "lock"):
                continue
            start_pos = match.start()
            start_line = text.count('\n', 0, start_pos) + 1
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
                print(f"[{rel}] METHOD > 50 LINES: {method_name} ({method_lines} lines, L{start_line}-L{end_line})")

analyze_methods(get_cs_files(client_src), "HPBim.Client")
analyze_methods(get_cs_files(test_src), "HPBim.Client.Tests")
