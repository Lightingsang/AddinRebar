import os
import sys

def verify_goilenh(lsp_path):
    print(f"Checking GoiLenh at: {lsp_path}")
    if not os.path.exists(lsp_path):
        print(f"FAIL: File not found: {lsp_path}")
        return False

    with open(lsp_path, "rb") as f:
        raw_bytes = f.read()

    # Check line endings and split
    # Normalized split by \n
    text = raw_bytes.decode("utf-8", errors="replace")
    split_lines = text.split("\n")
    print(f"Total lines by '\\n' split: {len(split_lines)}")
    
    # Check lines with standard splitlines()
    splitlines_lines = text.splitlines()
    print(f"Total lines by splitlines(): {len(splitlines_lines)}")

    # Check line count
    # Note: text has trailing newline, so split('\n') has 238 items, last one is empty string
    if len(split_lines) != 238:
        print(f"FAIL: Expected 238 lines when split by '\\n', got {len(split_lines)}")
        return False
    else:
        print("PASS: Exactly 238 lines when split by '\\n' (with trailing newline)")

    if len(splitlines_lines) != 237:
        print(f"FAIL: Expected 237 content lines, got {len(splitlines_lines)}")
        return False
    else:
        print("PASS: Exactly 237 content lines")

    # Verify line 152 (1-based, 151 in 0-based)
    line152 = splitlines_lines[151].strip()
    print(f"Line 152: '{line152}'")
    if line152 != ";kata_dim":
        print(f"FAIL: Line 152 is not ';kata_dim', got '{line152}'")
        return False
    else:
        print("PASS: Line 152 is ';kata_dim'")

    # Verify line 237 (1-based, 236 in 0-based)
    line237 = splitlines_lines[236].strip()
    print(f"Line 237: '{line237}'")
    if line237 != ";0":
        print(f"FAIL: Line 237 is not ';0', got '{line237}'")
        return False
    else:
        print("PASS: Line 237 is ';0'")

    # Verify lines 152-237 count
    config_lines = splitlines_lines[151:237]
    print(f"Config lines count (152..237): {len(config_lines)}")
    if len(config_lines) != 86:
        print(f"FAIL: Expected 86 config lines, got {len(config_lines)}")
        return False
    else:
        print("PASS: Exactly 86 config lines in lines 152..237")

    # Verify all config lines start with ';'
    non_comment_config = [i+152 for i, l in enumerate(config_lines) if not l.strip().startswith(";")]
    if non_comment_config:
        print(f"FAIL: Config lines not starting with ';': {non_comment_config}")
        return False
    else:
        print("PASS: All 86 config lines start with ';'")

    # Verify 118 command aliases in lines 1..151
    aliases = []
    lisp_code_lines = splitlines_lines[0:151]
    
    open_parens = 0
    close_parens = 0

    for idx, line in enumerate(lisp_code_lines):
        stripped = line.strip()
        # Count parentheses outside of comments and strings
        in_string = False
        in_comment = False
        line_open = 0
        line_close = 0
        i = 0
        while i < len(line):
            ch = line[i]
            if not in_string and ch == ';':
                # comment starts
                break
            elif ch == '"':
                in_string = not in_string
            elif not in_string:
                if ch == '(':
                    line_open += 1
                elif ch == ')':
                    line_close += 1
            i += 1
        
        open_parens += line_open
        close_parens += line_close

        if stripped.startswith("(defun c:"):
            # Extract alias
            # Format: (defun c:ALIAS()(command "COMMAND"))
            alias_part = stripped[len("(defun c:"):].split("(")[0].strip()
            # Extract command
            cmd_part = ""
            if '"' in stripped:
                cmd_part = stripped.split('"')[1]
            aliases.append((idx + 1, alias_part, cmd_part))

    print(f"LISP section parentheses: open={open_parens}, close={close_parens}")
    if open_parens != close_parens or open_parens == 0:
        print(f"FAIL: Parentheses unbalanced in lines 1..151: {open_parens} open vs {close_parens} close")
        return False
    else:
        print(f"PASS: Parentheses 100% balanced across LISP code section ({open_parens} pairs)")

    print(f"Total defun c: command aliases found: {len(aliases)}")
    if len(aliases) != 118:
        print(f"FAIL: Expected 118 command aliases, got {len(aliases)}")
        return False
    else:
        print("PASS: Exactly 118 command aliases found")

    # Check for duplicate aliases
    seen = {}
    duplicates = []
    for line_no, alias, cmd in aliases:
        if alias in seen:
            duplicates.append((alias, seen[alias], line_no))
        else:
            seen[alias] = line_no

    if duplicates:
        print(f"WARNING/FAIL: Duplicate aliases detected: {duplicates}")
    else:
        print("PASS: Zero duplicate aliases among the 118 definitions")

    return True

if __name__ == "__main__":
    lsp_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\GoiLenh_HPKata.lsp"
    ok = verify_goilenh(lsp_file)
    sys.exit(0 if ok else 1)
