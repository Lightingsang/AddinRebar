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

def analyze_csharp_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # We want to identify every method/constructor and count its lines
    # Let's track line by line
    total_lines = len(lines)
    methods = []
    
    # State tracking
    # We find candidates with signature keywords followed by { or =>
    i = 0
    while i < total_lines:
        line = lines[i]
        trimmed = line.strip()
        
        # Check if line looks like method or constructor
        # Matches: [modifiers] [return_type] Name([args]) [where ...] {
        # Avoid class, struct, record, interface, enum, property { get; set; }, namespace
        is_type_decl = re.search(r'\b(class|struct|record|interface|enum|namespace)\b', trimmed)
        
        # Method signature pattern:
        # e.g., public bool Matches(...)
        # e.g., public static BimExecutionPlan Create(...)
        # e.g., [Fact] public void Parse_ValidCanonicalKey()
        m_sig = re.search(r'^(?:\[.*?\]\s*)*(?:public|private|protected|internal|static|virtual|override|async|sealed|partial|\s)+\s+([\w\<\>\[\],\s\?]+?)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\((.*?)\)', trimmed)
        ctor_sig = re.search(r'^(?:\[.*?\]\s*)*(?:public|private|protected|internal|\s)+\s+([A-Za-z0-9_]+)\s*\((.*?)\)\s*(?::\s*(?:base|this)\(.*?\))?', trimmed)
        
        # Check if it's a property with { get; set; }
        is_property = re.search(r'\{\s*(?:get|set|init)\b', trimmed)
        
        if (m_sig or ctor_sig) and not is_type_decl and not is_property and not trimmed.startswith('//') and not trimmed.startswith('*'):
            name = m_sig.group(2) if m_sig else ctor_sig.group(1)
            sig_start_line = i + 1
            
            # Find the body: either '{' or '=>'
            # Look ahead across lines for { or =>
            body_start_line = -1
            is_expression_bodied = False
            
            j = i
            while j < min(i + 15, total_lines):
                cur_line = lines[j]
                if '=>' in cur_line:
                    is_expression_bodied = True
                    body_start_line = j
                    break
                elif '{' in cur_line:
                    body_start_line = j
                    break
                j += 1
                
            if body_start_line != -1:
                if is_expression_bodied:
                    # Find trailing semicolon
                    k = body_start_line
                    while k < total_lines:
                        if ';' in lines[k]:
                            end_line = k + 1
                            methods.append({
                                'name': name,
                                'start': sig_start_line,
                                'end': end_line,
                                'lines': end_line - sig_start_line + 1,
                                'type': 'expression'
                            })
                            i = k
                            break
                        k += 1
                else:
                    # Find matching closing brace
                    depth = 0
                    k = body_start_line
                    # Scan characters
                    found_open = False
                    while k < total_lines:
                        l = lines[k]
                        for ch in l:
                            if ch == '{':
                                depth += 1
                                found_open = True
                            elif ch == '}':
                                depth -= 1
                                if found_open and depth == 0:
                                    end_line = k + 1
                                    methods.append({
                                        'name': name,
                                        'start': sig_start_line,
                                        'end': end_line,
                                        'lines': end_line - sig_start_line + 1,
                                        'type': 'block'
                                    })
                                    i = k
                                    break
                        if found_open and depth == 0:
                            break
                        k += 1
        i += 1
        
    return total_lines, methods

print("=== METHOD LENGTH AUDIT (RULE M3 <= 50 LINES) ===")
violations_m3 = []
for tf in target_files:
    tl, methods = analyze_csharp_methods(tf)
    print(f"\nFile: {tf} (Total lines: {tl})")
    print(f"Detected {len(methods)} methods:")
    for m in methods:
        status = "PASS" if m['lines'] <= 50 else "VIOLATION"
        if m['lines'] > 50:
            violations_m3.append((tf, m))
        print(f"  [{status}] {m['name']:<35} Lines {m['start']:>3}-{m['end']:>3} ({m['lines']:>2} lines)")

print("\n" + "=" * 80)
if violations_m3:
    print(f"M3 VIOLATIONS DETECTED: {len(violations_m3)}")
    for tf, m in violations_m3:
        print(f"FAIL: {tf} -> {m['name']} ({m['lines']} lines)")
else:
    print("M3 AUDIT RESULT: ALL METHODS STRICTLY <= 50 LINES (PASS)")
print("=" * 80)
