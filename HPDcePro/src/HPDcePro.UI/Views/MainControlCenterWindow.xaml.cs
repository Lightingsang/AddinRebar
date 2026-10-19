using System;
using System.Windows;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;

namespace HPDcePro.UI.Views
{
    /// <summary>
    /// Interaction logic for MainControlCenterWindow.xaml.
    /// Acts as the central GETFORM hub connecting all 6 detailing and calculation modules.
    /// </summary>
    public partial class MainControlCenterWindow : MetroWindow
    {
        public MainControlCenterViewModel ViewModel { get; }

        public MainControlCenterWindow(MainControlCenterViewModel? viewModel = null)
        {
            ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            ThemeManager.Apply(this, dark: true);

            ViewModel = viewModel ?? new MainControlCenterViewModel();
            DataContext = ViewModel;
        }

        private void LoadProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "AutoStr Project (*.autostr)|*.autostr|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Chọn tệp dự án AutoStr (.autostr)"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ViewModel.LoadFromFile(dialog.FileName);
                    MessageBox.Show("Đã nạp thành công dữ liệu dự án!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // Reason: Inform user when project file cannot be read or parsed
                    MessageBox.Show($"Không thể đọc tệp dự án: {ex.Message}", "Lỗi nạp dự án", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "AutoStr Project (*.autostr)|*.autostr|JSON Files (*.json)|*.json",
                Title = "Lưu tệp dự án AutoStr (.autostr)",
                FileName = "DuAnMau.autostr"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ViewModel.SaveToFile(dialog.FileName);
                    MessageBox.Show("Đã lưu tệp dự án thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // Reason: Inform user when project file write operation fails
                    MessageBox.Show($"Lỗi khi ghi tệp dự án: {ex.Message}", "Lỗi lưu dự án", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
