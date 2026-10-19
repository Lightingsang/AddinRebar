using System;
using System.IO;
using System.Reflection;
using HPKata.Core.Configuration;
using HPKata.UI.Common;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Controller for hosting and synchronizing SetupView with AutoCAD runtime.
    /// </summary>
    public static class SetupController
    {
        public static void OpenSetupView(bool isGeneralSetup = true)
        {
            try
            {
                string lspPath = ResolveGoiLenhPath();
                KataConfig config = File.Exists(lspPath)
                    ? KataConfigParser.ParseFile(lspPath)
                    : new KataConfig();

                var vm = new SetupViewModel(config);
                var view = new SetupView { DataContext = vm };

                ThemeManager.Attach(view, AutocadHostTheme.Instance);
                bool? dialogResult = AcadApp.ShowModalWindow(view);

                if (dialogResult == true || vm.IsDirty)
                {
                    SaveConfiguration(vm.ToConfig(), lspPath);
                }
            }
            catch (Exception ex)
            {
                var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage($"\n[HPKata] Error displaying setup: {ex.Message}\n");
            }
        }

        private static string ResolveGoiLenhPath()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string localLsp = Path.Combine(assemblyDir, "GoiLenh_HPKata.lsp");
            if (File.Exists(localLsp)) return localLsp;

            string envLsp = KataInstallEnvironment.Current.GoiLenhLspPath;
            if (File.Exists(envLsp)) return envLsp;

            return localLsp;
        }

        private static void SaveConfiguration(KataConfig config, string lspPath)
        {
            try
            {
                if (File.Exists(lspPath))
                {
                    string[] lines = File.ReadAllLines(lspPath);
                    if (lines.Length >= 237)
                    {
                        var configLines = KataConfigParser.SerializeToLines(config);
                        for (int i = 0; i < 86 && (151 + i) < lines.Length; i++)
                        {
                            lines[151 + i] = configLines[i];
                        }
                        File.WriteAllLines(lspPath, lines);
                    }
                }
            }
            catch (Exception ex)
            {
                var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage($"\n[HPKata] Warning saving configuration to {lspPath}: {ex.Message}\n");
            }
        }
    }
}
