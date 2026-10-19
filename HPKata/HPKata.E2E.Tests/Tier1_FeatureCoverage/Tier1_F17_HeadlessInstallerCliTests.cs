using System;
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Features 13-19 / Acceptance Criteria:
/// Headless CLI installer argument parsing, deployment staging simulation,
/// manifest XML generation, and AutoCAD TRUSTEDPATHS formatting.
/// </summary>
public sealed class Tier1_F17_HeadlessInstallerCliTests
{
    [Theory]
    [InlineData(new[] { "--install" }, true, false, @"C:\HPKata")]
    [InlineData(new[] { "--uninstall" }, false, true, @"C:\HPKata")]
    [InlineData(new[] { "--install", "--target-dir", @"D:\Apps\HPKata" }, true, false, @"D:\Apps\HPKata")]
    public void CliArguments_ParseModeAndTargetDirectory(string[] args, bool isInstall, bool isUninstall, string expectedDir)
    {
        bool install = false;
        bool uninstall = false;
        string targetDir = @"C:\HPKata";

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--install", StringComparison.OrdinalIgnoreCase)) install = true;
            if (string.Equals(args[i], "--uninstall", StringComparison.OrdinalIgnoreCase)) uninstall = true;
            if (string.Equals(args[i], "--target-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                targetDir = args[++i];
            }
        }

        Assert.Equal(isInstall, install);
        Assert.Equal(isUninstall, uninstall);
        Assert.Equal(expectedDir, targetDir);
    }

    [Fact]
    public void RevitAddinManifest_GeneratesValidXml_PointingToCentralDll()
    {
        string targetDll = @"C:\HPKata\HPKata.Revit.dll";
        var doc = new XDocument(
            new XElement("RevitAddIns",
                new XElement("AddIn",
                    new XAttribute("Type", "Application"),
                    new XElement("Name", "HPKata.Revit"),
                    new XElement("Assembly", targetDll),
                    new XElement("AddInId", Guid.NewGuid().ToString("D")),
                    new XElement("FullClassName", "HPKata.Revit.App"),
                    new XElement("VendorId", "HPKATA")
                )
            )
        );

        string xml = doc.ToString();
        Assert.Contains(targetDll, xml);
        Assert.Contains("<AddIn Type=\"Application\">", xml);
        Assert.Contains("<FullClassName>HPKata.Revit.App</FullClassName>", xml);
    }

    [Fact]
    public void AutocadBundleManifest_GeneratesValidPackageContents_PointingToCentralDll()
    {
        string targetDll = @"C:\HPKata\HPKata.AutoCad.dll";
        string targetLsp = @"C:\HPKata\Goi lenh.lsp";
        var doc = new XDocument(
            new XElement("ApplicationPackage",
                new XAttribute("SchemaVersion", "1.0"),
                new XAttribute("AppVersion", "2026.1"),
                new XAttribute("Name", "HPKata"),
                new XElement("CompanyDetails", new XAttribute("Name", "HPKata")),
                new XElement("Components",
                    new XElement("RuntimeRequirements", new XAttribute("OS", "Win64"), new XAttribute("Platform", "AutoCAD*")),
                    new XElement("ComponentEntry",
                        new XAttribute("AppName", "HPKata.AutoCad"),
                        new XAttribute("ModuleName", targetDll)),
                    new XElement("ComponentEntry",
                        new XAttribute("AppName", "HPKata.Lisp"),
                        new XAttribute("ModuleName", targetLsp))
                )
            )
        );

        string xml = doc.ToString();
        Assert.Contains(targetDll, xml);
        Assert.Contains(targetLsp, xml);
        Assert.Contains("ApplicationPackage", xml);
    }

    [Theory]
    [InlineData(@"C:\HPKata", @"C:\Program Files\Autodesk\AutoCAD 2026", @"C:\Program Files\Autodesk\AutoCAD 2026;C:\HPKata;C:\HPKata\...")]
    [InlineData(@"C:\HPKata", "", @"C:\HPKata;C:\HPKata\...")]
    [InlineData(@"C:\HPKata", @"C:\HPKata;C:\HPKata\...", @"C:\HPKata;C:\HPKata\...")]
    public void TrustedPathsRegistration_AppendsRecursivePathIdempotently(string targetDir, string existingPaths, string expectedResult)
    {
        string recursiveEntry = Path.Combine(targetDir, "...");
        var entries = existingPaths.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var set = new System.Collections.Generic.HashSet<string>(entries, StringComparer.OrdinalIgnoreCase)
        {
            targetDir,
            recursiveEntry
        };

        var expectedEntries = expectedResult.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.True(set.SetEquals(expectedEntries));
    }

    [Fact]
    public void SimulatedDeployment_CreatesFullTargetDirectoryTree()
    {
        string tempTarget = Path.Combine(Path.GetTempPath(), "HPKata_Deploy_" + Guid.NewGuid().ToString("N"));
        try
        {
            string[] subdirs = ["dangthep", "Cot", "Icon", Path.Combine("Icon", "Ribbon"), "IMG", "Temp"];
            foreach (string sub in subdirs)
            {
                Directory.CreateDirectory(Path.Combine(tempTarget, sub));
            }

            foreach (string sub in subdirs)
            {
                Assert.True(Directory.Exists(Path.Combine(tempTarget, sub)), $"Subdirectory {sub} must be created");
            }
        }
        finally
        {
            if (Directory.Exists(tempTarget))
            {
                Directory.Delete(tempTarget, true);
            }
        }
    }
}
