---
phase: 4
title: "Excel COM writer"
status: in-progress
priority: P2
effort: "0.5d"
dependencies: [2]
---

# Phase 4: Excel COM writer

## Overview
Ghi `KataSheet` vào workbook đang active của Excel đang chạy, sheet `Dam`, không thêm package, không Save.

## Requirements
- Functional: attach Excel đang chạy; đọc tên workbook đích (hiển thị trước khi ghi); chặn nếu không có Excel/không có sheet `Dam`; xoá vùng theo hợp đồng P1 (mặc định C11+n → BZ23 như Dynamo); ghi B3:B10 (cột) + 5 hàng từ C; báo số cột đã ghi.
- Functional (bổ sung): ô kiểu `KataText` (B10 `"+3.300"`) ghi dạng text — `NumberFormat = "@"` hoặc tiền tố `'` trước khi gán, để Excel không đổi thành số 3.3.
- Non-functional: P/Invoke `ole32!CLSIDFromProgID` + `oleaut32!GetActiveObject` (pattern `HPExcel/HPExcel.McpBridge/Com/ComInteropHelper.cs:17-47`); late binding `Type.InvokeMember` (không `dynamic` → net48 compile được); release mọi COM object trung gian; không đổi `DisplayAlerts`.

## Architecture
- `ExcelComAttach`: `TryGetRunningExcel(out object app, out string reason)` — phân biệt "Excel không chạy" và "không đăng ký ROT".
- `ComLateBinding`: `Get(obj, name, args)`, `Set(obj, name, value)`, `Release(obj)`.
- `KataExcelWriter`:
  - `Probe()` → tên workbook + có sheet `Dam` không.
  - `Write(KataSheet)` → ghi mỗi hàng 1 lần qua `Range("C11").Resize(1,n).Value2 = object[1,n]`, B3:B10 qua `Resize(8,1)`.
  - `COMException` 0x80010001 (`RPC_E_CALL_REJECTED`) → "Excel đang bận (đang sửa ô?)"; lỗi giữa chừng → dừng, báo hàng lỗi.

## Related Code Files
- Create: `HPRebar/HPRebar/KataExport/Service/{ExcelComAttach,ComLateBinding,KataExcelWriter}.cs`

## Implementation Steps
1. Chép P/Invoke + `ReleaseComObject` an toàn từ ComInteropHelper (không message filter — YAGNI).
2. `ComLateBinding`.
3. `KataExcelWriter.Probe/Write`; vùng xoá theo kết quả P1.
4. Build Debug.R26 + Debug.R24 compile.
5. Thử tay: workbook nháp có sheet `Dam` mở trong Excel, gọi writer qua một test harness nhỏ trong scratchpad (không commit) hoặc qua P5; đọc lại bằng `hprebar-excel` `read_range`.

## Success Criteria
- [ ] Build R26 + compile R24 pass
- [ ] Ghi vào workbook nháp đúng ô; workbook khác (không có sheet `Dam`) bị chặn với thông báo rõ

## Tiến độ (2026-09-27)
- Bản đầu do một agent khác viết (không phải Claude). Claude đã review ([code-review-phase-04-05-excel-ui.md](reports/code-review-phase-04-05-excel-ui.md), chấm 7/10) và sửa:
  - B10 kiểu `KataText` → ghi dạng chuỗi text.
  - Bỏ phần ghi I8/J7/J9 do agent kia tự thêm (không có trong hợp đồng).
  - Vùng xoá trả về đúng C11+n → BZ23 như P1 đã chốt.
  - Lỗi COM được bóc khỏi `TargetInvocationException`; nhận diện Excel bận qua 0x80010001 / 0x8001010A / 0x800AC472.
  - Chặn ghi khi workbook đang kích hoạt khác workbook đã hiển thị (so `FullName`).
  - Báo rõ khi sheet bị ghi dở.
  - Chuỗi được ghi với tiền tố `'` (tránh `1-2` bị đổi thành ngày); chuỗi số thuần như `"1"` ghi thành số.
- ✅ Build R26 + R24 pass.
- ❌ Ghi thử vào workbook nháp: CHƯA TEST (chuyển P6).

## Risk Assessment
Excel đang ở chế độ sửa ô → COM từ chối → thông báo, không ghi dở. Nhiều instance Excel → GetActiveObject trả instance đăng ký ROT đầu tiên → tên workbook hiển thị trước khi ghi để người dùng phát hiện.
