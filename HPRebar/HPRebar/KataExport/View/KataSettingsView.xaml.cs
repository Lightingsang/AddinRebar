using System;
using System.Windows;
using HPRebar.KataExport.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;

namespace HPRebar.KataExport.View;

public partial class KataSettingsView : Window
{
    public KataSettingsView(KataSettingsViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Height = Math.Min(Height, SystemParameters.WorkArea.Height - 30);
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataExport);
    }
}
