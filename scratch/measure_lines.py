import os
import re

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

def analyze_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    total_lines = len(lines)
    methods = []
    
    # Simple brace-based scanner for methods
    method_sig_pattern = re.compile(
        r'^\s*(?:\[.*?\]\s*)*(?:public|private|protected|internal|static|virtual|override|async|sealed|partial|\s)+\s+([\w\<\>\[\],\s\?]+?)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\((.*?)\)\s*(?:where\s+.*)?$'
    )
    ctor_pattern = re.compile(
        r'^\s*(?:\[.*?\]\s*)*(?:public|private|protected|internal|static|\s)+\s+([A-Za-z0-9_]+)\s*\((.*?)\)\s*(?::\s*(?:base|this)\(.*?\))?$'
    )

    i = 0
    while i < total_lines:
        line = lines[i]
        stripped = line.strip()
        
        is_class = re.search(r'\b(class|struct|record|interface|enum)\b', stripped)
        
        m_sig = method_sig_pattern.match(stripped)
        m_ctor = ctor_pattern.match(stripped) if not is_class else None
        
        if (m_sig or m_ctor) and not is_class and not stripped.startswith('//'):
            method_name = m_sig.group(2) if m_sig else m_ctor.group(1)
            sig_start = i + 1
            
            # Check for expression-bodied or block body
            body_start = -1
            j = i
            while j < min(i + 10, total_lines):
                line_j = lines[j]
                if '=>' in line_j:
                    k = j
                    while k < min(j + 15, total_lines) and ';' not in lines[k]:
                        k += 1
                    methods.append({
                        'name': method_name,
                        'start': sig_start,
                        'end': k + 1,
                        'lines': (k + 1) - sig_start + 1
                    })
                    i = k
                    break
                elif '{' in line_j:
                    body_start = j
                    break
                j += 1
            
            if body_start != -1 and (not methods or methods[-1]['start'] != sig_start):
                depth = 0
                k = body_start
                while k < total_lines:
                    l = lines[k]
                    depth += l.count('{') - l.count('}')
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
        
    return total_lines, methods

print("=" * 80)
print("LINE COUNT AND METHOD LENGTH AUDIT (RULES C2 & M3)")
print("=" * 80)

all_passed_c2 = True
all_passed_m3 = True

for tf in target_files:
    if not os.path.exists(tf):
        print(f"MISSING: {tf}")
        continue
    tl, meths = analyze_file(tf)
    c2_pass = tl <= 300
    if not c2_pass:
        all_passed_c2 = False
    
    over_m3 = [m for m in meths if m['lines'] > 50]
    if over_m3:
        all_passed_m3 = False
    
    max_m_len = max([m['lines'] for m in meths]) if meths else 0
    max_m_name = [m['name'] for m in meths if m['lines'] == max_m_len][0] if meths else "N/A"
    
    status_c2 = "PASS" if c2_pass else "FAIL"
    status_m3 = "PASS" if not over_m3 else "FAIL"
    
    print(f"{tf}:")
    print(f"  File Lines: {tl}/300 [{status_c2}] | Methods: {len(meths)} | Max Method: {max_m_len} lines ({max_m_name}) [{status_m3}]")
    if over_m3:
        for om in over_m3:
            print(f"    VIOLATION M3: {om['name']} has {om['lines']} lines (lines {om['start']}-{om['end']})")

print("=" * 80)
print(f"Overall C2 (<= 300 lines): {'PASSED' if all_passed_c2 else 'FAILED'}")
print(f"Overall M3 (<= 50 lines):  {'PASSED' if all_passed_m3 else 'FAILED'}")
print("=" * 80)
