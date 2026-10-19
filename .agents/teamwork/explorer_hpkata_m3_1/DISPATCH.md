# Dispatch Specification — M3 Template Importer Explorer (explorer_hpkata_m3_1)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_1\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M3, Feature 9)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## Your Mission:
1. Investigate Feature 9: `KataDwgTemplateImporter` in `HPKata.AutoCad`:
   - Inspect `C:\HPKata\Iso_kata.dwg` and existing template import logic in `HPKata.AutoCad/Interop/`.
   - Architect `IKataDwgTemplateImporter` and `KataDwgTemplateImporter` to clone 119 block definitions, 12 layers, TextStyleTableRecord, and DimStyleTableRecord from `C:\HPKata\Iso_kata.dwg` into active AutoCAD database using AutoCAD .NET `Database.Insert` / `WblockCloneObjects` with `DuplicateRecordCloning.Ignore` or `Replace`.
   - Include robust fallback if `Iso_kata.dwg` is missing or locked.
2. Ensure Clean Code compliance:
   - File length <= 250 lines (Rule C2 <= 300).
   - Method length <= 40 lines (Rule M3 <= 50).
   - 0 unannotated catch blocks (Rule Q-B2).
3. Design unit/mock tests in `HPKata.AutoCad.Tests` to verify:
   - Template path resolution via `KataInstallEnvironment.Current.IsoKataDwgPath`.
   - Block cloning logic, duplicate name handling, layer import, text style import.
4. Write your comprehensive report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_1\handoff.md` and send a message to parent when done.

## 2026-10-18T05:10:09Z
You are explorer_hpkata_m3_1 (M3 Template Importer Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_1\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_1\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M3, Feature 9).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

