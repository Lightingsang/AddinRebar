# Robot Structural Analysis Tool Catalog (24 Tools)

MCP Server `hprebar-robot` cung cấp 24 tools tuân theo chuẩn hệ sinh thái MCP HPRebar:

## Core Tools (4)

1. **`execute_robot_code`**: Biên dịch và thực thi script C# Roslyn trực tiếp trên mô hình Robot. Hỗ trợ `transaction` (none/auto), `dryRun`, `timeoutSeconds`, `args`.
2. **`get_robot_context`**: Lấy thông tin trạng thái mô hình RTD đang mở, cờ gắn kết COM, cờ bật tắt an toàn `executionEnabled`, `heavyOperationsEnabled`.
3. **`robot://` Resources**: Cung cấp tài nguyên đọc cấu trúc mô hình dạng JSON.
4. **Prompts**: Cung cấp gợi ý ngữ cảnh lập trình RobotOM.

## Registry Meta Tools (8)

1. `search_tools`: Tìm kiếm công cụ trong registry theo từ khóa và category.
2. `propose_tool`: Đề xuất công cụ mới vào registry dạng draft.
3. `test_tool`: Chạy thử công cụ trong sandbox trước khi phát hành.
4. `publish_tool`: Xuất bản công cụ đã kiểm thử thành công.
5. `disable_tool`: Vô hiệu hóa tạm thời một công cụ.
6. `enable_tool`: Kích hoạt lại công cụ đã bị vô hiệu hóa.
7. `deprecate_tool`: Đánh dấu công cụ sắp ngừng hỗ trợ.
8. `delete_tool`: Xóa công cụ khỏi registry.

## Embedded Seed Tools (12)

| Category | Tool | Chức năng | Cấp độ Tier |
|---|---|---|---|
| Model | `get_model_info` | Lấy phiên bản Robot, đường dẫn file, loại kết cấu, số lượng cấu kiện | R |
| Geometry | `get_structural_objects` | Liệt kê danh sách Nodes, Bars, Panels kèm thông số hình học | R |
| Property | `get_materials_and_sections` | Đọc danh mục vật liệu và đặc trưng tiết diện thanh/tấm | R |
| Geometry | `get_coordinate_systems_and_grids` | Đọc hệ toạ độ và hệ lưới trục kết cấu | R |
| Load | `get_load_definitions` | Đọc danh sách tải trọng trường hợp và tổ hợp tải trọng | R |
| Geometry | `draw_bar_by_coords` | Tạo thanh dầm/cột mới theo toạ độ 2 đầu và gán tiết diện | W |
| Geometry | `assign_node_support` | Gán liên kết gối/điều kiện biên cho nút | W |
| Property | `assign_bar_section` | Gán tiết diện mới cho thanh kết cấu | W |
| Load | `assign_bar_load` | Gán tải trọng phân bố hoặc tập trung lên thanh | W |
| Analysis | `run_calculations` | Kích hoạt bộ giải FEA của Robot (`project.CalcEngine.Calculate`) | D |
| Results | `get_node_reactions` | Trích xuất phản lực gối liên kết theo trường hợp/tổ hợp tải | R |
| Results | `get_bar_forces` | Trích xuất biểu đồ nội lực (My, Mz, Fz, Fy, Fx) tại các điểm trên thanh | R |
