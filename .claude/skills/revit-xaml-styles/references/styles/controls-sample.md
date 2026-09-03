# Buttons.xaml + TextBoxes.xaml + Controls.xaml — Sample

## Buttons.xaml

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== PrimaryButton ===== -->
    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="{DynamicResource Brush.Accent}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.OnAccent}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="MinWidth" Value="96"/>
        <Setter Property="MinHeight" Value="32"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Bg"
                            Background="{TemplateBinding Background}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="{DynamicResource Brush.Accent.Hover}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="{DynamicResource Brush.Accent.Pressed}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="Bg" Property="Background" Value="{DynamicResource Brush.Border.Disabled}"/>
                            <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Disabled}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ===== SecondaryButton ===== -->
    <Style x:Key="SecondaryButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
        <Setter Property="Background" Value="{DynamicResource Brush.Surface}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Bg"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="4"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="{DynamicResource Brush.SurfaceHover}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="{DynamicResource Brush.SurfacePressed}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ===== DangerButton ===== -->
    <Style x:Key="DangerButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
        <Setter Property="Background" Value="{DynamicResource Brush.Danger}"/>
    </Style>

    <!-- ===== IconButton ===== -->
    <Style x:Key="IconButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="4"/>
        <Setter Property="Width" Value="32"/>
        <Setter Property="Height" Value="32"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Bg" Background="{TemplateBinding Background}" CornerRadius="4">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="{DynamicResource Brush.SurfaceHover}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ===== LinkButton ===== -->
    <Style x:Key="LinkButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Accent}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="0"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <TextBlock Text="{TemplateBinding Content}"
                               TextDecorations="Underline"
                               Foreground="{TemplateBinding Foreground}"/>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>
```

## TextBoxes.xaml

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== StandardTextBox ===== -->
    <Style x:Key="StandardTextBox" TargetType="TextBox">
        <Setter Property="Background" Value="{DynamicResource Brush.Surface}"/>
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
        <Setter Property="CaretBrush" Value="{DynamicResource Brush.Foreground.Primary}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="MinHeight" Value="32"/>
        <Setter Property="VerticalContentAlignment" Value="Center"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="TextBox">
                    <Border x:Name="Bd"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="4">
                        <ScrollViewer x:Name="PART_ContentHost"
                                      Padding="{TemplateBinding Padding}"
                                      VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="Bd" Property="BorderBrush" Value="{DynamicResource Brush.Border.Focus}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Disabled}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- ===== NumberTextBox (validation trong ViewModel) ===== -->
    <Style x:Key="NumberTextBox" TargetType="TextBox" BasedOn="{StaticResource StandardTextBox}">
        <Setter Property="HorizontalContentAlignment" Value="Right"/>
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Mono}"/>
    </Style>

    <!-- ===== SearchTextBox (placeholder qua adorner) ===== -->
    <Style x:Key="SearchTextBox" TargetType="TextBox" BasedOn="{StaticResource StandardTextBox}">
        <Setter Property="Padding" Value="32,6,8,6"/>
        <Setter Property="Tag" Value="Tìm kiếm..."/>
    </Style>

</ResourceDictionary>
```

## Controls.xaml (Card / Separator / Badge)

```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- ===== Card ===== -->
    <Style x:Key="Card" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource Brush.SurfaceElevated}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="6"/>
        <Setter Property="Padding" Value="{DynamicResource Spacing.Medium}"/>
    </Style>

    <!-- ===== Horizontal Separator ===== -->
    <Style x:Key="Separator" TargetType="Border">
        <Setter Property="Height" Value="1"/>
        <Setter Property="Background" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="Margin" Value="{DynamicResource Spacing.MediumVertical}"/>
    </Style>

    <!-- ===== Badge (count, status pill) ===== -->
    <Style x:Key="Badge" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource Brush.Accent}"/>
        <Setter Property="CornerRadius" Value="10"/>
        <Setter Property="Padding" Value="8,2"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
    </Style>

    <Style x:Key="BadgeText" TargetType="TextBlock">
        <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.OnAccent}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Caption}"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
    </Style>

    <!-- ===== Tag (inline color label) ===== -->
    <Style x:Key="Tag" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource Brush.Surface}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="3"/>
        <Setter Property="Padding" Value="6,2"/>
    </Style>

</ResourceDictionary>
```

## Usage examples

```xml
<!-- Card -->
<Border Style="{DynamicResource Card}">
    <StackPanel>
        <TextBlock Style="{DynamicResource Subheading}" Text="Wall Stats"/>
        <Border Style="{DynamicResource Separator}"/>
        <TextBlock Style="{DynamicResource Body}" Text="Total: 247 walls"/>
    </StackPanel>
</Border>

<!-- Badge -->
<Border Style="{DynamicResource Badge}">
    <TextBlock Style="{DynamicResource BadgeText}" Text="12"/>
</Border>

<!-- Action row -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
    <Button Style="{DynamicResource SecondaryButton}" Content="Cancel" Margin="0,0,8,0"
            Command="{Binding CancelCommand}"/>
    <Button Style="{DynamicResource PrimaryButton}" Content="Save"
            Command="{Binding SaveCommand}"/>
</StackPanel>
```
