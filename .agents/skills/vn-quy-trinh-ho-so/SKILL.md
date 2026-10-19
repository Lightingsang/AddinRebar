---
name: vn-quy-trinh-ho-so
description: "-"
  Điều phối TRỌN quy trình dựng hồ sơ bản vẽ thiết kế thi công (TK BVTC) nhà ở chuẩn Việt Nam,
  từ yêu cầu ban đầu tới bàn giao DXF, DWG, PDF và dự toán Excel. Dùng khi người dùng muốn một bộ
  hồ sơ mới cho nhà phố, nhà ống, biệt thự phố; muốn thêm tờ, sửa tờ hoặc soát một bộ hồ sơ đã có;
  hoặc nói "làm bản vẽ thi công", "dựng hồ sơ thiết kế", "ra file CAD cho căn nhà". Skill này chia
  việc cho vn-kien-truc, vn-ket-cau, vn-co-dien, vn-bo-cuc-to-giay, vn-soat-ban-ve, vn-du-toan,
  vn-xuat-autocad.
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# Quy trình dựng hồ sơ bản vẽ thi công từ đầu đến cuối

Một bộ hồ sơ mẫu **26 tờ** (nhà phố 5x18m, 3 tầng: 15 tờ kiến trúc, 5 tờ kết cấu, 6 tờ cơ điện,
kèm dự toán) được dựng bằng đúng quy trình này, nằm ở
`.agents/skills/_shared/vn-autocad/examples/nha_pho_5x18m`. Phân vân cách làm một bước nào thì mở
file tương ứng trong ví dụ ra xem, đừng tự chế cách khác.

## Làm được và không làm

**Làm được:** mặt bằng, mặt đứng, mặt cắt, khai triển WC và cầu thang, lát gạch, trần, chi tiết
chống thấm, tổng mặt bằng; móng cọc, dầm sàn, chi tiết thép, cầu thang, bảng thống kê cốt thép;
mặt bằng điện nước, sơ đồ nguyên lý; dự toán Excel lấy khối lượng từ chính bản vẽ; soát lỗi tự động;
xuất DWG qua AutoCAD.

**Không làm:** tính nội lực kết cấu, thay chữ ký của người có chứng chỉ hành nghề. Bộ hồ sơ này
là bản triển khai theo cấu tạo; trước khi xin phép hay thi công phải có kỹ sư, kiến trúc sư có
chứng chỉ phù hợp kiểm tra và ký. Nói rõ điều này với người dùng ngay từ đầu.

## Chuẩn bị môi trường (một lần)

```bash
pip install -e ".agents/skills/_shared/vn-autocad"                 # cai thu vien vncad
```

Tạo dự án mới bằng cách chép hồ sơ mẫu rồi sửa dữ liệu, không viết lại từ đầu:

```bash
mkdir -p du_an && touch du_an/__init__.py
cp -r ".agents/skills/_shared/vn-autocad/examples/nha_pho_5x18m" du_an/nha_ong_4x16
```

Mọi lệnh sau chạy từ thư mục chứa `du_an/`.

## Cấu trúc một dự án

| File | Chứa gì | Ai sửa |
|---|---|---|
| `layout.py` | trục, cốt tầng, tường kèm lỗ mở, phòng, thiết bị, cửa, ghi chú, khung tên | kiến trúc |
| `regions.py` | lưới ô model space, gốc đặt từng cụm hình, tâm cửa sổ nhìn | bố cục |
| `draw_*.py` | hàm vẽ từng cụm hình, mỗi file một chủ đề | theo bộ môn |
| `model.py` | gọi mọi hàm vẽ vào đúng ô | bố cục |
| `sheets.py`, `sheets_ky_thuat.py` | các tờ giấy KT và KC, ME | bố cục |
| `foundation_rebar.py`, `rebar_quantities.py` | bảng thép sinh từ kích thước, thép toàn nhà | kết cấu |
| `build_ho_so.py`, `build_dutoan.py` | dựng cả bộ, dự toán | điều phối |

Công cụ trong `.agents/skills/_shared/vn-autocad/tools/` dùng được cho mọi dự án theo đúng ba tên
`model.build_model`, `sheets.build_sheets`, `regions.{CELL_W, CELL_H, O_LUOI}`.

## Mười một bước, mỗi bước có cửa soát

| # | Bước | Skill | Qua cửa khi |
|---|---|---|---|
| 1 | Tiếp nhận yêu cầu | skill này | người dùng xác nhận bảng thông số |
| 2 | Dữ liệu công trình `layout.py` | vn-kien-truc | không còn toạ độ gõ tay trong file vẽ |
| 3 | Danh mục tờ và tỷ lệ | skill này | có bảng số hiệu, tên tờ, tỷ lệ |
| 4 | Bố cục model space theo lưới ô | vn-bo-cuc-to-giay | `check_cell_bleed` sạch |
| 5 | Vẽ kiến trúc | vn-kien-truc | mỗi hình có kích thước, trục, tiêu đề |
| 6 | Vẽ kết cấu và bảng thép | vn-ket-cau | bảng thép sinh từ kích thước, khớp bước ghi |
| 7 | Vẽ cơ điện | vn-co-dien | có bảng ký hiệu, ghi chú tiết diện |
| 8 | Dựng tờ giấy | vn-bo-cuc-to-giay | `check_views` sạch, tâm cửa sổ lấy bằng đo |
| 9 | Soát | vn-soat-ban-ve | 4 phép sạch, đã nhìn ảnh, hàm lượng thép hợp lý |
| 10 | Dự toán | vn-du-toan | mọi dòng thép lấy từ bảng; dòng ước có chữ SỐ ƯỚC |
| 11 | Xuất và bàn giao | vn-xuat-autocad | DWG đếm lại đủ số tờ; báo cáo giả thiết |

Chi tiết từng bước, câu hỏi mẫu cho bước 1 và việc cụ thể phải làm:
[references/quy-trinh-chi-tiet.md](references/quy-trinh-chi-tiet.md).

### Bước 1: hỏi đủ, hỏi kèm phương án

Không vẽ khi chưa có: kích thước lô và mặt tiền, số tầng và chiều cao tầng, công năng từng tầng,
số phòng ngủ và WC, gara, hướng nhà, **địa chất** (quyết định giải pháp móng). Mỗi câu hỏi kèm
2 đến 4 phương án để người dùng chọn, không hỏi trống.

## Hai nhịp chạy

- **Khi đang sửa:** soát không render, vài giây.
  `python ".agents/skills/_shared/vn-autocad/tools/soat-ban-ve-khong-render.py" --du-an du_an.nha_ong_4x16`
- **Khi chốt:** dựng đủ, vài phút, ra DXF + PNG từng tờ + Excel.
  `python -m du_an.nha_ong_4x16.build_ho_so`

## Chia việc cho agent

Phiên chính điều phối; agent không gọi được agent khác. Sau bước 2 đến 4, ba bộ môn vẽ song song
được vì chạm file khác nhau.

| Agent | Phần việc | File sở hữu |
|---|---|---|
| `kien-truc-su` | bước 2, 5 | `layout.py`, `draw_*` kiến trúc, `sheets.py` |
| `ky-su-ket-cau` | bước 6 | `draw_structure.py`, `foundation_rebar.py`, `draw_stair_structure.py`, ... |
| `ky-su-co-dien` | bước 7 | `draw_electrical.py`, `draw_plumbing.py`, `draw_schematic.py` |
| `kiem-soat-ban-ve` | bước 9 | chỉ đọc; báo lỗi kèm cách sửa |
| `ky-su-du-toan` | bước 10 | `build_dutoan.py`, `rebar_quantities.py` |

Khi giao việc cho agent: nêu đường dẫn dự án, file được sửa, tiêu chí qua cửa, đường dẫn báo cáo.

## Năm luật không được phá

1. **Dữ liệu trước, vẽ sau.** Sửa nhà là sửa `layout.py`, không sửa toạ độ rải trong file vẽ.
2. **Khối lượng không gõ tay.** Bảng thép và dự toán sinh từ kích thước. Chỗ nào chưa có căn cứ
   thì ghi rõ "SỐ ƯỚC" ngay trong dòng.
3. **Không nói "xong" khi chưa soát và chưa nhìn ảnh.** Bộ soát có điểm mù (xem vn-soat-ban-ve).
4. **Giả thiết thiết kế phải lộ ra.** Mọi thông số tự đề xuất (tiết diện, thép giằng, cao đài...)
   ghi vào báo cáo bàn giao với nhãn "cần kỹ sư duyệt".
5. **Không đưa dữ liệu thật của chủ đầu tư ra ngoài.** Hồ sơ mẫu, ảnh chụp, repo công khai
   dùng tên mẫu kiểu "Nguyễn Văn A".

## Bàn giao

Mẫu báo cáo và danh sách kiểm cuối: [references/checklist-ban-giao.md](references/checklist-ban-giao.md).

## Tra cứu

- [Chi tiết từng bước](references/quy-trinh-chi-tiet.md)
- [Danh mục bộ hồ sơ và bộ mẫu 26 tờ](references/danh-muc-ho-so-ban-ve.md)
- [Checklist bàn giao](references/checklist-ban-giao.md)
- [25 bài học rút ra khi dựng bộ mẫu](references/bai-hoc-thuc-chien.md): đọc trước khi bắt đầu,
  mỗi bài là một lỗi thật đã gặp và cách tránh.
