# Đợt 4 + 5 — tag mặt cắt, số hiệu (2026-10-05)

## Fixture
- Exporter gán leader theo điểm chạm thanh (|x − tâm| ≤ 350), tag theo điểm cuối leader → tag phía phải (vượt cửa sổ
  ±800) nay có trong fixture; leader có toạ độ.

## Đợt 4 — tag
- ✅ Tag thanh (`nØd`) 14 mặt cắt đúng vị trí ±2 (`KataB01DwgSectionTests.Every_bar_tag_*`), trừ:
  - tag lớp trong phía trên: Kata b/2 + 400 ở B01 (cả b 300 và 500), DY7 b/2 + 370 cùng b 300 → chưa ra quy tắc;
  - tag cốt giá 2 lớp: lệch 7 (Kata 574.5, HP 581.5); chân leader Kata 87.7 / −100.7, HP 59.3 / −65.3.
- ✅ Tag cốt giá 1 lớp: `b/2 + max(275, 370 − 0.95(h − 500))` khớp DY7 (520), DY14 (425), B01 h900 (425) — trước: 140.
- ❌ Chưa sửa (bố cục Kata theo va chạm, chưa suy ra):
  - tag U/C: Kata đặt bên phải x = b/2 + 400 (14-14: +350), 1 dòng "Ø10a500" / "2xØ10a500", leader ngang từ nhánh; C = U − 125;
    cao độ U: −820.8 (h1100), −300 / −250 (h700); 2-2: C lên trên (62.5) vì tag 17 chiếm chỗ.
  - tag đai ngoài: lệch 2–5 (Kata −467.5 / −365, HP −463 / −363).
  - tag 2 lớp gộp (3-3: "9+10 6Ø20+6Ø20" tại 881.9, −192.5): HP 2 tag riêng.

## Đợt 5 — số hiệu: 1…35 khớp Kata (14 mặt cắt + mặt đứng)
- Thứ tự theo nhịp: C cốt giá → đai ngoài → U/C (DY7 vẫn 14 → 15/16/17).
- Đai C: một số cho mỗi Ø + bề rộng nhịp (B01 22 ở b500, 33 ở b300; DY7 14 chung).
- I17 (gối bề rộng 0): đánh cùng thép gối theo vị trí (12), không dùng chung số với thép nhịp
  (`KataSpanRebarSpec.BottomExtraJointAtMm`).
- U/C nhịp gộp tách tại bước đỉnh (H: 30/31, J: 27/28 như F).
- C kê lớp 2: chỉ lớp đúng 3 thanh (bỏ 1 thanh C kê sát gối M nhịp L — Kata không có số cho nó). **Thép 3D đổi.**
- Test: `KataB01DwgNumberTests` (15). Tổng 1428/1428, golden DY7/DY14 xanh, build R26 OK.
