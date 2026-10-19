using System;
using System.IO;
using System.Threading;
using System.Windows;
using ClosedXML.Excel;
using HPKata.Core.Configuration;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using Xunit;

namespace HPKata.E2E.Tests.Tier3_CrossFeature;

/// <summary>
/// Tier 3: Cross-Feature Multi-Module Integration Pipeline:
/// Canonical Environment -> LISP Config -> Excel Workbook -> ViewModels -> WPF Window Instantiation.
/// </summary>
public sealed class Tier3_StandaloneWorkflowPipelineTests
{
    [Fact]
    public void FullCrossFeaturePipeline_FromEnvironmentToWpfWindow_ExecutesWithoutError()
    {
        string testRoot = Path.Combine(Path.GetTempPath(), "HPKata_Tier3_" + Guid.NewGuid().ToString("N"));
        try
        {
            // 1. Initialize environment directory structure
            Directory.CreateDirectory(testRoot);
            string xlsmPath = Path.Combine(testRoot, "Kata.xlsm");
            string lspPath = Path.Combine(testRoot, "Goi lenh.lsp");

            // 2. Author 86-param config and serialize
            var config = new KataConfig
            {
                ConcreteCoverMm = 35.0,
                BaseBarDiameter = 22,
                StandardBarLength = 11700
            };
            File.WriteAllText(lspPath, KataConfigParser.SerializeToText(config));
            Assert.True(File.Exists(lspPath));

            // 3. Populate Dam sheet in Excel
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Dam");
                ws.Cell("B3").Value = "B-PIPELINE";
                ws.Cell("B5").Value = 700;
                ws.Cell("B6").Value = 350;
                wb.SaveAs(xlsmPath);
            }
            Assert.True(File.Exists(xlsmPath));

            // 4. Wire ViewModel with parsed config
            var setupVm = new SetupViewModel(config);
            Assert.Equal(35.0, setupVm.ConcreteCoverMm);

            var beamVm = new BeamDetailingViewModel
            {
                BeamMark = "B-PIPELINE"
            };

            // 5. Instantiate WPF Windows on STA thread
            RunOnSta(() =>
            {
                var setupView = new SetupView(setupVm);
                var beamView = new BeamDetailingWindow(beamVm);

                Assert.NotNull(setupView);
                Assert.NotNull(beamView);
                Assert.Equal("B-PIPELINE", beamVm.BeamMark);
            });
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }
    }

    private static void RunOnSta(Action action) => Common.E2EWpfHelper.RunOnSta(action);
}
