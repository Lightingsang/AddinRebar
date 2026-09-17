# Phase 0 report — `McpShared` additive Civil 3D contracts + engine tests + gate 4 host (2026-09-17)

**Status:** built + tested + gate pass (byte-identical). Chưa code review lúc viết report này (review + test report ghi file riêng cùng thư mục).

## Đã sửa (đúng bảng phase-00, không thêm hàng)
`git diff --numstat -- 'McpShared/*.cs'` — **chỉ thêm, 0 xoá**:

| # | File | +dòng | Nội dung |
|---|---|---|---|
| 1 | `HPRebar.Mcp.Contracts/PipeNaming.cs` | +8 | `Civil3dHost = "civil3d"` + case `=> "hpcivil3d-mcp-" + version` (= nhánh mặc định, test pin) |
| 2 | `HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs` | +1 | `Civil3dPrefix = "civil3d."` |
| 3 | `HPRebar.Mcp.Contracts/HostScriptContracts.cs` | +26 | `Civil3dImports` (AutoCAD 8 − `HPAutoCad.Aec` + 5 `Autodesk.Civil*` + Scripting = 14), `Civil3dGlobals` (AutoCAD + `civil`) |
| 4 | `HPRebar.Mcp.Contracts/Messages/ContextMessages.cs` | +23 | slot `Civil3dInfo? Civil3d` + record 11 field |
| 5 | `HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` | +30 | `Civil3d` = `Autocad.Denied*` **nối** thêm (không lặp literal — `GuardProfile` đã expose các list public; sai lệch nhỏ so ghi chú phase-00 "literal đầy đủ", tốt hơn: superset cấu trúc, test pin) |
| 6 | `HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs` | +5 | `Civil3d` = quy tắc AutoCAD (`StartTransaction`, `StartOpenCloseTransaction`) |
| 7 | test | | `Shape` giữ `autocad` + `civil3d`, bỏ `revitVersion/isFamily` — chứng minh, không sửa `Shape` |

Mới: `HPRebar.Mcp.Server.Core.Tests/Civil3dProfileTests.cs` — **28 test** (9 Fact/Theory method: constants; imports; globals; guard deny 19 mẫu; `tr.Commit` message host-neutral như AutoCAD; superset cấu trúc; reads/writes/rebuild-settings cho phép; analyzer; `Civil3dInfo` camelCase round-trip + omit khi null; `Shape` qua pipe thật với `FakeRevitExecutor`).

Không sửa: `IHostProfile`, `RequestDispatcher`, `McpBridgeHost`, `BridgeClient`, `Execute*`, `AnalyzeRequest`, `ScriptGuard` base, `ScriptCompiler`, `ScriptUnits`, `ContextService.Shape`, `ToolValidator`, `ToolManager`, `McpServerHost`, meta tool descriptions. `grep Autodesk. McpShared/*.csproj` = 2 hit, đều là comment "Never references Autodesk.*" (có sẵn).

## Gate
| Kiểm tra | Before | After | Kết quả |
|---|---|---|---|
| `tools/list` revit / autocad / navis / etabs | 33 / 62 / 24 / 24 | 33 / 62 / 24 / 24 | **4 JSON byte-identical** (`phase-00-tools-list-{before,after}-*.json`) |
| `HPRebar.Mcp.Server.Core.dll` SHA-256 | `3A3A774F…` | `086DF637…` | khác → rebuild thật |
| `McpShared/HPRebar.Mcp.Server.Core.Tests` | 164 | **192** (+28) | 0 fail, 0 skip ×3 lần |
| `McpShared/HPRebar.McpBridge.Core.Net48Tests` | 62 | 62 | 0 fail |
| `HPRebar.Mcp.Server.Tests` | 109 | 109 | 0 fail |
| `HPAutoCad.Mcp.Server.Tests` / `HPAutoCad.Aec.Tests` | 280 / 225 | 280 / 225 | 0 fail |
| `HPNavis.Mcp.Server.Tests` / `HPEtabs.Mcp.Server.Tests` | 49 / 81 | 49 / 81 | 0 fail |
| Build Debug: `HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` · `HPAutoCad.slnx -c Debug -p:DeployBundle=false` · `HPNavis.slnx -c Debug -p:DeployPlugin=false` · `HPEtabs.slnx -c Debug` | — | 4 × Build succeeded | 0 error |

Không chạy (cần host cài, ngoài gate engine): `HPNavis.McpBridge.Tests` (135), `HPEtabs.McpBridge.Tests` (184).

## Ghi chú
- Line ending: 6 file CRLF toàn bộ (như HEAD); warning `LF will be replaced by CRLF` là noise `core.autocrlf=true` thường lệ của repo.
- Sai lệch có chủ đích so phase-00 #5: dùng `Autocad.DeniedIdentifiers/DeniedMembers/DeniedNamespaces/DeniedMembersOnIdentifier` (đã public) thay literal — bảo đảm superset bằng cấu trúc; test `Civil3d_guard_profile_is_a_strict_superset_of_the_autocad_profile` pin.
- `RebuildAutomatic`/`AutoRebuild` **không** deny (ADR-04 §2) — test cho phép đọc.

## Sau code review (cùng ngày) — `reports/code-review-phase-00.md` 8/10
| Finding | Xử lý |
|---|---|
| **H1 (High, có sẵn, 5 host)** — `a?.b` là `MemberBindingExpression`; walker chỉ override `VisitMemberAccessExpression` → `corridor?.Rebuild()`, `db.TransactionManager?.StartTransaction()`, `tr?.Commit()` lọt ở mọi profile | **Fixed** — `ScriptGuard.VisitMemberBindingExpression` (+18 dòng, hàng #8 phase-00); test `Null_conditional_member_access_is_judged_like_a_dot` 9 case × 5 profile trong `ScriptGuardTests.cs` (link net48 → chạy cả 2 TFM). Precedent: fix `global::` ETABS phase 0 #15. User revert được 1 override |
| **M2 (Medium)** — deny-list file-member thiếu `ExportPoints`, `ImportPoints`, `CreateFromDEM`, `CreateSolidsAtDepthToFile`, `CreateSolidsAtSurfaceToFile` (tồn tại — probe xác nhận) | **Fixed** — +5 tên (hàng #9), +5 test case |
| **L3 (Low)** — `ExportTo` nhận `Database`, không phải path; `Rebuild` bắt cả `Spline.Rebuild` | **Fixed** comment (lý do đúng: ghi style sang bản vẽ khác; breadth chấp nhận) — vẫn deny |
| **L4 (Low, info, 5 host)** — `((HashSet<string>)GuardProfile.Civil3d.DeniedMembers).Clear()` compile được vì `HPRebar.McpBridge.Core.Scripting` nằm trong imports | **Chấp nhận MVP** (thiết kế "defense-in-depth, not a sandbox" + opt-in + audit) — ghi known gap CLAUDE.md phase 5; hardening (frozen set / tách `GuardProfile` khỏi namespace import) là follow-up engine |

**Gate lần 2:** Core.Tests **206**, Net48 **71**, 109/280/225/49/81 nguyên; 4 `tools/list` byte-identical; Core.dll `28779CBB…`; 4 Debug build xanh. `numstat`: 8 file chỉ thêm (+150) + `Civil3dProfileTests.cs` mới.
