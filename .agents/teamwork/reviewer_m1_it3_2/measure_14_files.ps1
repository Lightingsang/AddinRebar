$files = @(
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs",
    "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs",
    "HPBim/src/HPBim.Core/Validation/SchemaValidationResult.cs",
    "HPBim/src/HPBim.Core/Validation/SchemaValidationException.cs",
    "HPBim/src/HPBim.Core/Execution/BimExecutionPlan.cs",
    "HPBim/src/HPBim.Core/Execution/ElementPlanItem.cs",
    "HPBim/src/HPBim.Core/Execution/ElementExecutionRecord.cs",
    "HPBim/src/HPBim.Core/Execution/ElementExecutionStatus.cs",
    "HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs",
    "HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs",
    "HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs",
    "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs",
    "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.ArchitectureAndMep.cs",
    "HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs"
)

$results = @()
foreach ($f in $files) {
    if (Test-Path $f) {
        $lines = (Get-Content $f).Count
        $results += [PSCustomObject]@{
            File = (Split-Path -Leaf $f)
            Lines = $lines
            C2_Compliant = ($lines -le 300)
        }
    } else {
        $results += [PSCustomObject]@{
            File = (Split-Path -Leaf $f)
            Lines = -1
            C2_Compliant = "NOT FOUND"
        }
    }
}
$results | Format-Table -AutoSize
