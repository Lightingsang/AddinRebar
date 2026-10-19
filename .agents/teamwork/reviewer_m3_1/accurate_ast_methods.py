import os
import re

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge"

target_files = []
for root, dirs, files in os.walk(bridge_dir):
    if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
        continue
    for f in files:
        if f.endswith(".cs"):
            target_files.append(os.path.join(root, f))

# Let's read file and parse methods accurately
def parse_methods_in_file(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        text = f.read()

    lines = text.splitlines()
    total_lines = len(lines)

    # Let's tokenize characters to locate '{' and '}' and track depth
    # Remove strings and comments to find structure
    cleaned = []
    i = 0
    n = len(text)
    
    # We will build a list of (line_num, col_num, char) for structural tokens
    # But simpler: scan line by line
    # Let's locate methods
    methods = []
    
    line_idx = 0
    while line_idx < total_lines:
        line = lines[line_idx].strip()
        
        # Check if line contains start of a method
        # Must not be comment, attribute, class/interface/struct/enum/namespace
        if (not line or line.startswith("//") or line.startswith("/*") or line.startswith("*") or
            any(re.search(rf"\b{kw}\b", line) for kw in ["class", "interface", "struct", "record", "enum", "namespace"])):
            line_idx += 1
            continue

        # Check for '(' which might start parameter list
        if "(" in line:
            # exclude control flow keywords
            if any(re.match(rf"^\b{kw}\b", line) for kw in ["if", "for", "foreach", "while", "switch", "using", "catch", "lock", "fixed"]):
                line_idx += 1
                continue
                
            sig_start = line_idx
            # Accumulate signature until either '{' or '=>' or ';'
            sig_lines = []
            curr = line_idx
            found_body = False
            is_expression = False
            has_semicolon = False
            
            # Look ahead for opening of body or ';'
            while curr < total_lines and curr - sig_start < 25:
                cl = lines[curr].strip()
                # strip line comment
                if "//" in cl:
                    cl = cl[:cl.index("//")].strip()
                sig_lines.append(cl)
                
                # Check for '{' or '=>' or ';'
                if "{" in cl:
                    found_body = True
                    break
                if "=>" in cl:
                    found_body = True
                    is_expression = True
                    break
                if ";" in cl:
                    has_semicolon = True
                    break
                curr += 1
                
            if found_body and not has_semicolon:
                # Find the end of method
                body_start_line = curr
                sig_str = " ".join(sig_lines)
                
                # Verify that sig_str actually looks like a method / constructor declaration
                # Needs '(' and ')'
                if "(" in sig_str and ")" in sig_str:
                    # Exclude local function or delegate if not at class level?
                    # All methods and local functions count towards clean code
                    if is_expression:
                        # Scan forward for ';'
                        end_line = body_start_line
                        while end_line < total_lines:
                            if ";" in lines[end_line]:
                                break
                            end_line += 1
                        total_span = end_line - sig_start + 1
                        methods.append((sig_start + 1, end_line + 1, total_span, sig_str.split("=>")[0].strip()))
                        line_idx = end_line
                    else:
                        # Block body: scan matching braces
                        brace_count = 0
                        found_first_brace = False
                        end_line = body_start_line
                        for scan_idx in range(body_start_line, total_lines):
                            l = lines[scan_idx]
                            # Clean comments & string literals
                            clean = re.sub(r'//.*$', '', l)
                            clean = re.sub(r'"(?:\\.|[^"\\])*"', '', clean)
                            for ch in clean:
                                if ch == '{':
                                    brace_count += 1
                                    found_first_brace = True
                                elif ch == '}':
                                    brace_count -= 1
                                    if found_first_brace and brace_count == 0:
                                        end_line = scan_idx
                                        break
                            if found_first_brace and brace_count == 0:
                                break
                        total_span = end_line - sig_start + 1
                        body_span = end_line - body_start_line + 1
                        methods.append((sig_start + 1, end_line + 1, total_span, body_span, sig_str.split("{")[0].strip()))
                        line_idx = end_line
        line_idx += 1
    return methods

print("Analyzing all .cs files in McpBridge...")
all_results = []
violations = []

for tf in sorted(target_files):
    rel = os.path.relpath(tf, bridge_dir)
    res = parse_methods_in_file(tf)
    for item in res:
        if len(item) == 5:
            start_l, end_l, total_span, body_span, sig = item
            all_results.append((rel, start_l, end_l, total_span, body_span, sig))
            if total_span > 50:
                violations.append((rel, start_l, end_l, total_span, body_span, sig))
        else:
            start_l, end_l, total_span, sig = item
            all_results.append((rel, start_l, end_l, total_span, total_span, sig))
            if total_span > 50:
                violations.append((rel, start_l, end_l, total_span, total_span, sig))

all_results.sort(key=lambda x: -x[3])
print(f"Total methods/functions parsed: {len(all_results)}")
print("\n--- Longest Methods (Total Span > 40 lines) ---")
for rel, start_l, end_l, total_span, body_span, sig in all_results:
    if total_span >= 35:
        print(f"[{total_span:2d} lines, body {body_span:2d}] {rel:<35} Lines {start_l:>3}-{end_l:>3} -> {sig[:60]}")

print("\n" + "=" * 80)
print(f"RULE M3 VIOLATIONS (> 50 LINES TOTAL): {len(violations)}")
for rel, start_l, end_l, total_span, body_span, sig in violations:
    print(f"  FAIL: {rel} Lines {start_l}-{end_l} (Total: {total_span} lines, Body: {body_span} lines) -> {sig}")
print("=" * 80)
