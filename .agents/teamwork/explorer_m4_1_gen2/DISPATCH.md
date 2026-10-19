# Dispatch Instructions: explorer_m4_1_gen2

## Identity
- Role: CLI & Agent Packaging Architecture Explorer
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_1_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## Inputs to Review
1. `ORIGINAL_REQUEST.md`:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `PROJECT.md`:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md`
3. Domain contracts in `HPBim.Core` and client services in `HPBim.Client`.

## Exploration Objectives
1. **Design `HPBim.Cli` (`net10.0`)**:
   - CLI parsing approach: lightweight, zero extra dependencies or standard System.CommandLine / simple argument parser ensuring clean decoupled code.
   - Design 4 commands:
     * `validate`: loads JSON, invokes `QcEngine.Validate(model)`, renders formatted ASCII/Unicode report table, returns exit code 0 or 1.
     * `generate`: options `--input <path>`, `--dry-run`, `--pipe <name>`. Invokes `BimPipelineEngine.ExecuteAsync()`, renders live stage progress, element counts, rollback confirmation if dry-run.
     * `verify`: options `--input <path>`, `--pipe <name>`. Invokes `RevitModelVerifier.VerifyAsync()`, renders expected vs actual element counts comparison table.
     * `estimate`: options `--input <path>`, `[--output <path>]`. Computes concrete volume ($m^3$), formwork area ($m^2$), rebar weight ($kg$ matching TCVN 7966.5 kg benchmark), MEP counts, exports report.
   - Layout files in `HPBim/src/HPBim.Cli/`: `Program.cs`, `Commands/ValidateCommand.cs`, `Commands/GenerateCommand.cs`, `Commands/VerifyCommand.cs`, `Commands/EstimateCommand.cs`, `Formatting/ConsoleFormatter.cs`.
   - Ensure every file is <= 200 lines and every method <= 40 lines.
2. **Design 6 AI Agent Roles in `HPBim/agents/`**:
   - `kien-truc-su.md`: Architect persona, schema rules, Vietnamese architectural standards.
   - `ky-su-ket-cau.md`: Structural Engineer persona, TCVN 5574:2018 detailing rules, rebar calculation.
   - `ky-su-co-dien.md`: MEP Engineer persona, plumbing/electrical layout rules.
   - `kiem-soat-ban-ve.md`: QC Inspector persona, pre-flight clash and clearance checklists.
   - `ky-su-du-toan.md`: Quantity Surveyor persona, BOQ calculation methods and cost norms.
   - `dieu-phoi-bim.md`: BIM Coordinator persona, 8-stage pipeline orchestration, dry-run validation, model audit.
3. **Design CLI Tests**:
   - In `HPBim.Client.Tests` or dedicated test class, test command handlers against `NhaPho5x18mFixture` (validate passes, estimate returns exact 7966.5 kg rebar, generate routes through fake pipe client).

## Deliverable
- Write comprehensive handoff report to `handoff.md`.
- Report back to orchestrator via `send_message`.


## 2026-10-06T03:07:05Z
From: 18c3ad8c-1fc9-42cc-83e7-40ab9000587e (parent)
Content:
You are explorer_m4_1_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_1_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_1_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md

Investigate and design Milestone 4:
1. HPBim.Cli design (Program.cs, ValidateCommand, GenerateCommand, VerifyCommand, EstimateCommand, ConsoleFormatter). Ensure all classes <= 200 lines, all methods <= 40 lines.
2. 6 AI Agent Roles in HPBim/agents/ (kien-truc-su.md, ky-su-ket-cau.md, ky-su-co-dien.md, kiem-soat-ban-ve.md, ky-su-du-toan.md, dieu-phoi-bim.md).
3. CLI test suite architecture.
Write full architecture blueprint to handoff.md and report back via send_message.
