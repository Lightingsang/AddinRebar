# Phase 04 — Thép gia cường gối (hàng 13–16)

Status: done 2026-09-28 — 715/715 test; live Revit 2026 (model nháp, DT1: C13 2f20, C14 2f18, E14 2f18): 6 thanh gia cường + 7 thép chủ + 3 bộ đai, số đo khớp `KataSupportTopBarTests` từng mm (lớp 1 y ±53.5 z −43 x 43→1900 chân 443; lớp 2 y ±108 z −87 x 87→1400 / 5400→6713 chân 407; thép dưới lùi tới x 131 chân 331). Quyết định user 2026-09-28 (sau MVP, commit 2a8e657).

## Rule (bổ sung rule-table)

| ID | Rule |
|---|---|
| T1 | Hàng 13 = lớp 1 (cùng cao độ thép chủ B11), 14 = lớp 2, 15 = lớp 3, 16 = lớp 4 (Kata ghi "lớp 3" hai lần; đặt 16 dưới 15 để không chồng) |
| T2 | Ô `trái;phải`: vế trái vươn vào nhịp trái, vế phải vào nhịp phải; không có `;` = hai bên như nhau; `0`/trống = không có thanh bên đó. Gối biên: dùng vế phía nhịp |
| T3 | Chiều dài vươn: lớp 1 = H5 × L từ gốc I5; lớp 2+ = H3 × L từ gốc I3. Gốc chứa "tâm" → tâm gối, còn lại → mép gối. L = nhịp thông thủy kề; gối giữa = max(L trái, L phải) |
| T4 | Gối biên: neo như thép chủ trên (G2·d từ mép trong; thiếu → tới mép xa − a, bẻ 90° xuống, chân ≥ 10d, không quá tới lớp thép dưới) |
| T5 | Lớp 1: thép chủ giữ vị trí; thanh gia cường chia vào các khe giữa thép chủ (mỗi khe chia đều), thừa ưu tiên khe giữa. Khe thông thủy < max(25, d) → cảnh báo |
| T6 | Lớp 2+: rải đều hết bề rộng (thanh biên sát đai); tâm lớp sau = tâm lớp trên − (d_trên/2 + max(25, d_max) + d/2) |
| T7 | Chân bẻ lớp n ≥ 2 lùi vào trong so với chân lớp trên một khoảng bằng khoảng cách tâm hai lớp; chân thép dưới lùi (A5) tính từ chân trong cùng của thép trên |
| T8 | Gối giữa, hai vế khác nhau: mỗi vế là thanh riêng, từ điểm cắt tới mép xa gối − a (chỉ unit test; nhiều nhịp vẫn bị chặn) |

## Việc
- Core: `KataSupportTopBarLayout` thay phần top của `KataAdditionalBarLayout`; parser hàng 13–16 giữ chuỗi `trái;phải`; scope filter thôi bỏ qua 13–16; planner/handler lấy đường kính gia cường.
- Revit: tạo `ExtraTopBars` như thép chủ (curves), mark `3.{gối}.{lớp}`; bảng kiểu thép có dòng gia cường.
- Test + live: sheet nháp C13 `2f20`, C14 `2f18`, E14 `2f18;0`… trên dầm DT1.
