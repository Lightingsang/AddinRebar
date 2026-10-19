using System;
using System.Windows;
using Microsoft.Win32;
using MahApps.Metro.Controls;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;

namespace HPDcePro.UI.Views
{
    public partial class AutoStrConfigWindow : MetroWindow
    {
        public AutoStrConfigViewModel ViewModel { get; }

        public AutoStrConfigWindow(AutoStrConfigViewModel? viewModel = null)
        {
            ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
            ThemeManager.Apply(this, dark: true);

            ViewModel = viewModel ?? new AutoStrConfigViewModel();
            DataContext = ViewModel;
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "AutoStr Project (*.autostr)|*.autostr|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Chọn tệp cấu hình AutoStr"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ViewModel.LoadFromFile(dialog.FileName);
                }
                catch (Exception ex)
                {
                    // Reason: Display user-friendly notification when invalid JSON or IO error occurs
                    MessageBox.Show($"Không thể đọc tệp cấu hình: {ex.Message}", "Lỗi tải tệp", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "AutoStr Project (*.autostr)|*.autostr|JSON Files (*.json)|*.json",
                Title = "Lưu tệp cấu hình AutoStr",
                FileName = "DuAnMau.autostr"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ViewModel.SaveToFile(dialog.FileName);
                    MessageBox.Show("Đã lưu tệp cấu hình thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // Reason: Notify user if write permission or file path is invalid
                    MessageBox.Show($"Lỗi khi ghi tệp: {ex.Message}", "Lỗi lưu tệp", MessageBoxButton.OK, MessageBoxImage.Error);
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
