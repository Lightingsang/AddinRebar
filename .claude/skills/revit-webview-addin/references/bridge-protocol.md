# Bridge JSON v1

## Envelope

```text
request  {v:1, kind:"request",  id, command, payload}
response {v:1, kind:"response", id, ok:true,  payload}
         {v:1, kind:"response", id, ok:false, error:{code, message, details}}
event    {v:1, kind:"event",    event, payload}
```

- Tên trường camelCase, enum camelCase (`BridgeJson.Options`), null giữ nguyên (trang kiểm null).
- `id` do trang sinh (UUID), host trả đúng `id`.
- Lệnh `<module>.<verb>`; event `<danh từ>.<quá khứ>` (`selection.changed`, `model.stale`).
- `app.hello` trả version, phiên bản Revit, theme, `capabilities` (danh sách lệnh + tool) → trang tự ẩn tính năng host chưa có.
- Thêm trường = tương thích ngược; đổi nghĩa/xoá = protocol v2.

## Mã lỗi (không đổi sau khi đã phát hành)

| Mã | Khi nào | Trang làm gì |
|---|---|---|
| `BAD_REQUEST` | JSON hỏng, payload sai, validate Domain từ chối | báo lỗi, không thử lại |
| `UNKNOWN_COMMAND` | host chưa có lệnh | ẩn tính năng |
| `NO_DOCUMENT` | không có tài liệu | trạng thái trống |
| `BUSY` | lệnh dài khác đang chạy (`SingleFlightRun`) | thử lại sau |
| `TIMEOUT` | Revit không chạy kịp (thường có hộp thoại modal) | toast + nút thử lại |
| `CANCELLED` | dự trữ: transport chưa truyền token của trang, chưa có lệnh huỷ | im lặng |
| `REVIT_API_ERROR` | Revit từ chối thao tác | báo lỗi |
| `INTERNAL` | lỗi khác | báo lỗi + đường dẫn log |

Handler ném `BridgeException(code, message)`; exception khác được `CommandDispatcher` đổi mã (Revit exception → `REVIT_API_ERROR`).

## Timeout

- Host: mặc định 30 s; lệnh dài truyền timeout riêng khi `Register` (thống kê 120 s).
- Trang: luôn dài hơn host vài giây (35 s / 130 s) để TIMEOUT của host về trước.
- Một nguồn hủy duy nhất: hết giờ → token đã hủy → request còn trong queue bị bỏ qua, không chạy sau khi trang đã nghe TIMEOUT.

## Event

- Làm việc O(1) trong handler Revit (đếm id), coalesce (selection 150 ms, thay đổi 400 ms).
- Pane ẩn → không gửi; hiện lại → phát lại trạng thái mới nhất.
- Thay đổi model → `model.stale {added, deleted, modified}` + nhãn "Dữ liệu cũ"; không tự quét lại.
- Trang bỏ câu trả lời thuộc tài liệu/scope khác (`documentKey`, `viewId`).

## Kiểu TypeScript

`contracts.generated.ts` sinh từ C# (`HPBimToolkit.Core.Tests/TypeScript/`): tên/enum lấy từ serializer thật, optional = tham số có default của **request**, kiểu không ánh xạ được (Guid, DateTime, Dictionary…) → generator báo lỗi. Không sửa tay; `protocol.ts` chỉ giữ envelope. Chuỗi có tập giá trị cố định → khai trong `ProtocolCatalog.StringUnions`.
