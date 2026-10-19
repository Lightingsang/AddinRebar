import os
import re

touched_files = [
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperContextExtractor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\McpBridgeWorkRequest.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\CanvasUndoScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\GrasshopperExecutionEngine.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\RevitTransactionScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerRunner.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Coordinator\GrasshopperBridgeCoordinatorTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Coordinator\Milestone2QueueAndContextTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\CanvasUndoScopeTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\Milestone2RollbackAndUndoTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fakes\GrasshopperTestDoubles.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fakes\TestScopeFactory.cs"
]

base = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

method_decl_re = re.compile(
    r'^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|unsafe)\s+)*'
    r'(?:[A-Za-z0-9_<>\[\],\?]+\s+)+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
)

print("=== METHOD LINE COUNT AUDIT (RULE M3 <= 50 LINES) ===")

violations = []
total_methods = 0

for rel in touched_files:
    filepath = os.path.join(base, rel)
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
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
            if method_decl_re.match(stripped) and not stripped.endswith(';'):
                m = method_decl_re.match(stripped)
                name = m.group(1)
                if name not in ('if', 'for', 'foreach', 'while', 'switch', 'catch', 'lock', 'using'):
                    in_method = True
                    method_name = name
                    method_start = idx
                    target_depth = brace_depth

        for ch in clean:
            if ch == '{':
                brace_depth += 1
                if in_method and body_start == 0:
                    body_start = idx
            elif ch == '}':
                brace_depth -= 1
                if in_method and brace_depth == target_depth:
                    # Method ended
                    total_lines = idx - method_start + 1
                    total_methods += 1
                    if total_lines > 50:
                        violations.append((rel, method_name, method_start, idx, total_lines))
                    in_method = False
                    method_name = ""
                    body_start = 0

print(f"Total methods analyzed: {total_methods}")
print(f"Total M3 violations: {len(violations)}")
for rel, name, start, end, length in violations:
    print(f"FAIL: {rel} :: {name}() lines {start}-{end} ({length} lines > 50)")

if len(violations) == 0:
    print("M3 VERDICT: PASS (all methods <= 50 lines)")
else:
    print("M3 VERDICT: FAIL")
