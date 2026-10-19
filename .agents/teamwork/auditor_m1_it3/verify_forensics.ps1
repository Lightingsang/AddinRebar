# Forensic Verification Script for auditor_m1_it3
$ErrorActionPreference = "Stop"

Write-Host "=== 1. VERIFYING BimModelSchemaValidator.cs ==="
$schemaValidatorLines = Get-Content "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs"
Write-Host "Total lines: $($schemaValidatorLines.Count)"
$validateLines = $schemaValidatorLines[35..64]
Write-Host "Validate start (line 36): $($validateLines[0].Trim())"
Write-Host "Validate end (line 65): $($validateLines[-1].Trim())"
Write-Host "Validate line count: $($validateLines.Count)"

$validateModelStructureLines = $schemaValidatorLines[169..187]
Write-Host "ValidateModelStructure start (line 170): $($validateModelStructureLines[0].Trim())"
Write-Host "ValidateModelStructure end (line 188): $($validateModelStructureLines[-1].Trim())"
Write-Host "ValidateModelStructure line count: $($validateModelStructureLines.Count)"

Write-Host "`n=== 2. VERIFYING BimModelSchemaValidator.Elements.cs ==="
$elementsLines = Get-Content "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs"
Write-Host "Total lines: $($elementsLines.Count)"

Write-Host "`n=== 3. VERIFYING BimPipelineEngine.PlanExecution.cs ==="
$planExecLines = Get-Content "HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs"
Write-Host "Total lines: $($planExecLines.Count)"
$execPlanAsyncLines = $planExecLines[17..51]
Write-Host "ExecutePlanAsync start (line 18): $($execPlanAsyncLines[0].Trim())"
Write-Host "ExecutePlanAsync end (line 52): $($execPlanAsyncLines[-1].Trim())"
Write-Host "ExecutePlanAsync line count: $($execPlanAsyncLines.Count)"

Write-Host "`n=== 4. VERIFYING ExecutionPlanBuilder.cs ==="
$planBuilderLines = Get-Content "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs"
Write-Host "Total lines: $($planBuilderLines.Count)"
$buildLines = $planBuilderLines[18..46]
Write-Host "Build start (line 19): $($buildLines[0].Trim())"
Write-Host "Build end (line 47): $($buildLines[-1].Trim())"
Write-Host "Build line count: $($buildLines.Count)"

Write-Host "`n=== 5. CHECKING C2 FILE LINE LIMIT (<= 300) ON ALL MODIFIED FILES ==="
$modifiedFiles = @(
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs",
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs",
    "HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs",
    "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs"
)
foreach ($f in $modifiedFiles) {
    $cnt = (Get-Content $f).Count
    $pass = $cnt -le 300
    Write-Host "$f : $cnt lines (<= 300: $pass)"
}
