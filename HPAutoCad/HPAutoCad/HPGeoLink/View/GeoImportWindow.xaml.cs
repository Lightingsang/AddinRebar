using System.Runtime.Loader;
using System.Windows;
using HPAutoCad.HPGeoLink.ViewModel;
using HPAutoCad.Resources.Themes;

namespace HPAutoCad.HPGeoLink.View;

/// <summary>Code-behind: resources, DataContext, close on request. No logic.</summary>
public partial class GeoImportWindow : Window
{
    public GeoImportWindow(GeoImportViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(GeoImportWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            Icon = GeoIconHelper.WindowIcon;
            MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
        }
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }
}
