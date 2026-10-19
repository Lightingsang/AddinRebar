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

print("=== INDEPENDENT FORENSIC VERIFICATION OF LINE COUNTS ===")
all_exist = True
results = []

for tf in target_files:
    if not os.path.exists(tf):
        print(f"FILE MISSING: {tf}")
        all_exist = False
        continue
    with open(tf, 'r', encoding='utf-8') as f:
        content = f.read()
        lines = content.splitlines()
        line_count = len(lines)
    
    # Check for empty catch
    empty_catches = []
    catch_matches = re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
    for cm in catch_matches:
        body = cm.group(1).strip()
        if not body:
            empty_catches.append("Empty catch block with no content")
        elif not re.search(r'//|/\*|\bthrow\b|\blog\b|\bConsole\b|\breturn\b', body):
            empty_catches.append(f"Suspicious catch block: {body}")
            
    # Check for commented-out code (heuristic: multiple lines starting with // containing code keywords like ;, {, }, var, return)
    commented_code_lines = []
    for idx, line in enumerate(lines, 1):
        s = line.strip()
        if s.startswith('//') and not s.startswith('///'):
            rem = s[2:].strip()
            if re.search(r'(?:var|int|string|if\s*\(|for\s*\(|return|new\s+[\w<>]|throw\s+new)\b.*[;{}]', rem):
                commented_code_lines.append((idx, line))

    results.append({
        'path': tf,
        'lines': line_count,
        'empty_catches': empty_catches,
        'commented_code': commented_code_lines
    })

print(f"{'File Path':<65} | {'Lines':<6} | {'C2 <= 300':<10} | {'Empty Catch':<12} | {'Commented Code'}")
print("-" * 120)
for r in results:
    c2 = "PASS" if r['lines'] <= 300 else "FAIL"
    ec = "NONE" if not r['empty_catches'] else f"FOUND ({len(r['empty_catches'])})"
    cc = "NONE" if not r['commented_code'] else f"FOUND ({len(r['commented_code'])})"
    print(f"{r['path']:<65} | {r['lines']:<6} | {c2:<10} | {ec:<12} | {cc}")

