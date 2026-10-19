$paths = @(
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

& "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it3_r1\measure_methods.ps1" -Paths $paths -MaxMethodLines 50 -MaxFileLines 300
