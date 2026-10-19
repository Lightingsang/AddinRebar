# BRIEFING — 2026-10-05T23:25:00Z

## Mission
Xây dựng Deliverable độc lập cấp cao HPBim/ viết 100% bằng C# .NET với solution HPBim.slnx, tuân thủ nghiêm ngặt quy định HP Clean Code Governance (chỉ tham chiếu McpShared/, không tham chiếu chéo), chuyển đổi logic TCVN từ vn-autocad-skill để tự động sinh mô hình BIM 3D bản địa trong Revit 2026 qua Revit MCP Named Pipe.

## 🔒 My Identity
- Archetype: orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10
- Original parent: Sentinel
- Original parent conversation ID: cac5848e-6ab0-47c5-bede-48190f3afbc1

## 🔒 My Workflow
- **Pattern**: Project Pattern (Orchestrator)
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md
1. **Survey**: Spawn 3 Explorers in parallel to survey vn-autocad-skill, McpShared/Revit MCP, and Clean Code requirements. Merge reports into Feature Inventory.
2. **Decompose & Delegate**:
   - M1: Cấu Trúc Solution & Domain C# Thuần (HPBim.slnx, Directory.Build.props, global.json, HPBim.Core netstandard2.0)
   - M2: Bộ Test Suite & Benchmark NhaPho 5x18m (HPBim.Core.Tests, NhaPho5x18mFixture.cs, xUnit v3 / MTP)
   - M3: Bộ Điều Phối & Client Giao Tiếp Revit (HPBim.Client, Named Pipe \\.\pipe\hprebar-mcp-r2026, Roslyn type generation, dryRun)
   - M4: Ứng Dụng Dòng Lệnh CLI & Bộ Tác Tử AI (HPBim.Cli, console tool, agent roles/prompts)
   - M5: Tích Hợp Toàn Diện, Live / DryRun Verification & Clean Code Audit
3. **Dispatch & Execute**:
   - Milestone iteration loop: Explorer(s) -> Worker -> Reviewer(s) -> Challenger(s) -> Auditor -> Gate
4. **On failure**:
   - Retry -> Replace -> Skip (never audit) -> Redistribute -> Redesign
5. **Succession**:
   - At 16 spawns, write handoff.md and spawn successor
- **Work items**:
  1. Survey & Architecture [done]
  2. M1: HPBim.Core & Solution [in-progress]
  3. M2: HPBim.Core.Tests & NhaPho5x18m [pending]
  4. M3: HPBim.Client & Named Pipe [pending]
  5. M4: HPBim.Cli & Agent Roles [pending]
  6. M5: Final Verification & Audit [pending]
- **Current phase**: 1 (M1 Execution)
- **Current focus**: Milestone 1: Solution setup (HPBim.slnx, Directory.Build.props, global.json) and pure domain library HPBim.Core (netstandard2.0) with DTOs, geometry math, TCVN rules, and QC engine.

## 🔒 Key Constraints
- Strict compliance with docs/clean-code/HP_CLEAN_CODE_CORE.md (methods <= 50 lines, classes <= 300 lines, no empty catch, no dead code, Q-B1..Q-B3).
- Zero cross-referencing between MCP folders. HPBim only references McpShared/.
- HPBim.Core MUST target netstandard2.0 and have ZERO references to Autodesk.Revit.*.
- 100% C# (0 Python runtime).
- Dispatch-only orchestrator: Never write code or run tests directly; delegate to subagents.

## Current Parent
- Conversation ID: cac5848e-6ab0-47c5-bede-48190f3afbc1
- Updated: 2026-10-05T23:25:00Z

## Key Decisions Made
- Established 5-milestone decomposition for HPBim deliverable.
- Starting Step 0 (Survey Phase) with 3 parallel Explorers:
  - Explorer 1: Survey vncad (Python -> C# mapping, geometry structures, NhaPho 5x18m dataset, TCVN rebar rules, QC checks).
  - Explorer 2: Survey McpShared & Revit MCP infrastructure (Named Pipe protocol, JSON-RPC schema, Roslyn script execution, element creation patterns).
  - Explorer 3: Survey Clean Code standards, solution structure, build props, test runner config (global.json, MTP runner, xUnit v3).

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| explorer_survey_vncad | teamwork_preview_explorer | Survey vn-autocad-skill & vncad domain | completed | 09a76bca-d661-4894-9158-fdf9a58cd489 |
| explorer_survey_mcp | teamwork_preview_explorer | Survey McpShared & Revit MCP pipe | completed | 9ffe5d4e-ed4d-47b1-bc92-b59e95c223eb |
| explorer_survey_cleancode | teamwork_preview_explorer | Survey Clean Code & Solution setup | completed | 6256f536-1526-4459-8e77-771b5325c304 |
| explorer_m1_1_hpbim | teamwork_preview_explorer | M1 Solution & Geometry Explorer | completed | ae953f32-7b30-49de-a8d1-b1a399f9d5d6 |
| explorer_m1_2_hpbim | teamwork_preview_explorer | M1 BIM Schema & Spatial Algorithms | completed | 1a500b70-9f48-4728-8380-c55a237141d0 |
| explorer_m1_3_hpbim | teamwork_preview_explorer | M1 TCVN Rules & QC Engine | completed | b2bf64ac-b50d-44a2-8742-0fd17c3bf2c8 |
| worker_m1_hpbim | teamwork_preview_worker | M1 Core Implementation Worker | completed | 647361bc-c32d-4182-9f9a-8d4886bca86b |
| reviewer_m1_1_hpbim | teamwork_preview_reviewer | M1 Solution & Architecture Reviewer | in-progress | cc72ff19-ac6b-45d3-9621-7bc937e0d840 |
| reviewer_m1_2_hpbim | teamwork_preview_reviewer | M1 Domain & Algorithm Reviewer | in-progress | 4420d18b-8d2b-4a01-b726-790d3559591d |
| challenger_m1_1_hpbim | teamwork_preview_challenger | M1 Geometry & Math Challenger | in-progress | 1a012a9c-cf6c-4004-9c7c-9c1d5daa03c4 |
| challenger_m1_2_hpbim | teamwork_preview_challenger | M1 TCVN & QC Engine Challenger | in-progress | a6b47ab7-f5f0-40e7-b3c3-34ba0782f65e |
| auditor_m1_hpbim | teamwork_preview_auditor | M1 Forensic Integrity Auditor | completed | 31608aec-d0e2-4da3-a3f7-ca74d331f4a3 |
| worker_m1_fix_hpbim | teamwork_preview_worker | M1 Remediation Worker | completed | eb4178bf-03e1-46dd-81f3-c3adbd4aa810 |
| reviewer_m1_it2_1_hpbim | teamwork_preview_reviewer | M1 Iteration 2 Reviewer | completed | ade128a6-4a3b-4775-ad4f-54e245931675 |
| challenger_m1_it2_1_hpbim | teamwork_preview_challenger | M1 Iteration 2 Challenger | completed | ea70b14a-85e1-4fce-a7db-af97859b331f |
| auditor_m1_it2_1_hpbim | teamwork_preview_auditor | M1 Iteration 2 Forensic Auditor | completed | 1b101b4b-f6fb-420b-b8a6-1b0ce6de3ed1 |
| worker_m1_it3_hpbim | teamwork_preview_worker | M1 Audit Defect Worker | in-progress | fdbdae39-3b43-4813-9e62-b09fad6ab397 |

## Succession Status
- Succession required: no (single persistent orchestrator mode)
- Spawn count: 16
- Pending subagents: none
- Predecessor: none
- Successor: none (continued by Orchestrator 10)

## Active Timers
- Heartbeat cron: f21aade0-effc-4aa5-a071-ae17d49dabe3/task-256
- Safety timer: none

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md — Authoritative User Request
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\DISPATCH.md — Dispatch log
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\context.md — Context and requirements
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\progress.md — Liveness & status tracking
