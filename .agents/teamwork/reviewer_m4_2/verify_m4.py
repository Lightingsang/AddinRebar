import os
import re
import sys

# Target files from M4
FILES_M4 = [
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

ROOT_DIR = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def strip_trivia_and_strings(line):
    l = re.sub(r'//.*', '', line)
    l = re.sub(r'"(\\.|[^"\\])*"', '""', l)
    return l

def analyze_csharp_members(content):
    """
    Parses methods, constructors, local functions, and properties with bodies.
    Detects any block from opening '{' to matching '}' or '=>' to ';'.
    """
    lines = content.splitlines()
    members = []

    # Patterns for methods and constructors
    method_sig = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:(?:public|private|protected|internal|static|async|override|virtual|sealed|partial)\s+)+([A-Za-z0-9_<>,\.\[\]\?]+)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\('
    )
    ctor_sig = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:(?:public|private|protected|internal|static)\s+)+([A-Za-z0-9_]+)\s*\('
    )

    i = 0
    while i < len(lines):
        line = lines[i]
        m_sig = method_sig.search(line)
        m_ctor = ctor_sig.search(line) if not m_sig else None

        # Check if line matches class/struct/interface/record/enum declaration
        if re.search(r'\b(class|struct|record|interface|enum)\b', line) and not ('(' in line and ')' in line and not m_ctor):
            i += 1
            continue

        if m_sig or m_ctor:
            name = m_sig.group(2) if m_sig else m_ctor.group(1)
            # Avoid keywords being treated as method names
            if name in ('if', 'for', 'foreach', 'while', 'switch', 'using', 'lock', 'catch'):
                i += 1
                continue

            start_line = i + 1
            brace_count = 0
            started = False
            end_line = start_line
            in_raw_string = False

            for j in range(i, len(lines)):
                raw_l = lines[j]
                if '"""' in raw_l:
                    in_raw_string = not in_raw_string
                    continue
                if in_raw_string:
                    continue

                clean_l = strip_trivia_and_strings(raw_l)
                if '{' in clean_l:
                    brace_count += clean_l.count('{')
                    started = True
                if '}' in clean_l:
                    brace_count -= clean_l.count('}')

                if started and brace_count == 0:
                    end_line = j + 1
                    break

            if not started and '=>' in line:
                for j in range(i, len(lines)):
                    if ';' in lines[j]:
                        end_line = j + 1
                        break

            length = end_line - start_line + 1
            members.append((name, start_line, end_line, length))
        i += 1

    return members

def check_empty_catches(content):
    catches = re.findall(r'catch\s*\([^\)]*\)\s*\{([^}]*)\}', content, re.DOTALL)
    violating = []
    for c in catches:
        cleaned = re.sub(r'//.*', '', c)
        cleaned = re.sub(r'/\*.*?\*/', '', cleaned, flags=re.DOTALL)
        if not cleaned.strip() and not ('//' in c or '/*' in c):
            violating.append(c)
    return violating

def check_commented_dead_code(content):
    """
    Looks for commented out C# statements (Rule D).
    """
    dead_code_patterns = [
        re.compile(r'^\s*//\s*(?:var|int|string|bool|double|float|return|if|for|foreach|while|throw|Assert\.)\s+[A-Za-z0-9_]'),
        re.compile(r'^\s*//\s*await\s+[A-Za-z0-9_]'),
        re.compile(r'^\s*//\s*\[(?:Fact|Theory|InlineData)\]'),
    ]
    findings = []
    lines = content.splitlines()
    for idx, l in enumerate(lines, 1):
        for pat in dead_code_patterns:
            if pat.search(l):
                findings.append((idx, l.strip()))
    return findings

print("=== VERIFYING M4 CLEAN CODE COMPLIANCE ===")
all_pass = True
for rel_path in FILES_M4:
    abs_path = os.path.join(ROOT_DIR, rel_path)
    if not os.path.exists(abs_path):
        print(f"[FAIL] File not found: {rel_path}")
        all_pass = False
        continue

    with open(abs_path, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.splitlines()
    total_lines = len(lines)
    members = analyze_csharp_members(content)

    max_len = 0
    longest_member = None
    for m in members:
        if m[3] > max_len:
            max_len = m[3]
            longest_member = m

    c2_ok = total_lines <= 300
    m3_ok = max_len <= 50

    empty_catches = check_empty_catches(content)
    dead_code = check_commented_dead_code(content)

    status = "PASS"
    if not c2_ok or not m3_ok or empty_catches or dead_code:
        status = "FAIL"
        all_pass = False

    info = f"{max_len} lines ({longest_member[0]} L{longest_member[1]}-L{longest_member[2]})" if longest_member else "none"
    print(f"[{status}] {rel_path} | Lines: {total_lines}/300 | MaxMethod: {info}")

    if empty_catches:
        print(f"       Empty catch violation: {empty_catches}")
    if dead_code:
        print(f"       Dead code violation: {dead_code}")

print("\n=== SUMMARY ===")
print("Overall Clean Code Compliance:", "PASS" if all_pass else "FAIL")
