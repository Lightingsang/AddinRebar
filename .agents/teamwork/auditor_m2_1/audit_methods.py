import os
import re

files = [
    'HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs',
    'HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs',
    'HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs',
    'HPBim/src/HPBim.Client/Recovery/ReconcileService.cs',
    'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs',
    'HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs',
    'HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ExtensibleStorageReceiptReaderTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReceiptReaderTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReconcileIdempotencyTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileReceiptCompletenessTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileDocumentIdentityTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Cli/GenerateCommandColdProcessRerunTests.cs',
    'HPBim/tests/HPBim.E2E.Tests/Tiers/Tier4_RealWorldWorkloadTests.cs'
]

def analyze_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    methods = []
    brace_depth = 0
    in_method = False
    method_start = -1
    method_sig = ''
    
    # Method pattern
    method_decl_pat = re.compile(
        r'^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|readonly|unsafe)\s+)+'
        r'(?:[\w\<\>\[\],\?\s]+)\s+([A-Z]\w*)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
    )
    ctor_pat = re.compile(
        r'^\s*(?:(?:public|private|protected|internal)\s+)+([A-Z]\w*)\s*\([^\)]*\)\s*(?::\s*(?:base|this)\([^\)]*\))?$'
    )

    for i, line in enumerate(lines):
        line_num = i + 1
        stripped = line.strip()

        # Check potential signature if not in method
        if not in_method and brace_depth in (1, 2):
            m = method_decl_pat.match(stripped) or ctor_pat.match(stripped)
            if m and not any(kw in stripped for kw in ['class ', 'interface ', 'struct ', 'enum ', 'record ']):
                method_start = line_num
                method_sig = m.group(1)
        
        # Count braces
        cleaned = re.sub(r'"(\\.|[^"])*"', '""', line)
        cleaned = re.sub(r'//.*$', '', cleaned)
        for ch in cleaned:
            if ch == '{':
                brace_depth += 1
                if method_start != -1 and not in_method:
                    in_method = True
            elif ch == '}':
                brace_depth -= 1
                if in_method and brace_depth < (2 if any('namespace' in l for l in lines[:10]) else 1):
                    method_end = line_num
                    length = method_end - method_start + 1
                    methods.append((method_sig, method_start, method_end, length))
                    in_method = False
                    method_start = -1
    return methods

all_methods = []
violations = []
for f in files:
    m_list = analyze_methods(f)
    for sig, start, end, length in m_list:
        all_methods.append((f, sig, start, end, length))
        if length > 50:
            violations.append((f, sig, start, end, length))

print(f"Total methods analyzed across {len(files)} files: {len(all_methods)}")
if not violations:
    print("PASS: All methods are <= 50 lines (Rule M3 compliant)!")
    all_methods.sort(key=lambda x: x[4], reverse=True)
    print("\nTop 10 longest methods:")
    for f, sig, start, end, length in all_methods[:10]:
        print(f"  {length:2d} lines: {sig} ({f}:{start}-{end})")
else:
    print(f"VIOLATIONS ({len(violations)}):")
    for f, sig, start, end, length in violations:
        print(f"  FAIL ({length} lines > 50): {sig} in {f}:{start}-{end}")
