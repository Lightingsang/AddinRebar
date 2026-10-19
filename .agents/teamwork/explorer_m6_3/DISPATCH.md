# DISPATCH — Milestone M6: Revit Headless Testing & Scheduling Runner Explorer

**Assigned Agent**: `explorer_m6_3` (`teamwork_preview_explorer`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_3\`  
**Target Project**: `HPKata/HPKata.Revit.Tests/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission

Milestones M1–M5 are complete.
We are now initiating **Milestone M6: HPKata.Revit 2026 Add-in**.
Your task is to explore and design the headless test architecture, mock context, and schedule command runners for `HPKata.Revit`:
1. **Decoupled Document Interface**:
   - Similar to `ICadDocumentContext` in `HPKata.AutoCad`, design `IRevitDocumentContext`:
     - `GetActiveRebarElements()`
     - `WriteNotification(string message)`
     - `CreateScheduleView(...)` or insert schedule parameters
   - Design `MockRevitDocumentContext` for testing in `HPKata.Revit.Tests`.
2. **Revit Command Runners**:
   - `RevitScheduleRunner.cs`:
     - Collects all `Rebar` elements from document (or `IRevitDocumentContext`).
     - Converts each to `HPKata.Core.Models.RebarItem` via parameter bridge.
     - Aggregates by `Partition` and `Mark`.
     - Calculates total lengths, cutting lengths, unit masses, and group totals.
   - `RevitCuttingStockRunner.cs`:
     - Extracts cutting stock demand from active Revit schedule.
     - Invokes `CuttingStockEngine.Solve(demand, config)` with 11,700 mm stock length.
     - Formats cutting patterns and offcut summaries.
   - `RevitExportExcelRunner.cs`:
     - Calls `ClosedXmlExporter.ExportSchedule(items, path)`.
3. **Headless Unit Test Suite (`HPKata.Revit.Tests.csproj`)**:
   - Target framework: `net8.0-windows`
   - xUnit v3 with Microsoft Testing Platform (`Microsoft.Testing.Platform`)
   - Test cases:
     - Parameter mapping with mock Revit rebars (all standard properties).
     - Decimal feet to metric mm conversion accuracy and tolerances.
     - Shape mapping (Revit shape codes to Kata shapes).
     - Schedule aggregation runner against `MockRevitDocumentContext`.
     - 1D cutting-stock solver invocation from Revit data.
     - Target: >= 25 headless tests passing 100% green without requiring a live `Revit.exe` process.
4. **Clean Code Governance**:
   - Rule C2 $\le 300$ lines, Rule M3 $\le 50$ lines, 0 dead code, 0 unannotated catches.
5. **Deliverables**:
   - Deliver `analysis.md` and `handoff.md` with complete architecture and test specifications.


## 2026-10-17T04:35:09Z
You are explorer_m6_3, the Revit Headless Testing & Scheduling Runner Explorer for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_3\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_3\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 7, 8, 9).
4. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md.

YOUR TASK:
Investigate and design headless testing, mock contexts, and command runners for HPKata.Revit:
1. Decoupled Context: Design IRevitDocumentContext and MockRevitDocumentContext to allow 100% headless execution of scheduling and cutting stock commands.
2. Command Runners: Design RevitScheduleRunner, RevitCuttingStockRunner, and RevitExportExcelRunner accepting IRevitDocumentContext.
3. Headless Test Architecture: Design HPKata.Revit.Tests.csproj (net8.0-windows, xUnit v3, MTP runner) with >= 25 tests covering parameter mapping, unit conversions, shape adapters, and scheduling runners without needing a live Revit.exe.
4. Clean Code structure: Ensure all classes <= 300 lines (Rule C2) and methods <= 50 lines (Rule M3).
5. Deliver structured analysis in analysis.md and complete handoff report in handoff.md.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
