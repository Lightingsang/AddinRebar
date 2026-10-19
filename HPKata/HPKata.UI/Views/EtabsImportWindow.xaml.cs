using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for EtabsImportWindow.xaml
    /// </summary>
    public partial class EtabsImportWindow : Window
    {
        public EtabsImportWindow()
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
        }

        public EtabsImportWindow(EtabsImportViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose()
        {
            Close();
        }
    }
}
