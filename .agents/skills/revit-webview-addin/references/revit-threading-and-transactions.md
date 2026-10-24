# Revit API: luồng, đọc, ghi

## Luồng

- WebView2 nhận message trên UI thread của WPF, trong callback COM → `Dispatcher.BeginInvoke` ra ngoài callback trước khi xử lý (JIT/nạp assembly trong callback từng làm treo UI thread khi Revit đang mở model).
- Revit API chỉ hợp lệ trong `IExternalEventHandler.Execute` → mọi việc gửi qua `RevitRequestQueue.RunAsync(app => …, token)`.
- `TaskCompletionSource(RunContinuationsAsynchronously)`; không `.Result`/`.Wait()` trên UI thread.
- Kiểm `ExternalEvent.Raise()` (Accepted/Pending), không thì fail ngay.
- Revit đang mở hộp thoại modal → Execute không chạy → host trả TIMEOUT, request tự hủy.

## Đọc

- Một `FilteredElementCollector` cho cả lượt; filter nhanh (`OfCategory`, `OfClass`, `WhereElementIsNotElementType`) trước filter chậm (`ElementParameterFilter`, LINQ).
- Lượt dài: kiểm `token` mỗi ~2 000 phần tử; giới hạn kết quả (paging `limit ≤ 500`, `ids` ≤ 200 trả về, đếm thì không giới hạn).
- Đếm không cần duyệt: `GetElementCount()`.
- Tên category/parameter đọc một lần mỗi id (cache), không mỗi phần tử.
- Thống kê chỉ chạy khi mở pane, bấm Làm mới, đổi scope/tài liệu.

## Ghi (chuẩn cho module ghi đầu tiên — chưa có code)

```text
preview (đọc + diff, không transaction) ─► trang hiện bảng cũ → mới, ước lượng số phần tử ─► người dùng Áp dụng
apply A (mặc định, 1 Undo): MỘT RunAsync → trong Execute:
         TransactionGroup "HP: <tác vụ>" → các lô Transaction → Commit → Assimilate
apply B (rất lớn, cần tiến độ + huỷ): N lượt RunAsync, mỗi lượt một Transaction "HP: <tác vụ> (i/n)" → N mục Undo
```

- `TransactionGroup` và `Transaction` mở và đóng **trong cùng một** `Execute` (luật repo, `CLAUDE.md` ▸ Feature Folder Convention); không bao giờ giữ group mở qua nhiều lượt `RunAsync`.
- Trong một `Execute` dài, UI thread của Revit (cũng là thread của WebView2) bận → trang không nhận được tiến độ, nút Huỷ không tới được host: phương án A chỉ cho khối lượng mà preview cho thấy chạy nhanh; trang xác nhận khi lớn. Phương án B trả tiến độ/huỷ giữa các lượt nhưng mất "1 Undo" — nói rõ cho người dùng trong preview. GIẢ ĐỊNH CHƯA XÁC MINH: chưa đo ngưỡng thời gian, chưa có module ghi nào chạy live.
- Huỷ cần lệnh riêng (`<module>.cancel`) + `CancellationTokenSource` theo thao tác: bridge hiện chưa có (mã `CANCELLED` mới chỉ dự trữ).
- Lỗi một phần tử: ghi vào `errors[]`, không dừng cả lô, trừ khi tác vụ yêu cầu "tất cả hoặc không".
- `IFailuresPreprocessor` gom warning thành kết quả trả về, không để Revit bật hộp thoại.
- Kiểm `document.IsReadOnly`, workset không sở hữu, phần tử bị pin trước khi ghi → báo trong preview.
- Ghi audit log (người, giờ, lệnh, số phần tử).
- Tên Undo bắt đầu bằng "HP: " để người dùng nhận ra.

## API nguy hiểm (đã gặp thật)

| API | Vấn đề | Cách làm |
|---|---|---|
| `UIDocument.ShowElements` với phần tử không view mở nào thấy | hộp thoại modal "search closed views… Continue?" chặn API thread | chỉ zoom phần tử view hiện hành thấy (`FilteredElementCollector(doc, view.Id).WherePasses(ElementIdSetFilter)`) |
| `Selection.SetElementIds` ~20 000 phần tử | treo Revit 18 s | trang hỏi xác nhận trên 5 000 |
| `PostCommand` (tool nhanh) | mở hộp thoại modal → request kế tiếp TIMEOUT | đúng hành vi; trang báo, request sau tự phục hồi |
| Đóng view cuối của tài liệu | đóng tài liệu (có thể hỏi lưu) | event `document.changed {hasDocument:false}` → trang trống |
