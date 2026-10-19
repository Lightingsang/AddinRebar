import os
import re

files_all = [
    r'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/McpBridgeWorkRequest.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Coordinator/GrasshopperBridgeCoordinatorTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Coordinator/Milestone2QueueAndContextTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/CanvasUndoScopeTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/Milestone2RollbackAndUndoTests.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fakes/GrasshopperTestDoubles.cs',
    r'HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fakes/TestScopeFactory.cs'
]

method_decl_re = re.compile(
    r'^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|unsafe)\s+)*'
    r'(?:[A-Za-z0-9_<>\[\],\?]+\s+)+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
)

print("=== METHOD LINE COUNT AUDIT ===")

all_methods = []

for filepath in files_all:
    if not os.path.exists(filepath):
        continue
    with open(filepath, "r", encoding="utf-8") as fp:
        raw_lines = fp.readlines()

    in_block_comment = False
    clean_lines = []
    for line in raw_lines:
        line_clean = ""
        i = 0
        in_string = False
        str_char = ''
        while i < len(line):
            if in_block_comment:
                if line[i:i+2] == '*/':
                    in_block_comment = False
                    i += 2
                else:
                    i += 1
                continue
            if in_string:
                if line[i] == '\\' and str_char != '@':
                    i += 2
                elif line[i] == str_char:
                    in_string = False
                    i += 1
                else:
                    i += 1
                continue
            if line[i:i+2] == '/*':
                in_block_comment = True
                i += 2
                continue
            if line[i:i+2] == '//':
                break
            if line[i] in ('"', "'"):
                in_string = True
                str_char = line[i]
                i += 1
                continue
            line_clean += line[i]
            i += 1
        clean_lines.append(line_clean)

    brace_depth = 0
    in_method = False
    method_name = ""
    method_start = 0
    body_start = 0
    target_depth = 0

    for idx, (raw, clean) in enumerate(zip(raw_lines, clean_lines), 1):
        stripped = raw.strip()

        if not in_method and brace_depth >= 1:
            decl_candidate = stripped.split('{')[0].strip()
            m = method_decl_re.match(decl_candidate)
            if m and not stripped.endswith(';') and not stripped.startswith('if') and not stripped.startswith('while') and not stripped.startswith('for'):
                in_method = True
                method_name = m.group(1)
                method_start = idx
                target_depth = brace_depth

        opens = clean.count('{')
        closes = clean.count('}')
        if in_method and opens > 0 and body_start == 0:
            body_start = idx

        brace_depth += opens - closes

        if in_method and brace_depth <= target_depth and (opens > 0 or closes > 0):
            body_end = idx
            total_lines = body_end - method_start + 1
            inner_lines = (body_end - 1) - (body_start + 1) + 1 if body_end > body_start + 1 else 0
            
            # Count non-blank, non-comment lines inside body
            code_lines = 0
            for l_idx in range(body_start, body_end - 1):
                if raw_lines[l_idx].strip() and not raw_lines[l_idx].strip().startswith('//'):
                    code_lines += 1

            is_test = 'Tests' in filepath
            all_methods.append({
                'file': filepath,
                'name': method_name,
                'start': method_start,
                'end': body_end,
                'total_lines': total_lines,
                'inner_lines': inner_lines,
                'code_lines': code_lines,
                'is_test': is_test
            })
            in_method = False
            body_start = 0

prod_methods = [m for m in all_methods if not m['is_test']]
test_methods = [m for m in all_methods if m['is_test']]

print(f"Total methods analyzed: {len(all_methods)} (Production: {len(prod_methods)}, Tests: {len(test_methods)})")

prod_max = max(prod_methods, key=lambda x: x['total_lines']) if prod_methods else None
if prod_max:
    print(f"Production max total lines: {prod_max['file']}::{prod_max['name']} = {prod_max['total_lines']} lines (code lines: {prod_max['code_lines']})")

test_max = max(test_methods, key=lambda x: x['total_lines']) if test_methods else None
if test_max:
    print(f"Test max total lines: {test_max['file']}::{test_max['name']} = {test_max['total_lines']} lines (code lines: {test_max['code_lines']})")

print("\n--- Production Methods with total_lines > 50 ---")
prod_viol = [m for m in prod_methods if m['total_lines'] > 50]
for m in prod_viol:
    print(f"[PROD] {m['file']}:{m['start']}-{m['end']} {m['name']}: total={m['total_lines']}, inner={m['inner_lines']}, code={m['code_lines']}")
if not prod_viol:
    print("None! 100% of production methods have total_lines <= 50.")

print("\n--- Test Methods with total_lines > 50 ---")
test_viol = [m for m in test_methods if m['total_lines'] > 50]
for m in test_viol:
    print(f"[TEST] {m['file']}:{m['start']}-{m['end']} {m['name']}: total={m['total_lines']}, inner={m['inner_lines']}, code={m['code_lines']}")

