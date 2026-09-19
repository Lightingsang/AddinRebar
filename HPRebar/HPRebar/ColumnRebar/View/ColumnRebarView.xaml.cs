using System.Windows;
using HPRebar.Resources.Themes;
using HPRebar.ColumnRebar.ViewModel;

namespace HPRebar.ColumnRebar.View;

public partial class ColumnRebarView : Window
{
    public ColumnRebarView(ColumnRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, icons => icons.ColumnRebar);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
