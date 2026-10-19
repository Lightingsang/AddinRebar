## 2026-10-07T16:27:23Z
You are worker_m1_it1_r1, a teamwork_preview_worker agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it1_r1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first, especially ## 2026-10-07T13:49:28Z).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Detailed specifications and implementation guidance are prepared by the Explorers:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\report.md (Schema validation, raw CAD rejection, CS0535 fix)
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2\report.md (BimExecutionPlan IR suite & ExecutionPlanBuilder)
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\report.md (ElementExecutionRecord, granularity, zero-fallback enforcement)

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your Exclusive Write Ownership:
- HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs
- HPBim/src/HPBim.Core/Validation/*
- HPBim/src/HPBim.Core/Models/BimModel.cs
- HPBim/src/HPBim.Core/Serialization/BimModelSerializer.cs
- HPBim/src/HPBim.Cli/Commands/ValidateCommand.cs
- HPBim/src/HPBim.Core/Execution/*
- HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs
- HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs
- HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs
- HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs
- HPBim/src/HPBim.Client/ScriptGenerators/*
- HPBim/tests/HPBim.Core.Tests/Validation/*
- HPBim/tests/HPBim.Core.Tests/Execution/*
- HPBim/tests/HPBim.Client.Tests/Pipeline/*

Implementation Tasks:
1. Fix CS0535 in FakeRevitPipeClient.cs by implementing CancelAsync properly.
2. Implement BimModelSchemaValidator, SchemaValidationResult, SchemaValidationException in HPBim.Core/Validation/ (enforcing schema version 1.0, artifactType HPBim.Model.v1, rejecting raw CAD JSON and empty {} bypass, referential integrity). Integrate with ValidateCommand and BimModelSerializer.
3. Implement BimExecutionPlan, ElementPlanItem, ExecutionOptions, ElementExecutionRecord, ElementExecutionStatus, DocumentIdentitySnapshot, CompositeSourceKey in HPBim.Core/Execution/ and Coordination/.
4. Implement ExecutionPlanBuilder in HPBim.Client/Pipeline/ resolving levels and symbols ahead-of-time with zero heuristic fallbacks.
5. Update BimPipelineEngine so Preview (dryRun: true with rollback) and Apply (dryRun: false with commit) execute the exact same execution plan IR, with ViewPlan.Create unified.
6. Eradicate heuristic .FirstOrDefault() ?? .First() fallbacks and silent error swallows in script generators.
7. Implement unit tests in HPBim.Core.Tests and HPBim.Client.Tests verifying schema validation, execution plan, and zero regression on existing 370 tests.
8. Execute `dotnet build` and `dotnet test` commands and document all results.

Deliver your report to report.md and your completion handoff to handoff.md in your working directory, then send message to orchestrator.
