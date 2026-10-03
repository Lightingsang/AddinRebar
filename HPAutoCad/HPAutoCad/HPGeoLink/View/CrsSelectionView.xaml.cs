using System.Runtime.Loader;
using System.Windows.Controls;
using HPAutoCad.Resources.Themes;

namespace HPAutoCad.HPGeoLink.View;

/// <summary>Code-behind: InitializeComponent with themes; the owner binds DataContext to its CrsSelectionViewModel.</summary>
public partial class CrsSelectionView : UserControl
{
    public CrsSelectionView()
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(CrsSelectionView).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
        }
    }
}
