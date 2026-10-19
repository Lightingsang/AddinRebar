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

print("=== CHECK 1: RULE C2 (<= 300 LINES) ===")
c2_pass = True
for rel in touched_files:
    full = os.path.join(base, rel)
    with open(full, "r", encoding="utf-8") as f:
        lines = len(f.readlines())
    ok = lines <= 300
    if not ok:
        c2_pass = False
    print(f"[{'PASS' if ok else 'FAIL'}] {rel}: {lines} lines (<= 300)")

print(f"\nOverall C2: {'PASS' if c2_pass else 'FAIL'}")

print("\n=== CHECK 2: RULE Q-B1 (COMMENTED-OUT CODE) & Q-B2 (EMPTY CATCH) ===")
qb1_pass = True
qb2_pass = True

# Patterns for commented out code: // followed by code keywords like var, if, for, return, using, doc., new, throw
code_patterns = [
    re.compile(r"^\s*//\s*(?:var|if|for|foreach|while|return|using|new|throw|try|catch)\b"),
    re.compile(r"^\s*//\s*[A-Z][a-zA-Z0-9_]*\s*\(.*?\);"),
    re.compile(r"^\s*//\s*[a-z][a-zA-Z0-9_]*\.[a-zA-Z0-9_]+\s*=")
]

for rel in touched_files:
    full = os.path.join(base, rel)
    with open(full, "r", encoding="utf-8") as f:
        content = f.read()
        flines = content.splitlines()

    # Check commented-out code
    for idx, l in enumerate(flines):
        # Allow documentation tags like ///<summary>, or explanatory comments
        clean = l.strip()
        if clean.startswith("///") or clean.startswith("// Multi-version") or clean.startswith("// Rule") or clean.startswith("// ADR") or clean.startswith("// Phase") or clean.startswith("// Case") or clean.startswith("// Arrange") or clean.startswith("// Act") or clean.startswith("// Assert") or clean.startswith("// Multi-") or clean.startswith("// Note") or clean.startswith("// <summary>"):
            continue
        for cp in code_patterns:
            if cp.match(l):
                # Check if it looks like real code or just an explanation
                print(f"[POTENTIAL Q-B1] {rel}:{idx+1}: {l.strip()}")
                qb1_pass = False

    # Check empty catch blocks: catch (...) { }
    empty_catch_re = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}")
    matches = empty_catch_re.findall(content)
    if matches:
        print(f"[FAIL Q-B2] {rel}: Found empty catch without comment: {matches}")
        qb2_pass = False

print(f"Overall Q-B1: {'PASS' if qb1_pass else 'REVIEW_NEEDED'}")
print(f"Overall Q-B2: {'PASS' if qb2_pass else 'FAIL'}")
