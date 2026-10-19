import os
import re

files = [
    r'HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs',
    r'HPBim/src/HPBim.Client/Recovery/ReconcileService.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs',
    r'HPBim/tests/HPBim.Client.Tests/Cli/GenerateCommandColdProcessRerunTests.cs',
    r'HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs',
    r'HPBim/tests/HPBim.Client.Tests/Recovery/ChallengerM2AdversarialPayloadTests.cs',
    r'HPBim/tests/HPBim.E2E.Tests/Tiers/Tier4_RealWorldWorkloadTests.cs'
]

def analyze_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    methods = []
    depth = 0
    method_start = None
    method_name = None
    in_arrow = False
    
    for idx, line in enumerate(lines, 1):
        s = line.strip()
        clean = re.sub(r'"(?:\\.|[^"\\])*"', '', s)
        clean = re.sub(r'//.*$', '', clean)
        
        open_b = clean.count('{')
        close_b = clean.count('}')
        
        # Method signature starting at depth 1 (inside class/struct)
        if depth == 1 and not method_start:
            if any(s.startswith(k) for k in [
                'public ', 'private ', 'protected ', 'internal ', 'static ', 'async '
            ]):
                if '(' in s and not any(k in s for k in ['class ', 'struct ', 'record ', 'interface ']):
                    method_start = idx
                    method_name = s.split('(')[0].split()[-1]
                    if '=>' in s:
                        in_arrow = True
                        if s.endswith(';'):
                            methods.append((method_name, idx, idx, 1))
                            method_start = None
                            method_name = None
                            in_arrow = False
        
        if in_arrow:
            if clean.endswith(';'):
                length = idx - method_start + 1
                methods.append((method_name, method_start, idx, length))
                method_start = None
                method_name = None
                in_arrow = False
            continue

        depth += open_b - close_b
        
        if method_start and depth == 1 and close_b > 0:
            length = idx - method_start + 1
            methods.append((method_name, method_start, idx, length))
            method_start = None
            method_name = None

    return methods

all_pass = True
for f in files:
    print(f'=== {f} ===')
    m_list = analyze_methods(f)
    for name, start, end, count in m_list:
        status = 'PASS' if count <= 40 else 'FAIL (>40)'
        if count > 40:
            all_pass = False
        print(f'  {name} ({start}-{end}): {count} lines [{status}]')

print('\n' + '='*50)
if all_pass:
    print('VERDICT: 100% of methods are <= 40 lines (PASS)')
else:
    print('VERDICT: FAIL - some methods exceed 40 lines')
assert all_pass
