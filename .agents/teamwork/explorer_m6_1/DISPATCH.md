# DISPATCH — Milestone M6: Revit 2026 Architecture Explorer

**Assigned Agent**: `explorer_m6_1` (`teamwork_preview_explorer`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_1\`  
**Target Project**: `HPKata/HPKata.Revit/HPKata.Revit.csproj`  
**Date**: 2026-10-17  

---

## 1. Context & Mission

Milestones M1–M5 are complete (HPKata.Core, HPKata.UI, and HPKata.AutoCad 2026).
We are now initiating **Milestone M6: HPKata.Revit 2026 Add-in**.
Your task is to explore and design the Revit 2026 (.NET 8) add-in architecture for `HPKata.Revit`:
1. **Project Setup & Dependencies**:
   - `HPKata/HPKata.Revit/HPKata.Revit.csproj`:
     - Target framework: `net8.0-windows`
     - Project references to `../HPKata.Core/HPKata.Core.csproj` and `../HPKata.UI/HPKata.UI.csproj`
     - Revit 2026 API reference: `Revit_All_Main_Versions_API_x64` (Version `2026.0.0` or package reference `Nice3point.Revit.Api.RevitAPI` / local reference assemblies)
     - Add `HPKata.Revit.csproj` to `HPKata/HPKata.slnx`
2. **Lifecycle & Ribbon Setup**:
   - `IExternalApplication` implementation (`HPKataApplication.cs`): `OnStartup` and `OnShutdown`
   - Ribbon Tab `"HPKATA"` with Panel `"Cốt Thép (Rebar)"`:
     - PushButton `"Thiết Lập"` (`SetupCommand`: opens `SetupView` from `HPKata.UI`)
     - PushButton `"Vẽ & Sửa Thép"` (`DetailingCommand`: opens `DetailingView` from `HPKata.UI`)
     - PushButton `"Thống Kê Thép"` (`ScheduleCommand`: opens `ScheduleView` from `HPKata.UI`)
     - PushButton `"Tổ Hợp Cắt Thép"` (`CuttingStockCommand`: opens `CuttingStockView` from `HPKata.UI`)
     - PushButton `"Xuất Excel"` (`ExportExcelCommand`: headless export via ClosedXML)
3. **WPF Dialog Hosting in Revit 2026**:
   - Safe hosting of WPF dialogs using Revit window owner: `System.Windows.Interop.WindowInteropHelper` with `uiApp.MainWindowHandle`
   - Theme sync: read Revit theme or synchronize with `ThemeManager` in `HPKata.UI`
4. **Clean Code Governance**:
   - Ensure all classes $\le 300$ lines (Rule C2), methods $\le 50$ lines (Rule M3), 0 dead code (Rule Q-B1), 0 unannotated catches (Rule Q-B2).
5. **Deliverables**:
   - Deliver `analysis.md` and `handoff.md` with complete architectural blueprints.


## 2026-10-17T04:35:09Z
[Message] timestamp=2026-10-17T04:35:09Z sender=4300a9ce-e3eb-419c-8493-f2001d2d04d6 priority=MESSAGE_PRIORITY_HIGH content=You are explorer_m6_1, the Revit 2026 .NET 8 Architecture Explorer for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_1\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 1, 9, 10).
4. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md.

YOUR TASK:
Investigate and design the Revit 2026 (.NET 8) add-in architecture for HPKata.Revit:
1. Project configuration: HPKata.Revit.csproj targeting net8.0-windows, referencing Revit 2026 API (Revit_All_Main_Versions_API_x64 2026.0.0 or Nice3point.Revit.Api.RevitAPI / reference assemblies with PrivateAssets="all"), HPKata.Core, and HPKata.UI.
2. Lifecycle management: IExternalApplication (OnStartup and OnShutdown), registering Ribbon tab "HPKATA" with push buttons for Setup, Detailing, Schedule, Cutting Stock, and Export.
3. WPF Host Integration: Safe hosting of WPF dialogs (SetupView, DetailingView, ScheduleView, CuttingStockView from HPKata.UI) from Revit commands using Autodesk.Revit.UI.UIFramework.RevitColors or WindowInteropHelper with MainWindowHandle.
4. Clean Code structure: Ensure all classes <= 300 lines (Rule C2) and methods <= 50 lines (Rule M3).
5. Deliver structured analysis in analysis.md and complete handoff report in handoff.md.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
