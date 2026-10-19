using System;
using System.IO;
using HPKata.Installer.Services;
using HPKata.Installer.Views;

namespace HPKata.Installer
{
    /// <summary>
    /// Application entry point supporting both headless CLI automation and interactive WPF GUI mode.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                return RunHeadlessCli(args);
            }

            var app = new App();
            var window = new MainWindow();
            return app.Run(window);
        }

        private static int RunHeadlessCli(string[] args)
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch (IOException) { }
            string? targetDir = null;
            string? excelPath = null;
            string? outDwgPath = null;
            string? logFilePath = null;
            bool isInstall = false;
            bool isUninstall = false;
            bool isDrawBeamFromExcel = false;
            bool closeAcad = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "--install", StringComparison.OrdinalIgnoreCase)) isInstall = true;
                else if (string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase)) isUninstall = true;
                else if (string.Equals(arg, "--draw-beam-from-excel", StringComparison.OrdinalIgnoreCase)) isDrawBeamFromExcel = true;
                else if (string.Equals(arg, "--close-acad", StringComparison.OrdinalIgnoreCase)) closeAcad = true;
                else if (string.Equals(arg, "--target-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) targetDir = args[++i];
                else if (string.Equals(arg, "--excel", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) excelPath = args[++i];
                else if (string.Equals(arg, "--out-dwg", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) outDwgPath = args[++i];
                else if (string.Equals(arg, "--log-file", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) logFilePath = args[++i];
            }

            Action<string> logger = msg =>
            {
                Console.WriteLine(msg);
                if (!string.IsNullOrWhiteSpace(logFilePath))
                {
                    File.AppendAllText(logFilePath, msg + Environment.NewLine, System.Text.Encoding.UTF8);
                }
            };

            if (isDrawBeamFromExcel)
            {
                if (!string.IsNullOrWhiteSpace(logFilePath) && File.Exists(logFilePath)) File.Delete(logFilePath);
                return AutocadMcpBeamRunner.Execute(excelPath, outDwgPath, closeAcad, logger);
            }

            if (isUninstall)
            {
                logger($"[HPKata] Executing headless uninstall (Target: {targetDir ?? InstallerEngine.DefaultTargetDirectory})...");
                return InstallerEngine.Uninstall(targetDir, logger) ? 0 : 1;
            }

            if (isInstall)
            {
                logger($"[HPKata] Executing headless install (Target: {targetDir ?? InstallerEngine.DefaultTargetDirectory})...");
                return InstallerEngine.Install(targetDir, logger) ? 0 : 1;
            }

            logger("[HPKata Setup Usage]");
            logger("  HPKata_Setup.exe                               Launch interactive installer GUI");
            logger("  HPKata_Setup.exe --install                     Install to default C:\\HPKata");
            logger("  HPKata_Setup.exe --uninstall                   Uninstall HPKata and remove integration");
            logger("  HPKata_Setup.exe --draw-beam-from-excel        Read C:\\HPKata\\Kata.xlsm & draw beam in AutoCAD via MCP");
            return 0;
        }
    }
}
