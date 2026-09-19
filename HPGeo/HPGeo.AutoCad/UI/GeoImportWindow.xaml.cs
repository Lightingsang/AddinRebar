using System.Runtime.Loader;
using System.Windows;

namespace HPGeo.AutoCad.UI;

/// <summary>Code-behind: resources, DataContext, close on request. No logic.</summary>
public partial class GeoImportWindow : Window
{
    public GeoImportWindow(GeoImportViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(GeoImportWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            MaterialThemeBridge.Attach(this, HPGeoHostTheme.Instance);
        }
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }
}
