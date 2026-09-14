# Phase 3 — `HPAutoCad.Mcp.Server.exe` over stdio, verified with AutoCAD 2026 + two exes side by side (2026-09-14)

**Cách chạy:** `dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server` (7,4 MB) → `pwsh HPAutoCad/tools/harness/run-server-smoke.ps1` (start AutoCAD với `bridge.scr`, UIA tick opt-in, `mcp-call.py` stdio như host AI, kill acad). Không cần `.mcp.json`.

## Stdio smoke (harness run 1 + run 2 sau review): 7/7 ×2

| # | Bước | Kết quả |
|---|---|---|
| 1 | `initialize` | ✅ `serverInfo.name = "HPAutoCad MCP"`, capabilities tools/prompts/resources/logging |
| 2 | `tools/list` | ✅ **12**: `execute_autocad_code`, `get_autocad_context`, `inspect_type`, `cancel_execution` + 8 registry (`search_tools … manage_tool`); không tên nào chứa "revit" (`reports/phase-03-tools-list-autocad.json`) |
| 3 | `get_autocad_context {includeSelection:true}` | ✅ `host=autocad, hostVersion=2026`, **không có `revitVersion`**, `autocad{insunits=Inches, measurement=English, currentLayout=Model, currentLayer=0, isModelSpace, isQuiescent, isNamedDrawing=false}`, `executionEnabled=true` |
| 4 | `execute_autocad_code` `return db.Filename;` (`none`) | ✅ 8 ms, `runId=1` |
| 5 | dryRun tạo Line (`args.lengthMm=500`) | ✅ `changed.added=1`, `rolledBack=true`, value `{handle:"25E", length:19.685}`, count 0→0 |
| 6 | real run tạo Line (`args.lengthMm=1234.5`) | ✅ `changed.added=1`, `runId=5`, `hint` "looks reusable → get_run 5 → propose_tool", count 0→1 |
| 7 | `get_run 5` | ✅ record `kind=adhoc, success=true, args, code, docTitle=Drawing1.dwg` từ `%AppData%\HPAutoCad\McpServer\registry.db` |

Registry root `%AppData%\HPAutoCad\McpServer\{registry.db, tools-library}` tạo tự động ở lần start đầu (0 seed → warning trong stderr, đúng phase 3).

## Hai exe cùng phiên

| Exe | `serverInfo.name` | `tools/list` | Bridge |
|---|---|---|---|
| `HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` (bản publish hiện có, trước phase 0) | `HPRebar.Mcp.Server` 1.0.0.0 | **34**, tên y hệt `phase-00-tools-list-after.json` | Revit 2026 đang chạy: `get_revit_context` trả `revitVersion` + doc thật (không đổi) |
| `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe` | `HPAutoCad MCP` 1.0.0 | **12** | AutoCAD 2026 qua harness |

- Registry tách biệt: exe AutoCAD chỉ chạm `%AppData%\HPAutoCad\McpServer\`; `%AppData%\HPRebar\McpServer\tools-library` giữ mtime 2026-09-12 (`registry.db` Revit chỉ đổi khi chính exe Revit start — hành vi cũ).
- Bản Revit publish trong `output/` là build trước phase 0 (tên server cũ); phase 0 đã verify exe mới byte-identical tools/list. Publish lại là việc của user khi muốn (không bắt buộc).

## Ghi chú / follow-up
- `get_run` record của registry vẫn dùng field `revitVersion` (DTO engine, host-neutral chưa đổi tên) — phase 4 cân nhắc alias `hostVersion` (đổi tên ảnh hưởng output Revit).
- `tools/list` version `1.0.0` = version assembly engine; gắn version sản phẩm là việc pack.
- `.mcp.json` snippet (untracked, user thêm khi muốn dùng trong Claude Code):

```json
"hprebar-autocad": {
  "command": "F:\\1-CONG VIEC\\05-AI\\01_Revit\\02_Csharp\\AddinRebar\\HPAutoCad\\output\\HPAutoCad.Mcp.Server\\HPAutoCad.Mcp.Server.exe",
  "args": [],
  "env": { "HPAUTOCAD_MCP_Bridge__HostVersion": "2026" }
}
```

## Số liệu
`HPAutoCad.slnx` Debug 0 warn/err (4 project); `HPAutoCad.Mcp.Server.Tests` 8/8 (profile, options, tool surface 12, resources/prompts, context/resource/execute/refusal over pipe thật); McpShared 89/89 (+1: Revit giữ `revitVersion`); HPRebar MCP 106/106.
