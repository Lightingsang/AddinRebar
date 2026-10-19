# DISPATCH — teamwork_preview_reviewer_m5_2

## Task Assignment
**Role**: Architecture & Isolation Reviewer for Milestone 5 (teamwork_preview_reviewer)
**Mission**: Conduct an architectural, isolation, and regression review of Milestone 5 (`HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests`).

## Required Reading
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Project Specifications: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
3. Worker Handoff Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m5_1\handoff.md`

## Review Scope
1. **Solution Integration**:
   - Verify `HPDynamo/HPDynamo.slnx` properly registers both test projects.
   - Verify `Directory.Build.props` has `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`.
   - Verify `HPDynamo.McpBridge.csproj` has `<InternalsVisibleTo Include="HPDynamo.McpBridge.Tests" />`.
2. **Host Assembly Isolation**:
   - Verify zero host DLLs (`RevitAPI*.dll`, `DynamoCore*.dll`) are copied to output bins of `HPDynamo.McpBridge.Tests/bin/Debug/` or `HPDynamo.Mcp.Server.Tests/bin/Debug/`.
   - Verify cross-host isolation: HPDynamo references McpShared ONLY, zero sibling host references.
3. **24-Tool Surface Invariant Verification**:
   - Verify `ToolSurfaceTests.cs` asserts exactly 24 tools (4 Core + 8 Meta + 12 Seeds) and verifies `inspect_type` suppression.
4. **Run Verification Commands**:
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"`
   *(Do NOT pass `--nologo`)*

## Deliverable
Write your comprehensive review report with an explicit verdict (**APPROVE** or **REQUEST_CHANGES**) to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m5_2\handoff.md`
When complete, send a message back to orchestrator_4.

## 2026-10-08T12:41:06Z
You are teamwork_preview_reviewer_m5_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m5_2\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m5_2\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
Read the worker handoff report in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m5_1\handoff.md

Conduct architectural, isolation, and regression review for Milestone 5:
1. Verify HPDynamo.slnx, Directory.Build.props, and HPDynamo.McpBridge.csproj configuration.
2. Verify host assembly isolation: confirm 0 host DLLs (RevitAPI*.dll, DynamoCore*.dll) in test output bins.
3. Verify 24-tool surface invariant tested by ToolSurfaceTests.cs (4 Core + 8 Meta + 12 Seeds, inspect_type suppressed).
4. Run verification tests:
   dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
   (Do NOT pass --nologo)
5. Deliver your handoff report with explicit verdict APPROVE or REQUEST_CHANGES to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m5_2\handoff.md
When finished, send a message back to orchestrator_4.
