using System;
using System.IO;
using System.Linq;
using System.Text;
using HPKata.AutoCad.Interop;
using HPKata.Core.Configuration;
using Xunit;

namespace HPKata.AutoCad.Tests
{
    public class KataDwgTemplateImporterTests
    {
        [Fact]
        public void ImportTemplateObjects_NullTarget_ThrowsArgumentNullException()
        {
            var importer = new KataDwgTemplateImporter();
            Assert.Throws<ArgumentNullException>(() => importer.ImportTemplateObjects(null!));
        }

        [Fact]
        public void EnsureStandardLayers_NullTarget_ThrowsArgumentNullException()
        {
            var importer = new KataDwgTemplateImporter();
            Assert.Throws<ArgumentNullException>(() => importer.EnsureStandardLayers(null!));
        }

        [Fact]
        public void EnsureStandardTextStyles_NullTarget_ThrowsArgumentNullException()
        {
            var importer = new KataDwgTemplateImporter();
            Assert.Throws<ArgumentNullException>(() => importer.EnsureStandardTextStyles(null!));
        }

        [Fact]
        public void StandardLayers_DefinesAll12CanonicalKataLayers()
        {
            string[] expectedLayers =
            [
                "kata_thep chu",
                "kata_thep dai",
                "kata_thep - V",
                "kata_thep - H",
                "kata_dim",
                "kata_net thay",
                "kata_net khung",
                "kata_net khuat",
                "kata_grid",
                "kata_net manh",
                "kata_text",
                "Defpoints"
            ];

            var actualNames = KataDwgTemplateImporter.StandardLayers.Select(l => l.Name).ToArray();
            Assert.Equal(12, actualNames.Length);
            foreach (string layer in expectedLayers)
            {
                Assert.Contains(layer, actualNames);
            }
        }

        [Fact]
        public void StandardTextStyle_MatchesKataTextArialSpecification()
        {
            Assert.Equal("kata_text", KataDwgTemplateImporter.StandardTextStyleName);
            Assert.Equal("arial.ttf", KataDwgTemplateImporter.StandardFontFileName);
            Assert.Equal(0.80, KataDwgTemplateImporter.StandardTextWidthFactor, 2);
        }

        [Fact]
        public void ResolveTemplatePath_DefaultsToCentralIsoKataDwgWithValidDwgHeader()
        {
            string resolved = KataDwgTemplateImporter.ResolveTemplatePath(null);
            Assert.Equal(KataInstallEnvironment.Current.IsoKataDwgPath, resolved, ignoreCase: true);

            if (File.Exists(resolved))
            {
                byte[] header = new byte[6];
                using var fs = new FileStream(resolved, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int read = fs.Read(header, 0, header.Length);
                Assert.Equal(6, read);
                Assert.Equal("AC1027", Encoding.ASCII.GetString(header));
            }
        }
    }
}
