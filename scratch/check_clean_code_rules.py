import os
import re

files_worker_m1 = [
    r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary\Revit\generate_rebar_curves\code.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\ExecutionOutcome.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\RevitTransactionScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\CanvasUndoScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Host\GrasshopperAddinApplication.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\McpBridgeWorkRequest.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\GrasshopperExecutionEngine.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Regression\Milestone1RemediationEmpiricalTests.cs"
]

all_modified_today = [
    r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary\Revit\generate_rebar_curves\code.cs",
    r"HPGrasshopper\HPGrasshopper.Mcp.Server.Tests\Seeds\AdversarialSeedStressTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\McpBridgeWorkRequest.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\CanvasUndoScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\ExecutionOutcome.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\GrasshopperExecutionEngine.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\RevitTransactionScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Host\GrasshopperAddinApplication.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\FindingR3CommitOrderingAdversarialTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\Milestone2CommitOrderingAndRollbackLedgerTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fakes\TestScopeFactory.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Regression\ChallengerM1AdversarialTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Regression\Milestone1RemediationEmpiricalTests.cs"
]

def analyze_csharp(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        lines = content.splitlines()

    total_lines = len(lines)
    
    # 1. Method length (rough block tracking for methods)
    # Match method signatures: [public|private|protected|internal|static|async|override|sealed|virtual|void|Task|string|int|bool|CanvasRollbackResult|ExecutionOutcome...]
    # We will track braces
    long_methods = []
    
    # Simple brace-counting method parser
    method_sig_pattern = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|async|override|sealed|virtual|new|unsafe|\s)+'
        r'[\w\<\>\[\],\?\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+[^{]+)?$'
    )
    
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        m = method_sig_pattern.match(lines[i])
        if m and not line.endswith(';') and not line.startswith('//'):
            method_name = m.group(1)
            start_line = i + 1
            # find opening brace
            brace_count = 0
            found_open = False
            j = i
            while j < len(lines):
                for char in lines[j]:
                    if char == '{':
                        brace_count += 1
                        found_open = True
                    elif char == '}':
                        brace_count -= 1
                if found_open and brace_count == 0:
                    end_line = j + 1
                    method_len = end_line - start_line + 1
                    if method_len > 50:
                        long_methods.append((method_name, start_line, end_line, method_len))
                    i = j
                    break
                j += 1
        i += 1

    # 2. Dead code / commented-out code (Rule Q-B1)
    # Look for commented-out C# statements like // var x =, // return, // if (, etc.
    dead_code_lines = []
    commented_code_pattern = re.compile(
        r'^\s*//\s*(?:var\s+\w+\s*=|return\s|if\s*\(|while\s*\(|for\s*\(|throw\s+new|Assert\.|throw\s|using\s*\(\w+|\w+\.\w+\(.*\);)\s*$'
    )
    for idx, line in enumerate(lines):
        # Exclude documentation comments ///
        if line.strip().startswith('///'):
            continue
        if commented_code_pattern.match(line):
            dead_code_lines.append((idx + 1, line.strip()))

    # 3. Unannotated catch blocks (Rule Q-B2)
    # Look for catch blocks that are empty or have no comment explaining why
    unannotated_catches = []
    catch_pattern = re.compile(r'catch(?:\s*\([^\)]*\))?\s*\{([^}]*)\}')
    # Also multiline catches
    for match in re.finditer(r'catch(?:\s*\([^\)]*\))?\s*\{', content):
        start_idx = match.start()
        # Find closing brace
        brace_count = 1
        pos = match.end()
        while pos < len(content) and brace_count > 0:
            if content[pos] == '{':
                brace_count += 1
            elif content[pos] == '}':
                brace_count -= 1
            pos += 1
        catch_body = content[match.end():pos-1].strip()
        line_num = content[:start_idx].count('\n') + 1
        if len(catch_body) == 0:
            unannotated_catches.append((line_num, "EMPTY CATCH WITH NO CONTENT OR COMMENT"))
        elif not catch_body and '//' not in content[match.start():pos]:
            unannotated_catches.append((line_num, "EMPTY CATCH WITHOUT COMMENT"))
        elif len(catch_body.splitlines()) <= 2 and not any(k in catch_body for k in ['throw', 'return', 'Log', 'log', '//', '/*']):
            unannotated_catches.append((line_num, f"POTENTIAL SILENT CATCH: {catch_body}"))

    return total_lines, long_methods, dead_code_lines, unannotated_catches

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("=================================================================")
print("=== DETAILED STATIC ANALYSIS ON WORKER_M1 FILES ===")
print("=================================================================")
for rel_path in files_worker_m1:
    full_path = os.path.join(base_dir, rel_path)
    total_lines, long_methods, dead_code, unannotated_catches = analyze_csharp(full_path)
    print(f"\nFile: {rel_path} ({total_lines} lines)")
    if long_methods:
        print(f"  [M3 VIOLATION] Methods > 50 lines:")
        for m_name, s, e, l in long_methods:
            print(f"    - {m_name}: lines {s}-{e} ({l} lines)")
    else:
        print("  [M3 PASS] No methods > 50 lines")
    
    if dead_code:
        print(f"  [Q-B1 VIOLATION] Commented-out code detected:")
        for l_num, text in dead_code:
            print(f"    - Line {l_num}: {text}")
    else:
        print("  [Q-B1 PASS] 0 commented-out code")
        
    if unannotated_catches:
        print(f"  [Q-B2 VIOLATION] Unannotated / silent catch blocks:")
        for l_num, desc in unannotated_catches:
            print(f"    - Line {l_num}: {desc}")
    else:
        print("  [Q-B2 PASS] 0 unannotated catch blocks")
