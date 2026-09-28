using System;
using System.Windows;
using HPRebar.KataRebar.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;

namespace HPRebar.KataRebar.View;

public partial class KataRebarView : Window
{
    public KataRebarView(KataRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // Ensure window fits comfortably within work area
        Height = Math.Min(Height, SystemParameters.WorkArea.Height - 30);
        Width = Math.Min(Width, SystemParameters.WorkArea.Width - 30);

        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataRebar);

        viewModel.CloseRequested += Close;
    }
}
