## 2026-09-21T13:18:23Z

You are Explorer 2 (RobotOM API Researcher) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_2\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\context.md.

YOUR MISSION:
Investigate and document the Autodesk Robot Structural Analysis Professional 2026 COM API (`RobotOM.dll`), installation environment, type definitions, and interaction patterns.

INVESTIGATION SCOPE:
1. Locate and inspect the Robot 2026 installation and COM type library:
   - Check `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` or `RobotOM.dll`.
   - Check if there are tlb files or GAC / registry registrations (`IRobotApplication`, ProgID `Robot.Application`).
   - Check how C# projects in .NET 8 / .NET 10 can reference or interop with `RobotOM.dll` (COM reference, Direct reference to Interop.RobotOM.dll, embed interop types vs reference assembly).
2. Deep-dive into `RobotOM` object model:
   - Root: `RobotApplicationClass` / `IRobotApplication`, `IRobotProject`, `IRobotStructure`.
   - Structural objects:
     - Nodes: `IRobotNodeServer`, creating nodes by coordinates (X, Y, Z), node numbers.
     - Bars: `IRobotBarServer`, creating bars between start node and end node, setting section.
     - Panels / Contours / Slabs: `IRobotObjObjectServer`, creating panels, thickness.
   - Properties & Labels:
     - `IRobotLabelServer`: Materials (`I_LT_MATERIAL`), Bar sections (`I_LT_BAR_SECTION`), Supports (`I_LT_NODE_SUPPORT`).
     - Support definitions: Fixed, pinned, roller, UX/UY/UZ/RX/RY/RZ releases.
   - Loads:
     - `IRobotCaseServer`: Dead load, live load, wind, seismic, load combinations.
     - `IRobotLoadRecord`: Uniform bar load, concentrated bar load, nodal force, self-weight.
   - Calculations & FEA:
     - Running calculations: `Project.CalcEngine.Calculate()`. Status query and progress handling.
   - Results:
     - Node reactions: `IRobotNodeForceData`, extreme values, cases.
     - Bar internal forces: `IRobotBarForceServer` (FX, FY, FZ, MX, MY, MZ) along bar points.
   - Grids & Axes:
     - `IRobotStructuralAxisServer`, structural grids, coordinate systems.
3. Units & RobotUnitsPolicy:
   - How Robot stores and configures units: `Preferences.GetUnitsAndFormats()`, length units, force units, moment units, stress units.
   - Standardize to Metric (Meter, kN, kN·m, MPa) during script execution and restore user units afterwards.
4. Script execution model:
   - How globals `robot` (`IRobotApplication`), `structure` (`IRobotStructure`), `args`, `units` are initialized and passed to Roslyn script compiler.

DELIVERABLES:
Write your comprehensive investigation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_2\analysis.md`
And write your self-contained handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_2\handoff.md`
When finished, send a message to your parent with summary and file paths.
