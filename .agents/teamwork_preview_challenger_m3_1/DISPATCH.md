# Dispatch Assignment: Milestone 3 Challenger 1 — Stdio Protocol & JSON-RPC Surface Stress Test

## Role: Challenger (teamwork_preview_challenger)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_1`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md`

---

## Challenge Scope
Adversarially challenge `HPTekla.Mcp.Server`:
1. Build `HPTekla/HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj` in Release configuration.
2. Launch `HPTekla.Mcp.Server.exe` as a subprocess with standard I/O redirection.
3. Test the full MCP stdio protocol handshake:
   - Send `initialize` request -> verify protocol version 2024-11-05 / 2.2.0, serverInfo (`name: "HPTekla MCP"`).
   - Send `notifications/initialized`.
   - Send `tools/list` -> count exactly 24 tools (4 core, 8 meta, 12 seeds). Assert all expected tool names exist.
   - Send `resources/list` -> verify `tekla://model/info`, `tekla://selection`, and `registry://tools`.
   - Send `prompts/list` -> verify prompts exist.
4. Challenge tool calls when bridge is not running:
   - Call `execute_tekla_code` -> assert clean JSON-RPC error response indicating bridge not connected, with NO crash or unhandled exception.
   - Call `get_tekla_context` -> assert clean response with `isModifiable = false`, error hint, NO crash.
5. Challenge malformed requests and unexpected methods.
6. Verify clean shutdown on stdin close.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if protocol compliance is 100% verified.
- `REQUEST_CHANGES` if any crashes, malformed responses, or protocol deviations occur.

## 2026-09-21T18:32:52Z
You are teamwork_preview_challenger_m3_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_1
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_1\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Worker handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Adversarially challenge HPTekla.Mcp.Server over stdio JSON-RPC per your DISPATCH.md:
Build the server in Release, spawn the executable, send initialize, tools/list (verify exactly 24 tools), resources/list (3 resources), prompts/list (4 prompts), test error handling when bridge is disconnected, test malformed requests, and verify clean shutdown.
Write your findings to `.agents/teamwork_preview_challenger_m3_1/report.md` and handoff to `.agents/teamwork_preview_challenger_m3_1/handoff.md`.
Deliver an explicit APPROVE or REQUEST_CHANGES verdict.
Send a message to the orchestrator (caller) with your verdict and summary when done.
