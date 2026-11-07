---
phase: 2
title: "Dựng và chạy ETABS"
status: in-progress
priority: P1
effort: ""
dependencies: [1]
type: HITL
---

# Phase 2: Dựng và chạy ETABS

## Overview
Từ `FoundationInput` tạo EDB mới hoàn toàn, chạy, đọc nội lực. Cổng nghiệm thu: tái tạo được model tay MB1.

## Requirements
- Instance ETABS riêng: `cHelper.CreateObject(path)` + `ApplicationStart`, `InitializeNewModel(Ton_m_C)`, `File.NewBlank`.
- **Không** dùng `GetObject`: `NewBlank` trên ETABS đang mở xoá model của người dùng.
- Model: `CONC` theo Eb; `SetPrecastI` b = `[bd, b, bd, 0]`, d = `[h, h − h1, 0, 0, vát, bản]`; lưới 0.1 m có điểm tại tim cột; spring U3 + U1 = U2 = 10⁶ T/m.
- Chiều dày mép cánh và chiều cao vát không có ô nào trong Excel: 2 ô nhập trên giao diện tool, mặc định theo MB1 (0.3 + 0.3 m). Người dùng đã chốt.
- Trục lệch lưới (MB9 tại 4.15 m): thêm nút riêng tại cột, không lò xo. Người dùng đã chốt.
- Lò xo: `Z1` = ki giữa, `Z2` = ki đầu, gán cả **hai** đầu mút. Làm tròn số nguyên.
- Tải tại cột: C → −F3, E → F1, G → F2, D → M1, F → M2.
- Combo = 1.0 × tải chân cột. Tắt case thừa.
- Mọi `ret` phải kiểm tra. Lưu EDB vào thư mục chạy.
- Trả `AnalysisResult { x, M3, V2 }` theo station, tổng phản lực.
- Cân bằng: tổng phản lực = tổng tải, lệch > 0.01 T là dừng.

## Architecture
```text
Core:  ModelSpec (hàm thuần từ FoundationInput), AnalysisResult, EquilibriumCheck
App:   EtabsSession, ModelBuilder, ResultReader, EtabsAssemblyResolver
```
`ModelSpec` thuần để test không cần ETABS: danh sách nút, spring, tải.

## Related Code Files
- Create: `HPFoundation/HPFoundation.Core/Model/**`
- Create: `HPFoundation/HPFoundation.App/Etabs/**`
- Tham khảo: `HPEtabs/HPEtabs.McpBridge/EtabsAssemblyResolver.cs`, `HPEtabs/Directory.Build.props`

## Implementation Steps
1. Spike: tạo instance riêng khi một ETABS khác đang mở. Ghi kết quả license.
2. Spike: `GetPrecastI` trên MB1, xác nhận nghĩa từng phần tử `d`, đặc biệt `vát` và `bản` (đang suy từ diện tích).
3. Test đỏ `ModelSpec`: 118 nút, 4 nút tải, spring hai đầu mút.
4. `ModelBuilder`, `ResultReader`.
5. Cổng tái tạo: chạy **bộ tham số model tay** (tải ETABS cũ, E 2.7×10⁶, đầu phải 160). So M3, V2 với MB1.
6. Chạy bộ tham số đã chốt.

## Success Criteria
- [ ] Bộ tham số tay: M3, V2 lệch ≤ 0.01 tại mọi station so với `MB1.EDB`.
- [ ] Tổng phản lực = tổng tải.
- [ ] ETABS của người dùng không bị đổi, không bị đóng.
- [ ] Instance của tool đóng sạch, kể cả khi lỗi.
- [ ] 👤 Kỹ sư xem model mới trong ETABS và xác nhận.

## Risk Assessment
- **License** chạy instance thứ hai chưa kiểm chứng. Nếu không được: tool yêu cầu đóng ETABS trước khi chạy, không tự đóng.
- Spike cần ETABS, phải có người trực.
- ETABS mở sai cách không đăng ký API: dùng `CreateObject`, không phụ thuộc ROT.
- Kết quả lưu vài chục file: thư mục chạy cục bộ, dọn theo số lần.
