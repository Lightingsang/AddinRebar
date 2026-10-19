---
name: vn-co-dien
description: "-"
  Vẽ phần cơ điện (MEP) hồ sơ thi công nhà ở bằng thư viện vncad: mặt bằng cấp điện chiếu sáng,
  ổ cắm, điều hoà; mặt bằng cấp thoát nước từng tầng; sơ đồ nguyên lý tủ điện tổng; sơ đồ không
  gian cấp thoát nước; bảng ký hiệu. Dùng khi vẽ hoặc sửa các tờ ME.
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# Vẽ cơ điện

Ví dụ đầy đủ: `draw_electrical.py`, `draw_plumbing.py`, `draw_schematic.py` trong
`.agents/skills/_shared/vn-autocad/examples/nha_pho_5x18m`.

## Nền kiến trúc mờ

Mặt bằng cơ điện vẽ trên nền chỉ có tường và cột, không nội thất, lớp `M-WALL` nét mảnh:

```python
from vncad import mep
mep.background(msp, walls, cols, layer="M-WALL")      # walls, cols lay tu layout.FLOOR_WALLS, COLUMNS
```

## Ký hiệu và tuyến

```python
mep.place(msp, doc, "DEN-DOWNLIGHT", (x, y), "1/100")
mep.wire(msp, [(x0, y0), (x1, y1)])                   # day dien, lop M-ELEC
mep.pipe(msp, pts, "cap", "1/100", "Ong cap PPR D25", label_at=(x, y))    # cap: net lien
mep.pipe(msp, pts, "thoat", "1/100", "Thoat san D60", label_at=(x, y))   # thoat: net dut
```

Block có sẵn: điện `DEN-DOWNLIGHT`, `DEN-OP-TRAN`, `QUAT-TRAN`, `CONG-TAC-1`, `CONG-TAC-2`,
`CONG-TAC-3`, `O-CAM-DOI`, `MAY-LANH`, `TU-DIEN`; nước `DONG-HO-NUOC`, `VAN-KHOA`, `ONG-DUNG`,
`HO-GA`.

**Luôn truyền `label_at`** và đặt nhãn ống ở chỗ không có tường: nhãn ống là nhóm lỗi "chữ bị nét
cắt" nhiều thứ ba trong bộ mẫu. Sau khi vẽ chạy bộ soát; chỗ nào báo lỗi dùng
`tools/de-xuat-cho-dat-chu.py "<nhan>"`.

## Bảng ký hiệu

Mọi ký hiệu dùng trên tờ phải có trong bảng ký hiệu vẽ trên giấy, ký hiệu ở đúng cỡ thật trên giấy
(mẫu: hàm `legend` trong `draw_electrical.py`). Danh sách ký hiệu giữ trong một hằng
`KY_HIEU_DIEN`, `KY_HIEU_NUOC` để bảng và hình không lệch nhau.

## Sơ đồ nguyên lý

- Sơ đồ tủ điện, sơ đồ không gian cấp thoát nước **không theo tỷ lệ hình học**; chọn tỷ lệ cửa sổ
  nhìn cho vừa giấy mà chữ vẫn cao 2mm (bộ mẫu: 1/75). Ghi "không tỷ lệ" ở tiêu đề.
- Tủ điện: nguồn vào, aptomat tổng, chống giật tổng, từng nhánh ghi dòng định mức và tiết diện dây,
  cọc tiếp địa.
- Nước: ống đứng cấp, ống đứng thoát xuyên các tầng, nhánh vào thiết bị, bể tự hoại, hố ga.

## Nội dung kỹ thuật tối thiểu trong ghi chú

- Mọi nhánh ổ cắm và bình nóng lạnh qua aptomat chống giật 30mA.
- Ghi tiết diện dây từng nhánh (chiếu sáng 1,5mm², ổ cắm 2,5mm², điều hoà và bếp 4,0mm²).
- Ống cấp PPR hàn nhiệt, thử áp; ống thoát PVC, độ dốc 2%; ống thông hơi bể tự hoại lên quá mái.
- Ống xuyên sàn đặt trước khi đổ bê tông, quét chống thấm quanh cổ ống.

## Tra cứu

- Bố cục tờ và bảng trên giấy: skill `vn-bo-cuc-to-giay`.
