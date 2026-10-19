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

root_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("DEEP FORENSIC SCAN FOR FORBIDDEN PATTERNS")
print("=" * 80)

commented_code_pattern = re.compile(r'^\s*//\s*(?:var|int|string|bool|if|for|foreach|while|return|throw|using|new|try|catch)\b')
not_implemented_pattern = re.compile(r'throw\s+new\s+NotImplementedException')
assert_true_true_pattern = re.compile(r'Assert\.(?:True\(true\)|False\(false\)|Equal\((?:1,\s*1|"pass",\s*"pass")\))')

total_issues = 0

for rel in FILES:
    full = os.path.join(root_dir, rel)
    with open(full, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        content = "".join(lines)
    
    file_issues = []
    
    # 1. Commented-out code
    for idx, line in enumerate(lines, 1):
        # Exclude schema comments or docs
        if "///" in line:
            continue
        if commented_code_pattern.search(line):
            file_issues.append(f"Line {idx}: Potential commented-out code: {line.strip()}")
            
    # 2. NotImplementedException
    for idx, line in enumerate(lines, 1):
        if not_implemented_pattern.search(line):
            file_issues.append(f"Line {idx}: NotImplementedException facade: {line.strip()}")
            
    # 3. Assert.True(true)
    for idx, line in enumerate(lines, 1):
        if assert_true_true_pattern.search(line):
            file_issues.append(f"Line {idx}: Self-certifying assertion: {line.strip()}")
            
    # 4. Empty catch
    catches = re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
    for match in catches:
        body = match.group(1)
        # Check if statements exist or comments exist
        has_comment = '//' in body or '/*' in body
        has_statement = bool(re.sub(r'[\s;]', '', re.sub(r'//.*', '', body)))
        if not has_statement and not has_comment:
            start_pos = match.start()
            line_no = content[:start_pos].count('\n') + 1
            file_issues.append(f"Line {line_no}: Empty catch block without comment")
            
    if file_issues:
        print(f"ISSUES IN {rel}:")
        for iss in file_issues:
            print(f"  - {iss}")
        total_issues += len(file_issues)
    else:
        print(f"CLEAN: {rel}")

print("=" * 80)
print(f"Total issues found: {total_issues}")
