---
phase: 3
title: "G3 — cột xoay: mặt cắt và bảng tag bám trục cột"
status: implemented
effort: 1.5d
depends: []
---

# Phase 03 — Cột xoay trong plan

## Context Links

- [plan.md](plan.md)
- D4 (canvas `DrawingContext`) — không đụng ở phase này
- Nhánh đúng để đối chiếu: [DetailViewCreator.cs:158-165](../../HPRebar/HPRebar/Column%20Rebar/DetailViewCreator.cs#L158-L165) · [PointMapper.cs:26-42](../../HPRebar/HPRebar/Column%20Rebar/PointMapper.cs#L26-L42)
- Fixture spec: [HPRebar.Tests/Fixtures/README.md](../../HPRebar/HPRebar.Tests/Fixtures/README.md) — **chưa có cột xoay**, phải bổ sung

## Overview

- **Priority:** High
- **Status:** draft · **CHẶN Ở BƯỚC 0** — cần user chọn hướng, không tự quyết
- Cột xoay trong plan lọt qua validation, phân giải được mặt, tạo được thép **đúng chỗ**, nhưng mặt cắt và bảng tag đặt theo trục global nên lệch.

## Đã thực thi (2026-09-05) — chọn hướng B, KHÔNG thêm rule validator

Bản kế hoạch khuyến nghị hướng A vì cho rằng B có rủi ro hồi quy. Kiểm lại thì **rủi ro đó bằng 0**, nên
đảo lại quyết định:

- Với cột không xoay, `east == XYZ.BasisX` và `north == XYZ.BasisY`, nên transform mới
  (`BasisX = east`, `BasisY = -north`, `BasisZ = east × (-north) = -Z`) ra **đúng y hệt**
  `Transform.CreateRotation(XYZ.BasisX, Math.PI)` cũ. B là mở rộng thuần, bản vẽ đang có không xê dịch.
- Hướng A sẽ chặn một trường hợp rất phổ biến ngoài thực tế (lưới xoay, cánh nhà xiên) mà không cần thiết.

**Không thêm rule validator nào**, vì khoảng trống thực tế đã kín:

- Rule 6 `IsNotRotated` **đã** chặn stack có các cột xoay lệch nhau — nó so `FaceNormal` từng cặp liền kề.
- `SideFacing` dùng `alignment < Math.PI / 4` **chặt**, nên cột xoay đúng 45° không phân giải được mặt và
  rớt sẵn ở rule 5.
- Khoảng trống còn lại đúng bằng "mọi cột xoay cùng một góc" — mà đó chính là cái hướng B xử lý đúng.

**Bản sửa**

- Mới: [ColumnPlanAxes.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnPlanAxes.cs) — trả `(East, North)` từ
  face normal của cột, lùi về trục thế giới cho tiết diện tròn (hình tròn không có hướng để bám).
- [SectionViewCreator.cs](../../HPRebar/HPRebar/Column%20Rebar/SectionViewCreator.cs) `:77-87` — transform
  dựng từ trục cột. Crop `Min`/`Max` vốn tính từ `section.B`/`H` giờ mới thật sự cùng hệ trục với chúng.
- [RebarTableTagCreator.cs](../../HPRebar/HPRebar/Column%20Rebar/RebarTableTagCreator.cs) — `Create`,
  `WriteRow`, `TableOrigin` nhận `(East, North)`; mọi `XYZ.BasisX`/`BasisY` thay bằng trục cột. Sửa cùng
  commit với view, đúng như §Key Insights cảnh báo. `XYZ.BasisZ` giữ nguyên — cột đã được rule 2 bảo đảm
  thẳng đứng nên Z đúng là trục thế giới.

**Chưa làm:** cột `C-ROTATED` trong spec fixture và 2 test TUnit — vẫn BLOCKED vì chưa có `.rvt`.

## Key Insights

### Bốn điểm đã xác minh

| Điểm | Bằng chứng | Nội dung |
|---|---|---|
| Validator | [ColumnStackValidator.cs:78-100](../../HPRebar/HPRebar/Column%20Rebar/ColumnStackValidator.cs#L78-L100), so sánh ở `:95` | `IsNotRotated` chạy `for (i = 1; ...)` — chỉ so **hai cột liền kề**. Stack 1 cột không vào vòng lặp lần nào; stack nhiều cột cùng xoay một góc thì mọi cặp đều khớp. Cả hai đều **pass** |
| Face reader | [ColumnSolidFaceReader.cs:223-240](../../HPRebar/HPRebar/Column%20Rebar/ColumnSolidFaceReader.cs#L223-L240) | `SideFacing` nhận `alignment < Math.PI / 4` → lệch tới 45° vẫn phân giải được South/North/East/West. Cột xoay 30° vẫn ra đủ 4 mặt → rule 5 (`GetSectionStyle`) cũng pass |
| Mặt cắt | [SectionViewCreator.cs:80](../../HPRebar/HPRebar/Column%20Rebar/SectionViewCreator.cs#L80) | `Transform.CreateRotation(XYZ.BasisX, Math.PI)` — trục global. Mặt phẳng cắt vẫn nằm ngang (đúng), nhưng hai trục trong mặt phẳng là global X/Y, trong khi `Min`/`Max` ở `:88-89` tính từ `section.B`/`section.H` là kích thước **theo hướng cột**. Crop box vuông trục thế giới bao một tiết diện xoay → cắt vào góc cột, và view quay sai hướng |
| Bảng tag | [RebarTableTagCreator.cs:161](../../HPRebar/HPRebar/Column%20Rebar/RebarTableTagCreator.cs#L161) | Offset theo `XYZ.BasisX` global. Cùng vấn đề ở `:158` (circular, `XYZ.BasisY`) và layout ô ở `:50`, `:108-112` |

### Điểm mấu chốt: hai file view phải sửa CÙNG NHAU

Hiện tại `SectionViewCreator` và `RebarTableTagCreator` **nhất quán với nhau** — cả hai đều global.
Bảng nằm đúng chỗ *trong hệ trục của view*. Sửa view sang bám hướng cột mà không sửa bảng thì bảng
sẽ bay ra ngoài view. Không tách hai file này thành hai phase được.

### Điểm không phải lỗi

`PointMapper.For` ([PointMapper.cs:26-42](../../HPRebar/HPRebar/Column%20Rebar/PointMapper.cs#L26-L42))
dùng `faces.East!.FaceNormal` / `North` cho rectangle, chỉ dùng trục global cho circular — mà hình tròn
thì không có hướng. **Thép chính đặt đúng ngay cả khi cột xoay.** Đây là lý do hậu quả G3 giới hạn ở
view và annotation, không lan sang model thép.

## Requirements

**Chức năng**

1. Cột xoay hoặc bị chặn tường minh với thông báo đọc được, hoặc được hỗ trợ đầy đủ ở view + bảng tag. Không còn trạng thái "lọt rồi vẽ sai".
2. Cột không xoay giữ nguyên hành vi hiện tại — không đổi tên view, không đổi vị trí bảng.
3. Ngưỡng "xoay" phải là một hằng số có tên, đặt cùng chỗ với ngưỡng 45° hiện có.

**Phi chức năng**

4. Không đụng `PointMapper`, `StirrupGeometry`, `RebarCreationService` — thép đang đúng.
5. Không mở `Transaction` mới (D8).

## Architecture

### Hai lựa chọn — trình bày để user chọn

**Lựa chọn A — chặn ở validator (rẻ, an toàn, mất tính năng)**

Thêm một rule mới cạnh rule 6: mọi cột trong stack phải có `East.FaceNormal` song song với trục global
trong ngưỡng cho trước, nếu không thì fail với mã lỗi riêng.

- Sửa: `ColumnStackValidator.cs` + 1 chuỗi i18n trong `LocalizationService`
- Khối lượng: ~0.5d · Rủi ro: thấp · Verify: TUnit 1 test (BLOCKED) + build
- Đánh đổi: **người dùng có cột xoay không dùng được tool.** Bản gốc R01 cũng không hỗ trợ, nên đây không phải hồi quy so với nguồn
- Rủi ro riêng: ngưỡng đặt chặt quá sẽ chặn nhầm cột lệch 0.5° do dựng hình — cần ngưỡng thực dụng (ví dụ 1°), không phải `1e-9`

**Lựa chọn B — bám hướng cột ở view (đầy đủ, đắt, khó verify)**

Dựng `Transform` của mặt cắt từ `faces.East!.FaceNormal` như `DetailViewCreator` đang làm, và đổi bảng
tag sang cùng hệ trục đó.

- Sửa: `SectionViewCreator.cs` (`:80-90`, `:103-116`) + `RebarTableTagCreator.cs` (`:50`, `:108-112`, `:151-161`)
- Khối lượng: ~1.5d · Rủi ro: **cao** · Verify: **BLOCKED hoàn toàn** — sai lệch hình học chỉ thấy được bằng mắt trong Revit
- Đánh đổi: hỗ trợ đầy đủ cột xoay, nhưng chạm hai file đang chạy đúng cho cột thẳng, mà không có fixture để chứng minh không hồi quy
- Vẫn cần thêm rule validator cho trường hợp **stack có các cột xoay khác nhau** — B không thay được A hoàn toàn

**Khuyến nghị: A, và ghi B vào backlog.**
Lý do: (1) không có fixture nên B không chứng minh được đúng hay sai, mà B lại sửa đúng hai file đang
hoạt động đúng cho trường hợp phổ biến; (2) hậu quả hiện tại là bản vẽ sai — im lặng và khó phát hiện —
A biến nó thành thông báo lỗi ngay lúc chọn cột; (3) A không chặn đường làm B sau này, còn làm B trước
rồi phát hiện sai thì phải hoàn tác hai file.

**Quyết định là của user.** Các bước dưới đây viết cho lựa chọn A; nếu user chọn B thì bước 4–8 thay bằng nhánh B ghi ở bước 9.

## Related Code Files

**Sửa — lựa chọn A**

- `HPRebar/HPRebar/Column Rebar/ColumnStackValidator.cs` — bảng rule `:24-40`, thêm hàm cạnh `IsNotRotated` `:78-100`
- `HPRebar/HPRebar/Column Rebar/LocalizationService.cs` — 1 chuỗi lỗi EN + VN
- `HPRebar/HPRebar.Tests/ColumnStackValidatorTests.cs` — 2 test
- `HPRebar/HPRebar.Tests/Fixtures/README.md` — thêm cột `C-ROTATED` vào spec fixture

**Sửa thêm — chỉ khi user chọn B**

- `HPRebar/HPRebar/Column Rebar/SectionViewCreator.cs` — `:80-90`, `:103-116`
- `HPRebar/HPRebar/Column Rebar/RebarTableTagCreator.cs` — `:50`, `:108-112`, `:151-161`

**Tạo / Xoá:** không.

## Implementation Steps

1. **BƯỚC 0 — hỏi user chọn A hay B.** Trình bày bảng đánh đổi ở §Architecture. Không code trước khi có câu trả lời.
2. Bổ sung `C-ROTATED` vào [Fixtures/README.md](../../HPRebar/HPRebar.Tests/Fixtures/README.md): cùng family type với `C1-LOWER`, 400×600, Foundation → Level 1, **xoay 30° trong plan**, đứng một mình (không có cột trên).
3. Đặt hằng số ngưỡng trong `ColumnStackValidator` cạnh `Tolerance` ở `:16`, ví dụ `RotationToleranceRadians = 1° tính bằng radian`. Ghi comment giải thích vì sao **không** dùng `1e-9`: mô hình thật luôn lệch vài phần nghìn độ.
4. Viết `IsAlignedToProjectAxes(IReadOnlyList<Element> columns)` cạnh `IsNotRotated` (`:78`): với mỗi cột rectangle, lấy `ColumnSolidFaceReader.GetEast(column)`, kiểm góc giữa `FaceNormal` và `XYZ.BasisX` nằm trong ngưỡng của `0` hoặc `π/2` hoặc `π` hoặc `3π/2`. Cột circular bỏ qua — không có hướng.
5. Chèn rule mới vào bảng `:24-40` **ngay sau** rule 6 (`IsNotRotated`), lấy mã 15 — mã mới, không chèn giữa để khỏi đánh số lại các mã đang dùng trong i18n.
6. Thêm chuỗi lỗi mã 15 vào `LocalizationService` cho cả EN và VN, nội dung nói rõ cột nào và lệch bao nhiêu độ.
7. Ghi `Log.Warning` khi rule 15 fail, kèm `element.Id` và góc đo được — cùng phong cách log đã có ở [ColumnSolidFaceReader.cs:160-165](../../HPRebar/HPRebar/Column%20Rebar/ColumnSolidFaceReader.cs#L160-L165).
8. Viết 2 test TUnit (xem §Success Criteria).
9. **Nhánh B (chỉ khi user chọn B):** trong `SectionViewCreator.Create` thay `:80` bằng transform dựng từ `faces.East!.FaceNormal` — `BasisX = east`, `BasisY = north`, `BasisZ = -XYZ.BasisZ` (giữ hướng nhìn xuống như hiện tại), đối chiếu cách `DetailViewCreator` làm ở `:81-85`. Rồi trong `RebarTableTagCreator` thay `XYZ.BasisX`/`XYZ.BasisY` ở `:50`, `:108-112`, `:158`, `:161` bằng `east`/`north` của `stack.Faces[index]`. Vẫn giữ rule 15 cho trường hợp các cột trong stack xoay lệch nhau.
10. Chạy build gate.

## Todo List

- [ ] **Hỏi user: A hay B** — chặn mọi việc còn lại
- [ ] Thêm `C-ROTATED` vào spec fixture
- [ ] Hằng số `RotationToleranceRadians` + comment lý do
- [ ] `IsAlignedToProjectAxes` + rule mã 15
- [ ] Chuỗi i18n EN + VN cho mã 15
- [ ] `Log.Warning` kèm id + góc
- [ ] 2 test TUnit
- [ ] (nếu B) `SectionViewCreator` + `RebarTableTagCreator` sửa cùng nhau
- [ ] `dotnet build HPRebar.slnx -c Debug.R26`
- [ ] `dotnet build HPRebar.slnx -c Debug.R23`
- [ ] `dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26`
- [ ] `dotnet test HPRebar.Core.Tests` — chống hồi quy

## Success Criteria

**Xác minh được ngay (build + grep):**

- `dotnet build HPRebar.slnx -c Debug.R26` và `-c Debug.R23` — 0 error.
- `dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26` — 0 error (**project path**, không qua solution).
- `dotnet test HPRebar.Core.Tests` — vẫn xanh.
- Lựa chọn A: bảng rule ở `ColumnStackValidator.cs:24-40` có đúng 15 mục, mã 15 nằm sau mã 6.
- Lựa chọn B: `grep -n "XYZ.BasisX\|XYZ.BasisY" "HPRebar/HPRebar/Column Rebar/SectionViewCreator.cs" "HPRebar/HPRebar/Column Rebar/RebarTableTagCreator.cs"` — 0 hit ngoài các dòng đã ghi chú là cố ý.

**BLOCKED (thiếu fixture; và fixture hiện tại còn thiếu `C-ROTATED`, phải dựng thêm):**

- **Test chứng minh G3** — `ASingleRotatedColumnIsRejected` (TUnit): `Validate(doc, [C-ROTATED])`.
  Trước khi sửa trả `ValidationResult.Ok` (chứng minh cột xoay lọt); sau khi sửa trả `Fail(15)`.
- **Test giữ hành vi** — `AStraightStackStillPasses` (TUnit): `Validate(doc, [C1-LOWER, C1-UPPER])` vẫn `Ok`.
- Lựa chọn B, F5 Revit 2026 trên `C-ROTATED`: mặt cắt phải quay theo cột, crop không cắt góc, bảng tag nằm cạnh mặt đông của cột trong view.
- R23, R24, R27 build-only — máy dev chỉ có Revit 2025 và 2026.

## Risk Assessment

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Ngưỡng xoay chặn nhầm cột dựng hơi lệch | Trung bình | Ngưỡng 1°, không phải `1e-9`; log góc đo được để người dùng tự đối chiếu |
| Chèn rule làm đánh số lại mã lỗi, vỡ i18n | Trung bình | Dùng mã mới 15, không chèn giữa dãy 1–14 |
| (B) Sửa hai file đang đúng mà không có fixture chứng minh | **Cao** | Chính là lý do khuyến nghị A; nếu user vẫn chọn B thì bắt buộc dựng fixture trước, không sửa mù |
| (B) View bám hướng cột nhưng bảng vẫn global → bảng ra ngoài view | Cao | Hai file sửa trong cùng một commit, có checklist chung ở §Todo |
| Cột circular bị rule mới chặn nhầm | Thấp | Bước 4 loại tường minh cột circular |

## Security Considerations

Không có bề mặt bảo mật mới. Thông báo lỗi và log chỉ chứa `ElementId` và góc — không chứa đường dẫn
model hay tên người dùng. Chuỗi i18n là hằng số dịch sẵn, không nội suy từ dữ liệu ngoài.

## Next Steps

- Nếu chọn A: mở backlog item cho B, kèm điều kiện tiên quyết là fixture có `C-ROTATED`.
- Phase 4 không phụ thuộc phase này.
