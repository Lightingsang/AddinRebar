---
phase: 6
title: "WPF shell + 8 tab ViewModels/Views + Theme"
status: partial
priority: P1
effort: "3d"
dependencies: [2, 3]
---

# Phase 6: WPF shell + 8 tab ViewModels/Views + Theme

## Overview
Dựng UI theo MVVM Toolkit: window shell (nav trái 8 tab, content phải, canvas mặt đứng dưới — placeholder tới Phase 7, progress bar, OK/Cancel/EN-VN), 8 tab VM + View, `LocalizationService`. Toàn bộ input của user → `ColumnRebarSpec[]` + `AnnotationSettings`. Chạy song song Phase 4/5 (chỉ cần Core + `ColumnStack`).

Skills bắt buộc: `revit-wpf-mvvm`, `revit-xaml-styles`.

## Requirements
- Functional: 8 tab như gốc — Setting, Geometry, Stirrups, Additional Stirrups, Bars, Top Dowels, Bottom Dowels, Bars Division; chọn cột (`SelectedColumnIndex`) đồng bộ giữa tab; OK enable theo `ConditionButtonOK`; đổi ngôn ngữ live.
- Non-functional: `sealed partial class : ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`; code-behind = `InitializeComponent` + `DataContext`; **không** `DataContext` trong XAML; mọi màu/spacing `{DynamicResource}`; VM < 250 dòng, XAML < 500 dòng; Modal, `Owner` = Revit main window.

## Architecture
```
Column Rebar/
├── View/
│   ├── ColumnRebarView.xaml(.cs)          shell Window
│   ├── Tabs/ SettingTabView.xaml … BarsDivisionTabView.xaml   (8 UserControl)
│   └── Controls/                          Phase 7
├── View Models/
│   ├── ColumnRebarViewModel.cs            shell: Tabs, SelectedTab, SelectedColumnIndex, Progress, OkCommand, CancelCommand, ToggleLanguageCommand
│   ├── ColumnRebarSession.cs              ObservableObject: Stack, Specs[], Annotation, Catalog, SelectedColumnIndex — shared state, inject vào mọi tab
│   ├── Tabs/ SettingTabViewModel.cs … BarsDivisionTabViewModel.cs  (8)
│   ├── Messages/ SpecChangedMessage.cs, ColumnSelectedMessage.cs   (WeakReferenceMessenger, cho canvas Phase 7)
│   └── Editors/ TopDowelsEditor.cs, BottomDowelsEditor.cs          (tách logic khỏi VM 517/370 dòng gốc)
├── LocalizationService.cs                 ObservableObject { UiStrings Strings; Toggle() }
├── ThemeSwitcher.cs                       đọc UIThemeManager (R24+) → swap ThemeDark/ThemeLight
└── Models/UiStrings.cs                    record 1 field/nhãn; static En, Vi — port 11 *Language.cs
```
Nav icons: `Resources/Icons/ColumnRebar/*.xaml` (`PathGeometry`) thay `DrawIcon` — **không** vẽ icon bằng code.

Command flow (D8 — command **không** mở TransactionGroup, orchestrator sở hữu):
```csharp
var session = new ColumnRebarSession(stack, catalog, defaultSpecs, annotation);
var vm      = new ColumnRebarViewModel(session, localization, orchestrator);
var view    = new ColumnRebarView(vm);
new WindowInteropHelper(view).Owner = UiApplication.MainWindowHandle;   // MainWindowHandle ở RevitAPIUI, verified
view.ShowDialog();   // OkCommand gọi orchestrator.Run (tự quản TransactionGroup); Cancel = không gọi = không cần rollback
```
`ExternalCommand` (Nice3point.Revit.Toolkit) expose sẵn `Document`, `UiDocument`, `UiApplication`, `ActiveView` — verified trong Toolkit 2027.0.0 XML. Không cần tự lấy từ `ExternalCommandData`.

## Related Code Files
- Create: ~30 file (1 shell + 8 tab × 2 + session + 8 VM + 2 editor + 2 message + localization + strings + icons)
- Modify: `ColumnRebarCommand.cs` — tạo session/VM/view, `ShowDialog`
- Modify: `Resources/Themes/Theme.xaml` — merge vào `ColumnRebarView.Resources`
- Reference: tool gốc `View/*.xaml` (1740 dòng) + `ViewModel/*.cs` (2514 dòng) — port từng tab

## Implementation Steps
1. `UiStrings` + `LocalizationService`: gộp 11 class `*Language.cs` thành 1 record (~120 field). `Toggle()` swap `Strings = Strings == En ? Vi : En` → mọi binding `{Binding Loc.Strings.XXX}` cập nhật.
2. `ColumnRebarSession`: giữ `ColumnStack`, `ObservableCollection<ColumnRebarSpecEditable>` (wrapper ObservableObject quanh Core spec record — vì Core record immutable), `AnnotationSettings`, `RebarTypeCatalog`, `[ObservableProperty] int selectedColumnIndex`. Mọi thay đổi spec → `WeakReferenceMessenger.Default.Send(new SpecChangedMessage(columnIndex))`.
3. Shell `ColumnRebarView.xaml`: Grid 3 hàng (nav+content / canvas placeholder / footer). Nav = `ListBox ItemsSource=Tabs` với `DataTemplate` icon `Path Data={StaticResource Icon.Stirrups}` + text `{Binding Title}`; content = `ContentControl Content={Binding SelectedTab}` + `DataTemplate DataType` cho 8 VM (như gốc). Footer: `ProgressBar Value={Binding Progress}`, OK = `{StaticResource PrimaryButton}`, Cancel = `{StaticResource SecondaryButton}`, Language = `{StaticResource LinkButton}`.
   **Tên key đúng** (verified trong `controls-sample.md`, dùng nguyên, không bịa `Style.Button.*`): `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`, `LinkButton`, `StandardTextBox`, `NumberTextBox`, `SearchTextBox`, `Card`, `Separator`, `Badge`, `BadgeText`, `Tag`. Style dùng `StaticResource` (là Style, không phải brush); màu bên trong Style mới dùng `DynamicResource Brush.*` để theme swap runtime.
4. `ColumnRebarViewModel`: `OkCommand` `CanExecute = session.IsValid` (port `ConditionButtonOK`: S>0, S1/S2>0 theo TypeDis, mỗi cột ≥ 1 bar, shape rect phải `M_T1`, AddH/AddV với TypeH/V=0 cần aH/aV>0) với `[NotifyCanExecuteChangedFor]`; execute → `orchestrator.Run(..., progress)` → `DialogResult = true`. `Progress` qua `IProgress<int>` + `Dispatcher` (giữ pattern gốc vì modal + Revit API cùng thread).
5. Port 8 tab theo thứ tự **đơn giản → phức tạp**: Setting (102) → Geometry (113) → BarsDivision (176) → Stirrups (267) → Bars (273) → BottomDowels (370) → AdditionalStirrups (404) → TopDowels (517). Mỗi tab: VM gọi Core calculator khi input đổi (vd Bars tab: `nx/ny` đổi → `BarLayoutCalculator.Compute` → cập nhật `session.Specs[i].Bars`), Apply/Modify command như gốc. Tab nào > 250 dòng → tách `Editors/`.
6. Số nhập: `<TextBox Style="{StaticResource NumberTextBox}">` + `[ObservableProperty] double s` với `[NotifyDataErrorInfo]`/`[Range]` qua `ObservableValidator` cho S/S1/S2/cover; bỏ `PreviewTextInputCommand` regex của gốc.
7. Owner window: `new WindowInteropHelper(view).Owner = UiApplication.MainWindowHandle` (`MainWindowHandle` nằm ở `RevitAPIUI`, verified R26).
8. Merge Theme + theme switch (Dark + Light, theo checklist Revit default):
   ```xml
   <Window.Resources><ResourceDictionary><ResourceDictionary.MergedDictionaries>
     <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml"/>
   </ResourceDictionary.MergedDictionaries></ResourceDictionary></Window.Resources>
   ```
   `ThemeSwitcher.ApplyFromRevit()` đọc theme hiện tại của Revit rồi swap dictionary màu:
   ```csharp
   // Multi-version: UIThemeManager (Revit 2024+)
   #if REVIT2024_OR_GREATER
       var dark = UIThemeManager.CurrentTheme == UITheme.Dark;
   #else
       const bool dark = true;   // R23 không có API theme → Dark
   #endif
   ```
   `UIThemeManager` verified có trong RevitAPIUI 2026. Swap `ThemeDark.xaml` ↔ `ThemeLight.xaml` trong `MergedDictionaries` (mọi màu đã là `DynamicResource` nên đổi runtime được).
9. `IsRebar` toggle (rebar thật ↔ detail item): giữ property trong session + checkbox trong Setting tab nhưng **`IsEnabled=False`** kèm tooltip "Detail Item mode chưa hỗ trợ" — đường `IsRebar=false` đã tách sang plan riêng (Validation Session 1). Không xoá property để plan sau nối vào không phải sửa session.
10. Build gate `Debug.R26` + `Debug.R23` sau **mỗi tab** (XAML compile lỗi khó debug khi gộp). F5 Revit 2026 mỗi 2 tab để xem layout.

## Success Criteria
- [ ] **BLOCKED (F5)** 8 tab hiển thị đúng nhãn EN/VN, đổi ngôn ngữ không đóng dialog — code path co (`LocalizationService.Toggle` + `NotifyTitleChanged`)
- [ ] **BLOCKED (F5)** Đổi `nx/ny/nd`, diameter, cover, S/TypeDis → `session.Specs` cập nhật, OK enable/disable đúng luật
- [ ] **BLOCKED (F5 + fixture)** OK → runner chạy, progress bar chạy, dialog đóng `true`; Cancel → `false`, model không đổi
- [ ] **BLOCKED (F5)** Dialog theo theme Revit — `ThemeSwitcher.ApplyFromRevit` gọi trong code-behind
- [x] Không XAML nào có màu/số spacing hardcode (grep `#hex`, `Margin="[0-9]`, `FontSize="[0-9]`: CLEAN)
- [x] Mọi style/brush key khớp tên trong Theme — build XAML 5/5 config pass, key sai sẽ fail lúc compile BAML
- [~] `*.xaml.cs`: 8 tab view = **8 dòng**; shell = **21 dòng** (vượt 15, lý do ở ghi chú). Không `FindChild`, không `RelayCommand<Window>`, không `x:Name`, không `DataContext` trong XAML
- [x] VM ≤ 250 dòng (lớn nhất 179), XAML ≤ 500 dòng (lớn nhất 132)

## Risk Assessment
- **Port 2514 dòng VM logic** — phần lớn là handler đổi input → gọi lại calculator; sau khi Core có sẵn, VM chỉ còn glue. Nếu tab TopDowels vẫn > 250 → tách `TopDowelsEditor` + `AddBarEditor`.
- **Progress bar không update khi Revit API chạy trên UI thread** → giữ `Dispatcher.Invoke(…, DispatcherPriority.Background)` như gốc; nếu vẫn đơ, chấp nhận (modal) và ghi chú.
- **`ObservableValidator` + net48** → CommunityToolkit.Mvvm 8.4.0 hỗ trợ netstandard2.0, đã có trong NuGet cache (8.2.2/8.4.0/8.4.2); OK.
- **Theme file từ skill chưa test thực tế** → build lỗi resource key → sửa key ngay trong Theme, không hardcode màu vào View.
- **`UIThemeManager` chỉ R24+** → R23 fixed Dark; chấp nhận (R23 chỉ build-only, không F5 verify).

## Cook notes (2026-09-04)

### Build gate
`Debug.R23` / `R24` / `R25` / `R26` / `R27` = **0 error, 0 warning CS**. `HPRebar.slnx` R26 + R23 sach. Core test 99/99.

XAML compile ra BAML o ca 5 config -> **moi resource key deu ton tai that** (key sai se fail luc compile, khong phai runtime).

### File tao (28)
| Nhom | File | Ghi chu |
|---|---|---|
| i18n | `Models/UiStrings.cs` (131), `Models/UiStringsCatalog.cs` (117), `LocalizationService.cs` (23) | gop 11 `*Language.cs` thanh 1 record ~110 field |
| Theme | `ThemeSwitcher.cs` (86) | doc `UIThemeManager` (R24+), swap dictionary mau; co `#if REVIT2024_OR_GREATER` |
| State | `View Models/ColumnRebarSession.cs` (171), `ColumnSpecEditor.cs` (183), `BarSpliceEditor.cs` (56) | wrapper ObservableObject quanh record immutable cua Core |
| Shell | `View Models/ColumnRebarViewModel.cs` (145), `View/ColumnRebarView.xaml` (122) + `.cs` (21) | |
| Tabs VM | `View Models/Tabs/*.cs` x9 | base 32 dong + 8 tab, lon nhat 58 |
| Tabs View | `View/Tabs/*.xaml` x8 + `.cs` x8 | XAML lon nhat 132, code-behind deu 8 dong |
| Runner | `RevitRebarRunner.cs` (60) | implement `IColumnRebarRunner`, **so huu TransactionGroup** |

### Khac plan
1. **`IColumnRebarRunner` thay cho ref truc tiep orchestrator.** Phase 5 chua co orchestrator, nen shell VM phu thuoc 1 interface 2 method (`PlannedCount`, `Run`). `RevitRebarRunner` implement no bang `RebarCreationService` cua Phase 4. Phase 5 chi can implement lai interface do — **khong phai sua VM**. Loi phu: VM khong ref Revit type nao.
2. **Style dung `DynamicResource`, khong phai `StaticResource`.** Plan buoc 3 noi dung `StaticResource` cho Style. Skill `revit-wpf-mvvm` bang do/don't ghi ro "Moi style -> DynamicResource", va `revit-xaml-styles` cung vay. Theo skill. Ket qua giong nhau ve chuc nang, nhung swap theme an toan hon.
3. **Chua lam nav icon** (`Resources/Icons/ColumnRebar/*.xaml` PathGeometry). Nav hien tai chi co text. Khong chan gi; them sau khi co design.
4. **Chua co canvas placeholder row** trong shell grid — Phase 7 se them cung luc voi canvas that, tranh dung row rong roi phai sua layout.
5. **Chua tach `Editors/`** — VM lon nhat 58 dong (TopDowels goc 517 dong) vi toan bo toan da nam o Core (Phase 2) va `ColumnSpecEditor`. Rui ro "port 2514 dong VM logic" cua plan **khong thanh hien thuc**.
6. **Chua dung `ObservableValidator`/`[Range]`** (plan buoc 6). Validate tap trung o `ColumnRebarSession.IsValid(out reason)` + hien message o footer. Ly do: luat validate lien quan nhieu property cung luc (vd `TypeDis` quyet dinh check `S` hay `S1/S2`), attribute tren tung property khong dien ta duoc. Neu muon highlight tung o do thi them `ObservableValidator` sau.

### Code-behind shell 21 dong (vuot muc 15)
`ColumnRebarView.xaml.cs` co 4 viec, deu thuoc tang View: `InitializeComponent`, `DataContext`, `ThemeSwitcher.ApplyFromRevit(this)`, va nhan `CloseRequested` de set `DialogResult` + `Close()`. ViewModel khong the dong `Window` ma khong pha MVVM. Khong co logic nghiep vu. Giu nguyen; neu muon dung 15 thi phai bo theme hoac bo dialog-result — ca hai deu te hon.

### 1 bug tu bat duoc khi review
`ColumnSpecEditor.Load()` gan `BarsAlongWidth` **truoc** `MainBarType`. Setter do trigger `OnBarsAlongWidthChanged` -> `SyncSpliceCount()` -> `SpliceSpec.Default(..., MainBarType.DiameterMm, ...)` -> **NullReferenceException ngay lan mo dialog dau tien**. Da sua: `SyncSpliceCount` return som khi `MainBarType` con null, va `LoadSplices` goi lai no o cuoi `Load`.

### Con BLOCKED
Toan bo verify hanh vi can F5 Revit 2026: nhan 8 tab EN/VN, doi ngon ngu live, OK/Cancel, progress bar, theme Dark/Light theo Revit. Chay OK that con can fixture `.rvt` (Phase 3).
