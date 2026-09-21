# BRIEFING — 2026-09-21T19:16:00Z

## Mission
Remediate the 4 defects in HPTekla live verification harness (`live-verify.py` and `run-live-verify.ps1`) identified by Challenger 2 and empirically verify all fixes.

## 🔒 My Identity
- Archetype: Worker M4 Gen 2 (HPTekla Live Harness Remediation)
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2
- Original parent: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Milestone: Milestone 4 (Live Verification Harness Review & Adversarial Stress Testing)

## 🔒 Key Constraints
- Exclusive write ownership: `HPTekla/tools/harness/live-verify.py`, `HPTekla/tools/harness/run-live-verify.ps1`, `.agents/teamwork_preview_worker_m4_gen2/**`
- No cheating, no fake outputs, genuine implementation of validation and arguments.
- Maintain compatibility with `McpShared/tools/harness_common.py`.
- Strict stage validation rejecting invalid stages with code 2 and `parser.error`.
- Support `--json` flag cleanly in `live-verify.py`.
- Support `-WhatIf` via `[CmdletBinding(SupportsShouldProcess)]` and propagate non-zero exit codes in `run-live-verify.ps1`.
- Align Stage F test IDs between detached skip mode and live execution mode.

## Current Parent
- Conversation ID: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Updated: not yet

## Task Summary
- **What to build**: Fix 4 defects in `HPTekla/tools/harness/live-verify.py` and `run-live-verify.ps1`.
- **Success criteria**:
  1. `live-verify.py --stages Z` exits with code 2 via `parser.error`.
  2. `live-verify.py --json` succeeds with valid JSON output.
  3. `run-live-verify.ps1 -WhatIf` does not spawn process and exits 0; invalid stages exit non-zero without printing `[SUCCESS]`.
  4. Stage F test IDs consistently map F1: `create_beam`, F2: `get_part_properties`, F3: `create_rebar_group`, F4: `export_ifc`.
  5. Empirical verification of all 4 fixes passes.
- **Interface contracts**: `McpShared/tools/harness_common.py`, Tekla MCP tool specifications.
- **Code layout**: `HPTekla/tools/harness/`

## Key Decisions Made
- Use `parser.error()` in `live-verify.py` for invalid stages and empty stages to match standard CLI conventions and exit code 2.
- In `run-live-verify.ps1`, validate `$Stages` parameter via `[ValidatePattern('^[A-Fa-f,\s]+$')]` and handle `$PSCmdlet.ShouldProcess` for `-WhatIf`.
- Align Stage F test IDs across detached skip mode and live execution mode with F1: `create_beam`, F2: `get_part_properties`, F3: `create_rebar_group`, F4: `export_ifc`.

## Artifact Index
- `.agents/teamwork_preview_worker_m4_gen2/DISPATCH.md` — assignment and instructions
- `.agents/teamwork_preview_worker_m4_gen2/BRIEFING.md` — working memory
- `.agents/teamwork_preview_worker_m4_gen2/progress.md` — heartbeat and progress tracking
- `.agents/teamwork_preview_worker_m4_gen2/report.md` — detailed remediation report
- `.agents/teamwork_preview_worker_m4_gen2/handoff.md` — 5-component handoff report

## Change Tracker
- **Files modified**:
  - `HPTekla/tools/harness/live-verify.py`: Added strict stage validation with `parser.error`, `--json` argument, and aligned Stage F test IDs.
  - `HPTekla/tools/harness/run-live-verify.ps1`: Added `[CmdletBinding(SupportsShouldProcess)]`, `$Stages` pattern validation, `-WhatIf` handling, `-Json` forwarding, and exit code propagation.
- **Build status**: Verification passed (all 4 defects resolved, suite 100% passing).
- **Pending issues**: None.

## Quality Status
- **Build/test result**: 13 passed, 4 skipped (detached), 0 failed across full test harness.
- **Lint status**: Clean.
- **Tests added/modified**: Empirical CLI and PowerShell parameter verification for all 4 defect fixes.

## Loaded Skills
- None
