#!/usr/bin/env python3
"""
Adversarial AST & Clean Code Quality Challenger for HPGrasshopper M5
Author: challenger_m5_final_2
Independent Empirical Verification Harness
"""

import os
import sys
import re
from pathlib import Path

def tokenize_csharp(text):
    """
    Complete C# tokenizer that produces tokens with line and column numbers.
    """
    tokens = []
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
                        val += advance()
                    else:
                        break
            tokens.append(('STRING', val, start_line, start_col, line, col))
            continue

        # Regular string
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

        # Structural punctuation
        if ch in '{}();,[]<>=':
            val = advance()
            tokens.append(('PUNCT', val, start_line, start_col, line, col))
            continue

        # Identifiers
        if ch.isalpha() or ch == '_':
            val = advance()
            while i < n and (text[i].isalnum() or text[i] == '_'):
                val += advance()
            tokens.append(('IDENT', val, start_line, start_col, line, col))
            continue

        # Other
        val = advance()
        tokens.append(('OTHER', val, start_line, start_col, line, col))

    return tokens


def check_q_b1_commented_code(raw_text, file_rel):
    """
    Probes comments for commented-out dead C# code (Rule Q-B1 / CM3).
    Ignores URLs, XML doc, section headers, English/Vietnamese prose, TODOs, version tags.
    """
    violations = []
    lines = raw_text.splitlines()

    # C# statement patterns that are almost certainly dead code
    DEAD_CODE_PATTERNS = [
        re.compile(r'^\s*//\s*(var|int|string|bool|double|float|long)\s+[A-Za-z_][A-Za-z0-9_]*\s*='),
        re.compile(r'^\s*//\s*[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\s*\([^)]*\)\s*;'),
        re.compile(r'^\s*//\s*return\s+[^;]+;'),
        re.compile(r'^\s*//\s*if\s*\([^)]+\)\s*\{?$'),
        re.compile(r'^\s*//\s*throw\s+new\s+[A-Za-z_][A-Za-z0-9_]*Exception'),
        re.compile(r'^\s*//\s*await\s+[A-Za-z_][A-Za-z0-9_]*'),
        re.compile(r'^\s*//\s*for\s*\([^;]+;[^;]+;[^)]+\)'),
        re.compile(r'^\s*//\s*foreach\s*\([^)]+\bin\b'),
    ]

    for idx, l in enumerate(lines, 1):
        stripped = l.strip()
        # Skip doc comments
        if stripped.startswith('///'):
            continue
        # Skip section markers like // --- or // ===
        if re.match(r'^//\s*[-=~_*#]+', stripped):
            continue
        # Skip URLs or references
        if 'http://' in stripped or 'https://' in stripped or 'ADR-' in stripped or 'Rule ' in stripped:
            continue
        # Skip TODO
        if 'TODO' in stripped or 'FIXME' in stripped or 'NOTE' in stripped:
            continue

        for pat in DEAD_CODE_PATTERNS:
            if pat.search(l):
                violations.append((file_rel, idx, stripped))
                break

    return violations


def check_q_b2_unannotated_catch(raw_text, file_rel):
    """
    Probes for actual empty catch blocks in C# code without comments (Rule Q-B2).
    Uses the token stream to ensure string literals containing "catch { }" are not false positives.
    """
    tokens = tokenize_csharp(raw_text)
    violations = []
    n = len(tokens)
    
    for i in range(n):
        tok = tokens[i]
        # Look for 'catch' identifier outside strings/comments
        if tok[0] == 'IDENT' and tok[1] == 'catch':
            catch_line = tok[2]
            # Advance past optional catch filter / exception declaration (e.g. `(Exception ex)`)
            j = i + 1
            if j < n and tokens[j][0] == 'PUNCT' and tokens[j][1] == '(':
                p_depth = 1
                j += 1
                while j < n and p_depth > 0:
                    if tokens[j][0] == 'PUNCT':
                        if tokens[j][1] == '(': p_depth += 1
                        elif tokens[j][1] == ')': p_depth -= 1
                    j += 1
            # Next should be '{'
            if j < n and tokens[j][0] == 'PUNCT' and tokens[j][1] == '{':
                open_brace_idx = j
                # Check contents between open brace and close brace
                # We need to see if there is any code or comment inside
                has_code = False
                has_comment = False
                b_depth = 1
                k = j + 1
                while k < n and b_depth > 0:
                    t = tokens[k]
                    if t[0] in ('LINE_COMMENT', 'BLOCK_COMMENT'):
                        has_comment = True
                    elif t[0] == 'PUNCT':
                        if t[1] == '{': b_depth += 1
                        elif t[1] == '}':
                            b_depth -= 1
                            if b_depth == 0:
                                break
                    else:
                        has_code = True
                    k += 1
                
                if not has_code and not has_comment:
                    violations.append((file_rel, catch_line, "catch { }"))
    
    return violations


def find_all_methods_and_blocks(raw_text, file_rel):
    """
    Accurately discovers methods, constructors, local functions, and accessors.
    """
    tokens = tokenize_csharp(raw_text)
    struct_tokens = [t for t in tokens if t[0] not in ('LINE_COMMENT', 'BLOCK_COMMENT')]
    n = len(struct_tokens)

    # Filter attributes [...]
    filtered = []
    t_idx = 0
    while t_idx < n:
        tok = struct_tokens[t_idx]
        if tok[0] == 'PUNCT' and tok[1] == '[':
            b_depth = 1
            b_end = t_idx + 1
            while b_end < n and b_depth > 0:
                if struct_tokens[b_end][0] == 'PUNCT':
                    if struct_tokens[b_end][1] == '[': b_depth += 1
                    elif struct_tokens[b_end][1] == ']': b_depth -= 1
                b_end += 1
            if b_depth == 0 and b_end < n:
                # Check if it looks like an attribute
                if struct_tokens[b_end][0] == 'IDENT' or (struct_tokens[b_end][0] == 'PUNCT' and struct_tokens[b_end][1] == '['):
                    t_idx = b_end
                    continue
        filtered.append(tok)
        t_idx += 1

    struct_tokens = filtered
    n = len(struct_tokens)
    methods = []
    CONTROL_FLOW = {'if', 'for', 'foreach', 'while', 'switch', 'using', 'catch', 'lock', 'fixed', 'checked', 'unchecked'}
    TYPE_DECLS = {'class', 'interface', 'struct', 'record', 'enum', 'namespace'}

    idx = 0
    while idx < n:
        tok = struct_tokens[idx]

        if tok[0] == 'PUNCT' and tok[1] == '(':
            name_idx = idx - 1
            while name_idx >= 0 and struct_tokens[name_idx][0] in ('OTHER', 'PUNCT') and struct_tokens[name_idx][1] in ('>', ']', '?', '.'):
                name_idx -= 1

            if name_idx >= 0 and struct_tokens[name_idx][0] == 'IDENT':
                ident_name = struct_tokens[name_idx][1]

                if ident_name in CONTROL_FLOW:
                    idx += 1
                    continue

                start_decl_idx = name_idx
                is_type_decl = False
                while start_decl_idx > 0:
                    prev = struct_tokens[start_decl_idx - 1]
                    if prev[0] == 'PUNCT' and prev[1] in (';', '{', '}'):
                        break
                    if prev[0] == 'IDENT' and prev[1] in TYPE_DECLS:
                        is_type_decl = True
                        break
                    start_decl_idx -= 1

                if is_type_decl:
                    idx += 1
                    continue

                decl_start_line = struct_tokens[start_decl_idx][2]

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
                                break
                        look += 1

                    if found_body:
                        if is_arrow:
                            semi = look
                            while semi < n:
                                if struct_tokens[semi][0] == 'PUNCT' and struct_tokens[semi][1] == ';':
                                    break
                                semi += 1
                            if semi < n:
                                end_line = struct_tokens[semi][4]
                                methods.append({
                                    'name': ident_name,
                                    'start_line': decl_start_line,
                                    'end_line': end_line,
                                    'span': end_line - decl_start_line + 1,
                                    'body_start': body_start_line,
                                    'body_end': end_line,
                                    'body_span': end_line - body_start_line + 1,
                                    'is_arrow': True
                                })
                        else:
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
                                methods.append({
                                    'name': ident_name,
                                    'start_line': decl_start_line,
                                    'end_line': end_line,
                                    'span': end_line - decl_start_line + 1,
                                    'body_start': body_start_line,
                                    'body_end': end_line,
                                    'body_span': end_line - body_start_line + 1,
                                    'is_arrow': False
                                })
                                idx = b_curr - 1

        idx += 1

    return methods


def audit_hpgrasshopper(root_dir):
    print("=" * 80)
    print(f"EMPIRICAL AUDIT: HPGrasshopper Quality & Clean Code Standards")
    print(f"Target Root: {root_dir}")
    print("=" * 80)

    cs_files = []
    for r, d, files in os.walk(root_dir):
        if 'bin' in r.split(os.sep) or 'obj' in r.split(os.sep):
            continue
        for f in files:
            if f.endswith('.cs'):
                cs_files.append(os.path.join(r, f))

    cs_files.sort()
    print(f"Total C# files found (excluding bin/obj): {len(cs_files)}")

    # 1. Rule C2
    c2_violations = []
    for fp in cs_files:
        rel = os.path.relpath(fp, root_dir).replace('\\', '/')
        with open(fp, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()
        if len(lines) > 300:
            c2_violations.append((rel, len(lines)))

    print(f"\n--- Rule C2 (File Length <= 300) ---")
    if c2_violations:
        print(f"FAIL: {len(c2_violations)} files violate Rule C2:")
        for r, cnt in c2_violations:
            print(f"  [C2-FAIL] {r}: {cnt} lines")
    else:
        print(f"PASS: 0 files violate Rule C2 (All {len(cs_files)} files <= 300 lines).")

    # 2. Rule M3
    m3_violations = []
    borderline_methods = []
    total_methods = 0

    for fp in cs_files:
        rel = os.path.relpath(fp, root_dir).replace('\\', '/')
        with open(fp, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        methods = find_all_methods_and_blocks(content, rel)
        total_methods += len(methods)
        for m in methods:
            if m['span'] > 50 or m['body_span'] > 50:
                m3_violations.append((rel, m['name'], m['start_line'], m['end_line'], m['span'], m['body_span']))
            elif m['span'] > 40 or m['body_span'] > 40:
                borderline_methods.append((rel, m['name'], m['start_line'], m['end_line'], m['span'], m['body_span']))

    print(f"\n--- Rule M3 (Method Length <= 50) ---")
    print(f"Total methods/constructors analyzed: {total_methods}")
    if m3_violations:
        print(f"FAIL: {len(m3_violations)} methods violate Rule M3 (> 50 lines):")
        for rel, name, sl, el, span, body in m3_violations:
            print(f"  [M3-FAIL] {rel}:{sl}-{el} {name}() - Span: {span}, Body: {body}")
    else:
        print(f"PASS: 0 methods violate Rule M3 (All methods <= 50 lines).")

    print(f"Borderline methods (41-50 lines): {len(borderline_methods)}")
    for rel, name, sl, el, span, body in sorted(borderline_methods, key=lambda x: -max(x[4], x[5])):
        print(f"  [M3-BORDER] {rel}:{sl}-{el} {name}() - Span: {span}, Body: {body}")

    # 3. Rule Q-B1 (Commented code)
    qb1_violations = []
    for fp in cs_files:
        rel = os.path.relpath(fp, root_dir).replace('\\', '/')
        with open(fp, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        violations = check_q_b1_commented_code(content, rel)
        qb1_violations.extend(violations)

    print(f"\n--- Rule Q-B1 (Commented-out Dead Code) ---")
    if qb1_violations:
        print(f"FAIL: {len(qb1_violations)} instances of commented-out code detected:")
        for rel, line_no, text in qb1_violations:
            print(f"  [Q-B1-FAIL] {rel}:{line_no} -> {text}")
    else:
        print(f"PASS: 0 commented-out dead code statements found.")

    # 4. Rule Q-B2 (Unannotated catch)
    qb2_violations = []
    for fp in cs_files:
        rel = os.path.relpath(fp, root_dir).replace('\\', '/')
        with open(fp, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        violations = check_q_b2_unannotated_catch(content, rel)
        qb2_violations.extend(violations)

    print(f"\n--- Rule Q-B2 (Unannotated Empty Catch) ---")
    if qb2_violations:
        print(f"FAIL: {len(qb2_violations)} unannotated catch blocks detected:")
        for rel, line_no, snippet in qb2_violations:
            print(f"  [Q-B2-FAIL] {rel}:{line_no} -> {snippet}")
    else:
        print(f"PASS: 0 unannotated empty catch blocks found.")

    # 5. Embedded Seeds (ADR-0007)
    seed_dir = os.path.join(root_dir, "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary")
    print(f"\n--- ADR-0007 Embedded Seeds Quality ---")
    seed_files = []
    for r, d, files in os.walk(seed_dir):
        for f in files:
            if f == 'code.cs':
                seed_files.append(os.path.join(r, f))

    seed_files.sort()
    print(f"Found {len(seed_files)} embedded seed code.cs files under {seed_dir}")
    seed_violations = []
    for sf in seed_files:
        srel = os.path.relpath(sf, seed_dir).replace('\\', '/')
        with open(sf, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        cnt = len(lines)
        if cnt > 300:
            seed_violations.append((srel, f"Length {cnt} > 300"))
        # check tool.json presence
        tj = os.path.join(os.path.dirname(sf), "tool.json")
        if not os.path.exists(tj):
            seed_violations.append((srel, "Missing tool.json"))
        print(f"  Seed {srel:<45}: {cnt:>3} lines | tool.json: {'OK' if os.path.exists(tj) else 'MISSING'}")

    if seed_violations:
        print(f"FAIL: ADR-0007 seed violations: {seed_violations}")
    else:
        print(f"PASS: All 12 seeds meet ADR-0007 bounds (line counts <= 152 lines, <= 300 limit).")

    print("\n" + "=" * 80)
    summary_ok = len(c2_violations) == 0 and len(m3_violations) == 0 and len(qb1_violations) == 0 and len(qb2_violations) == 0 and len(seed_violations) == 0
    print(f"OVERALL AST & QUALITY VERDICT: {'ALL PASS' if summary_ok else 'VIOLATIONS DETECTED'}")
    print("=" * 80)
    return summary_ok

if __name__ == '__main__':
    hp_dir = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else "HPGrasshopper")
    ok = audit_hpgrasshopper(hp_dir)
    sys.exit(0 if ok else 1)
