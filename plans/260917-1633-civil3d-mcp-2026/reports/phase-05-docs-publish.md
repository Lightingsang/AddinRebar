# Phase 5 — Publish + docs (2026-09-18)

## Publish
- `dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server` → exe **7.12 MB** (gitignored). `python McpShared/tools/mcp-call.py <exe> tools/list --env HPCIVIL3D_MCP_Registry__LibraryPath=<tmp> --env HPCIVIL3D_MCP_Registry__DbPath=<tmp>/registry.db` → **24 tool** (`phase-05-tools-list-civil3d-published.json`; mcp-call cần đường dẫn exe tuyệt đối từ Git Bash).
- Bundle: `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug` (0 acad.exe) → `%AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\` (`Platform="Civil3D"`, `SeriesMin="R25.1"`, `AppVersion="0.1.0"`, Loader.dll 14:59). Bridge không đổi từ phase 2 → không cần run live lại.

## Docs
| File | Thay đổi |
|---|---|
| `CLAUDE.md` | `:11` "six unrelated deliverables", `HPCivil3d/` trong chiều phụ thuộc + ngoại lệ tooling cross-MCP (mirror test đọc text `HPAutoCad/**`, harness chạy `HPAutoCad/tools/harness/{bridge.scr, run-live-verify.ps1}`); hàng bảng `HPCivil3d/` (khuôn HPEtabs); hàng `McpShared/` liệt kê `HPCivil3d/tools/harness/`; mục **"HPCivil3d MCP Bridge (Dynamic Civil 3D MCP Server)"** (ADR-01 A + mirror contract, sự thật spike, bridge = AutoCAD trừ deltas, server + seed contract, harness + kết quả, install/publish/`.mcp.json`, known gaps) |
| `AGENTS.md` | regen bằng lệnh `_TO_PORTABLE` trong CLAUDE.md § Agent Config Sync; chạy lại → idempotent `True`; `python scripts/sync-agent-skills.py check` → conflicts/drift/findings **none**, exit 0; `grep -c "Civil 3D" AGENTS.md` = 11 |
| `docs/codebase-summary.md` | hàng `HPEtabs/` (thiếu trước đó) + `HPCivil3d/` trong Repository Layout; mục "Civil 3D MCP (`HPCivil3d/`)" (bảng 6 project/harness, sự thật spike, phase 0–5, known gaps) |
| `docs/system-architecture.md` | "five verified MCP hosts" + `civil3d.execute` trong suffix routing; mục "Civil 3D MCP Bridge — Architecture" (sơ đồ, bảng quyết định, live verify) |
| `docs/project-changelog.md` | 6 entry Civil 3D (phase 0–5) trên đầu |
| `McpShared/README.md` | consumer thứ năm + `HPCivil3d/tools/harness/`; ghi rõ Civil đọc *file* AutoCAD, không project |
| `HPCivil3d/README.md` | dòng "plan complete", tests 84 → 106 + test default, đoạn `run-live-verify.ps1` |
| `HPCivil3d/tools/harness/README.md` | cột kết quả run-live-verify: 3 × 76 + 8/8; 80/80 + 9/9 |
| `docs/mcp-architecture.md` | **không đụng** — file tự khai là tham khảo cũ (TypeScript/TCP), không mô tả per-host |

Kiểm path: script nhỏ đọc mục CLAUDE.md mới + hàng bảng, lấy mọi token có `/` → 21 path kiểm `os.path.exists`; "thiếu" chỉ còn token không phải path (`/product C3D`, `tools/list`, `doc/db/…`), path tương đối trong ngữ cảnh (`../McpShared/`, `reports/phase-01-spike.md`) và `AcadShared/` (follow-up, ghi rõ là chưa có). Wording: "Verified"/"verified live" chỉ ở câu có số phase 4; nhóm M = "not driven by the harness"/"untested"; Civil 3D 2025 ghi "untested — nothing beyond Civil 3D 2026 … is supported".

## Client wiring (user)
`.mcp.json` (tracked, path máy — không commit): thêm `hprebar-civil3d` → `HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe` + env `HPCIVIL3D_MCP_Bridge__HostVersion=2026`; Civil 3D: ribbon **HPCivil3d ▸ MCP ▸ MCP Bridge** → start listener → tick *Allow AI code execution*. Exe publish bị lock khi server đang chạy → restart Claude Code trước khi publish lại.

## Ngoài scope (follow-up)
- ~~Skill `.claude/skills/hp-mcp-civil3d/` (+ mirror `.agents/`) như 4 host khác.~~ → làm ngay sau phase 5 (2026-09-18): SKILL.md + 4 reference + 2 script + evals; mirror bằng sync engine, drift none.
- ADR-01 option B `AcadShared/` — chỉ khi user muốn, sau khi hai bridge verified (đã).
- Seeds: `create_tin_surface_from_points`, `import_landxml` (cần path policy), point-group membership, pressure parts.
- Civil 3D 2025 / AutoCAD 2026 Update 1.2 (.NET 10).
