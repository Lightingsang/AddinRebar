import re

files = [
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs'
]

violations = 0
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    brace_depth = 0
    method_start = None
    method_header = ''
    for i, line in enumerate(lines, 1):
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
                print(f'{f}:{method_start} Method "{method_header}" has {length} lines (> 50)')
                violations += 1
            method_start = None

print(f'M3 Check complete: {violations} violation(s).')
