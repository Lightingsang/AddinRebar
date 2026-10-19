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

repo_root = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("=" * 80)
print("1. FILE LINE COUNTS (RULE C2 <= 300)")
print("=" * 80)
c2_failures = []
for rel_path in files:
    full_path = os.path.join(repo_root, rel_path)
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = len(lines)
    status = "PASS" if count <= 300 else "FAIL"
    if count > 300:
        c2_failures.append((rel_path, count))
    print(f"[{status}] {count:3d} lines : {rel_path}")

print(f"\nC2 Failures: {len(c2_failures)}")

print("\n" + "=" * 80)
print("2. METHOD LINE COUNTS (RULE M3 <= 50)")
print("=" * 80)

def find_all_methods(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    methods = []
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # Look for attributes
        attr_start = i + 1 if (line.startswith("[") and "]" in line) else None
        
        # Method or constructor pattern
        m_decl = re.search(r'\b(public|private|protected|internal|static|async|virtual|override|sealed)\b.*?\b([A-Za-z0-9_]+)\s*\(', line)
        if m_decl and not any(kw in line for kw in ["class ", "interface ", "struct ", "enum ", "record ", "namespace "]):
            name = m_decl.group(2)
            decl_start = i + 1
            start_line = attr_start if attr_start else decl_start
            
            # Find opening brace or =>
            k = i
            body_start = -1
            is_arrow = False
            while k < len(lines) and k < i + 10:
                l_k = lines[k]
                if "=>" in l_k:
                    is_arrow = True
                    body_start = k + 1
                    break
                elif "{" in l_k:
                    body_start = k + 1
                    break
                k += 1
            
            if body_start != -1:
                if is_arrow:
                    # scan for semicolon
                    m = k
                    while m < len(lines):
                        if ";" in lines[m]:
                            end_line = m + 1
                            methods.append((name, start_line, end_line, end_line - start_line + 1, "arrow"))
                            i = m
                            break
                        m += 1
                else:
                    # scan matching brace
                    depth = 0
                    m = k
                    found_open = False
                    while m < len(lines):
                        text = re.sub(r'//.*', '', lines[m])
                        text = re.sub(r'"(\\.|[^"])*"', '""', text)
                        for ch in text:
                            if ch == '{':
                                depth += 1
                                found_open = True
                            elif ch == '}':
                                depth -= 1
                                if found_open and depth == 0:
                                    end_line = m + 1
                                    methods.append((name, start_line, end_line, end_line - start_line + 1, "block"))
                                    i = m
                                    break
                        if found_open and depth == 0:
                            break
                        m += 1
        i += 1
    return methods

m3_failures = []
for rel_path in files:
    full_path = os.path.join(repo_root, rel_path)
    file_methods = find_all_methods(full_path)
    for name, start, end, span, kind in file_methods:
        if span > 50:
            m3_failures.append((rel_path, name, start, end, span))
            print(f"[FAIL] {rel_path}:{start}-{end} : {name} ({span} lines > 50)")

if not m3_failures:
    print("PASS: Zero methods > 50 lines found.")
else:
    print(f"\nTotal M3 failures: {len(m3_failures)}")
