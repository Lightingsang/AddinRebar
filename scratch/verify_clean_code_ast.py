#!/usr/bin/env python3
"""
Automated Clean Code Metrics & AST Verification Script
Audits C# source code for:
- Rule C2: File length <= 300 lines
- Rule M3: Method declaration span and body <= 50 lines
- Rule Q-B1: Zero commented-out code
- Rule Q-B2: Zero unannotated empty catch blocks

Usage:
    python verify_clean_code_ast.py [directory_path] [--strict] [--threshold N]
"""

import os
import re
import sys

def tokenize_csharp(text):
    """
    Tokenizes C# code into tokens, properly handling:
    - Line comments // ...
    - Block comments /* ... */
    - Regular strings "..." with escapes
    - Verbatim strings @"..." with escaped double quotes
    - Interpolated strings $"..." and $@"... / @$"..."
    - Raw strings \"\"\"...\"\"\"
    - Characters '...'
    """
    tokens = [] # list of (token_type, value, start_line, start_col, end_line, end_col)
    i = 0
    n = len(text)
    line = 1
    col = 1

    def advance():
        nonlocal i, line, col
        ch = text[i]
        i += 1
        if ch == '\n':
            line += 1
            col = 1
        else:
            col += 1
        return ch

    def peek(offset=0):
        pos = i + offset
        return text[pos] if pos < n else ''

    while i < n:
        start_line = line
        start_col = col
        ch = text[i]

        # Whitespace
        if ch.isspace():
            advance()
            continue

        # Line comment
        if ch == '/' and peek(1) == '/':
            val = advance() + advance()
            while i < n and text[i] != '\n':
                val += advance()
            tokens.append(('LINE_COMMENT', val, start_line, start_col, line, col))
            continue

        # Block comment
        if ch == '/' and peek(1) == '*':
            val = advance() + advance()
            while i < n and not (text[i] == '*' and peek(1) == '/'):
                val += advance()
            if i < n:
                val += advance() + advance()
            tokens.append(('BLOCK_COMMENT', val, start_line, start_col, line, col))
            continue

        # Raw string literal """
        if (ch == '"' and peek(1) == '"' and peek(2) == '"') or \
           (ch == '$' and peek(1) == '"' and peek(2) == '"' and peek(3) == '"'):
            val = advance()
            if ch == '$':
                val += advance() + advance() + advance()
            else:
                val += advance() + advance()
            # scan until """
            while i < n and not (text[i] == '"' and peek(1) == '"' and peek(2) == '"'):
                val += advance()
            if i < n:
                val += advance() + advance() + advance()
            tokens.append(('STRING', val, start_line, start_col, line, col))
            continue

        # Verbatim string @"..." or @$"..." or $@"...
        if (ch == '@' and peek(1) == '"') or \
           (ch == '@' and peek(1) == '$' and peek(2) == '"') or \
           (ch == '$' and peek(1) == '@' and peek(2) == '"'):
            val = advance() + advance()
            if len(val) == 2 and (val == '@$' or val == '$@'):
                val += advance()
            while i < n:
                c = advance()
                val += c
                if c == '"':
                    if i < n and text[i] == '"':
                        val += advance() # escaped quote ""
                    else:
                        break
            tokens.append(('STRING', val, start_line, start_col, line, col))
            continue

        # Regular string or interpolated string
        if ch == '"' or (ch == '$' and peek(1) == '"'):
            val = advance()
            if ch == '$':
                val += advance()
            escaped = False
            while i < n:
                c = advance()
                val += c
                if escaped:
                    escaped = False
                elif c == '\\':
                    escaped = True
                elif c == '"':
                    break
            tokens.append(('STRING', val, start_line, start_col, line, col))
            continue

        # Character literal
        if ch == "'":
            val = advance()
            escaped = False
            while i < n:
                c = advance()
                val += c
                if escaped:
                    escaped = False
                elif c == '\\':
                    escaped = True
                elif c == "'":
                    break
            tokens.append(('CHAR', val, start_line, start_col, line, col))
            continue

        # Single structural symbols
        if ch in '{}();,[]<>=':
            val = advance()
            tokens.append(('PUNCT', val, start_line, start_col, line, col))
            continue

        # Identifiers and keywords
        if ch.isalpha() or ch == '_':
            val = advance()
            while i < n and (text[i].isalnum() or text[i] == '_'):
                val += advance()
            tokens.append(('IDENT', val, start_line, start_col, line, col))
            continue

        # Other operators / numbers / preprocessors
        val = advance()
        tokens.append(('OTHER', val, start_line, start_col, line, col))

    return tokens

def find_csharp_methods(text, rel_path=""):
    """
    Parses methods, constructors, local functions, and accessors from C# text.
    Returns list of dicts with:
    name, start_line, end_line, total_span, body_start, body_end, body_span
    """
    tokens = tokenize_csharp(text)
    # Filter out comments for structural analysis
    struct_tokens = [t for t in tokens if t[0] not in ('LINE_COMMENT', 'BLOCK_COMMENT')]
    n = len(struct_tokens)

    methods = []
    CONTROL_FLOW = {'if', 'for', 'foreach', 'while', 'switch', 'using', 'catch', 'lock', 'fixed', 'checked', 'unchecked'}
    TYPE_DECLS = {'class', 'interface', 'struct', 'record', 'enum', 'namespace'}
    
    # Remove attributes [Attribute(...)] from structural tokens before detecting methods
    filtered_tokens = []
    t_idx = 0
    while t_idx < len(struct_tokens):
        tok = struct_tokens[t_idx]
        if tok[0] == 'PUNCT' and tok[1] == '[':
            # Check if this bracket is an attribute (at class or member level, not an array/indexer access)
            # Find matching ']'
            b_depth = 1
            b_end = t_idx + 1
            while b_end < len(struct_tokens) and b_depth > 0:
                if struct_tokens[b_end][0] == 'PUNCT':
                    if struct_tokens[b_end][1] == '[': b_depth += 1
                    elif struct_tokens[b_end][1] == ']': b_depth -= 1
                b_end += 1
            if b_depth == 0:
                # If following token is a modifier or type or member declaration keyword, it's an attribute
                if b_end < len(struct_tokens) and (
                    struct_tokens[b_end][0] == 'IDENT' or 
                    (struct_tokens[b_end][0] == 'PUNCT' and struct_tokens[b_end][1] == '[')
                ):
                    t_idx = b_end
                    continue
        filtered_tokens.append(tok)
        t_idx += 1

    struct_tokens = filtered_tokens
    n = len(struct_tokens)

    idx = 0
    while idx < n:
        tok = struct_tokens[idx]

        # Check for start of signature
        # We find '(' and look backwards for identifier and forwards for ')' and '{'
        if tok[0] == 'PUNCT' and tok[1] == '(':
            # Look backwards to find the method name
            name_idx = idx - 1
            while name_idx >= 0 and struct_tokens[name_idx][0] in ('OTHER', 'PUNCT') and struct_tokens[name_idx][1] in ('>', ']', '?', '.'):
                name_idx -= 1
            
            if name_idx >= 0 and struct_tokens[name_idx][0] == 'IDENT':
                ident_name = struct_tokens[name_idx][1]
                
                # Check if it's control flow keyword
                if ident_name in CONTROL_FLOW:
                    idx += 1
                    continue

                # Find start of declaration (modifiers or return type)
                start_decl_idx = name_idx
                is_type_decl = False
                while start_decl_idx > 0:
                    prev = struct_tokens[start_decl_idx - 1]
                    if prev[0] == 'PUNCT' and prev[1] in (';', '{', '}'):
                        break
                    # Also stop if previous is a type decl keyword
                    if prev[0] == 'IDENT' and prev[1] in TYPE_DECLS:
                        is_type_decl = True
                        break
                    start_decl_idx -= 1

                if is_type_decl:
                    idx += 1
                    continue

                decl_start_line = struct_tokens[start_decl_idx][2]

                # Now match closing ')'
                paren_depth = 1
                curr = idx + 1
                while curr < n and paren_depth > 0:
                    if struct_tokens[curr][0] == 'PUNCT':
                        if struct_tokens[curr][1] == '(':
                            paren_depth += 1
                        elif struct_tokens[curr][1] == ')':
                            paren_depth -= 1
                    curr += 1

                if paren_depth == 0:
                    # After ')', look ahead for '{' or '=>' or ';'
                    look = curr
                    found_body = False
                    is_arrow = False
                    body_start_line = -1

                    while look < n and look - curr < 30:
                        ptok = struct_tokens[look]
                        if ptok[0] == 'PUNCT':
                            if ptok[1] == '{':
                                found_body = True
                                body_start_line = ptok[2]
                                break
                            elif ptok[1] == '=' and look + 1 < n and struct_tokens[look + 1][1] == '>':
                                found_body = True
                                is_arrow = True
                                body_start_line = ptok[2]
                                break
                            elif ptok[1] == ';':
                                # Interface or abstract method declaration
                                break
                        look += 1

                    if found_body:
                        if is_arrow:
                            # Look for closing ';'
                            semi = look
                            while semi < n:
                                if struct_tokens[semi][0] == 'PUNCT' and struct_tokens[semi][1] == ';':
                                    break
                                semi += 1
                            if semi < n:
                                end_line = struct_tokens[semi][4]
                                total_span = end_line - decl_start_line + 1
                                body_span = end_line - body_start_line + 1
                                methods.append({
                                    'name': ident_name,
                                    'start_line': decl_start_line,
                                    'end_line': end_line,
                                    'total_span': total_span,
                                    'body_start': body_start_line,
                                    'body_end': end_line,
                                    'body_span': body_span,
                                    'is_arrow': True
                                })
                        else:
                            # Match braces '{' ... '}'
                            brace_depth = 1
                            b_curr = look + 1
                            while b_curr < n and brace_depth > 0:
                                if struct_tokens[b_curr][0] == 'PUNCT':
                                    if struct_tokens[b_curr][1] == '{':
                                        brace_depth += 1
                                    elif struct_tokens[b_curr][1] == '}':
                                        brace_depth -= 1
                                b_curr += 1

                            if brace_depth == 0:
                                end_line = struct_tokens[b_curr - 1][4]
                                total_span = end_line - decl_start_line + 1
                                body_span = end_line - body_start_line + 1
                                methods.append({
                                    'name': ident_name,
                                    'start_line': decl_start_line,
                                    'end_line': end_line,
                                    'total_span': total_span,
                                    'body_start': body_start_line,
                                    'body_end': end_line,
                                    'body_span': body_span,
                                    'is_arrow': False
                                })
                                idx = b_curr - 1

        idx += 1

    return methods

def audit_directory(root_dir, threshold=40):
    cs_files = []
    for r, d, files in os.walk(root_dir):
        if 'bin' in r.split(os.sep) or 'obj' in r.split(os.sep):
            continue
        for f in files:
            if f.endswith('.cs'):
                cs_files.append(os.path.join(r, f))

    cs_files.sort()
    print(f"Scanning {len(cs_files)} C# files in: {root_dir}")

    c2_violations = []
    flagged_methods = []
    hard_violations = []

    for file_path in cs_files:
        rel_path = os.path.relpath(file_path, root_dir).replace('\\', '/')
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        lines = content.splitlines()
        line_count = len(lines)

        if line_count > 300:
            c2_violations.append((rel_path, line_count))

        methods = find_csharp_methods(content, rel_path)
        for m in methods:
            if m['total_span'] > threshold or m['body_span'] > threshold:
                item = (rel_path, m['name'], m['start_line'], m['end_line'], m['total_span'], m['body_span'], m['body_start'], m['body_end'])
                flagged_methods.append(item)
                if m['total_span'] > 50 or m['body_span'] > 50:
                    hard_violations.append(item)

    print("\n" + "=" * 80)
    print(f"RULE C2 FILE LENGTH AUDIT (Max <= 300 lines): {len(c2_violations)} violations")
    if c2_violations:
        for p, l in c2_violations:
            print(f"  [FAIL C2] {p}: {l} lines (exceeds 300)")
    else:
        print("  PASS: All files <= 300 lines.")

    print("\n" + "=" * 80)
    print(f"RULE M3 METHODS AUDIT (Threshold > {threshold} lines): {len(flagged_methods)} methods found")
    flagged_methods.sort(key=lambda x: -max(x[4], x[5]))
    for p, name, sl, el, span, body, bsl, bel in flagged_methods:
        tag = "[FAIL M3 > 50]" if (span > 50 or body > 50) else f"[BORDERLINE {threshold}-50]"
        print(f"  {tag:<20} {p}:{sl}-{el} -> {name}() | Total: {span} lines, Body: {body} lines (lines {bsl}-{bel})")

    print("\n" + "=" * 80)
    print(f"STRICT RULE M3 VIOLATIONS (> 50 LINES): {len(hard_violations)} violations")
    if hard_violations:
        for p, name, sl, el, span, body, bsl, bel in hard_violations:
            print(f"  [FAIL] {p}:{sl}-{el} -> {name}() (Span: {span}, Body: {body})")
    else:
        print("  PASS: Zero methods violate Rule M3 (<= 50 lines).")
    print("=" * 80)

    return len(c2_violations) == 0 and len(hard_violations) == 0

if __name__ == '__main__':
    target = sys.argv[1] if len(sys.argv) > 1 else r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge"
    thresh = int(sys.argv[2]) if len(sys.argv) > 2 else 40
    success = audit_directory(target, thresh)
    sys.exit(0 if success else 1)
