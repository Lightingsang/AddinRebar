import os
import re

files_to_check = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasBatchValidator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerDataTreeHelper.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerRunner.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasReader.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Tools\ImageExporter.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server\Tools\BatchEditGrasshopperCanvasTool.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\CanvasEditorTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\PlayerDataTreeTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\ImageExporterTests.cs",
]

def analyze_file(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    total = len(lines)
    methods = []
    
    i = 0
    while i < total:
        line = lines[i].strip()
        if (not line or line.startswith("//") or line.startswith("/*") or line.startswith("*") or
            any(re.search(rf"\b{kw}\b", line) for kw in ["class", "interface", "struct", "record", "enum", "namespace"])):
            i += 1
            continue
            
        if "(" in line and not any(re.match(rf"^\b{kw}\b", line) for kw in ["if", "for", "foreach", "while", "switch", "using", "catch", "lock"]):
            sig_start = i
            sig_lines = []
            curr = i
            found_body = False
            is_expr = False
            has_semi = False
            while curr < total and curr - sig_start < 25:
                cl = lines[curr].strip()
                if "//" in cl:
                    cl = cl[:cl.index("//")].strip()
                sig_lines.append(cl)
                if "{" in cl:
                    found_body = True
                    break
                if "=>" in cl:
                    found_body = True
                    is_expr = True
                    break
                if ";" in cl:
                    has_semi = True
                    break
                curr += 1
                
            if found_body and not has_semi:
                sig_str = " ".join(sig_lines)
                if "(" in sig_str and ")" in sig_str:
                    body_start = curr
                    if is_expr:
                        end_line = body_start
                        while end_line < total and ";" not in lines[end_line]:
                            end_line += 1
                        methods.append((sig_start + 1, end_line + 1, end_line - sig_start + 1, sig_str.split("=>")[0].strip()))
                        i = end_line
                    else:
                        brace = 0
                        found_first = False
                        end_line = body_start
                        for scan in range(body_start, total):
                            clean = re.sub(r'//.*$', '', lines[scan])
                            clean = re.sub(r'"(?:\\.|[^"\\])*"', '', clean)
                            for ch in clean:
                                if ch == '{':
                                    brace += 1
                                    found_first = True
                                elif ch == '}':
                                    brace -= 1
                                    if found_first and brace == 0:
                                        end_line = scan
                                        break
                            if found_first and brace == 0:
                                break
                        methods.append((sig_start + 1, end_line + 1, end_line - sig_start + 1, sig_str.split("{")[0].strip()))
                        i = end_line
        i += 1
    return methods

print("--- AUDITING METHOD LENGTHS IN M3 PRODUCTION & TEST FILES ---")
for fpath in files_to_check:
    fname = os.path.basename(fpath)
    res = analyze_file(fpath)
    for start_l, end_l, span, sig in res:
        if span > 50:
            print(f"VIOLATION: {fname} lines {start_l}-{end_l} (span: {span}) -> {sig}")
        elif span >= 40:
            print(f"  NEAR-LIMIT ({span} lines): {fname} lines {start_l}-{end_l} -> {sig[:50]}")
print("--- AUDIT COMPLETE ---")
