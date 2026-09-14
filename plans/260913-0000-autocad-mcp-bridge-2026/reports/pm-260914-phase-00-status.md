# PM status — HPAutoCad MCP Bridge 2026 · phase 0 (2026-09-14)

**Plan:** `plans/260913-0000-autocad-mcp-bridge-2026/` · **Branch:** `RebarVersion1` (10 commit mới, chưa push) · **Plan status:** in-progress (1/6 phase)

## Phase 0 — Extract `McpShared/` + scaffold `HPAutoCad/`

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| git mv Contracts + McpBridge.Core → `McpShared/`, `HPRebar.slnx` path `../McpShared/…` | ✅ Built | `8a1144f` (28 rename) |
| `ResolveConfigurationsModule` pin `Solutions.HPRebar` | ✅ Built | `96ec3a9`; `build/Build.csproj` compile 0 warning |
| Contracts additive + Core host-neutral | ✅ Built + Tested | `89c3928` |
| Extract `HPRebar.Mcp.Server.Core`; exe Revit mỏng | ✅ Built + Tested | `648b3bd` (30 rename) |
| Tests split + host-neutrality tests | ✅ Tested | `0fa3985` |
| Scaffold `HPAutoCad/` | ✅ Built | `bf680f4` |
| CLAUDE.md + AGENTS.md | ✅ | `884a229` |
| Code review → fix 2 major + 8 minor | ✅ Tested | `876c3d6`; `reports/phase-00-code-review.md` |
| Tester 8/8 gate | ✅ | `reports/phase-00-test-report.md` |
| `tools/list` before ≡ after (34 tool, byte-identical) | ✅ Verified | `reports/phase-00-tools-list-{before,after}.json` |
| Live smoke exe mới ↔ bridge Revit **cũ** (không redeploy) | ✅ Verified (đọc) | `get_revit_context` trả doc `THBB2-HPC-ZZ-ZZ-CM-ES-0001`; `execute_revit_code` → `-32001` (opt-in OFF trong phiên Revit — đường lỗi đúng; đường ghi CHƯA TEST phiên này) |
| docs/ (`codebase-summary`, `system-architecture`, changelog) | 🟡 đang cập nhật (docs-manager) | — |

Tests: `McpShared/HPRebar.Mcp.Server.Core.Tests` **70**, `HPRebar/HPRebar.Mcp.Server.Tests` **106** (= 159 gốc + 17 mới), `HPRebar.Core.Tests` 334 không đổi. Build: `McpShared.slnx`, `HPRebar.slnx` Debug.R26 + Debug.R23, `HPAutoCad.slnx`, `build/Build.csproj` xanh.

## Sai lệch so với plan (đã ghi trong phase-00)
- Test mới gộp thành `HostNeutralityTests` + `ProfileOptionsBindingTests` thay vì 5 file dự kiến.
- Hành vi Revit đổi có chủ ý: `serverInfo.name` = `HPRebar Revit MCP`; 3 chuỗi lỗi bỏ chữ "HPRebar"/"Revit" cứng.
- `cd HPRebar/build && dotnet run` (Compile mọi Release.R\*) không chạy — R27 fail sẵn ngoài scope; gate thay bằng compile pipeline.
- Chưa làm (không chặn): rename `FakeRevitExecutor`; `GuardProfile.Autocad` giữ trong McpShared (quyết định, README ghi lý do).

## Phát hiện đáng nhớ
- Options binder ghi ngược giá trị getter → default tính toán bị pin trước `PostConfigure`; fix bằng setter bỏ qua giá trị bằng default + `Configure` trước `Bind` (4 regression test).
- `SynchronizationContext.Current` không đảm bảo trên UI thread Revit → VM nhận marshaller bắt buộc; `StateChanged` trong `try` để không kẹt `_busy`.

## Tiếp theo
Phase 1 (`HPAutoCad.McpBridge.Loader` + `HPAutoCad.McpBridge`, bundle, ALC spike) — sẵn sàng; cần AutoCAD 2026 đóng khi deploy bundle.
