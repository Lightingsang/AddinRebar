import os
import re

M1_FILES = [
    "HPGrasshopper/HPGrasshopper.McpBridge/Host/GrasshopperRpcDispatcher.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/McpBridgeWorkRequest.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasReader.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge/Host/GrasshopperAddinApplication.cs",
    "HPGrasshopper/HPGrasshopper.Mcp.Server/Tools/GrasshopperToolResult.cs",
    "HPGrasshopper/HPGrasshopper.Mcp.Server/Tools/RunGrasshopperPlayerTool.cs",
    "HPGrasshopper/HPGrasshopper.Mcp.Server/Tools/BatchEditGrasshopperCanvasTool.cs",
    "HPGrasshopper/HPGrasshopper.Mcp.Server/Tools/ExportGrasshopperCanvasImageTool.cs",
    "HPGrasshopper/HPGrasshopper.Mcp.Server/Tools/ReadGrasshopperCanvasTool.cs",
    "HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/Tools/GrasshopperToolResultTests.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge.Tests/Coordinator/McpBridgeWorkRequestTests.cs",
    "HPGrasshopper/HPGrasshopper.McpBridge.Tests/Host/GrasshopperRpcDispatcherTests.cs",
]

ROOT_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def scan_methods(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    method_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|async|\s)+\s+[\w\<\>\[\]\?\,\s]+\s+(\w+)\s*\([^\)]*\)\s*(?:where\s+.*)?$')
    long_methods = []
    
    in_method = False
    method_name = ""
    start_line = 0
    brace_depth = 0
    method_brace_start = 0
    
    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        open_b = line.count('{')
        close_b = line.count('}')
        
        m = method_pattern.match(line)
        if m and not in_method and not stripped.endswith(';'):
            potential_name = m.group(1)
            if potential_name not in ('if', 'for', 'foreach', 'while', 'switch', 'catch', 'using', 'lock'):
                in_method = True
                method_name = potential_name
                start_line = i
                method_brace_start = brace_depth
        
        brace_depth += (open_b - close_b)
        
        if in_method and open_b > 0 and brace_depth == method_brace_start:
            length = i - start_line + 1
            if length > 50:
                long_methods.append((method_name, start_line, i, length))
            in_method = False
            
    return long_methods

all_m3_pass = True
for rel in M1_FILES:
    full = os.path.join(ROOT_DIR, rel.replace("/", "\\"))
    long_methods = scan_methods(full)
    if long_methods:
        all_m3_pass = False
        print(f"FAIL M3 in {rel}:")
        for m, s, e, l in long_methods:
            print(f"  Method {m} (lines {s}-{e}): {l} lines (> 50)")

if all_m3_pass:
    print("Rule M3 (all methods <= 50 lines): PASS for all 15 files!")
