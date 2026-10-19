# Scan all M1 core & client pipeline files for C2 & M3
$m1Files = @(
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs",
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs",
    "HPBim/src/HPBim.Core/Validation/SchemaValidationResult.cs",
    "HPBim/src/HPBim.Core/Execution/BimExecutionPlan.cs",
    "HPBim/src/HPBim.Core/Execution/ElementPlanItem.cs",
    "HPBim/src/HPBim.Core/Execution/ElementExecutionRecord.cs",
    "HPBim/src/HPBim.Core/Execution/ElementExecutionStatus.cs",
    "HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs",
    "HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs",
    "HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs",
    "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs",
    "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.ArchitectureAndMep.cs",
    "HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs",
    "HPBim/src/HPBim.Client/Pipeline/StageResult.cs"
)

Write-Host "=== C2 FILE LINE COUNT SCAN (M1 Deliverables) ==="
$anyC2Violations = $false
foreach ($f in $m1Files) {
    if (Test-Path $f) {
        $c = (Get-Content $f).Count
        $pass = $c -le 300
        if (-not $pass) { $anyC2Violations = $true }
        Write-Host "$f : $c lines (<=300: $pass)"
    } else {
        Write-Warning "File not found: $f"
    }
}
Write-Host "C2 Overall Status: $(if (-not $anyC2Violations) { 'ALL PASS' } else { 'FAIL' })"
