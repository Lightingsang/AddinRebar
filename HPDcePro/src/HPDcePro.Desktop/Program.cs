using System;
using System.IO;
using System.Linq;
using System.Windows;
using HPDcePro.Core.Common;
using HPDcePro.UI.Common;
using HPDcePro.UI.ViewModels;
using HPDcePro.UI.Views;

namespace HPDcePro.Desktop
{
    public static class Program
    {
        public static string DefaultAutoStrSamplePath => AssetPathResolver.ResolveAutoStrProject("DuAnMau.autostr");

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                ThemeManager.EnsureAssembliesLoaded();

                var viewModel = CreateInitialViewModel(args);
                if (args != null && args.Contains("--headless-smoke", StringComparer.OrdinalIgnoreCase))
                {
                    return string.IsNullOrWhiteSpace(viewModel.ProjectTitle) ? 1 : 0;
                }

                var app = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
                var mainWindow = new MainControlCenterWindow(viewModel);
                ThemeManager.Apply(mainWindow, dark: true);
                app.MainWindow = mainWindow;
                app.Run(mainWindow);
                return 0;
            }
            catch (Exception ex)
            {
                // Reason: Return non-zero exit code if standalone desktop startup encounters an unrecoverable error
                Console.Error.WriteLine($"HPDcePro.Desktop error: {ex.Message}");
                return 1;
            }
        }

        public static MainControlCenterViewModel CreateInitialViewModel(string[]? args = null)
        {
            var vm = new MainControlCenterViewModel();
            string? customPath = args?.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal) && File.Exists(a));
            string pathToLoad = customPath ?? DefaultAutoStrSamplePath;

            if (File.Exists(pathToLoad))
            {
                try
                {
                    vm.LoadFromFile(pathToLoad);
                }
                catch (Exception)
                {
                    // Reason: Keep default KPI metrics if sample .autostr file cannot be parsed on startup
                }
            }

            return vm;
        }
    }
}
