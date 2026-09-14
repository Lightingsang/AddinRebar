---
title: "HPAutoCad MCP Bridge 2026"
description: "AI → MCP → AutoCAD 2026 runtime trong folder top-level riêng HPAutoCad/, mã chung ở McpShared/ (Contracts, script engine, registry engine); vòng lặp tự sinh & ghi nhớ tool ánh xạ 1:1 với Revit"
status: in-progress
priority: P2
effort: 64h
branch: RebarVersion1
tags: [autocad, mcp, roslyn, named-pipe, registry, assemblyloadcontext, mcpshared]
created: 2026-09-13
revised: 2026-09-14
---

# HPAutoCad MCP Bridge 2026 — Plan

**Ngày:** 2026-09-13 · **Revised 2026-09-14** (ràng buộc mới: mỗi MCP một folder top-level → [ADR-06](adr/adr-06-one-mcp-one-folder.md); bản 2026-09-13 "một exe + host profile" superseded) · **Status:** in-progress — phase 0 built + tested + smoke-verified 2026-09-14 (9 commit trên `RebarVersion1`, chưa push); phase 1–5 planned · **Branch:** `RebarVersion1` · Template: Stack-Aware 6-phase (phase 0 = di dời mã chung + scaffold; phase 3 WPF gộp vào 2; phase 4 = registry theo profile)

## Executive summary
- **Ba folder top-level:** `HPRebar/` (Revit add-in + Revit MCP, hành vi không đổi) · `HPAutoCad/` (AutoCAD MCP: exe `HPAutoCad.Mcp.Server`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`, tests, `HPAutoCad.slnx`) · `McpShared/` (thư viện host-neutral, **không phải MCP**: `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core` mới tách từ exe Revit, tests engine). Chiều phụ thuộc duy nhất: MCP → `McpShared`; không bao giờ MCP → MCP (ADR-06).
- **Hai exe, một engine:** 8 registry tool, `toolify_run`, CLI `registry approve`, `ToolManager`/`ToolValidator`/`DynamicToolRegistrar`/SQLite/watcher, pipe client, `ExecuteCodeService` sống trong `McpShared/HPRebar.Mcp.Server.Core`; mỗi exe chỉ còn `Program.cs` mỏng + `IHostProfile` + 4 tool/prompt/resource + seed nhúng. Registry root theo product: `%AppData%\HPRebar\McpServer\` (không đổi) / `%AppData%\HPAutoCad\McpServer\` (ADR-04 revised).
- **Bridge AutoCAD** = loader mỏng (default ALC, `IExtensionApplication`, command `HPMCPBRIDGE`) + bridge thật trong **AssemblyLoadContext riêng** — bắt buộc: AutoCAD 2026 ship Roslyn 4.10, framework .NET 8 chỉ có `System.Collections.Immutable` 8.0 (Roslyn 5.9 cần 10.0.1), **đã kiểm trên máy dev** (ADR-05, research §0.3). Thread: `Application.Idle` + `IsQuiescent` + `LockDocument` (ADR-02). Transaction: bridge mở transaction ngoài cùng → global `tr`; `Abort` = rollback (ADR-03).
- 12 seed AutoCAD nhúng trong exe AutoCAD, compile-check xUnit bằng `AutoCAD.NET` 25.1.0 (`lib/net8.0`, có trong NuGet cache). 21 seed Revit **không di chuyển**.

## Design of record
[architecture.md](architecture.md) (3 folder, 2 exe §1; sequence + registry loop §2; tool surface §3; IPC §4; layout §5; ma trận tái dùng §6; **bảng Revit ↔ AutoCAD 21 hàng §7**) · ADR: [06 one MCP one folder (ma trận a/b/c)](adr/adr-06-one-mcp-one-folder.md) · [01 (superseded phần server, giữ ma trận lịch sử)](adr/adr-01-reuse-mcp-server-host-profile.md) · [02 main thread](adr/adr-02-autocad-main-thread-marshalling.md) · [03 transaction/undo/dryRun](adr/adr-03-autocad-transaction-undo-dryrun-policy.md) · [04 registry per exe (revised)](adr/adr-04-registry-per-host-library-and-host-field.md) · [05 packaging/ALC/multi-version](adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) · Research: [AutoCAD .NET API 2026 (+§0 corrections, on-machine)](research/autocad-dotnet-api-2026-report.md) · [repo tham khảo + precedent .NET](research/autocad-bridge-reference-report.md) · [NotebookLM Q13](research/notebooklm-addendum-q13-server-granularity.md) · Revit: [ADR-01..06](../260912-1521-dynamic-revit-mcp-server-2026/adr/).

## Phases (tên file giữ để không vỡ link; tiêu đề trong file đã cập nhật)
| # | File | Status | Depends | Effort |
|---|---|---|---|---|
| 0 | [phase-00](phase-00-host-profile-server-and-core-neutralization.md) — **Extract `McpShared/`** (git mv Contracts/Core, tách `Server.Core` khỏi exe Revit, test engine theo mã, `McpShared.slnx`, `HPRebar.slnx` path, fix `ResolveConfigurationsModule`), Core host-neutral, scaffold `HPAutoCad/`; CLAUDE.md/AGENTS.md | **built + tested + smoke-verified (2026-09-14)** — 9 commit `8a1144f..876c3d6`; 70 + 106 test; `tools/list` byte-identical; live `get_revit_context` với bridge cũ OK; review 6.5/10 → 2 major đã fix (VM marshaller, options binder) | — | 14h (≈12h) |
| 1 | [phase-01](phase-01-autocad-plugin-scaffold-loader-alc-spike.md) — `HPAutoCad/` 2 project bridge, bundle, ALC, **spike có gate** | **built + tested + verified in AutoCAD 2026 (2026-09-14)** — spike 5/5 unattended ×3, AutoCAD tự thoát; review 7/10 → 16/16 fix; 176/176 test (`reports/phase-01-*.md`) | 0 | 8h (≈5h) |
| 2 | [phase-02](phase-02-autocad-bridge-runtime-threading-transactions-context.md) — executor, runner (lock/tr/dryRun), context, serializer, change counter, status window; harness pipe | **built + tested + verified in AutoCAD 2026 (2026-09-14)** — harness 21/21 ×2; review 7.5/10 → 16/17 fix; 88+106 test; ADR-03 Accepted (revised: 2 transaction, script không mở transaction, undo gộp) | 0, 1 | 14h (≈7h) |
| 3 | [phase-03](phase-03-server-autocad-host-tools-prompts-resources.md) — exe `HPAutoCad.Mcp.Server` (Program mỏng + `AutocadHostProfile` + 4 tool/prompt/resource), tests, `.mcp.json`, hai-exe smoke | **built + tested + verified (2026-09-14)** — stdio 7/7 với AutoCAD 2026, 34/12 hai exe, 8 test | 0 | 6h (≈2h) |
| 4 | [phase-04](phase-04-registry-per-host-and-autocad-seed-tools.md) — registry theo profile trong `McpShared` (categories/validator/text/`host`), **12 seed AutoCAD** nhúng trong exe + compile-check | planned | 3 | 12h |
| 5 | [phase-05](phase-05-verify-live-autocad-registry-loop-and-docs.md) — execute matrix 14 · mọi seed · **HIT · MISS→approve→gọi tên · HỎNG→quarantine→restore** · hồi quy Revit + hai exe song song · docs/CLAUDE.md/AGENTS.md | planned | 2, 3, 4 | 10h |

## Key decisions
- Mỗi MCP một folder + `McpShared/` cho mã chung; hai exe riêng; `IHostProfile` là seam compile-time (không env switch). Wire-compat với bridge Revit đã deploy: Contracts chỉ thêm; dispatcher nhận cả `revit.*` và `autocad.*`.
- MVP **AutoCAD 2026 base .NET 8** (máy dev R25.1.74, chưa Update 1.2). `AutoCAD.NET` pin **[25.1.0]** (25.1.1/26.0.0 là net10). Đường mở 2025 (25.0.1) và .NET 10 ghi ADR-05 §4.
- Script contract AutoCAD: `doc, db, ed, app, tr, units, ct, log, progress, args` (`units` = Core `ScriptUnits`); không import `Autodesk.AutoCAD.Runtime`/`.Core`; guard deny mọi prompt `ed.Get*`, `SendStringToExecute`, `Command*`, modal, `tr.Commit/Abort`.
- `none` = mở `tr` + luôn Abort + lỗi nếu `Changed ≠ 0`; timeout luôn fail + Abort (giữ quy tắc Revit đã verified).

## Top-5 risks
| Risk | L×I | Mitigation |
|---|---|---|
| ~~Roslyn 5.9 trong ALC riêng vẫn bind Roslyn 4.10 / Immutable 8.0 của AutoCAD~~ **ĐÃ LOẠI (spike 2026-09-14)** | — | ALC riêng verified; canary `ScriptingSelfCheck` giữ ở startup |
| Di dời `McpShared/` phá build/test Revit (`.slnx` `..\` `[unverified]`, `FindFile(".slnx")` bắt nhầm solution, `SeedInstaller` mặc định assembly sai, `WithToolsFromAssembly` quét sai assembly) | M×M | phase 0 là phase riêng, không đổi hành vi; snapshot `tools/list` before/after; pin `Solutions.HPRebar`; `SeedInstaller(hostAssembly)`; fallback `.sln`/`McpServerTool.Create` |
| ~~`Application.Idle` không subscribe được từ pipe thread~~ **ĐÃ LOẠI (spike)**: subscribe từ thread ngoài OK, handler trên main thread; `ExecuteInApplicationContext` bị loại (callback trên thread gọi) | — | ADR-02 Accepted |
| Script treo AutoCAD (prompt/vòng lặp không hợp tác) hoặc AutoCAD bận (command/modal) | M×M | guard deny `ed.Get*`/command; `ct` cooperative; `IsQuiescent` + `BusyGraceSeconds` → `-32002` "press ESC"; `LockDocument` luôn |
| Bundle không nạp (SECURELOAD/TRUSTEDPATHS) hoặc nạp vào Civil 3D/Advance Steel cùng R25.1 → tranh pipe | L×M | `Platform="AutoCAD"` MVP; NETLOAD tay + TRUSTEDPATHS docs; pipe `maxInstances=1` fail-fast; pipe Revit/AutoCAD khác tên nên hai MCP không xung đột nhau |

## Quyết định Claude tự chốt (đổi được)
| # | Vấn đề | Chốt | Lý do |
|---|---|---|---|
| 1 | Tên folder | `HPAutoCad/` (mirror `HPRebar/`), `McpShared/` (thư viện chung) | theo gợi ý user; `McpShared` không mang tên host — **xem câu hỏi 1** |
| 2 | Tên exe / project AutoCAD | `HPAutoCad.Mcp.Server`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`, `HPAutoCad.Mcp.Server.Tests`; pipe `hpautocad-mcp-2026`; env prefix `HPAUTOCAD_MCP_`; bundle `HPAutoCad.McpBridge.bundle` | đối xứng với Revit |
| 3 | Cách chia mã chung | (b) folder `McpShared/` với 3 lib + tests engine (ADR-06 ma trận) | không cross-wire, build độc lập, chi phí một lần |
| 4 | Tên assembly/namespace mã chung | **giữ** `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core` (mới, cùng tiền tố cho nhất quán) trong phase 0; rename `HPMcp.*` là bước sau | git mv sạch, không churn ~90 file `using` — **xem câu hỏi 2** |
| 5 | `build/` | Revit giữ `HPRebar/build/` (+1 dòng pin solution); AutoCAD MVP **không** có pipeline — `dotnet build/publish` + target `DeployBundle`; `McpShared/` chỉ `.slnx` | YAGNI; pack AutoCAD sau MVP |
| 6 | Test engine | git mv theo mã sang `McpShared/HPRebar.Mcp.Server.Core.Tests`; Revit giữ test seed/profile Revit; gate = tổng hai suite ≥ 159 + mới | test của engine sống cạnh engine |
| 7 | Registry root / `host` field / seed | root `%AppData%\<Product>\McpServer\`; `host` giữ làm metadata + hàng rào copy tay; seed nhúng per exe, prefix `SeedLibrary/` không đổi | zero migration cho 22 tool Revit; không di chuyển 63 file seed |
| 8 | Globals `tr` + `units` (đã xác nhận khuyến nghị 2026-09-13, chưa có trả lời) | Thêm | AutoCAD không đọc/ghi được nếu không cầm `Transaction`; `units` tránh lặp 12 lần |
| 9 | Bundle `Platform` | `"AutoCAD"` (không `*`) cho MVP | tránh Civil 3D/Advance Steel 2026 cùng R25.1 tranh pipe |
| 10 | Feature-folder convention | AutoCAD bridge không phải Nice3point add-in nhưng giữ `Model/ Service/ View/ ViewModel/` | đồng nhất repo |

## Quyết định user đã xác nhận (2026-09-14, theo khuyến nghị)
1. **Tên folder:** `HPAutoCad/` + `McpShared/` — **xác nhận**.
2. **Rename assembly chung sang `HPMcp.*`:** **để sau** phase 0 (commit atomic riêng khi Revit + AutoCAD đều xanh) — **xác nhận**.
3. **Runtime mục tiêu:** AutoCAD 2026 base **.NET 8** (đang cài); .NET 10 (Update 1.2) là bước sau MVP — **xác nhận**. Gate đầu phase 1: kiểm lại `acdbmgd.runtimeconfig.json`.
Cũng đã xác nhận (từ 2026-09-13): globals `tr` + `units`; bundle `Platform="AutoCAD"`.
