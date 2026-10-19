# DISPATCH — Gate M6: Revit 2026 Architecture & Lifecycle Reviewer

**Assigned Agent**: `reviewer_m6_1` (`teamwork_preview_reviewer`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_1\`  
**Target Project**: `HPKata/HPKata.Revit/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission
Milestone M6 (`HPKata.Revit` and `HPKata.Revit.Tests`) has been implemented by `worker_m6`.
Your task is to conduct an objective, rigorous architecture and lifecycle review of `HPKata.Revit`:
1. Audit project configuration:
   - `HPKata.Revit.csproj`: `net8.0-windows`, `UseWPF=true`, references `Nice3point.Revit.Api.RevitAPI` (2026.0.4) with `PrivateAssets="all"` and `ExcludeAssets="runtime"`. Verify no Revit host binaries are copied into output.
   - `HPKata.Revit.addin`: Verify AddInId and Type="Application" pointing to `HPKata.Revit.Entry.HPKataApplication`.
2. Review lifecycle & UI integration:
   - `HPKataApplication.cs` (`IExternalApplication`): `OnStartup` and `OnShutdown` lifecycle, Ribbon Tab `"HPKATA"` and Panel `"Cốt Thép (Rebar)"` registration.
   - `RibbonBuilder.cs` & `RibbonIconFactory.cs`: 5 PushButtons (`Thiết Lập`, `Vẽ & Sửa Thép`, `Thống Kê Thép`, `Tổ Hợp Cắt Thép`, `Xuất Excel`), dynamic vector DrawingImage icons.
   - `RevitHostTheme.cs`: Dynamic theme synchronization with `Autodesk.Revit.UI.UIThemeManager.CurrentTheme`.
   - `RevitWindowHelper.cs`: Win32 owner assignment via `WindowInteropHelper.Owner = uiApp.MainWindowHandle` and `ThemeManager.Attach`.
3. Review 3D Rebar geometry creation:
   - `RebarCreationService.cs` & `KataRebarCurveFactory.cs`: Native rebar creation using Revit 2026's modern `BarTerminationsData`.
4. Audit Clean Code compliance:
   - Rule C2: all files $\le 300$ lines.
   - Rule M3: all method bodies $\le 50$ lines.
   - Rule Q-B1: zero commented-out code.
   - Rule Q-B2: zero unannotated empty catch blocks.
5. Resource bounds:
   - Solution already built. If running tests, use `--no-build --disable-build-servers -m:1`.
   - Concurrency isolation: pass `--results-directory "$env:TEMP\mtp-rev61-$([guid]::NewGuid())"`.
6. Deliver findings in `review.md` and explicit verdict in `handoff.md`: `APPROVE` or `REQUEST_CHANGES`.


## 2026-10-17T05:04:39Z
You are reviewer_m6_1, the Architecture, Lifecycle & Ribbon Reviewer for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m6_1\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 1, 9, 10).
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md and docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built and verified the solution in Release mode.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-rev61-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Perform an objective architecture and Clean Code review of Milestone M6:
1. Audit project configuration: HPKata.Revit.csproj (net8.0-windows, ExcludeAssets="runtime" PrivateAssets="all", EnableDynamicLoading=true) and HPKata.Revit.addin.
2. Review lifecycle & hosting: HPKataApplication (IExternalApplication, Ribbon Tab "HPKATA" and Panel "Cốt Thép (Rebar)"), RibbonBuilder, RibbonIconFactory, RevitHostTheme (UIThemeManager.CurrentTheme sync), and RevitWindowHelper (safe modal hosting).
3. Review 3D Rebar creation: RebarCreationService & KataRebarCurveFactory (leveraging Revit 2026 BarTerminationsData).
4. Audit Clean Code compliance: Rule C2 (<= 300 lines), Rule M3 (<= 50 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (annotated catches).
5. Execute test suite: dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-rev61-$([guid]::NewGuid())".
6. Record findings in review.md and deliver handoff.md with explicit verdict: APPROVE or REQUEST_CHANGES.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.
