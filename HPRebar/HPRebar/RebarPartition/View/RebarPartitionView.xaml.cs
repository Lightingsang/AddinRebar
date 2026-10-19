using System;
using System.Windows;
using HPRebar.RebarPartition.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;

namespace HPRebar.RebarPartition.View;

public partial class RebarPartitionView : Window
{
    public RebarPartitionView(RebarPartitionViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // Ensure window fits within the usable work area
        Height = Math.Min(Height, SystemParameters.WorkArea.Height - 40);
        Width = Math.Min(Width, SystemParameters.WorkArea.Width - 40);

        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).RebarPartition);

        viewModel.RequestClose += Close;
    }
}
