# Forensic Audit Assignment: Milestone M5 Documentation Integrity Forensics

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m5_1`

## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (§ 2026-09-21T09:44:29Z)
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6\PROJECT.md` (§ Milestone M5)
3. Worker M5 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1\handoff.md`

## Mission
Conduct a rigorous forensic integrity audit on Milestone M5:
1. Documentation veracity & fabrication audit:
   - Check if the capabilities, tools, parameters, and behaviors documented in `SKILL.md` and `AGENTS.md` genuinely match the actual codebase implementation.
   - Verify that no fabricated tools or nonexistent flags are documented.
2. Architecture isolation audit:
   - Verify that documentation does not introduce or prescribe cross-wiring with sibling host projects.
3. Solution integrity:
   - Run `dotnet build HPExcel/HPExcel.slnx -c Debug` to confirm 0 errors/warnings.
   - Run tests to confirm non-regression.
4. Verdict determination:
   - Must be **CLEAN** or **INTEGRITY VIOLATION**.

## Handoff Requirements
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m5_1\handoff.md`
State clearly your verdict: **CLEAN** or **INTEGRITY VIOLATION**.
Send a completion message back when done.

## 2026-09-21T11:44:32Z
**Context**: Server restarted. Resume Milestone M5 Forensic Integrity Audit.
**Content**: Please resume your forensic integrity audit of Milestone M5 per DISPATCH.md. Audit documentation veracity in `.agents/skills/hp-mcp-excel/SKILL.md` and `AGENTS.md`, architecture isolation, and run build/test verification. Write your complete report to handoff.md and deliver your binary verdict (CLEAN or INTEGRITY VIOLATION).
**Action**: Finish audit and send completion message back.
