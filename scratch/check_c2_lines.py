import os
import re

files_to_check = [
    r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary\Revit\generate_rebar_curves\code.cs",
    r"HPGrasshopper\HPGrasshopper.Mcp.Server.Tests\Seeds\AdversarialSeedStressTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\McpBridgeWorkRequest.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\CanvasUndoScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\ExecutionOutcome.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\GrasshopperExecutionEngine.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Execution\RevitTransactionScope.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Host\GrasshopperAddinApplication.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\FindingR3CommitOrderingAdversarialTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Execution\Milestone2CommitOrderingAndRollbackLedgerTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Fakes\TestScopeFactory.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Regression\ChallengerM1AdversarialTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Regression\Milestone1RemediationEmpiricalTests.cs"
]

print("=== 1. RULE C2: FILE LINE COUNT (<= 300 LINES) ===")
c2_violations = []
for rel_path in files_to_check:
    full_path = os.path.join(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar", rel_path)
    if not os.path.exists(full_path):
        print(f"NOT FOUND: {rel_path}")
        continue
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = len(lines)
    status = "PASS" if count <= 300 else "FAIL"
    print(f"[{status}] {count:3d} lines: {rel_path}")
    if count > 300:
        c2_violations.append((rel_path, count))

if not c2_violations:
    print("ALL FILES PASS RULE C2 (<= 300 lines).\n")
else:
    print(f"VIOLATIONS FOUND: {c2_violations}\n")
