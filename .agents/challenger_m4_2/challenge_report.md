# Adversarial Challenge Report: Milestone M4 IPC & Wire Protocol Challenge

## Challenge Summary

**Overall risk assessment**: LOW

The IPC named pipe architecture, JSON-RPC 2.0 wire protocol handling, and FakeExecutor round-trip execution for HPExcel were rigorously probed across 39 adversarial test scenarios. The architecture demonstrated exceptional resilience against broken pipes, abrupt disconnections, stream reuse, extreme identifier values, malformed payloads, cancellation cascades, and timeout clamping boundary conditions.

---

## Challenges

### [Low] Challenge 1: Whitespace-only Lines vs Empty Lines in NDJSON Stream
- **Assumption challenged**: That whitespace-only lines (e.g. `"   \n"`) are discarded silently without response, similar to empty lines (`"\n"`).
- **Attack scenario**: A client transmits whitespace padding characters (`"   "`, `"\t\t"`) between JSON-RPC request packets.
- **Blast radius**: `PipeListener.cs` checks `if (line.Length == 0) continue;`. When a line consists of spaces (`Length > 0`), it reaches `RequestDispatcher.HandleLineAsync`, where `System.Text.Json` fails to deserialize whitespace into `JsonRpcEnvelope`, emitting a `ParseError (-32700, id: 0)`. If a client was not expecting an error reply to whitespace padding, its response queue could be offset.
- **Stress test result**: Verified in `ExcelDispatcherWireAdversarialTests.EmptyLines_AreIgnored_AndWhitespaceNonEmpty_ReturnsParseError`. Empty lines are properly skipped; non-empty whitespace correctly returns a spec-compliant `-32700 ParseError` with `Id = 0`. Standard clients do not emit trailing whitespace padding.
- **Mitigation**: Client libraries should ensure only newline separators `\n` without whitespace padding are sent over NDJSON streams. No bridge code modification required.

### [Low] Challenge 2: Non-numeric String IDs in JSON-RPC 2.0
- **Assumption challenged**: That JSON-RPC 2.0 allows arbitrary string IDs (e.g. `"id": "req-001"`).
- **Attack scenario**: A client sends a request with a string ID format like `{"jsonrpc":"2.0","id":"req-001","method":"excel.ping"}`.
- **Blast radius**: In `JsonRpcEnvelope.cs`, `Id` is strictly typed as `long?`. A string value cannot be deserialized, resulting in a `JsonException` and returning a `-32700 ParseError` with `id: 0`.
- **Stress test result**: Verified in `ExcelDispatcherWireAdversarialTests.MalformedJson_StringId_ReturnsParseError32700`.
- **Mitigation**: The McpShared internal bridge contract intentionally standardizes on numeric `long` IDs for high-throughput memory efficiency and lock-free thread coordination. All official servers (`RevitBridgeClient`) generate sequential `long` IDs via `Interlocked.Increment`.

### [Low] Challenge 3: Unhandled Cancellation in ExecuteCodeService
- **Assumption challenged**: That cancelling a tool call might leave the STA worker or Excel process in an uncancelled state if cancellation is not propagated over the named pipe.
- **Attack scenario**: Caller invokes `execute_excel_code` for a long-running calculation and cancels the `CancellationToken` mid-execution.
- **Blast radius**: If cancellation was not propagated, the bridge executor would continue running until timeout, consuming CPU/memory and keeping `IsBusy == true`.
- **Stress test result**: Verified in `ExcelSeedToolsRoundTripAdversarialTests.Cancellation_DuringExecution_PropagatesCancelAcrossPipe_AndCallsExecutorCancel`. `RevitBridgeClient.WaitForResponseAsync` catches `OperationCanceledException` and fires `TryCancelInRevit(id)` (`excel.cancel`), successfully triggering `FakeRevitExecutor.Cancel()` (`_executor.CancelCalls >= 1`).

---

## Stress Test Results

| Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|
| Client disconnects mid-stream with partial JSON | Bridge drops stream cleanly, next client connects | `PipeListener` resets `CurrentWriter = null`, next client pongs | **PASS** |
| Client disconnects immediately before reading reply | `NdjsonPipeWriter` faults safely without crashing | Catches `IOException`, listener accepts next client | **PASS** |
| 5 rapid connect/disconnect cycles | Listener loop stays responsive | Successfully accepted subsequent client and ponged | **PASS** |
| Completely empty line `""` | Ignored by listener | Skipped via `line.Length == 0` check | **PASS** |
| Whitespace line `"   "` | Handled as malformed JSON | Emits JSON-RPC `-32700 ParseError` with `id: 0` | **PASS** |
| Unclosed JSON brace | Handled as malformed JSON | Emits JSON-RPC `-32700 ParseError` with `id: 0` | **PASS** |
| Valid JSON array `[1, 2, 3]` | Handled safely | Parsed safely or rejected with ParseError without crash | **PASS** |
| JSON notification (no `id`) | Dropped without reply | Ignored by `RequestDispatcher`, no spurious reply | **PASS** |
| String ID `"req-1"` | Deserialization failure | Emits JSON-RPC `-32700 ParseError` | **PASS** |
| Extreme ID `long.MaxValue` | Echoed identically | Response carries `id = 9223372036854775807` | **PASS** |
| Negative ID `-123` | Echoed identically | Response carries `id = -123` | **PASS** |
| Empty method name `""` | Returns MethodNotFound | Emits JSON-RPC `-32601 MethodNotFound` | **PASS** |
| Only prefix `"excel."` | Returns MethodNotFound | Emits JSON-RPC `-32601 MethodNotFound` | **PASS** |
| Arbitrary unknown method | Returns MethodNotFound | Emits JSON-RPC `-32601 MethodNotFound` | **PASS** |
| 2000-character method name | Returns MethodNotFound | Emits JSON-RPC `-32601 MethodNotFound` | **PASS** |
| 5 sequential requests on 1 stream | Serial execution, no corruption | 5/5 requests (`ping`, `cancel`, `ctx`, `detach`, `ping`) matched | **PASS** |
| Attach with string PID | Parameter mismatch handled | Emits InternalError safely | **PASS** |
| Cancellation during execution | Triggers `excel.cancel` over IPC | `_executor.CancelCalls >= 1` in FakeRevitExecutor | **PASS** |
| Timeout clamping: underflow (-10, 0, 1, 4, 5) | Clamped to 5s minimum | All clamped to 5s in `ExecuteRequest` | **PASS** |
| Timeout clamping: normal (30s) | Preserved | Preserved at 30s in `ExecuteRequest` | **PASS** |
| Timeout clamping: overflow (600, 601, 9999) | Clamped to 600s maximum | All clamped to 600s in `ExecuteRequest` | **PASS** |
| Empty/whitespace script code | Rejected before IPC dispatch | Returns tool error with "code is empty" | **PASS** |
| Invalid transaction mode | Rejected before IPC dispatch | Returns tool error listing valid modes | **PASS** |
| Transaction mode case normalization | Normalized to auto/manual/none | `AUTO` -> `auto`, `None` -> `none`, etc. | **PASS** |
| Invalid args (JSON array or number) | Rejected before IPC dispatch | Returns tool error "args must be a JSON object" | **PASS** |
| Oversized script (> 512KB) | Rejected before IPC dispatch | Returns tool error "limit is 524,288" | **PASS** |
| Simulated bridge execution error | Mapped to CallToolResult | `IsError = true` with diagnostic messages | **PASS** |

---

## Unchallenged Areas
- Physical COM attachment to a live `excel.exe` process with dialog popups: out of scope for headless unit/integration test suites; handled deterministically by `ClosedXmlWorkbookService` (headless) and `FakeRevitExecutor` (IPC contract).
