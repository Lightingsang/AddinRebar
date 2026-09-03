# Spacing.xaml + Typography.xaml — Tokens Sample

## Spacing.xaml

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- Multiples of 4 -->
    <sys:Double x:Key="Spacing.XSmall.Value">4</sys:Double>
    <sys:Double x:Key="Spacing.Small.Value">8</sys:Double>
    <sys:Double x:Key="Spacing.Medium.Value">16</sys:Double>
    <sys:Double x:Key="Spacing.Large.Value">24</sys:Double>
    <sys:Double x:Key="Spacing.XLarge.Value">32</sys:Double>

    <!-- Thickness (all sides) -->
    <Thickness x:Key="Spacing.XSmall">4</Thickness>
    <Thickness x:Key="Spacing.Small">8</Thickness>
    <Thickness x:Key="Spacing.Medium">16</Thickness>
    <Thickness x:Key="Spacing.Large">24</Thickness>
    <Thickness x:Key="Spacing.XLarge">32</Thickness>

    <!-- Horizontal only (Left, Right) -->
    <Thickness x:Key="Spacing.SmallHorizontal">8,0</Thickness>
    <Thickness x:Key="Spacing.MediumHorizontal">16,0</Thickness>
    <Thickness x:Key="Spacing.LargeHorizontal">24,0</Thickness>

    <!-- Vertical only (Top, Bottom) -->
    <Thickness x:Key="Spacing.SmallVertical">0,8</Thickness>
    <Thickness x:Key="Spacing.MediumVertical">0,16</Thickness>
    <Thickness x:Key="Spacing.LargeVertical">0,24</Thickness>

    <!-- Top only -->
    <Thickness x:Key="Spacing.SmallTop">0,8,0,0</Thickness>
    <Thickness x:Key="Spacing.MediumTop">0,16,0,0</Thickness>

    <!-- Bottom only -->
    <Thickness x:Key="Spacing.SmallBottom">0,0,0,8</Thickness>
    <Thickness x:Key="Spacing.MediumBottom">0,0,0,16</Thickness>

</ResourceDictionary>
```

## Typography.xaml

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- Font family -->
    <FontFamily x:Key="Font.Family.Default">Segoe UI</FontFamily>
    <FontFamily x:Key="Font.Family.Mono">Cascadia Mono, Consolas</FontFamily>

    <!-- Font sizes -->
    <sys:Double x:Key="Font.Size.Caption">11</sys:Double>
    <sys:Double x:Key="Font.Size.Body">14</sys:Double>
    <sys:Double x:Key="Font.Size.BodyStrong">14</sys:Double>
    <sys:Double x:Key="Font.Size.Subheading">16</sys:Double>
    <sys:Double x:Key="Font.Size.Heading">20</sys:Double>
    <sys:Double x:Key="Font.Size.Title">28</sys:Double>

    <!-- Pre-baked TextBlock styles -->
    <Style x:Key="Caption" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Caption}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Tertiary}"/>
    </Style>

    <Style x:Key="Body" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>

    <Style x:Key="BodyStrong" TargetType="TextBlock" BasedOn="{StaticResource Body}">
        <Setter Property="FontWeight" Value="SemiBold"/>
    </Style>

    <Style x:Key="Subheading" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Subheading}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>

    <Style x:Key="Heading" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Heading}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>

    <Style x:Key="Title" TargetType="TextBlock">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Title}"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
    </Style>

</ResourceDictionary>
```

## Usage

```xml
<StackPanel Margin="{DynamicResource Spacing.Large}">
    <TextBlock Text="Wall Report" Style="{DynamicResource Heading}"/>
    <TextBlock Text="Tổng số tường trong project" Style="{DynamicResource Caption}"
               Margin="{DynamicResource Spacing.SmallTop}"/>
    <TextBlock Text="247" Style="{DynamicResource Title}"
               Margin="{DynamicResource Spacing.SmallTop}"/>
</StackPanel>
```
