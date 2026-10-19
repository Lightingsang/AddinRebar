import os
import re

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
    """
    Tokenizes C# source code character-by-character into tokens tagged with line numbers,
    accurately tracking comments, normal strings, verbatim strings, raw multiline strings,
    and character literals.
    """
    n = len(code)
    i = 0
    line_no = 1
    
    # State tracking
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
            start_l = line_no
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
            
        # Raw string literal """ (3 or more double quotes)
        if c == '"' and i + 2 < n and code[i+1] == '"' and code[i+2] == '"':
            # Count opening quotes
            q_count = 0
            while i + q_count < n and code[i + q_count] == '"':
                q_count += 1
            i += q_count
            start_l = line_no
            # Scan until matching quote count
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
            if c == '@' and code[i+1] == '"':
                i += 2
            else:
                i += 3
            start_l = line_no
            while i < n:
                if code[i] == '\n':
                    line_no += 1
                    i += 1
                elif code[i] == '"':
                    if i + 1 < n and code[i+1] == '"':
                        i += 2  # escaped ""
                    else:
                        i += 1
                        break
                else:
                    i += 1
            tokens.append(('STRING', start_l, line_no))
            continue
            
        # Regular or interpolated string "..." or $"..."
        if c == '"' or (c == '$' and i + 1 < n and code[i+1] == '"'):
            if c == '$':
                i += 2
            else:
                i += 1
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
            
        # Numbers or other
        i += 1
        
    return tokens

def find_csharp_methods(content):
    tokens = tokenize_csharp(content)
    methods = []
    
    # We look for method declarations:
    # A method declaration typically has:
    # [Modifiers] ReturnType MethodName ( ParameterList ) Body
    # where Body is either:
    #   { ... } (Block)
    #   => Expr ; (Expression body)
    
    MODIFIERS = {'public', 'private', 'protected', 'internal', 'static', 'async', 'override', 'virtual', 'sealed', 'partial'}
    
    t_idx = 0
    total_tokens = len(tokens)
    
    while t_idx < total_tokens:
        tok = tokens[t_idx]
        
        # Check if this token starts a potential member header
        if tok[0] == 'IDENT' and (tok[1] in MODIFIERS):
            header_start_line = tok[2]
            # Advance past modifiers
            curr = t_idx
            has_modifier = False
            while curr < total_tokens and tokens[curr][0] == 'IDENT' and tokens[curr][1] in MODIFIERS:
                has_modifier = True
                curr += 1
                
            if not has_modifier or curr >= total_tokens:
                t_idx += 1
                continue
                
            # Next is return type (or class/struct/record/enum/interface/event)
            if tokens[curr][0] == 'IDENT' and tokens[curr][1] in {'class', 'struct', 'record', 'interface', 'enum', 'event', 'delegate'}:
                t_idx = curr + 1
                continue
                
            # Scan type: can include generic <...> or arrays [] or ?
            # Return type can be e.g. Task<string>, List<int>, void, etc.
            # Then followed by MethodName, then '('
            type_start = curr
            # Scan until we find an identifier followed by '('
            method_ident_idx = -1
            paren_idx = -1
            
            look = curr
            angle_count = 0
            while look < total_tokens and tokens[look][2] <= header_start_line + 5: # allow multi-line header
                t = tokens[look]
                if t[0] == 'PUNCT' and t[1] == '<':
                    angle_count += 1
                elif t[0] == 'PUNCT' and t[1] == '>':
                    angle_count -= 1
                elif t[0] == 'PUNCT' and t[1] == '{' or (t[0] == 'PUNCT' and t[1] == ';'):
                    break
                elif t[0] == 'IDENT' and angle_count == 0:
                    # Check if next token is '('
                    if look + 1 < total_tokens and tokens[look+1][0] == 'PUNCT' and tokens[look+1][1] == '(':
                        # Found method name!
                        method_ident_idx = look
                        paren_idx = look + 1
                        break
                look += 1
                
            if method_ident_idx != -1 and paren_idx != -1:
                method_name = tokens[method_ident_idx][1]
                start_line = tokens[method_ident_idx][2]
                
                # Scan matching ')' for parameter list
                p_curr = paren_idx + 1
                p_depth = 1
                while p_curr < total_tokens and p_depth > 0:
                    if tokens[p_curr][0] == 'PUNCT' and tokens[p_curr][1] == '(':
                        p_depth += 1
                    elif tokens[p_curr][0] == 'PUNCT' and tokens[p_curr][1] == ')':
                        p_depth -= 1
                    p_curr += 1
                    
                if p_depth == 0 and p_curr < total_tokens:
                    # Now after ')', check if next is '{' or '=>'
                    # skip attributes or where clauses if any
                    body_curr = p_curr
                    while body_curr < total_tokens and not (tokens[body_curr][0] == 'PUNCT' and tokens[body_curr][1] in {'{', ';', '='}):
                        body_curr += 1
                        
                    if body_curr < total_tokens:
                        # Check block body '{'
                        if tokens[body_curr][0] == 'PUNCT' and tokens[body_curr][1] == '{':
                            # Scan to matching '}'
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
                            
                        # Check expression body '=>'
                        elif tokens[body_curr][0] == 'PUNCT' and tokens[body_curr][1] == '=' and \
                             body_curr + 1 < total_tokens and tokens[body_curr+1][0] == 'PUNCT' and tokens[body_curr+1][1] == '>':
                            # Expression body ends at the next semicolon ';' (which is NOT inside a string/comment because our tokenizer already stripped them!)
                            e_scan = body_curr + 2
                            while e_scan < total_tokens:
                                if tokens[e_scan][0] == 'PUNCT' and tokens[e_scan][1] == ';':
                                    end_line = tokens[e_scan][2]
                                    methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                                    t_idx = e_scan + 1
                                    break
                                elif tokens[e_scan][0] == 'PUNCT' and tokens[e_scan][1] == '}':
                                    # Fallback safety if missing ';'
                                    end_line = tokens[e_scan][2]
                                    methods.append((method_name, start_line, end_line, end_line - start_line + 1))
                                    t_idx = e_scan
                                    break
                                e_scan += 1
                            continue
        t_idx += 1
    return methods

if __name__ == "__main__":
    root_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
    print(f"{'File':<55} | {'Max Method':<30} | {'Status':<10}")
    print("-" * 105)
    for rel in FILES:
        p = os.path.join(root_dir, rel)
        with open(p, 'r', encoding='utf-8') as f:
            code = f.read()
        ms = find_csharp_methods(code)
        max_len = max([m[3] for m in ms]) if ms else 0
        top = [m for m in ms if m[3] == max_len][0] if ms else None
        info = f"{top[0]} ({max_len}L: {top[1]}-{top[2]})" if top else "N/A"
        status = "PASS" if max_len <= 50 else "FAIL"
        print(f"{os.path.basename(rel):<55} | {info:<30} | {status:<10}")
