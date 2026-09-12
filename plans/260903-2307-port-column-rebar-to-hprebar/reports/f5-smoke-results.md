# F5 Smoke Results — Column Rebar

Trạng thái: **CHƯA CHẠY.** Không có kết quả nào trong file này là đã verify.

Cập nhật lần cuối: 2026-09-04

## Vì sao chưa chạy

Cần một trong hai, chưa có cái nào:

1. **Chạy tay trong Revit** — Revit 2026 đang mở suốt phiên cook (PID 1836) và khoá DLL đã deploy, nên mọi build phải thêm `-p:DeployAddin=false`. Không tự khởi động Revit thứ hai để tránh phá session đang mở.
2. **Model mẫu** — `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` chưa tồn tại, nên 16 test TUnit skip hết. Spec dựng model: `HPRebar/HPRebar.Tests/Fixtures/README.md`.

## Cách chạy

```bash
# 1. Đóng Revit hoàn toàn
# 2. Build + deploy
cd HPRebar
dotnet build HPRebar.slnx -c Debug.R26

# 3. Mở Revit 2026 → tab HPRebar → panel Rebar → nút Column Rebar
```

Log runtime ghi ở `%LocalAppData%\HPRebar\logs\hprebar-<ngày>.log` — đọc file này trước khi báo lỗi, mọi warning "đã bỏ qua" đều nằm trong đó.

## Ma trận cần chạy — Revit 2026

| # | Case | Kỳ vọng | Kết quả |
|---|---|---|---|
| 1 | 1 cột chữ nhật, không dầm đỉnh | thép + đai đều; dowel dùng `BendDepth = max(b,h)`; 2 mặt đứng + 1 mặt cắt | ⬜ chưa chạy |
| 2 | 2 cột chữ nhật thu tiết diện + dầm đỉnh cột dưới | thanh bẻ xiên dưới đáy dầm; neo so le 35d/70d; dim bắt đúng mặt dầm | ⬜ chưa chạy |
| 3 | 1 cột tròn `nd=8` | thép vòng tròn; đai `M_T3`; dowel phân bố theo góc phần tư | ⬜ chưa chạy |
| 4 | Bấm Cancel | model không đổi, không view rác | ⬜ chưa chạy |
| 5 | Model thiếu family `M_T1` | dialog rõ ràng **trước** transaction đầu tiên, không exception | ⬜ chưa chạy |
| 6 | Pick cột nghiêng | báo **code 2** (không phải code cuối) — chứng minh B1+B2 đã sửa | ⬜ chưa chạy |
| 7 | Pick 2 cột không liên tục | báo **code 4** | ⬜ chưa chạy |
| 8 | Đổi ngôn ngữ sang VN | nhãn 8 tab + message lỗi tiếng Việt, dialog không đóng | ⬜ chưa chạy |
| 9 | Revit đặt theme Light | dialog + canvas theo Light | ⬜ chưa chạy |
| 10 | Undo 1 lần sau OK | xoá sạch cả view lẫn thép | ⬜ chưa chạy |

Chạy lại case 1–3 trên **Revit 2025** (cùng TFM `net8.0-windows7.0`, rẻ) để có điểm dữ liệu thứ hai.

## Điểm cần soi kỹ nhất

Xếp theo mức độ *chưa từng chạy lần nào* nhân với *hậu quả nếu sai*:

1. **`DimensionCreator.ToLinearReference`** — hack đổi token `SURFACE` → `LINEAR` trong stable representation. Undocumented, chưa chạy trên bất kỳ version nào. Nếu vỡ: dimension mặt cắt bị bỏ qua **im lặng** (có log warning). Xem case 1, kiểm tra log.
2. **`Rebar.CreateFreeForm`** đường R26 — dùng overload `RebarStyle` mới, trả `RebarFreeFormCreationResult`. Đường R23–R25 (`out` param) và đường R26+ là 2 nhánh `#if` khác nhau; chỉ nhánh R26 có thể chạy trên máy này.
3. **Hướng rải đai** — `SetLayoutAsNumberWithSpacing(count, spacing, true, true, true)`. Nếu 3 cờ bool sai thì đai rải ngược trục Z. Case 1 nhìn là thấy ngay.
4. **`PointMapper`** — gốc toạ độ = góc Tây-Nam cột đáy chiếu lên mặt datum. Sai thì toàn bộ thép lệch vị trí (không phải lệch nhẹ). Case 1.
5. **`ColumnSpecEditor.Load` thứ tự gán** — đã sửa một NRE ở đây trong Phase 6; đáng xác nhận dialog mở được lần đầu.

## Verify build (đã chạy)

| Config | Debug | Release |
|---|---|---|
| R23 (net48) | ✅ 0 error | ✅ 0 error |
| R24 (net48) | ✅ 0 error | ✅ 0 error |
| R25 (net8.0-windows7.0) | ✅ 0 error | ✅ 0 error |
| R26 (net8.0-windows7.0) | ✅ 0 error | ✅ 0 error |
| R27 (net10.0-windows7.0) | ✅ 0 error | ✅ 0 error |

0 warning `CS` ở mọi config. Warning còn lại đều là `EXEC` của ILRepack, có từ baseline trước khi feature này bắt đầu.

`dotnet test HPRebar.Core.Tests` — **99/99 pass**, không cần Revit.

`cd build && dotnet run` (ModularPipelines) **chưa chạy** — nó build Release có deploy, mà `DeployAddin=true` hardcode trong csproj nên sẽ vấp file lock khi Revit mở. Chạy sau khi đóng Revit.

## Giới hạn phải nêu trong mọi báo cáo

R23 / R24 / R27 **build-only, chưa verify runtime** — máy dev chỉ cài Revit 2025 + 2026. Rủi ro thật còn lại ở đó: `FormattedText` trên net48, Polyfill `MinBy` trên net48, .NET 10 runtime của R27. **Không tuyên bố "đã hỗ trợ R23–R27".**
