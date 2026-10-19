import os
import re

PROD_FILES = [
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
    r"HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.cs",
    r"HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.Structural.cs",
    r"HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.ProfileGrid.cs",
]

REPO_ROOT = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def parse_methods_in_file(relpath):
    fullpath = os.path.join(REPO_ROOT, relpath)
    with open(fullpath, 'r', encoding='utf-8-sig', errors='replace') as f:
        lines = f.readlines()

    # Track methods using exact line by line scanning
    methods = []
    # Identify method boundaries
    # Standard C# regex for methods, ctors
    sig_re = re.compile(r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|partial|\s+)+\s+([A-Za-z0-9_<>,?\[\]\s]+)\s+([A-Za-z0-9_]+)\s*\([^;]*\)(\s*where\s+[^{]+)?\s*$')
    ctor_re = re.compile(r'^\s*(public|private|protected|internal)\s+([A-Za-z0-9_]+)\s*\([^;]*\)\s*(:\s*(base|this)\s*\([^)]*\))?\s*$')
    arrow_re = re.compile(r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|\s+)+\s+([A-Za-z0-9_<>,?\[\]\s]+)\s+([A-Za-z0-9_]+)\s*\([^;]*\)\s*=>')

    i = 0
    while i < len(lines):
        line = lines[i]
        # Ignore comments and attributes
        if line.strip().startswith('//') or line.strip().startswith('/*') or line.strip().startswith('[') or not line.strip():
            i += 1
            continue

        m_arrow = arrow_re.search(line)
        if m_arrow:
            start_l = i + 1
            k = i
            # Find terminating semicolon
            # Handle multiline string literals
            in_raw_string = False
            while k < len(lines):
                if '"""' in lines[k]:
                    in_raw_string = not in_raw_string
                if not in_raw_string and ';' in lines[k]:
                    break
                k += 1
            end_l = k + 1
            methods.append((m_arrow.group(3), start_l, end_l, end_l - start_l + 1))
            i = end_l
            continue

        m_sig = sig_re.search(line) or ctor_re.search(line)
        if m_sig and not line.strip().endswith(';'):
            name = m_sig.group(3) if sig_re.search(line) else m_sig.group(2)
            start_l = i + 1
            k = i
            found_brace = False
            while k < min(i + 5, len(lines)):
                if '{' in lines[k]:
                    found_brace = True
                    break
                k += 1
            if found_brace:
                depth = 0
                started = False
                end_l = start_l
                in_raw_string = False
                for j in range(k, len(lines)):
                    cur = lines[j]
                    if '"""' in cur:
                        in_raw_string = not in_raw_string
                    if not in_raw_string:
                        depth += cur.count('{')
                        depth -= cur.count('}')
                        if depth > 0:
                            started = True
                        if started and depth == 0:
                            end_l = j + 1
                            break
                methods.append((name, start_l, end_l, end_l - start_l + 1))
                i = max(i + 1, end_l)
                continue
        i += 1
    return methods

def main():
    print(f"Auditing methods in all {len(PROD_FILES)} Wave 2 production files...\n")
    all_pass = True
    for rel in PROD_FILES:
        methods = parse_methods_in_file(rel)
        fname = os.path.basename(rel)
        print(f"File: {fname} ({len(methods)} methods)")
        for name, start_l, end_l, length in methods:
            status = "PASS" if length <= 50 else "FAIL (>50)"
            if length > 50:
                all_pass = False
            # Check target <= 40
            target_flag = " (target <= 40 PASS)" if length <= 40 else " (target 40 exceeded, <=50 PASS)"
            print(f"  - {name} [L{start_l}..L{end_l}]: {length} lines [{status}]{target_flag if length <= 50 else ''}")
        print()
    if all_pass:
        print(">>> VERDICT: ALL PRODUCTION METHODS SATISFY RULE M3 (<= 50 lines)! <<<")
    else:
        print(">>> VERDICT: RULE M3 VIOLATION FOUND! <<<")

if __name__ == '__main__':
    main()
