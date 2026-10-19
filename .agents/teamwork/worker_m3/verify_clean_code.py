import os
import re

ROOT = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"

def tokenize_csharp(code):
    """Tokenize C# code into tokens with line numbers, handling comments and strings."""
    tokens = []
    i = 0
    n = len(code)
    line = 1
    
    while i < n:
        c = code[i]
        
        # Newline
        if c == '\n':
            line += 1
            i += 1
            continue
            
        # Line comment
        if c == '/' and i + 1 < n and code[i+1] == '/':
            start = i
            while i < n and code[i] != '\n':
                i += 1
            tokens.append(('LINE_COMMENT', code[start:i], line))
            continue
            
        # Block comment
        if c == '/' and i + 1 < n and code[i+1] == '*':
            start = i
            start_line = line
            i += 2
            while i + 1 < n and not (code[i] == '*' and code[i+1] == '/'):
                if code[i] == '\n':
                    line += 1
                i += 1
            i += 2 # skip */
            tokens.append(('BLOCK_COMMENT', code[start:i], start_line))
            continue
            
        # Raw string literal ($$""" or """ )
        if c == '"' and i + 2 < n and code[i+1] == '"' and code[i+2] == '"':
            start = i
            start_line = line
            i += 3
            while i + 2 < n and not (code[i] == '"' and code[i+1] == '"' and code[i+2] == '"'):
                if code[i] == '\n':
                    line += 1
                i += 1
            i += 3
            tokens.append(('STRING', code[start:i], start_line))
            continue

        # Verbatim string literal (@"...")
        if c == '@' and i + 1 < n and code[i+1] == '"':
            start = i
            start_line = line
            i += 2
            while i < n:
                if code[i] == '"':
                    if i + 1 < n and code[i+1] == '"':
                        i += 2
                        continue
                    else:
                        i += 1
                        break
                elif code[i] == '\n':
                    line += 1
                i += 1
            tokens.append(('STRING', code[start:i], start_line))
            continue

        # Interpolated verbatim string ($@"..." or @$"...")
        if ((c == '$' and i + 2 < n and code[i+1] == '@' and code[i+2] == '"') or
            (c == '@' and i + 2 < n and code[i+1] == '$' and code[i+2] == '"')):
            start = i
            start_line = line
            i += 3
            while i < n:
                if code[i] == '"':
                    if i + 1 < n and code[i+1] == '"':
                        i += 2
                        continue
                    else:
                        i += 1
                        break
                elif code[i] == '\n':
                    line += 1
                i += 1
            tokens.append(('STRING', code[start:i], start_line))
            continue

        # Regular string ("...")
        if c == '"':
            start = i
            start_line = line
            i += 1
            while i < n:
                if code[i] == '\\' and i + 1 < n:
                    i += 2
                    continue
                if code[i] == '"':
                    i += 1
                    break
                if code[i] == '\n':
                    line += 1
                i += 1
            tokens.append(('STRING', code[start:i], start_line))
            continue

        # Char literal ('...')
        if c == "'":
            start = i
            start_line = line
            i += 1
            while i < n:
                if code[i] == '\\' and i + 1 < n:
                    i += 2
                    continue
                if code[i] == "'":
                    i += 1
                    break
                i += 1
            tokens.append(('CHAR', code[start:i], start_line))
            continue

        # Structural punctuation
        if c in '{}();':
            tokens.append((c, c, line))
            i += 1
            continue

        # Identifiers and keywords
        if c.isalpha() or c == '_':
            start = i
            start_line = line
            while i < n and (code[i].isalnum() or code[i] == '_'):
                i += 1
            tokens.append(('IDENT', code[start:i], start_line))
            continue

        i += 1
        
    return tokens

def find_methods_and_blocks(tokens, code_lines):
    # Analyze methods and blocks
    # A method is an identifier followed by '(' ')' followed by '{' at class level (depth == 1 or 2)
    brace_stack = []
    methods = []
    
    # We track class level
    depth = 0
    i = 0
    n = len(tokens)
    
    while i < n:
        tok_type, tok_val, tok_line = tokens[i]
        
        if tok_type == '{':
            # Check what opened this brace
            # Look backwards for method signature
            brace_stack.append((depth, tok_line, get_enclosing_signature(tokens, i - 1)))
            depth += 1
            
        elif tok_type == '}':
            if brace_stack:
                start_depth, start_line, sig = brace_stack.pop()
                depth -= 1
                end_line = tok_line
                lines_span = end_line - start_line + 1
                if sig and sig.get('is_method'):
                    methods.append({
                        'name': sig['name'],
                        'start': start_line,
                        'end': end_line,
                        'lines': lines_span,
                        'is_local': start_depth > 1
                    })
        i += 1
        
    return methods

def get_enclosing_signature(tokens, idx):
    # Scan backwards from '{' to find if this is a method
    sig_tokens = []
    while idx >= 0:
        ttype, tval, tline = tokens[idx]
        if ttype in ('LINE_COMMENT', 'BLOCK_COMMENT'):
            idx -= 1
            continue
        if ttype in (';', '}', '{'):
            break
        sig_tokens.append(tokens[idx])
        idx -= 1
        
    sig_tokens.reverse()
    if not sig_tokens:
        return None
        
    # If the signature contains class, record, struct, interface, enum, it's not a method
    for t in sig_tokens:
        if t[1] in ('class', 'record', 'struct', 'interface', 'enum', 'namespace'):
            return None
        
    # Look for name(...) or name<...>(...)
    # Check if there is '(' and ')'
    paren_indices = [k for k, t in enumerate(sig_tokens) if t[0] == '(']
    if paren_indices:
        first_paren = paren_indices[0]
        # Token before first paren should be name
        if first_paren > 0 and sig_tokens[first_paren - 1][0] == 'IDENT':
            name = sig_tokens[first_paren - 1][1]
            # Filter out control structures and types
            if name not in ('if', 'for', 'foreach', 'while', 'switch', 'catch', 'lock', 'using', 'fixed', 'Transaction'):
                return {'is_method': True, 'name': name}
                
    return None

def check_file(filepath):
    relpath = os.path.relpath(filepath, ROOT)
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    lines = content.splitlines()
    
    # Rule C2: File lines <= 300
    c2 = len(lines) <= 300
    
    tokens = tokenize_csharp(content)
    methods = find_methods_and_blocks(tokens, lines)
    
    # Rule M3: Methods <= 50 lines
    m3_violations = [m for m in methods if m['lines'] > 50]
    
    # Rule Q-B1: dead / commented-out code
    # Find line comments that look like code statements
    qb1_violations = []
    code_re = re.compile(r'^\s*//\s*(?:var|int|string|bool|return|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|new\s+[A-Z]|await\s+)[^/]+;')
    for idx, l in enumerate(lines, 1):
        if code_re.search(l):
            if not any(w in l.lower() for w in ['e.g.', 'example', 'note:', 'see', 'multi-version', 'todo']):
                qb1_violations.append((idx, l.strip()))
                
    # Rule Q-B2: empty catch without comment
    qb2_violations = []
    for k in range(len(tokens) - 2):
        if tokens[k][1] == 'catch':
            # find next '{'
            j = k + 1
            while j < len(tokens) and tokens[j][0] != '{':
                j += 1
            if j + 1 < len(tokens) and tokens[j][0] == '{' and tokens[j+1][0] == '}':
                # Empty catch block: check if any comment between { and }
                # In tokens, check if LINE_COMMENT or BLOCK_COMMENT exists between j and j+1
                qb2_violations.append((tokens[j][2], "Empty catch block"))

    return {
        'file': relpath,
        'lines': len(lines),
        'c2': c2,
        'methods_checked': len(methods),
        'm3_violations': m3_violations,
        'qb1_violations': qb1_violations,
        'qb2_violations': qb2_violations
    }

def main():
    total_files = 0
    all_m3 = []
    all_c2 = []
    all_qb1 = []
    all_qb2 = []
    
    for root, dirs, files in os.walk(ROOT):
        if 'bin' in dirs: dirs.remove('bin')
        if 'obj' in dirs: dirs.remove('obj')
        for f in files:
            if f.endswith('.cs'):
                total_files += 1
                res = check_file(os.path.join(root, f))
                if not res['c2']:
                    all_c2.append((res['file'], res['lines']))
                if res['m3_violations']:
                    all_m3.append((res['file'], res['m3_violations']))
                if res['qb1_violations']:
                    all_qb1.append((res['file'], res['qb1_violations']))
                if res['qb2_violations']:
                    all_qb2.append((res['file'], res['qb2_violations']))
                    
    print(f"Total C# files: {total_files}")
    print(f"Rule C2 (> 300 lines) violations: {len(all_c2)}")
    for f, l in all_c2:
        print(f"  {f}: {l} lines")
        
    print(f"Rule M3 (> 50 lines) violations: {len(all_m3)}")
    for f, mlist in all_m3:
        for m in mlist:
            print(f"  {f}: {m['name']} ({m['start']}-{m['end']}, {m['lines']} lines)")
            
    print(f"Rule Q-B1 (commented code) violations: {len(all_qb1)}")
    for f, qlist in all_qb1:
        for line_no, text in qlist:
            print(f"  {f}:{line_no} {text}")
            
    print(f"Rule Q-B2 (unannotated catch) violations: {len(all_qb2)}")
    for f, qlist in all_qb2:
        for line_no, text in qlist:
            print(f"  {f}:{line_no} {text}")

if __name__ == '__main__':
    main()
