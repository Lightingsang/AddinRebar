import os
import re

def strip_strings_and_comments(code):
    result = []
    i = 0
    n = len(code)
    in_block_comment = False
    in_line_comment = False
    in_raw_string = False
    in_string = False
    
    while i < n:
        if in_line_comment:
            if code[i] == '\n':
                in_line_comment = False
                result.append('\n')
            else:
                result.append(' ')
            i += 1
        elif in_block_comment:
            if i + 1 < n and code[i:i+2] == '*/':
                in_block_comment = False
                result.append('  ')
                i += 2
            else:
                if code[i] == '\n':
                    result.append('\n')
                else:
                    result.append(' ')
                i += 1
        elif in_raw_string:
            if i + 2 < n and code[i:i+3] == '"""':
                in_raw_string = False
                result.append('   ')
                i += 3
            else:
                if code[i] == '\n':
                    result.append('\n')
                else:
                    result.append(' ')
                i += 1
        elif in_string:
            if code[i] == '\\':
                result.append('  ')
                i += 2
            elif code[i] == '"':
                in_string = False
                result.append(' ')
                i += 1
            else:
                if code[i] == '\n':
                    result.append('\n')
                else:
                    result.append(' ')
                i += 1
        else:
            if i + 2 < n and code[i:i+3] == '"""':
                in_raw_string = True
                result.append('   ')
                i += 3
            elif i + 1 < n and code[i:i+2] == '//':
                in_line_comment = True
                result.append('  ')
                i += 2
            elif i + 1 < n and code[i:i+2] == '/*':
                in_block_comment = True
                result.append('  ')
                i += 2
            elif code[i] == '"':
                in_string = True
                result.append(' ')
                i += 1
            else:
                result.append(code[i])
                i += 1
    return ''.join(result)

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

for f in files:
    if not os.path.exists(f):
        print(f"File not found: {f}")
        continue
    with open(f, 'r', encoding='utf-8') as fp:
        raw = fp.read()
    clean = strip_strings_and_comments(raw)
    raw_lines = raw.split('\n')
    clean_lines = clean.split('\n')
    
    i = 0
    oversized = []
    while i < len(clean_lines):
        line = clean_lines[i].strip()
        m = re.match(r'^(public|private|internal|protected)\s+.*?\b([A-Za-z0-9_]+)\s*\([^;]*$', line)
        if m and not line.endswith(';'):
            mname = m.group(2)
            s_line = i + 1
            brace_idx = i
            while brace_idx < len(clean_lines) and '{' not in clean_lines[brace_idx]:
                if clean_lines[brace_idx].strip().endswith(';'):
                    brace_idx = -1
                    break
                brace_idx += 1
            if brace_idx != -1 and brace_idx < len(clean_lines):
                bcount = 0
                for j in range(brace_idx, len(clean_lines)):
                    bcount += clean_lines[j].count('{')
                    bcount -= clean_lines[j].count('}')
                    if bcount == 0:
                        e_line = j + 1
                        cnt = e_line - s_line + 1
                        if cnt > 40:
                            oversized.append((mname, s_line, e_line, cnt))
                        i = j
                        break
        i += 1
    if oversized:
        print(f"{f}:")
        for name, s, e, cnt in oversized:
            status = "VIOLATION >50" if cnt > 50 else "WARN >40"
            print(f"   [{status}] {name:50} ({s:3d}-{e:3d}): {cnt:2d} lines")
