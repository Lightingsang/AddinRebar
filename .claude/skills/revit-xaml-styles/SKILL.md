---
name: revit-xaml-styles
description: "Chuẩn hóa XAML styles và Material Design cho mọi Add-In WPF (Revit, AutoCAD, Civil 3D, standalone bridge...) — BẮT BUỘC sử dụng MaterialDesignInXamlToolkit (v5.2.1 hoặc v5.3.2). Bao gồm MaterialBridge.xaml, Theme.xaml, MaterialThemeBridge.cs (switch Dark/Light runtime theo host), font Segoe UI (cấm Roboto), md:Card, Outlined inputs, md:PackIcon, phân cấp Button và cấu hình ILRepack. TRIGGER when: tạo/sửa file .xaml, task chứa 'style', 'theme', 'material design', 'wpf', 'button style', 'dark mode', hoặc khi xây dựng một addin/window WPF mới."
user-invocable: true
when_to_use: "Khi bắt đầu một add-in mới có UI WPF, hoặc khi tạo/sửa đổi cửa sổ Window, UserControl, style XAML trong toàn bộ repo."
category: revit
keywords: [xaml, wpf, style, theme, material-design, materialdesigninxamltoolkit, resourcedictionary, dark, light, dynamicresource, packicon, ilrepack]
metadata:
  author: hoang
  version: "2.0.0"
---

# WPF XAML Styles & Material Design Standard

> **QUY TẮC BẮT BUỘC CỦA TOÀN REPO:**
> Mọi add-in hoặc công cụ mới có giao diện WPF (Revit, AutoCAD, Civil 3D, SAP2000, Robot, Tekla, các standalone bridge...) **BẮT BUỘC 100% SỬ DỤNG `MaterialDesignInXamlToolkit`** (phiên bản `5.2.1` hoặc `5.3.2`).

---

## 1. Cấu trúc thư mục chuẩn `Resources/Themes/`

Mọi project WPF add-in phải tổ chức tài nguyên giao diện tập trung tại thư mục `Resources/Themes/` ở project root:

```
Resources/Themes/
├── Theme.xaml              ← Master Theme, merge MaterialBridge.xaml đầu tiên
├── MaterialBridge.xaml     ← Cầu nối MaterialDesign + CustomColorTheme + override Segoe UI
├── MaterialThemeBridge.cs  ← C# static helper đồng bộ theme Dark/Light theo Host CAD/BIM
├── ThemeDark.xaml          ← Color tokens chế độ Tối (Dark)
├── ThemeLight.xaml         ← Color tokens chế độ Sáng (Light)
├── Typography.xaml         ← Font sizes & TextBlock styles
└── Spacing.xaml            ← Spacing tokens (4/8/16/24/32)
```

---

## 2. Bảy nguyên tắc vàng bắt buộc (Core Rules)

### 1. Font chữ: BẮT BUỘC override `Segoe UI` (CẤM Roboto)
Thư viện `MaterialDesignThemes` mặc định dùng font `Roboto` qua Pack URI. Khi add-in được đóng gói bằng **ILRepack** (bắt buộc cho CAD/BIM), Pack URI của Roboto không thể giải quyết và gây crash hoặc lỗi layout.
Trong `MaterialBridge.xaml`, bắt buộc ghi đè:
```xml
<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>
```
Toàn bộ cửa sổ và điều khiển phải sử dụng font hệ thống chuẩn `Segoe UI`.

### 2. Thứ tự merge trong Master `Theme.xaml`
`MaterialBridge.xaml` phải được merge ở **vị trí đầu tiên** để các control nhận style Material Design mặc định. Sau đó mới đến `ThemeDark.xaml` / `ThemeLight.xaml` và các từ điển phụ trợ:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="pack://application:,,,/MyAddIn;component/Resources/Themes/MaterialBridge.xaml"/>
        <ResourceDictionary Source="pack://application:,,,/MyAddIn;component/Resources/Themes/ThemeDark.xaml"/>
        <ResourceDictionary Source="pack://application:,,,/MyAddIn;component/Resources/Themes/Spacing.xaml"/>
        <ResourceDictionary Source="pack://application:,,,/MyAddIn;component/Resources/Themes/Typography.xaml"/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

### 3. Đồng bộ Theme Dark / Light qua `{DynamicResource}`
- **Tuyệt đối KHÔNG hardcode màu** (`Background="#1E1E1E"`).
- **Tuyệt đối KHÔNG dùng `{StaticResource ...}` cho Color hoặc Brush** (sẽ làm mất khả năng đổi theme runtime).
- Mọi thuộc tính màu sắc phải bind qua `{DynamicResource Brush.Background}`, `{DynamicResource Brush.Surface}`, `{DynamicResource Brush.Foreground.Primary}`, `{DynamicResource Brush.Accent}`.
- Trong code-behind của Window, kết nối với phần mềm chủ qua `MaterialThemeBridge.Attach(this, hostTheme)` hoặc `MaterialThemeBridge.Apply(this, isDark)`.

### 4. Bố cục Card phân tầng (`md:Card` / `Card.Panel`)
- Không để các trường nhập liệu trôi nổi trên nền phẳng không phân cách.
- Gom nhóm các nhóm tính năng hoặc section vào các khối Card với độ sâu (elevation):
```xml
<Border Style="{StaticResource Card.Panel}" Margin="{DynamicResource Margin.Section}">
    <StackPanel>
        <!-- Tiêu đề Section có Icon -->
        <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
            <md:PackIcon Kind="MapMarkerRadiusOutline" Width="18" Height="18" Foreground="{DynamicResource Brush.Accent}"/>
            <TextBlock Text="Hệ tọa độ VN-2000" Style="{StaticResource Text.Section}" Margin="8,0,0,0"/>
        </StackPanel>
        <!-- Nội dung control -->
    </StackPanel>
</Border>
```

### 5. Input Controls: Dạng Outlined có Floating Hint
Mọi `TextBox` và `ComboBox` nên kế thừa style Outlined để có giao diện hiện đại, rõ ràng:
- `Style="{StaticResource MaterialDesignOutlinedTextBox}"` hoặc `Style="{StaticResource StandardTextBox}"`
- `Style="{StaticResource MaterialDesignOutlinedComboBox}"` hoặc `Style="{StaticResource StandardComboBox}"`
- Sử dụng thuộc tính `md:HintAssist.Hint="Tên trường..."` để nhãn tự động nổi lên trên viền khi người dùng focus hoặc có dữ liệu.

### 6. Biểu tượng chuẩn: `md:PackIcon`
- Sử dụng trực tiếp `xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"` và `<md:PackIcon Kind="..." Width="16" Height="16"/>`.
- Kích thước icon chuẩn:
  - 16×16: Icon trong Button nhỏ, DataGrid row action, hoặc inline hint.
  - 18×18 hoặc 20×20: Icon tiêu đề Section / Card header.
  - 24×24 hoặc 32×32: Header bar icon chính hoặc nút lớn.

### 7. Phân cấp Button (Tiered Action Buttons)
Không đặt các nút bấm ngang hàng bằng một kiểu giống nhau. Phân cấp rõ rệt:
- **Primary Action (CTA chính)**: Dùng `Style="{StaticResource PrimaryButton}"` (dựa trên `MaterialDesignRaisedButton`) với màu Accent nổi bật. Ví dụ: *Chạy phân tích, Xuất KMZ, Vẽ dầm*.
- **Secondary Action (Hành động phụ)**: Dùng `Style="{StaticResource SecondaryButton}"` (dựa trên `MaterialDesignOutlinedButton`). Ví dụ: *Đóng, Hủy, Xem trước, Copy*.
- **Danger Action**: Dùng `Style="{StaticResource DangerButton}"` (màu đỏ) cho thao tác xóa dữ liệu, rollback không thể hoàn tác.
- **Quick / Inline Action**: Dùng `Style="{StaticResource IconButton}"` (nút icon vuông 32×32 bo tròn không viền).

---

## 3. Đóng gói ILRepack (Bắt buộc cho môi trường In-Process CAD/BIM)

Do add-in chạy chung tiến trình với CAD/BIM host (Revit, AutoCAD, Civil 3D), **bắt buộc phải gộp các thư viện MaterialDesign vào DLL chính**:
- **Revit (`Nice3point.Revit.Sdk`)**: Khai báo `<IsRepackable>true</IsRepackable>` trong `.csproj`.
- **AutoCAD / Civil 3D / Khác**: Dùng `ILRepack` target `RepackMaterialDesign` gộp `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`.
- Chi tiết xem file: `references/boilerplate/packaging-csproj.md`.

---

## 4. Danh mục File mẫu sẵn sàng sử dụng (Boilerplates)

Khi tạo một add-in mới, copy trực tiếp từ thư mục `references/boilerplate/`:
1. [MaterialBridge.xaml](references/boilerplate/MaterialBridge.xaml): File cầu nối XAML định nghĩa font Segoe UI và các style nút, textbox, combobox, card.
2. [Theme.xaml](references/boilerplate/Theme.xaml): File Master ResourceDictionary gộp các theme.
3. [MaterialThemeBridge.cs](references/boilerplate/MaterialThemeBridge.cs): Code C# quản lý gắn theme và tráo đổi DynamicResource an toàn đa ALC.
4. [packaging-csproj.md](references/boilerplate/packaging-csproj.md): Mẫu cấu hình file `.csproj` để build và ILRepack không lỗi.

---

## 5. Bảng kiểm tra trước khi hoàn thành UI (Pre-flight Checklist)

- [ ] Window đã merge `Theme.xaml` ở thẻ gốc.
- [ ] Font chữ toàn bộ là `Segoe UI`, không xuất hiện tham chiếu `Roboto` hay `{md:MaterialDesignFont}`.
- [ ] 100% mã màu/brush dùng `{DynamicResource ...}`, không có mã màu HEX hardcode trong XAML (trừ icon vector trắng cố định hoặc trường hợp đặc biệt).
- [ ] Các input controls có floating hint rõ ràng (`md:HintAssist.Hint`).
- [ ] Bố cục giao diện sử dụng Card (`md:Card` hoặc `Card.Panel`) phân vùng trực quan.
- [ ] Nút bấm có phân cấp rõ rệt (Raised Primary cho hành động chính, Outlined Secondary cho hành động phụ).
- [ ] Cửa sổ đã test hiển thị tốt trên cả 2 giao diện: Dark Mode và Light Mode.
- [ ] Build thành công và ILRepack gộp sạch `MaterialDesignThemes.Wpf.dll`.
