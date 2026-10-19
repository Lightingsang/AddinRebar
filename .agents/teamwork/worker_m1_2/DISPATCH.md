# Dispatch: Worker M1.2 (Truth Matrix Remediation & Code Parity)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_2\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_worker`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (`## 2026-10-18T05:27:06Z`)
- Review Feedback: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_1\handoff.md`

## File Ownership
- Write Ownership: `docs/rea-dce-pro/TRUTH_MATRIX.md`, `src/HPDcePro.AutoCad/Commands/RebarCommands.cs`, and metadata in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_2\`.
- Strictly Read-Only: `C:\Dce Pro\` (never modify any file in `C:\Dce Pro`).

## Tasks
1. Update `docs/rea-dce-pro/TRUTH_MATRIX.md`:
   - Reclassify the 8 mock commands to `ĐANG LÀ MOCK` with exact source file paths and lines:
     - `GetForm`: `src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs:23`
     - `TMCOC`: `src/HPDcePro.AutoCad/Commands/FootingCommands.cs:115`
     - `TKTSSoLuongCauKien`: `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:24`
     - `TKTSDuongKinhThep`: `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:41`
     - `TKT_DCE`: `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:58`
     - `STK_DCE`: `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:74`
     - `TongHopThongKe_DCE`: `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:81`
     - `QuetThongKe_DCE`: `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:115`
   - Fix `ChiaKhoangCach` (row 67): point to `src/HPDcePro.AutoCad/Commands/SlabCommands.cs:135`.
   - Update summary counts in table (Section 1):
     - Subsystem 1: 0 HOÀN THÀNH, 64 ĐANG LÀ MOCK, 247 CHƯA GIẢI MÃ ĐƯỢC (Total 311).
     - Grand Total: 130 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 362 CHƯA GIẢI MÃ ĐƯỢC (Total 602).
   - Add footnote to Subsystems 7 & 8 clarifying that `Jigs/*.cs` and `Views/*.xaml` represent the M3 clean-room target architecture.
2. In `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:128`:
   - Rename the colliding duplicate `[CommandMethod("ChiaKhoangCach")]` to `[CommandMethod("ChiaKhoangCachDaiDam")]` to eliminate command name collisions in AutoCAD.
3. Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` to verify all tests pass with 0 errors and 0 warnings.
4. Report results in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_2\handoff.md`.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.


## 2026-10-18T06:22:52Z
You are Worker M1.2 for Milestone M1 Iteration 2 (Truth Matrix Remediation).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_2\
Read your dispatch specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_2\DISPATCH.md
Read the review findings from Reviewer M1.1: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_1\handoff.md.
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (`## 2026-10-18T05:27:06Z`).
Read the project specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md.

File ownership: `docs/rea-dce-pro/TRUTH_MATRIX.md` and `src/HPDcePro.AutoCad/Commands/RebarCommands.cs`.
Strictly read-only on `C:\Dce Pro`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Mission:
1. Update `docs/rea-dce-pro/TRUTH_MATRIX.md`:
   - Reclassify the 8 mock commands to `ĐANG LÀ MOCK` with exact source file paths and lines (`GetForm`, `TMCOC`, `TKTSSoLuongCauKien`, `TKTSDuongKinhThep`, `TKT_DCE`, `STK_DCE`, `TongHopThongKe_DCE`, `QuetThongKe_DCE`).
   - Fix `ChiaKhoangCach` (row 67): point to `src/HPDcePro.AutoCad/Commands/SlabCommands.cs:135`.
   - Update summary counts in table (Section 1):
     - Subsystem 1: 0 HOÀN THÀNH, 64 ĐANG LÀ MOCK, 247 CHƯA GIẢI MÃ ĐƯỢC (Total 311).
     - Grand Total: 130 HOÀN THÀNH, 110 ĐANG LÀ MOCK, 362 CHƯA GIẢI MÃ ĐƯỢC (Total 602).
   - Add footnote to Subsystems 7 & 8 clarifying that `Jigs/*.cs` and `Views/*.xaml` represent the M3 clean-room target architecture.
2. In `src/HPDcePro.AutoCad/Commands/RebarCommands.cs:128`:
   - Rename the colliding duplicate `[CommandMethod("ChiaKhoangCach")]` to `[CommandMethod("ChiaKhoangCachDaiDam")]`.
3. Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` to verify all tests pass with 0 errors and 0 warnings.
4. Report results in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_2\handoff.md`.
Communicate your completion back to parent via send_message.
