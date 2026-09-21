using System;
using System.Windows;
using System.Windows.Media;
using HPTekla.McpBridge.ViewModels;

namespace HPTekla.McpBridge.Views;

public partial class BridgeStatusWindow : Window
{
    private readonly BridgeStatusViewModel _viewModel;

    public BridgeStatusWindow(BridgeStatusViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.CloseRequested += Close;
        _viewModel.ThemeChanged += ApplyTheme;

        Closed += (_, _) => _viewModel.Detach();
    }

    private void ApplyTheme()
    {
        var isDark = _viewModel.IsDarkMode;
        var res = Resources;

        if (isDark)
        {
            res["Brush.Background"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            res["Brush.Surface"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26));
            res["Brush.Surface.Light"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
            res["Brush.Border"] = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46));
            res["Brush.Foreground.Primary"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF1, 0xF1));
            res["Brush.Foreground.Secondary"] = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
        }
        else
        {
            res["Brush.Background"] = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            res["Brush.Surface"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            res["Brush.Surface.Light"] = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
            res["Brush.Border"] = new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xDC));
            res["Brush.Foreground.Primary"] = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));
            res["Brush.Foreground.Secondary"] = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
        }
    }
}
