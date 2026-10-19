import re
import os

files = [
    r"HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs",
    r"HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs",
    r"HPDynamo/HPDynamo.McpBridge/Support/DynamoScriptGlobals.cs",
    r"HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs",
    r"HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs",
    r"HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs",
    r"HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs"
]

print("=== METHOD / FUNCTION MEASUREMENT (INCLUDING MULTI-LINE SIGNATURES) ===")
for f in files:
    print(f"\n--- {f} ---")
    with open(f, "r", encoding="utf-8") as fp:
        lines = fp.readlines()

    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # Look for start of method/function/ctor
        # Patterns like: public ... methodName(
        m = re.search(r'(?:public|private|internal|protected)\s+(?:static\s+|async\s+|override\s+|sealed\s+|virtual\s+)*([\w\<\>\[\],\?\.\s]+)\s+([A-Za-z0-9_]+)\s*\(', line)
        if not m and f.endswith("code.cs"):
            m = re.search(r'^(?:void|bool|int|string|Task|[\w\<\>\[\],\?]+)\s+([A-Za-z0-9_]+)\s*\(', line)
            if m and (line.startswith("if") or line.startswith("return") or line.startswith("log")):
                m = None

        if m:
            method_name = m.group(2) if m.lastindex >= 2 else m.group(1)
            start_line = i + 1
            # find opening brace
            brace_idx = i
            while brace_idx < len(lines) and "{" not in lines[brace_idx]:
                brace_idx += 1
            if brace_idx < len(lines) and "{" in lines[brace_idx]:
                depth = 0
                end_line = -1
                for j in range(brace_idx, len(lines)):
                    depth += lines[j].count("{") - lines[j].count("}")
                    if depth == 0:
                        end_line = j + 1
                        break
                if end_line != -1:
                    total_lines = end_line - start_line + 1
                    body_lines = (end_line - 1) - (brace_idx + 1) + 1
                    status = "PASS" if total_lines <= 50 else ("PASS (body<=50)" if body_lines <= 50 else "OVER_50")
                    print(f"  {method_name} (L{start_line}-L{end_line}): total={total_lines}, body={body_lines} [{status}]")
                    i = end_line
                    continue
        i += 1
