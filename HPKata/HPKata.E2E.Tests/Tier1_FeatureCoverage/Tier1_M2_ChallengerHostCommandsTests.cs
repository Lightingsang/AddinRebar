using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Controllers;
using HPKata.AutoCad.Interop;
using HPKata.Core.Beams.Models;
using HPKata.Core.Columns.Models;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;
using HPKata.Core.Models;
using HPKata.Core.Serialization;
using HPKata.Core.Walls.Models;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Runners;
using HPKata.Revit.Services;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Empirical challenge suite for Host Commands and Detailing verification (Milestone M2, Features 7 & 8).
/// Verifies AutoCAD detailing commands, registries, KataEntityStyler XData, and Revit parameter calculations.
/// </summary>
public sealed class Tier1_M2_ChallengerHostCommandsTests
{
    [Fact]
    public void AutoCad_DetailingCommands_DeclareAllRequiredAliases()
    {
        var continuousBeamMethods = typeof(ContinuousBeamCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var beamAliases = continuousBeamMethods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                                               .Select(a => a.GlobalName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("loadbeam", beamAliases);
        Assert.Contains("ve_dam", beamAliases);
        Assert.Contains("vd", beamAliases);
        Assert.Contains("ve_dam_shop", beamAliases);
        Assert.Contains("vds", beamAliases);

        var columnMethods = typeof(ColumnCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var colAliases = columnMethods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                                      .Select(a => a.GlobalName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("loadcolumn", colAliases);
        Assert.Contains("ve_cot", colAliases);
        Assert.Contains("vc", colAliases);
        Assert.Contains("ve_cot_shop", colAliases);
        Assert.Contains("vcs", colAliases);

        var wallMethods = typeof(WallCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var wallAliases = wallMethods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                                     .Select(a => a.GlobalName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("loadwall", wallAliases);
        Assert.Contains("ve_vach", wallAliases);
        Assert.Contains("vv", wallAliases);
        Assert.Contains("ve_vach_shop", wallAliases);
        Assert.Contains("vvs", wallAliases);

        var scheduleMethods = typeof(ScheduleCommands).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var scheduleAliases = scheduleMethods.SelectMany(m => m.GetCustomAttributes<CommandMethodAttribute>())
                                             .Select(a => a.GlobalName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("toexcel", scheduleAliases);
        Assert.Contains("toexcelqh", scheduleAliases);
    }

    [Fact]
    public void AutoCad_Registries_PopulateRetrieveAndClearCleanly()
    {
        BeamTakeoffRegistry.Clear();
        ColumnTakeoffRegistry.Clear();
        WallTakeoffRegistry.Clear();

        var beamLayout = new BeamRebarLayout { BeamName = "B_EMPIRICAL" };
        beamLayout.Spans.Add(new BeamSpanModel { SpanIndex = 0, SpanName = "Nhip 1", ClearLengthMm = 6000, WidthMm = 350, HeightMm = 700 });
        BeamTakeoffRegistry.Register(beamLayout);
        Assert.True(BeamTakeoffRegistry.TryGet("B_EMPIRICAL", out var bOut));
        Assert.NotNull(bOut);
        Assert.Equal("B_EMPIRICAL", bOut.BeamName);

        var colLayout = new ColumnRebarLayout { ColumnMark = "C_EMPIRICAL" };
        colLayout.Stories.Add(new ColumnStorySegment { StoryIndex = 0, StoryName = "Tang 1", StoryHeightMm = 3600, WidthMm = 500, DepthMm = 500 });
        ColumnTakeoffRegistry.Register(colLayout);
        Assert.True(ColumnTakeoffRegistry.TryGet("C_EMPIRICAL", out var cOut));
        Assert.NotNull(cOut);
        Assert.Equal("C_EMPIRICAL", cOut.ColumnMark);

        var wallModel = new ShearWallModel { WallMark = "SW_EMPIRICAL", ThicknessMm = 300, LengthMm = 4000 };
        wallModel.Stories.Add(new WallStorySegment { StoryIndex = 0, StoryName = "Tang 1", StoryHeightMm = 3600, ThicknessMm = 300, LengthMm = 4000 });
        WallTakeoffRegistry.Register(wallModel);
        Assert.True(WallTakeoffRegistry.TryGet("SW_EMPIRICAL", out var wOut));
        Assert.NotNull(wOut);
        Assert.Equal("SW_EMPIRICAL", wOut.WallMark);

        BeamTakeoffRegistry.Clear();
        ColumnTakeoffRegistry.Clear();
        WallTakeoffRegistry.Clear();
        Assert.False(BeamTakeoffRegistry.TryGet("B_EMPIRICAL", out _));
        Assert.False(ColumnTakeoffRegistry.TryGet("C_EMPIRICAL", out _));
        Assert.False(WallTakeoffRegistry.TryGet("SW_EMPIRICAL", out _));
    }

    [Fact]
    public void KataEntityStyler_NullArgumentsValidation()
    {
        Assert.Throws<ArgumentNullException>(() =>
            KataEntityStyler.StyleAndTag(null!, null!, null!, null!, KataLayerHelper.ThepDoc));
    }

    [Fact]
    public void KataXDataRecord_FullSerializationRoundTrip_MatchesKataSpecification()
    {
        var record = new KataXDataRecord
        {
            Dang = "01",
            STT = 42,
            Phi = 25,
            KhoangCach = "@200",
            SoLuong = "6",
            SoLuongCauKien = 2,
            ChieuDaiCacDoanMm = [250.0, 5800.0, 250.0],
            GocUon = "90",
            TenCauKien = "D101",
            SttThepChinh = "1",
            ChuThich = "Gia cuong",
            Z = "+3.600"
        };

        var typedValues = XDataService.ToTypedValues(record);
        Assert.NotNull(typedValues);
        Assert.True(typedValues.Length >= 13);
        Assert.Equal("KATA", typedValues[0].Value?.ToString());

        var recovered = XDataService.FromTypedValues(typedValues);
        Assert.Equal(record.Dang, recovered.Dang);
        Assert.Equal(record.STT, recovered.STT);
        Assert.Equal(record.Phi, recovered.Phi);
        Assert.Equal(record.SoLuong, recovered.SoLuong);
        Assert.Equal(record.TenCauKien, recovered.TenCauKien);
        Assert.Equal(3, recovered.ChieuDaiCacDoanMm.Length);
        Assert.Equal(5800.0, recovered.ChieuDaiCacDoanMm[1]);
    }

    [Fact]
    public void KataLayerHelper_CanonicalLayersIntegrity()
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

    [Fact]
    public void RevitGeometryCalculators_BoundaryAndExtremeValues_DoNotProduceNaNOrInf()
    {
        RevitPoint3D startPt = new(0.0, 0.0, 10.0);
        RevitPoint3D endPt = new(20.0, 0.0, 10.0);
        RevitPoint3D normal = new(0.0, 1.0, 0.0);

        var (botStart0, botEnd0) = FramingRebarGeometryCalculator.CalculateBottomBarEndpoints(
            startPt, endPt, normal, 0.0, 2.0, 0.0);
        Assert.False(double.IsNaN(botStart0.X) || double.IsInfinity(botStart0.X));
        Assert.False(double.IsNaN(botEnd0.Z) || double.IsInfinity(botEnd0.Z));

        double extremeWidthFt = RevitUnitConverter.MmToFeet(1200.0);
        double extremeDepthFt = RevitUnitConverter.MmToFeet(1200.0);
        double extremeCoverFt = RevitUnitConverter.MmToFeet(50.0);

        double halfW = (extremeWidthFt / 2.0) - extremeCoverFt;
        double halfD = (extremeDepthFt / 2.0) - extremeCoverFt;
        Assert.True(halfW > 0.0);
        Assert.True(halfD > 0.0);
        Assert.False(double.IsNaN(halfW) || double.IsInfinity(halfW));

        double wallThickFt = RevitUnitConverter.MmToFeet(500.0);
        double wallCoverFt = RevitUnitConverter.MmToFeet(30.0);
        double curtainOffsetFt = (wallThickFt / 2.0) - wallCoverFt;
        Assert.True(curtainOffsetFt > 0.0);
        Assert.Equal(220.0, RevitUnitConverter.FeetToMm(curtainOffsetFt), 0.01);
    }

    [Fact]
    public void RevitExportExcelRunner_FallbackToClosedXml_CreatesValidWorkbook()
    {
        var context = new Common.E2ERevitContext { DocumentTitle = "ChallengerTestDoc" };
        context.AllRebars.Add(Common.E2EDataFactory.CreateRevitRebarDto(
            999, "Dam_D1", "1", "RebarShape 00", 22, 4, 5500.0));

        string tempPath = Path.Combine(Path.GetTempPath(), $"Challenger_Fallback_{Guid.NewGuid():N}.xlsx");
        try
        {
            var failingAdapter = new FailingDetailingAdapter();
            string outPath = RevitExportExcelRunner.Execute(context, tempPath, adapter: failingAdapter);

            Assert.Equal(tempPath, outPath);
            Assert.True(File.Exists(tempPath));

            using var wb = new ClosedXML.Excel.XLWorkbook(tempPath);
            var ws = wb.Worksheet("Thong_ke");
            Assert.NotNull(ws);
            Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", ws.Cell("A1").GetString());
            Assert.Equal("Dam_D1", ws.Cell("A4").GetString());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private sealed class FailingDetailingAdapter : IKataExcelAdapter
    {
        public bool IsDesktopExcelOpen => false;
        public bool ReadBeamSheet(string? path, out KataBeamSheetData data) { data = new KataBeamSheetData(); return false; }
        public bool WriteBeamSheet(string? path, in KataBeamSheetData data) => false;
        public bool ReadColumnSheet(string? path, out KataColumnSheetData data) { data = new KataColumnSheetData(); return false; }
        public bool WriteColumnSheet(string? path, in KataColumnSheetData data) => false;
        public bool ReadWallSheet(string? path, out KataWallSheetData data) { data = new KataWallSheetData(); return false; }
        public bool WriteWallSheet(string? path, in KataWallSheetData data) => false;
        public bool ExportSchedule(string? path, IEnumerable<KataRebarScheduleItem> items) => false;
        public bool ExportSchedule(string? path, IEnumerable<RebarItem> items) => false;
        public bool ExportQhPlusSchedule(string? path, IEnumerable<KataRebarScheduleItem> items) => false;
        public bool ExportQhPlusSchedule(string? path, IEnumerable<RebarItem> items) => false;
    }
}
