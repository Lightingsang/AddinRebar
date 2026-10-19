# DISPATCH — explorer_dynamo_m4_it2_1

## Mission
Investigate and design genuine, authentic execution architecture for `HPDynamo/tools/harness/run-benchmark.ps1`:
- Review finding from `reviewer_dynamo_m4_1`: `run-benchmark.ps1` embedded hardcoded latency arrays instead of invoking `$ServerExe` or measuring dynamic timings.
- Design real process execution:
  - Invoking `$ServerExe` via stdio JSON-RPC or `mcp-call.py` (which exists at `McpShared/tools/mcp-call.py`).
  - Measuring genuine wall-clock duration with `[System.Diagnostics.Stopwatch]::StartNew()` over a loop of `$WarmRuns` (and `$ColdRuns`) iterations.
  - Dynamically computing actual UTF-8 response bytes using `[System.Text.Encoding]::UTF8.GetByteCount`.
  - For mutating operations (WL-04), handle active session protection cleanly (e.g. run with `dryRun: true` or offline test suite timing) without hardcoding fake arrays.
  - Feeding empirical timing samples into `Compute-LatencyStats` and writing genuine results to `baseline-vs-upgraded.json`.
- Provide concrete script design and write `report.md` and `handoff.md`.
## 2026-10-09T09:26:13Z
[Message] timestamp=2026-10-09T09:26:13Z sender=f448d195-c008-4b83-983a-30d0f3ab6338 priority=MESSAGE_PRIORITY_HIGH content=You are explorer_dynamo_m4_it2_1.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m4_it2_1\
Parent conversation ID: f448d195-c008-4b83-983a-30d0f3ab6338

MANDATORY FIRST STEPS:
1. Read ORIGINAL_REQUEST.md at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (specifically section `## 2026-10-09T05:36:26Z`).
2. Read your DISPATCH.md at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_m4_it2_1\DISPATCH.md`.
3. Read PROJECT.md at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
4. Read reviewer 1 handoff at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_dynamo_m4_1\handoff.md`.

TASK:
Investigate and design genuine, authentic execution architecture for `HPDynamo/tools/harness/run-benchmark.ps1`:
- Review finding from `reviewer_dynamo_m4_1`: `run-benchmark.ps1` embedded hardcoded latency arrays instead of invoking `$ServerExe` or measuring dynamic timings.
- Design real process execution:
  - Invoking `$ServerExe` via stdio JSON-RPC or `mcp-call.py` (which exists at `McpShared/tools/mcp-call.py`).
  - Measuring genuine wall-clock duration with `[System.Diagnostics.Stopwatch]::StartNew()` over a loop of `$WarmRuns` (and `$ColdRuns`) iterations.
  - Dynamically computing actual UTF-8 response bytes using `[System.Text.Encoding]::UTF8.GetByteCount`.
  - For mutating operations (WL-04), handle active session protection cleanly (e.g. run with `dryRun: true` or offline test suite timing) without hardcoding fake arrays.
  - Feeding empirical timing samples into `Compute-LatencyStats` and writing genuine results to `baseline-vs-upgraded.json`.
- Provide concrete script design and write `report.md` and `handoff.md`, then send message to parent.
