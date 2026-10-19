using System.Windows;
using MahApps.Metro.Controls;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;

namespace HPDcePro.UI.Views
{
    public partial class MongCocChonThepTuDongWindow : MetroWindow
    {
        public MongCocChonThepTuDongViewModel ViewModel { get; }

        public MongCocChonThepTuDongWindow(MongCocChonThepTuDongViewModel? viewModel = null)
        {
            ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            ThemeManager.Apply(this, dark: true);

            ViewModel = viewModel ?? new MongCocChonThepTuDongViewModel();
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
