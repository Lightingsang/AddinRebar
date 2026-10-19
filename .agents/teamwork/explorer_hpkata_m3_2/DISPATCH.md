# Dispatch Specification — M3 86-Param Config Sync Explorer (explorer_hpkata_m3_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M3, Feature 10)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## Your Mission:
1. Investigate Feature 10: 86-Parameter Config Synchronization:
   - Inspect `C:\HPKata\Goi lenh.lsp` (lines 152–237 holding the 86 config parameters, total exactly 238 split lines).
   - Inspect `KataConfigParser.cs` in `HPKata.Core/Configuration/` and `SetupViewModel.cs` / `SetupController.cs` (`setupkata` command) / `SetupCommand.cs`.
   - Architect complete roundtrip synchronization:
     - Reading all 86 parameters from `C:\HPKata\Goi lenh.lsp` into strongly typed `KataConfig` DTO.
     - Binding parameters to `SetupViewModel` tabs (General, Rebar, Dim, Text, Layer, Advanced).
     - Writing modified parameters back to `C:\HPKata\Goi lenh.lsp` preserving the EXACT 238 split lines, comments, and structure.
     - Synchronizing with AutoCAD active session via `SetupController.ApplySettings`.
2. Ensure Clean Code compliance:
   - File length <= 250 lines (Rule C2 <= 300).
   - Method length <= 40 lines (Rule M3 <= 50).
   - 0 unannotated catch blocks (Rule Q-B2).
3. Design unit tests in `HPKata.Core.Tests` and `HPKata.AutoCad.Tests` to verify:
   - 86-parameter full roundtrip parsing and serialization without line drift.
   - Isolation with `KataInstallEnvironment.UseRootDirectory(tempDir)` to protect master assets.
4. Write your comprehensive report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_2\handoff.md` and send a message to parent when done.

## 2026-10-18T05:10:09Z
[Message] timestamp=2026-10-18T05:10:09Z sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a priority=MESSAGE_PRIORITY_HIGH content=You are explorer_hpkata_m3_2 (M3 86-Param Config Sync Explorer)...
