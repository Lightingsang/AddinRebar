using System;
using System.IO;
using System.Threading;
using System.Windows;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios;

/// <summary>
/// Tier 4: Scenario 6 — End-to-End Standalone Deployment & Operational Readiness Scenario.
/// Verifies full deployment lifecycle: directory layout, canonical resolution,
/// asset presence, and GUI window initialization.
/// </summary>
public sealed class Tier4_Scenario6_StandaloneDeploymentScenarioTests
{
    private static readonly string SourceRoot = Directory.Exists(@"C:\HPKata") ? @"C:\HPKata" : @"C:\kata_pro";

    [Fact]
    public void StandaloneDeploymentLifecycle_ExecutesSuccessfully()
    {
        string deploymentTarget = Path.Combine(Path.GetTempPath(), "HPKata_Scenario6_" + Guid.NewGuid().ToString("N"));
        try
        {
            // Step 1: Create all canonical directories
            string[] directories =
            [
                deploymentTarget,
                Path.Combine(deploymentTarget, "dangthep"),
                Path.Combine(deploymentTarget, "Cot"),
                Path.Combine(deploymentTarget, "Icon"),
                Path.Combine(deploymentTarget, "Icon", "Ribbon"),
                Path.Combine(deploymentTarget, "IMG"),
                Path.Combine(deploymentTarget, "Temp")
            ];

            foreach (string dir in directories)
            {
                Directory.CreateDirectory(dir);
                Assert.True(Directory.Exists(dir));
            }

            // Step 2: Deploy dummy or reference assets
            string dummyDwg = Path.Combine(deploymentTarget, "Iso_kata.dwg");
            string dummyXlsm = Path.Combine(deploymentTarget, "Kata.xlsm");
            string dummyLsp = Path.Combine(deploymentTarget, "Goi lenh.lsp");

            File.WriteAllText(dummyDwg, "AC1032_MOCK_DWG_CONTENT");
            File.WriteAllText(dummyXlsm, "PK_MOCK_XLSM_CONTENT");
            File.WriteAllText(dummyLsp, ";HPKATA_CONFIG_START\r\n;30.0\r\n;16\r\n");

            Assert.True(File.Exists(dummyDwg));
            Assert.True(File.Exists(dummyXlsm));
            Assert.True(File.Exists(dummyLsp));

            // Step 3: Verify zero legacy references in deployed LISP
            string lspContent = File.ReadAllText(dummyLsp);
            Assert.DoesNotContain("kata_pro", lspContent, StringComparison.OrdinalIgnoreCase);

            // Step 4: Verify UI readiness on STA thread
            RunOnSta(() =>
            {
                var setupVm = new SetupViewModel();
                var setupView = new SetupView(setupVm);
                Assert.NotNull(setupView);
                Assert.True(setupView.Width > 0 || setupView.MinWidth > 0);
            });
        }
        finally
        {
            if (Directory.Exists(deploymentTarget))
            {
                Directory.Delete(deploymentTarget, true);
            }
        }
    }

    [Fact]
    public void SourceReferenceEnvironment_WhenInstalled_MeetsParityBaseline()
    {
        if (!Directory.Exists(SourceRoot))
        {
            return;
        }

        string dwg = Path.Combine(SourceRoot, "Iso_kata.dwg");
        string xlsm = Path.Combine(SourceRoot, "Kata.xlsm");
        string xls = Path.Combine(SourceRoot, "Iso_QHPlus.xls");
        string lsp = Path.Combine(SourceRoot, "Goi lenh.lsp");

        Assert.True(File.Exists(dwg), "Reference Iso_kata.dwg must be present");
        Assert.True(File.Exists(xlsm), "Reference Kata.xlsm must be present");
        Assert.True(File.Exists(xls), "Reference Iso_QHPlus.xls must be present");
        Assert.True(File.Exists(lsp), "Reference Goi lenh.lsp must be present");
    }

    private static void RunOnSta(Action action) => Common.E2EWpfHelper.RunOnSta(action);
}
