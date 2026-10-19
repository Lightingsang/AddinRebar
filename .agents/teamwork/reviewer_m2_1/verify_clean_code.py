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

print("=== Rule C2: File Length <= 300 lines ===")
c2_ok = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = len(fp.readlines())
    status = "PASS" if lines <= 300 else "FAIL"
    if lines > 300:
        c2_ok = False
    print(f"{f}: {lines} lines -> {status}")
print("Overall C2:", "PASS" if c2_ok else "FAIL")

print("\n=== Rule Q-B2: Empty catch blocks without comment ===")
qb2_ok = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        content = fp.read()
    # Match catch (...) { } with only whitespace
    empty_catches = re.findall(r'catch\s*(?:\([^)]*\))?\s*\{\s*\}', content)
    if empty_catches:
        # Check if there is a comment inside
        for ec in empty_catches:
            print(f"{f}: Found empty catch without comment: {ec}")
            qb2_ok = False
print("Overall Q-B2:", "PASS" if qb2_ok else "FAIL")

print("\n=== Rule Q-B1: Commented-out dead code ===")
# Check for blocks of code commented out
qb1_ok = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    for idx, l in enumerate(lines):
        trimmed = l.strip()
        # Look for commented code statements like // var x = ..., // if (..., etc.
        if re.match(r'^//\s*(var|if|for|foreach|while|return|public|private|internal|protected)\s+[a-zA-Z0-9_]+', trimmed):
            print(f"{f}:{idx+1}: Suspicious commented code: {trimmed}")
            qb1_ok = False
print("Overall Q-B1:", "PASS" if qb1_ok else "FAIL")
