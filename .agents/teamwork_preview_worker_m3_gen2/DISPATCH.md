# Dispatch Assignment: Milestone 3 Remediation (Worker M3 Gen 2)

## Role: Worker (teamwork_preview_worker)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3_gen2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Reviewer 2 Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_2\handoff.md`
## Challenger 2 Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2\handoff.md`

---

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

---

## Write Ownership
You have exclusive write access to:
- `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/**`
- `.agents/teamwork_preview_worker_m3_gen2/**`

---

## Remediation Objectives
Apply the verified drop-in fixes to the 5 seed tools in `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/` to ensure they compile and execute cleanly against Tekla Structures 2025.0 Open API assemblies (`C:\Program Files\Tekla Structures\2025.0\bin\`) under .NET Framework 4.8:

### 1. `Drawing/list_drawings/code.cs`
- Replace `Math.Clamp(...)` with `Math.Max(1, Math.Min(..., 200))` because `Math.Clamp` is absent in .NET Framework 4.8.

### 2. `Model/get_model_info/code.cs`
- In `ProjectInfo`, replace `proj.ProjectName` with `proj.Name` (the correct property in Tekla Open API).

### 3. `Model/select_objects/code.cs`
- Replace `Math.Clamp` with `Math.Max(1, Math.Min(..., 200))`.
- For `filter == "REBAR"`, do NOT use `ModelObjectEnum.REBAR` (does not exist). Instead, query objects and check if `mo is Reinforcement` or handle specific reinforcement enums (`SINGLEREBAR`, `REBARGROUP`, `REBARMESH`, `REBARSTRAND`).

### 4. `Rebar/get_reinforcement_info/code.cs`
- Replace `Math.Clamp` with `Math.Max(1, Math.Min(..., 200))`.
- Do NOT use `selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.REBAR)` (does not exist). Use `selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.UNKNOWN)` or `GetAllObjects()` and filter `if (enumerator.Current is Reinforcement rebar)`.
- Base class `Reinforcement` does not define `Size`. Extract size safely:
  ```csharp
  string size = "";
  if (rebar is SingleRebar sr) size = sr.Size;
  else if (rebar is RebarGroup rg) size = rg.Size;
  ```

### 5. `Export/export_ifc/code.cs`
- Replace invalid enum values and struct fields with valid Tekla 2025 Open API types:
  - `exportView`: use `Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW` or `REFERENCE_VIEW` (without `IFC4_` or `IFC2X3_` prefixes).
  - `exportBasePoint`: use `Tekla.Structures.Model.Operations.Operation.ExportBasePoint.WORK_PLANE`.
  - `flags`: `Tekla.Structures.Model.Operations.Operation.IFCExportFlags` is a struct; pass `new Tekla.Structures.Model.Operations.Operation.IFCExportFlags()` or `default`.

---

## Verification Requirements
1. Build `HPTekla.Mcp.Server`:
   ```bash
   dotnet build HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj -c Release
   dotnet build HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj -c Debug
   ```
   Both must build with 0 warnings and 0 errors.
2. Verify all 12 seed tools compile against Tekla 2025 assemblies (`C:\Program Files\Tekla Structures\2025.0\bin`):
   Run the verification scripts provided by Reviewer 2:
   ```bash
   python .agents/teamwork_preview_reviewer_m3_2/verify_fixes.py
   ```
   All 12 tools must output `[PASS]`.
3. Verify stdio handshake returns 24 tools:
   ```bash
   python .agents/teamwork_preview_worker_m3/verify_stdio.py
   ```
4. Write report to `.agents/teamwork_preview_worker_m3_gen2/report.md` and handoff to `.agents/teamwork_preview_worker_m3_gen2/handoff.md`.
5. Send completion message to parent orchestrator.

## 2026-09-21T18:43:19Z
You are teamwork_preview_worker_m3_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3_gen2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3_gen2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Reviewer 2 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m3_2\handoff.md
Challenger 2 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

