# Dispatch Log

## 2026-09-27T15:57:37Z

You are the Project Orchestrator for the Kata Rebar feature in the HPRebar ecosystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9
Your context file is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_9\context.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-27T15:57:37Z.
Project working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Mission:
Implement the **Kata Rebar** feature in the `HPRebar` ecosystem, enabling automated generation of 3D concrete beam reinforcement in Revit 2026 based on structural calculation and detailing data read from sheet `Dam` of `Kata.xlsm` (via active COM or file fallback).

Scope:
1. R1: Kata Dam Sheet Data Parser in `HPRebar.Core` (DTOs `KataBeamRebarSpec`, dual source COM + ClosedXML fallback).
2. R2: Rebar Geometry & Distribution Calculator in `HPRebar.Core` (pure maths, 3D curves, covers, cutoffs, stirrups, 0 Revit references).
3. R3: Revit 3D Rebar Generation & Idempotent Update in `HPRebar` (match beam selection, smart RebarBarType resolution, native `Rebar.CreateFromCurves` / `CreateFromCurvesAndShape`, tag `Comments` = `"HPRebar_Kata_{BeamName}"`, delete previous rebars on re-run).
4. R4: User Interface & Ribbon Integration in `HPRebar` (Ribbon button "Kata Rebar" adjacent to "Kata Export", WPF MVVM dialog with ThemeDark/ThemeLight dynamic styling and MaterialDesign tokens, preview & transaction commit).
5. Comprehensive unit tests in `HPRebar.Core.Tests` passing 100%, and clean build `dotnet build HPRebar.slnx -c Debug.R26`.

Orchestration rules:
- Decompose into structured milestones/phases (e.g. Phase 1: Core DTOs & Sheet Parser; Phase 2: Core Rebar Geometry Calculator; Phase 3: xUnit Test Suites; Phase 4: Revit Feature & Rebar Creation Service; Phase 5: WPF MVVM UI & Ribbon; Phase 6: Build & Integration Verification).
- Dispatch tasks to specialized subagents (explorers, workers, reviewers, challengers) following file workspace conventions (.agents/<type>_<milestone>...).
- Regularly update your progress.md in your working directory (.agents/orchestrator_9/progress.md).
- When fully complete and all verification passes, send a completion report message to the Sentinel (caller ID) claiming victory, so the Sentinel can trigger the independent Victory Auditor.
