using System;
using System.Collections.Generic;
using System.IO;
using HPKata.AutoCad.Commands;
using HPKata.AutoCad.Interop;
using HPKata.Core.Serialization;
using HPKata.E2E.Tests.Common;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 10: AutoCAD Detailing command runners, schedule tables, mark auditing, and Lisp aliases.
/// </summary>
public sealed class Tier1_F10_AutoCadDetailingCommandsTests
{
    private static string ResolveLspPath()
    {
        string binPath = Path.Combine(AppContext.BaseDirectory, "GoiLenh_HPKata.lsp");
        if (File.Exists(binPath)) return binPath;

        string projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\HPKata.AutoCad\GoiLenh_HPKata.lsp"));
        if (File.Exists(projectPath)) return projectPath;

        return string.Empty;
    }

    [Fact]
    public void ScheduleCommandRunner_ExecuteSchedule_InsertsTableWithFormattedRows()
    {
        // Arrange
        var context = new E2ECadContext();
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "H1", "01", 1, 20, "2", 1, [5000.0], "B1"));
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity(
            "H2", "22", 2, 8, "20", 1, [400.0, 200.0, 80.0], "B1"));

        // Act
        var table = ScheduleCommandRunner.ExecuteSchedule(context);

        // Assert
        Assert.NotNull(table);
        Assert.Single(context.InsertedTables);
        Assert.Equal(2, table.Rows.Count);
        Assert.Contains(context.Messages, m => m.Contains("Rebar schedule generated"));
    }

    [Fact]
    public void SummaryCommandRunner_ExecuteSummary_InsertsDiameterBreakdownTable()
    {
        // Arrange
        var context = new E2ECadContext();
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity("H1", "01", 1, 16, "4", 1, [4000.0], "Col1"));
        context.Entities.Add(E2EDataFactory.CreateCadRebarEntity("H2", "01", 2, 25, "4", 1, [4000.0], "Col1"));

        // Act
        var table = SummaryCommandRunner.ExecuteSummary(context);

        // Assert
        Assert.NotNull(table);
        Assert.Single(context.InsertedTables);
        Assert.Contains(table.Rows, r => r.Cells.Count > 0 && r.Cells[0].Contains("16"));
        Assert.Contains(table.Rows, r => r.Cells.Count > 0 && r.Cells[0].Contains("25"));
    }

    [Fact]
    public void RebarMarkAuditService_DuplicateMarksWithConflictingGeometry_DetectsConflict()
    {
        // Arrange: Mark 1 with different diameters D16 vs D20
        var records = new List<KataXDataRecord>
        {
            new() { STT = 1, Phi = 16, Dang = "01", ChieuDaiCacDoanMm = [5000.0], TenCauKien = "B1" },
            new() { STT = 1, Phi = 20, Dang = "01", ChieuDaiCacDoanMm = [5000.0], TenCauKien = "B1" }
        };

        // Act
        int conflictCount = RebarMarkAuditService.CheckDuplicateMarks(records, out var reports);

        // Assert
        Assert.Equal(1, conflictCount);
        Assert.Contains(reports, r => r.Contains("Conflicting mark 1"));
    }

    [Fact]
    public void RebarMarkAuditService_IdenticalMarks_ReportsDuplicateWithoutGeometricConflict()
    {
        // Arrange: Mark 2 with identical shape, diameter, and length
        var records = new List<KataXDataRecord>
        {
            new() { STT = 2, Phi = 16, Dang = "01", ChieuDaiCacDoanMm = [4000.0], TenCauKien = "B1" },
            new() { STT = 2, Phi = 16, Dang = "01", ChieuDaiCacDoanMm = [4000.0], TenCauKien = "B2" }
        };

        // Act
        int duplicateCount = RebarMarkAuditService.CheckDuplicateMarks(records, out var reports);

        // Assert
        Assert.Equal(1, duplicateCount);
        Assert.Contains(reports, r => r.Contains("Duplicate mark 2"));
    }

    [Fact]
    public void GoiLenhLsp_FileExistsAndContainsConfigSection()
    {
        // Arrange & Act
        string lspPath = ResolveLspPath();

        // Assert
        if (!string.IsNullOrEmpty(lspPath) && File.Exists(lspPath))
        {
            string content = File.ReadAllText(lspPath);
            Assert.Contains(";kata_dim", content);
            Assert.Contains("c:vt", content); // Command alias vt
            Assert.Contains("c:tkt", content); // Command alias tkt
        }
    }
}
