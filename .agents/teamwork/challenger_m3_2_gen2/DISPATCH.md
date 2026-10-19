# Dispatch Instructions: challenger_m3_2_gen2

## Identity
- Role: Adversarial Challenger & Concurrency Stress Tester
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2_gen2
- Target Milestone: Milestone 3 (Revit Client & Pipe Engine)

## Scope & Tasks
1. Adversarially probe `HPBim.Client`:
   - Connection error handling: Does `RevitPipeClient` throw `RevitPipeException` with clear error details when connection fails or times out?
   - Pipeline engine behavior: Does `BimPipelineEngine` properly halt downstream stages when an intermediate stage fails?
   - Stage selection: Can specific stages be isolated and run independently (e.g. Stages 1-2 only)?
   - Atomic dry-run: Verify that `atomicDryRun` flag invokes `CompositeBimGenerator` and executes in a single step.
2. Stress & Performance:
   - Run tests under Release configuration: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
   - Run tests under Debug configuration: `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Debug`
   - Check performance stability across runs.
3. Deliverable:
   - Write `handoff.md` with explicit verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Send completion message to orchestrator via `send_message`.


## 2026-10-06T02:16:54Z
You are challenger_m3_2_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2_gen2
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2_gen2\DISPATCH.md
Read ORIGINAL_REQUEST.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Adversarially probe HPBim.Client:
Test connection failures, pipeline engine halting on error, stage filtering, and atomic dry-run execution.
Run dotnet test under Release and Debug configurations.
Write handoff.md with test counts and verdict APPROVE or REQUEST_CHANGES and report back via send_message.
