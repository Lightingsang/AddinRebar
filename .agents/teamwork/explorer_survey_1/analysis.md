# Báo Cáo Khảo Sát & Phân Tích Hệ Thống Styling / Theming HPRebar
## Core Theme, Style Dictionaries, Font Configuration & Custom Styles Inventory
**Mã Báo Cáo:** `SURVEY-HPREBAR-THEME-CORE-01`  
**Ngày Khảo Sát:** 2026-10-10  
**Tác Giả:** Explorer 1 (Theme Core Explorer)  
**Tình Trạng:** Hoàn tất khảo sát hiện trạng (Survey & Mapping Complete)  
**Đối Tượng Khảo Sát:** `HPRebar/HPRebar/` (Revit Add-in), `HPRebar/HPRebar.McpBridge/` (Revit MCP Bridge), `HPRebar/tools/theme-gallery/` (Visual Gate), `HPRebar.Core.Tests/Themes/` (Theme Tests).  

---

## 1. Bản Đồ Hiện Trạng Hệ Thống Giao Diện (Architecture Map)

### 1.1. Cấu Trúc Thư Mục Tài Nguyên (Resource Directory Structure)
Toàn bộ tài nguyên giao diện, từ điển màu sắc và hạ tầng cầu nối MaterialDesign được tổ chức tại hai vị trí chính:
```
HPRebar/
├── HPRebar/
│   ├── Resources/
│   │   ├── Icons/
│   │   │   └── RibbonIcons.cs              ← Vẽ vector glyphs cho Ribbon & Window title-bar
│   │   └── Themes/
│   │       ├── Theme.xaml                  ← Master ResourceDictionary (merged vào các Window)
│   │       ├── MaterialBridge.xaml         ← Cầu nối MaterialDesign + CustomColorTheme + override Segoe UI
│   │       ├── MaterialThemeBridge.cs      ← C# static helper: chuyển đổi Dark/Light runtime theo Revit
│   │       ├── IHostTheme.cs               ← Interface trừu tượng trạng thái Dark/Light của host CAD/BIM
│   │       ├── RevitHostTheme.cs           ← C# adapter đọc UIThemeManager (Revit 2024+)
│   │       ├── ThemeInfo.cs                ← Attribute [assembly: ThemeInfo] cho ILRepack BAML Generic.xaml
│   │       ├── ThemeDark.xaml              ← Bảng mã màu & Brush chế độ Tối (Dark Palette)
│   │       ├── ThemeLight.xaml             ← Bảng mã màu & Brush chế độ Sáng (Light Palette)
│   │       ├── Controls.xaml               ← Custom styles: Card, Separator, Badge, BadgeText, Tag
│   │       ├── Typography.xaml             ← Font families, font sizes & TextBlock styles
│   │       ├── Spacing.xaml                ← Khoảng cách chuẩn lưới 4px (Thickness & Double)
│   │       └── ThemeIcons.xaml             ← 8 Vector ControlTemplates cho tab icons cột
│   ├── ColumnRebar/View/                   ← Window & 8 Tab UserControls
│   ├── BeamRebar/View/                     ← Window, 5 Tab UserControls, 3D Helix Viewport
│   ├── FoundationRebar/View/               ← Window, 2 Tab UserControls
│   ├── KataExport/View/                    ← Window chính (KataExportView) & Dialog (KataSettingsView)
│   ├── KataRebar/View/                     ← Window chính (KataRebarView)
│   └── RebarPartition/View/                ← Window chính (RebarPartitionView)
├── HPRebar.McpBridge/
│   ├── Resources/Themes/
│   │   └── Theme.xaml                  ← Master dictionary riêng của MCP Bridge (pack URI nội bộ)
│   │   (Các file khác: MaterialBridge.xaml, ThemeDark.xaml, v.v. được link trực tiếp qua .csproj)
│   └── View/
│       └── McpBridgeStatusView.xaml        ← Cửa sổ giám sát MCP Bridge
├── tools/theme-gallery/
│   ├── Program.cs                          ← Harness chạy off-Revit chụp ảnh mọi view ở 2 chế độ Dark/Light
│   └── KataElevationGallery.cs             ← Harness render chi tiết mặt đứng dầm Kata
└── HPRebar.Core.Tests/Themes/
    └── ThemeTokenCoverageTests.cs          ← xUnit tests kiểm tra tính toàn vẹn 100% của token XAML
```

### 1.2. Cơ Chế Đóng Gói & ILRepack trong Dự Án
- **`HPRebar/HPRebar/HPRebar.csproj`** (dòng 7, 28–30):
  - Khai báo `<IsRepackable>true</IsRepackable>` (Nice3point Revit SDK).
  - Sử dụng package `MaterialDesignThemes` phiên bản `5.3.2`.
  - Toàn bộ `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll` và `Microsoft.Xaml.Behaviors.dll` được ILRepack gộp trực tiếp vào `HPRebar.dll`.
  - File `ThemeInfo.cs` (dòng 6) chứa `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]` là điều kiện bắt buộc để WPF nhận diện `Themes/Generic.xaml` của toolkit sau khi gộp DLL.
- **`HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj`** (dòng 9, 28, 48–61, 66–74):
  - Không bật `IsRepackable` toàn cục (để cô lập Roslyn), nhưng có Target riêng `RepackMaterialDesign` chạy sau `CopyFilesToOutputDirectory` để gộp `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll` và `Microsoft.Xaml.Behaviors.dll` vào `HPRebar.McpBridge.dll`.
  - Sử dụng `<Page Include="..\HPRebar\Resources\Themes\*.xaml" Exclude="..\HPRebar\Resources\Themes\Theme.xaml" Link="..."/>` và `<Compile Link="..."/>` để dùng chung 100% tài nguyên theming với `HPRebar`.

---

## 2. Đối Soát Với Tiêu Chuẩn `revit-xaml-styles` & MaterialDesign 5.3.2

| Tiêu chí | Quy định chuẩn (`revit-xaml-styles` & R1–R5) | Hiện trạng mã nguồn `HPRebar` | Đánh giá & Phát hiện |
|---|---|---|---|
| **Toolkit Version** | Bắt buộc `MaterialDesignInXamlToolkit` v5.2.1 hoặc v5.3.2, style MD2 | Đang dùng `5.3.2` và `MaterialDesign2.Defaults.xaml` | **ĐẠT** |
| **Font chữ** | Bắt buộc ghi đè `MaterialDesignFont` thành `Segoe UI`. CẤM Roboto pack URI. | `MaterialBridge.xaml:20`: `<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>` | **ĐẠT**. Đã kiểm tra: 0 tham chiếu `{md:MaterialDesignFont}`, 0 tham chiếu Roboto trong toàn bộ XAML. |
| **Thứ tự Merge Master Theme** | `MaterialBridge.xaml` phải đứng ĐẦU TIÊN trong `Theme.xaml`. | `Theme.xaml:8`: `MaterialBridge.xaml` đặt ở vị trí thứ 1, trước `ThemeDark.xaml`. | **ĐẠT** (`ThemeTokenCoverageTests` có test khóa chặt thứ tự này). |
| **Đổi Theme Runtime không có App.Current** | Dùng `MaterialThemeBridge.Attach(this, hostTheme)` hoặc `Apply(this, isDark)` hoán đổi overlay dictionary ở cấp Window. | Cả 8 Window đều gọi `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, ...)` trong code-behind. | **ĐẠT**. Cơ chế hoạt động tốt, không gọi `Application.Current` hay `PaletteHelper`. |
| **Màu nhận diện thương hiệu** | Primary `#0696D7` (Revit Blue), Secondary `#E0641E` (Steel Orange). | `MaterialBridge.xaml:12`: `<md:CustomColorTheme BaseTheme="Dark" PrimaryColor="#0696D7" SecondaryColor="#E0641E"/>` | **ĐẠT** |
| **Màu bề mặt & Controls (R3)** | Dùng theme resources của toolkit cho background, card, border, popup; KHÔNG dùng palette HP ghi đè toàn bộ màu bề mặt toolkit. | `MaterialThemeBridge.cs:63–69` ghi đè `theme.Background`, `theme.Cards.Background`, `theme.ToolTips.Background` bằng `Color.*` của HP; các Window dùng `Brush.Background`, `Brush.Surface`, `Brush.Border`. | **CẦN ĐIỀU CHỈNH**. Cần để toolkit quản lý màu surface/card/popup tự nhiên theo BaseTheme Dark/Light của toolkit, kết hợp đồng bộ tokens. |
| **Implicit Styles cho Controls chuẩn** | Cung cấp implicit styles (`TargetType="Button"`, `TextBox`, `ComboBox`, `CheckBox`, `DataGrid`, v.v.) trong `Theme.xaml` để các control mặc định tự động đẹp. | `Theme.xaml` HIỆN CHƯA CÓ implicit styles cho controls. Các Window phải gắn key thủ công hoặc để control trần (raw WPF). | **LỖ HỔNG LỚN (GAP)**. Cần bổ sung hệ thống implicit styles vào `Theme.xaml` như chuẩn AutoCAD/Dynamo. |
| **Phân cấp Button (Tiered Buttons)** | Primary (Raised), Secondary (Outlined), Danger (Red), IconButton, LinkButton. | `MaterialBridge.xaml:23–60` đã định nghĩa đủ 5 keys trên, dựa trên toolkit. | **ĐẠT**. Tuy nhiên nhiều Window gọi tên khác hoặc viết Button không có Style. |
| **Input Controls Outlined** | `TextBox` và `ComboBox` dạng Outlined có CornerRadius=4 và Hint nổi (`md:HintAssist.Hint`). | `StandardTextBox`, `NumberTextBox`, `SearchTextBox` đã có trong `MaterialBridge.xaml`. Nhưng **THIẾU** `StandardComboBox`. | **THIẾU SÓT**. Cần bổ sung `StandardComboBox` vào `MaterialBridge.xaml`. |
| **Card phân tầng** | Dùng `md:Card` hoặc `Border Style="{StaticResource Card.Panel}"`. | Đang dùng `<Style x:Key="Card" TargetType="Border">` trong `Controls.xaml`, và nhiều View tự chế `CardBorderStyle`. | **CẦN CHUẨN HÓA**. Chuyển đổi/aliasing sang chuẩn `Card.Panel` và hỗ trợ `md:Card`. |

---

## 3. Khảo Sát Chi Tiết Các File Core Theming Trong `HPRebar/Resources/Themes/`

### 3.1. `MaterialBridge.xaml` (`HPRebar/Resources/Themes/MaterialBridge.xaml`)
- **Dòng 11–14:**
  ```xml
  <ResourceDictionary.MergedDictionaries>
      <md:CustomColorTheme BaseTheme="Dark" PrimaryColor="#0696D7" SecondaryColor="#E0641E"/>
      <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml"/>
  </ResourceDictionary.MergedDictionaries>
  ```
- **Dòng 20:** `<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>` (Ghi đè Roboto thành Segoe UI).
- **Dòng 23–60:** Các Style Button kế thừa toolkit:
  - `PrimaryButton` (TargetType="Button", BasedOn="{StaticResource MaterialDesignRaisedButton}")
  - `SecondaryButton` (TargetType="Button", BasedOn="{StaticResource MaterialDesignOutlinedButton}")
  - `DangerButton` (TargetType="Button", BasedOn="{StaticResource PrimaryButton}", Background Brush.Danger)
  - `IconButton` (TargetType="Button", BasedOn="{StaticResource MaterialDesignIconButton}", Width=32, Height=32)
  - `LinkButton` (TargetType="Button", BasedOn="{StaticResource MaterialDesignFlatButton}")
- **Dòng 63–81:** Các Style TextBox kế thừa toolkit:
  - `StandardTextBox` (TargetType="TextBox", BasedOn="{StaticResource MaterialDesignOutlinedTextBox}")
  - `NumberTextBox` (TargetType="TextBox", BasedOn="{StaticResource StandardTextBox}", căn phải, font mono)
  - `SearchTextBox` (TargetType="TextBox", BasedOn="{StaticResource StandardTextBox}", Hint="Tìm kiếm...")
- **Điểm thiếu hụt:**
  - Chưa có `StandardComboBox` (dựa trên `MaterialDesignOutlinedComboBox`).
  - Chưa có `Card.Panel` (Border card chuẩn có DropShadowEffect).

### 3.2. `Theme.xaml` (`HPRebar/Resources/Themes/Theme.xaml`)
- **Dòng 5–17:**
  ```xml
  <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/MaterialBridge.xaml"/>
      <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/ThemeDark.xaml"/>
      <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Typography.xaml"/>
      <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Spacing.xaml"/>
      <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Controls.xaml"/>
      <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/ThemeIcons.xaml"/>
  </ResourceDictionary.MergedDictionaries>
  ```
- **Điểm thiếu hụt:**
  - File này chỉ thuần túy merge 6 từ điển con. Hoàn toàn không chứa implicit styles cho các control cơ bản (`TextBox`, `ComboBox`, `Button`, `CheckBox`, `RadioButton`, `DataGrid`, `TabControl`, `Expander`, `ScrollViewer`).

### 3.3. `MaterialThemeBridge.cs` (`HPRebar/Resources/Themes/MaterialThemeBridge.cs`)
- **Dòng 33–40:** Phương thức `Attach(Window window, IHostTheme host, Func<bool, ImageSource>? icon = null)`:
  - Gọi `Apply(window, host.IsDark, icon)`.
  - Đăng ký lắng nghe sự kiện `host.Changed` qua `window.Dispatcher.BeginInvoke`.
  - Hủy đăng ký khi `window.Closed`.
- **Dòng 43–77:** Phương thức `Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)`:
  - Tạo `palette = new ResourceDictionary { Source = new Uri(dark ? DarkUri : LightUri) };`.
  - Tạo `overlay = new ResourceDictionary { [OverlayMarker] = true }; overlay.MergedDictionaries.Add(palette);`.
  - Dò tìm `IMaterialDesignThemeDictionary` (chính là `CustomColorTheme`):
    - Đọc `seedTheme = seed.GetTheme()`.
    - Sinh `theme = Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, seedTheme.PrimaryMid.Color, seedTheme.SecondaryMid.Color)`.
    - Đồng bộ `theme.Background`, `Foreground`, `ForegroundLight`, `ValidationError`, `Cards.Background`, `Cards.Border`, `ToolTips.Background`.
    - Gọi `overlay.SetTheme(theme)`.
  - Hoán đổi vị trí của `overlay` trong `window.Resources.MergedDictionaries`.
- **Đánh giá so với R3:**
  - Cơ chế hoán đổi `overlay` cấp Window hoạt động hoàn hảo mà không cần `Application.Current`.
  - Tuy nhiên, việc ép `theme.Background = Token(palette, "Color.Background")` và `theme.Cards.Background = Token(palette, "Color.SurfaceElevated")` cần được tinh chỉnh để tôn trọng dải màu tự nhiên của MaterialDesign theme theo yêu cầu R3.

### 3.4. `ThemeDark.xaml` & `ThemeLight.xaml`
- Định nghĩa 21 cặp `Color.*` và `SolidColorBrush` tương ứng `Brush.*`:
  - Background & Surface: `Brush.Background`, `Brush.Surface`, `Brush.SurfaceElevated`, `Brush.SurfaceHover`, `Brush.SurfacePressed`.
  - Foreground: `Brush.Foreground.Primary`, `Brush.Foreground.Secondary`, `Brush.Foreground.Tertiary`, `Brush.Foreground.Disabled`, `Brush.Foreground.OnAccent`.
  - Accent (Revit Blue): `Brush.Accent` (#0696D7), `Brush.Accent.Hover`, `Brush.Accent.Pressed`.
  - Border: `Brush.Border`, `Brush.Border.Focus`, `Brush.Border.Disabled`, `Brush.GridLine`.
  - Semantic: `Brush.Success` (#16C172), `Brush.Warning` (#F5A623), `Brush.Danger` (#E5484D), `Brush.Info` (#0696D7).
  - Canvas (Đặc thù vẽ cốt thép 2D): `Brush.Canvas.Fill`, `Brush.Canvas.MainBar`, `Brush.Canvas.MainBar.Selected`, `Brush.Canvas.Stirrup`, `Brush.Canvas.Bound`, `Brush.Canvas.Tag`.
- **Đánh giá:** Bộ token này rất đầy đủ và nhất quán giữa Dark và Light (được kiểm chứng bởi `ThemeTokenCoverageTests:37`). Tuyệt đối cần bảo toàn bộ token `Brush.*` này để các canvas kỹ thuật và text blocks không bị lỗi runtime.

### 3.5. `Controls.xaml` (`HPRebar/Resources/Themes/Controls.xaml`)
- Dòng 6–12: `Card` (Border với Background `Brush.SurfaceElevated`, BorderBrush `Brush.Border`, CornerRadius 6).
- Dòng 15–19: `Separator` (Border 1px làm đường kẻ ngang).
- Dòng 22–27: `Badge` (Border làm pill badge trạng thái, Background `Brush.Accent`, CornerRadius 10).
- Dòng 29–33: `BadgeText` (TextBlock chữ trắng nhỏ trong Badge).
- Dòng 36–42: `Tag` (Border làm nhãn nhỏ bo góc 3px).

### 3.6. `Typography.xaml` (`HPRebar/Resources/Themes/Typography.xaml`)
- Font Families: `Font.Family.Default` = "Segoe UI", `Font.Family.Mono` = "Cascadia Mono, Consolas".
- Font Sizes: Caption (11), Body (14), BodyStrong (14), Subheading (16), Heading (20), Title (28).
- Styles TextBlock: `Caption`, `Body`, `BodyStrong`, `Subheading`, `Heading`, `Title`.

### 3.7. `ThemeIcons.xaml` (`HPRebar/Resources/Themes/ThemeIcons.xaml`)
- Chứa 8 Vector `ControlTemplate` (Viewbox 32x32) vẽ bằng hình học Canvas/Line/Rectangle/Ellipse:
  - `TabIcon.Setting`, `TabIcon.Geometry`, `TabIcon.Stirrups`, `TabIcon.AdditionalStirrups`, `TabIcon.Bars`, `TabIcon.TopDowels`, `TabIcon.BottomDowels`, `TabIcon.BarsDivision`.
- Sử dụng các brush động: `Brush.Foreground.Primary`, `Brush.Foreground.Secondary`, `Brush.Danger`.
- Đây là tài nguyên vector chuyên dụng của add-in cho tab cột, cần bảo toàn 100%.

---

## 4. Kiểm Kê Toàn Diện 10 Điều Khiển Chuẩn (Controls Inventory)

Dưới đây là bảng thống kê chi tiết toàn bộ các điều khiển chuẩn xuất hiện trong toàn bộ các file XAML của `HPRebar/HPRebar` và `HPRebar.McpBridge`:

### 4.1. Button
| Vị trí (File & Dòng) | Style hiện tại | Phân loại | Đề xuất chuyển đổi / xử lý |
|---|---|---|---|
| `HPRebar.McpBridge/View/McpBridgeStatusView.xaml:84` | `{DynamicResource PrimaryButton}` | Primary CTA | Giữ nguyên key `PrimaryButton` |
| `HPRebar.McpBridge/View/McpBridgeStatusView.xaml:122` | `{DynamicResource LinkButton}` | Link Button | Giữ nguyên key `LinkButton` |
| `HPRebar.McpBridge/View/McpBridgeStatusView.xaml:150` | `{DynamicResource LinkButton}` | Link Button | Giữ nguyên key `LinkButton` |
| `HPRebar.McpBridge/View/McpBridgeStatusView.xaml:155` | `{DynamicResource SecondaryButton}` | Secondary | Giữ nguyên key `SecondaryButton` |
| `HPRebar.McpBridge/View/McpBridgeStatusView.xaml:158` | `{DynamicResource PrimaryButton}` | Primary CTA | Giữ nguyên key `PrimaryButton` |
| `BeamRebar/View/BeamRebarView.xaml:243` | `{DynamicResource LinkButton}` | Link Button | Giữ nguyên key `LinkButton` |
| `BeamRebar/View/BeamRebarView.xaml:264` | `{DynamicResource SecondaryButton}` | Secondary | Giữ nguyên key `SecondaryButton` |
| `BeamRebar/View/BeamRebarView.xaml:268` | `{DynamicResource PrimaryButton}` | Primary CTA | Giữ nguyên key `PrimaryButton` |
| `BeamRebar/View/Controls/Beam3DViewport.xaml:49` | `{DynamicResource MaterialDesignFlatButton}` | Toolkit Direct | Chuẩn hóa sang `IconButton` hoặc giữ direct toolkit |
| `BeamRebar/View/Tabs/AdditionalBarsTabView.xaml:29, 121` | `{DynamicResource IconButton}` | Inline Action | Giữ nguyên key `IconButton` |
| `ColumnRebar/View/ColumnRebarView.xaml:111` | `{DynamicResource LinkButton}` | Link Button | Giữ nguyên key `LinkButton` |
| `ColumnRebar/View/ColumnRebarView.xaml:132` | `{DynamicResource SecondaryButton}` | Secondary | Giữ nguyên key `SecondaryButton` |
| `ColumnRebar/View/ColumnRebarView.xaml:136` | `{DynamicResource PrimaryButton}` | Primary CTA | Giữ nguyên key `PrimaryButton` |
| `ColumnRebar/View/Tabs/*.xaml` (6 tab con) | `{DynamicResource SecondaryButton}` | Action Buttons | Giữ nguyên key `SecondaryButton` |
| `FoundationRebar/View/FoundationRebarView.xaml:94` | `{DynamicResource SecondaryButton}` | Secondary | Giữ nguyên key `SecondaryButton` |
| `FoundationRebar/View/FoundationRebarView.xaml:101` | `{DynamicResource PrimaryButton}` | Primary CTA | Giữ nguyên key `PrimaryButton` |
| `KataExport/View/KataExportView.xaml:184, 192, 449` | `{DynamicResource SecondaryButton}` / `IconButton` | Inline Action | Giữ nguyên |
| `KataExport/View/KataExportView.xaml:771, 784, 797, 809` | `{DynamicResource SecondaryButton}` / `PrimaryButton` | Action bar | Giữ nguyên |
| `KataExport/View/KataSettingsView.xaml:153, 154, 155` | `SecondaryButton` / `PrimaryButton` | Dialog action | Giữ nguyên |
| `KataRebar/View/KataRebarView.xaml:146, 157` | **Không có Style** (Raw button) | Action | Áp dụng implicit style `MaterialDesignOutlinedButton` |
| `KataRebar/View/KataRebarView.xaml:320` | **Không có Style** (Raw button) | Primary CTA | Đổi sang `Style="{DynamicResource PrimaryButton}"` |
| `KataRebar/View/KataRebarView.xaml:328` | **Không có Style** (Raw button) | Secondary | Đổi sang `Style="{DynamicResource SecondaryButton}"` |
| `RebarPartition/View/RebarPartitionView.xaml:212, 331` | `{StaticResource MaterialDesignOutlinedButton}` | Secondary | Chuẩn hóa dùng `{DynamicResource SecondaryButton}` |
| `RebarPartition/View/RebarPartitionView.xaml:241, 244` | `{StaticResource MaterialDesignFlatButton}` | Flat Button | Chuẩn hóa dùng `{DynamicResource LinkButton}` hoặc implicit |
| `RebarPartition/View/RebarPartitionView.xaml:336` | `{StaticResource MaterialDesignRaisedButton}` | Primary CTA | Chuẩn hóa dùng `{DynamicResource PrimaryButton}` |

### 4.2. TextBox
| Vị trí (File & Dòng) | Style hiện tại | Phân loại & Ràng buộc nghiệp vụ | Đề xuất chuyển đổi / xử lý |
|---|---|---|---|
| `ColumnRebar/View/Tabs/SettingTabView.xaml:24, 30, 36` | `{DynamicResource StandardTextBox}` | Chữ thông thường | Giữ nguyên |
| `ColumnRebar/View/Tabs/SettingTabView.xaml:65` | `{DynamicResource NumberTextBox}` | Ô số (Căn phải, font mono) | Giữ nguyên `NumberTextBox` |
| `ColumnRebar/View/Tabs/*.xaml` (~20 ô số) | `{DynamicResource NumberTextBox}` | Kích thước, khoảng rải cốt thép | Giữ nguyên `NumberTextBox` |
| `BeamRebar/View/Tabs/AdditionalBarsTabView.xaml:49, 54` | **Không có Style** (Raw TextBox) | Nhập số tỉ lệ / số lượng | Thừa hưởng implicit style TextBox từ `Theme.xaml` |
| `FoundationGeometryView.xaml:47, 57, 67, 77, 87` | `{DynamicResource NumberTextBox}` | Kích thước đài móng (IsReadOnly) | Giữ nguyên `NumberTextBox` |
| `FoundationSettingView.xaml:49, 67, 115, 133, ...` | `{DynamicResource NumberTextBox}` | Đường kính, bước thép đài | Giữ nguyên `NumberTextBox` |
| `KataSettingsView.xaml:69, 72, 75, 78, ...` | `{StaticResource SettingBox}` (kế thừa `NumberTextBox`) | Tham số Kata (Width=70) | Giữ nguyên `SettingBox` (bảo toàn kích thước compact) |
| `RebarPartitionView.xaml:185, 198, 203` | `{StaticResource MaterialDesignOutlinedTextBox}` | Prefix/Suffix & Tên tham số | Chuẩn hóa sang `{DynamicResource StandardTextBox}` |
| `McpBridgeStatusView.xaml:130` | `{DynamicResource StandardTextBox}` | Log Script (Font mono, readonly) | Giữ nguyên |

### 4.3. ComboBox
| Vị trí (File & Dòng) | Style hiện tại | Số lượng & Đặc điểm | Đề xuất chuyển đổi / xử lý |
|---|---|---|---|
| `RebarPartitionView.xaml:135, 173` | `{StaticResource MaterialDesignOutlinedComboBox}` | 2 ComboBox chọn Scope & Source | Chuẩn hóa sang `{DynamicResource StandardComboBox}` |
| `KataExportView.xaml:280, 300` | `{StaticResource ParamPickerComboBoxStyle}` | 2 ComboBox custom template (Cog icon picker) | **Bảo toàn 100%** (Custom UI picker đặc thù nghiệp vụ) |
| `KataExportView.xaml:101, 105` | **Không có Style** (Width=70) | 2 ComboBox chọn góc bẻ móc | Thừa hưởng implicit style `StandardComboBox` |
| `KataRebarView.xaml:186` | **Không có Style** (trong DataGrid Template) | Ghép nối RebarBarType | Thừa hưởng implicit style `StandardComboBox` |
| `FoundationSettingView.xaml:40, 58, 106, 124, 194` | **Không có Style** | 5 ComboBox chọn đường kính chuẩn | Thừa hưởng implicit style `StandardComboBox` |
| `BeamRebar/View/Tabs/AdditionalBarsTabView.xaml:23, 59, 115, 151` | **Không có Style** | 4 ComboBox chọn thanh thép | Thừa hưởng implicit style `StandardComboBox` |
| `BeamRebar/View/Tabs/MainBarsTabView.xaml:34, 42, 49, 80, 88, 95` | **Không có Style** | 6 ComboBox chọn thép chủ & neo | Thừa hưởng implicit style `StandardComboBox` |
| `BeamRebar/View/Tabs/StirrupsTabView.xaml:29, 36, 94, 121` | **Không có Style** | 4 ComboBox chọn kiểu đai & thép | Thừa hưởng implicit style `StandardComboBox` |
| `BeamRebar/View/Tabs/ViewsTabView.xaml:37, 65` | **Không có Style** | 2 ComboBox chọn tỉ lệ & số mặt cắt | Thừa hưởng implicit style `StandardComboBox` |
| `ColumnRebar/View/Tabs/AdditionalStirrupsTabView.xaml:15, 49, 87` | **Không có Style** | 3 ComboBox chọn loại thép | Thừa hưởng implicit style `StandardComboBox` |
| `ColumnRebar/View/Tabs/BarsTabView.xaml:22, 54, 90` | **Không có Style** | 3 ComboBox chọn thanh thép cột | Thừa hưởng implicit style `StandardComboBox` |
| `ColumnRebar/View/Tabs/BottomDowelsTabView.xaml:21` | **Không có Style** | 1 ComboBox | Thừa hưởng implicit style `StandardComboBox` |
| `ColumnRebar/View/Tabs/TopDowelsTabView.xaml:21` | **Không có Style** | 1 ComboBox | Thừa hưởng implicit style `StandardComboBox` |
| `ColumnRebar/View/Tabs/StirrupsTabView.xaml:15, 51, 68` | **Không có Style** | 3 ComboBox | Thừa hưởng implicit style `StandardComboBox` |

*Nhận xét:* Tổng cộng có tới **35 ComboBox** đang ở trạng thái raw WPF không có style. Khi bổ sung `StandardComboBox` và implicit style trong `Theme.xaml`, toàn bộ 35 ComboBox này sẽ ngay lập tức có giao diện Outlined hiện đại.

### 4.4. CheckBox
| Vị trí (File & Dòng) | Thuộc tính & Style | Đề xuất chuyển đổi / xử lý |
|---|---|---|
| `HPRebar.McpBridge/View/McpBridgeStatusView.xaml:94, 104` | Raw CheckBox, gắn cứng Foreground Primary | Bỏ Foreground cứng, thừa hưởng implicit `MaterialDesignCheckBox` |
| `FoundationSettingView.xaml:84` | Raw CheckBox (Bật tắt lớp thép trên) | Thừa hưởng implicit `MaterialDesignCheckBox` |
| `KataExportView.xaml:393, 414, 425, 438` | Raw CheckBox (Đảo chiều, ẩn hiện thép/tag) | Thừa hưởng implicit `MaterialDesignCheckBox` |
| `ColumnRebar/View/Tabs/SettingTabView.xaml:48` | Raw CheckBox, gắn cứng Foreground Primary | Bỏ Foreground cứng, thừa hưởng implicit `MaterialDesignCheckBox` |
| `ColumnRebar/View/Tabs/*.xaml` (3 ô) | Raw CheckBox (Thép đai phương X/Y) | Thừa hưởng implicit `MaterialDesignCheckBox` |
| `BeamRebar/View/Tabs/*.xaml` (10 ô) | Raw CheckBox (Layer 2, Stagger, Đai gối, ...) | Thừa hưởng implicit `MaterialDesignCheckBox` |
| `RebarPartitionView.xaml:156` | Raw CheckBox (Chọn Category Host) | Thừa hưởng implicit `MaterialDesignCheckBox` |

### 4.5. RadioButton
| Vị trí (File & Dòng) | Style hiện tại | Đề xuất chuyển đổi / xử lý |
|---|---|---|
| `BeamRebarView.xaml:148, 158, 169` | `SegmentedRadioButton` (Custom ControlTemplate) | **Bảo toàn** (Là khối Segmented Switcher chọn chế độ xem 2D / 3D / Split). Bổ sung implicit style `MaterialDesignRadioButton` vào `Theme.xaml` cho các radio button tiêu chuẩn khác. |

### 4.6. TabControl & TabItem
| Vị trí (File & Dòng) | Style hiện tại | Đề xuất chuyển đổi / xử lý |
|---|---|---|
| `FoundationRebarView.xaml:57` | Raw TabControl (Background Surface, BorderBrush Border) | Khai báo implicit style cho `TabControl` và `TabItem` kế thừa `MaterialDesignTabControl` |
| `KataRebarView.xaml:202` | Raw TabControl (Background Transparent, BorderThickness 0) | Thừa hưởng implicit style hoặc chuyển đổi sang MaterialDesign tabs |

### 4.7. DataGrid
| Vị trí (File & Dòng) | Header / Row Style hiện tại | Đề xuất chuyển đổi / xử lý |
|---|---|---|
| `KataRebarView.xaml:170, 205, 227, 248` | `DataGridHeaderStyle` / `DataGridRowStyle` (custom) | **Thay thế** bằng `MaterialDesignDataGridColumnHeader` và `MaterialDesignDataGridRow` chuẩn |
| `RebarPartitionView.xaml:252` | `DataGridHeaderStyle` / `DataGridRowStyle` (custom) | **Thay thế** bằng style DataGrid chuẩn |
| `ColumnRebar/View/Tabs/BarsTabView.xaml:124` | Hardcoded Surface & SurfaceElevated backgrounds | Chuyển sang thừa hưởng style `MaterialDesignDataGrid` chuẩn |
| `BeamRebar/View/Tabs/GeometryTabView.xaml:64, 93` | Raw DataGrid | Thừa hưởng style `MaterialDesignDataGrid` |
| `ColumnRebar/View/Tabs/BottomDowelsTabView.xaml:44` | Raw DataGrid | Thừa hưởng style `MaterialDesignDataGrid` |
| `ColumnRebar/View/Tabs/TopDowelsTabView.xaml:44` | Raw DataGrid | Thừa hưởng style `MaterialDesignDataGrid` |
| `ColumnRebar/View/Tabs/BarsDivisionTabView.xaml:29` | Raw DataGrid | Thừa hưởng style `MaterialDesignDataGrid` |
| `ColumnRebar/View/Tabs/GeometryTabView.xaml:29` | Raw DataGrid | Thừa hưởng style `MaterialDesignDataGrid` |

### 4.8. Expander, ScrollViewer & ToolTip
- **Expander:** Chưa sử dụng trực tiếp trong view nào. Cần trang bị implicit style `<Style TargetType="Expander" BasedOn="{StaticResource MaterialDesignExpander}"/>` trong `Theme.xaml` để sẵn sàng cho các nhóm tuỳ chọn mở rộng.
- **ScrollViewer:** Xuất hiện tại 15 vị trí trong các tab và dialog. Hiện đang dùng mặc định của WPF hoặc bị ép màu nền cứng (`ColumnRebarView.xaml:92`). Cần loại bỏ các thuộc tính màu thủ công và để `MaterialDesignScrollViewer` hiển thị thanh cuộn phẳng thanh lịch.
- **ToolTip:** Xuất hiện tại 26 vị trí. Đã được liên kết màu nền tự động qua `MaterialThemeBridge.cs:69` (`theme.ToolTips.Background`). Cần đảm bảo typography và padding theo chuẩn MaterialDesign.

---

## 5. Danh Mục Custom Styles: Loại Bỏ, Chuẩn Hóa và Giữ Lại

### 5.1. Nhóm 1: Các Custom Style/Template CẦN LOẠI BỎ hoặc THAY THẾ (Deprecate & Replace)

| Custom Key | File định nghĩa | Lý do loại bỏ / thay thế | Thay thế bằng chuẩn Toolkit |
|---|---|---|---|
| `CardBorderStyle` | `KataRebarView.xaml:36`, `RebarPartitionView.xaml:37` | Tự chế Border bo góc, trùng lặp chức năng với `Card` trong `Controls.xaml` và chuẩn `Card.Panel` | Thay bằng `Style="{StaticResource Card.Panel}"` hoặc `{DynamicResource Card}` |
| `BadgeBorderStyle` | `KataRebarView.xaml:45`, `RebarPartitionView.xaml:46` | Tự chế Border pill badge, trùng lặp với `Badge` và `Tag` trong `Controls.xaml` | Thay bằng `Style="{DynamicResource Badge}"` hoặc `{DynamicResource Tag}` |
| `SectionHeaderStyle` | `KataRebarView.xaml:29`, `RebarPartitionView.xaml:30` | Tự chế TextBlock header, trùng lặp với `Subheading` / `BodyStrong` trong `Typography.xaml` | Thay bằng `Style="{DynamicResource Subheading}"` hoặc `{DynamicResource BodyStrong}` |
| `DataGridHeaderStyle` | `KataRebarView.xaml:52`, `RebarPartitionView.xaml:53` | Ghi đè giao diện header cột DataGrid bằng Border 0,0,1,1 thủ công | Thay bằng `DataGrid.ColumnHeader` kế thừa `MaterialDesignDataGridColumnHeader` |
| `DataGridRowStyle` | `KataRebarView.xaml:61`, `RebarPartitionView.xaml:62` | Tự viết trigger MouseOver background cho dòng | Thay bằng `MaterialDesignDataGridRow` của toolkit |
| Hardcoded backgrounds trên DataGrid | `BarsTabView.xaml:130–134` | Gán cứng `RowBackground` và `AlternatingRowBackground` thủ công | Thừa hưởng trực tiếp từ `MaterialDesignDataGrid` |

### 5.2. Nhóm 2: Các Custom Key CẦN GIỮ LẠI NHƯNG ALIASING / RE-BASE (Retain & Re-base)

| Resource Key | Nơi định nghĩa chuẩn | Ràng buộc bảo toàn | Chiến lược Re-base |
|---|---|---|---|
| `PrimaryButton` | `MaterialBridge.xaml:23` | Bắt buộc bởi `ThemeTokenCoverageTests` và 15+ vị trí XAML | BasedOn `MaterialDesignRaisedButton` (đang có, giữ nguyên) |
| `SecondaryButton` | `MaterialBridge.xaml:30` | Bắt buộc bởi `ThemeTokenCoverageTests` và 25+ vị trí XAML | BasedOn `MaterialDesignOutlinedButton` (đang có, giữ nguyên) |
| `DangerButton` | `MaterialBridge.xaml:39` | Nút đỏ nguy hiểm | BasedOn `PrimaryButton` + Brush.Danger (đang có, giữ nguyên) |
| `IconButton` | `MaterialBridge.xaml:45` | Nút icon vuông 32x32 | BasedOn `MaterialDesignIconButton` (đang có, giữ nguyên) |
| `LinkButton` | `MaterialBridge.xaml:53` | Nút văn bản dạng link | BasedOn `MaterialDesignFlatButton` (đang có, giữ nguyên) |
| `StandardTextBox` | `MaterialBridge.xaml:63` | Ô nhập Outlined chuẩn | BasedOn `MaterialDesignOutlinedTextBox` (đang có, giữ nguyên) |
| `NumberTextBox` | `MaterialBridge.xaml:73` | Ô nhập số căn phải, font mono | BasedOn `StandardTextBox` (đang có, giữ nguyên) |
| `SearchTextBox` | `MaterialBridge.xaml:78` | Ô tìm kiếm có hint | BasedOn `StandardTextBox` + Hint (đang có, giữ nguyên) |
| `StandardComboBox` | **Cần bổ sung vào `MaterialBridge.xaml`** | Chuẩn hóa cho 35+ comboboxes | Khai báo mới BasedOn `MaterialDesignOutlinedComboBox` |
| `Card` | `Controls.xaml:6` | 40+ Border trong toàn bộ dự án đang bind `{DynamicResource Card}` | Aliasing hoặc re-base sang `Card.Panel` với DropShadow |
| `Card.Panel` | **Cần bổ sung vào `MaterialBridge.xaml`** | Chuẩn của skill `revit-xaml-styles` | Border có CornerRadius 4, Padding 12, DropShadowEffect |

### 5.3. Nhóm 3: Các Style/Template NGHIỆP VỤ ĐẶC THÙ BẮT BUỘC BẢO TOÀN 100% (Domain Functional Preserved)

Theo yêu cầu R2 ("Bảo toàn 100% cấu hình phục vụ chức năng và nghiệp vụ: căn lề phải ô số, định dạng dữ liệu, validation rules/triggers, kích thước layout cần thiết, preview hình học và màu sắc kỹ thuật cốt thép"), các phần sau **TUYỆT ĐỐI KHÔNG BỊ XÓA HAY LÀM THAY ĐỔI HÀNH VI**:

1. **`ParamPickerComboBoxStyle` (`KataExportView.xaml:55–141`):**
   - ControlTemplate đặc thù tạo nút bấm dạng icon (Cog + Mũi tên xổ xuống) kích thước 25x22px để người dùng nhấp chọn nhanh tham số Revit mapping sang ô Excel Kata B3/B4.
   - *Hành động:* Giữ nguyên cấu trúc nghiệp vụ; chỉ đảm bảo các mã màu nền/viền popup liên kết đúng token (`Brush.SurfaceElevated`, `Brush.GridLine`, `Brush.SurfaceHover`).
2. **`ListBoxItem` horizontal template (`KataExportView.xaml:568–585`):**
   - ControlTemplate tạo các ô cột bảng tính ngang KATA (Cột C..BZ) với viền dọc 1px (`BorderThickness="0,0,1,0"`), MinWidth 62px để cuộn ngang như bảng Excel.
   - *Hành động:* Giữ nguyên 100%.
3. **`SegmentedRadioButton` (`BeamRebarView.xaml:33–63`):**
   - ControlTemplate biến RadioButton thành nút Segmented Switcher chuyển đổi 3 chế độ (2D / 3D / Split).
   - *Hành động:* Giữ nguyên 100%.
4. **DataTriggers hiển thị trạng thái lỗi (`HasError`):**
   - `KataExportView.xaml:220–227` và `KataRebarView.xaml:306–314`: Khi `HasError == True`, TextBlock chuyển màu `Brush.Danger`.
   - *Hành động:* Giữ nguyên 100%.
5. **DataTriggers trạng thái Pipe của MCP Bridge (`McpBridgeStatusView.xaml:25–44`):**
   - `StatusDot`: Listening -> `Brush.Info`, Connected -> `Brush.Success`, Busy -> `Brush.Warning`, Error -> `Brush.Danger`.
   - `CompileCountText`: IsCompileCountHigh -> `Brush.Warning`.
   - *Hành động:* Giữ nguyên 100%.
6. **Bộ Brush đồ họa cốt thép kỹ thuật (`ThemeDark.xaml:64–78`, `ThemeLight.xaml:60–74`):**
   - `Brush.Canvas.MainBar` (Đỏ thép chủ), `Brush.Canvas.Stirrup` (Xanh lá cốt đai), `Brush.Canvas.Bound` (Xám bê tông), v.v.
   - *Hành động:* Giữ nguyên 100%, không bị ảnh hưởng bởi theme toolkit.
7. **8 Vector Tab Icons (`ThemeIcons.xaml`):**
   - Hình học chi tiết mặt cắt cột, đai gia cường, thép chờ móng.
   - *Hành động:* Giữ nguyên 100%.

---

## 6. Đề Xuất Kế Hoạch Triển Khai Cho Nhóm Implementers

Dựa trên kết quả khảo sát, quy trình di chuyển và chuẩn hóa styling được đề xuất gồm 4 bước tuần tự:

### Bước 1: Nâng cấp Core Theming (`MaterialBridge.xaml`, `Theme.xaml`, `MaterialThemeBridge.cs`)
1. **Trong `MaterialBridge.xaml`:**
   - Bổ sung `StandardComboBox` (TargetType="ComboBox", BasedOn="{StaticResource MaterialDesignOutlinedComboBox}").
   - Bổ sung `Card.Panel` (TargetType="Border" với DropShadow).
2. **Trong `Theme.xaml`:**
   - Khai báo hệ thống Implicit Styles cho controls chuẩn:
     - `TargetType="TextBox"` -> BasedOn `StandardTextBox`.
     - `TargetType="ComboBox"` -> BasedOn `StandardComboBox`.
     - `TargetType="CheckBox"` -> BasedOn `MaterialDesignCheckBox`.
     - `TargetType="RadioButton"` -> BasedOn `MaterialDesignRadioButton`.
     - `TargetType="Button"` -> BasedOn `MaterialDesignOutlinedButton`.
     - `TargetType="DataGrid"` -> BasedOn `MaterialDesignDataGrid`.
     - `TargetType="TabControl"` -> BasedOn `MaterialDesignTabControl`.
     - `TargetType="Expander"` -> BasedOn `MaterialDesignExpander`.
3. **Trong `MaterialThemeBridge.cs`:**
   - Tinh chỉnh phương thức `Apply`: đảm bảo toolkit theme quản lý bề mặt card/popup một cách tự nhiên theo yêu cầu R3.

### Bước 2: Chuẩn hóa Views & Loại bỏ Style Thừa
1. **Module KataRebar & RebarPartition:**
   - Xóa bỏ `CardBorderStyle`, `BadgeBorderStyle`, `SectionHeaderStyle`, `DataGridHeaderStyle`, `DataGridRowStyle` khỏi `KataRebarView.xaml` và `RebarPartitionView.xaml`.
   - Chuyển sang sử dụng `{StaticResource Card.Panel}`, `{DynamicResource Badge}`, `{DynamicResource Subheading}`, và DataGrid toolkit mặc định.
2. **Module ColumnRebar & BeamRebar:**
   - Dọn dẹp các thuộc tính màu gán cứng (`RowBackground`, `AlternatingRowBackground`) trên DataGrid trong `BarsTabView.xaml`.
   - Để các ComboBox và TextBox raw tự động thừa hưởng Implicit Styles từ `Theme.xaml`.

### Bước 3: Cập Nhật Harness & Test Coverage
1. Cập nhật `ThemeTokenCoverageTests.cs`:
   - Xác nhận nhận diện đúng các resource keys mới (`StandardComboBox`, `Card.Panel`).
   - Đảm bảo 100% test xUnit tiếp tục PASS không hồi quy.
2. Kiểm tra `ThemeGallery`:
   - Chạy lệnh visual gallery để xác nhận tất cả các UserControl parse sạch và render chuẩn ở cả hai chế độ Dark và Light.

### Bước 4: Kiểm Chứng Biên Dịch & Đóng Gói
- Chạy `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` và các cấu hình R23..R27.
- Xác nhận ILRepack đóng gói thành công không lỗi BAML hay thiếu resource key.
