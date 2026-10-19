import os, re

files = [
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Tools/FindingN6PlayerDataTreeAssignmentTests.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Tools/FindingN7DuplicateOutputNicknamesTests.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/FindingN8DiagnosticsAndCamelCaseTests.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Tools/FindingN9ContainerSchemaValidationTests.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Coordinator/FindingN10ContextStaleAndIsolationTests.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fakes/GrasshopperTestDoubles.cs'
]

c2_fail = 0
m3_fail = 0
qb1_fail = 0
qb2_fail = 0

for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    if len(lines) > 300:
        print(f'C2 VIOLATION: {f} has {len(lines)} lines (> 300)')
        c2_fail += 1

    brace_depth = 0
    method_start = None
    method_header = ''
    for i, line in enumerate(lines, 1):
        s = line.strip()
        clean_line = re.sub(r'"(?:\\.|[^"\\])*"', '', line)
        clean_line = re.sub(r'//.*', '', clean_line)
        open_b = clean_line.count('{')
        close_b = clean_line.count('}')
        if brace_depth == 1 and open_b > 0 and method_start is None:
            method_start = i
            method_header = lines[i-2].strip() if i > 1 else 'unknown'
        brace_depth += open_b - close_b
        if brace_depth == 1 and method_start is not None:
            length = i - method_start + 1
            if length > 50:
                print(f'M3 VIOLATION: {f}:{method_start} "{method_header}" has {length} lines (> 50)')
                m3_fail += 1
            method_start = None

        if re.match(r'^\s*//\s*(?:var|if|return|foreach|for|while|try|catch|throw|using|public|private|internal)\s+', line):
            print(f'Q-B1 VIOLATION: {f}:{i} {s}')
            qb1_fail += 1
        if re.match(r'^\s*catch\s*(?:\([^)]*\))?\s*\{\s*\}', line):
            print(f'Q-B2 VIOLATION: {f}:{i} {s}')
            qb2_fail += 1

print(f'Audit Results across all {len(files)} files:')
print(f'  C2 (<=300 lines): {c2_fail} violations')
print(f'  M3 (<=50 lines/method): {m3_fail} violations')
print(f'  Q-B1 (0 dead code): {qb1_fail} violations')
print(f'  Q-B2 (0 unannotated catches): {qb2_fail} violations')
assert c2_fail == 0 and m3_fail == 0 and qb1_fail == 0 and qb2_fail == 0, 'Clean code audit failed!'
print('CLEAN CODE AUDIT 100% PASSED!')
