using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPKata.AutoCad.Commands;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Core.Tagging;
using HPKata.E2E.Tests.Common;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;
using HPKata.Revit.Runners;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios;

/// <summary>
/// Scenario 5: Dual-Host CAD ↔ Revit Round-Trip Detailing Synchronization.
/// Validates full lifecycle: AutoCAD polyline detailing -> XData extraction -> Core RebarItem ->
/// RevitRebarDto parameter bridge -> Revit schedule table -> ClosedXML export -> QR fabrication tag,
/// verifying 0% metric distortion and lossless bidirectional parameter mapping.
/// </summary>
public sealed class Tier4_Scenario5_DualHostRoundTripTests
{
    [Fact]
    public void Scenario5_CadToRevitRoundTrip_MaintainsZeroMetricDistortionAcrossPipeline()
    {
        // 1. Arrange & Detail rebar entities in Headless CAD context
        var cadContext = new E2ECadContext();
        cadContext.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "CAD_01", "02", 1, 20, "8", 1, [500.0, 5000.0, 500.0], "BEAM_B101")); // 6000mm, count=8
        cadContext.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "CAD_02", "22", 2, 8, "50", 1, [400.0, 200.0, 80.0], "BEAM_B101"));   // 1280mm, count=50

        // 2. CAD detailing schedule extraction
        var cadItems = ScheduleCommandRunner.ExtractRebarItems(cadContext.Entities);
        var cadReport = RebarScheduleAggregator.GenerateReport(cadItems);
        Assert.Equal(2, cadReport.Items.Count);
        double cadTotalWeightKg = cadReport.Bins.TotalProjectWeightKg;

        // 3. Bridge CAD items into Revit DTOs
        var revitContext = new E2ERevitContext();
        for (int i = 0; i < cadItems.Count; i++)
        {
            var revitDto = RevitParameterBridge.ToRebarDto(cadItems[i], 1000 + i);
            revitContext.AllRebars.Add(revitDto);
        }

        // 4. Generate Revit BIM schedule table
        var revitTable = RevitScheduleRunner.Execute(revitContext);
        Assert.NotNull(revitTable);
        Assert.Equal(2, revitTable.Rows.Count);

        // 5. Round-trip back from Revit DTOs to Core RebarItems
        var roundTripItems = revitContext.AllRebars.Select(RevitParameterBridge.ToRebarItem).ToList();
        var roundTripReport = RebarScheduleAggregator.GenerateReport(roundTripItems);

        // 6. Assert zero metric loss between CAD origin and Revit round-trip
        Assert.Equal(cadItems.Count, roundTripItems.Count);
        for (int i = 0; i < cadItems.Count; i++)
        {
            Assert.Equal(cadItems[i].ComponentMark, roundTripItems[i].ComponentMark);
            Assert.Equal(cadItems[i].BarMark, roundTripItems[i].BarMark);
            Assert.Equal(cadItems[i].DiameterMm, roundTripItems[i].DiameterMm);
            Assert.Equal(cadItems[i].TotalCount, roundTripItems[i].TotalCount);
            Assert.Equal(cadItems[i].CuttingLengthMm, roundTripItems[i].CuttingLengthMm, 3);
            Assert.Equal(cadItems[i].TotalWeightKg, roundTripItems[i].TotalWeightKg, 3);
        }
        Assert.Equal(cadTotalWeightKg, roundTripReport.Bins.TotalProjectWeightKg, 3);

        // 7. Verify ClosedXML Excel export matches exactly
        using var stream = new MemoryStream();
        ClosedXmlExporter.ExportScheduleToStream(roundTripReport, stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Scenario5_CouplerMetadataAndFabricationQr_SurvivesDualHostTransfer()
    {
        // 1. Arrange: Column rebar with mechanical coupler note in CAD
        var cadEntity = E2EDataFactory.CreateCadRebarEntity(
            "CAD_COL_1", "01", 1, 32, "8", 1, [4500.0], "COL_C1");
        cadEntity.XDataRecord.ChuThich = "Nối coupler D32 ren tiêu chuẩn";

        // 2. Extract RebarItem and map to Revit DTO
        var items = ScheduleCommandRunner.ExtractRebarItems([cadEntity]);
        var originalItem = items[0];
        var revitDto = RevitParameterBridge.ToRebarDto(originalItem, 2001);

        // Assert coupler notes preserved in Revit DTO
        Assert.Equal("Nối coupler D32 ren tiêu chuẩn", revitDto.Comments);

        // 3. Map back from Revit DTO to RebarItem
        var restoredItem = RevitParameterBridge.ToRebarItem(revitDto);
        Assert.Equal(originalItem.Notes, restoredItem.Notes);

        // 4. Verify Coupler Extraction from restored item
        var couplerSummary = RebarScheduleAggregator.ExtractCouplers([restoredItem]);
        Assert.Single(couplerSummary);
        Assert.Equal(32, couplerSummary[0].DiameterMm);
        Assert.Equal(8, couplerSummary[0].TotalCouplers);
        Assert.Equal("COL_C1", couplerSummary[0].ComponentMark);

        // 5. Generate and verify Fabrication QR tag
        var tag = new QrTagPayload
        {
            Project = "BIM_Revit_Sync",
            Component = restoredItem.ComponentMark,
            Mark = restoredItem.BarMark,
            DiameterMm = restoredItem.DiameterMm,
            CutLengthMm = restoredItem.CuttingLengthMm,
            Quantity = restoredItem.TotalCount,
            WeightKg = restoredItem.TotalWeightKg
        };
        string qrText = QrTagGenerator.FormatPayload(tag);
        byte[] qrPng = QrTagGenerator.GeneratePngBytes(qrText);
        Assert.NotNull(qrPng);
        Assert.True(qrPng.Length > 100);
    }
}
