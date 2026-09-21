# Dispatch for Survey Explorer 2: Tekla Structures 2025.0 Open API & Bridge Architecture

## 2026-09-21T17:22:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_2
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (read header ## 2026-09-21T17:20:33Z)
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Investigate Tekla Structures 2025.0 Open API environment, assemblies, and In-Process Bridge design:
1. Verify Tekla Structures 2025.0 installation path, reference assemblies location (e.g. `C:\Program Files\Tekla Structures\2025.0\bin` or similar), versions, and key DLLs:
   - Tekla.Structures.dll
   - Tekla.Structures.Model.dll
   - Tekla.Structures.Catalogs.dll
   - Tekla.Structures.Datatype.dll
   - Tekla.Structures.Drawing.dll (if present)
2. Study In-Process Plugin / Extension architecture for Tekla Structures (.NET Framework 4.8, CLR v4.0.30319):
   - Plugin registration attribute (`[Plugin("...")]`, `[PluginUserInterface("...")]`), PluginBase, or Application Extension / Ribbon integration.
   - UI thread vs Tekla Model thread execution: how to safely queue and execute Roslyn scripts on the Tekla thread using McpBridge.Core (`MainThreadQueue` or custom queue).
   - How `dryRun = true` rollback should be enforced (e.g. not calling `model.CommitChanges()` or rolling back uncommitted changes).
   - Model snapshot management: how Tekla model files (`.db1`, `.db2`, model folder) can be backed up / snapshot before mutating scripts.
   - Ribbon button and WPF Status window (Theme, connection indicator, log view, safety toggles).
3. Compare with existing bridges: `HPNavis.McpBridge` (which also targets `net48`), `HPAutoCad.McpBridge`, and `HPRobot.McpBridge`.
4. Output report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_2\report.md and handoff.md.
