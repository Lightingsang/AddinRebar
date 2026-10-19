using System.Windows;
using HPKata.UI.ViewModels;

namespace HPKata.UI.Views
{
    /// <summary>
    /// Interaction logic for ColumnDetailingWindow.xaml
    /// </summary>
    public partial class ColumnDetailingWindow : Window
    {
        public ColumnDetailingWindow()
        {
            HPKata.UI.Common.ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            HPKata.UI.Common.ThemeManager.Apply(this, dark: true);
        }

        public ColumnDetailingWindow(ColumnDetailingViewModel viewModel) : this()
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
