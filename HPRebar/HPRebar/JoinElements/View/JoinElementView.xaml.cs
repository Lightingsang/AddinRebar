using System;
using System.Windows;
using HPRebar.JoinElements.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;

namespace HPRebar.JoinElements.View;

public partial class JoinElementView : Window
{
    public JoinElementView(JoinElementViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // Ensure window fits within the usable work area
        Height = Math.Min(Height, SystemParameters.WorkArea.Height - 40);
        Width = Math.Min(Width, SystemParameters.WorkArea.Width - 40);

        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).JoinElements);

        viewModel.RequestClose += Close;
    }
}
