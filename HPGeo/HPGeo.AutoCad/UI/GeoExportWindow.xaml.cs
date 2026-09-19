using System.Runtime.Loader;
using System.Windows;

namespace HPGeo.AutoCad.UI;

/// <summary>Code-behind: resources, DataContext, close on request. No logic.</summary>
public partial class GeoExportWindow : Window
{
    public GeoExportWindow(GeoExportViewModel viewModel)
    {
        // WPF resolves "/HPGeo.AutoCad;component/..." through Assembly.Load, which searches the default load
        // context — this assembly lives in the loader's own context, so the lookup must run inside it.
        using (AssemblyLoadContext.GetLoadContext(typeof(GeoExportWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            // Palette + MaterialDesign brushes for the current COLORTHEME, re-applied while the dialog is open.
            MaterialThemeBridge.Attach(this, HPGeoHostTheme.Instance);
            MapView.DarkTheme = HPGeoHostTheme.Instance.IsDark;
        }
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }

    /// <summary>False (settings.json "MapEnabled": false) keeps the satellite panel off: nothing leaves the machine before an export.</summary>
    public bool MapEnabled
    {
        get => MapView.Visibility == System.Windows.Visibility.Visible;
        set => MapView.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
}
