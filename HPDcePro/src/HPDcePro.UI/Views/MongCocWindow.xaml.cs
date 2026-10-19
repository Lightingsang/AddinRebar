using System.Windows;
using MahApps.Metro.Controls;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;

namespace HPDcePro.UI.Views
{
    public partial class MongCocWindow : MetroWindow
    {
        public MongCocViewModel ViewModel { get; }

        public MongCocWindow(MongCocViewModel? viewModel = null)
        {
            ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            ThemeManager.Apply(this, dark: true);

            ViewModel = viewModel ?? new MongCocViewModel();
            DataContext = ViewModel;
        }

        private void OpenLopThepThem_Click(object sender, RoutedEventArgs e)
        {
            var win = new MongCocLopThepThemWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void OpenChonThepTuDong_Click(object sender, RoutedEventArgs e)
        {
            var win = new MongCocChonThepTuDongWindow();
            win.Owner = this;
            win.ShowDialog();
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
