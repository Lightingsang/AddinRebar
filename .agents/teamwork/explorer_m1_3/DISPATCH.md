# DISPATCH: explorer_m1_3 (M1 Codebase Purge & Integrity Explorer)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\`
- Milestone: M1 — Standalone C:\HPKata\ Environment & Asset Migration
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`

## Mission
Investigate and produce the exact implementation specification for Feature 4 (Complete purge of all 9 `kata_pro` occurrences in `HPKata/`):
1. Exact edits for:
   - `HPKata/HPKata.AutoCad/Commands/SetupController.cs` (line 50) -> use `KataInstallEnvironment.Current.GoiLenhLspPath`
   - `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp` (lines 7 & 42) -> `C:\HPKata\HPKata.AutoCad.dll` and `;HPKATA@`
   - `HPKata/HPKata.Core.Tests/Configuration/KataConfigParserTests.cs` (lines 11 & 83) -> use `KataInstallEnvironment.Current.GoiLenhLspPath` and `C:\HPKata`
   - `HPKata/tools/run_m3_empirical_challenge_tests.ps1` (lines 221, 222, 247) -> replace with `C:\HPKata\Goi lenh.lsp`
   - `HPKata/tools/run_m2_empirical_tests.ps1` (line 10) -> replace with `C:\HPKata\Goi lenh.lsp`
2. Specify static analysis check: automated PowerShell script / unit test that verifies 0 occurrences of `kata_pro` (case-insensitive) across all `.cs`, `.xaml`, `.lsp`, `.csproj`, `.xml`, `.addin` files in `HPKata/`.
3. Verify zero regressions: ensure all 926 existing tests pass cleanly.

Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\handoff.md`.

## 2026-10-18T02:47:26Z
[Message] sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a
You are explorer_m1_3 (M1 Codebase Purge & Integrity Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\DISPATCH.md
Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Provide the exact replacement diffs for all 9 occurrences of kata_pro across SetupController.cs, GoiLenh_HPKata.lsp, KataConfigParserTests.cs, and the 2 test scripts.
2. Specify a static analysis test to add to HPKata.Core.Tests or HPKata.E2E.Tests that enforces zero occurrences of 'kata_pro' (case-insensitive) across all source files.
3. Verify that these edits cause zero regressions across all 926 existing tests.
4. Write your report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\handoff.md
5. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
