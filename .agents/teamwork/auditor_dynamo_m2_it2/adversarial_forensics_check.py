import re
import os

files = [
    r"HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs",
    r"HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs",
    r"HPDynamo/HPDynamo.McpBridge/Support/DynamoScriptGlobals.cs",
    r"HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs",
    r"HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs",
    r"HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs",
    r"HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs"
]

print("=== ADVERSARIAL INTEGRITY FORENSICS CHECK ===")

suspicious_items = []

for f in files:
    with open(f, "r", encoding="utf-8") as fp:
        lines = fp.readlines()

    for idx, line in enumerate(lines, 1):
        s = line.strip()
        # Look for suspicious hardcoded returns
        if re.search(r'return\s+(?:true|false|null|""|"OK"|"Success")\s*;', s):
            # Check context
            prev_line = lines[idx-2].strip() if idx > 1 else ""
            if not prev_line.startswith("if") and not prev_line.startswith("catch") and not "=>" in s:
                suspicious_items.append((f, idx, s, "Unconditional literal return"))

        # Look for NotImplementedException
        if "NotImplementedException" in s:
            suspicious_items.append((f, idx, s, "NotImplementedException"))

        # Look for TODO / FIXME / HACK
        if re.search(r'\b(TODO|FIXME|HACK|TEMPORARY|MOCK)\b', s, re.IGNORECASE) and not f.endswith("Tests.cs"):
            suspicious_items.append((f, idx, s, "Suspicious keyword"))

print(f"Total suspicious items flagged: {len(suspicious_items)}")
for item in suspicious_items:
    print(f"  {item[0]}:{item[1]} -> [{item[3]}] {item[2]}")
