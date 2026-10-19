using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Abstractions;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Controllers;
using HPKata.AutoCad.Interop;
using HPKata.AutoCad.Tests.Mocks;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;
using HPKata.Core.Models;
using HPKata.Core.Serialization;
using Xunit;

namespace HPKata.AutoCad.Tests.Controllers
{
    public sealed class CadDetailingCommandTests
    {
        [Fact]
        public void ContinuousBeamCommands_DeclareExpectedCommandMethods()
        {
            var methods = typeof(ContinuousBeamCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var names = methods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                               .Select(a => a.GlobalName)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string[] expected = { "ve_dam", "vd", "ve_dam_all", "vda", "ve_dam_shop", "vds", "loadbeam" };
            foreach (var cmd in expected)
            {
                Assert.Contains(cmd, names);
            }
        }

        [Fact]
        public void ColumnCommands_DeclareExpectedCommandMethods()
        {
            var methods = typeof(ColumnCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var names = methods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                               .Select(a => a.GlobalName)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string[] expected = { "ve_cot", "vc", "ve_cot_shop", "vcs", "raicot", "dimcot", "tkcot", "loadcolumn", "loadcolumn_etabs" };
            foreach (var cmd in expected)
            {
                Assert.Contains(cmd, names);
            }
        }

        [Fact]
        public void WallCommands_DeclareExpectedCommandMethods()
        {
            var methods = typeof(WallCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var names = methods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                               .Select(a => a.GlobalName)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string[] expected = { "ve_vach", "vv", "ve_vach_shop", "vvs", "ve_loi_vach", "vlv", "loadwall", "loadwall_etabs" };
            foreach (var cmd in expected)
            {
                Assert.Contains(cmd, names);
            }
        }

        [Fact]
        public void ScheduleCommands_DeclareExcelExportAliases()
        {
            var methods = typeof(ScheduleCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var names = methods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                               .Select(a => a.GlobalName)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Contains("toexcel", names);
            Assert.Contains("toexcelqh", names);
        }

        [Fact]
        public void LoadBeam_FromKataXlsm_PopulatesBeamTakeoffRegistry()
        {
            BeamTakeoffRegistry.Clear();
            var sheetData = new KataBeamSheetData
            {
                BeamMark = "D10-TEST",
                DefaultWidthMm = 300,
                DefaultHeightMm = 600
            };
            sheetData.Spans.Add(new KataBeamSpanData
            {
                SpanIndex = 0,
                ClearLengthMm = 5000,
                WidthMm = 300,
                HeightMm = 600
            });

            var layout = KataBeamDataMapper.ToRebarLayout(sheetData);
            BeamTakeoffRegistry.Register(layout);

            Assert.True(BeamTakeoffRegistry.TryGet("D10-TEST", out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal("D10-TEST", retrieved.BeamName);
            Assert.Single(retrieved.Spans);
            BeamTakeoffRegistry.Clear();
        }

        [Fact]
        public void LoadColumn_FromKataXlsm_PopulatesColumnTakeoffRegistry()
        {
            ColumnTakeoffRegistry.Clear();
            var sheetData = new KataColumnSheetData
            {
                ColumnMark = "C5-TEST"
            };
            sheetData.Stories.Add(new KataColumnStoryData
            {
                StoryIndex = 0,
                StoryName = "Tang 1",
                StoryHeightMm = 3600,
                WidthMm = 400,
                DepthMm = 400,
                CornerDiameter = 20
            });

            var layout = KataColumnDataMapper.ToRebarLayout(sheetData);
            ColumnTakeoffRegistry.Register(layout);

            Assert.True(ColumnTakeoffRegistry.TryGet("C5-TEST", out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal("C5-TEST", retrieved.ColumnMark);
            Assert.Single(retrieved.Stories);
            ColumnTakeoffRegistry.Clear();
        }

        [Fact]
        public void LoadWall_FromKataXlsm_PopulatesWallTakeoffRegistry()
        {
            WallTakeoffRegistry.Clear();
            var sheetData = new KataWallSheetData
            {
                WallMark = "W2-TEST",
                ThicknessMm = 250
            };
            sheetData.Stories.Add(new KataWallStoryData
            {
                StoryIndex = 0,
                StoryName = "Tang 1",
                StoryHeightM = 3.6,
                ThicknessTwMm = 250,
                LengthLwMm = 3000
            });

            var model = KataWallDataMapper.ToShearWallModel(sheetData);
            WallTakeoffRegistry.Register(model);

            Assert.True(WallTakeoffRegistry.TryGet("W2-TEST", out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal("W2-TEST", retrieved.WallMark);
            Assert.Single(retrieved.Stories);
            WallTakeoffRegistry.Clear();
        }

        [Fact]
        public void ExportCommandRunner_ToExcel_CallsAdapterWithKataWorkbookPath()
        {
            var context = CreateSampleContext();
            var fakeAdapter = new FakeDetailingExcelAdapter();

            string resultPath = ExportCommandRunner.ExecuteExcelExport(
                context, isQhPlus: false, overridePath: @"C:\Fake\Kata.xlsm", adapter: fakeAdapter);

            Assert.Equal(@"C:\Fake\Kata.xlsm", resultPath);
            Assert.Equal(@"C:\Fake\Kata.xlsm", fakeAdapter.LastExportPath);
            Assert.True(fakeAdapter.ExportScheduleCalled);
        }

        [Fact]
        public void ExportCommandRunner_ToExcelQh_CallsAdapterWithIsoQhPlusPath()
        {
            var context = CreateSampleContext();
            var fakeAdapter = new FakeDetailingExcelAdapter();

            string resultPath = ExportCommandRunner.ExecuteExcelExport(
                context, isQhPlus: true, overridePath: @"C:\Fake\Iso_QHPlus.xls", adapter: fakeAdapter);

            Assert.Equal(@"C:\Fake\Iso_QHPlus.xls", resultPath);
            Assert.Equal(@"C:\Fake\Iso_QHPlus.xls", fakeAdapter.LastExportPath);
            Assert.True(fakeAdapter.ExportQhPlusScheduleCalled);
        }

        [Fact]
        public void KataLayerHelper_DefinesCanonicalLayerConstants()
        {
            Assert.Equal("kata_thep chu", KataLayerHelper.ThepDoc);
            Assert.Equal("kata_thep dai", KataLayerHelper.ThepDai);
            Assert.Equal("kata_net thay", KataLayerHelper.NetThay);
            Assert.Equal("kata_net khung", KataLayerHelper.NetKhung);
            Assert.Equal("kata_net khuat", KataLayerHelper.NetKhuat);
            Assert.Equal("kata_dim", KataLayerHelper.Dim);
            Assert.Equal("kata_text", KataLayerHelper.Text);
            Assert.Equal("kata_thep - V", KataLayerHelper.ThepWallV);
            Assert.Equal("kata_thep - H", KataLayerHelper.ThepWallH);
        }

        private static MockCadDocumentContext CreateSampleContext()
        {
            var context = new MockCadDocumentContext();
            context.Entities.Add(new CadEntityDto
            {
                Handle = "1001",
                Layer = KataLayerHelper.ThepDoc,
                EntityType = "Polyline",
                TotalLengthMm = 3500.0,
                XDataRecord = new KataXDataRecord
                {
                    Dang = "01",
                    STT = 1,
                    Phi = 20,
                    SoLuong = "4",
                    SoLuongCauKien = 1,
                    TenCauKien = "D10-TEST",
                    ChieuDaiCacDoanMm = new[] { 3500.0 }
                }
            });
            return context;
        }

        private sealed class FakeDetailingExcelAdapter : IKataExcelAdapter
        {
            public bool IsDesktopExcelOpen => false;
            public string? LastExportPath { get; private set; }
            public bool ExportScheduleCalled { get; private set; }
            public bool ExportQhPlusScheduleCalled { get; private set; }

            public bool ReadBeamSheet(string? path, out KataBeamSheetData data) { data = new KataBeamSheetData(); return true; }
            public bool WriteBeamSheet(string? path, in KataBeamSheetData data) => true;
            public bool ReadColumnSheet(string? path, out KataColumnSheetData data) { data = new KataColumnSheetData(); return true; }
            public bool WriteColumnSheet(string? path, in KataColumnSheetData data) => true;
            public bool ReadWallSheet(string? path, out KataWallSheetData data) { data = new KataWallSheetData(); return true; }
            public bool WriteWallSheet(string? path, in KataWallSheetData data) => true;

            public bool ExportSchedule(string? path, IEnumerable<KataRebarScheduleItem> items)
            {
                LastExportPath = path;
                ExportScheduleCalled = true;
                return true;
            }

            public bool ExportSchedule(string? path, IEnumerable<RebarItem> items)
            {
                LastExportPath = path;
                ExportScheduleCalled = true;
                return true;
            }

            public bool ExportQhPlusSchedule(string? path, IEnumerable<KataRebarScheduleItem> items)
            {
                LastExportPath = path;
                ExportQhPlusScheduleCalled = true;
                return true;
            }

            public bool ExportQhPlusSchedule(string? path, IEnumerable<RebarItem> items)
            {
                LastExportPath = path;
                ExportQhPlusScheduleCalled = true;
                return true;
            }
        }
    }
}
