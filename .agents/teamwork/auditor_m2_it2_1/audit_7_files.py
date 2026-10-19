import os
import re

FILES = [
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery\ExtensibleStorageReceiptReader.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery\ReconcileService.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Cli\GenerateCommandColdProcessRerunTests.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Recovery\ChallengerM2AdversarialPayloadTests.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.E2E.Tests\Tiers\Tier4_RealWorldWorkloadTests.cs"
]

def check_file_lines(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    return len(lines), lines

def analyze_methods(filepath, lines):
    # Precise brace tracking to find all method/ctor declarations and their lengths
    method_pattern = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|async|override|virtual|sealed|\s)+\s+([\w\<\>\[\],\s\?]+?)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(([^;]*?)\)\s*(?:where\s+.*)?$'
    )
    ctor_pattern = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal)\s+([A-Za-z0-9_]+)\s*\(([^;]*?)\)\s*(?::\s*(?:base|this)\s*\([^)]*\))?\s*$'
    )

    methods = []
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        
        # Check if line matches method or ctor (or start of multi-line signature)
        # Note: avoid interface methods or abstract methods ending with ;
        sig_match = None
        method_name = None
        start_line = i + 1

        # Check single or multi-line header ending with { or no ;
        header = stripped
        j = i
        while j < len(lines) and not ('{' in header or ';' in header or '=>' in header):
            j += 1
            if j < len(lines):
                header += " " + lines[j].strip()

        # Check constructor
        m_ctor = ctor_pattern.match(header.split('{')[0].strip())
        if m_ctor and not header.strip().endswith(';'):
            method_name = m_ctor.group(1)
            sig_match = True

        if not sig_match:
            m_meth = method_pattern.match(header.split('{')[0].strip())
            if m_meth and not header.strip().endswith(';'):
                method_name = m_meth.group(2)
                sig_match = True

        # Check expression-bodied method e.g. public void Foo() => ...;
        if sig_match and '=>' in header and '{' not in header:
            # Expression bodied method
            end_line = j + 1
            length = end_line - start_line + 1
            methods.append((method_name, start_line, end_line, length))
            i = j + 1
            continue

        if sig_match:
            # Find the opening brace '{'
            brace_idx = j
            while brace_idx < len(lines) and '{' not in lines[brace_idx]:
                brace_idx += 1
            
            if brace_idx < len(lines):
                # Count braces
                depth = 0
                k = brace_idx
                found_start = False
                while k < len(lines):
                    l_content = lines[k]
                    # Simple string literal removal to avoid braces in strings
                    cleaned = re.sub(r'"(\\.|[^"\\])*"', '', l_content)
                    cleaned = re.sub(r'\'(\\.|[^\'\\])*\'', '', cleaned)
                    cleaned = re.sub(r'//.*', '', cleaned)
                    
                    depth += cleaned.count('{') - cleaned.count('}')
                    if '{' in cleaned:
                        found_start = True
                    if found_start and depth == 0:
                        end_line = k + 1
                        length = end_line - start_line + 1
                        methods.append((method_name, start_line, end_line, length))
                        i = k + 1
                        break
                    k += 1
                else:
                    i += 1
            else:
                i += 1
        else:
            i += 1

    return methods

def check_commented_code(lines):
    # Rule Q-B1: No commented-out code
    # Heuristics: // followed by code keywords like var, if, for, while, return, throw, new, try, catch, await, using
    violations = []
    code_keywords = re.compile(r'^\s*//\s*(var|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|return\s|throw\s|try\s*\{|catch\s*\(|await\s|using\s*\(|public\s|private\s|protected\s|internal\s|class\s|void\s|int\s|string\s|bool\s)')
    for idx, line in enumerate(lines):
        if code_keywords.match(line):
            # check if it's documentation or comment explaining something
            stripped = line.strip()
            violations.append((idx + 1, stripped))
    return violations

def check_unannotated_catches(lines):
    # Rule Q-B2: No unannotated catch blocks
    violations = []
    catch_pattern = re.compile(r'catch\s*(?:\([^\)]*\))?\s*(?:when\s*\([^\)]*\))?\s*\{')
    for idx, line in enumerate(lines):
        if 'catch' in line:
            # check the catch block body or line immediately before/after
            # A catch is unannotated if it is empty without comment, or swallows without explanation
            pass
    return violations

print("=== AUDIT START ===")
for path in FILES:
    fname = os.path.basename(path)
    count, lines = check_file_lines(path)
    print(f"\n--- File: {fname} ---")
    print(f"Total Lines: {count} (Rule C2 <= 300: {'PASS' if count <= 300 else 'FAIL'})")
    
    methods = analyze_methods(path, lines)
    print(f"Methods found: {len(methods)}")
    oversized = [m for m in methods if m[3] > 50]
    for m in methods:
        status = "PASS" if m[3] <= 50 else "FAIL (OVER 50)"
        warn = " (<= 40: PASS)" if m[3] <= 40 else " (> 40)"
        print(f"  {m[0]} (L{m[1]}-L{m[2]}): {m[3]} lines [{status}]{warn}")
    if oversized:
        print(f"  *** RULE M3 VIOLATIONS: {len(oversized)} methods exceed 50 lines ***")
    else:
        print(f"  Rule M3 Status: ALL METHODS <= 50 LINES (PASS)")

    commented = check_commented_code(lines)
    if commented:
        print(f"  Commented-out code suspicions: {len(commented)}")
        for line_no, c_line in commented:
            print(f"    Line {line_no}: {c_line}")
    else:
        print(f"  Rule Q-B1 (commented-out code): 0 violations (PASS)")

print("\n=== AUDIT COMPLETE ===")
