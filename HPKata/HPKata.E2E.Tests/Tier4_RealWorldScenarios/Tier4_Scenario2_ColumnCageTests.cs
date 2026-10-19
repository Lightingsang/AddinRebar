using System.Collections.Generic;
using HPKata.AutoCad.Commands;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.E2E.Tests.Common;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios;

/// <summary>
/// Scenario 2: High-Rise Column Cage Detailing with Mechanical Couplers.
/// Exercises vertical bars with mechanical couplers, master ties with 135° seismic hooks,
/// inner cross ties with 90°/135° hooks, coupler scheduling, and CAD summary table insertion.
/// </summary>
public sealed class Tier4_Scenario2_ColumnCageTests
{
    [Fact]
    public void Scenario2_HighRiseColumnCage_ExecutesCouplerAndTieReinforcement()
    {
        // 1. Arrange & Model 800x800 high-rise column cage (Story height = 4200mm)
        // 16 vertical longitudinal bars D32 with threaded couplers
        // Outer master stirrups: D12 @ 100mm (dense zone at base/top 1000mm) and @ 150mm (mid-height)
        // Inner cross ties: D10
        var context = new E2ECadContext();

        // Add 16 vertical longitudinal bars D32
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "COL_VERT", "01", 1, 32, "16", 1, [4200.0], "COL_C800"));
        context.Entities[0].XDataRecord.ChuThich = "Nối coupler D32 ren tiêu chuẩn";

        // Add outer master ties D12 (720x720 outer dimension, 135° seismic hooks)
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "COL_TIE_OUTER", "22", 2, 12, "34", 1, [720.0, 720.0, 120.0], "COL_C800"));

        // Add inner diamond ties D10
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "COL_TIE_INNER", "24", 3, 10, "34", 1, [500.0, 100.0], "COL_C800"));

        // 2. Execute Schedule generation in CAD
        var scheduleTable = ScheduleCommandRunner.ExecuteSchedule(context);
        Assert.NotNull(scheduleTable);
        Assert.Single(context.InsertedTables);
        Assert.Equal(3, scheduleTable.Rows.Count);

        // 3. Execute Coupler Summary generation in CAD
        var couplerTable = SummaryCommandRunner.ExecuteCouplerSummary(context);
        Assert.NotNull(couplerTable);
        Assert.Equal(2, context.InsertedTables.Count); // 1 schedule + 1 coupler table
        Assert.Contains("COUPLER", couplerTable.Title);
        Assert.Equal("COL_C800", couplerTable.Rows[0].Cells[1]);
        Assert.Equal("Ø32", couplerTable.Rows[0].Cells[2]);
        Assert.Equal("16", couplerTable.Rows[0].Cells[5]); // 16 total couplers

        // 4. Execute Diameter Summary generation in CAD
        var summaryTable = SummaryCommandRunner.ExecuteSummary(context);
        Assert.NotNull(summaryTable);
        Assert.Equal(3, context.InsertedTables.Count);
    }
}
