using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for CuttingStockView.xaml
    /// </summary>
    public partial class CuttingStockView : Window
    {
        public CuttingStockViewModel ViewModel { get; }

        public CuttingStockView(CuttingStockViewModel viewModel)
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        public CuttingStockView() : this(new CuttingStockViewModel())
        {
        }
    }
}
