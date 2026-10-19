using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.AutoCad.Commands;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;
using HPKata.E2E.Tests.Common;
using HPKata.Revit.Runners;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for empty rebar collections, 0-mass schedules, and zero-demand solvers.
/// </summary>
public sealed class Tier2_EmptyAndZeroMassSchedulesTests
{
    [Fact]
    public void GenerateReport_WithEmptyList_ReturnsEmptyReportWithZeroTotals()
    {
        // Act
        var report = RebarScheduleAggregator.GenerateReport(new List<RebarItem>());

        // Assert
        Assert.Empty(report.Items);
        Assert.Empty(report.DiameterSummaries);
        Assert.Empty(report.Couplers);
        Assert.Equal(0.0, report.Bins.TotalProjectWeightKg);
    }

    [Fact]
    public void CuttingStockEngine_WithEmptyDemands_ReturnsEmptyResult()
    {
        // Arrange
        var engine = new CuttingStockEngine();

        // Act
        var result = engine.Solve(20, new List<CuttingDemand>());

        // Assert
        Assert.Empty(result.Patterns);
        Assert.Equal(0, result.NewStandardBarsCount);
        Assert.Equal(0.0, result.GrossWastePercent);
    }

    [Fact]
    public void AutoCadSchedule_WithNoRebars_HandlesEmptySelectionGracefully()
    {
        // Arrange: empty document context
        var context = new E2ECadContext();

        // Act
        var table = ScheduleCommandRunner.ExecuteSchedule(context);

        // Assert
        Assert.NotNull(table);
        Assert.Empty(table.Rows);
        Assert.Contains(context.Messages, m => m.Contains("0 items, 0.0 kg"));
    }

    [Fact]
    public void RevitSchedule_WithNoRebars_NotifiesUserAndCreatesEmptyTable()
    {
        // Arrange: empty Revit context
        var context = new E2ERevitContext();

        // Act
        var table = RevitScheduleRunner.Execute(context);

        // Assert
        Assert.NotNull(table);
        Assert.Empty(table.Rows);
        Assert.Contains(context.Notifications, n => n.Contains("Không tìm thấy cốt thép"));
    }

    [Fact]
    public void ClosedXmlExport_WithEmptyReport_GeneratesValidEmptySheet()
    {
        // Arrange
        var report = new RebarScheduleReport();
        using var stream = new MemoryStream();

        // Act
        ClosedXmlExporter.ExportScheduleToStream(report, stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var wb = new XLWorkbook(stream);
        Assert.True(wb.Worksheets.Contains("Thong_ke"));
        var ws = wb.Worksheet("Thong_ke");
        Assert.Equal("BẢNG THỐNG KÊ CỐT THÉP", ws.Cell("A1").GetString());
    }

    [Fact]
    public void Schedule_WithZeroCountItems_CalculatesZeroTotalWeight()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new() { DiameterMm = 20, CountPerMember = 0, MemberCount = 1, CuttingLengthMm = 5000.0 }
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert
        Assert.Equal(0, report.Items[0].TotalCount);
        Assert.Equal(0.0, report.Bins.TotalProjectWeightKg);
    }
}
