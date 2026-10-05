# Kata B01 — canvas giống DWG, Revit theo quy định thép dầm

User decisions 2026-10-05 (grill-me): giống hoàn toàn (hình, thép, đai/móc, cờ, dim, tag, số hiệu); cờ cắt + số mặt cắt theo
Kata; số hiệu Kata → Revit **Rebar Number** (Schedule Mark để Revit tự sinh, không ghi Comments); nhận diện thanh Kata bằng
Extensible Storage; Revit từ chối số → giữ số Revit + cảnh báo; DWG B01 > 04_quy_dinh > QUY_TRINH khi lệch; không vỡ golden
DY7/DY14 (không tìm được quy tắc chung → hỏi); fixture JSON từ DWG + ảnh; lập lại file quy định thép dầm trong repo, thép Revit
tuân thủ file đó.

Constraints: DWG + KataB1 read-only; build Debug.R26 only; Revit test = bản sao; no commit without approval; no decompiled
code copied into repo.

| Đợt | Nội dung | Trạng thái |
|---|---|---|
| 0 | Lập lại quy định thép dầm: `docs/specs/kata-beam-rebar-rules.md` + bảng ô nhập B01 (hàng 1–28: giá trị, nghĩa theo 04_quy_dinh, bằng chứng DWG, HPRebar đúng/sai) `docs/specs/kata-dam-sheet-cells.md` | ✅ xong (agent) — 17 việc trong `reports/phase-00-rules-rebuild.md` |
| 1 | Fixture B01 từ DWG (`HPRebar.Core.Tests/KataRebar/Fixtures/b01-dwg.json`) + 3 lỗi: đai mặt đứng theo đỉnh nhịp, U/C theo thép chủ của nhịp, nhãn đai không đè dim | ✅ fixture 1:25 + 4 lỗi (1370 test) — `reports/phase-01-fixture-and-fixes.md` |
| 2 | Cờ cắt + số mặt cắt theo Kata (14 cờ B01) | 🟡 console L/3 ✅, 7/14 cờ khớp; user giữ 0.1 L — `reports/phase-02-03-cuts-and-sections.md` |
| 3 | Nội dung từng mặt cắt so fixture (thanh, cốt giá, U/C, móc C cốt giá) | ✅ 14/14 khớp DWG (1399 test) |
| 4 | Bố cục dim/tag mặt đứng + mặt cắt so fixture | 🟡 tag thanh ✅; U/C, đai ngoài, 2 lớp gộp chưa — `reports/phase-04-05-tags-and-numbers.md` |
| 5 | Đánh số thanh theo Kata (1…35 B01) | ✅ 1…35 khớp (1428 test) |
| 6 | Revit: Rebar Number, Extensible Storage, bỏ Comments/Schedule Mark; live Revit test | ✅ live Revit test (2 lần) — `reports/phase-06-revit-numbering.md`; review 6.5/10 → High + 4 Medium sửa, live lại 2 lần (`reports/code-review-b01-parity.md` ▸ Fix round) |

Reports: `reports/`.
