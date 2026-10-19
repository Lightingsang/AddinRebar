# Dispatch Instructions: reviewer_m4_2_gen2

## Identity
- Role: Adversarial Reviewer & Persona/BOQ Inspector
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## Scope & Adversarial Inspection
1. Command Execution & Edge Cases:
   - Does `CliParser` handle malformed flags, missing option values, empty args, and nulls without throwing unhandled exceptions?
   - Does `ValidateCommand` properly abort and return non-zero exit codes when given invalid inputs or non-existent files?
   - Does `EstimateCommand` correctly compute concrete, formwork, rebar (7966.5 kg), and MEP quantities?
2. AI Agent Roles Review:
   - Review all 6 prompt files in `HPBim/agents/`:
     * `kien-truc-su.md`, `ky-su-ket-cau.md`, `ky-su-co-dien.md`, `kiem-soat-ban-ve.md`, `ky-su-du-toan.md`, `dieu-phoi-bim.md`
   - Verify alignment with Vietnamese standards (TCVN 4451:2012, TCVN 5574:2018, TCVN 4513:1988, TT 12/2021/TT-BXD, etc.).
   - Verify alignment with the 5-step BIM Lifecycle and CLI syntax (`validate`, `generate`, `verify`, `estimate`).
3. Clean Code Compliance:
   - File length <= 200 lines, method length <= 40 lines.
4. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T03:35:02Z
You are reviewer_m4_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read worker handoff at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen2\handoff.md

Adversarially review HPBim.Cli commands and the 6 AI Agent Roles in HPBim/agents/.
Check command parsing error handling, BOQ estimation formulas, and agent role prompts adhering to Vietnamese construction standards.
Run tests: dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
Write handoff.md with verdict APPROVE or REQUEST_CHANGES and report back via send_message.
