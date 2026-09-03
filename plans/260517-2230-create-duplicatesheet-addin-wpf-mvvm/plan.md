---
title: "Tạo Revit Add-In DuplicateSheet (WPF + MVVM Toolkit, multi-version R22-R27)"
description: "Add-in cho phép user multi-select sheet trong Revit, cấu hình rule rename (prefix/suffix/replace), chọn duplicate mode (SheetOnly / WithDetachedViews / WithDependentViews), preview kết quả, execute trong 1 Transaction. Stack: Nice3point.Revit.Sdk + WPF + CommunityToolkit.Mvvm + Serilog, hỗ trợ Revit 2022–2027."
status: pending
priority: P2
created: 2026-05-17
owner: hoang.tran
scope: project
---

# Plan: Revit Add-In **DuplicateSheet** (WPF + MVVM Toolkit, R22–R27)

## 1. Mục tiêu (Why)

Đây là **add-in feature đầu tiên** trên project. Hiện repo mới chỉ có `.claude/` đã refactor cho stack Revit (plan `20260517-claude-revit-refactor`) + docs (`docs/code-standards.md` + `docs/system-architecture.md`) — **chưa có project .csproj nào**.

Add-in **DuplicateSheet** giải bài toán phổ biến: user phải tạo nhiều sheet giống nhau cho các tầng / khu / phase khác nhau, copy-paste viewport thủ công rất chậm và dễ lệch position. Add-in này:

- Chọn 1–N sheet gốc → cấu hình rule rename → duplicate đồng loạt trong 1 Transaction.
- 3 duplicate mode: **SheetOnly** (sheet rỗng, giữ titleblock + revisions), **WithDetachedViews** (clone view detached), **WithDependentViews** (clone view + dependent views).
- Multi-version R22–R27 (preprocessor split cho `ElementId.IntegerValue` vs `Value`).

## 2. Phạm vi (What)

### Trong scope (v1)

- ✅ External Application Nice3point template → register ribbon panel `Sheet Tools` với 1 button `Duplicate Sheets`.
- ✅ External Command `DuplicateSheetCommand` → mở modal WPF.
- ✅ ViewModel có: list sheets (multi-select), naming rule (prefix/suffix/find-replace/increment), duplicate mode (enum), preview list.
- ✅ Service `SheetDuplicator`: wrap Transaction, copy titleblock, copy viewport positions, optional view duplication.
- ✅ Theme Dark + Light với DynamicResource, switch runtime qua `IThemeService`.
- ✅ Test pure-logic: `NamingRuleEngine` (xUnit out-of-process).
- ✅ Multi-version: 12 configs (`Debug.R22..R27` + `Release.R22..R27`).
- ✅ Serilog logging.

### Ngoài scope (defer v2+)

- ❌ Schedule (`ScheduleSheetInstance`) duplicate — v1 chỉ copy viewport (image views). Schedule placement reserve cho v2.
- ❌ Cross-project sheet copy (chỉ trong cùng `Document`).
- ❌ Modeless dialog + ExternalEvent (modal là đủ cho op < 30s).
- ❌ TUnit in-process test — defer; v1 chỉ xUnit pure-logic.
- ❌ Installer/.msi/Autodesk bundle — defer; v1 chỉ DeployAddin local F5.
- ❌ Localization (chỉ tiếng Anh v1).

## 3. Quyết định đã chốt (locked-in)

| # | Quyết định | Nguồn |
|---|---|---|
| 1 | **Project name:** `RevitDuplicateSheet` (folder + csproj + addin name) | Project convention |
| 2 | **Revit versions:** 2022–2027 (6 versions, 12 configs) | `docs/code-standards.md §7` |
| 3 | **Target framework:** R22–R24 → `net48`, R25–R27 → `net8.0-windows` | Nice3point standard |
| 4 | **DI mode:** `container` (Microsoft.Extensions.DI) | Project default |
| 5 | **UI pattern:** Modal `ShowDialog()` (op < 30s, blocking OK) | UX analysis |
| 6 | **Theme:** Dark + Light, DynamicResource, runtime swap | Project default |
| 7 | **Logging:** Serilog file sink `%LocalAppData%/RevitDuplicateSheet/logs/` | Project default |
| 8 | **Test:** xUnit pure-logic cho `NamingRuleEngine`. TUnit defer | Lean v1 |
| 9 | **Duplicate modes:** `SheetOnly` / `WithDetachedViews` / `WithDependentViews` | Standard Revit ViewDuplicateOption |
| 10 | **Naming rule:** Prefix + Suffix + Find/Replace + Increment number — combinable | UX scope |
| 11 | **Manifest type:** `application` (cần CreateRibbon, không phải command-only) | Nice3point template choice |
| 12 | **Transaction:** Single `Transaction` wrap toàn batch — fail = rollback all | Atomic UX |

## 4. Phases

| Phase | Name | Status | Est | Why |
|-------|------|--------|-----|-----|
| 1 | [Scaffold Nice3point project](./phase-01-scaffold-nice3point-project.md) | Pending | 30' | Tạo project base, .addin, App.xaml, ribbon |
| 2 | [Multi-version config R22-R27](./phase-02-multi-version-config-r22-r27.md) | Pending | 45' | Configs + preprocessor + helper shim |
| 3 | [Architecture (DI + Folders + Ribbon)](./phase-03-architecture-di-folders-ribbon.md) | Pending | 1h | HostingConfiguration, folder layout, ribbon UI |
| 4 | [WPF UI (View + ViewModel + Styles)](./phase-04-wpf-ui-view-viewmodel-styles.md) | Pending | 2h | XAML view, VM (ObservableProperty/RelayCommand), Theme.xaml + 2 sub-themes |
| 5 | [Revit API integration (SheetDuplicator + Transaction)](./phase-05-revit-api-integration-sheetduplicator-transaction.md) | Pending | 2h | `SheetDuplicator` service, ViewSheet.Create, Viewport copy, transaction |
| 6 | [Test + Deploy](./phase-06-test-deploy.md) | Pending | 1h | xUnit `NamingRuleEngine`, F5 smoke, build Release.R27 |

**Tổng effort ước tính:** ~7h15'

## 5. High-Level Architecture

```
Revit Process
└─ RevitDuplicateSheet.dll
   ├─ Application.cs (Nice3point ExternalApplication)
   │   ├─ OnStartupAsync()
   │   │   ├─ Setup Serilog
   │   │   ├─ Build DI container (HostingConfiguration)
   │   │   └─ CreateRibbon() → Panel "Sheet Tools" → Button "Duplicate Sheets"
   ├─ Commands/DuplicateSheetCommand.cs (ExternalCommand, Transaction.Manual)
   │   └─ Resolve DuplicateSheetView from DI → ShowDialog(Owner = Revit main window)
   ├─ Views/DuplicateSheetView.xaml + .cs
   │   └─ Bind DataContext = DuplicateSheetViewModel (DI injected)
   ├─ ViewModels/DuplicateSheetViewModel.cs
   │   ├─ [ObservableProperty] ObservableCollection<SheetItem> Sheets
   │   ├─ [ObservableProperty] NamingRule Rule
   │   ├─ [ObservableProperty] DuplicateMode Mode
   │   ├─ [ObservableProperty] ObservableCollection<PreviewItem> Preview
   │   └─ [RelayCommand] async Task ExecuteAsync(CancellationToken)
   ├─ Services/
   │   ├─ ISheetDuplicator + SheetDuplicator (Revit API)
   │   ├─ INamingRuleEngine + NamingRuleEngine (pure logic, xUnit testable)
   │   └─ IThemeService + ThemeService (swap MergedDictionary)
   ├─ Models/ (POCO: SheetItem, NamingRule, PreviewItem, DuplicateMode enum)
   ├─ Helpers/ElementIdHelper.cs (multi-version shim)
   └─ Resources/
       ├─ Icons/ (16/32 PNG)
       └─ Themes/ (Theme.xaml + ThemeDark.xaml + ThemeLight.xaml + Buttons.xaml + ...)
```

## 6. Risk Summary

| Risk | Mitigation | Phase |
|---|---|---|
| Revit API `ViewSheet` không có `Duplicate()` method native | Implement manual: `ViewSheet.Create()` + loop `Viewport.Create` + `view.Duplicate(option)` | Phase 5 |
| Viewport position lệch sau khi copy | Lấy `Viewport.GetBoxCenter()` + `Viewport.SetBoxCenter()` thay vì location point | Phase 5 |
| Multi-version preprocessor scatter khắp code | Tập trung shim trong `Helpers/ElementIdHelper.cs` + comment `// Multi-version: ...` | Phase 2 |
| Dependent views không clone đúng parent | Dùng `ViewDuplicateOption.WithDetachedViewsAndDependents` API (R2018+, OK cho R22–R27) | Phase 5 |
| Naming collision khi rule generate trùng tên sheet hiện có | Validate trong preview, hiển thị warning row màu đỏ, block Execute | Phase 4 |
| Theme switch không refresh do StaticResource | Hard-rule: mọi color/brush dùng `DynamicResource` | Phase 4 |
| F5 không launch Revit | Verify `<LaunchRevit>true</LaunchRevit>` + cài Revit đúng version cho config | Phase 6 |

## 7. Success Criteria (toàn plan)

- [ ] `dotnet build -c Debug.R27` pass (HARD-GATE) — file `bin/Debug.R27/RevitDuplicateSheet.dll` xuất hiện.
- [ ] `dotnet build -c Release.R22..R27` (6 configs) đều pass.
- [ ] F5 launch Revit 2027 → thấy ribbon panel `Sheet Tools` + button `Duplicate Sheets`.
- [ ] Click button → modal hiện, danh sách sheet load, multi-select hoạt động.
- [ ] Chọn 3 sheet + rule `Prefix=COPY_` + Mode `SheetOnly` → preview hiển thị 3 row `Original → COPY_<name>`.
- [ ] Click Execute → 3 sheet mới xuất hiện trong Revit Browser, titleblock giữ, không có viewport.
- [ ] Mode `WithDetachedViews` → sheet mới có viewport, view detach khỏi parent.
- [ ] xUnit test cho `NamingRuleEngine`: ≥ 8 test case (prefix, suffix, find-replace, increment, combine, edge cases).
- [ ] Theme switch Dark ↔ Light runtime không cần restart dialog.
- [ ] Multi-version: build R22 (net48) + R27 (net8.0-windows) đều OK, `ElementId.IntegerValue/Value` chia qua `#if`.
- [ ] Serilog log file xuất hiện ở `%LocalAppData%\RevitDuplicateSheet\logs\addin-2026-05-17.log` sau lần chạy đầu.

## 8. Documentation Impact

| File | Action |
|---|---|
| `docs/code-standards.md` | Giữ nguyên — đã đủ cho v1 |
| `docs/system-architecture.md` | Cập nhật ví dụ từ `MyAddIn` → `RevitDuplicateSheet` (tùy chọn, sau Phase 6) |
| `docs/project-changelog.md` | Tạo mới — log v1.0.0 release |
| `docs/development-roadmap.md` | Tạo mới — roadmap v2 (Schedule support, modeless, installer) |

## 9. Dependencies

Cross-plan: **không** chặn / không bị chặn bởi plan khác trong repo. Plan `20260517-claude-revit-refactor` đã merge skills/rules Revit; plan này là **feature đầu tiên** consume các skill đó.

## 10. Suggested Next Step After Plan Approval

```
/bs:cook plans/260517-2230-create-duplicatesheet-addin-wpf-mvvm
```

Cook sẽ execute tuần tự Phase 1 → 6, HARD-GATE `dotnet build` sau mỗi phase chạm code.

---

## Open Questions

- Không có. Tất cả decisions đã lock-in ở §3 dựa trên project standards.
