using System.Collections.Generic;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Interop;
using HPKata.Core.Models;
using HPKata.Core.Serialization;
using HPKata.E2E.Tests.Common;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;
using HPKata.Revit.Runners;
using Xunit;

namespace HPKata.E2E.Tests.Tier3_CrossFeature;

/// <summary>
/// Tier 3 Pipeline: Dual-host CAD ↔ Revit data exchange, XData serialization, parameter bridging, and schedule round-trip.
/// </summary>
public sealed class Tier3_CadToRevitInteropPipelineTests
{
    [Fact]
    public void Pipeline_CadXDataToRevitRebarDtoRoundTrip_MaintainsMetricFidelity()
    {
        // 1. Arrange: Create CAD Rebar entity with full XData payload
        var cadEntity = E2EDataFactory.CreateCadRebarEntity(
            "100A", "02", 3, 20, "4", 2, [500.0, 5000.0, 500.0], "B101");

        // 2. Extract RebarItem from CAD Entity
        var items = ScheduleCommandRunner.ExtractRebarItems([cadEntity]);
        Assert.Single(items);
        var cadItem = items[0];

        // 3. Map to RevitRebarDto
        var revitDto = RevitParameterBridge.ToRebarDto(cadItem, 9001);
        Assert.Equal("B101", revitDto.Partition);
        Assert.Equal("3", revitDto.Mark);
        Assert.Equal(20, revitDto.DiameterMm);
        Assert.Equal(6000.0, revitDto.SingleBarLengthMm);

        // 4. Map back from RevitRebarDto to RebarItem
        var roundTripItem = RevitParameterBridge.ToRebarItem(revitDto);
        Assert.Equal(cadItem.ComponentMark, roundTripItem.ComponentMark);
        Assert.Equal(cadItem.BarMark, roundTripItem.BarMark);
        Assert.Equal(cadItem.DiameterMm, roundTripItem.DiameterMm);
        Assert.Equal(cadItem.CuttingLengthMm, roundTripItem.CuttingLengthMm);
    }

    [Fact]
    public void Pipeline_RevitScheduleRunner_ImportsCadExportedDtosAccurately()
    {
        // 1. Arrange: Seed CAD items and convert to Revit DTOs
        var cadEntity1 = E2EDataFactory.CreateCadRebarEntity("H1", "01", 1, 25, "4", 1, [6000.0], "COL1");
        var cadEntity2 = E2EDataFactory.CreateCadRebarEntity("H2", "22", 2, 10, "30", 1, [500.0, 500.0, 100.0], "COL1");

        var items = ScheduleCommandRunner.ExtractRebarItems([cadEntity1, cadEntity2]);
        var revitDtos = new List<RevitRebarDto>();
        for (int i = 0; i < items.Count; i++)
        {
            revitDtos.Add(RevitParameterBridge.ToRebarDto(items[i], i + 1));
        }

        // 2. Seed into Revit context and run schedule
        var revitContext = new E2ERevitContext();
        revitContext.AllRebars.AddRange(revitDtos);
        var table = RevitScheduleRunner.Execute(revitContext);

        // 3. Assert Revit table has 2 rows corresponding to CAD entities
        Assert.NotNull(table);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("COL1", table.Rows[0][1]);
        Assert.Equal("1", table.Rows[0][2]);
        Assert.Equal("25", table.Rows[0][4]);
    }

    [Fact]
    public void Pipeline_DuplicateMarkAudit_IdentifiesConflictBetweenCadAndRevitEntries()
    {
        // Arrange: Mark 1 in CAD has D20, Mark 1 in Revit has D25
        var cadRecord = new KataXDataRecord
        {
            STT = 1, Phi = 20, Dang = "01", ChieuDaiCacDoanMm = [5000.0], TenCauKien = "B1"
        };
        var revitDto = E2EDataFactory.CreateRevitRebarDto(201, "B1", "1", "M_00", 25, 2, 5000.0);
        var revitItem = RevitParameterBridge.ToRebarItem(revitDto);
        var revitRecord = new KataXDataRecord
        {
            STT = (short)int.Parse(revitItem.BarMark),
            Phi = (short)revitItem.DiameterMm,
            Dang = "01",
            ChieuDaiCacDoanMm = [revitItem.CuttingLengthMm],
            TenCauKien = revitItem.ComponentMark
        };

        // Act
        int conflicts = RebarMarkAuditService.CheckDuplicateMarks([cadRecord, revitRecord], out var reports);

        // Assert: Conflict detected due to diameter discrepancy (Ø20 vs Ø25)
        Assert.Equal(1, conflicts);
        Assert.Contains(reports, r => r.Contains("Conflicting mark 1"));
    }

    [Fact]
    public void Pipeline_HandleLinkingAndScheduleExtraction_MaintainsLinkTags()
    {
        // Arrange
        var rebarRecord = new KataXDataRecord { Dang = "01", STT = 1, Phi = 16, ChieuDaiCacDoanMm = [4000.0] };
        var tagRecord = new KataXDataRecord { Dang = "01", STT = 1, Phi = 16, ChieuDaiCacDoanMm = [4000.0] };

        // Act: Link
        HandleLinkService.LinkRecords(rebarRecord, "REBAR_HANDLE_1", tagRecord, "TAG_HANDLE_1");
        var dxf = KataXDataSerializer.SerializeToDxf(rebarRecord);
        var restored = KataXDataSerializer.DeserializeFromDxf(dxf);

        // Assert
        Assert.Equal("TAG_HANDLE_1", restored.LinkedHandle);
        Assert.Equal("THEP_DON", restored.LinkType);
    }
}
