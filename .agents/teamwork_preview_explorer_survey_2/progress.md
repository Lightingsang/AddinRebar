# Progress — Survey Explorer 2 (Tekla Structures 2025.0 Open API & Bridge Architecture)

- Last visited: 2026-09-22T00:31:00Z
- Status: In Progress
- Current step: Synthesizing findings and authoring report.md and handoff.md
- Completed steps:
  1. Verified Tekla Structures 2025.0 installation at `C:\Program Files\Tekla Structures\2025.0\bin` (version 2025.0.48669.0, .NET Framework 4.8).
  2. Inspected key reference assemblies (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Drawing.dll`, `Tekla.Structures.Plugins.dll`, `Tekla.Structures.Dialog.dll`).
  3. Identified registry paths (`HKLM:\SOFTWARE\Trimble\Tekla Structures\2025.0\setup`), environment layout, and extension directories.
  4. Analyzed in-process plugin mechanisms: `PluginBase`, Custom Tabs Ribbon XML (`Ribbon-Custom-*.xml`), and macros (`.cs` in modeling macros).
  5. Solved thread synchronization: `MainThreadQueue` with `ComponentDispatcher.ThreadIdle` / WPF Dispatcher and `PostMessage(WM_NULL)` / `BeginInvoke`.
  6. Discovered native rollback mechanism: `Tekla.Structures.ModelInternal.Operation.SetTestSavePoint()` and `RollbackToTestSavePoint()` for bulletproof `dryRun = true` rollback.
  7. Formulated snapshot backup mechanism for model database files (`.db1`, `.db2`, `environment.db`).
  8. Compared in detail with `HPNavis.McpBridge` (net48), `HPAutoCad.McpBridge`, and `HPRobot.McpBridge`.
