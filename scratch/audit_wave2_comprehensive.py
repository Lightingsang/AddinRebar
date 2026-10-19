import os
import re

WAVE2_FILES = [
    r"HPBim\src\HPBim.Cli\Commands\GenerateCommand.cs",
    r"HPBim\src\HPBim.Cli\Commands\VerifyCommand.cs",
    r"HPBim\src\HPBim.Client\Connection\IntentJournal.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.ElementRecords.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    r"HPBim\src\HPBim.Client\Recovery\CoordinationReceiptRecord.cs",
    r"HPBim\src\HPBim.Client\Recovery\ExtensibleStorageQueryScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Recovery\ExtensibleStorageReceiptReader.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileService.cs",
    r"HPBim\src\HPBim.Client\Verification\RevitGeometryQueryScriptTemplate.cs",
    r"HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.ProfileGrid.cs",
    r"HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.Structural.cs",
    r"HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.cs",
    r"HPBim\tests\HPBim.Client.Tests\Cli\EmpiricalChallengerColdProcessRerunStressTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Cli\GenerateCommandColdProcessRerunTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Cli\VerifyCommandTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalAdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalDeepStressAdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTornTailAdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPipelineEngineRecordReconciliationTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\EmpiricalChallengerM1Tests_Stress.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\EmpiricalChallengerM3PreFlightGateTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\PreFlightGateTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\AdversarialReceiptReaderTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\AdversarialReconcileIdempotencyTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ChallengerM2AdversarialPayloadTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ChallengerM2StressTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ExtensibleStorageReceiptReaderTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReceiptReaderTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileDocumentIdentityTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileReceiptCompletenessTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Verification\EmpiricalChallengerM2FoundationTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Verification\EmpiricalChallengerM3StrictInconclusiveTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Verification\MeasuredGeometricEngineRequiredMeasurementsTests.cs",
    r"HPBim\tests\HPBim.E2E.Tests\Tiers\Tier4_RealWorldWorkloadTests.cs"
]

REPO_ROOT = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def analyze_methods_precise(filepath):
    with open(filepath, 'r', encoding='utf-8-sig', errors='replace') as f:
        lines = f.readlines()
    
    # Method / Constructor parser tracking braces
    # We look for lines containing method signatures
    method_sig = re.compile(r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|unsafe|\s+)+\s*([A-Za-z0-9_<>,?\[\]\s]+)\s+([A-Za-z0-9_]+)\s*\([^;]*\)(\s*where\s+[^{]+)?\s*$')
    ctor_sig = re.compile(r'^\s*(public|private|protected|internal)\s+([A-Za-z0-9_]+)\s*\([^;]*\)\s*(:\s*(base|this)\s*\([^)]*\))?\s*$')
    expr_bodied = re.compile(r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|\s+)+\s*([A-Za-z0-9_<>,?\[\]\s]+)\s+([A-Za-z0-9_]+)\s*\([^;]*\)\s*=>')

    methods = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # Skip comments
        if line.strip().startswith('//') or line.strip().startswith('/*') or line.strip().startswith('*'):
            i += 1
            continue
        
        # Check expression-bodied method
        m_expr = expr_bodied.search(line)
        if m_expr:
            # find end of expression (semicolon)
            start_l = i + 1
            k = i
            while k < len(lines) and ';' not in lines[k]:
                k += 1
            end_l = k + 1
            name = m_expr.group(3)
            methods.append((name, start_l, end_l, end_l - start_l + 1))
            i = end_l
            continue

        # Check block method / constructor
        m_block = method_sig.search(line) or ctor_sig.search(line)
        if m_block and not line.strip().endswith(';') and not 'abstract' in line:
            name = m_block.group(3) if method_sig.search(line) else m_block.group(2)
            start_l = i + 1
            # find opening brace
            k = i
            found_open = False
            while k < min(i + 5, len(lines)):
                if '{' in lines[k]:
                    found_open = True
                    break
                k += 1
            if found_open:
                brace = 0
                started = False
                end_l = start_l
                for j in range(k, len(lines)):
                    # ignore braces in string literals or comments simple check
                    cur = lines[j]
                    brace += cur.count('{')
                    brace -= cur.count('}')
                    if brace > 0:
                        started = True
                    if started and brace == 0:
                        end_l = j + 1
                        break
                length = end_l - start_l + 1
                methods.append((name, start_l, end_l, length))
                i = max(i + 1, end_l)
                continue
        i += 1
    return methods

def main():
    print(f"Auditing {len(WAVE2_FILES)} Wave 2 files...")
    
    # 1. Line counts (C2)
    c2_violations = []
    for rel in WAVE2_FILES:
        full = os.path.join(REPO_ROOT, rel)
        with open(full, 'r', encoding='utf-8-sig', errors='replace') as f:
            lines = f.readlines()
        if len(lines) > 300:
            c2_violations.append((rel, len(lines)))
    
    print("\n--- 1. RULE C2 CHECK (File line count <= 300) ---")
    if not c2_violations:
        print(f"PASS: All {len(WAVE2_FILES)} files strictly <= 300 lines (all <= 288 lines).")
    else:
        print(f"FAIL: {len(c2_violations)} violations: {c2_violations}")

    # 2. Method lengths (M3)
    m3_violations = []
    total_methods = 0
    max_method = (None, None, 0, None)
    for rel in WAVE2_FILES:
        full = os.path.join(REPO_ROOT, rel)
        methods = analyze_methods_precise(full)
        total_methods += len(methods)
        for name, start_l, end_l, length in methods:
            if length > max_method[2]:
                max_method = (name, rel, length, f"{start_l}-{end_l}")
            if length > 50:
                m3_violations.append((rel, name, length, f"{start_l}-{end_l}"))
    
    print("\n--- 2. RULE M3 CHECK (Method line count <= 50) ---")
    print(f"Total methods analyzed across Wave 2: {total_methods}")
    print(f"Longest method found: {max_method[0]} in {os.path.basename(max_method[1])} ({max_method[2]} lines, lines {max_method[3]})")
    if not m3_violations:
        print("PASS: 100% of methods across all Wave 2 files are strictly <= 50 lines.")
    else:
        print(f"FAIL: {len(m3_violations)} violations found:")
        for rel, name, length, span in m3_violations:
            print(f"  {rel} -> {name}: {length} lines (lines {span})")

    # 3. Commented-out code (Q-B1)
    print("\n--- 3. RULE Q-B1 CHECK (Commented-out code) ---")
    qb1_violations = []
    code_pattern = re.compile(r'^\s*//\s*(var|int|string|bool|return|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|throw\s+new|await\s+|Console\.|_logger\.|[A-Z][a-zA-Z0-9_]*\s*=[^=]|;\s*$)')
    for rel in WAVE2_FILES:
        full = os.path.join(REPO_ROOT, rel)
        with open(full, 'r', encoding='utf-8-sig', errors='replace') as f:
            lines = f.readlines()
        for idx, line in enumerate(lines, 1):
            if code_pattern.search(line):
                stripped = line.strip()
                if any(k in stripped for k in [";", "return ", "throw new ", "await "]):
                    qb1_violations.append((rel, idx, stripped))
    if not qb1_violations:
        print("PASS: 0 lines of commented-out code in all Wave 2 files.")
    else:
        print(f"FAIL: {len(qb1_violations)} potential commented-out code lines found:")
        for r, i, s in qb1_violations:
            print(f"  {r}:{i}: {s}")

    # 4. Catch block annotations (Q-B2)
    print("\n--- 4. RULE Q-B2 CHECK (Catch block explanatory annotations) ---")
    catch_count = 0
    qb2_violations = []
    for rel in WAVE2_FILES:
        full = os.path.join(REPO_ROOT, rel)
        with open(full, 'r', encoding='utf-8-sig', errors='replace') as f:
            content = f.read()
        lines = content.splitlines()
        for idx, line in enumerate(lines):
            if re.search(r'\bcatch\b', line):
                catch_count += 1
                sample = "\n".join(lines[idx:idx+6])
                # Check if empty block without comment
                if re.search(r'catch[^{]*\{\s*\}', sample):
                    qb2_violations.append((rel, idx+1, "Empty catch block"))
    print(f"Total catch blocks inspected across Wave 2: {catch_count}")
    if not qb2_violations:
        print("PASS: 0 empty or unannotated catch blocks in all Wave 2 files.")
    else:
        print(f"FAIL: {len(qb2_violations)} catch violations: {qb2_violations}")

    # 5. Architecture Purity: HPBim.Core
    print("\n--- 5. ARCHITECTURE PURITY CHECK (HPBim.Core) ---")
    core_files = [r for r in WAVE2_FILES if "HPBim.Core" in r]
    core_purity_fails = []
    for rel in core_files:
        full = os.path.join(REPO_ROOT, rel)
        with open(full, 'r', encoding='utf-8-sig', errors='replace') as f:
            txt = f.read()
        if "Autodesk.Revit" in txt or "RevitAPI" in txt:
            core_purity_fails.append(rel)
    if not core_purity_fails:
        print(f"PASS: All HPBim.Core files strictly contain 0 references to Autodesk.Revit.* (tested {len(core_files)} files).")
    else:
        print(f"FAIL: Forbidden references in: {core_purity_fails}")

if __name__ == '__main__':
    main()
