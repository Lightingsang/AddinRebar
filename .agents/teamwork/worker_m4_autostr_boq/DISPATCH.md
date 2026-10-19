# DISPATCH — worker_m4_autostr_boq (Milestone M4: Phase 5 24-Step AutoStr Pipeline & Multi-Sheet BOQ)

You are `worker_m4_autostr_boq`, an expert C# domain implementation worker (`teamwork_preview_worker`).
Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_autostr_boq\`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T22:07:16Z`).
Master project scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_21\PROJECT.md`.
Specification blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\handoff.md`.
Codebase survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\handoff.md`.
Test architecture survey: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\handoff.md`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You exclusively own:
- `HPDcePro/src/HPDcePro.Core/AutoStr/*` (`IAutoStrStep.cs`, `AutoStrStepContext.cs`, `AutoStrStepResult.cs`, `AutoStrOrchestrator.cs`, `AutoStrCheckpoint.cs`, `AutoStrProgress.cs`, and `Steps/*` containing step processors `AutoStrStep01`..`AutoStrStep24`)
- `HPDcePro/src/HPDcePro.Core/Calculations/Takeoff/*` (`QuantityTakeoffModels.cs`, `QuantityTakeoffCalculator.cs`)
- `HPDcePro/src/HPDcePro.Excel/ClosedXmlBoqExporter.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/RebarCommands.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/AutoStrPipelineTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/AutoStrResizingLoopTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/QuantityTakeoffTests.cs`
- `HPDcePro/tests/HPDcePro.Core.Tests/BoqExcelExportTests.cs`

Do NOT modify `C:\Dce Pro` (strictly read-only!).

## Implementation Requirements for Milestone M4 (Phase 5)
1. **`AutoStrOrchestrator` & 24 Step Processors** in `HPDcePro.Core/AutoStr/`:
   - `IAutoStrStep`: `int StepNumber { get; }`, `string StepName { get; }`, `Task<AutoStrStepResult> ExecuteAsync(AutoStrStepContext context, CancellationToken cancellationToken)`.
   - `AutoStrStepContext`: Shared blackboard containing `AutoStrConfig Config`, `string ProjectRootPath`, `FramingTopology Topology`, `Dictionary<string, object> StepArtifacts`, `List<AutoStrLogEntry> Logs`, `Action<int, string, double> ReportProgress`.
   - Implement all 24 step processors (`AutoStrStep01_TaoSheetSet` to `AutoStrStep24_TMTT`) respecting `StepEnabled` flags from `DuAnMau.autostr`. Keep files modular ($\le 300$ lines each).
   - Checkpointing: JSON checkpoint serialization saving completed steps, durations, and state, with `ResumeFromCheckpointAsync`.
2. **Automated Iterative Resizing Loop**:
   - Integrated into `AutoStrOrchestrator`: checks column capacity ratios. If utilization $> 1.0$ and `CotChoTangTietDien == true`, increases column section by `CotBuocTang` (clamped to `CotHeSoTangToiDa`).
   - Checks pile reactions: if $P_{max} > P_{allow}$ and `CocTuTangKhiKhongDat == true`, increments pile count $n' = n + 1$ and updates pile cap layout.
   - Re-evaluates affected steps iteratively until converging or hitting max factor.
3. **`QuantityTakeoffCalculator`** in `HPDcePro.Core/Calculations/Takeoff/`:
   - Concrete volume ($m^3$) by element type (Beams, Columns, Walls, Slabs, Footings) and story.
   - Formwork contact area ($m^2$) by element type and story.
   - Rebar tonnage ($kg$) grouped by diameter categories: $\le 10\text{ mm}$, $\le 18\text{ mm}$, $> 18\text{ mm}$.
4. **`ClosedXmlBoqExporter`** in `HPDcePro.Excel`:
   - Multi-sheet workbook export without Excel COM:
     - `TongHop`: Executive summary and total quantities.
     - `BeTong`: Detailed concrete takeoff by story and element.
     - `VanKhuon`: Detailed formwork takeoff by story and element.
     - `CotThep`: Rebar schedule and tonnage breakdown.
     - `ChiTietTheoTang`: Story-by-story comprehensive bill of quantities.
5. **AutoCAD Commands Upgrade** in `HPDcePro.AutoCad`:
   - Upgrade `AutoStrCommands.cs`: `AutoStr`, `GETFORM`, `XuatDuLieuModelEtabs`, `UpdateModelEtabs`, `taomv`.
   - Upgrade `RebarCommands.cs`: `tkt_dce`, `stk_dce`, `TongHopThongKe_dce`, `QuetThongKe_dce`.
6. **Test Suite Expansion**:
   - `AutoStrPipelineTests.cs`: Pipeline execution, 24 steps progression, checkpointing, cancellation ($\ge 8$ tests).
   - `AutoStrResizingLoopTests.cs`: Column auto-resizing, pile auto-resizing, max factor guard ($\ge 6$ tests).
   - `QuantityTakeoffTests.cs`: Concrete, formwork, rebar tonnage calculations ($\ge 6$ tests).
   - `BoqExcelExportTests.cs`: Multi-sheet ClosedXML export, formula checks, cell formatting ($\ge 6$ tests).
   - Add at least 26+ new tests (bringing solution total to at least 238+ passing tests).
   - Verify `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` (0 warnings, 0 errors, `TreatWarningsAsErrors=true`).
   - Clean Code compliance: files $\le 300$ lines, methods $\le 50$ lines, 0 commented code, 0 unannotated catches.

Output:
Write a detailed handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_autostr_boq\handoff.md`.
Document exact build and test execution results.
Send a completion message back to orchestrator_21 when finished.


## 2026-10-17T23:04:19Z
You are worker_m4_autostr_boq, working in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_autostr_boq\.
Read your dispatch file at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_autostr_boq\DISPATCH.md and ORIGINAL_REQUEST.md.
Implement Milestone M4 (Phase 5): AutoStrOrchestrator and all 24 step processors in HPDcePro.Core/AutoStr/, iterative auto-resizing loop (CotChoTangTietDien, CocTuTangKhiKhongDat), QuantityTakeoffCalculator (concrete, formwork, rebar tonnage), ClosedXmlBoqExporter multi-sheet BOQ workbook export, upgrade AutoStrCommands and RebarCommands in HPDcePro.AutoCad, and create comprehensive tests in AutoStrPipelineTests.cs, AutoStrResizingLoopTests.cs, QuantityTakeoffTests.cs, and BoqExcelExportTests.cs.
Build and verify via dotnet build and dotnet test (--disable-build-servers -m:1). Verify TreatWarningsAsErrors=true (0 errors, 0 warnings) and all files <= 300 lines, methods <= 50 lines.
Write your detailed report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_autostr_boq\handoff.md and send a completion message back to orchestrator_21.
