# Controls — Sample (toolkit keys + HP colour tokens)

XAML mới dùng thẳng style của MaterialDesignInXamlToolkit 5.3.2 (MD2). Không viết `ControlTemplate` riêng
cho Button / TextBox / Card và không định nghĩa style bọc lại (`PrimaryButton`, `StandardTextBox`, `Card.Panel`…).
Màu riêng lấy từ token HP `{DynamicResource Brush.*}` (palette `ThemeDark.xaml` / `ThemeLight.xaml`).

## Bảng tra nhanh

| Nhu cầu | XAML |
|---|---|
| Nhóm nội dung | `<md:Card Padding="12" UniformCornerRadius="4" md:ElevationAssist.Elevation="Dp2">` |
| Tiêu đề cửa sổ / section / nội dung / ghi chú | `MaterialDesignHeadline6TextBlock` / `MaterialDesignSubtitle1TextBlock` / `MaterialDesignBody2TextBlock` / `MaterialDesignCaptionTextBlock` |
| Ô nhập chữ | `MaterialDesignOutlinedTextBox` + `md:HintAssist.Hint` |
| Ô nhập số | `MaterialDesignOutlinedTextBox` + `HorizontalContentAlignment="Right"` + `md:TextFieldAssist.SuffixText="mm"` |
| Danh sách chọn | `MaterialDesignOutlinedComboBox` + `md:HintAssist.Hint` |
| Bật / tắt | `MaterialDesignCheckBox` hoặc `MaterialDesignSwitchToggleButton` |
| Bảng | `MaterialDesignDataGrid` |
| Đường phân cách | `<Separator Style="{StaticResource MaterialDesignLightSeparator}"/>` |
| Hành động chính / phụ / chữ | `MaterialDesignRaisedButton` / `MaterialDesignOutlinedButton` / `MaterialDesignFlatButton` |
| Hành động nguy hiểm | `MaterialDesignRaisedButton` + `Background`/`BorderBrush` = `{DynamicResource Brush.Danger}` |
| Nút icon | `MaterialDesignIconButton` (thanh công cụ dày: `MaterialDesignToolButton`) + `md:PackIcon` |
| Tiến trình | `MaterialDesignLinearProgressBar` |

Key không có trong bảng: kiểm tra trong `MaterialDesignThemes.Wpf.dll` 5.3.2 trước khi dùng (tên MD3 khác MD2).

## Window mẫu

```xml
<Window x:Class="HPRebar.WallReport.View.WallReportView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="Wall Report" Width="560" Height="480"
        FontFamily="Segoe UI"
        Background="{DynamicResource Brush.Background}"
        Foreground="{DynamicResource Brush.Foreground.Primary}">

    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <DockPanel Margin="16">
        <TextBlock DockPanel.Dock="Top" Text="Thống kê tường"
                   Style="{StaticResource MaterialDesignHeadline6TextBlock}" Margin="0,0,0,12"/>

        <!-- Action row -->
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,12,0,0">
            <Button Content="Xóa kết quả" Command="{Binding ClearCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Background="{DynamicResource Brush.Danger}" BorderBrush="{DynamicResource Brush.Danger}"
                    Margin="0,0,8,0"/>
            <Button Content="Đóng" Command="{Binding CloseCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
            <Button Content="Chạy" Command="{Binding RunCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"/>
        </StackPanel>

        <StackPanel>
            <md:Card Padding="12" UniformCornerRadius="4" md:ElevationAssist.Elevation="Dp2" Margin="0,0,0,12">
                <StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                        <md:PackIcon Kind="Wall" Width="18" Height="18"
                                     Foreground="{DynamicResource Brush.Accent}"/>
                        <TextBlock Text="Bộ lọc" Margin="8,0,0,0"
                                   Style="{StaticResource MaterialDesignSubtitle1TextBlock}"/>
                    </StackPanel>

                    <ComboBox ItemsSource="{Binding Levels}" SelectedItem="{Binding SelectedLevel}"
                              Style="{StaticResource MaterialDesignOutlinedComboBox}"
                              md:HintAssist.Hint="Level" Margin="0,0,0,8"/>

                    <TextBox Text="{Binding MinLength, UpdateSourceTrigger=PropertyChanged}"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             HorizontalContentAlignment="Right"
                             md:HintAssist.Hint="Chiều dài tối thiểu"
                             md:TextFieldAssist.SuffixText="mm" Margin="0,0,0,8"/>

                    <CheckBox Content="Chỉ tường kết cấu" IsChecked="{Binding StructuralOnly}"
                              Style="{StaticResource MaterialDesignCheckBox}"/>
                </StackPanel>
            </md:Card>

            <md:Card Padding="12" UniformCornerRadius="4" md:ElevationAssist.Elevation="Dp2">
                <StackPanel>
                    <TextBlock Text="Kết quả" Style="{StaticResource MaterialDesignSubtitle1TextBlock}"/>
                    <Separator Style="{StaticResource MaterialDesignLightSeparator}"/>
                    <TextBlock Text="{Binding Summary}" Style="{StaticResource MaterialDesignBody2TextBlock}"/>
                    <TextBlock Text="Đơn vị: mm" Style="{StaticResource MaterialDesignCaptionTextBlock}"
                               Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
                </StackPanel>
            </md:Card>
        </StackPanel>
    </DockPanel>
</Window>
```

Code-behind chỉ `InitializeComponent()` + `DataContext = viewModel` + `MaterialThemeBridge.Attach(this, hostTheme, …)`
(đổi Dark/Light theo host); không set `DataContext` trong XAML.
