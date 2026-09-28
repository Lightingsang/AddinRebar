---
title: "Kata Rebar MVP — sheet Dam → thép dầm 1 nhịp trong Revit 2026"
description: "Sửa lõi KataRebar (commit ac6c2d4) theo bảng rule đã duyệt: B11/B12 + đai G7/G8 + cover J9, 1 dầm giữa 2 cột; verify live Revit 2026 trên model nháp."
status: completed
priority: P2
branch: RebarVersion1
tags: [revit, hprebar, kata, rebar, beam]
created: 2026-09-28
related: [260926-2317-kata-export-hprebar]
---

# Kata Rebar MVP

Hợp đồng: grill-me 2026-09-28 (user duyệt plan `~/.claude/plans/tiep-tuc-effervescent-key.md`). Rule + mapping ô: [reports/rule-table.md](reports/rule-table.md).

## Quyết định (user)
- Rule set riêng, tham số từ sheet (không cố khớp bản vẽ Kata CAD); không decompile DLL Kata.
- Giữ khung KataRebar, sửa lõi; Excel chỉ qua COM (bỏ reader ClosedXML; PackageReference giữ tới khi `HPRebar.csproj` sạch).
- J9 số 1 = mép → tâm thép chủ; neo G2/G3, bẻ khi thiếu; A5/S4/C2 theo mặc định trong rule table.
- Không sửa `HPRebar.csproj`, `Application.cs`, `install/Installer.cs` (thay đổi treo của phiên KATA_ONLY).

## Phases
| # | Phase | Status |
|---|---|---|
| 1 | [Core: parser + rules + layout tách + scope/geometry/planner](phase-01-core.md) | done — 707/707 test |
| 2 | [Revit: matcher (KataExport readers), tạo thép, transaction, UI](phase-02-revit.md) | done — build R26/R25/R24 |
| 3 | [Review + live verify Revit 2026](phase-03-live-verify.md) | done — [review](reports/code-review-mvp.md) H1/M1–M5 sửa; [live](reports/phase-live-verify.md) 4 lần chạy + 3 case âm khớp golden |

## Phase sau (mỗi phase verify live riêng)
1. Gia cường gối hàng 13–16 (H3/H5, I3/I5; nghĩa `;`). 2. Gia cường nhịp 17–18. 3. Nhiều nhịp + neo/nối qua gối giữa.
4. Giật cấp hàng 19/21. 5. Cốt giá G4/G5 + hàng 20. 6. Đai U/C hàng 25–44 + bước hàng 22/23 + hàng 24.
7. Console G9. 8. Dầm giao / vách. 9. Group / tag / view. 10. Kata Rebar vào bản KATA_ONLY.

## Rủi ro
- J9 một số → lớp bảo vệ đai có thể 12 mm (cảnh báo).
- `ScaleToBox` / hướng rải bộ đai: tự kiểm `GetBarPositionTransform`, sai hướng → rải lại.
- Cảnh báo Revit "outside host" giờ được log + đếm, không còn nuốt im lặng.
