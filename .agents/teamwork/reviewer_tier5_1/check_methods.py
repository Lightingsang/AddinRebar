import re

files = [
    'HPBim/tests/HPBim.Core.Tests/Adversarial/BimModelSchemaValidatorAdversarialCoverageTests.cs',
    'HPBim/tests/HPBim.Core.Tests/Adversarial/IdempotentElementResolverAdversarialCoverageTests.cs',
    'HPBim/tests/HPBim.Core.Tests/Adversarial/CompositeSourceKeyAdversarialCoverageTests.cs',
    'HPBim/tests/HPBim.Core.Tests/Adversarial/MeasuredGeometricVerifierAdversarialCoverageTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Connection/IntentJournalTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Connection/TimeoutPolicyTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReceiptReaderTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Pipeline/BimPlanRunnerScriptTemplateTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineTier5AdversarialTests.cs'
]

def scan_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        text = f.read()

    chars = []
    i = 0
    n = len(text)
    
    in_line_comment = False
    in_block_comment = False
    in_string = False
    in_verbatim = False
    
    line_num = 1
    cleaned_tokens = []
    
    while i < n:
        c = text[i]
        if c == '\n':
            line_num += 1
            if in_line_comment:
                in_line_comment = False
            cleaned_tokens.append((' ', line_num))
            i += 1
            continue
            
        if in_line_comment:
            i += 1
            continue
            
        if in_block_comment:
            if c == '*' and i + 1 < n and text[i+1] == '/':
                in_block_comment = False
                i += 2
            else:
                i += 1
            continue
            
        if in_string:
            if c == '\\' and i + 1 < n:
                i += 2
                continue
            if c == '"':
                in_string = False
            i += 1
            continue
            
        if in_verbatim:
            if c == '"':
                if i + 1 < n and text[i+1] == '"':
                    i += 2
                    continue
                else:
                    in_verbatim = False
            i += 1
            continue
            
        if c == '/' and i + 1 < n:
            if text[i+1] == '/':
                in_line_comment = True
                i += 2
                continue
            elif text[i+1] == '*':
                in_block_comment = True
                i += 2
                continue
                
        if c == '@' and i + 1 < n and text[i+1] == '"':
            in_verbatim = True
            i += 2
            continue
        if c == '$' and i + 2 < n and text[i+1] == '@' and text[i+2] == '"':
            in_verbatim = True
            i += 3
            continue
        if c == '@' and i + 2 < n and text[i+1] == '$' and text[i+2] == '"':
            in_verbatim = True
            i += 3
            continue
            
        if c == '"':
            in_string = True
            i += 1
            continue
            
        cleaned_tokens.append((c, line_num))
        i += 1
        
    token_str = ''.join(c for c, _ in cleaned_tokens)
    
    pattern = re.compile(r'(?:public|private|protected|internal)?\s*(?:static\s+)?(?:async\s+)?(?:void|Task|Task<[^>]+>|[A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\([^)]*\)\s*\{')
    
    results = []
    for match in pattern.finditer(token_str):
        name = match.group(1)
        open_brace_idx = match.end() - 1
        start_char_idx = match.start()
        start_line = cleaned_tokens[start_char_idx][1]
        
        depth = 0
        end_line = start_line
        for idx in range(open_brace_idx, len(cleaned_tokens)):
            ch, ln = cleaned_tokens[idx]
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    end_line = ln
                    break
        results.append((name, start_line, end_line, end_line - start_line + 1))
        
    return results

total_all = 0
violations_all = 0

for f in files:
    res = scan_methods(f)
    print(f"=== {f} ({len(res)} methods) ===")
    for name, s, e, l in res:
        total_all += 1
        if l > 50:
            violations_all += 1
            print(f"  [VIOLATION >50 LINES] {name}: lines {s}-{e} ({l} lines)")
        elif l > 40:
            print(f"  [41-50 lines] {name}: lines {s}-{e} ({l} lines)")

print(f"\nTotal methods scanned: {total_all}, Total violations: {violations_all}")
