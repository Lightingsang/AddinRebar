using System.Runtime.Loader;
using System.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPGeo.AutoCad.UI;

/// <summary>Code-behind: resources, DataContext, close on request. No logic.</summary>
public partial class GeoImportWindow : Window
{
    public GeoImportWindow(GeoImportViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(GeoImportWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Brushes(IsDarkTheme()));
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
        }
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }

    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(AcadApp.GetSystemVariable("COLORTHEME")) == 0; }
        catch (System.Exception) { return true; }
    }
}
