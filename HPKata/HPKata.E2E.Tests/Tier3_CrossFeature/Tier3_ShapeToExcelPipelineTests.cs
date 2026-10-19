using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;
using HPKata.Core.Shapes;
using HPKata.Core.Tagging;
using Xunit;

namespace HPKata.E2E.Tests.Tier3_CrossFeature;

/// <summary>
/// Tier 3 Pipeline: Shape generation -> Schedule aggregation -> Cutting-Stock solver -> ClosedXML export -> QR tag.
/// </summary>
public sealed class Tier3_ShapeToExcelPipelineTests
{
    [Fact]
    public void Pipeline_ShapesToScheduleToCuttingStockToExcel_ExecutesEndToEnd()
    {
        // 1. Arrange & Shape generation: 3-span continuous beam reinforcement
        var items = CreateContinuousBeamItems();

        // 2. Schedule aggregation
        var report = RebarScheduleAggregator.GenerateReport(items);
        Assert.Equal(3, report.Items.Count);
        Assert.True(report.Bins.TotalProjectWeightKg > 0.0);

        // 3. 1D Cutting-Stock optimization on main bars (D20 >= 10mm)
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };
        var demandsD20 = report.Items
            .Where(i => i.DiameterMm == 20)
            .Select(i => new CuttingDemand(i.DiameterMm, i.CuttingLengthMm, i.TotalCount, i.BarMark, i.ComponentMark));
        var cutResult = engine.Solve(20, demandsD20);
        Assert.True(cutResult.NewStandardBarsCount > 0);

        // 4. ClosedXML export to Excel stream
        using var stream = new MemoryStream();
        ClosedXmlExporter.ExportScheduleToStream(report, stream);
        Assert.True(stream.Length > 0);

        // 5. Verify Excel workbook content
        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet("Thong_ke");
        Assert.Equal("B201", ws.Cell(4, 1).GetString());
        Assert.Equal("20", ws.Cell(4, 11).GetString());

        // 6. QR Tag generation for fabricated bundle
        var topBar = report.Items[0];
        var qrPayload = new QrTagPayload
        {
            Project = "MetroStation",
            Component = topBar.ComponentMark,
            Mark = topBar.BarMark,
            DiameterMm = topBar.DiameterMm,
            CutLengthMm = topBar.CuttingLengthMm,
            Quantity = topBar.TotalCount,
            WeightKg = topBar.TotalWeightKg
        };
        string qrText = QrTagGenerator.FormatPayload(qrPayload);
        byte[] qrBytes = QrTagGenerator.GeneratePngBytes(qrText);
        Assert.NotNull(qrBytes);
        Assert.True(qrBytes.Length > 100);
    }

    [Fact]
    public void Pipeline_ColumnCage_WithCouplers_ExportsCouplerScheduleTable()
    {
        // 1. Generate column bars with coupler sleeves
        var items = new List<RebarItem>
        {
            new() { ComponentMark = "C1", BarMark = "1", ShapeCode = "01", DiameterMm = 28, CountPerMember = 8, MemberCount = 2, ParametersMm = [4200.0], CuttingLengthMm = 4200.0, Notes = "Nối coupler D28" },
            new() { ComponentMark = "C1", BarMark = "2", ShapeCode = "22", DiameterMm = 10, CountPerMember = 40, MemberCount = 2, ParametersMm = [600.0, 600.0, 100.0], CuttingLengthMm = 2600.0 }
        };

        // 2. Schedule report
        var report = RebarScheduleAggregator.GenerateReport(items);
        Assert.Single(report.Couplers);
        Assert.Equal(16, report.Couplers[0].TotalCouplers); // 8 per col * 2 cols

        // 3. Export to Excel
        using var stream = new MemoryStream();
        ClosedXmlExporter.ExportScheduleToStream(report, stream);

        // 4. Inspect Coupler section in Excel
        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheet("Thong_ke");
        Assert.Equal("C1", ws.Cell(12, 21).GetString());
        Assert.Equal("28", ws.Cell(12, 22).GetString());
        Assert.Equal("16", ws.Cell(12, 25).GetString());
    }

    [Fact]
    public void Pipeline_BendDeductionEnabled_PropagatesToCuttingLengthAndScheduleMass()
    {
        // Arrange
        var paramsUBar = new double[] { 500.0, 3000.0 }; // 2*500 + 3000 = 4000 nominal
        double lenWithoutDed = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang02, paramsUBar, 0, 20, false);
        double lenWithDed = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang02, paramsUBar, 0, 20, true);

        var itemNoDed = new RebarItem { DiameterMm = 20, CountPerMember = 10, MemberCount = 1, CuttingLengthMm = lenWithoutDed };
        var itemWithDed = new RebarItem { DiameterMm = 20, CountPerMember = 10, MemberCount = 1, CuttingLengthMm = lenWithDed };

        // Act
        var reportNoDed = RebarScheduleAggregator.GenerateReport([itemNoDed]);
        var reportWithDed = RebarScheduleAggregator.GenerateReport([itemWithDed]);

        // Assert: mass with bend deduction is strictly less than without
        Assert.True(reportWithDed.Bins.TotalProjectWeightKg < reportNoDed.Bins.TotalProjectWeightKg);
    }

    [Fact]
    public void Pipeline_CuttingStockRemnants_GeneratesWarehouseOffcutTags()
    {
        // Arrange: 1 cut of 7,000mm from 11,700mm => 4,700mm remnant
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, MinRemnantLengthMm = 1000.0 };
        var demands = new[] { new CuttingDemand(22, 7000.0, 1, "1", "W1") };

        // Act
        var result = engine.Solve(22, demands);

        // Assert
        Assert.NotEmpty(result.NewWarehouseOffcuts);
        var offcut = result.NewWarehouseOffcuts[0];
        Assert.Equal(4700.0, offcut.LengthMm);

        // Generate QR tag for warehouse inventory
        var tag = new QrTagPayload
        {
            Project = "WarehouseInventory",
            Component = "OffcutStock",
            Mark = $"OFFCUT_D{offcut.DiameterMm}",
            DiameterMm = offcut.DiameterMm,
            CutLengthMm = offcut.LengthMm,
            Quantity = offcut.Quantity
        };
        string text = QrTagGenerator.FormatPayload(tag);
        byte[] qrBytes = QrTagGenerator.GeneratePngBytes(text);
        Assert.NotNull(qrBytes);
    }

    private static List<RebarItem> CreateContinuousBeamItems() =>
    [
        new()
        {
            ComponentMark = "B201",
            BarMark = "1",
            ShapeCode = "02",
            DiameterMm = 20,
            CountPerMember = 3,
            MemberCount = 1,
            ParametersMm = [400.0, 5800.0],
            CuttingLengthMm = 6600.0
        },
        new()
        {
            ComponentMark = "B201",
            BarMark = "2",
            ShapeCode = "01",
            DiameterMm = 20,
            CountPerMember = 3,
            MemberCount = 1,
            ParametersMm = [5800.0],
            CuttingLengthMm = 5800.0
        },
        new()
        {
            ComponentMark = "B201",
            BarMark = "3",
            ShapeCode = "22",
            DiameterMm = 8,
            CountPerMember = 50,
            MemberCount = 1,
            ParametersMm = [500.0, 250.0, 80.0],
            CuttingLengthMm = 1660.0
        }
    ];
}
