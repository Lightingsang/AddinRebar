using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    public partial class DrawingUtilityWindow : Window
    {
        public DrawingUtilityWindow(DrawingUtilityViewModel? viewModel = null)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            DataContext = viewModel ?? new DrawingUtilityViewModel();
        }
    }
}
