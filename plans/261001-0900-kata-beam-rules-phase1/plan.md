---
title: "Kata beam rules — Giai đoạn 1: luật số theo spec + cắt lệch G1"
description: "Chuẩn hoá luật bố trí thép dầm theo spec 04_quy_dinh_thep_dam.md: cắt lệch G1, đai dày max(2h, 0.25Ln), cắt bụng 0.15Ln, chân móc 15d + làm tròn 25, khe lớp max(d,30), cảnh báo cốt giá h≥700; số trong KataSettings, ô sheet ghi đè."
status: completed
priority: P1
branch: RebarVersion1
tags: [revit, hprebar, kata, rebar, beam, rules]
created: 2026-10-01
related: [260929-0020-kata-export-rebar-roundtrip]
---

# Kata beam rules — Giai đoạn 1

Hợp đồng: /grill-me 2026-10-01 (user xác nhận "có"). Nguồn: `Q:\My Drive\03_ChuongTrinh\kata_structural_spec\kata_structural_spec\04_quy_dinh_thep_dam.md` (chưa đối chiếu được Kata thật: chuỗi DLL bị mã hoá).

## Quyết định (user)
- Theo spec, mọi số vào `KataSettings` (mặc định = spec); ô sheet có giá trị thì thắng (G1, H3/H5, I3/I5, G2/G3, G7/G8, J9).
- Cắt lệch G1 cho mọi lớp gia cường trên: lớp ngoài ≥ lớp liền trong + G1; kéo dài lớp ngoài; chặn tại mặt gối đối diện + cảnh báo.
- Đai dày = max(2h, 0.25·Ln) (≤ Ln/2). Cắt bụng 0.15·Ln (làm tròn xuống 50). Chân móc ≥ 15d, làm tròn lên 25 khi vừa. Khe lớp ≥ max(d, 30).
- Cốt giá: h ≥ 700 mà không có G5/hàng 20 → cảnh báo, không tự sinh.
- G1: số dương → dùng; trống → thiết lập; chuỗi khác (`-300;11700`) → thiết lập + cảnh báo.
- Export không ghi G1–J9. Giai đoạn sau (plan riêng, theo thứ tự): giật cấp WC → đai treo/vai bò.
- 2026-10-01 (user): bỏ chia nối 11.7 m — mô hình thiết kế vẽ thép chủ liền, phần nối tính trong Rebar Schedule, chia cây để shopdrawing; bỏ cảnh báo và thiết lập `MaxBarLength`.

## Phases
| # | Việc | Status |
|---|---|---|
| 1 | Settings + rules + parser G1 | done (Core) |
| 2 | Layout: cắt lệch, đai dày, cắt bụng, chân móc, khe lớp, cảnh báo cốt giá | done (Core) |
| 3 | Hộp thiết lập (Revit) | done (XAML/VM khởi tạo bởi Antigravity, Claude review + sửa kiểm hợp lệ) |
| 4 | Test + golden (816/816), build R26/R25/R24 0 lỗi, code review | done ([report](reports/code-review-phase1.md); C1 = file phiên khác, chờ user) |
| 5 | Tài liệu [kata-beam-rebar-rules.md](../../docs/specs/kata-beam-rebar-rules.md) | done |
| 6 | Live Revit 2026 (3 nhịp B_200x600, bản chép model THCPHCS2, workbook nháp) | done ([report](reports/phase-06-live-verify.md)) |
