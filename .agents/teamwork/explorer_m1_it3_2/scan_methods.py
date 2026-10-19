import os
import re
import sys

def strip_comments_and_strings(code):
    """
    Replaces comments and strings with whitespace preserving newlines and character positions.
    Returns stripped code and line map.
    """
    chars = list(code)
    n = len(chars)
    i = 0
    state = 'NORMAL' # NORMAL, LINE_COMMENT, BLOCK_COMMENT, STRING, VERBATIM_STRING, CHAR

    while i < n:
        c = chars[i]
        nxt = chars[i+1] if i + 1 < n else ''

        if state == 'NORMAL':
            if c == '/' and nxt == '/':
                chars[i] = ' '
                chars[i+1] = ' '
                i += 2
                state = 'LINE_COMMENT'
            elif c == '/' and nxt == '*':
                chars[i] = ' '
                chars[i+1] = ' '
                i += 2
                state = 'BLOCK_COMMENT'
            elif c == '@' and nxt == '"':
                chars[i] = ' '
                chars[i+1] = ' '
                i += 2
                state = 'VERBATIM_STRING'
            elif c == '$' and nxt == '"':
                chars[i] = ' '
                chars[i+1] = ' '
                i += 2
                state = 'STRING'
            elif c == '"':
                chars[i] = ' '
                i += 1
                state = 'STRING'
            elif c == "'":
                chars[i] = ' '
                i += 1
                state = 'CHAR'
            else:
                i += 1
        elif state == 'LINE_COMMENT':
            if c == '\n':
                state = 'NORMAL'
            else:
                chars[i] = ' '
            i += 1
        elif state == 'BLOCK_COMMENT':
            if c == '*' and nxt == '/':
                chars[i] = ' '
                chars[i+1] = ' '
                i += 2
                state = 'NORMAL'
            else:
                if c != '\n':
                    chars[i] = ' '
                i += 1
        elif state == 'STRING':
            if c == '\\':
                chars[i] = ' '
                if i + 1 < n and chars[i+1] != '\n':
                    chars[i+1] = ' '
                i += 2
            elif c == '"':
                chars[i] = ' '
                i += 1
                state = 'NORMAL'
            else:
                if c != '\n':
                    chars[i] = ' '
                i += 1
        elif state == 'VERBATIM_STRING':
            if c == '"' and nxt == '"':
                chars[i] = ' '
                chars[i+1] = ' '
                i += 2
            elif c == '"':
                chars[i] = ' '
                i += 1
                state = 'NORMAL'
            else:
                if c != '\n':
                    chars[i] = ' '
                i += 1
        elif state == 'CHAR':
            if c == '\\':
                chars[i] = ' '
                if i + 1 < n and chars[i+1] != '\n':
                    chars[i+1] = ' '
                i += 2
            elif c == "'":
                chars[i] = ' '
                i += 1
                state = 'NORMAL'
            else:
                if c != '\n':
                    chars[i] = ' '
                i += 1

    return "".join(chars)

def find_methods_in_file(filepath):
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        original_code = f.read()

    lines = original_code.splitlines()
    clean_code = strip_comments_and_strings(original_code)

    # Tokenize clean_code tracking line numbers
    # We want to identify method signatures followed by { ... }
    # A method signature typically has:
    # [modifiers] ReturnType MethodName ( params ) [where constraints] {
    
    method_pattern = re.compile(
        r'(?:(public|private|protected|internal|static|async|virtual|override|sealed|new|partial)\s+)+'
        r'([\w\<\>\[\],\s\?\.]+)\s+'
        r'(\b[A-Z]\w*)\s*'
        r'\(([^\)]*)\)\s*'
        r'(?:where\s+[^{]+)?\s*\{',
        re.MULTILINE
    )

    violations = []
    
    # We can also track brace balance to find the exact end line
    # Let's match all candidate method starts
    for match in method_pattern.finditer(clean_code):
        start_char_idx = match.start()
        # Find opening brace index
        brace_idx = match.end() - 1
        assert clean_code[brace_idx] == '{'

        # Get method name
        method_name = match.group(3)
        # Exclude class, struct, record, interface, enum, namespace
        if match.group(2) in ('class', 'struct', 'record', 'interface', 'enum', 'namespace'):
            continue

        # Line number of start (1-indexed)
        start_line = clean_code[:start_char_idx].count('\n') + 1

        # Now count braces from brace_idx to find matching closing brace
        depth = 1
        curr = brace_idx + 1
        end_line = None
        while curr < len(clean_code):
            ch = clean_code[curr]
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    end_line = clean_code[:curr].count('\n') + 1
                    break
            curr += 1

        if end_line is not None:
            span = end_line - start_line + 1
            if span > 50:
                violations.append({
                    'method': method_name,
                    'start_line': start_line,
                    'end_line': end_line,
                    'span': span
                })

    return violations

def scan_directory(root_dir):
    all_violations = {}
    for dirpath, _, filenames in os.walk(root_dir):
        if 'obj' in dirpath.split(os.sep) or 'bin' in dirpath.split(os.sep):
            continue
        for fn in filenames:
            if fn.endswith('.cs'):
                fp = os.path.join(dirpath, fn)
                v = find_methods_in_file(fp)
                if v:
                    rel_path = os.path.relpath(fp, root_dir)
                    all_violations[rel_path] = v
    return all_violations

if __name__ == '__main__':
    src_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src"
    results = scan_directory(src_dir)
    print(f"Total files with M3 violations: {len(results)}")
    for f, vlist in sorted(results.items()):
        print(f"\nFile: {f}")
        for v in vlist:
            print(f"  - Method {v['method']} (L{v['start_line']}..L{v['end_line']}): {v['span']} lines")
