import os
import re
import sys

target_files = [
    'HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs',
    'HPBim/src/HPBim.Client/Pipeline/DocumentIdentityQuery.cs',
    'HPBim/src/HPBim.Core/Execution/ElementPlanItem.cs',
    'HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs',
    'HPBim/src/HPBim.Core/Coordination/LedgerElementMapping.cs',
    'HPBim/src/HPBim.Core/Coordination/BimCoordinationLedger.cs',
    'HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs',
    'HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.Mappings.cs',
    'HPBim/src/HPBim.Core/Coordination/IdempotentElementResolver.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Structure.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Architecture.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.MepAndRebar.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs',
    'HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Pipeline/PreFlightGateTests.cs',
    'HPBim/tests/HPBim.Core.Tests/Coordination/BimCoordinationLedgerIdempotencyTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Pipeline/IdempotentExecutionTests.cs'
]

# Accurate method scanner
# Matches C# method signatures: modifiers, return type, name, generic parameters, argument list, where constraints
METHOD_SIG = re.compile(
    r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|virtual|override|async|sealed|partial|\s)+\s+([\w\<\>\[\],\s\?]+?)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\((.*?)\)\s*(?:where\s+.*)?$'
)
CTOR_SIG = re.compile(
    r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|\s)+\s+([A-Za-z0-9_]+)\s*\((.*?)\)\s*(?::\s*(?:base|this)\(.*?\))?$'
)

EMPTY_CATCH = re.compile(r'catch\s*(?:\([^\)]*\))?\s*\{\s*\}')

def audit_file(filepath):
    if not os.path.exists(filepath):
        return None
    
    with open(filepath, 'r', encoding='utf-8') as f:
        raw_lines = f.readlines()
        
    total_lines = len(raw_lines)
    
    methods = []
    empty_catches = []
    commented_code_lines = []
    
    # Check lines
    in_block_comment = False
    for idx, line in enumerate(raw_lines):
        line_num = idx + 1
        s = line.strip()
        
        # Block comment tracking
        if '/*' in s and '*/' not in s:
            in_block_comment = True
        elif '*/' in s and in_block_comment:
            in_block_comment = False
            continue
            
        if in_block_comment:
            continue
            
        # Commented-out code heuristic: comment line followed by typical code tokens
        if s.startswith('//') and not s.startswith('///'):
            comment_body = s[2:].strip()
            # check if it looks like code: var, if, for, return, throw, element., new, ;, {
            if re.match(r'^(?:var|if|for|foreach|while|return|throw|using|new|public|private)\s+.*[;\{]?$', comment_body) or \
               (comment_body.endswith(';') and len(comment_body) > 10):
                # Exclude purely explanatory comments
                if not re.search(r'\b(?:e\.g\.|note|todo|see|example)\b', comment_body, re.I):
                    commented_code_lines.append((line_num, s))
                    
    # Empty catch scan
    content = "".join(raw_lines)
    for m in re.finditer(r'catch\s*(?:\([^\)]*\))?\s*\{([^}]*)\}', content):
        body = m.group(1).strip()
        # Empty or only whitespace
        if not body:
            empty_catches.append(m.group(0))
        elif '//' not in body and '/*' not in body and not any(c.isalnum() for c in body):
            empty_catches.append(m.group(0))

    # Parse methods
    i = 0
    while i < total_lines:
        line = raw_lines[i]
        s = line.strip()
        
        is_type_decl = re.search(r'\b(class|struct|record|interface|enum)\b', s)
        
        m_sig = METHOD_SIG.match(s)
        m_ctor = CTOR_SIG.match(s) if not is_type_decl else None
        
        if (m_sig or m_ctor) and not is_type_decl and not s.startswith('//'):
            method_name = m_sig.group(2) if m_sig else m_ctor.group(1)
            sig_start = i + 1
            
            # Check for expression-bodied =>
            found_expr = False
            for j in range(i, min(i + 10, total_lines)):
                if '=>' in raw_lines[j]:
                    # scan until semicolon
                    k = j
                    while k < min(j + 20, total_lines) and ';' not in raw_lines[k]:
                        k += 1
                    methods.append({
                        'name': method_name,
                        'start': sig_start,
                        'end': k + 1,
                        'lines': (k + 1) - sig_start + 1
                    })
                    i = k
                    found_expr = True
                    break
                elif '{' in raw_lines[j]:
                    break
                    
            if not found_expr:
                # find body opening brace
                body_start = -1
                for j in range(i, min(i + 15, total_lines)):
                    if '{' in raw_lines[j]:
                        body_start = j
                        break
                        
                if body_start != -1:
                    depth = 0
                    k = body_start
                    while k < total_lines:
                        depth += raw_lines[k].count('{') - raw_lines[k].count('}')
                        if depth == 0:
                            methods.append({
                                'name': method_name,
                                'start': sig_start,
                                'end': k + 1,
                                'lines': (k + 1) - sig_start + 1
                            })
                            i = k
                            break
                        k += 1
        i += 1
        
    return {
        'filepath': filepath,
        'total_lines': total_lines,
        'methods': methods,
        'empty_catches': empty_catches,
        'commented_code_lines': commented_code_lines
    }

print("================================================================================")
print("INDEPENDENT REVIEW AUDIT: CLEAN CODE (RULES C2 & M3) & STATIC ANALYSIS")
print("================================================================================")

all_c2 = True
all_m3 = True
all_catches_clean = True
all_comments_clean = True

for path in target_files:
    res = audit_file(path)
    if not res:
        print(f"[MISSING] {path}")
        all_c2 = False
        continue
        
    c2_ok = res['total_lines'] <= 300
    if not c2_ok: all_c2 = False
    
    m3_violations = [m for m in res['methods'] if m['lines'] > 50]
    if m3_violations: all_m3 = False
    
    max_m_len = max([m['lines'] for m in res['methods']]) if res['methods'] else 0
    max_m_name = [m['name'] for m in res['methods'] if m['lines'] == max_m_len][0] if res['methods'] else "N/A"
    
    c2_status = "PASS" if c2_ok else "FAIL"
    m3_status = "PASS" if not m3_violations else "FAIL"
    
    print(f"\nFile: {path}")
    print(f"  Lines: {res['total_lines']}/300 [{c2_status}]")
    print(f"  Methods: {len(res['methods'])}, Max Method: {max_m_len} lines ({max_m_name}) [{m3_status}]")
    
    if m3_violations:
        for v in m3_violations:
            print(f"    [M3 VIOLATION] Method '{v['name']}': {v['lines']} lines (lines {v['start']}-{v['end']}) > 50 lines")
            
    if res['empty_catches']:
        all_catches_clean = False
        print(f"    [EMPTY CATCH VIOLATION] Found empty catch blocks: {res['empty_catches']}")
    else:
        print(f"  Empty Catch Blocks: NONE (PASS)")
        
    if res['commented_code_lines']:
        all_comments_clean = False
        print(f"    [COMMENTED CODE] Found possible commented code:")
        for ln, line_text in res['commented_code_lines']:
            print(f"      L{ln}: {line_text}")
    else:
        print(f"  Commented Code: NONE (PASS)")

print("\n" + "=" * 80)
print(f"Summary Rule C2 (all files <= 300 lines): {'PASSED' if all_c2 else 'FAILED'}")
print(f"Summary Rule M3 (all methods <= 50 lines): {'PASSED' if all_m3 else 'FAILED'}")
print(f"Summary Empty Catches (0 without reason):  {'PASSED' if all_catches_clean else 'FAILED'}")
print(f"Summary Commented-Out Code:               {'PASSED' if all_comments_clean else 'FAILED'}")
print("=" * 80)
