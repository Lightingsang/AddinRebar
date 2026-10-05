using System.Windows;
using HPRebar.KataExport.ViewModel;
using HPRebar.Resources.Icons;
using HPRebar.Resources.Themes;

namespace HPRebar.KataExport.View;

public partial class KataExportView : Window
{
    public KataExportView(KataExportViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // The elevation needs the height; on a small screen the window must still fit, buttons included.
        Height = Math.Min(Height, SystemParameters.WorkArea.Height - 20);
        MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataExport);

        // Modeless: when CloseRequested is fired, close the window cleanly.
        viewModel.CloseRequested += Close;

        // Out of the way while beams are picked in Revit, back in front afterwards.
        viewModel.PickStarted += Hide;
        viewModel.PickEnded += () =>
        {
            if (IsVisible) return;
            Show();
            Activate();
        };
    }
}
