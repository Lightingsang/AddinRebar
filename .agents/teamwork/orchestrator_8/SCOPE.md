# Scope: HPDynamo MCP Vòng 2 (orchestrator_8)

## Architecture
- Deliverables:
  * `HPDynamo/HPDynamo.McpBridge` (.NET 10.0-windows7.0): Bridge in-process trong Revit 2026 kết nối Named Pipe `\\.\pipe\hprebar-dynamo`
  * `HPDynamo/HPDynamo.Mcp.Server` (.NET 10.0): Stdio MCP Server cung cấp 25 tools (4 core, 8 registry, 13 seeds)
  * `McpShared/`: Thư viện nền tảng host-neutral dùng chung cho 10 hosts
- Test suites:
  * `HPDynamo.Mcp.Server.Tests` (.NET 10.0)
  * `HPDynamo.McpBridge.Tests` (.NET 10.0-windows7.0)
  * `HPDynamo.E2E.Tests` (.NET 10.0)
  * `HPRebar.McpBridge.Core.Net48Tests` (.NET Framework 4.8)
  * `HPRebar.Mcp.Server.Core.Tests` (.NET 10.0)

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | R2-01 Node Input Values Rollback | Snapshot & phục hồi 100% giá trị gốc của node inputs khi dryRun/cancel/exception | Đợt A | ORIGINAL_REQUEST §R1 |
| 2 | R2-02 Wire Replacement Ordering | Thứ tự phục hồi dây nối LIFO/snapshot gốc duy nhất khi thay thế nhiều dây cùng port | Đợt A | ORIGINAL_REQUEST §R1 |
| 3 | R2-03 CodeBlock Wire Preservation | Snapshot ports & connectors trước khi sửa CodeBlock, phục hồi nguyên vẹn khi rollback | Đợt A | ORIGINAL_REQUEST §R1 |
| 4 | R2-04 Coordinator Transaction Truth | Transaction boundary là chân lý, không ghi đè RolledBack = true khi cancel đến muộn | Đợt A | ORIGINAL_REQUEST §R1 |
| 5 | R2-05 Unified Evaluation Sync | EvaluationCompleted đồng bộ xuyên suốt mọi mode, headless inputs & error propagation | Đợt A | ORIGINAL_REQUEST §R1 |
| 6 | R2-06 Scheduler Drain on Cancel | Chờ Dynamo scheduler dừng hẳn khi cancel/timeout trước khi nhả thread (_busy = 0) | Đợt A | ORIGINAL_REQUEST §R1 |
| 7 | R2-07 Transaction AST Syntax Policy | Phân định transaction bằng AST syntax parsing, xóa bỏ substring check | Đợt A | ORIGINAL_REQUEST §R1 |
| 8 | R2-09 Session Isolation & Anti-Recursion | Session-scoped cancel (session, request), chống cancel-of-cancel đệ quy | Đợt A | ORIGINAL_REQUEST §R1 |
| 9 | R2-10 Byte Budget & Async Test Fixes | Giới hạn UTF-8 byte & string length, _truncated ngoài user data, await ThrowsAsync | Đợt A | ORIGINAL_REQUEST §R1 |
| 10 | Đợt B Quarantined Tools Recovery | Trích xuất diagnostics, fix compiler/refs, publish lại 3 tools, đạt 25 tools | Đợt B | ORIGINAL_REQUEST §R2 |
| 11 | R2-08 Honest Benchmark Harness | run-benchmark.ps1 phân tích JSON-RPC/MCP error, persistent warm session, isolate PID 26264 | Đợt C | ORIGINAL_REQUEST §R3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | Đợt A: Transaction, Rollback & Evaluation | R2-01..R2-07, R2-09, R2-10 | none | DONE |
| 2 | Đợt B: Surface Recovery 25 Tools | Gỡ quarantined 3 tools, compile seeds, publish | Đợt A | DONE |
| 3 | Đợt C: Benchmark & Isolation | R2-08, persistent session, raw metrics | Đợt B | PLANNED |

## Interface Contracts & Code Layout Boundaries
- `HPDynamo/HPDynamo.McpBridge/`: Canvas rollback, Wire tracker, Graph runners, Evaluation sync, Coordinator
- `McpShared/`: Host-neutral cancel correlation, ScriptGuard AST analyzers, RequestDispatcher
- `HPDynamo/HPDynamo.Mcp.Server/`: Seed tools, tool definitions, response formatters, registry tests
- `HPDynamo/tools/harness/`: PowerShell benchmark runner scripts
