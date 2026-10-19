# DISPATCH — Survey Explorer 3

## Mission
Survey HPKata.Revit commands (FramingRebar, ColumnRebar, WallRebar), element geometry extraction, picker fallback logic, and HPKata.UI.Tests infrastructure.

## Key Instructions
1. Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (specifically entry dated `2026-10-17T14:27:21Z`).
2. Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\DISPATCH.md`.
3. Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\`
4. Inspect:
   - `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, `WallRebarCommands.cs`.
   - Structural element geometry extraction mechanisms in `HPKata.Revit` (check if `RevitElementGeometryReader.cs` exists or needs creation; inspect parameters `b`, `h`, `Width`, `Height`, `Depth`, `Length`, bounding boxes, conversion from internal feet to mm using `RevitUnitConverter`).
   - Pre-selection vs interactive picking with `OperationCanceledException` (ESC key) fallback to manual mode, and executing rebar generation on `ShowDialog() == true`.
   - `HPKata/HPKata.UI.Tests/` setup (xUnit, STA thread testing requirements, existing test fixtures, structure of `Views/AllWindowsXamlInstantiationTests.cs` if any).
5. Write your comprehensive survey report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\handoff.md`.
6. Send a message to orchestrator_19 when complete.


## 2026-10-17T14:31:31Z

You are teamwork_preview_explorer_survey_3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).

Your mission is to survey HPKata.Revit commands (FramingRebar, ColumnRebar, WallRebar), element geometry extraction, picker fallback logic, and HPKata.UI.Tests infrastructure.
Inspect:
- HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs, ColumnRebarCommands.cs, WallRebarCommands.cs.
- Structural element geometry extraction mechanisms in HPKata.Revit (check if RevitElementGeometryReader.cs exists or needs creation; inspect parameters b, h, Width, Height, Depth, Length, bounding boxes, conversion from internal feet to mm using RevitUnitConverter).
- Pre-selection vs interactive picking with OperationCanceledException (ESC key) fallback to manual mode, and executing rebar generation on ShowDialog() == true.
- HPKata/HPKata.UI.Tests/ setup (xUnit, STA thread testing requirements, existing test fixtures, structure of Views/AllWindowsXamlInstantiationTests.cs if any).

Write your detailed survey report to handoff.md in your working directory.
When done, send a message to orchestrator_19 with your handoff summary.
