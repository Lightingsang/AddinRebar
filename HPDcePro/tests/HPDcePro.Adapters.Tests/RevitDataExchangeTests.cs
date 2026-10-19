using System;
using HPDcePro.Core.Geometry;
using HPDcePro.Revit;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class RevitDataExchangeTests
    {
        [Fact]
        public void UnitConversions_ConvertAccurately()
        {
            double feet = 10.0;
            double mm = RevitDataExchangeService.FeetToMm(feet);
            Assert.Equal(3048.0, mm, precision: 4);

            double feetBack = RevitDataExchangeService.MmToFeet(mm);
            Assert.Equal(feet, feetBack, precision: 4);
        }

        [Fact]
        public void FramingElementModel_InitializesProperly()
        {
            var framing = new RevitFramingElement
            {
                UniqueId = "guid-123",
                Mark = "B1",
                FamilyAndTypeName = "Concrete-Rectangular Beam: 220x400",
                WidthMm = 220.0,
                HeightMm = 400.0,
                LengthMm = 5000.0,
                StartPoint = new Point3D(0, 0, 3600),
                EndPoint = new Point3D(5000, 0, 3600),
                LevelName = "Level 2"
            };

            Assert.Equal("B1", framing.Mark);
            Assert.Equal(220.0, framing.WidthMm);
            Assert.Equal(400.0, framing.HeightMm);
            Assert.Equal(5000.0, framing.LengthMm);
        }

        [Fact]
        public void ColumnElementModel_InitializesProperly()
        {
            var col = new RevitColumnElement
            {
                UniqueId = "guid-col-456",
                Mark = "C1",
                FamilyAndTypeName = "Concrete-Rectangular Column: 220x220",
                WidthMm = 220.0,
                HeightMm = 220.0,
                LengthMm = 3600.0,
                Location = new Point3D(0, 0, 0),
                LevelName = "Level 1"
            };

            Assert.Equal("C1", col.Mark);
            Assert.Equal(3600.0, col.LengthMm);
        }

        [Fact]
        public void DeploymentManifests_RevitAddinAndAutoCadBundle_ContainExpectedEntries()
        {
            string deployDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "deploy"));

            string revitAddinPath = System.IO.Path.Combine(deployDir, "HPDcePro.Revit.addin");
            Assert.True(System.IO.File.Exists(revitAddinPath));
            string addinXml = System.IO.File.ReadAllText(revitAddinPath);
            Assert.Contains("HPDcePro.Revit.DceProRevitApplication", addinXml);
            Assert.Contains("HPDcePro.Revit.DceProControlCenterCommand", addinXml);
            Assert.Contains("HPDcePro.Revit.ExportFramingCommand", addinXml);

            string bundleXmlPath = System.IO.Path.Combine(deployDir, "HPDcePro.bundle", "PackageContents.xml");
            Assert.True(System.IO.File.Exists(bundleXmlPath));
            string bundleXml = System.IO.File.ReadAllText(bundleXmlPath);
            Assert.Contains("HPDcePro.AutoCad.dll", bundleXml);
            Assert.Contains("HPDcePro.lsp", bundleXml);
        }
    }
}
