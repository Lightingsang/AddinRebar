# Dispatch Instructions: worker_m4_gen2

## Identity
- Role: CLI & Agent Packaging Implementation Worker
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen2
- Target Milestone: Milestone 4 (`HPBim.Cli` & AI Agent Packaging)

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor and adversarial reviewers will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Authoritative Blueprint
Read the full implementation blueprint and drop-in code prepared by explorer_m4_1_gen2 at:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_1_gen2\handoff.md`

## Required Actions

### 1. Create `HPBim/src/HPBim.Cli/` Project and Components
- `HPBim/src/HPBim.Cli/HPBim.Cli.csproj`:
  * `<OutputType>Exe</OutputType>`, `<TargetFramework>net10.0</TargetFramework>`
  * Project references: `HPBim.Core`, `HPBim.Client`, `McpShared/HPRebar.Mcp.Contracts`
  * 0 references to `Autodesk.Revit.*`
- `CommandLine/CliParseResult.cs` (~30 lines)
- `CommandLine/CliParser.cs` (~75 lines)
- `Formatting/ConsoleFormatter.cs` (~150 lines)
- `Estimation/BoqReport.cs` (~45 lines)
- `Estimation/BoqEstimator.cs` (~150 lines)
- `Commands/ValidateCommand.cs` (~115 lines)
- `Commands/GenerateCommand.cs` (~150 lines)
- `Commands/VerifyCommand.cs` (~125 lines)
- `Commands/EstimateCommand.cs` (~130 lines)
- `Program.cs` (~65 lines)
- Add `HPBim.Cli` to `HPBim/HPBim.slnx` under `/src/`.

### 2. Create the 6 AI Agent Roles in `HPBim/agents/`
Create detailed, professional Markdown prompts adhering to the blueprint in `explorer_m4_1_gen2/handoff.md`:
1. `HPBim/agents/kien-truc-su.md`: Kiến Trúc Sư AI (TCVN 4451:2012, QCVN 06:2022/BXD, Shoelace rooms).
2. `HPBim/agents/ky-su-ket-cau.md`: Kỹ Sư Kết Cấu AI (TCVN 5574:2018, rebar detailing, 7966.5 kg benchmark).
3. `HPBim/agents/ky-su-co-dien.md`: Kỹ Sư Cơ Điện AI (TCVN 4513:1988, TCVN 4474:1987, slope rules).
4. `HPBim/agents/kiem-soat-ban-ve.md`: Kỹ Sư Kiểm Soát Bản Vẽ & Chất Lượng BIM AI (4-layer pre-flight QC).
5. `HPBim/agents/ky-su-du-toan.md`: Kỹ Sư Dự Toán & Kinh Tế Xây Dựng AI (TT 12/2021/TT-BXD, TT 13/2021/TT-BXD, 4-part BOQ).
6. `HPBim/agents/dieu-phoi-bim.md`: Điều Phối Viên BIM Trưởng AI (5-Step BIM Lifecycle).

### 3. Add Comprehensive CLI Unit Tests in `HPBim/tests/HPBim.Client.Tests/Cli/`
- `CliParserTests.cs` (parsing commands, flags, options, unmatched args)
- `ConsoleFormatterTests.cs` (table alignment, banners, badges)
- `ValidateCommandTests.cs` (valid model returns 0, invalid model returns 1, missing file returns 2)
- `EstimateCommandTests.cs` (matches 7966.5 kg rebar, partitions small/large, writes report file)
- `GenerateCommandTests.cs` (dry-run routes through pipe client, invalid model aborts)
- `VerifyCommandTests.cs` (matching model returns 0, mismatch returns 1)

### 4. Build and Test Verification
- Run: `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release /warnaserror` (0 warnings, 0 errors).
- Run: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release` (100% green tests).
- Clean Code Invariant: Every `.cs` file in `HPBim.Cli` must be <= 200 lines, all methods <= 40 lines.

### 5. Deliverable
- Write `handoff.md` in your working directory.
- Send completion message to orchestrator via `send_message`.

## 2026-10-06T03:16:40Z
You are worker_m4_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read Explorer blueprint at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_1_gen2\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor and adversarial reviewers will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Task:
1. Implement HPBim.Cli (net10.0 executable) in HPBim/src/HPBim.Cli/ with Program.cs, CliParser, ConsoleFormatter, BoqEstimator, and the 4 command handlers (Validate, Generate, Verify, Estimate).
2. Add HPBim.Cli to HPBim/HPBim.slnx.
3. Package the 6 AI Agent Roles in HPBim/agents/ (kien-truc-su.md, ky-su-ket-cau.md, ky-su-co-dien.md, kiem-soat-ban-ve.md, ky-su-du-toan.md, dieu-phoi-bim.md).
4. Implement CLI unit tests in HPBim/tests/HPBim.Client.Tests/Cli/.
5. Verify: dotnet build with /warnaserror (0 warnings, 0 errors) and dotnet test in Release mode.
6. Verify Clean Code rules: all classes <= 200 lines, all methods <= 40 lines.
Write handoff.md and report back via send_message.
