using System.Windows;
using HPRebar.Resources.Themes;
using HPRebar.FoundationRebar.ViewModel;

namespace HPRebar.FoundationRebar.View;

public partial class FoundationRebarView : Window
{
    public FoundationRebarView(FoundationRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, icons => icons.FoundationRebar);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
