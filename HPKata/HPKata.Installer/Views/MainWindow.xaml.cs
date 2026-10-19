using System.Windows;
using HPKata.Installer.ViewModels;

namespace HPKata.Installer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(InstallerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
