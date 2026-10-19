using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPKata.Installer.Services;
using Microsoft.Win32;

namespace HPKata.Installer.ViewModels
{
    /// <summary>
    /// ViewModel for the HPKata Installer WPF user interface.
    /// Manages target path selection, async installation tasks, and live status logging.
    /// </summary>
    public sealed partial class InstallerViewModel : ObservableObject
    {
        private readonly StringBuilder _logBuilder = new StringBuilder();

        [ObservableProperty]
        private string _targetDirectory = InstallerEngine.DefaultTargetDirectory;

        [ObservableProperty]
        private string _logText = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Ready to install HPKata Suite.";

        public InstallerViewModel()
        {
            AppendLog("HPKata Setup initialized.");
        }

        [RelayCommand]
        public async Task InstallAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Installing HPKata...";
            AppendLog($"Installation target: {TargetDirectory}");

            try
            {
                bool success = await Task.Run(() => InstallerEngine.Install(TargetDirectory, AppendLog));
                StatusMessage = success ? "Installation complete!" : "Installation failed. Check log.";
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: async installation task exception handling
                AppendLog($"Error: {ex.Message}");
                StatusMessage = "Installation encountered an error.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task RepairAsync()
        {
            AppendLog("Starting repair (re-installing all components)...");
            await InstallAsync();
        }

        [RelayCommand]
        public async Task UninstallAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Uninstalling HPKata...";

            try
            {
                bool success = await Task.Run(() => InstallerEngine.Uninstall(TargetDirectory, AppendLog));
                StatusMessage = success ? "Uninstallation complete!" : "Uninstallation completed with warnings.";
            }
            catch (Exception ex)
            {
                // Annotated catch per Rule Q-B2: async uninstallation error handling
                AppendLog($"Error: {ex.Message}");
                StatusMessage = "Uninstallation encountered an error.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void BrowseTarget()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select HPKata Installation Directory",
                InitialDirectory = Directory.Exists(TargetDirectory) ? TargetDirectory : @"C:\"
            };

            if (dialog.ShowDialog() == true)
            {
                TargetDirectory = dialog.FolderName;
                AppendLog($"Selected destination: {TargetDirectory}");
            }
        }

        public void AppendLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _logBuilder.AppendLine(line);
            LogText = _logBuilder.ToString();
        }
    }
}
