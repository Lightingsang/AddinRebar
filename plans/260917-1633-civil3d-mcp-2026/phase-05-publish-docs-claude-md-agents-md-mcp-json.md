---
phase: 5
title: "Publish (server single-file + bundle), docs: CLAUDE.md section + Repository Layout row, AGENTS.md regen bằng sync engine, docs/*, README, .mcp.json note (user), memory"
status: pending
priority: P2
effort: "3h"
dependencies: [4]
---

# Phase 5: Publish + docs — trạng thái "Verified" chỉ ghi khi phase 4 pass

## Context Links
- [phase-04 report](reports/phase-04-live-verify.md) (NEW — phase 4 sinh) (số pass/skip/fail, nhóm M, known gaps) · [ADR-01](adr/adr-01-code-sharing-with-hpautocad-copy-vs-acadshared.md) (A → follow-up B ghi vào CLAUDE.md known gaps; nếu B → sửa câu chiều phụ thuộc) · [ADR-06 §1](adr/adr-06-server-profile-client-wiring-ribbon-identity.md) (mọi token/đường dẫn để viết docs)
- Docs hiện có (tồn tại, grep 2026-09-17): `CLAUDE.md:11` (câu chiều phụ thuộc liệt kê 4 folder MCP — **thêm `HPCivil3d/`**; "five unrelated deliverables" → six), bảng Repository Layout (hàng `HPEtabs/` là khuôn), mục "HPEtabs MCP Bridge" (khuôn mục mới); `docs/codebase-summary.md:14,43–51` (hàng HPNavis + mục ETABS), `docs/system-architecture.md:245,268,287,512,534` (đoạn 4 host, mục HPNavis Solution, Ribbon, sơ đồ), `docs/project-changelog.md:5–43` (khuôn entry theo ngày/phase), `docs/mcp-architecture.md`; `AGENTS.md` = generated (lệnh `_TO_PORTABLE` trong CLAUDE.md § Agent Config Sync; `scripts/skill_sync/adapters/portable_markdown.py:26,140`; `.skill-sync/config.json` **tồn tại** trên máy này → `python scripts/sync-agent-skills.py check` chạy được); `.mcp.json` tracked mang path máy — **không commit sửa**; `HPCivil3d/README.md` (phase 1); memory `~/.claude/projects/.../memory/` (ETABS/Navis/AutoCAD note là khuôn).

## Overview
Publish exe server (single-file) + bundle từ Release; viết docs theo khuôn 4 host (CLAUDE.md hàng bảng + mục riêng; `AGENTS.md` **regen bằng engine, không sửa tay**; `docs/*` 3 file; README HPCivil3d + harness; changelog; memory); ghi `.mcp.json` entry mẫu vào README/CLAUDE.md để **user tự thêm** (untracked). Wording nghiêm: Verified chỉ cho những gì phase 4 đo; nhóm M = "chưa làm".

## Requirements
- Publish: `dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server` (kích cỡ ≈ 7.4 MB ghi số thật); bundle: `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug` (Civil đóng) deploy `%AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\`; publish exe bị lock khi một server `hprebar-civil3d` đang chạy → restart Claude Code trước (CLAUDE.md § Publish AutoCAD đã ghi cách; **không** dùng trick rename nếu chưa được user đồng ý).
- `CLAUDE.md`: (1) `:11` câu chiều phụ thuộc thêm `HPCivil3d/`; "five" → "six"; (2) hàng bảng Repository Layout `HPCivil3d/` (khuôn hàng `HPEtabs/`: gì, stack `C# / net8.0-windows · net10 / AutoCAD.NET 25.1.0 + Civil API AeccDbMgd 13.8 (installed, no NuGet)`, plan path, test counts, "Civil 3D runs on the same acad.exe as AutoCAD; `Platform="Civil3D"` keeps the bundle out of AutoCAD; copy of the AutoCAD bridge with a mirror test (ADR-01 A) — or `AcadShared/` nếu B); (3) mục **"HPCivil3d MCP Bridge (Dynamic Civil 3D MCP Server)"** theo khuôn ETABS/Navis: runtime facts verified in spike (do not re-research: `Platform="Civil3D"` loads only in Civil 3D; `CivilApplication.ActiveDocument` behaviour; `DrawingUnits` Meters/Feet; `Parcel` no Area in .NET; exception type for `FindElevationAtXY`; `Corridor.Rebuild()` under abort — điền từ `reports/phase-01-spike.md`), bridge (bundle, ALC + `Aec` prefix, pipe, guard Civil, units two-layer, context 11 field, ribbon), server (profile, 24 tool, seed contract Civil: `civil` global, `drawingUnit` envelope, unit suffix rule), harness (số run/pass), install/update/remove, `.mcp.json` entry `hprebar-civil3d` (user adds, untracked) + env `HPCIVIL3D_MCP_Bridge__HostVersion=2026`, **Known gaps**: nhóm M chưa làm; `list_parcels` area (S-09); `Rebuild` policy (S-10 kết quả); Civil 3D 2025/.NET 10; pressure network parts; point group membership đầy đủ; ADR-01 follow-up B (điều kiện); plugin MCP cũ của user nạp vào Civil (E14) — ghi cho user biết; `test_tool realRun=true`; mirror test là hàng rào drift duy nhất (A).
- `AGENTS.md`: regen **đúng lệnh** trong CLAUDE.md § Agent Config Sync (python một dòng `_TO_PORTABLE`); `python scripts/sync-agent-skills.py check` (đọc-only) → ghi kết quả; **không** sửa `AGENTS.md` tay.
- `docs/codebase-summary.md` (hàng `HPCivil3d/` + mục "Civil 3D MCP (`HPCivil3d/`)" theo khuôn ETABS `:43–51`), `docs/system-architecture.md` (đoạn "supports four verified MCP hosts" → five + sơ đồ Civil như `:512`, mục Ribbon `:287` thêm HPCivil3d), `docs/project-changelog.md` (entry mỗi phase 0–5 theo khuôn `:5–43`), `docs/mcp-architecture.md` (một đoạn host thứ năm nếu file mô tả per-host — kiểm khi làm).
- `HPCivil3d/README.md` (build cần Civil 3D 2026 cài cho bridge + compile tests; server mọi máy; deploy/remove bundle; `-p:DeployBundle=false`; harness cách chạy + cảnh báo bundle MCP cũ của user; `.mcp.json` mẫu), `HPCivil3d/tools/harness/README.md`, `McpShared/README.md` (+1 dòng host thứ năm dùng `tools/`).
- Memory: `~/.claude/projects/f--1-CONG-VIEC-05-AI-01-Revit-02-Csharp-AddinRebar/memory/civil3d-mcp-plan.md` + dòng `MEMORY.md` (trạng thái, gotchas: `Platform="Civil3D"`, `<<C3D_Metric>>` gạch dưới, `CivilDocument` ở `ApplicationServices`, Parcel no Area, mirror test, hai plugin cũ nạp vào Civil).
- Git: commit theo phase (conventional, không AI reference) — **chỉ khi user yêu cầu commit/push**; `.mcp.json` không stage.
- Tuỳ chọn (ngoài scope, ghi follow-up): skill `.claude/skills/hp-mcp-civil3d/` (NEW, chưa tồn tại; + mirror `.agents/`) như `hp-mcp-autocad`/`hp-mcp-etabs`; plan B `AcadShared/`.

## Architecture
Không code mới ngoài publish. Docs trích mọi số từ `reports/phase-0{0,1,2,3,4}-*.md` — không số ước lượng.

## Related Code Files
- Modify: `CLAUDE.md`, `AGENTS.md` (generated), `docs/{codebase-summary, system-architecture, project-changelog, mcp-architecture}.md`, `McpShared/README.md`, `HPCivil3d/README.md`, `HPCivil3d/tools/harness/README.md`; memory files.
- Create: `HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe` (publish, gitignored), `reports/phase-05-docs-publish.md`.
- Không đụng: `.mcp.json` (user), `McpShared/` code, `HPAutoCad/` (trừ khi ADR-01 B).

## Implementation Steps
1. Publish server + deploy bundle; `tools/list` từ exe publish = 24 (`reports/phase-05-tools-list-civil3d-published.json`).
2. CLAUDE.md (3 chỗ) — số từ reports; AGENTS.md regen bằng lệnh engine; `sync-agent-skills.py check`.
3. `docs/*` 4 file + README ×3 + `McpShared/README.md`.
4. Memory note + `MEMORY.md` dòng.
5. `reports/phase-05-docs-publish.md`; kiểm link (mọi path trong docs tồn tại — `ls`), wording Verified/Built/Tested đúng phase.
6. Đề xuất commit theo phase cho user (không tự commit).

## Todo List
- [ ] Publish exe + bundle · [ ] CLAUDE.md 3 chỗ · [ ] AGENTS.md regen + check · [ ] docs ×4 · [ ] README ×3 + McpShared · [ ] Memory · [ ] Report · [ ] Đề xuất commit

## Success Criteria
- [ ] `python McpShared/tools/mcp-call.py HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe tools/list --env HPCIVIL3D_MCP_Registry__LibraryPath=<tmp> --env HPCIVIL3D_MCP_Registry__DbPath=<tmp>/registry.db` → 24 tool từ **exe publish**.
- [ ] `grep -n "HPCivil3d" CLAUDE.md` ≥ 3 vị trí (câu chiều phụ thuộc, bảng, mục riêng); `grep -c "Civil 3D" AGENTS.md` > 0 **và** `AGENTS.md` = output đúng của lệnh `_TO_PORTABLE` (chạy lại lệnh → `git diff AGENTS.md` rỗng).
- [ ] `python scripts/sync-agent-skills.py check` → exit 0 (hoặc drift chỉ ở file ngoài scope — ghi rõ).
- [ ] Mọi path trong mục CLAUDE.md mới tồn tại (`ls` từng path — script nhỏ trong report).
- [ ] Wording: "Verified" chỉ ở mục có số phase 4; nhóm M ghi "chưa làm"; không "supported" cho Civil 3D 2025.
- [ ] `git status` không có `.mcp.json` staged; `output/` gitignored.

## Risk Assessment
| Risk | Mitigation |
|---|---|
| Exe publish bị lock (server `hprebar-civil3d` đang chạy nếu user đã thêm `.mcp.json`) | báo user restart Claude Code; không rename trick tự ý |
| `AGENTS.md` drift do sửa tay | chỉ regen bằng lệnh; test `git diff` rỗng sau chạy lại |
| CLAUDE.md quá dài (mục ETABS/AEC đã rất dài) | mục Civil ngắn hơn: tham chiếu AutoCAD cho phần chung ("= AutoCAD MCP except…"), chỉ liệt kê delta + known gaps |
| Chọn B muộn (sau phase 4) | ADR-01 follow-up ghi điều kiện; docs A nói rõ "copy + mirror test" |

## Security Considerations
- Docs không path máy/user; `.mcp.json` mẫu dùng path tương đối repo; không secret.

## Next Steps (ngoài plan)
- Follow-up B `AcadShared/` (nếu user muốn sau khi hai bridge verified); skill `hp-mcp-civil3d`; seed `create_tin_surface_from_points`, `import_landxml` (cần path policy), point-group membership; Civil 3D 2025 / .NET 10 (2027).
