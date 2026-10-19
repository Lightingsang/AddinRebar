# BRIEFING — 2026-10-09T18:26:00Z

## Mission
Khảo sát cơ chế Persistent Session cho warm runs và đo lường metrics (startup vs steady-state, raw latency, p50/p95, UTF-8 byte payload, reports/baseline-vs-upgraded.json schema) cho HPDynamo MCP Vòng 2 Milestone C.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2
- Original parent: 0a4397e4-543d-4476-a901-af2f39506668
- Milestone: Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement code in production folders
- Zero touch active Revit session (PID 26264)
- Preserve McpShared host-neutrality
- Clean code governance rules

## Current Parent
- Conversation ID: 0a4397e4-543d-4476-a901-af2f39506668
- Updated: 2026-10-09T18:26:00Z

## Investigation State
- **Explored paths**:
  - `McpShared/tools/` (`mcp-call.py`, `mcp-session.py`, `harness_common.py`)
  - `HPDynamo/tools/harness/run-benchmark.ps1`
  - `plans/261009-1118-hpdynamo-mcp-performance/reports/` (`baseline-vs-upgraded.json`, `codex-review-20261009-round2.md`)
  - `plans/261009-1118-hpdynamo-mcp-performance/` (`benchmark-plan.md`, `audit.md`, `plan.md`)
  - `HPDynamo/HPDynamo.Mcp.Server/Hosts/DynamoHostProfile.cs` & `Program.cs`
  - Thực nghiệm chạy đo trực tiếp server với script mẫu `test_session_measure.py`.
- **Key findings**:
  - `run-benchmark.ps1` hiện tại gọi `mcp-call.py` cho MỌI warm run -> mỗi warm run tốn ~710ms-900ms process spawn overhead, làm sai lệch kết quả đo.
  - `McpShared/tools/mcp-session.py` đã có sẵn class `Server` giữ kết nối persistent stdio; đo thực nghiệm chứng minh warm run steady-state giảm từ 750ms xuống ~0.98ms (giảm 750 lần)!
  - Khi bridge offline, server trả về `isError: true` và "Dynamo bridge not connected" sau 3000ms connect timeout; harness cũ nuốt lỗi này và in "Verified Live". Cần khắc phục triệt để.
  - Schema `baseline-vs-upgraded.json` cần bổ sung `rawSamples`, `startupMs`, `coldLatencyMs`, `warmLatencyMs` (p50, p95, min, max, mean), số lượng runs thành công/thất bại và loại bỏ so sánh giả định.
- **Unexplored areas**: None for this investigation phase.

## Key Decisions Made
- Đề xuất kiến trúc 2 tầng: PowerShell `run-benchmark.ps1` làm Orchestrator (PID isolation, môi trường) và Python `benchmark_session.py` (tận dụng `McpShared/tools/mcp-session.py`) làm Persistent Engine đo lường chính xác microsecond.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2\handoff.md` — Báo cáo khảo sát hoàn chỉnh (5 thành phần)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2\test_session_measure.py` — Script thực nghiệm chứng minh
