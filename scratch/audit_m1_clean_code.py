import os
import re
import sys

files_to_audit = [
    r"HPBim\src\HPBim.Core\Coordination\CompositeSourceKeyJsonConverter.cs",
    r"HPBim\src\HPBim.Core\Coordination\CompositeSourceKey.cs",
    r"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerService.cs",
    r"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerService.Mappings.cs",
    r"HPBim\src\HPBim.Core\Coordination\BimCoordinationLedgerSerializer.cs",
    r"HPBim\src\HPBim.Core\Execution\ElementExecutionStatus.cs",
    r"HPBim\src\HPBim.Core\Execution\ElementExecutionRecord.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineResult.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.ElementRecords.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Receipt.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Columns.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.MepAndRebar.cs",
    r"HPBim\src\HPBim.Client\ScriptGenerators\Stage7MepGenerator.cs",
    r"HPBim\src\HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs",
    r"HPBim\src\HPBim.Client\TypeResolver\TypeResolutionRules.cs",
    r"HPBim\src\HPBim.Client\Pipeline\ExecutionPlanBuilder.ArchitectureAndMep.cs",
    r"HPBim\tests\HPBim.Core.Tests\Coordination\CompositeSourceKeyJsonConverterTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\SourceKeyWireRoundtripTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\CardinalityAndTruncationTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\StrictBatchRollbackTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\RunnerHeuristicEliminationTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\EmpiricalChallengerBatchRollbackTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\EmpiricalChallengerHeuristicsTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\EmpiricalChallengerM1RemediationTests.cs"
]

repo_root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("=== STARTING AUDIT OF M1 REMEDIATION FILES ===")
total_checked = 0
violations = []

commented_code_pattern = re.compile(r"^\s*//\s*(var|int|string|if|for|foreach|return|throw|new|public|private|protected|static|class|void|bool)\b")
empty_catch_pattern = re.compile(r"catch\s*\([^\)]*\)\s*\{\s*\}")

for rel_path in files_to_audit:
    full_path = os.path.join(repo_root, rel_path)
    if not os.path.exists(full_path):
        print(f"[SKIP] File not found: {rel_path}")
        continue
    total_checked += 1
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    line_count = len(lines)
    # Rule C2: file <= 300 lines
    if line_count > 300:
        violations.append(f"Rule C2 (>300 lines): {rel_path} has {line_count} lines")
    
    # Q-B1: commented-out code
    for idx, l in enumerate(lines):
        if commented_code_pattern.search(l):
            # check if it's documentation or example
            if "e.g." not in l and "example" not in l and "<" not in l and not l.strip().startswith("// Multi-version:"):
                violations.append(f"Rule Q-B1 (commented code) at {rel_path}:{idx+1} -> {l.strip()}")

    # Q-B2: empty catch
    content = "".join(lines)
    if empty_catch_pattern.search(content):
        violations.append(f"Rule Q-B2 (empty catch without comment) in {rel_path}")

print(f"Total files checked: {total_checked}")
if violations:
    print(f"VIOLATIONS FOUND ({len(violations)}):")
    for v in violations:
        print("  - " + v)
else:
    print("ALL CLEAN CODE STATIC CHECKS PASSED!")
