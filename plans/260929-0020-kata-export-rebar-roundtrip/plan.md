---
title: "Kata Export round trip — sheet Dam thép → canvas → Revit, nhiều nhịp"
description: "Gộp đọc thép + canvas mặt đứng/mặt cắt + tạo thép vào cửa sổ Kata Export; nhiều nhịp nhiều dầm; cốt giá, đai U/C, hàng 22; bảng thiết lập Kata; bản cài KATA_ONLY."
status: completed
priority: P1
branch: RebarVersion1
tags: [revit, hprebar, kata, rebar, beam]
created: 2026-09-29
related: [260928-1259-kata-rebar-mvp, 260926-2317-kata-export-hprebar]
---

# Kata Export round trip

Hợp đồng: /grill-me 2026-09-28/29 (user xác nhận "implement"). Nền: commit 22c192d + WIP Antigravity (sao lưu `scratchpad/antigravity-wip-0018/`), build xanh 745/745.

## Quyết định (user)
- Luồng: Kata Export chọn dầm → ghi sheet Dam → user nhập thép → (1) Kata tự vẽ CAD, ngoài phạm vi; (2) Đọc thép → canvas → Tạo thép Revit. Giữ cả cửa sổ Kata Rebar.
- Nhiều nhịp + nhiều dầm Revit. Gối giữa 2 vế khác: so le Y; vế diện tích lớn qua gối bẻ móc mép xa; vế nhỏ thẳng qua gối + G2·d vào nhịp kia. Thép chủ liên tục, cảnh báo > chiều dài tối đa (11700).
- Canvas: mặt đứng + mặt cắt 2D tại mỗi gối và giữa mỗi nhịp.
- Hình học lệch > 50 / khác số gối → chặn; B3 khác tên → cảnh báo.
- Bảng thiết lập HPRebar (JSON `%AppData%\HPRebar`), mặc định = hộp "Detail thép" Kata: neo cốt giá 10d, móc đai □ 135° 7.5d, đai C 180° 7.5d, móc C ngang a400, max 11700.
- Làm: cốt giá G4/G5 + hàng 20, đai trong U/C hàng 25–44 (cặp cột k → nhịp k), hàng 22. Bỏ qua (báo ô): giật cấp 19/21, console G9, hàng 23/24, J7. Không nối chồng.
- Bản cài: mặc định trong tab HPRebar; `KATA_ONLY` qua script build.

## Phases
| # | Phase | Status |
|---|---|---|
| 1 | Service dùng chung + sửa luồng API + canvas = plan đo Revit | done |
| 2 | Nhiều nhịp: neo gối giữa, 1 dầm Revit nhiều nhịp | done |
| 3 | Bảng thiết lập: gọn, nối vào rule, JSON tự viết (net48) | done |
| 4 | Cốt giá G4/G5/hàng 20 + móc C ngang | done |
| 5 | Đai trong □/U/C hàng 25–44 + hàng 22 | done |
| 6 | Canvas mặt đứng + mặt cắt | done (xem live) |
| 7 | KATA_ONLY + [review](reports/code-review.md) + [live verify 2 nhịp](reports/phase-07-live-verify.md) | done — 774/774, R26/R25/R24 + KataOnly build; live Revit 2026 khớp, sửa hướng móc đầu thanh |

Mỗi phase: build R26/R25/R24 + test Core; phase 7 live Revit 2026 model nháp (3 cột, 2 dầm, 2 nhịp).

## Rủi ro
- Hình đai U/C suy từ công thức biểu đồ sheet (I61:R80) — đối chiếu bằng Excel COM trước khi code phase 5.
- Thanh qua nhiều dầm: host = dầm chứa trung điểm; Revit có thể báo "outside host" → đo ở phase 2/7.
- Không đụng DLL Kata; Excel/model chỉ bản nháp.
