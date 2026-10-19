using System.Windows;
using MahApps.Metro.Controls;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;

namespace HPDcePro.UI.Views
{
    public partial class SteelConnectionWindow : MetroWindow
    {
        public SteelConnectionViewModel ViewModel { get; }

        public SteelConnectionWindow(SteelConnectionViewModel? viewModel = null)
        {
            ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            ThemeManager.Apply(this, dark: true);

            ViewModel = viewModel ?? new SteelConnectionViewModel();
            DataContext = ViewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
            {
                DialogResult = true;
            }
            Close();
        }
    }
}
