import os
import re

TEST_FILES = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server.Tests\Tools\GrasshopperToolResultTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\Coordinator\McpBridgeWorkRequestTests.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\Host\GrasshopperRpcDispatcherTests.cs",
]

print("=" * 80)
print("Scanning Test Files for Integrity Violations & Trivial Assertions")
print("=" * 80)

trivial_patterns = [
    re.compile(r'Assert\.True\(true\)', re.IGNORECASE),
    re.compile(r'Assert\.False\(false\)', re.IGNORECASE),
    re.compile(r'Assert\.Equal\((["\w]+),\s*\1\)', re.IGNORECASE),
]

for tf in TEST_FILES:
    with open(tf, "r", encoding="utf-8") as f:
        lines = f.readlines()
    print(f"Checking: {os.path.basename(tf)} ({len(lines)} lines)")
    for i, line in enumerate(lines, 1):
        for pat in trivial_patterns:
            if pat.search(line):
                print(f"  SUSPICIOUS TRIVIAL ASSERTION at line {i}: {line.strip()}")

print("\n" + "=" * 80)
print("Checking for Facade or Bypass Patterns in Production Code")
print("=" * 80)

PROD_FILES = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Host\GrasshopperRpcDispatcher.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Execution\GrasshopperExecutionEngine.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server\Tools\GrasshopperToolResult.cs",
]

for pf in PROD_FILES:
    with open(pf, "r", encoding="utf-8") as f:
        text = f.read()
    print(f"Checking: {os.path.basename(pf)}")
    # Check for hardcoded test paths or fake returns
    if "nonexistent_definition.gh" in text or "fake" in text.lower():
        print(f"  WARNING: potential hardcoded string in {os.path.basename(pf)}")
    else:
        print(f"  PASS: No test fixtures or hardcoded mock returns found.")

print("\nIntegrity Scan Complete.")
