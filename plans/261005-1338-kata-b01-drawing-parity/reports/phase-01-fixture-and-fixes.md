# Đợt 1 — fixture B01 1:25 + 3 lỗi (2026-10-05)

## Fixture
- `HPRebar/HPRebar.Core.Tests/KataRebar/Fixtures/b01-dwg.json` (166 KB): T2-DY7.dwg sau khi user vẽ lại B01 TL 1/25.
  Xuất bằng COM chỉ đọc (`scratchpad/export-b01-fixture.ps1`, AutoCAD không bị sửa).
- Mặt đứng: gốc (6732, −3990) = mép ngoài cột C, đỉnh dầm; 348 đối tượng. 14 mặt cắt: gốc = điểm chèn tiêu đề (tâm mặt cắt).
- Loader `KataB01DwgFixture` (đọc file theo `CallerFilePath`, không sửa csproj).
- Dim xoay: COM không có điểm chân dim → dùng `measurement`, `textAt`, khung bao.

## Đã sửa
| Lỗi | Sửa | Test |
|---|---|---|
| Đai mặt đứng lòi khỏi bê tông (console N19 −200) | `KataDrawingFrame.TopAt(x)`, nét đai từ đỉnh tại x | `KataB01ParityTests`, `KataB01DwgElevationTests` |
| C console sai thanh | `KataInnerStirrupLayout` dùng `TopMainOf(s)` | `KataB01ParityTests` |
| Nhãn đai đè dim | `Lift` = hàng nhãn đai − 387.5; chuỗi dim, cờ, đầu lưới dịch theo (B01: 525 / 600 / 725, khớp DWG) | 2 file trên |
| Chuỗi dim tại gối bề rộng 0 (I) | 2 vùng đai khác số hiệu → chia giữa khe (20600) | `KataB01DwgElevationTests` |

Kết quả: 1370/1370 test, golden DY7/DY14 xanh; build R26 OK.

## Khớp DWG ±2 mm (ngoài vùng tải từ Revit)
- Nét đai đầu/cuối mỗi vùng: x, đỉnh, đáy.
- Hàng nhãn đai 525, chuỗi dim 600, cờ 725; vị trí x mọi nhãn đai.
- Chuỗi dim vùng đai: số đo + vị trí.

## Lệch còn lại (việc đợt sau)
1. **Vị trí cắt (cờ)** — Kata đặt cờ tại chân leader của tag; khối tag ở khuỷu cách cờ ±200 (DY7 cũng vậy).
   Kata B01: 1250, 5450, 9950, 11850, 14300, 17025, 18750, 20250, 22650, 23950, 25600, 27950, 30600, 32267.
   HP: 1450, 5450, 9750, 11850, 14300, 17050, 18750, 21200, 23950, 25625, 27950, 30575, 31800, 32550.
   - Cách mặt gối (đầu/cuối nhịp): Kata D 850/850, F 650/675, H 650/350, J −/650, L 600/600, console 1 cờ L/3 = 667.
     HP: 0.1 L. Chưa ra quy tắc chung với DY7 (HP khớp DY7) → đợt 2.
   - Kata đặt cờ riêng cho H và J (gối bề rộng 0) và cho console chỉ 1 cờ.
2. **Số mặt cắt**: Kata 1…14 không dùng lại số; HP gộp mặt cắt trùng (số 5 dùng 2 lần).
3. **Số hiệu thanh**: HP 16/17/19/20/25… ≠ Kata 17/18/20/23… (đợt 5).
4. **Tải từ Revit** (dầm giao span D 5150…5550, cột cấy span F): DWG 1:25 dời vai bò/đai nút 50 mm so với bản 1:50 (4050·4200·5100‖5600·6500·6650). Kiểm khi có model.

## Giả thuyết cho đợt 2 (chưa xác minh)
Cờ và khối tag luôn cách nhau 200, một trong hai nằm đúng 0.1 L (làm tròn 50) từ mặt gối:
- D đầu: tag 1450 = 0.1 L, cờ 1250 = tag − 200; D cuối: tag 9750 = 0.1 L, cờ 9950.
- F đầu: cờ 11850 = 0.1 L, tag 12050 = cờ + 200.
→ Kata dời cờ hay dời tag tuỳ va chạm (nhãn đai 23 tại 1850?). Cần thêm DY7/DY14 để chốt.
