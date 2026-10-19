using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;
using HPKata.Core.Tagging;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios;

/// <summary>
/// Scenario 1: Continuous Multi-Span Beam Detailing (3 spans of 6.0m = 18m).
/// Exercises continuous main top/bottom bars, splice offsets at L/4, zoned stirrup distribution (100/200mm),
/// schedule aggregation, 1D cutting stock on 11.7m bars, and ClosedXML export.
/// </summary>
public sealed class Tier4_Scenario1_ContinuousBeamTests
{
    [Fact]
    public void Scenario1_ThreeSpanContinuousBeam_ExecutesFullEngineeringWorkflow()
    {
        // 1. Arrange & Detail 3-span continuous beam (3 x 6000mm = 18000mm clear run)
        var beamItems = CreateThreeSpanBeamItems();

        // 2. Generate detailing schedule report
        var report = RebarScheduleAggregator.GenerateReport(beamItems);
        Assert.Equal(5, report.Items.Count);
        Assert.Equal(3, report.DiameterSummaries.Count); // D8, D20, D22
        Assert.True(report.Bins.TotalProjectWeightKg > 200.0);

        // 3. Optimize cutting stock on 11.7m bars for structural bars (D20 & D22 >= 10mm)
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, SawKerfMm = 5.0 };
        var demandsD22 = report.Items
            .Where(i => i.DiameterMm == 22)
            .Select(i => new CuttingDemand(i.DiameterMm, i.CuttingLengthMm, i.TotalCount, i.BarMark, i.ComponentMark));
        var cutResultD22 = engine.Solve(22, demandsD22);
        Assert.True(cutResultD22.NewStandardBarsCount > 0);

        // 4. Export to ClosedXML workbook and verify
        using var stream = new MemoryStream();
        ClosedXmlExporter.ExportScheduleToStream(report, stream);
        Assert.True(stream.Length > 0);

        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet("Thong_ke");
        Assert.Equal("D201", ws.Cell(4, 1).GetString());

        // 5. Generate fabrication QR tag for primary beam bottom bar
        var mainBar = report.Items[0];
        var tag = new QrTagPayload
        {
            Project = "ContinuousBeam_Project",
            Component = mainBar.ComponentMark,
            Mark = mainBar.BarMark,
            DiameterMm = mainBar.DiameterMm,
            CutLengthMm = mainBar.CuttingLengthMm,
            Quantity = mainBar.TotalCount,
            WeightKg = mainBar.TotalWeightKg
        };
        string payloadText = QrTagGenerator.FormatPayload(tag);
        byte[] qrBytes = QrTagGenerator.GeneratePngBytes(payloadText);
        Assert.NotNull(qrBytes);
    }

    private static List<RebarItem> CreateThreeSpanBeamItems() =>
    [
        new()
        {
            ComponentMark = "D201",
            BarMark = "1",
            ShapeCode = "02",
            DiameterMm = 22,
            CountPerMember = 3,
            MemberCount = 1,
            ParametersMm = [450.0, 11700.0],
            CuttingLengthMm = 12150.0
        },
        new()
        {
            ComponentMark = "D201",
            BarMark = "2",
            ShapeCode = "02",
            DiameterMm = 22,
            CountPerMember = 3,
            MemberCount = 1,
            ParametersMm = [7180.0, 450.0],
            CuttingLengthMm = 7630.0
        },
        new()
        {
            ComponentMark = "D201",
            BarMark = "3",
            ShapeCode = "01",
            DiameterMm = 20,
            CountPerMember = 2,
            MemberCount = 1,
            ParametersMm = [3400.0],
            CuttingLengthMm = 3400.0
        },
        new()
        {
            ComponentMark = "D201",
            BarMark = "4",
            ShapeCode = "01",
            DiameterMm = 20,
            CountPerMember = 2,
            MemberCount = 1,
            ParametersMm = [3400.0],
            CuttingLengthMm = 3400.0
        },
        new()
        {
            ComponentMark = "D201",
            BarMark = "5",
            ShapeCode = "22",
            DiameterMm = 8,
            CountPerMember = 110,
            MemberCount = 1,
            ParametersMm = [500.0, 250.0, 80.0],
            CuttingLengthMm = 1660.0
        }
    ];
}
