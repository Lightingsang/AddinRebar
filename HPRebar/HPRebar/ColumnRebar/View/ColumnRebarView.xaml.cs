using System.Windows;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;
using HPRebar.ColumnRebar.ViewModel;

namespace HPRebar.ColumnRebar.View;

public partial class ColumnRebarView : Window
{
    public ColumnRebarView(ColumnRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).ColumnRebar);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
