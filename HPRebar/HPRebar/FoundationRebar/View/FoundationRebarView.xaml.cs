using System.Windows;
using HPRebar.FoundationRebar.Service;
using HPRebar.FoundationRebar.ViewModel;

namespace HPRebar.FoundationRebar.View;

public partial class FoundationRebarView : Window
{
    public FoundationRebarView(FoundationRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        ThemeSwitcher.ApplyFromRevit(this);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
