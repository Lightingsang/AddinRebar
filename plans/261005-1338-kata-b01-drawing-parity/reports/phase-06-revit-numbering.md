# Đợt 6 — Revit: Rebar Number, Extensible Storage (2026-10-05)

## Code
- `KataRebarStorage` (mới): schema 6F2C8B4E-…, Public, trường HostUniqueId / KataNumber / Beam.
- `KataRebarStamp.Apply(rebar, host, beam, kataNumber)`: storage + Partition. Bỏ Comments + Schedule Mark.
- `KataRebarCleanupService`: storage trỏ host, hoặc thẻ Comments cũ (một lần).
- `KataRebarNumberAssigner` (mới): bước "Kata Rebar: số hiệu" cuối orchestrator; mỗi partition: số Revit → tạm (max + 1000) → số Kata; `ArgumentException` → giữ số Revit hoặc số trống kế tiếp + cảnh báo; log `Rebar Number in partition …`.

## Live (Revit 2026.5 test, bản sao `Dam kata test.rvt`, KataB1 chỉ Đọc)
| Lần | Kết quả |
|---|---|
| 1 | beam B01 done; Rebar Number 36 nhóm, Kata 1…37 (thiếu 18), 36 đổi; không số nào bị từ chối |
| 2 | deleted 60 (đúng phần tử lần 1, theo storage); đánh số lại như lần 1 |
- Thiếu 18: F17 và I17 cùng 4300 trong HP → Revit gộp → số 12 + cảnh báo "Revit coi các thanh số Kata 12, 18 là giống nhau". Kata I17 18950…23300 (R-135 chưa rõ).
- CHƯA đo: Comments/Schedule Mark trống trên thanh (không có MCP vào Revit test); suy ra từ code (không còn chỗ ghi).
