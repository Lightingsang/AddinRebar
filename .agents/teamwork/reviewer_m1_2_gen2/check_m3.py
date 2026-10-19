import os
import re

files = [
    'HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Execution/ExecutionOutcome.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Host/GrasshopperAddinApplication.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/McpBridgeWorkRequest.cs',
    'HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs'
]

print('=== Checking Rule M3 (Methods <= 50 lines) ===')
for path in files:
    with open(path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    in_method = False
    method_name = ''
    start_line = 0
    brace_depth = 0
    method_brace_depth = 0

    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        clean_line = re.sub(r'"(\\.|[^"])*"', '', stripped)
        clean_line = re.sub(r'//.*', '', clean_line)
        open_b = clean_line.count('{')
        close_b = clean_line.count('}')

        if not in_method:
            if re.search(r'(public|private|protected|internal|static|async)\s+[\w<>\[\],?\s]+\s+(\w+)\s*\([^)]*\)\s*$', stripped) or \
               re.search(r'(public|private|protected|internal|static|async)\s+[\w<>\[\],?\s]+\s+(\w+)\s*\([^)]*\)\s*\{', stripped):
                match = re.search(r'(\w+)\s*\(', stripped)
                if match and match.group(1) not in ('if', 'for', 'while', 'switch', 'catch'):
                    method_name = match.group(1)
                    start_line = idx
                    in_method = True
                    method_brace_depth = brace_depth

        brace_depth += open_b - close_b

        if in_method and brace_depth <= method_brace_depth and (open_b > 0 or close_b > 0):
            method_length = idx - start_line + 1
            if method_length > 50:
                print(f'[FAIL M3] {path}:{start_line} {method_name}() has {method_length} lines (> 50)')
            else:
                pass
            in_method = False

print('Rule M3 verification finished.')
