import os
import re

files_to_check = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasBatchValidator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\ImageExporter.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\CanvasEditorTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerDataTreeHelper.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerRunner.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasReader.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server\Tools\BatchEditGrasshopperCanvasTool.cs"
]

code_patterns = [
    re.compile(r'^\s*//\s*(?:var|int|string|bool|double|if|for|foreach|while|return|throw|try|catch|using|new\s+\w+|doc\.|canvas\.)\b'),
    re.compile(r'^\s*//\s*\w+\s*=\s*[^;]+;'),
    re.compile(r'^\s*//\s*\w+\([^)]*\);')
]

empty_catch_pattern = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{\s*\}')

print("=== AUDITING Q-B1 (Commented-out code) and Q-B2 (Empty catch) ===")
qb1_violations = []
qb2_violations = []

for fp in files_to_check:
    fname = os.path.basename(fp)
    with open(fp, 'r', encoding='utf-8') as f:
        content = f.read()
        lines = content.splitlines()
    
    # Check Q-B1
    for idx, l in enumerate(lines):
        for pat in code_patterns:
            if pat.search(l):
                # ignore comments that are explanatory
                qb1_violations.append((fname, idx + 1, l))
    
    # Check Q-B2
    for m in empty_catch_pattern.finditer(content):
        # find line number
        line_num = content[:m.start()].count('\n') + 1
        qb2_violations.append((fname, line_num, m.group(0)))

print(f"Q-B1 Violations: {len(qb1_violations)}")
for f, l, text in qb1_violations:
    print(f"  {f}:{l} -> {text.strip()}")

print(f"Q-B2 Violations: {len(qb2_violations)}")
for f, l, text in qb2_violations:
    print(f"  {f}:{l} -> {text.strip()}")
