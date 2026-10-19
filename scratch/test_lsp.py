import os

def check_lisp_syntax(filepath):
    print(f"=== Inspecting {filepath} ===")
    assert os.path.exists(filepath), f"File does not exist: {filepath}"
    
    with open(filepath, "r", encoding="utf-8", errors="ignore") as fp:
        raw_text = fp.read()
    
    # 1. Split lines check
    split_lines = raw_text.split("\n")
    print(f"raw_text.split('\\n') count: {len(split_lines)}")
    
    read_lines = raw_text.splitlines()
    print(f"splitlines() count: {len(read_lines)}")
    
    # 2. Check parenthesis balance (ignoring string literals and comments)
    paren_depth = 0
    in_string = False
    in_comment = False
    max_depth = 0
    errors = []
    
    for line_no, line in enumerate(split_lines, 1):
        i = 0
        while i < len(line):
            ch = line[i]
            if not in_string and ch == ';':
                # rest of line is comment
                break
            elif ch == '"':
                if i > 0 and line[i-1] == '\\':
                    # escaped quote
                    pass
                else:
                    in_string = not in_string
            elif not in_string:
                if ch == '(':
                    paren_depth += 1
                    if paren_depth > max_depth:
                        max_depth = paren_depth
                elif ch == ')':
                    paren_depth -= 1
                    if paren_depth < 0:
                        errors.append(f"Line {line_no}: Unmatched closing parenthesis ')'")
            i += 1
            
    if in_string:
        errors.append("Unclosed string literal at EOF")
    if paren_depth != 0:
        errors.append(f"Unbalanced parentheses at EOF: depth={paren_depth}")
        
    print(f"Parenthesis balance: {'OK' if not errors else 'ERRORS'}, max depth={max_depth}")
    for err in errors:
        print(f"  ERROR: {err}")
        
    # 3. Check parameter lines 152-237 (0-indexed: 151 to 236)
    print("Checking parameter block lines 152 to 237...")
    param_lines = split_lines[151:237]
    print(f"Param lines count: {len(param_lines)}")
    param_setqs = [l.strip() for l in param_lines if l.strip().startswith("(setq ")]
    print(f"Setq parameter statements: {len(param_setqs)}")
    print("Sample parameter line 152:", split_lines[151].strip())
    print("Sample parameter line 237:", split_lines[236].strip())
    
    # 4. Check for legacy kata_pro
    assert "kata_pro" not in raw_text.lower(), "Found kata_pro in LISP file!"
    print("Zero kata_pro verified: OK")
    
    return len(split_lines), len(errors) == 0

c1, ok1 = check_lisp_syntax(r"C:\HPKata\Goi lenh.lsp")
c2, ok2 = check_lisp_syntax(r"C:\HPKata\GoiLenh_HPKata.lsp")

print("\n=== FINAL LISP VERDICT ===")
print(f"Goi lenh.lsp: {c1} lines (Target 238), Syntax OK: {ok1}")
print(f"GoiLenh_HPKata.lsp: {c2} lines (Target 238), Syntax OK: {ok2}")
assert c1 == 238, f"Expected 238 lines, got {c1}"
assert ok1, "Goi lenh.lsp syntax errors found"
assert ok2, "GoiLenh_HPKata.lsp syntax errors found"
print("ALL LISP CHECKS PASSED EMPIRICALLY!")
