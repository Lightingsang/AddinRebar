import os
import re

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
hpkata_dir = os.path.join(root_dir, "HPKata")

# Find all cs files
all_cs_files = []
for root, dirs, files in os.walk(hpkata_dir):
    if any(p in root.split(os.sep) for p in ["bin", "obj", ".vs"]):
        continue
    for f in files:
        if f.endswith(".cs"):
            all_cs_files.append(os.path.join(root, f))

phase4_keywords = [
    "Foundation", "Footing", "Pile", "Slab", "Stair", "Lintel", "SectionCut", 
    "GenericSection", "Tier3", "Phase4"
]

phase4_files = [
    p for p in all_cs_files 
    if any(k.lower() in os.path.relpath(p, hpkata_dir).lower() for k in phase4_keywords)
]

def strip_comments_and_strings(code):
    # Replace strings and comments while preserving line breaks
    # This keeps line numbers intact!
    output = []
    i = 0
    n = len(code)
    in_single_comment = False
    in_multi_comment = False
    in_string = False
    in_verbatim_string = False
    in_char = False

    while i < n:
        c = code[i]
        c2 = code[i:i+2] if i+1 < n else ""

        if in_single_comment:
            if c == '\n':
                in_single_comment = False
                output.append('\n')
            else:
                output.append(' ')
            i += 1
        elif in_multi_comment:
            if c2 == '*/':
                in_multi_comment = False
                output.append('  ')
                i += 2
            elif c == '\n':
                output.append('\n')
                i += 1
            else:
                output.append(' ')
                i += 1
        elif in_verbatim_string:
            if c2 == '""':
                output.append('  ')
                i += 2
            elif c == '"':
                in_verbatim_string = False
                output.append(' ')
                i += 1
            elif c == '\n':
                output.append('\n')
                i += 1
            else:
                output.append(' ')
                i += 1
        elif in_string:
            if c2 == '\\"':
                output.append('  ')
                i += 2
            elif c == '"':
                in_string = False
                output.append(' ')
                i += 1
            elif c == '\n':
                output.append('\n')
                i += 1
            else:
                output.append(' ')
                i += 1
        elif in_char:
            if c2 == "\\'":
                output.append('  ')
                i += 2
            elif c == "'":
                in_char = False
                output.append(' ')
                i += 1
            else:
                output.append(' ')
                i += 1
        else:
            if c2 == '//':
                in_single_comment = True
                output.append('  ')
                i += 2
            elif c2 == '/*':
                in_multi_comment = True
                output.append('  ')
                i += 2
            elif c2 == '@"' or c2 == '$"':
                if c2 == '@"':
                    in_verbatim_string = True
                else:
                    in_string = True
                output.append('  ')
                i += 2
            elif code[i:i+3] == '$@"' or code[i:i+3] == '@$"':
                in_verbatim_string = True
                output.append('   ')
                i += 3
            elif c == '"':
                in_string = True
                output.append(' ')
                i += 1
            elif c == "'":
                in_char = True
                output.append(' ')
                i += 1
            else:
                output.append(c)
                i += 1

    return "".join(output)

# Method parser using sanitized code
def find_methods(raw_code):
    sanitized = strip_comments_and_strings(raw_code)
    lines_san = sanitized.splitlines(True)
    lines_raw = raw_code.splitlines(True)

    methods = []
    # Regular expression to identify start of a method / constructor declaration
    # e.g., public void Foo(int a, string b)
    # e.g., private static async Task<bool> BarAsync(...)
    sig_re = re.compile(
        r'^\s*(?:\[.+?\]\s*)*(?:public|private|protected|internal|static|virtual|override|abstract|async|sealed|unsafe|\s)+'
        r'[\w\<\>\[\],\?\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+[^{;]+)?$'
    )

    total_lines = len(lines_san)
    i = 0
    while i < total_lines:
        line_clean = lines_san[i].strip()
        # Look for candidate line
        if "(" in line_clean and not any(line_clean.startswith(kw) for kw in [
            "if", "for", "foreach", "while", "switch", "using", "catch", "lock", "fixed", "new"
        ]):
            # Try to build signature up to '{' or ';' or '=>'
            sig_start = i
            sig_parts = []
            curr = i
            found_open_brace = False
            found_expr = False
            has_semicolon = False
            brace_pos = -1

            while curr < total_lines and curr - sig_start < 15:
                cl = lines_san[curr].strip()
                sig_parts.append(cl)
                if "{" in cl:
                    found_open_brace = True
                    break
                if "=>" in cl:
                    found_expr = True
                    break
                if ";" in cl:
                    has_semicolon = True
                    break
                curr += 1

            sig_text = " ".join(sig_parts)
            if (found_open_brace or found_expr) and not has_semicolon:
                # Check if it looks like a method
                m = re.search(r'\b([A-Za-z0-9_]+)\s*\([^;]*\)', sig_text)
                if m and not any(sig_text.strip().startswith(kw) for kw in [
                    "class ", "interface ", "struct ", "enum ", "record ", "namespace ", "delegate "
                ]):
                    name = m.group(1)
                    if name not in ["if", "for", "foreach", "while", "switch", "catch", "using"]:
                        if found_open_brace:
                            # Match closing brace
                            start_line = sig_start + 1
                            brace_count = 0
                            found_first = False
                            end_line = start_line
                            for scan_idx in range(curr, total_lines):
                                for char in lines_san[scan_idx]:
                                    if char == '{':
                                        brace_count += 1
                                        found_first = True
                                    elif char == '}':
                                        brace_count -= 1
                                        if found_first and brace_count == 0:
                                            end_line = scan_idx + 1
                                            break
                                if found_first and brace_count == 0:
                                    break
                            
                            span = end_line - start_line + 1
                            methods.append((name, start_line, end_line, span, False))
                            i = end_line - 1
                        elif found_expr:
                            # Expression-bodied method
                            start_line = sig_start + 1
                            end_line = curr + 1
                            while end_line <= total_lines and ";" not in lines_san[end_line - 1]:
                                end_line += 1
                            span = end_line - start_line + 1
                            methods.append((name, start_line, end_line, span, True))
                            i = end_line - 1
        i += 1
    return methods

print(f"Auditing method lengths across {len(phase4_files)} Phase 4 files...")
violations = []
checked_methods_count = 0
longest_methods = []

for p in phase4_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        content = f.read()
    ms = find_methods(content)
    for name, s, e, span, is_expr in ms:
        checked_methods_count += 1
        longest_methods.append((rel, name, s, e, span))
        if span > 50:
            violations.append((rel, name, s, e, span))

longest_methods.sort(key=lambda x: -x[4])

print(f"Total methods analyzed in Phase 4: {checked_methods_count}")
print("\n--- Top 10 Longest Methods in Phase 4 ---")
for rel, name, s, e, span in longest_methods[:10]:
    print(f"  {span:2d} lines: {rel} -> {name} (lines {s}-{e})")

print(f"\nRule M3 Violations (> 50 lines): {len(violations)}")
for rel, name, s, e, span in violations:
    print(f"  [VIOLATION] {rel} -> {name}: {span} lines (lines {s}-{e})")

