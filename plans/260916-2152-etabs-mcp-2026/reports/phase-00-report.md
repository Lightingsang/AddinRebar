# Phase 0 report — McpShared additive ETABS contracts + engine tests + byte-identical gate

**Ngày:** 2026-09-16 · **Status:** Implemented + Built + Tested (không có gì "Verified" với ETABS thật — phase 0 không chạm ETABS) · Review [9/10 approve](code-review-phase-00.md) · Tests [3 lần, 0 flaky](test-report-phase-00.md) · Baseline [phase-00-baseline.md](phase-00-baseline.md) · Runner `run-phase-00-gate.ps1` + `snapshot-tools-list.ps1`.

## Đã làm (17 file code + 4 file test, +157/−14 dòng, tất cả trong `McpShared/`)

| # | File | Nội dung |
|---|---|---|
| 1 | `Contracts/PipeNaming.cs` | `EtabsHost = "etabs"` + arm `hpetabs-mcp-{version}` (= nhánh mặc định) |
| 2 | `Contracts/JsonRpc/JsonRpcMethods.cs` | `EtabsPrefix = "etabs."` |
| 3 | `Contracts/HostScriptContracts.cs` | `EtabsImports` (`ETABSv1`, không `CSiAPIv1`/Interop), `EtabsGlobals` (`sapModel, etabs, units, ct, log, progress, args`), `EtabsHeavyMaxTimeoutSeconds = 600` — append cuối, cạnh diff AEC uncommitted |
| 4 | `Contracts/Messages/ContextMessages.cs` | `ContextResult.Etabs` + `EtabsInfo` 10 field (3 chuỗi `string?` — null khi chưa attach) |
| 5 | `Contracts/Messages/ExecuteResult.cs` | `Snapshot : string?` — tên file |
| 6 | `Contracts/Messages/AnalyzeMessages.cs` | `AnalyzeRequest(string Code, string? Transaction = null)` |
| 7 | `Server.Core/Registry/ToolLifecycleService.cs` | `AnalyzeAsync(code, ct, string? transaction = null)`; `ProposeAsync` truyền `record.Transaction` |
| 8 | `Core/Scripting/GuardProfile.cs` | `GuardProfile.Etabs`: identifiers `Helper`, `MessageBox`; 7 member `cOAPI`; namespaces `System.Windows.Forms`, `HPEtabs.McpBridge`, `HPRebar.McpBridge.Core.Host` |
| 9 | `Core/Scripting/AnalyzerProfile.cs` | `AnalyzerProfile.Etabs` rỗng (không transaction) |
| 10 | `Server.Core/Hosts/IHostProfile.cs` + `HostProfile.cs` | `BridgeNotConnectedHint`, `TimeoutSemanticsHint` (`init`, null; `WithHostAssembly` copy) |
| 11 | `Server.Core/Services/RevitBridgeClient.cs` | 2 message = `hint ?? <câu cũ>` |
| 12 | `Core/Pipe/RequestDispatcher.cs` + `Host/McpBridgeHost.cs` | ctor param cuối `executionDisabledMessage = null` |
| 13 | `Server.Core/Bootstrap/McpServerHost.cs` | `.Configure` seed `HostVersion = profile.DefaultVersion` trước `.Bind` |
| 14 | test | `run_tool` + bridge trả `-32001` → `BridgeErrorException` ném ra, `runs` không tăng, tool vẫn published (6 lần); contrast: `ExecuteResult{IsError}` → 1 run |
| 15 | `Core/Scripting/ScriptGuard.cs` (**sau review**) | strip `global::` trong `IsDeniedNamespace` — lỗ có sẵn ở cả 3 host (`global::System.IO.File.WriteAllText`, `global::System.Diagnostics.Process.Start`, `global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current` từng lọt). 1 dòng, **revert được** nếu user không muốn |
| 16 | `Server.Core/Services/ResultFormatter.cs` (**sau review**) | `Snapshot = Path.GetFileName(Snapshot)` — bridge lỡ gửi full path cũng không lọt username |

Tests mới (34): `EtabsProfileTests.cs` (198 dòng: hằng, guard 13 + `global::` 3×4 profile + 1, analyzer, hints/`WithHostAssembly`, validator 600, `ConfigureOptions` 7 case kể cả alias `Bridge:RevitVersion`, wire invisible-when-unused, `Snapshot` strip), `EtabsBridgeMessagesTests.cs` (243 dòng: clamp 3, `-32001` text cũ/mới, not-connected cũ/mới, timeout cũ/mới, context shape `etabs`, analyze forward transaction, refusal không thành run), `EtabsTestProfile.cs` (43, helper chung); `Fakes/FakeRevitExecutor.cs` +`LastAnalyzeRequest`.

## Gate (đo 2 lần: sau implement, sau review)

| Kiểm tra | Baseline (trước) | Sau | Kết quả |
|---|---|---|---|
| `McpShared/HPRebar.Mcp.Server.Core.Tests` | 128 | **162** (3 lần liên tiếp) | ✅ +34, 0 fail, 0 skip |
| `McpShared/HPRebar.McpBridge.Core.Net48Tests` | 60 | 60 | ✅ |
| `HPRebar/HPRebar.Mcp.Server.Tests` | 109 | 109 | ✅ |
| `HPAutoCad/HPAutoCad.Mcp.Server.Tests` | 136 | 153 | ✅ 0 fail — +17 do session AEC song song sửa `SeedLibraryTests.cs` sau baseline, không do phase này |
| `HPNavis/HPNavis.Mcp.Server.Tests` | 49 | 49 | ✅ |
| `tools/list` Revit (Release exe, registry cách ly) | 33 | 33 | ✅ byte-identical |
| `tools/list` Navis | 24 | 24 | ✅ byte-identical |
| `tools/list` AutoCAD | 37 | 40 | ✅ 37 tool chung byte-identical (so theo tên); +3 = seed AEC untracked mới (`cad_standards_check`, `audit_aec_drawing`, `create_issue_markup`) |
| `HPRebar.Mcp.Server.Core.dll` sha256 | `A546DECE…` | `40C1FEE3…` | ✅ engine rebuild thật |
| `dotnet build` HPRebar.slnx Debug.R26 / HPAutoCad.slnx Debug / HPNavis.slnx Debug (không deploy) | — | 0 error ×3 | ✅ |
| `grep "ETABSv1\|Autodesk\." McpShared --include=*.csproj` | — | 0 | ✅ |

Byte-identity 3 message engine khi hint = null được pin bằng test (`Execution_disabled_text_…`, `Bridge_not_connected_…`, `Timeout_message_…`); `tools/list` không đổi vì mọi field mới là `WhenWritingNull`.

## Review (9/10) → đã xử lý
- H1 `global::` bypass → #15 + 4 test. M1 `EtabsInfo` `string?` → áp dụng. M2 `Snapshot` strip → #16 + test. L1 (normalize `transaction` trước `analyze`) → ghi cho phase 2 (bridge coi giá trị lạ là null). L2 test file > 300 dòng → tách 3 file, fake timeout 1 s thay 4 s. L3 số file trong SC → sửa.

## Docs
CLAUDE.md (hàng `McpShared/` + count 162) → AGENTS.md regen; `docs/project-changelog.md`, `docs/codebase-summary.md`, `docs/system-architecture.md` (docs-manager); journal `docs/journals/2026-09-16-etabs-mcp-plan-red-teamed.md`.

## Còn lại / lưu ý cho phase 1
- Chưa commit (user quyết). Diff `HostScriptContracts.cs` chứa cả dòng `"HPAutoCad.Aec"` của plan AEC (uncommitted trước đó) — commit tách nếu cần.
- Phase 1 cần 👤 mở ETABS 22 + model bỏ đi + duyệt 2 probe ghi (`Save(tmp)`, `SetModelIsLocked`).
- `AnalyzeRequest.Transaction`: 3 bridge cũ nhận thêm key `transaction` trên wire `*.analyze` (System.Text.Json bỏ qua) — bridge Revit/AutoCAD/Navis đã deploy không cần rebuild; test deserialize `{"code":…}` cũ → `Transaction = null` pin.
