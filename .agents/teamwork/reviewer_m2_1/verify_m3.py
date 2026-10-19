import os
import re

files = [
    r'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/McpBridgeWorkRequest.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/Milestone2RollbackAndUndoTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Coordinator/Milestone2QueueAndContextTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Coordinator/GrasshopperBridgeCoordinatorTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fakes/TestScopeFactory.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fakes/GrasshopperTestDoubles.cs'
]

print("=== Rule M3: Method Length <= 50 lines ===")
m3_ok = True

for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()

    # Simple brace-counting method parser
    in_method = False
    method_name = ""
    start_line = 0
    brace_depth = 0
    method_brace_depth = 0

    for idx, line in enumerate(lines):
        line_num = idx + 1
        trimmed = line.strip()

        # Count braces
        open_braces = line.count('{')
        close_braces = line.count('}')

        # Detect potential method signature
        if not in_method and open_braces > 0:
            # Check previous line or current line for method signature
            check_text = trimmed
            if idx > 0 and not lines[idx-1].strip().endswith(';'):
                check_text = lines[idx-1].strip() + " " + trimmed

            if any(k in check_text for k in ['public ', 'private ', 'internal ', 'protected ']) and '(' in check_text and not any(k in check_text for k in ['class ', 'interface ', 'record ', 'enum ', 'struct ']):
                # If it's a property getter/setter, ignore
                if not ('get;' in check_text or 'set;' in check_text or 'get =>' in check_text):
                    in_method = True
                    method_name = check_text.split('(')[0].split()[-1] if '(' in check_text else "method"
                    start_line = line_num
                    method_brace_depth = brace_depth

        brace_depth += open_braces - close_braces

        if in_method and brace_depth <= method_brace_depth:
            length = line_num - start_line + 1
            if length > 50:
                print(f"{f}:{start_line}: Method {method_name} has {length} lines (> 50)!")
                m3_ok = False
            in_method = False

print("Overall M3:", "PASS" if m3_ok else "FAIL")
