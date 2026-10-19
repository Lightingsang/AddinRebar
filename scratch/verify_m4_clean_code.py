"""
Authentic Clean Code Verification Tool for Milestone M4 (HPBim)
Measures true physical line spans (EndLine - StartLine + 1) for every method,
properly handling C# 11 raw string literals (\"\"\"...\"\"\"), multiline strings,
expression-bodied methods, and comment trivia.
"""
import os
import re
import sys

FILES = [
    r"HPBim\src\HPBim.Client\Connection\IntentStatus.cs",
    r"HPBim\src\HPBim.Client\Connection\IntentJournalRecord.cs",
    r"HPBim\src\HPBim.Client\Connection\IIntentJournal.cs",
    r"HPBim\src\HPBim.Client\Connection\IntentJournal.cs",
    r"HPBim\src\HPBim.Client\Connection\TimeoutPolicy.cs",
    r"HPBim\src\HPBim.Client\Recovery\CoordinationReceiptRecord.cs",
    r"HPBim\src\HPBim.Client\Recovery\IExtensibleStorageReceiptReader.cs",
    r"HPBim\src\HPBim.Client\Recovery\ExtensibleStorageQueryScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Recovery\ExtensibleStorageReceiptReader.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileStatus.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileResult.cs",
    r"HPBim\src\HPBim.Client\Recovery\IReconcileService.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileService.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Receipt.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.IntentJournal.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\TimeoutPolicyTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ExtensibleStorageReceiptReaderTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\AutoReconcileIdempotencyTests.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineResult.cs",
    r"HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs",
]

def tokenize_csharp(code):
    n = len(code)
    i = 0
    line_no = 1
    tokens = []
    
    while i < n:
        c = code[i]
        
        # Newline
        if c == '\n':
            line_no += 1
            i += 1
            continue
        if c in ' \r\t':
            i += 1
            continue
            
        # Single-line comment
        if c == '/' and i + 1 < n and code[i+1] == '/':
            while i < n and code[i] != '\n':
                i += 1
            continue
            
        # Multi-line comment
        if c == '/' and i + 1 < n and code[i+1] == '*':
            i += 2
            while i < n:
                if code[i] == '\n':
                    line_no += 1
                elif code[i] == '*' and i + 1 < n and code[i+1] == '/':
                    i += 2
                    break
                i += 1
            continue
            
        # Raw string literal """ (or interpolated raw string literal $$""")
        d_count = 0
        while i + d_count < n and code[i + d_count] == '$':
            d_count += 1
        rem = i + d_count
        if rem + 2 < n and code[rem] == '"' and code[rem+1] == '"' and code[rem+2] == '"':
            q_count = 0
            while rem + q_count < n and code[rem + q_count] == '"':
                q_count += 1
            i = rem + q_count
            start_l = line_no
            while i < n:
                if code[i] == '\n':
                    line_no += 1
                    i += 1
                elif code[i] == '"':
                    closing = 0
                    while i + closing < n and code[i + closing] == '"':
                        closing += 1
                    if closing >= q_count:
                        i += closing
                        break
                    else:
                        i += closing
                else:
                    i += 1
            tokens.append(('STRING', start_l, line_no))
            continue
            
        # Verbatim string @"..." or $@"...
        if (c == '@' and i + 1 < n and code[i+1] == '"') or \
           (c == '$' and i + 2 < n and code[i+1] == '@' and code[i+2] == '"') or \
           (c == '@' and i + 2 < n and code[i+1] == '$' and code[i+2] == '"'):
            i += 2 if (c == '@' and code[i+1] == '"') else 3
            start_l = line_no
            while i < n:
                if code[i] == '\n':
                    line_no += 1
                    i += 1
                elif code[i] == '"':
                    if i + 1 < n and code[i+1] == '"':
                        i += 2
                    else:
                        i += 1
                        break
                else:
                    i += 1
            tokens.append(('STRING', start_l, line_no))
            continue
            
        # Regular or interpolated string "..." or $"..."
        if c == '"' or (c == '$' and i + 1 < n and code[i+1] == '"'):
            i += 2 if c == '$' else 1
            start_l = line_no
            while i < n:
                if code[i] == '\n':
                    line_no += 1
                    i += 1
                    break
                elif code[i] == '\\':
                    i += 2
                elif code[i] == '"':
                    i += 1
                    break
                else:
                    i += 1
            tokens.append(('STRING', start_l, line_no))
            continue
            
        # Character literal '.'
        if c == "'":
            i += 1
            while i < n:
                if code[i] == '\n':
                    line_no += 1
                    i += 1
                    break
                elif code[i] == '\\':
                    i += 2
                elif code[i] == "'":
                    i += 1
                    break
                else:
                    i += 1
            continue
            
        # Identifiers and keywords
        if c.isalpha() or c == '_':
            start_i = i
            while i < n and (code[i].isalnum() or code[i] == '_'):
                i += 1
            tokens.append(('IDENT', code[start_i:i], line_no))
            continue
            
        # Operators and punctuation
        if c in '{};()=><[],.:+-*!&|?':
            tokens.append(('PUNCT', c, line_no))
            i += 1
            continue
            
        i += 1
        
    return tokens

def analyze_csharp_methods(content):
    tokens = tokenize_csharp(content)
    methods = []
    
    MODIFIERS = {'public', 'private', 'protected', 'internal', 'static', 'async', 'override', 'virtual', 'sealed', 'partial'}
    
    t_idx = 0
    total_tokens = len(tokens)
    
    while t_idx < total_tokens:
        tok = tokens[t_idx]
        
        if tok[0] == 'IDENT' and (tok[1] in MODIFIERS):
            header_start_line = tok[2]
            curr = t_idx
            has_modifier = False
            while curr < total_tokens and tokens[curr][0] == 'IDENT' and tokens[curr][1] in MODIFIERS:
                has_modifier = True
                curr += 1
                
            if not has_modifier or curr >= total_tokens:
                t_idx += 1
                continue
                
            if tokens[curr][0] == 'IDENT' and tokens[curr][1] in {'class', 'struct', 'record', 'interface', 'enum', 'event', 'delegate'}:
                t_idx = curr + 1
                continue
                
            method_ident_idx = -1
            paren_idx = -1
            
            look = curr
            angle_count = 0
            while look < total_tokens and tokens[look][2] <= header_start_line + 5:
                t = tokens[look]
                if t[0] == 'PUNCT' and t[1] == '<':
                    angle_count += 1
                elif t[0] == 'PUNCT' and t[1] == '>':
                    angle_count -= 1
                elif t[0] == 'PUNCT' and t[1] in {'{', ';'}:
                    break
                elif t[0] == 'IDENT' and angle_count == 0:
                    if look + 1 < total_tokens and tokens[look+1][0] == 'PUNCT' and tokens[look+1][1] == '(':
                        method_ident_idx = look
                        paren_idx = look + 1
                        break
                look += 1
                
            if method_ident_idx != -1 and paren_idx != -1:
                method_name = tokens[method_ident_idx][1]
                start_line = tokens[method_ident_idx][2]
                
                p_curr = paren_idx + 1
                p_depth = 1
                while p_curr < total_tokens and p_depth > 0:
                    if tokens[p_curr][0] == 'PUNCT' and tokens[p_curr][1] == '(':
                        p_depth += 1
                    elif tokens[p_curr][0] == 'PUNCT' and tokens[p_curr][1] == ')':
                        p_depth -= 1
                    p_curr += 1
                    
                if p_depth == 0 and p_curr < total_tokens:
                    body_curr = p_curr
                    while body_curr < total_tokens and not (tokens[body_curr][0] == 'PUNCT' and tokens[body_curr][1] in {'{', ';', '='}):
                        body_curr += 1
                        
                    if body_curr < total_tokens:
                        # Block body '{ ... }'
                        if tokens[body_curr][0] == 'PUNCT' and tokens[body_curr][1] == '{':
                            b_depth = 1
                            b_scan = body_curr + 1
                            while b_scan < total_tokens and b_depth > 0:
                                if tokens[b_scan][0] == 'PUNCT' and tokens[b_scan][1] == '{':
                                    b_depth += 1
                                elif tokens[b_scan][0] == 'PUNCT' and tokens[b_scan][1] == '}':
                                    b_depth -= 1
                                b_scan += 1
                            end_line = tokens[b_scan - 1][2] if b_scan > 0 else start_line
                            methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                            t_idx = b_scan
                            continue
                            
                        # Expression body '=> ... ;'
                        elif tokens[body_curr][0] == 'PUNCT' and tokens[body_curr][1] == '=' and \
                             body_curr + 1 < total_tokens and tokens[body_curr+1][0] == 'PUNCT' and tokens[body_curr+1][1] == '>':
                            e_scan = body_curr + 2
                            while e_scan < total_tokens:
                                if tokens[e_scan][0] == 'PUNCT' and tokens[e_scan][1] == ';':
                                    end_line = tokens[e_scan][2]
                                    methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                                    t_idx = e_scan + 1
                                    break
                                elif tokens[e_scan][0] == 'PUNCT' and tokens[e_scan][1] == '}':
                                    end_line = tokens[e_scan][2]
                                    methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                                    t_idx = e_scan
                                    break
                                e_scan += 1
                            continue
        t_idx += 1
    return methods

def check_empty_catches(content):
    catches = re.findall(r'catch\s*\([^\)]*\)\s*\{([^}]*)\}', content, re.DOTALL)
    violating = []
    for c in catches:
        cleaned = re.sub(r'//.*', '', c)
        cleaned = re.sub(r'/\*.*?\*/', '', cleaned, flags=re.DOTALL)
        if not cleaned.strip() and not ('//' in c or '/*' in c):
            violating.append(c)
    return violating

if __name__ == "__main__":
    root_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

    print(f"{'File':<65} | {'Lines':<6} | {'Max Method':<30} | {'C2 (<=300)':<10} | {'M3 (<=50)':<10}")
    print("-" * 135)

    all_passed = True
    violations = []

    for rel_path in FILES:
        full_path = os.path.join(root_dir, rel_path)
        if not os.path.exists(full_path):
            print(f"MISSING: {rel_path}")
            all_passed = False
            continue
        
        with open(full_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        total_lines = len(content.splitlines())
        methods = analyze_csharp_methods(content)
        max_method_len = max([m[3] for m in methods]) if methods else 0
        longest_method = [m for m in methods if m[3] == max_method_len][0] if methods else None
        
        c2_pass = total_lines <= 300
        # Check all methods in file
        file_m3_pass = True
        for m in methods:
            if m[3] > 50:
                file_m3_pass = False
                violations.append(f"{rel_path}: {m[0]} ({m[3]} lines: L{m[1]}-L{m[2]}) > 50")
        
        if not c2_pass or not file_m3_pass:
            all_passed = False
            
        method_info = f"{max_method_len} ({longest_method[0]})" if longest_method else "N/A"
        print(f"{rel_path:<65} | {total_lines:<6} | {method_info:<30} | {'PASS' if c2_pass else 'FAIL':<10} | {'PASS' if file_m3_pass else 'FAIL':<10}")
        
        violating_catches = check_empty_catches(content)
        if violating_catches:
            print(f"  WARNING: Violating empty catch found in {rel_path}!")
            all_passed = False

    print("-" * 135)
    if all_passed:
        print("ALL 24 FILES PASS CLEAN CODE LIMITS: C2 <= 300 lines, M3 <= 50 lines, No Empty Catches!")
        sys.exit(0)
    else:
        print("FAILED CLEAN CODE LIMITS!")
        for v in violations:
            print("  VIOLATION: " + v)
        sys.exit(1)
