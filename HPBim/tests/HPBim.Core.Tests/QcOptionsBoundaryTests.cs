namespace HPBim.Core.Tests;

using System;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using Xunit;

public sealed class QcOptionsBoundaryTests
{
    [Fact]
    public void QcOptions_DefaultConstructor_HasAllFlagsEnabled()
    {
        var opts = new QcOptions();
        Assert.True(opts.CheckClashes);
        Assert.True(opts.CheckClearances);
        Assert.True(opts.CheckTcvnCompliance);
    }

    [Fact]
    public void QcOptions_PositionalConstructor_RespectsGivenFlags()
    {
        var allFalse = new QcOptions(false, false, false);
        Assert.False(allFalse.CheckClashes);
        Assert.False(allFalse.CheckClearances);
        Assert.False(allFalse.CheckTcvnCompliance);

        var clashesOnly = new QcOptions(true, false, false);
        Assert.True(clashesOnly.CheckClashes);
        Assert.False(clashesOnly.CheckClearances);
        Assert.False(clashesOnly.CheckTcvnCompliance);

        var clearancesOnly = new QcOptions(false, true, false);
        Assert.False(clearancesOnly.CheckClashes);
        Assert.True(clearancesOnly.CheckClearances);
        Assert.False(clearancesOnly.CheckTcvnCompliance);

        var tcvnOnly = new QcOptions(false, false, true);
        Assert.False(tcvnOnly.CheckClashes);
        Assert.False(tcvnOnly.CheckClearances);
        Assert.True(tcvnOnly.CheckTcvnCompliance);
    }

    [Fact]
    public void Validate_NullModel_ThrowsArgumentNullException()
    {
        var engine = new QcEngine();
        Assert.Throws<ArgumentNullException>(() => engine.Validate(null!));
    }

    [Fact]
    public void Validate_OptionsNull_RunsAllSixChecksByDefault()
    {
        var engine = new QcEngine();
        var model = new BimModel();
        var report = engine.Validate(model, null);

        Assert.Equal(6, report.TotalChecksRun);
        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
        Assert.Empty(report.Clashes);
    }

    [Fact]
    public void Validate_DefaultQcOptions_RunsAllSixChecks()
    {
        var engine = new QcEngine();
        var model = new BimModel();
        var report = engine.Validate(model, new QcOptions());

        Assert.Equal(6, report.TotalChecksRun);
        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_AllFalseOptions_RunsZeroChecks()
    {
        var engine = new QcEngine();
        var model = new BimModel();
        var report = engine.Validate(model, new QcOptions(false, false, false));

        Assert.Equal(0, report.TotalChecksRun);
        Assert.True(report.IsValid);
    }

    [Theory]
    [InlineData(true, false, false, 2)]  // Clashes only: 2 checks
    [InlineData(false, true, false, 3)]  // Clearances only: 3 checks
    [InlineData(false, false, true, 1)]  // TCVN only: 1 check
    [InlineData(true, true, false, 5)]   // Clashes + Clearances: 5 checks
    [InlineData(true, false, true, 3)]   // Clashes + TCVN: 3 checks
    [InlineData(false, true, true, 4)]   // Clearances + TCVN: 4 checks
    [InlineData(true, true, true, 6)]    // All enabled: 6 checks
    public void Validate_PartialFlags_ExecutesExactCheckCount(
        bool clashes, bool clearances, bool tcvn, int expectedChecks)
    {
        var engine = new QcEngine();
        var model = new BimModel();
        var opts = new QcOptions(clashes, clearances, tcvn);
        var report = engine.Validate(model, opts);

        Assert.Equal(expectedChecks, report.TotalChecksRun);
    }

    [Fact]
    public void Validate_IllegalRebarCover_CaughtWhenTcvnEnabled()
    {
        var engine = new QcEngine();
        var model = new BimModel
        {
            Rebars = new[]
            {
                new RebarDef
                {
                    Id = "R1",
                    BarMark = 1,
                    CoverMm = 10.0, // Illegal: < 15.0mm TCVN minimum for slabs
                    DiameterMm = 12.0,
                    LengthMm = 2000.0,
                    Quantity = 2
                }
            }
        };

        // When TCVN compliance is ON (default)
        var reportWithTcvn = engine.Validate(model);
        Assert.False(reportWithTcvn.IsValid);
        Assert.Single(reportWithTcvn.Errors);
        Assert.Equal("TCVN-COVER-001", reportWithTcvn.Errors[0].RuleId);

        // When TCVN compliance is OFF
        var reportWithoutTcvn = engine.Validate(model, new QcOptions(CheckClashes: true, CheckClearances: true, CheckTcvnCompliance: false));
        Assert.True(reportWithoutTcvn.IsValid);
        Assert.Empty(reportWithoutTcvn.Errors);
    }

    [Theory]
    [InlineData(14.9, false)] // Just below threshold: violation
    [InlineData(15.0, true)]  // Exact threshold: compliant
    [InlineData(15.1, true)]  // Just above threshold: compliant
    [InlineData(25.0, true)]  // Well above threshold: compliant
    [InlineData(0.0, true)]   // Cover <= 0 is ignored by cover check guard
    [InlineData(-5.0, true)]  // Negative cover is ignored
    public void Validate_RebarCoverBoundaryConditions(double coverMm, bool shouldBeValid)
    {
        var engine = new QcEngine();
        var model = new BimModel
        {
            Rebars = new[]
            {
                new RebarDef
                {
                    Id = "R-Test",
                    BarMark = 99,
                    CoverMm = coverMm,
                    DiameterMm = 10.0,
                    LengthMm = 1000.0
                }
            }
        };

        var report = engine.Validate(model, new QcOptions(false, false, true));
        Assert.Equal(shouldBeValid, report.IsValid);
    }

    [Fact]
    public void Validate_NullRebarsCollection_DoesNotThrow()
    {
        var engine = new QcEngine();
        var model = new BimModel { Rebars = null! };
        var report = engine.Validate(model);

        Assert.Equal(6, report.TotalChecksRun);
        Assert.True(report.IsValid);
    }
}
