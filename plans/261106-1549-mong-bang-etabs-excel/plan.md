---
title: "HPFoundation: tự động hoá móng băng Excel ↔ ETABS"
status: pending
created: 2026-11-06
blockedBy: []
blocks: []
---

# HPFoundation — móng băng Excel ↔ ETABS

Chưa code. Yêu cầu chốt tại [phase-03-workflow-as-is.md](reports/phase-03-workflow-as-is.md); nguồn: [Excel](reports/phase-01-excel-map.md), [ETABS](reports/phase-02-etabs-map.md).

## Kiến trúc

App desktop riêng, thư mục mới `HPFoundation/`. Excel vẫn là bộ máy tính, tool chỉ điều phối trên **bản sao**.

| Project | TFM | Vai trò |
|---|---|---|
| `HPFoundation.Core` | net8.0 | Thuần: record, profile sheet, quy tắc lấy nội lực, đề xuất và kiểm tra thép, bảng vật liệu. Không COM |
| `HPFoundation.Core.Tests` | net8.0 | xUnit v3 |
| `HPFoundation.App` | net8.0-windows | WPF MaterialDesign, adapter COM Excel và ETABS |

Không tham chiếu `McpShared` hay thư mục MCP nào. Chỉ dùng lại mẫu `EtabsAssemblyResolver` và `ETABSv1` `Private=false`.

## Luồng một lần chạy

```text
Chọn .xls + sheet
→ Băm SHA-256 file gốc, chép vào %LocalAppData%\HPFoundation\runs\<sheet>-<ts>\
→ Excel riêng (ẩn) mở bản sao → áp F34, D39 quy ước ETABS → tính lại
→ Đọc FoundationInput (có F128, F129 sau tính lại)
→ ETABS riêng → tạo EDB mới → chạy → đọc M3, V2 (Ton_m_C)
→ Áp quy tắc lấy M, Q → ghi C136:C146, A172:A173 vào bản sao
→ Đề xuất thép + cảnh báo → kỹ sư duyệt → ghi ô thép
→ Báo cáo đối chiếu + run.json → kỹ sư chủ động xuất file
```

File gốc và ETABS, Excel của người dùng **không bao giờ** bị đụng.

## Các phase

| # | Phase | Loại | Phụ thuộc | Mục tiêu người dùng |
|---|---|---|---|---|
| 1 | [Đọc Excel trên bản sao](phase-01-excel-input.md) | Xong | | 1 |
| 2 | [Dựng và chạy ETABS](phase-02-etabs-build-run.md) | HITL | 1 | 2, 3 |
| 3 | [Nội lực về Excel](phase-03-results-to-excel.md) | AFK | 2 | 4, 6 |
| 4 | [Đề xuất và kiểm tra thép](phase-04-rebar.md) | HITL | 3 | 5 |
| 5 | [App WPF](phase-05-wpf-app.md) | HITL | 3, 4 | tất cả |
| 6 | [Mốc chuẩn, 8 sheet 4 cột, phát hành](phase-06-baseline-release.md) | HITL | 5 | 6 |

## Quyết định

| Quyết định | Chọn | Lý do |
|---|---|---|
| Vị trí | Thư mục mới, ADR-0015 | Dùng cả Excel và ETABS; các thư mục MCP không được tham chiếu nhau |
| Không làm seed MCP | App riêng | Kết quả lặp lại được, không cần phiên AI, không dính lỗi tier Excel |
| Bộ máy tính | Excel trên bản sao | Không viết lại 387 công thức, giữ đúng sheet như đã chốt |
| Tìm ô | Theo nhãn neo, không theo địa chỉ cứng | `MB10- 4COT` dài 174 hàng, khác 176 |
| Tiến trình | Instance Excel và ETABS riêng | `NewBlank` trên ETABS đang mở sẽ xoá model của người dùng |
| Excel COM | Late-bound | Ép kiểu interface lỗi `TYPE_E_ELEMENTNOTFOUND` trên máy này |
| Vật liệu | `materials.json` chép từ sheet `Data`, test ghim giá trị | Không phụ thuộc ổ `Y:` lúc chạy |

## Điểm mở

| # | Nội dung | Chặn |
|---|---|---|
| 2 | Ảnh `N145` có dữ liệu cần dùng, người dùng gửi sau | Phase 3 |
| 4 | Quy tắc chọn thép, ngưỡng μ, αR | Phase 4 |
| 5 | Chạy instance ETABS thứ hai có cần license riêng không | Phase 2 |

## Ngoài phạm vi

Sheet 2, 3 cột. Sửa `D64`, `C71`. Sửa lỗi tier Excel bridge (plan riêng). Xuất bản vẽ.
