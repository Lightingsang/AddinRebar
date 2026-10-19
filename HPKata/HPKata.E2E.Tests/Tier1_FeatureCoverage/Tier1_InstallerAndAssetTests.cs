using System;
using System.IO;
using System.Xml.Linq;
using HPKata.Installer.Services;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage
{
    public class Tier1_InstallerAndAssetTests
    {
        [Fact]
        public void InstallerEngine_DeploysToIsolatedTarget_Successfully()
        {
            string customTarget = Path.Combine(Path.GetTempPath(), "HPKata_InstallTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                bool success = InstallerEngine.Install(customTarget, msg => { });
                Assert.True(success, "InstallerEngine.Install must succeed");

                // Assert directory structure
                Assert.True(Directory.Exists(customTarget));
                Assert.True(Directory.Exists(Path.Combine(customTarget, "dangthep")));
                Assert.True(Directory.Exists(Path.Combine(customTarget, "Cot")));
                Assert.True(Directory.Exists(Path.Combine(customTarget, "Icon")));
                Assert.True(Directory.Exists(Path.Combine(customTarget, "IMG")));
                Assert.True(Directory.Exists(Path.Combine(customTarget, "Temp")));

                // Assert library assets presence if source assets exist
                if (Directory.Exists(@"C:\HPKata"))
                {
                    Assert.True(File.Exists(Path.Combine(customTarget, "Iso_kata.dwg")));
                    Assert.True(File.Exists(Path.Combine(customTarget, "Kata.xlsm")));
                    Assert.True(File.Exists(Path.Combine(customTarget, "Goi lenh.lsp")));
                }

                // Verify manifests XML formatting
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string revitAddin = Path.Combine(appData, @"Autodesk\Revit\Addins\2026\HPKata.Revit.addin");
                string acadBundle = Path.Combine(appData, @"Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml");

                if (File.Exists(revitAddin))
                {
                    string revitXml = File.ReadAllText(revitAddin);
                    var doc = XDocument.Parse(revitXml);
                    Assert.NotNull(doc.Root);
                    Assert.DoesNotContain("kata_pro", revitXml, StringComparison.OrdinalIgnoreCase);
                }

                if (File.Exists(acadBundle))
                {
                    string acadXml = File.ReadAllText(acadBundle);
                    var doc = XDocument.Parse(acadXml);
                    Assert.NotNull(doc.Root);
                    Assert.DoesNotContain("kata_pro", acadXml, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                InstallerEngine.Uninstall(customTarget, msg => { });
                if (Directory.Exists(customTarget))
                {
                    Directory.Delete(customTarget, true);
                }
            }
        }

        [Fact]
        public void DeployRevitManifest_GeneratesCompliantXmlWithTargetPath()
        {
            string customTarget = @"D:\Apps\HPKata";
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string manifestPath = Path.Combine(appData, @"Autodesk\Revit\Addins\2026\HPKata.Revit.addin");

            InstallerEngine.DeployRevitManifest(customTarget, null);

            Assert.True(File.Exists(manifestPath));
            string content = File.ReadAllText(manifestPath);

            Assert.Contains(@"D:\Apps\HPKata\HPKata.Revit.dll", content);
            Assert.Contains("C184E347-734B-4811-9A77-9E49B7423D19", content);
            Assert.DoesNotContain("kata_pro", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DeployAutoCadBundle_GeneratesCompliantPackageContentsXml()
        {
            string customTarget = @"D:\Apps\HPKata";
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bundleXmlPath = Path.Combine(appData, @"Autodesk\ApplicationPlugins\HPKata.bundle\PackageContents.xml");

            InstallerEngine.DeployAutoCadBundle(customTarget, null);

            Assert.True(File.Exists(bundleXmlPath));
            string content = File.ReadAllText(bundleXmlPath);

            Assert.Contains(@"D:\Apps\HPKata\HPKata.AutoCad.dll", content);
            Assert.Contains(@"D:\Apps\HPKata\Goi lenh.lsp", content);
            Assert.DoesNotContain("kata_pro", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
