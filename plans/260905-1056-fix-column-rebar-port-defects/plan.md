---
title: "Sửa 4 khiếm khuyết port Column Rebar (G4, G6, G3, A2)"
status: implemented
created: 2026-09-05
scope: feature
mode: hard
source: "plans/260903-2307-port-column-rebar-to-hprebar/plan.md (D1–D8 còn hiệu lực)"
target: "HPRebar/HPRebar/Column Rebar/ + HPRebar/HPRebar.Core/ColumnRebar/"
blockedBy: ["HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt"]
---

# Sửa khiếm khuyết port Column Rebar

4 mục đã xác minh trên code hiện tại (2026-09-05). Không đổi D1–D8. Không đụng DetailItem / Detail Shop.

## Kết quả xác minh

| # | Khiếm khuyết | Bằng chứng | Tái hiện |
|---|---|---|---|
| G4 | `ScaleToBox` nhận vector đơn vị làm cạnh hộp | [StirrupGeometry.cs:117](../../HPRebar/HPRebar/Column%20Rebar/StirrupGeometry.cs#L117) `east` · `:140` `north` · `:183` `XYZ.BasisY` · `:204` `XYZ.BasisX`; tiêu thụ [AdditionalTieCreator.cs:231](../../HPRebar/HPRebar/Column%20Rebar/AdditionalTieCreator.cs#L231) | ✅ đúng nguyên văn |
| G6 | `BuildCurves` bỏ điểm giữa | [MainBarCreator.cs:89-95](../../HPRebar/HPRebar/Column%20Rebar/MainBarCreator.cs#L89-L95) | ⚠️ **tái hiện một phần — kịch bản trong đề bài KHÔNG tái hiện**, xem [phase-01](phase-01-core-geometry-math-and-xunit.md) §Key Insights |
| G3 | Cột xoay lọt validation, view + bảng tag lệch | [ColumnStackValidator.cs:95](../../HPRebar/HPRebar/Column%20Rebar/ColumnStackValidator.cs#L95) · [ColumnSolidFaceReader.cs:231-233](../../HPRebar/HPRebar/Column%20Rebar/ColumnSolidFaceReader.cs#L231-L233) · [SectionViewCreator.cs:80](../../HPRebar/HPRebar/Column%20Rebar/SectionViewCreator.cs#L80) · [RebarTableTagCreator.cs:161](../../HPRebar/HPRebar/Column%20Rebar/RebarTableTagCreator.cs#L161) | ✅ cả 4 điểm |
| A2 | Không có `FailureHandlingOptions` | `grep -rn "FailureHandling" "HPRebar/HPRebar/Column Rebar"` → 0 hit; 5 `Transaction` ở orchestrator + 2 ở service | ✅ |

## Phases

| # | Phase | File | Trạng thái | Verify |
|---|---|---|---|---|
| 1 | Toán G6 xuống Core + xUnit | [phase-01](phase-01-core-geometry-math-and-xunit.md) | ✅ xong | ✅ 112/112 xUnit |
| 2 | Nối Core vào `MainBarCreator`; G4 ở `AdditionalTieCreator` | [phase-02](phase-02-revit-geometry-wiring.md) | ✅ xong | ⚠️ build 6/6 config — TUnit BLOCKED |
| 3 | G3 cột xoay — view + bảng tag bám trục cột | [phase-03](phase-03-rotated-column-orientation.md) | ✅ xong | ⚠️ build — TUnit BLOCKED |
| 4 | A2 `FailureHandlingOptions` | [phase-04](phase-04-transaction-failure-handling.md) | ✅ xong | ⚠️ build — TUnit BLOCKED |

## Đã thực thi — sai khác so với kế hoạch

Ba điều chỉnh, đều quyết sau khi đọc hợp đồng API, không đổi phạm vi. Lý lẽ đầy đủ ở phase file.

1. **Bỏ `CrossTieBoxCalculator` khỏi Core** — span (`H-2c`, `B-2c`, `D-2c`) ở `StirrupGeometry` vốn đã
   đúng; lỗi G4 chỉ ở cạnh hộp thứ hai. Đưa xuống Core không sửa gì, chỉ thêm gián tiếp. KISS/YAGNI.
2. **G4 sửa bằng đo shape, không bằng công thức tiết diện** — `ScaleToBox` khi bị overconstrain sẽ
   "scale toàn shape đến khi **một trong hai** cạnh đúng", không nói cạnh nào. Đặt cạnh thứ hai đúng bằng
   chiều cao shape ở tham số mặc định → span là thứ duy nhất còn phải scale. Chi tiết: [phase-02](phase-02-revit-geometry-wiring.md).
3. **G3 chọn hướng B, KHÔNG thêm rule validator** — rule 6 đã chặn stack xoay lệch nhau; `SideFacing` đã
   chặn 45°; và với cột không xoay transform mới ra **y hệt** cũ nên rủi ro hồi quy bằng 0. Điều này lật
   khuyến nghị "chọn A" của bản kế hoạch. Chi tiết: [phase-03](phase-03-rotated-column-orientation.md).

## Build gate (mọi phase)

```bash
cd HPRebar
dotnet build HPRebar.slnx -c Debug.R26                          # chính
dotnet build HPRebar.slnx -c Debug.R23                          # net48, bắt lỗi TFM sớm
dotnet test HPRebar.Core.Tests                                  # Phase 1 bắt buộc; phase khác chạy để chống hồi quy
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26    # theo project path — .slnx đặt Build Project="false"
```

Revit đang mở sẽ khoá DLL → thêm `-p:DeployAddin=false` khi chỉ cần kiểm tra biên dịch.

## Ma trận verify

R25 + R26 có trên máy dev (R26 là môi trường F5 + TUnit chính). R23, R24, R27 build-only.

## Rủi ro còn lại

1. **Fixture `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` chưa tồn tại** → toàn bộ TUnit skip. G4, G3, A2 chưa được chứng minh bằng test chạy thật, chỉ bằng build + đọc hợp đồng API. Chỉ G6 xác minh trọn vẹn (xUnit).
2. **G4 chưa chạy trong Revit lần nào.** Suy luận dựa trên tài liệu `ScaleToBox` là vững, nhưng `GetCenterlineCurves` ngay sau `CreateFromRebarShape` cần một lần F5 xác nhận trả về hình học ở tham số mặc định như tài liệu mô tả.
3. **G3 chưa thấy bằng mắt.** Với cột không xoay bản sửa là identity nên an toàn; với cột xoay cần F5 kiểm mặt cắt và bảng tag.
4. **A2 nuốt warning** — nếu `DeleteWarning` bị gọi nhầm cho mục severity Error thì model hỏng im lặng. Đã lọc theo `FailureSeverity.Warning`, nhưng cần F5 xác nhận error vẫn rollback được cả group.

## Tham chiếu

- Plan port gốc (D1–D8, KHÔNG sửa): [../260903-2307-port-column-rebar-to-hprebar/plan.md](../260903-2307-port-column-rebar-to-hprebar/plan.md)
- Bug list B1–B6 đã đóng: [../260903-2307-port-column-rebar-to-hprebar/reports/source-analysis-r01-columnsrebar.md](../260903-2307-port-column-rebar-to-hprebar/reports/source-analysis-r01-columnsrebar.md)
