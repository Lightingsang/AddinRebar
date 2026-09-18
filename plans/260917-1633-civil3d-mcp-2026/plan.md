---
title: "HPCivil3d MCP 2026 (Civil 3D 2026 host for the shared HP MCP engine)"
description: "AI → MCP → Civil 3D 2026 (acad.exe /product C3D, R25.1, .NET 8) trong folder top-level riêng HPCivil3d/; bridge = copy bundle AutoCAD với Platform=\"Civil3D\", pipe hpcivil3d-mcp-2026, global civil, guard chặn rebuild/data-shortcut; engine McpShared/ chỉ thêm hằng/profile/DTO"
status: in-progress
priority: P2
effort: 38h
branch: RebarVersion1
tags: [civil3d, autocad, mcp, roslyn, named-pipe, registry, mcpshared, bundle]
created: 2026-09-17
revised: 2026-09-17
blockedBy: []
blocks: []
---

# HPCivil3d MCP 2026 — Plan

**Ngày:** 2026-09-17 16:33 · **Status:** in-progress — ADR-01 = A chốt 2026-09-17; phase 0 bắt đầu cùng ngày; mọi khẳng định API/runtime có nguồn ([evidence E1–E17](research/evidence-on-machine-2026-09-17.md) on-machine, [reflection addendum](research/reflection-addendum-verified-signatures.md) metadata-only, Autodesk docs) hoặc gắn `[chưa xác minh]` → spike phase 1 · ck CLI không có → file viết trực tiếp · Template: Stack-Aware (phase 0 = engine chung; WPF/ribbon gộp vào 2; server + seed gộp vào 3).

## Executive summary
- **Host thứ năm = AutoCAD vertical:** Civil 3D 2026 chạy trên **cùng `acad.exe`** (E1: `ACAD-9100:409` Civil / `9101` AutoCAD / `9126` Advance Steel, một thư mục cài, .NET 8 — E2) + Civil API `C3D\AeccDbMgd.dll` 13.8 (+ `ACA\AecBaseMgd.dll`, không NuGet — E3/E5). Bridge = **copy** loader/ALC/runtime/ribbon/harness AutoCAD với token Civil ([ADR-01 A](adr/adr-01-code-sharing-with-hpautocad-copy-vs-acadshared.md) + **MirrorTests** chống drift; B `AcadShared/` là **👤 câu hỏi duy nhất**).
- **Cách ly:** bundle riêng `HPCivil3d.McpBridge.bundle` với `Platform="Civil3D"` (Autodesk DevGuide: "loaded and only loaded in Civil 3D" — E15), `SeriesMin/Max R25.1`, pipe `hpcivil3d-mcp-2026`, method `civil3d.*`; AutoCAD 2026 + Civil 3D 2026 chạy song song = hai bundle/pipe/registry root ([ADR-02](adr/adr-02-bundle-platform-civil3d-pipe-isolation.md)). Fallback: demand-load `HKCU\…\ACAD-9100:409\Applications` (E14).
- **Globals/đơn vị/context:** AutoCAD globals + `civil` (`CivilDocument`); **mm** cho hình học phẳng qua `units` (dựng từ Civil `DrawingUnits` Meters/Feet, không INSUNITS), **đơn vị bản vẽ** cho station/elevation, mỗi envelope `drawingUnit`; `Civil3dInfo` 11 field ([ADR-03](adr/adr-03-globals-units-context.md)).
- **Transaction = AutoCAD nguyên** (outer/inner, dryRun `Abort()`); MVP guard chặn `Rebuild*`, data shortcuts, survey, `AeccUiMgd`/`AECC.Interop`, member nhận path — S-10 quyết mở `Rebuild` dưới `auto` ([ADR-04](adr/adr-04-transactions-rebuilds-guard-civil3d.md)).
- **12 seed** = 10 đọc + 2 ghi (`create_cogo_points`, `create_alignment_from_polyline`), 24 tool; `Parcel` **không** có `Area` trong .NET 2026 (E16) → known gap ([ADR-05](adr/adr-05-seed-library-mvp-12.md)). Định danh đầy đủ ở [ADR-06](adr/adr-06-server-profile-client-wiring-ribbon-identity.md).
- **Ràng buộc cứng:** `McpShared/` chỉ additive (bảng phase 0, 6 hàng), 4 host `tools/list` byte-identical; `HPCivil3d/` ⇏ `HPAutoCad/`; không đụng bundle MCP cũ của user (`Civil3dMcp.bundle`/`AutoCadMcp.bundle` `Platform="AutoCAD*"` đang nạp vào cả hai product — E4/E14); **Verified chỉ sau phase 4**.

## Design of record
[architecture.md](architecture.md) · ADR [01 👤](adr/adr-01-code-sharing-with-hpautocad-copy-vs-acadshared.md) · [02](adr/adr-02-bundle-platform-civil3d-pipe-isolation.md) · [03](adr/adr-03-globals-units-context.md) · [04](adr/adr-04-transactions-rebuilds-guard-civil3d.md) · [05](adr/adr-05-seed-library-mvp-12.md) · [06](adr/adr-06-server-profile-client-wiring-ribbon-identity.md) · Research: [evidence](research/evidence-on-machine-2026-09-17.md) · [addendum (sửa 8 điểm sai của researcher)](research/reflection-addendum-verified-signatures.md) · [researcher-01 reflection](research/researcher-01-civil3d-api-reflection.md) · [researcher-02 web facts](research/researcher-02-civil3d-autoloader-launch-rebuild-facts.md) (sửa: profile `<<C3D_Metric>>` gạch dưới; `RebuildAutomatic` có; `Renumber` trên `CogoPoint`; `StationOffset`; một pipe chung bị loại) · [researcher-03 đo HPAutoCad](research/researcher-03-hpautocad-mirror-shareable-core-and-tests.md) (cách đếm 1 021 vs 1 663 — ADR-01) · Kế thừa: [AutoCAD plan](../260913-0000-autocad-mcp-bridge-2026/plan.md), [Navis](../260915-0824-navisworks-mcp-2026/plan.md), [ETABS](../260916-2152-etabs-mcp-2026/plan.md).

## Phases
| # | File | Status | Depends | Effort |
|---|---|---|---|---|
| 0 | [phase-00](phase-00-mcpshared-additive-civil3d-contracts-and-gate.md) — 6 sửa engine additive (`Civil3dHost`/prefix, `Civil3dImports/Globals`, `Civil3dInfo` 11 field, `GuardProfile`/`AnalyzerProfile.Civil3d`) + ≈20 test; gate `tools/list` **4 host** byte-identical | **completed 2026-09-17** — built + tested + reviewed: Core.Tests 206 (=164+28+9+5), Net48 71, 5 suite host nguyên số; 4 host `tools/list` byte-identical ×2; review 8/10 → H1 `?.` bypass **engine (5 host)** + M2 + L3 fixed ([report](reports/phase-00-report.md) · [review](reports/code-review-phase-00.md) · [tests](reports/test-report-phase-00.md)) | — | 3h (≈3h) |
| 1 | [phase-01](phase-01-scaffold-bundle-alc-spike-civil3d-isolation.md) — scaffold `HPCivil3d/` (props dò `C3D\`, 2 project bridge copy, bundle `Civil3D`, server tạm, harness spike) + **spike gate** S-01…S-11, W1–W2 (nạp chỉ Civil, AutoCAD/ADVS không nạp, coexist, `civil`, đọc alignment, đơn vị, exception, indexer, parcel area, dryRun rollback, alignment từ polyline, rebuild/abort) | **completed 2026-09-18** — scaffold built (Debug + Release), spike 5 run trên Civil 3D 2026 thật: run 5 **0 FAIL** (PS 12/12 + spike 7/7 + disabled 2/2 + nodoc 2/2), run 1 S-02/S-02b/S-03, run 2 W1, run 3 W2, run 4 S-05…S-09; AutoCAD harness 21/21 với bundle Civil; ADR-02 Accepted, ADR-03/04/05 Accepted (revised) — `Rebuild*` vẫn deny MVP ([spike](reports/phase-01-spike.md)); review 7.5/10 → 9/13 fixed (bypass guard xoá, SECURELOAD *Load Once* pid-scoped), 4 → phase 2; tester 206+71+280 xanh, server smoke 12 tool; spike run 6/8 sau fix 0 FAIL; **SECURELOAD có** — 1 prompt/hash DLL không ký, ≤ 4 lần ([review](reports/code-review-phase-01.md) · [tests](reports/test-report-phase-01.md)) | 0 | 8h (≈7h) |
| 2 | [phase-02](phase-02-civil3d-bridge-runtime-ribbon-mirror-tests.md) — runtime đầy đủ (context 11 field, units Civil, serializer Civil, `Rebuild` deny giữ), cửa sổ, ribbon `HPCivil3d ▸ MCP ▸ MCP Bridge`, **MirrorTests**, harness pipe ≥ 30 + ribbon 12+1 | **completed 2026-09-18** — serializer Civil (AlignmentEntity/SubEntity, CogoPoint, style name), `Civil3dUnitTable` thuần, description `insunitsMismatch`, cửa sổ Civil 3D; `HPCivil3d.McpBridge.Tests` 47 (mirror 21 file sau token + strip `civil-only`, AutoCAD-side coverage, 5 pin sha256; mutation 1 ký tự → 1 fail); harness pipe **31/31 ×4** (20 AutoCAD + 9 Civil + ESC/retry), ribbon **12/12 + 1 MANUAL ×2** (icon 2 theme xem tay); `Stop-Acad` graceful; review 8/10 → 10/13 fixed; SECURELOAD 4 × Load Once mỗi start ([report](reports/phase-02-bridge-runtime.md)); review + tester → [review](reports/code-review-phase-02.md) · [tests](reports/test-report-phase-02.md) | 0, 1 | 8h (≈3h) |
| 3 | [phase-03](phase-03-server-profile-seeds-tests.md) — server hoàn chỉnh: profile, 4 core, prompts/resources, 12 seed nhúng, tests (structure/pipe mọi máy; compile-check skip quan sát được), `tools/list` 24, smoke ≥ 9, publish | **completed 2026-09-18** — prompts/resources `civil3d://`, description 1 784 chars, **12 seed** sinh bởi `tools/generate-seed-library.py` (mọi member compile-check trên API thật; cap trang theo byte đo), `HPCivil3d.Mcp.Server.Tests` 94 (13 skip quan sát được không có Civil), mirror contract phủ server tree (55), publish 7.47 MB, `tools/list` 24, smoke live **26/26 + 28/28 ×2** trên `Profile-5F` + `Corridor-1a`; review 7/10 → 14/14 xử lý ([report](reports/phase-03-server-seeds.md)); review + tester → [review](reports/code-review-phase-03.md) · [tests](reports/test-report-phase-03.md) | 0, 1 | 8h (≈3h) |
| 4 | [phase-04](phase-04-live-verify-harness-registry-loop-isolation.md) — harness ≈ 70 scenario ×3 (execute matrix, 12 seed trên tutorial drawings, registry loop MISS→approve→quarantine→restore, isolation **hai chiều**, hồi quy 4 host) | pending | 2, 3 | 8h |
| 5 | [phase-05](phase-05-publish-docs-claude-md-agents-md-mcp-json.md) — publish, CLAUDE.md (3 chỗ) + `AGENTS.md` regen bằng engine, docs ×4, README ×3, `.mcp.json` note 👤, memory | pending | 4 | 3h |

## Key dependencies / constraints
- Máy dev: Civil 3D 2026 + AutoCAD 2026 + Advance Steel 2026 cùng `C:\Program Files\Autodesk\AutoCAD 2026\` (E1); profile `<<C3D_Metric>>`/`<<C3D_Imperial>>` (E13); 163 tutorial drawing làm scene (E6). Bridge + compile-check **cần Civil 3D cài**; server + server tests build mọi máy.
- `HPCivil3d/` chỉ `ProjectReference ../McpShared/*`; không `HPAutoCad/` (A). Nếu 👤 chọn B → phase 1 tách 1a/1b (+12–16 h), CLAUDE.md sửa chiều phụ thuộc.
- Chỉ phase 1 và 4 (và smoke phase 2–3) được start/drive/close Civil 3D/AutoCAD — harness unattended, kills only what it started.

## `[chưa xác minh]` → spike phase 1 (addendum §12)
U1 `ActiveDocument` khi không phải Civil doc · U2 `Alignment.Create` siteName/labelSet `""` · U3 kiểu exception `FindElevationAtXY` ngoài biên · U4 `Rebuild()`/`AddVertices` dưới `Abort()` · U5 diện tích `Parcel` · U6 `Profile.Name`/`Feature` · U7 indexer `Item` · U8 `StyleBase.Name` get + label set root · U9 `CogoPoints.Add` trùng số · U10 field Pipe/Surface = drawing unit · U11 `GetCoordinateSystemByCode("")` · chiều "bundle `Civil3D` không nạp AutoCAD/ADVS" (S-02) · SECURELOAD trong Civil · `PolylineOptions` nhận loại polyline nào · `layerName ""`.

## Top risks
| Risk | L×I | Mitigation |
|---|---|---|
| Autoloader không lọc `Civil3D` đúng chiều | L×H | S-01/S-02 gate; fallback demand-load theo product key (ADR-02 §4) |
| `Corridor.Rebuild()` dưới abort làm hỏng/treo | M×H | MVP guard deny; S-10 chạy cuối phiên trên copy; mở chỉ khi sạch |
| Drift copy ↔ HPAutoCad | M×M | MirrorTests (A); follow-up B sau phase 4 |
| Plugin MCP cũ của user nạp vào Civil bật dialog → harness kẹt | M×M | log window lạ, README hướng dẫn disable; không đụng bundle user |
| Đơn vị lẫn (Imperial tutorial, INSUNITS ≠ Civil) | M×M | `units` từ Civil `DrawingUnits`, mismatch log, S-06 hai chiều |

## 👤 User Review (duy nhất) — **đã chốt 2026-09-17: A** (user "có" theo khuyến nghị)
**ADR-01:** đo được **1 663 dòng C#** identical/token-only giữa AutoCAD ↔ Civil (vượt ngưỡng ~1 500 ≈ 10 %; cách đếm "host-neutral" của researcher-03 = 1 021). **Khuyến nghị:** **A** (copy + MirrorTests) cho plan này, **B** (`AcadShared/`) làm follow-up có điều kiện sau phase 4 — lý do: B đặt refactor sản phẩm đã verified trước spike Civil và đổi quy tắc repo. Chọn B ngay = +12–16 h, phase 1 tách 1a/1b. Mọi quyết định khác Claude tự chốt theo ADR-02…06 (đổi được).
