# ThemeLight.xaml — Color Palette Sample

Copy nội dung dưới đây vào `Resources/Themes/ThemeLight.xaml`. Cùng key, khác giá trị → swap được runtime.

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== Background ===== -->
    <Color x:Key="Color.Background">#F8F8F8</Color>
    <Color x:Key="Color.Surface">#FFFFFF</Color>
    <Color x:Key="Color.SurfaceElevated">#FFFFFF</Color>
    <Color x:Key="Color.SurfaceHover">#EEEEEE</Color>
    <Color x:Key="Color.SurfacePressed">#E0E0E0</Color>

    <!-- ===== Foreground ===== -->
    <Color x:Key="Color.Foreground.Primary">#1A1A1A</Color>
    <Color x:Key="Color.Foreground.Secondary">#4D4D4D</Color>
    <Color x:Key="Color.Foreground.Tertiary">#8E8E93</Color>
    <Color x:Key="Color.Foreground.Disabled">#B0B0B0</Color>
    <Color x:Key="Color.Foreground.OnAccent">#FFFFFF</Color>

    <!-- ===== Accent ===== -->
    <Color x:Key="Color.Accent">#0696D7</Color>
    <Color x:Key="Color.Accent.Hover">#0578AD</Color>
    <Color x:Key="Color.Accent.Pressed">#045D87</Color>

    <!-- ===== Border ===== -->
    <Color x:Key="Color.Border">#D0D0D0</Color>
    <Color x:Key="Color.Border.Focus">#0696D7</Color>
    <Color x:Key="Color.Border.Disabled">#E5E5E5</Color>

    <!-- ===== Semantic ===== -->
    <Color x:Key="Color.Success">#138A56</Color>
    <Color x:Key="Color.Warning">#D8800E</Color>
    <Color x:Key="Color.Danger">#C72731</Color>
    <Color x:Key="Color.Info">#0696D7</Color>

    <!-- ===== Brushes ===== -->
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

## Quy tắc cùng key

- ThemeDark + ThemeLight phải có **cùng tập key** → swap không broken binding.
- Contrast WCAG AA: Light theme `Color.Foreground.Primary` trên `Color.Background` ≥ 4.5:1.
- Semantic color (Success/Warning/Danger) **đậm hơn** ở light theme để readable trên background sáng.
