using System.Windows;
using HPRebar.ColumnRebar.Service;
using HPRebar.ColumnRebar.ViewModel;

namespace HPRebar.ColumnRebar.View;

public partial class ColumnRebarView : Window
{
    public ColumnRebarView(ColumnRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        ThemeSwitcher.ApplyFromRevit(this);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
