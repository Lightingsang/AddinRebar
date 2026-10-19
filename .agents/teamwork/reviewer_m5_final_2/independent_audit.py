#!/usr/bin/env python3
import os
import sys
import json
import re
import hashlib
from datetime import datetime

REPO_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
HPGRASSHOPPER_DIR = os.path.join(REPO_ROOT, "HPGrasshopper")
MCPSHARED_DIR = os.path.join(REPO_ROOT, "McpShared")
SEEDS_DIR = os.path.join(HPGRASSHOPPER_DIR, "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary")

def get_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        # Exclude obj and bin
        dirs[:] = [d for d in dirs if d not in ('obj', 'bin', '.vs')]
        for f in files:
            if f.endswith('.cs'):
                cs_files.append(os.path.join(root, f))
    return sorted(cs_files)

def tokenize_csharp(text):
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

        if ch.isspace():
            advance()
            continue

        if ch == '/' and peek(1) == '/':
            val = advance() + advance()
            while i < n and text[i] != '\n':
                val += advance()
            tokens.append(('LINE_COMMENT', val, start_line, start_col, line, col))
            continue

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

        # Verbatim string
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

        if ch in '{}();,[]<>=':
            val = advance()
            tokens.append(('PUNCT', val, start_line, start_col, line, col))
            continue

        if ch.isalpha() or ch == '_':
            val = advance()
            while i < n and (text[i].isalnum() or text[i] == '_'):
                val += advance()
            tokens.append(('IDENT', val, start_line, start_col, line, col))
            continue

        val = advance()
        tokens.append(('OTHER', val, start_line, start_col, line, col))

    return tokens

def find_csharp_methods(tokens):
    struct_tokens = [t for t in tokens if t[0] not in ('LINE_COMMENT', 'BLOCK_COMMENT')]
    methods = []
    CONTROL_FLOW = {'if', 'for', 'foreach', 'while', 'switch', 'using', 'catch', 'lock', 'fixed', 'checked', 'unchecked'}
    TYPE_DECLS = {'class', 'interface', 'struct', 'record', 'enum', 'namespace'}

    # Filter attributes
    filtered = []
    t_idx = 0
    while t_idx < len(struct_tokens):
        tok = struct_tokens[t_idx]
        if tok[0] == 'PUNCT' and tok[1] == '[':
            b_depth = 1
            b_end = t_idx + 1
            while b_end < len(struct_tokens) and b_depth > 0:
                if struct_tokens[b_end][0] == 'PUNCT':
                    if struct_tokens[b_end][1] == '[': b_depth += 1
                    elif struct_tokens[b_end][1] == ']': b_depth -= 1
                b_end += 1
            if b_depth == 0 and b_end < len(struct_tokens):
                if struct_tokens[b_end][0] == 'IDENT' or (struct_tokens[b_end][0] == 'PUNCT' and struct_tokens[b_end][1] == '['):
                    t_idx = b_end
                    continue
        filtered.append(tok)
        t_idx += 1

    struct_tokens = filtered
    n = len(struct_tokens)
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
                        if struct_tokens[curr][1] == '(': paren_depth += 1
                        elif struct_tokens[curr][1] == ')': paren_depth -= 1
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
                                    'body_span': end_line - body_start_line + 1,
                                    'is_arrow': True
                                })
                        else:
                            brace_depth = 1
                            b_curr = look + 1
                            while b_curr < n and brace_depth > 0:
                                if struct_tokens[b_curr][0] == 'PUNCT':
                                    if struct_tokens[b_curr][1] == '{': brace_depth += 1
                                    elif struct_tokens[b_curr][1] == '}': brace_depth -= 1
                                b_curr += 1

                            if brace_depth == 0:
                                end_line = struct_tokens[b_curr - 1][4]
                                methods.append({
                                    'name': ident_name,
                                    'start_line': decl_start_line,
                                    'end_line': end_line,
                                    'span': end_line - decl_start_line + 1,
                                    'body_span': end_line - body_start_line + 1,
                                    'is_arrow': False
                                })
                                idx = b_curr - 1
        idx += 1
    return methods

def check_commented_code(tokens):
    # Rule Q-B1: detect commented out code statements
    code_patterns = [
        re.compile(r'^\s*//\s*(if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|return\b|var\s+\w+\s*=|await\b|public\b|private\b|protected\b|new\s+\w+[\(<])'),
        re.compile(r'^\s*//\s*\w+\.\w+\('),
        re.compile(r'^\s*//\s*\w+\s*=\s*[^;]+;'),
    ]
    violations = []
    for tok in tokens:
        if tok[0] == 'LINE_COMMENT':
            val = tok[1]
            for pat in code_patterns:
                if pat.search(val):
                    # exclude doc comments /// and explanatory notes like // var x represents ...
                    if not val.startswith('///'):
                        # check if it looks like actual commented-out statement
                        if ';' in val or '{' in val or '}' in val:
                            violations.append((tok[2], val.strip()))
                            break
    return violations

def check_empty_catch(tokens):
    # Rule Q-B2: empty catch block without reason comment
    # Look for IDENT 'catch' followed by optional (...) followed by '{' then comments/empty then '}'
    violations = []
    n = len(tokens)
    for i in range(n):
        tok = tokens[i]
        if tok[0] == 'IDENT' and tok[1] == 'catch':
            # look ahead for '{'
            j = i + 1
            # skip through optional (...)
            if j < n and tokens[j][0] == 'PUNCT' and tokens[j][1] == '(':
                depth = 1
                j += 1
                while j < n and depth > 0:
                    if tokens[j][0] == 'PUNCT':
                        if tokens[j][1] == '(': depth += 1
                        elif tokens[j][1] == ')': depth -= 1
                    j += 1
            # next non-space should be '{'
            if j < n and tokens[j][0] == 'PUNCT' and tokens[j][1] == '{':
                brace_start = tokens[j][2]
                k = j + 1
                inner_tokens = []
                while k < n:
                    if tokens[k][0] == 'PUNCT' and tokens[k][1] == '}':
                        break
                    inner_tokens.append(tokens[k])
                    k += 1
                
                # Check inner tokens
                # If inner tokens only comments or whitespace
                code_tokens = [t for t in inner_tokens if t[0] not in ('LINE_COMMENT', 'BLOCK_COMMENT')]
                if len(code_tokens) == 0:
                    # It's an empty body! Does it have a comment explaining why?
                    comment_tokens = [t for t in inner_tokens if t[0] in ('LINE_COMMENT', 'BLOCK_COMMENT')]
                    if len(comment_tokens) == 0:
                        violations.append((brace_start, "Completely empty catch block"))
                    else:
                        comment_text = " ".join([t[1] for t in comment_tokens]).lower()
                        # Verify comment provides reason
                        if len(comment_text.strip()) < 5:
                            violations.append((brace_start, f"Catch block has trivial comment: {comment_text}"))
    return violations

def audit_all():
    print("=" * 80)
    print("AUDIT: HPGrasshopper C# Files (Rules C2, M3, Q-B1, Q-B2)")
    print("=" * 80)

    cs_files = get_cs_files(HPGRASSHOPPER_DIR)
    print(f"Total C# files found in HPGrasshopper/: {len(cs_files)}")

    c2_violations = []
    m3_violations = []
    qb1_violations = []
    qb2_violations = []

    for path in cs_files:
        rel = os.path.relpath(path, REPO_ROOT).replace('\\', '/')
        with open(path, 'r', encoding='utf-8') as f:
            content = f.read()

        lines = content.splitlines()
        if len(lines) > 300:
            c2_violations.append((rel, len(lines)))

        tokens = tokenize_csharp(content)
        methods = find_csharp_methods(tokens)
        for m in methods:
            if m['span'] > 50 or m['body_span'] > 50:
                m3_violations.append((rel, m['name'], m['start_line'], m['end_line'], m['span'], m['body_span']))

        # Check Q-B1
        dead = check_commented_code(tokens)
        if dead:
            for lno, text in dead:
                qb1_violations.append((rel, lno, text))

        # Check Q-B2
        catches = check_empty_catch(tokens)
        if catches:
            for lno, desc in catches:
                qb2_violations.append((rel, lno, desc))

    print(f"\n1. Rule C2 (File Length <= 300 lines): {len(c2_violations)} violations")
    for r, l in c2_violations:
        print(f"   [FAIL] {r}: {l} lines")
    if not c2_violations:
        print("   PASS: All files <= 300 lines.")

    print(f"\n2. Rule M3 (Method Length <= 50 lines): {len(m3_violations)} violations")
    for r, name, sl, el, span, bspan in m3_violations:
        print(f"   [FAIL] {r}:{sl}-{el} {name}() span={span}, body={bspan}")
    if not m3_violations:
        print("   PASS: All methods <= 50 lines.")

    print(f"\n3. Rule Q-B1 (Commented-out Dead Code): {len(qb1_violations)} violations")
    for r, lno, text in qb1_violations:
        print(f"   [FAIL] {r}:{lno}: {text}")
    if not qb1_violations:
        print("   PASS: Zero commented-out dead code detected.")

    print(f"\n4. Rule Q-B2 (Unannotated Empty Catch Blocks): {len(qb2_violations)} violations")
    for r, lno, desc in qb2_violations:
        print(f"   [FAIL] {r}:{lno}: {desc}")
    if not qb2_violations:
        print("   PASS: Zero unannotated empty catch blocks.")

    print("\n" + "=" * 80)
    print("AUDIT: ADR-0007 Embedded Seeds Compliance")
    print("=" * 80)

    seeds = []
    for root, dirs, files in os.walk(SEEDS_DIR):
        if 'tool.json' in files and 'code.cs' in files:
            seeds.append(root)

    print(f"Found {len(seeds)} embedded seeds under {SEEDS_DIR}")
    seed_issues = []

    for s in sorted(seeds):
        rel_s = os.path.relpath(s, REPO_ROOT).replace('\\', '/')
        tool_json_path = os.path.join(s, 'tool.json')
        code_cs_path = os.path.join(s, 'code.cs')

        # check line count of code.cs
        with open(code_cs_path, 'r', encoding='utf-8') as f:
            code_content = f.read()
        code_lines = len(code_content.splitlines())
        if code_lines > 300:
            seed_issues.append(f"{rel_s}: code.cs exceeds 300 lines ({code_lines})")

        # check tool.json
        try:
            with open(tool_json_path, 'r', encoding='utf-8') as f:
                meta = json.load(f)
            if 'name' not in meta or 'description' not in meta or 'inputSchema' not in meta:
                seed_issues.append(f"{rel_s}: tool.json missing required keys")
        except Exception as e:
            seed_issues.append(f"{rel_s}: invalid JSON ({e})")

        # compute sha256 of code.cs
        sha = hashlib.sha256(code_content.encode('utf-8')).hexdigest()
        print(f"   Seed: {os.path.basename(s):<32} | lines: {code_lines:<4} | sha256: {sha[:12]}...")

    if seed_issues:
        print(f"   [FAIL] Seed issues: {seed_issues}")
    else:
        print(f"   PASS: All {len(seeds)} seeds meet ADR-0007 criteria (<= 300 lines, valid tool.json).")

    print("\n" + "=" * 80)
    print("AUDIT: McpShared Invariance Verification")
    print("=" * 80)

    # Check git status for McpShared files modified since 2026-10-12
    cutoff = datetime(2026, 10, 12, 0, 0, 0)
    touched_after_cutoff = []
    for root, dirs, files in os.walk(MCPSHARED_DIR):
        dirs[:] = [d for d in dirs if d not in ('obj', 'bin', '.vs')]
        for f in files:
            p = os.path.join(root, f)
            mtime = datetime.fromtimestamp(os.path.getmtime(p))
            if mtime >= cutoff:
                touched_after_cutoff.append((os.path.relpath(p, REPO_ROOT).replace('\\', '/'), mtime))

    print(f"Files in McpShared modified on/after {cutoff.isoformat()}: {len(touched_after_cutoff)}")
    for p, mt in touched_after_cutoff:
        print(f"   MODIFIED RECENTLY: {p} (mtime: {mt})")

    if not touched_after_cutoff:
        print("   PASS: McpShared has ZERO files created or modified during orchestrator_14.")

if __name__ == '__main__':
    audit_all()
