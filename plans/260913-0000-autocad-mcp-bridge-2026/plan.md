---
title: "HPRebar AutoCAD MCP Bridge 2026"
description: "AI → MCP → AutoCAD 2026 runtime, tái dùng server/registry/Core của Revit bridge; vòng lặp tự sinh & ghi nhớ tool ánh xạ 1:1"
status: planned
priority: P2
effort: 62h
branch: RebarVersion1
tags: [autocad, mcp, roslyn, named-pipe, registry, assemblyloadcontext]
created: 2026-09-13
---

# HPRebar AutoCAD MCP Bridge 2026 — Plan

**Ngày:** 2026-09-13 · **Status:** planned (chưa đổi source) · **Branch:** `RebarVersion1` · Template: Stack-Aware 6-phase (biến thể: phase 0 = host abstraction thay scaffold Nice3point; phase 3 WPF gộp vào phase 2; phase 4 = registry theo host)

## Executive summary
- **Một exe `HPRebar.Mcp.Server`, một instance = một host** (`HPREBAR_MCP_Host=autocad`, pipe `hprebar-mcp-acad2026`). 8 registry tool, `toolify_run`, CLI `registry approve`, `ToolManager`/`ToolValidator`/`DynamicToolRegistrar`/SQLite/watcher **dùng chung mã**; chỉ 4 core tool + 2 prompt + 2 resource là file mỏng theo host (ADR-01).
- **Bridge AutoCAD** = 2 assembly: loader mỏng (`IExtensionApplication`, command `HPMCPBRIDGE`) tạo **AssemblyLoadContext riêng** cho bridge thật (Core + Roslyn 5.9 + Contracts) vì AutoCAD 2026 nạp plugin vào default ALC, ship sẵn Roslyn 4.10 và framework .NET 8 chỉ có `System.Collections.Immutable` 8.0 (Roslyn 5.9 cần 10.0.1) — **đã kiểm trên máy dev** (ADR-05, research §0.3).
- Thread: `Application.Idle` one-shot + `IsQuiescent` + `LockDocument` (ADR-02; precedent = plugin AutoCadMcp cũ của user trên chính máy này). Transaction: bridge mở transaction ngoài cùng, script nhận `tr`; `Abort` = rollback cho dryRun/none/lỗi/timeout (ADR-03; AutoCAD không có TransactionGroup).
- Registry: library + DB **riêng theo host** (`tools-library-autocad`, `registry-autocad.db`), `tool.json` thêm `host`, seed `SeedLibrary/<Host>/…`, categories/reserved theo profile; 22 tool Revit đã cài không đổi chỗ (ADR-04). 12 seed AutoCAD compile-check trong xUnit bằng `AutoCAD.NET` 25.1.0 (`lib/net8.0`, có sẵn trong NuGet cache).

## Design of record
[architecture.md](architecture.md) (component/sequence + registry loop §1–2, tool surface §3, IPC §4, layout §5, ma trận tái dùng §6, **bảng Revit ↔ AutoCAD 20 hàng §7**) · ADR: [01 server reuse (ma trận 3 phương án)](adr/adr-01-reuse-mcp-server-host-profile.md) · [02 main thread](adr/adr-02-autocad-main-thread-marshalling.md) · [03 transaction/undo/dryRun](adr/adr-03-autocad-transaction-undo-dryrun-policy.md) · [04 registry per host](adr/adr-04-registry-per-host-library-and-host-field.md) · [05 packaging/ALC/multi-version](adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) · Research: [AutoCAD .NET API 2026 (+§0 corrections, on-machine)](research/autocad-dotnet-api-2026-report.md) · [repo tham khảo + precedent .NET](research/autocad-bridge-reference-report.md) · [NotebookLM Q13](research/notebooklm-addendum-q13-server-granularity.md) · Revit: [ADR-01..06](../260912-1521-dynamic-revit-mcp-server-2026/adr/), [NotebookLM Q01–Q12](../260912-1521-dynamic-revit-mcp-server-2026/research/notebooklm-mcp-csharp-report.md).

## Phases
| # | File | Status | Depends | Effort |
|---|---|---|---|---|
| 0 | [phase-00-host-profile-server-and-core-neutralization.md](phase-00-host-profile-server-and-core-neutralization.md) — `IHostProfile`, `ExecuteCodeService`, dispatcher suffix, `GuardProfile`/`AnalyzerProfile`, ViewModel/Host → Core; **0 đổi hành vi Revit** (gate 159 test + `tools/list`=34) | planned | — | 8h |
| 1 | [phase-01-autocad-plugin-scaffold-loader-alc-spike.md](phase-01-autocad-plugin-scaffold-loader-alc-spike.md) — 2 project, bundle, ALC, **spike có gate** (Roslyn trong ALC, Idle từ thread ngoài, WPF modeless) | planned | — (∥ 0) | 8h |
| 2 | [phase-02-autocad-bridge-runtime-threading-transactions-context.md](phase-02-autocad-bridge-runtime-threading-transactions-context.md) — executor, runner (lock/tr/dryRun), context, serializer, change counter, status window; harness pipe 10 kịch bản | planned | 0, 1 | 14h |
| 3 | [phase-03-server-autocad-host-tools-prompts-resources.md](phase-03-server-autocad-host-tools-prompts-resources.md) — `AutocadHostProfile`, `execute_autocad_code`, `get_autocad_context`, prompts, resources, `.mcp.json`, dual-host smoke | planned | 0 | 8h |
| 4 | [phase-04-registry-per-host-and-autocad-seed-tools.md](phase-04-registry-per-host-and-autocad-seed-tools.md) — ADR-04 trong mã chung, seed Revit → `SeedLibrary/Revit/`, **12 seed AutoCAD** + compile-check, registry tests theo host | planned | 3 | 14h |
| 5 | [phase-05-verify-live-autocad-registry-loop-and-docs.md](phase-05-verify-live-autocad-registry-loop-and-docs.md) — execute matrix 14 · mọi seed · **HIT · MISS→approve→gọi tên · HỎNG→quarantine→restore** · hồi quy Revit + dual-host · docs/CLAUDE.md/AGENTS.md | planned | 2, 3, 4 | 10h |

## Key decisions
- Host switch trong một exe, instance domain-focused (ADR-01; NotebookLM Q13). Wire-compat với bridge Revit đã deploy: Contracts chỉ thêm; dispatcher nhận cả `revit.*` và `autocad.*`.
- MVP **AutoCAD 2026 base .NET 8** (máy dev R25.1.74, chưa Update 1.2). `AutoCAD.NET` pin **[25.1.0]** (25.1.1/26.0.0 là net10). Đường mở 2025 (25.0.1) và .NET 10 ghi ADR-05 §4.
- Loader + ALC riêng là **bắt buộc**, không phải tối ưu (research §0.3). Canary `ScriptingSelfCheck` + log tên ALC của Roslyn.
- Script contract AutoCAD: `doc, db, ed, app, tr, units, ct, log, progress, args`; không import `Autodesk.AutoCAD.Runtime`/`.Core`; guard deny mọi prompt `ed.Get*`, `SendStringToExecute`, `Command*`, modal, `tr.Commit/Abort`.
- `none` = mở `tr` + luôn Abort + lỗi nếu `Changed ≠ 0`; timeout luôn fail + Abort (giữ quy tắc Revit đã verified).

## Top-5 risks
| Risk | L×I | Mitigation |
|---|---|---|
| Roslyn 5.9 trong ALC riêng vẫn bind Roslyn 4.10 / Immutable 8.0 của AutoCAD (default ALC) | M×H | ALC `Load` ưu tiên resolver cho mọi assembly trong deps.json; canary phase 1; phương án B: compile trong server gửi IL (Revit ADR-03 alt 1) |
| `Application.Idle` không subscribe được từ pipe thread / `ExecuteInApplicationContext` không gọi được từ thread ngoài | M×M | spike phase 1; fallback subscribe vĩnh viễn ở `Start()`; ADR-02 cập nhật theo kết quả |
| Script treo AutoCAD (prompt/vòng lặp không hợp tác) | M×M | guard deny `ed.Get*`/command; `ct` cooperative; `BusyGraceSeconds` → `-32002`; description nói rõ "cannot abort" |
| Document lock / command đang chạy / modal → `eLockViolation`, `-32002` liên tục | M×M | `IsQuiescent` gate + grace + thông điệp "press ESC"; `LockDocument` luôn; verify F phase 5 |
| Bundle không nạp (SECURELOAD/TRUSTEDPATHS) hoặc nạp nhầm vào Civil 3D/Advance Steel cùng R25.1 → tranh pipe | L×M | `Platform="AutoCAD"` MVP; NETLOAD tay + TRUSTEDPATHS docs; pipe `maxInstances=1` fail-fast; hai host Revit/AutoCAD khác tên pipe nên **không** xung đột nhau |

## Quyết định Claude tự chốt (đổi được)
| # | Vấn đề | Chốt | Lý do |
|---|---|---|---|
| 1 | Thêm globals `tr` và `units` ngoài danh sách user | Thêm | AutoCAD không đọc/ghi được nếu không cầm `Transaction`; `units` tránh lặp 12 lần trong seed (ADR-03) — **xem câu hỏi 1** |
| 2 | Library path | Revit giữ `tools-library`/`registry.db`; AutoCAD `tools-library-autocad`/`registry-autocad.db` (không `tools-library/<host>/`) | zero migration cho 22 tool Revit đã cài (ADR-04) |
| 3 | Seed embedded | `git mv` 21 seed Revit → `SeedLibrary/Revit/`, AutoCAD → `SeedLibrary/Autocad/` | đối xứng; checksum nội dung không đổi → không ghi đè library user |
| 4 | `IRevitExecutor` → `IBridgeExecutor`; `McpBridgeHost` + ViewModel → Core; `BridgeSettingsStore(productFolder)` | Làm ở phase 0 | rename cơ học, compile-checked, 159 test là gate |
| 5 | Field wire `revitVersion`/`RevitVersions`/cột `revit_version` | Giữ tên, thêm `Host`/`HostVersion(s)` additive | không phá bridge Revit đã deploy; dọn tên sau |
| 6 | `app` global | `DocumentCollection` (`Application.DocumentManager`) | `Application` là static class |
| 7 | Default usings | không `Autodesk.AutoCAD.Runtime` (CS0104 `Exception`), không `.Core` (`Application` trùng) | tránh lỗi compile hàng loạt |
| 8 | Bundle `Platform` | `"AutoCAD"` (không `*`) cho MVP | tránh Civil 3D/Advance Steel 2026 trên máy dev nạp cùng pipe — **xem câu hỏi 2** |
| 9 | `.mcp.json` | 2 entry cùng exe, khác env | theo sách: một codebase, cấu hình bằng env |
| 10 | Feature-folder convention | AutoCAD bridge không phải Nice3point add-in nhưng giữ `Model/ Service/ View/ ViewModel/` | đồng nhất repo |

## Cần người dùng quyết định (tối đa 3, kèm khuyến nghị)
1. **Globals `tr` + `units`** thêm vào contract `execute_autocad_code` (user liệt kê 8 globals, plan đề xuất 10). **Khuyến nghị: thêm** — không có `tr` thì không script AutoCAD nào chạy được.
2. **Bundle nạp vào Civil 3D 2026 / Advance Steel 2026** hay chỉ AutoCAD? **Khuyến nghị: chỉ AutoCAD (`Platform="AutoCAD"`)** trong MVP; mở `AutoCAD*` sau khi verify F.
3. **Runtime mục tiêu:** AutoCAD 2026 base (.NET 8, đang cài) hay cài Update 1.2 (.NET 10) trước khi implement? **Khuyến nghị: .NET 8 như đang cài**; .NET 10 là bước sau MVP (ADR-05 §4) — nếu bạn định cài Update 1.2 sớm, báo trước để phase 1 đổi TFM + `[25.1.1]`.
