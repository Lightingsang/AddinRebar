# Troubleshooting HPDynamo MCP — symptom → cause → what to do

| Triệu chứng (Symptom) | Nguyên nhân (Cause) | Cách xử lý (What to do) |
|---|---|---|
| Không thấy tool nào trong AI coding agent / `tools/list` không phản hồi | File `.mcp.json` chưa cấu hình server `hprebar-dynamo` hoặc sai đường dẫn file exe | Thêm entry `hprebar-dynamo` vào file `.mcp.json` trỏ tới `HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe` |
| Mã lỗi `-32001`: "Allow AI Dynamo execution is OFF" | Nút gạt bảo vệ an toàn trên cửa sổ Bridge đang tắt (mặc định tắt mỗi khi bật Revit) | Người dùng mở ribbon Revit: **HPDynamo ▸ MCP ▸ Status View** và tick chọn **"Allow AI execution in this Revit session"** |
| Mã lỗi `-32002`: "Another Dynamo script or graph is still running" | Đang có một lệnh hoặc đồ thị khác đang thực thi trong Dynamo/Revit | Chờ lệnh trước hoàn tất; hoặc gọi tool `cancel_execution` để hủy tác vụ đang chạy |
| Báo lỗi "Pipe not connected / hpdynamo-mcp-2026 missing" | Revit 2026 chưa mở, hoặc chưa deploy add-in manifest | Mở Revit 2026 với một dự án; kiểm tra tệp `%AppData%\Autodesk\Revit\Addins\2026\HPDynamo.McpBridge.addin` |
| Lỗi "No document is open in Revit" | Revit đang ở màn hình Start hoặc chưa mở file dự án | Mở một dự án Revit (.rvt) có sẵn trước khi gọi các tool Dynamo |
| Lỗi "Active workspace is not available" khi gọi `add_node_to_workspace` hoặc `connect_node_ports` | Người dùng chưa mở cửa sổ giao diện Dynamo canvas trong Revit | Khởi động Dynamo UI từ tab **Manage ▸ Dynamo** trong Revit để kích hoạt ViewExtension |
| Lỗi `GUARD`: "Namespace 'System.IO' is forbidden" | Script C# hoặc seed cố gắng gọi namespace bị cấm trong `GuardProfile.Dynamo` | Bỏ các lời gọi I/O trực tiếp, chuyển sang dùng `args` hoặc API chuẩn của Dynamo |
| Node bị lỗi đỏ (Warning/Error) sau khi chạy graph | Thiếu tham số đầu vào, sai kiểu dữ liệu hoặc thiếu Dynamo package | Gọi `get_active_graph_info` kiểm tra danh sách `unresolvedPackages`, hoặc gọi `get_node_output_values` để xem chi tiết lỗi của node |
| Lỗi cyclic connection khi gọi `connect_node_ports` | Cố gắng nối đầu ra của một node vào chính đầu vào của node đó | Kiểm tra GUID và tên cổng của node nguồn và node đích trước khi nối |
| Lỗi "inspect_type tool is suppressed" (mã `-32602`) | Tool `inspect_type` bị ẩn có chủ đích theo kiến trúc Dynamo MCP (thay bằng `inspect_node_type`) | Dùng tool `inspect_node_type` để tra cứu chi tiết cổng và kiểu dữ liệu của node Dynamo |
| Thay đổi trong Revit tự động biến mất sau khi chạy | Tham số `dryRun` đang đặt là `true` (mặc định cho các thao tác thử nghiệm) | Đặt `dryRun: false` khi đã xác nhận kết quả xem trước chính xác |

---

### Các thư mục log và thiết lập quan trọng:
- **Log máy chủ:** `%LocalAppData%\HPDynamo\McpServer\logs\`
- **Log Bridge:** `%LocalAppData%\HPDynamo\McpBridge\logs\mcpbridge-YYYYMMDD.log`
- **Thư viện Registry Tool:** `%AppData%\HPDynamo\McpServer\tools-library\`
- **Database lưu trữ Registry:** `%AppData%\HPDynamo\McpServer\registry.db`
