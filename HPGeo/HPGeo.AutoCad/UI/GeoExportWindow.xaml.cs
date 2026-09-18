using System.Runtime.Loader;
using System.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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
            var dark = IsDarkTheme();
            Resources.MergedDictionaries.Add(ThemeResources.Brushes(dark));
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            MapView.DarkTheme = dark;
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

    /// <summary>COLORTHEME 0 = dark (AutoCAD's default), 1 = light; unreadable → dark.</summary>
    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(AcadApp.GetSystemVariable("COLORTHEME")) == 0; }
        catch (System.Exception) { return true; }
    }
}
