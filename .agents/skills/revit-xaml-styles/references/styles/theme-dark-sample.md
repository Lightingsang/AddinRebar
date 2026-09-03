# ThemeDark.xaml — Color Palette Sample

Copy nội dung dưới đây vào `Resources/Themes/ThemeDark.xaml`.

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== Background ===== -->
    <Color x:Key="Color.Background">#1E1E1E</Color>
    <Color x:Key="Color.Surface">#2D2D30</Color>
    <Color x:Key="Color.SurfaceElevated">#3E3E42</Color>
    <Color x:Key="Color.SurfaceHover">#3A3A3E</Color>
    <Color x:Key="Color.SurfacePressed">#4A4A4E</Color>

    <!-- ===== Foreground ===== -->
    <Color x:Key="Color.Foreground.Primary">#FFFFFF</Color>
    <Color x:Key="Color.Foreground.Secondary">#CCCCCC</Color>
    <Color x:Key="Color.Foreground.Tertiary">#8E8E93</Color>
    <Color x:Key="Color.Foreground.Disabled">#6C6C70</Color>
    <Color x:Key="Color.Foreground.OnAccent">#FFFFFF</Color>

    <!-- ===== Accent (Autodesk Revit blue) ===== -->
    <Color x:Key="Color.Accent">#0696D7</Color>
    <Color x:Key="Color.Accent.Hover">#1FA8E0</Color>
    <Color x:Key="Color.Accent.Pressed">#0578AD</Color>

    <!-- ===== Border ===== -->
    <Color x:Key="Color.Border">#3E3E42</Color>
    <Color x:Key="Color.Border.Focus">#0696D7</Color>
    <Color x:Key="Color.Border.Disabled">#2A2A2D</Color>

    <!-- ===== Semantic ===== -->
    <Color x:Key="Color.Success">#16C172</Color>
    <Color x:Key="Color.Warning">#F5A623</Color>
    <Color x:Key="Color.Danger">#E5484D</Color>
    <Color x:Key="Color.Info">#0696D7</Color>

    <!-- ===== Brushes (luôn dùng Brush.X, KHÔNG dùng Color.X trực tiếp) ===== -->
    <SolidColorBrush x:Key="Brush.Background" Color="{StaticResource Color.Background}"/>
    <SolidColorBrush x:Key="Brush.Surface" Color="{StaticResource Color.Surface}"/>
    <SolidColorBrush x:Key="Brush.SurfaceElevated" Color="{StaticResource Color.SurfaceElevated}"/>
    <SolidColorBrush x:Key="Brush.SurfaceHover" Color="{StaticResource Color.SurfaceHover}"/>
    <SolidColorBrush x:Key="Brush.SurfacePressed" Color="{StaticResource Color.SurfacePressed}"/>

    <SolidColorBrush x:Key="Brush.Foreground.Primary" Color="{StaticResource Color.Foreground.Primary}"/>
    <SolidColorBrush x:Key="Brush.Foreground.Secondary" Color="{StaticResource Color.Foreground.Secondary}"/>
    <SolidColorBrush x:Key="Brush.Foreground.Tertiary" Color="{StaticResource Color.Foreground.Tertiary}"/>
    <SolidColorBrush x:Key="Brush.Foreground.Disabled" Color="{StaticResource Color.Foreground.Disabled}"/>
    <SolidColorBrush x:Key="Brush.Foreground.OnAccent" Color="{StaticResource Color.Foreground.OnAccent}"/>

    <SolidColorBrush x:Key="Brush.Accent" Color="{StaticResource Color.Accent}"/>
    <SolidColorBrush x:Key="Brush.Accent.Hover" Color="{StaticResource Color.Accent.Hover}"/>
    <SolidColorBrush x:Key="Brush.Accent.Pressed" Color="{StaticResource Color.Accent.Pressed}"/>

    <SolidColorBrush x:Key="Brush.Border" Color="{StaticResource Color.Border}"/>
    <SolidColorBrush x:Key="Brush.Border.Focus" Color="{StaticResource Color.Border.Focus}"/>
    <SolidColorBrush x:Key="Brush.Border.Disabled" Color="{StaticResource Color.Border.Disabled}"/>

    <SolidColorBrush x:Key="Brush.Success" Color="{StaticResource Color.Success}"/>
    <SolidColorBrush x:Key="Brush.Warning" Color="{StaticResource Color.Warning}"/>
    <SolidColorBrush x:Key="Brush.Danger" Color="{StaticResource Color.Danger}"/>
    <SolidColorBrush x:Key="Brush.Info" Color="{StaticResource Color.Info}"/>

</ResourceDictionary>
```

## Lưu ý

- **Brush vs Color:** `Color` chỉ là giá trị màu raw; `Brush` là cái UI control dùng (`Background`, `Foreground`, `BorderBrush`). LUÔN bind `{DynamicResource Brush.X}` không phải `Color.X`.
- **Color name semantic, không hardcode "Blue", "Red":** `Brush.Accent` thay vì `Brush.Blue` — đổi accent sau dễ.
- **Match Revit UI:** `#1E1E1E` background = Revit 2024+ dark theme; `#0696D7` accent = Autodesk blue (close to Revit ribbon).
