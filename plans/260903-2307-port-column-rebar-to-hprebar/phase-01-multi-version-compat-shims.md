---
phase: 1
title: "Multi-version compat + R27 restore verify"
status: completed
priority: P1
effort: "0.5d"
dependencies: [0]
---

# Phase 1: Multi-version compat + R27 restore verify

## Overview
Chốt ma trận version, xác nhận package Revit 2027 restore được, dựng 3 helper nhỏ (unit, dialog, id) để mọi code Revit phía sau không phải rải `#if`. Source gốc target R2021 nên gần như không có API bị xóa — rủi ro thật nằm ở TFM (net48 vs net8) và package 2027 chưa có trên máy.

## Requirements
- Functional: build pass **cả 5 Debug config** R23–R27.
- Non-functional: mọi block `#if REVIT…` kèm comment `// Multi-version: <topic>`; không nested quá 2 cấp; API khác biệt gói trong helper, không rải trong service.

## Architecture
TFM do Nice3point.Revit.Sdk 6.2.3 quyết định — verified tại `~/.nuget/packages/nice3point.revit.sdk/6.2.3/Sdk/*.props`:
```xml
<TargetFramework Condition="$(RevitVersion) >= '2021'">net48</TargetFramework>
<TargetFramework Condition="$(RevitVersion) >= '2025'">net8.0-windows7.0</TargetFramework>
<TargetFramework Condition="$(RevitVersion) >= '2027'">net10.0-windows7.0</TargetFramework>
```

| Revit | TFM | Runtime | Ghi chú |
|---|---|---|---|
| R23, R24 | `net48` | .NET Framework 4.8 | `Polyfill` bù `record`/`init`/`MinBy` (contentFiles/cs/net461) |
| R25, R26 | `net8.0-windows7.0` | .NET 8 | Máy dev có Revit 2025 + 2026 → verify runtime thật ở đây |
| R27 | `net10.0-windows7.0` | **.NET 10** | `global.json` pin SDK `10.0.300` — khớp. Package `Nice3point.Revit.Api.RevitAPI 2027.*` **chưa có trong NuGet cache** (cache: 2020/2022/2023/2024/2026). Toolkit 2027.0.0 **đã có** |

> Skill `revit-addin/references/multi-version-strategy.md` ghi R2027 = `net8.0-windows` — **sai** so với SDK 6.2.3. Sửa ở bước 8.

API cần guard/đã verify (từ `reports/source-analysis-r01-columnsrebar.md` §11):
- `UnitTypeId`, `SpecTypeId` — R21+ ✅ không guard
- `Rebar.CreateFreeForm`, `CreateFromRebarShape`, `ScaleToBox`, `SetLayoutAsNumberWithSpacing` — ✅ R23–R26, R27 verify bước 2
- `ElementId.Value` (R24+) vs `IntegerValue` — chỉ cần nếu so sánh số; ưu tiên so `ElementId` trực tiếp → tránh guard
- `RebarHookOrientation` mất ở R27 — **không dùng** `CreateFromCurves`
- `System.Windows.Forms` — **không ref**; dùng `TaskDialog`

## Related Code Files
- Create: `HPRebar/HPRebar/Column Rebar/RevitUnits.cs` — `static double MmToFt(double)`, `FtToMm(double)`, `string FormatForDisplay(Document, double ft)`
- Create: `HPRebar/HPRebar/Column Rebar/RevitDialogs.cs` — `Error(title, msg)`, `Confirm(...)` wrap `TaskDialog`
- Create: `HPRebar/HPRebar/Column Rebar/ElementIdCompat.cs` — chỉ tạo nếu Phase 3/4 thật sự cần int id; mặc định KHÔNG tạo (YAGNI)
- Modify: `HPRebar/HPRebar/HPRebar.csproj` — nếu restore 2027 fail, xem bước 2

## Implementation Steps
1. `dotnet restore HPRebar.slnx` với `-p:Configuration=Debug.R27` → xem log package `Nice3point.Revit.Api.RevitAPI 2027.*`. Kiểm tra cache: `ls ~/.nuget/packages/nice3point.revit.api.revitapi/`.
2. Nếu 2027 không tồn tại trên nuget.org: (a) hỏi user có Revit 2027 SDK local không → `<Reference Include="RevitAPI"><HintPath>` guard `Condition="$(RevitVersion)=='2027'"`; (b) hoặc tạm bỏ R27 khỏi `<Configurations>` + `.slnx` và ghi vào plan.md Unresolved. **Không tự quyết** — đưa user chọn.
3. Nếu restore OK: `for c in R23 R24 R25 R26 R27; do dotnet build HPRebar.slnx -c Debug.$c; done` → bảng pass/fail.
4. Viết `RevitUnits`:
   ```csharp
   namespace HPRebar.ColumnRebar;

   internal static class RevitUnits
   {
       public static double MmToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
       public static double FtToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
       public static string Display(Document doc, double ft) =>
           UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ft, false);
   }
   ```
   Thay hoàn toàn `DSP.UnitProject` + `double.Parse(UnitFormatUtils.Format(...))` (bug B6).
5. Viết `RevitDialogs` (TaskDialog wrapper, có `MainIcon = TaskDialogIcon.TaskDialogIconError`).
6. Grep source gốc lần cuối để chắc không sót API lạ: `grep -rhoE "Autodesk\.Revit\.DB\.[A-Za-z.]+" R01_ColumnsRebar --include=*.cs | sort -u` → đối chiếu XML doc 2023 + 2026 (lệnh grep đã dùng khi research). Ghi kết quả vào `reports/api-surface-check.md`.
7. Build gate 5 config lần nữa sau khi thêm helper.
8. Sửa `.claude/skills/revit-addin/references/multi-version-strategy.md`: bảng TFM ghi 2027 = `net8.0-windows`, thực tế SDK 6.2.3 map `>= 2027 → net10.0-windows7.0`. Commit type `fix` (không dùng `chore`/`docs` cho file trong `.claude`, theo CLAUDE.md).

## Success Criteria
- [x] `dotnet build -c Debug.R23/R24/R25/R26/R27` đều pass (hoặc R27 có quyết định user rõ ràng ghi trong plan.md)
- [x] `reports/api-surface-check.md` liệt kê mọi API Revit source dùng + trạng thái R23/R27
- [x] Không còn `System.Windows.Forms` trong bất kỳ csproj/cs
- [x] `grep -rn "Multi-version:" HPRebar/HPRebar` khớp số block `#if REVIT` (0 = 0 — khong can guard nao)
- [x] `multi-version-strategy.md` bảng TFM khớp SDK 6.2.3

## Risk Assessment
- **Package 2027 chưa publish / tên khác** → bước 2, hỏi user. Không block Phase 2 (Core không phụ thuộc).
- **net48 thiếu `System.Index`/`Range`, `init`** → Polyfill đã có trong HPRebar.csproj; Core cũng thêm.
- **`UnitFormatUtils.Format` đổi signature** — verified R23/R26 giống; nếu R27 khác → gói trong `RevitUnits.Display` duy nhất 1 chỗ sửa.

## Cook notes (2026-09-03)

### Buoc 1-3: R27 restore + build 5 config
`Nice3point.Revit.Api.RevitAPI 2027.2.0` + `RevitAPIUI 2027.2.0` **co tren nuget.org**, restore OK. TFM resolve dung `net10.0-windows7.0`. Buoc 2 (hoi user bo R27) **khong can chay**.

| Config | Ket qua |
|---|---|
| `Debug.R23` (net48) | 0 error |
| `Debug.R24` (net48) | 0 error |
| `Debug.R25` (net8.0-windows7.0) | 0 error |
| `Debug.R26` (net8.0-windows7.0) | 0 error |
| `Debug.R27` (net10.0-windows7.0) | 0 error |

### Buoc 6: API surface — ket qua quan trong nhat
`reports/api-surface-check.md`: **106 type Revit** duoc source dung, **0 type mat** giua R23 / R26 / R27, **0 khac biet ten member**. => **khong can bat ky `#if REVIT` nao** cho phan da port. `ElementIdCompat.cs` khong tao (YAGNI, dung nhu plan noi).

Targeted check xac nhan 2 claim cua plan:
- `RebarHookOrientation` **that su bien mat o R27** (co o R23/R26). Source **khong dung** no (grep 0 hit) -> khong anh huong.
- `Rebar.CreateFromCurves` overload: 4 (R23) -> 6 (R26) -> **2 (R27)**. Quyet dinh **D3 (khong dung CreateFromCurves) la dung**.
- `CreateFreeForm` / `CreateFromRebarShape` co du ca 3 version.

**Sua sai sot cua Validation Log:** `ScaleToBox` + `SetLayoutAsNumberWithSpacing` **khong phai member cua `Rebar`**. Chung nam o `RebarShapeDrivenAccessor` / `RebarFreeFormAccessor`, lay qua `rebar.GetShapeDrivenAccessor()` / `GetFreeFormAccessor()`. Signature giong het R23 va R27. Phase 4 phai goi qua accessor.

### Buoc 8
`.claude/skills/revit-addin/references/multi-version-strategy.md` bang TFM da sua: 2027 = `net10.0-windows7.0` (.NET 10), 2025/2026 them hau to `7.0`. Kem trich nguon SDK props.

### Han che cua sweep
Chi so sanh type-level + ten member. **Khong** bat duoc doi signature khi ten member con nguyen (chinh vi vay `CreateFromCurves` phai check tay). Xem muc Caveats trong report.
