using System.Windows;
using HPRebar.Resources.Themes;
using HPRebar.BeamRebar.ViewModel;

namespace HPRebar.BeamRebar.View;

/// <summary>
/// Code-behind for BeamRebarView. Initialises DataContext and subscribes to close event.
/// </summary>
public partial class BeamRebarView : Window
{
    public BeamRebarView(BeamRebarViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, icons => icons.BeamRebar);

        // Modeless: there is no DialogResult to hand back, the window just goes away.
        viewModel.CloseRequested += Close;
    }
}
