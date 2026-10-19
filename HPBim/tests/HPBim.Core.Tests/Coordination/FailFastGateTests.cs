namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using Xunit;

public sealed class FailFastGateTests
{
    private readonly FailFastGate _gate = new();

    [Fact]
    public void EvaluateCadSource_MissingCadHandle_ReturnsViolationFF1()
    {
        var spec = new CadSourceSpec
        {
            Handle = "", // Empty handle
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" }
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, "GB-01");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-1"));
    }

    [Fact]
    public void EvaluateCadSource_MissingTopElevation_ReturnsViolationFF3()
    {
        var spec = new CadSourceSpec
        {
            Handle = "1A2B3",
            TopElevation = null, // Missing top elevation
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" }
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, "GB-02");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-3"));
    }

    [Fact]
    public void EvaluateCadSource_FootingMissingBottomElevation_ReturnsViolationFF3()
    {
        var spec = new CadSourceSpec
        {
            Handle = "CAP_01",
            TopElevation = -1.600,
            BottomElevation = null, // Missing bottom elevation for footing
            Width = 1200,
            Height = 1200,
            Supports = new[] { "GRID-1A" }
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.Footing, "FT-01");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-3") && v.Contains("đáy đài"));
    }

    [Fact]
    public void EvaluateCadSource_BottomElevationHigherThanTop_ReturnsViolationFF3()
    {
        var spec = new CadSourceSpec
        {
            Handle = "CAP_02",
            TopElevation = -1.600,
            BottomElevation = -1.200, // Bottom higher than top!
            Width = 1200,
            Height = 1200,
            Supports = new[] { "GRID-1A" }
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.Footing, "FT-02");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-3") && v.Contains(">="));
    }

    [Fact]
    public void EvaluateCadSource_MissingOrZeroDimensions_ReturnsViolationFF4()
    {
        var specZeroWidth = new CadSourceSpec
        {
            Handle = "3F11",
            TopElevation = -1.600,
            Width = 0, // Zero width
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" }
        };

        var res1 = _gate.EvaluateCadSource(specZeroWidth, CoordinationCategory.GradeBeam, "GB-03");
        Assert.False(res1.IsPassed);
        Assert.Contains(res1.Violations, v => v.Contains("FF-4") && v.Contains("Width"));

        var specNullHeight = new CadSourceSpec
        {
            Handle = "3F12",
            TopElevation = -1.600,
            Width = 200,
            Height = null, // Missing height
            Supports = new[] { "FT-01", "FT-02" }
        };

        var res2 = _gate.EvaluateCadSource(specNullHeight, CoordinationCategory.GradeBeam, "GB-04");
        Assert.False(res2.IsPassed);
        Assert.Contains(res2.Violations, v => v.Contains("FF-4") && v.Contains("Height"));
    }

    [Fact]
    public void EvaluateCadSource_PileMissingDiameterAndRectDimensions_ReturnsViolationFF4()
    {
        var spec = new CadSourceSpec
        {
            Handle = "PILE_1",
            TopElevation = -2.500,
            Diameter = null, // No diameter
            Width = null,
            Height = null,
            Supports = new[] { "FT-DC1-01" }
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.Pile, "P-01");
        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-4") && v.Contains("tiết diện"));
    }

    [Fact]
    public void EvaluateCadSource_GradeBeamInsufficientSupports_ReturnsViolationFF5()
    {
        var specNoSupports = new CadSourceSpec
        {
            Handle = "3F20",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = Array.Empty<string>() // 0 supports
        };

        var res1 = _gate.EvaluateCadSource(specNoSupports, CoordinationCategory.GradeBeam, "GB-05");
        Assert.False(res1.IsPassed);
        Assert.Contains(res1.Violations, v => v.Contains("FF-5") && v.Contains("2 đầu"));

        var specOneSupport = new CadSourceSpec
        {
            Handle = "3F21",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-01" } // Only 1 support
        };

        var res2 = _gate.EvaluateCadSource(specOneSupport, CoordinationCategory.GradeBeam, "GB-06");
        Assert.False(res2.IsPassed);
        Assert.Contains(res2.Violations, v => v.Contains("FF-5") && v.Contains("2 đầu"));
    }

    [Fact]
    public void EvaluateCadSource_PileMissingCapSupport_ReturnsViolationFF5()
    {
        var spec = new CadSourceSpec
        {
            Handle = "PILE_2",
            TopElevation = -2.500,
            Diameter = 400,
            Supports = Array.Empty<string>() // No footing cap support
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.Pile, "P-02");
        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-5") && v.Contains("đài móng"));
    }

    [Fact]
    public void EvaluateCadSource_CrossCheckConflict_ReturnsViolationFF6()
    {
        var spec = new CadSourceSpec
        {
            Handle = "3F30",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "FT-01", "FT-02" },
            CrossCheckPassed = false // Conflicting drawing sources
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, "GB-07");
        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-6"));
    }

    [Fact]
    public void AssertValidCadSource_WhenViolationsExist_ThrowsFailFastCoordinationException()
    {
        var invalidSpec = new CadSourceSpec
        {
            Handle = "",
            TopElevation = null
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(invalidSpec, CoordinationCategory.GradeBeam, "GB-ERR"));

        Assert.Equal("GB-ERR", ex.ElementId);
        Assert.NotEmpty(ex.Violations);
        Assert.Contains("FailFastGate", ex.Message);
    }

    [Fact]
    public void AssertReadyForRevit_ValidElement_PassesWithoutException()
    {
        var validElement = new CoordinationElementRecord
        {
            Id = "GB-VALID",
            Category = CoordinationCategory.GradeBeam,
            Code = "GM-1",
            CadSourceSpec = new CadSourceSpec
            {
                Handle = "3F99",
                Layer = "DK",
                TopElevation = -1.600,
                BottomElevation = -2.000,
                Width = 200,
                Height = 400,
                Supports = new[] { "FT-01", "FT-02" },
                CrossCheckPassed = true
            }
        };

        // Must not throw
        _gate.AssertReadyForRevit(validElement);
    }
}
