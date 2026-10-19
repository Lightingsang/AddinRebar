using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for WallDetailingWindow.xaml
    /// </summary>
    public partial class WallDetailingWindow : Window
    {
        public WallDetailingWindow()
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
        }

        public WallDetailingWindow(WallDetailingViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose()
        {
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
