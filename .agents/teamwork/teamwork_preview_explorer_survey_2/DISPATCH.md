# Task Assignment: Survey Explorer 2 (CAD Drafting, Commands & UI Dashboard)

- Subagent Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
1. Read `ORIGINAL_REQUEST.md` at header `## 2026-10-18T00:11:55Z` and investigate the drafting and CAD architecture:
   - Existing drafting generators in `HPDcePro.Core/Drafting/` (e.g. `CadDraftingBuffer`, `CadEntityPrimitive`, etc.).
   - Requirements for **R2: `SteelFrameDraftingGenerator`**: 2D CAD primitives for PEB portal frame elevations (`MiddleFrameBT`, `MiddleFrame3`, `MiddleFrame3BT`, `MiddleFrame5`, `MiddleFrame5a`, `VDS1`), roof purlin & monitor plans (`PlanPurlinRoof`, `PlanPurlinMonitor`), and anchor bolt details (`DetailAnchorBolt`).
   - Requirements for `SteelCadCommands` in `HPDcePro.AutoCad/Commands/`: command registrations, handling, and LISP aliases in `HPDcePro.lsp`.
2. Investigate the UI Dashboard & AutoCAD Bundle requirements (**R4**):
   - Existing `MahApps.Metro` windows in `HPDcePro.UI` (e.g. `BeamDetailingWindow`, `ColumnWallDetailingWindow`, etc.).
   - Requirements for `MainControlCenterWindow` and `MainControlCenterViewModel` launched by `GETFORM`, navigation commands, live project summary stats.
   - Requirements for `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml` for AutoCAD 2025–2026 ApplicationPlugins deployment.
3. Identify existing files, interfaces, models, and proposed file placements complying with clean code (<=300 lines/file, <=50 lines/method).
4. Output your findings into `handoff.md` in your working directory.

## 2026-10-18T00:14:25Z

You are Survey Explorer 2 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Explore the codebase and specifications for:
1. R2: DbimSteelAutocad Steel CAD Drafting Generator & Commands:
   - Inspect HPDcePro.Core/Drafting/ and existing generators (e.g. BeamDraftingGenerator, ColumnDraftingGenerator, CadDraftingBuffer, CadEntityPrimitive).
   - Detail the architecture and design for SteelFrameDraftingGenerator in HPDcePro.Core/Drafting/ for 2D CAD primitives:
     PEB portal frame elevations (MiddleFrameBT, MiddleFrame3, MiddleFrame3BT, MiddleFrame5, MiddleFrame5a, VDS1),
     roof purlin & monitor plans (PlanPurlinRoof, PlanPurlinMonitor),
     anchor bolt details (DetailAnchorBolt).
   - Detail the command handlers in HPDcePro.AutoCad/Commands/SteelCadCommands.cs and their LISP aliases in HPDcePro.lsp.
2. R4: Unified MainControlCenterWindow (GETFORM Hub) & AutoCAD .bundle Manifest:
   - Inspect existing MahApps.Metro windows in HPDcePro.UI.
   - Detail MainControlCenterWindow and MainControlCenterViewModel with navigation commands to the 6 existing windows and live project summary statistics.
   - Detail HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml for AutoCAD 2025–2026 ApplicationPlugins.
3. Clean code constraints and file decomposition strategy (keep all files <= 300 lines, methods <= 50 lines).

Write your detailed findings into:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\handoff.md
When finished, send a message to the orchestrator (caller) informing that you are done with the path to handoff.md.
