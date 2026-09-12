---
phase: 4
title: "A2 — FailureHandlingOptions cho mọi Transaction"
status: implemented
effort: 0.5d
depends: []
---

# Phase 04 — Xử lý cảnh báo Revit trong Transaction

## Context Links

- [plan.md](plan.md)
- **D8** — chỉ `ColumnRebarOrchestrator` sở hữu `TransactionGroup`; service chỉ mở `Transaction`; command không chạm group. Phase này KHÔNG được phá D8
- [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs) · [RebarCreationService.cs](../../HPRebar/HPRebar/Column%20Rebar/RebarCreationService.cs)

## Overview

- **Priority:** High
- **Status:** draft · **BLOCKED verify** (thiếu fixture `.rvt`)
- Không `Transaction` nào trong feature đặt `FailureHandlingOptions`, nên dialog cảnh báo của Revit chặn luồng giữa chừng. Với modal dialog của tool (D7) và progress bar đang chạy, người dùng không có đường thoát tự động.

## Đã thực thi (2026-09-05)

Ship đúng thiết kế. Mới: [RebarFailureHandling.cs](../../HPRebar/HPRebar/Column%20Rebar/RebarFailureHandling.cs)
— `Apply(Transaction)` + `SwallowWarnings : IFailuresPreprocessor`, lọc theo `FailureSeverity.Warning`,
ghi `Log.Warning` rồi `DeleteWarning`; mục Error không đụng vào nên vẫn tới được rollback của orchestrator.

7/7 call-site đã chèn `RebarFailureHandling.Apply(transaction);` **ngay sau** `transaction.Start()`
(orchestrator `:99 :111 :126 :146 :167`, service `:78 :112`). `TransactionGroup` không đụng — D8 nguyên vẹn,
grep xác nhận chỉ orchestrator sở hữu group.

**API đã kiểm tay R23 + R27:** `GetFailureHandlingOptions`, `SetFailureHandlingOptions`,
`SetFailuresPreprocessor`, `SetClearAfterRollback`, `GetFailureMessages()`, `DeleteWarning`, `GetSeverity`,
`GetDescriptionText` — có đủ, chữ ký giống nhau, không cần `#if REVIT*`. Hai setter của
`FailureHandlingOptions` trả về chính object đó, nên code gán lại `options = options.Set...()` cho chắc.

**Chưa làm:** 2 test TUnit — BLOCKED vì thiếu fixture.

## Key Insights

### Xác minh

```
grep -rn "FailureHandling\|SetFailureHandlingOptions\|IFailuresPreprocessor" "HPRebar/HPRebar/Column Rebar"
→ 0 kết quả
```

### Đính chính phạm vi so với mô tả ban đầu

Đề bài liệt kê `RebarCreationService`, `DetailViewCreator`, `SectionViewCreator`, `DimensionCreator`,
`RebarTableTagCreator`. Thực tế **4 creator sau không mở `Transaction` nào** — orchestrator bọc chúng.
`grep` toàn feature cho ra đúng **7 `Transaction` + 1 `TransactionGroup`**:

| File | Dòng | Tên transaction |
|---|---|---|
| [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L97) | `:97` | Create Detail View |
| [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L108) | `:108` | Create Section View |
| [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L122) | `:122` | Create Dimension View |
| [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L141) | `:141` | Create Dimension Section |
| [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L162) | `:162` | Create Tag Bars |
| [RebarCreationService.cs](../../HPRebar/HPRebar/Column%20Rebar/RebarCreationService.cs#L76) | `:76` | Create Stirrup Bars |
| [RebarCreationService.cs](../../HPRebar/HPRebar/Column%20Rebar/RebarCreationService.cs#L109) | `:109` | Create Main Bars |
| [ColumnRebarOrchestrator.cs](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L56) | `:56` | `TransactionGroup "Column Rebar"` |

### Đặt handler ở đâu để không phá D8

`TransactionGroup` **không có** `SetFailureHandlingOptions` — chỉ `Transaction` có. Nên không có cám dỗ
đặt ở tầng group. Việc cần tránh là ngược lại: đưa cái gì đó xuống service khiến service phải biết về
group. Cách giữ D8 nguyên vẹn:

- Một helper tĩnh, không trạng thái, không giữ `Document` và không mở transaction nào.
- Mỗi call-site tự gọi helper trên `Transaction` **của chính nó**, ngay sau `Start()` — trước `Start()` thì
  options chưa gắn được vào transaction đang chạy; sau `Commit()` thì vô nghĩa.
- Service vẫn không nhìn thấy group; orchestrator vẫn là nơi duy nhất có `TransactionGroup`.

## Requirements

**Chức năng**

1. Cả 7 `Transaction` đặt `FailureHandlingOptions` với `SetClearAfterRollback(true)` và preprocessor tự nuốt warning.
2. Preprocessor **xoá warning, không xoá error** — error phải nổi lên để `catch` ở [ColumnRebarOrchestrator.cs:83-89](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L83-L89) rollback cả group.
3. Mọi warning bị nuốt phải ghi `Log.Warning` kèm mô tả — nuốt im lặng là đổi một lỗi lộ thành một lỗi ẩn.

**Phi chức năng**

4. D8 giữ nguyên: `grep -c "TransactionGroup" "HPRebar/HPRebar/Column Rebar/RebarCreationService.cs"` vẫn là 1 (chỉ dòng docstring `:17`).
5. Không thêm `#if REVIT*` — `IFailuresPreprocessor` và `FailureHandlingOptions` giống nhau R23→R27; **kiểm chữ ký tay** cho R23 và R27 trước khi tin.

## Architecture

```
HPRebar/Column Rebar/
└── RebarFailureHandling.cs   (mới)
    ├── sealed class SwallowWarnings : IFailuresPreprocessor
    │      PreprocessFailures → DeleteWarning cho mỗi FailureMessageAccessor
    │      mức Warning; log; trả FailureProcessingResult.Continue
    └── static void Apply(Transaction transaction)
           gọi ngay sau transaction.Start()
```

Đặt ở gốc thư mục feature theo Feature Folder Convention — đây là service, không phải Model/View/ViewModel.
Namespace `HPRebar.ColumnRebar`, khai báo tường minh, file-scoped.

## Related Code Files

**Tạo**

- `HPRebar/HPRebar/Column Rebar/RebarFailureHandling.cs`

**Sửa**

- `HPRebar/HPRebar/Column Rebar/ColumnRebarOrchestrator.cs` — thêm 1 dòng sau mỗi `Start()` ở `:99`, `:110`, `:124`, `:143`, `:163`
- `HPRebar/HPRebar/Column Rebar/RebarCreationService.cs` — thêm 1 dòng sau mỗi `Start()` ở `:78`, `:111`
- `HPRebar/HPRebar.Tests/ColumnRebarOrchestratorTests.cs` — 1 test TUnit

**Xoá:** không.

## Implementation Steps

1. Tạo `HPRebar/HPRebar/Column Rebar/RebarFailureHandling.cs`, namespace `HPRebar.ColumnRebar`, file-scoped, `internal static class RebarFailureHandling` + `private sealed class SwallowWarnings : IFailuresPreprocessor`.
2. `PreprocessFailures(FailuresAccessor accessor)`: duyệt `accessor.GetFailureMessages()`; với mục có `GetSeverity() == FailureSeverity.Warning` thì `accessor.DeleteWarning(message)` và `Log.Warning` kèm `message.GetDescriptionText()`; mục `FailureSeverity.Error` **không đụng vào**. Trả `FailureProcessingResult.Continue`.
3. `Apply(Transaction transaction)`: lấy `transaction.GetFailureHandlingOptions()`, `SetFailuresPreprocessor(new SwallowWarnings())`, `SetClearAfterRollback(true)`, rồi `transaction.SetFailureHandlingOptions(options)`.
4. Kiểm chữ ký tay: `FailuresAccessor.GetFailureMessages`, `DeleteWarning`, `FailureHandlingOptions.SetFailuresPreprocessor`, `SetClearAfterRollback` trên `RevitAPI.xml` của **2023.1.90 và 2027.2.0**. Không tin sweep theo tên — CLAUDE.md ghi rõ sweep đã từng bỏ sót `CreateFreeForm` đổi signature.
5. Chèn `RebarFailureHandling.Apply(transaction);` ngay sau `transaction.Start();` tại [ColumnRebarOrchestrator.cs:99](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L99), [:110](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L110), [:124](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L124), [:143](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L143), [:163](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L163).
6. Chèn tương tự tại [RebarCreationService.cs:78](../../HPRebar/HPRebar/Column%20Rebar/RebarCreationService.cs#L78) và [:111](../../HPRebar/HPRebar/Column%20Rebar/RebarCreationService.cs#L111).
7. **Không** đụng `TransactionGroup` ở [ColumnRebarOrchestrator.cs:56](../../HPRebar/HPRebar/Column%20Rebar/ColumnRebarOrchestrator.cs#L56) — không có API tương ứng, và đụng vào là phá D8.
8. Cập nhật docstring `:9-18` của orchestrator: nói thêm rằng warning bị nuốt và ghi log, error vẫn rollback cả group.
9. Viết 1 test TUnit (xem §Success Criteria).
10. Chạy build gate.

## Todo List

- [ ] `RebarFailureHandling.cs` — preprocessor + `Apply`
- [ ] Kiểm chữ ký API tay trên R23 (2023.1.90) và R27 (2027.2.0)
- [ ] 5 call-site trong `ColumnRebarOrchestrator`
- [ ] 2 call-site trong `RebarCreationService`
- [ ] Cập nhật docstring orchestrator
- [ ] 1 test TUnit
- [ ] `dotnet build HPRebar.slnx -c Debug.R26`
- [ ] `dotnet build HPRebar.slnx -c Debug.R23`
- [ ] `dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26`
- [ ] `dotnet test HPRebar.Core.Tests` — chống hồi quy

## Success Criteria

**Xác minh được ngay (build + grep):**

- `grep -rn "transaction.Start();" "HPRebar/HPRebar/Column Rebar"` → 7 hit, **mỗi hit có `RebarFailureHandling.Apply` ở dòng kế tiếp**.
- `grep -rn "TransactionGroup" "HPRebar/HPRebar/Column Rebar"` → chỉ `ColumnRebarOrchestrator.cs:12,56` và dòng docstring `RebarCreationService.cs:17`. **D8 nguyên vẹn.**
- `grep -n "SetFailureHandlingOptions" "HPRebar/HPRebar/Column Rebar/RebarFailureHandling.cs"` → 1 hit, đặt ở helper chứ không rải khắp call-site.
- `dotnet build HPRebar.slnx -c Debug.R26` và `-c Debug.R23` — 0 error, 0 warning CS.
- `dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26` — 0 error (**project path**, `.slnx` đặt `Build Project="false"`).
- `dotnet test HPRebar.Core.Tests` — vẫn xanh.

**BLOCKED (thiếu `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`):**

- **Test chứng minh A2** — `ARunThatRaisesAWarningCompletesWithoutADialog` (TUnit trên `ColumnRebarOrchestratorTests`): chạy `Run` trên một stack sinh cảnh báo Revit quen thuộc (thép chồng lấn / thanh ra ngoài lớp bảo vệ). Trước khi sửa, test treo chờ dialog và timeout; sau khi sửa, `Run` trả kết quả và Serilog có dòng `Log.Warning` mô tả cảnh báo.
- **Test giữ hành vi** — `AnErrorStillRollsBackTheWholeGroup`: dựng tình huống error thật; `Run` vẫn ném và model không còn phần tử nào — chứng minh preprocessor không nuốt nhầm error.
- F5 Revit 2026 trên một cột có bê tông mỏng: chạy tool, không có dialog nào chặn, log `%LocalAppData%\HPRebar\logs\` có dòng warning.
- R23, R24, R27 build-only — máy dev chỉ có Revit 2025 và 2026.

## Risk Assessment

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Nuốt nhầm error → model hỏng im lặng | **Cao** | Bước 2 lọc theo `FailureSeverity.Warning`; test `AnErrorStillRollsBackTheWholeGroup` (BLOCKED) là kiểm chứng chính; F5 thủ công là lưới an toàn tạm |
| Nuốt warning im lặng, mất dấu vết chẩn đoán | Trung bình | Bắt buộc `Log.Warning` cho mọi warning bị xoá; đây là yêu cầu chức năng 3, không phải tuỳ chọn |
| Đặt `Apply` trước `Start()` → không có tác dụng | Trung bình | Grep ở §Success Criteria kiểm đúng thứ tự dòng |
| API `DeleteWarning` khác giữa R23 và R27 | Thấp | Bước 4 kiểm tay hai đầu dải version |
| Sửa 7 call-site rải rác, sót một chỗ | Trung bình | Grep đếm 7/7, không đếm bằng mắt |

## Security Considerations

`GetDescriptionText()` là chuỗi do Revit sinh, có thể chứa tên phần tử và tên family của model khách hàng.
Log ghi vào `%LocalAppData%\HPRebar\logs\` — máy người dùng, giữ 7 ngày theo cấu hình Serilog hiện có.
**Không** đẩy nội dung này ra dialog, không gửi đi đâu, không đính kèm vào report trong `plans/`.
Không thêm bề mặt I/O hay mạng nào khác.

## Next Steps

- Phase này độc lập, merge trước hay sau Phase 1–3 đều được.
- Sau khi có fixture, chạy lại toàn bộ tiêu chí BLOCKED của cả 4 phase trong một lượt.
