# Challenge Assignment: Challenger 2 (M1 - Parser, Sanitization & Concurrency Stress)

## Objective
Adversarially challenge and stress-test `LayoutRangeParser`, `FileNameService`, and `PresetService` in `HPAutoCad.Core/SmartPlot/`:
1. Challenge `LayoutRangeParser`:
   - Malicious inputs: huge string lengths, deeply nested delimiters, integer overflow attempts ("2147483648", "-2147483649"), negative indices, non-ASCII characters, null/empty.
   - Verify that no unhandled exceptions are thrown and memory is protected.
2. Challenge `FileNameService`:
   - Exotic paths: control characters (0x00 to 0x1F), Windows special device names ("CON", "aux.pdf", "NUL.txt"), paths exceeding MAX_PATH, unicode characters, empty token combinations.
3. Challenge `PresetService`:
   - Corrupted JSON (syntax errors, truncated files, wrong types), concurrent read/write.
4. Deliver verdict: `APPROVE` or `REJECT` / `REQUEST_CHANGES`.

## Authoritative Reference
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (entry ## 2026-09-20T22:21:59Z)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md`

## Output
Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1\handoff.md`.

## 2026-09-20T22:34:50Z
You are Challenger 2 for Milestone M1 (Parser, Sanitization & Concurrency Stress).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1
Read your task: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Read worker_m1 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md
Stress test LayoutRangeParser, FileNameService, and PresetService with invalid, malicious, or corrupted inputs. Write your report with verdict APPROVE or REJECT to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_2_m1\handoff.md.
Send a message when complete.

