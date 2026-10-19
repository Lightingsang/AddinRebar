namespace HPBim.Core.Tests.Coordination;

using System;
using HPBim.Core.Coordination;
using Xunit;

/// <summary>
/// Adversarial stress tests probing FailFastGate edge cases:
/// NaN, Infinity, negative dimensions, whitespace/null supports, and inverted elevations.
/// </summary>
public sealed class FailFastGateAdversarialProbeTests
{
    private readonly FailFastGate _gate = new();

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void EvaluateCadSource_PathologicalTopElevation_MustFailGate(double topElevation)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = topElevation,
            Width = 200,
            Height = 400,
            Supports = new[] { "S1", "S2" },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, "GB-TOP-ELEV");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-3"));
    }

    [Theory]
    [InlineData(CoordinationCategory.Footing, double.NaN)]
    [InlineData(CoordinationCategory.Footing, double.PositiveInfinity)]
    [InlineData(CoordinationCategory.Footing, double.NegativeInfinity)]
    [InlineData(CoordinationCategory.GradeBeam, double.NaN)]
    [InlineData(CoordinationCategory.GradeBeam, double.PositiveInfinity)]
    [InlineData(CoordinationCategory.GradeBeam, double.NegativeInfinity)]
    public void EvaluateCadSource_PathologicalBottomElevation_MustFailGate(
        CoordinationCategory category, double bottomElevation)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = -1.600,
            BottomElevation = bottomElevation,
            Width = 300,
            Height = 500,
            Supports = new[] { "S1", "S2" },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, category, "ELEM-BOT-ELEV");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-3"));
    }

    [Theory]
    [InlineData(double.NaN, 400.0)]
    [InlineData(double.PositiveInfinity, 400.0)]
    [InlineData(double.NegativeInfinity, 400.0)]
    [InlineData(0.0, 400.0)]
    [InlineData(-200.0, 400.0)]
    [InlineData(200.0, double.NaN)]
    [InlineData(200.0, double.PositiveInfinity)]
    [InlineData(200.0, double.NegativeInfinity)]
    [InlineData(200.0, 0.0)]
    [InlineData(200.0, -400.0)]
    public void EvaluateCadSource_PathologicalDimensions_MustFailGate(double width, double height)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = -1.600,
            Width = width,
            Height = height,
            Supports = new[] { "S1", "S2" },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, "GB-DIM");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-4"));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0.0)]
    [InlineData(-400.0)]
    public void EvaluateCadSource_PileDiameter_PathologicalInputs_MustFailGate(double diameter)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = -2.500,
            Diameter = diameter,
            Supports = new[] { "FT-01" },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.Pile, "P-DIAM");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-4"));
    }

    [Theory]
    [InlineData(CoordinationCategory.Footing, -1.600, -1.600)]
    [InlineData(CoordinationCategory.Footing, -1.600, -1.200)]
    [InlineData(CoordinationCategory.GradeBeam, -1.600, -1.600)]
    [InlineData(CoordinationCategory.GradeBeam, -1.600, 0.000)]
    [InlineData(CoordinationCategory.Column, 3.800, 3.800)]
    [InlineData(CoordinationCategory.Column, 3.800, 4.000)]
    public void EvaluateCadSource_BottomGreaterThanOrEqualToTop_MustFailGate(
        CoordinationCategory category, double top, double bottom)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = top,
            BottomElevation = bottom,
            Width = 300,
            Height = 500,
            Supports = new[] { "S1", "S2" },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, category, "ELEM-INVERTED");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-3") && v.Contains(">="));
    }

    [Theory]
    [InlineData(CoordinationCategory.GradeBeam)]
    [InlineData(CoordinationCategory.Footing)]
    [InlineData(CoordinationCategory.Pile)]
    [InlineData(CoordinationCategory.Column)]
    public void EvaluateCadSource_NullSupportsCollection_MustFailGate(CoordinationCategory category)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = -1.600,
            BottomElevation = category == CoordinationCategory.Footing ? -2.500 : null,
            Width = 300,
            Height = 500,
            Diameter = 400,
            Supports = null!,
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, category, "ELEM-NULL-SUPPORTS");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-5"));
    }

    [Fact]
    public void EvaluateCadSource_GradeBeamSingleValidAndOneWhitespace_MustFailGate()
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = -1.600,
            Width = 200,
            Height = 400,
            Supports = new[] { "VALID-S1", "   " },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, CoordinationCategory.GradeBeam, "GB-1VALID-1WS");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-5"));
    }

    [Theory]
    [InlineData(CoordinationCategory.GradeBeam)]
    [InlineData(CoordinationCategory.Footing)]
    [InlineData(CoordinationCategory.Pile)]
    [InlineData(CoordinationCategory.Column)]
    public void EvaluateCadSource_SupportsWithOnlyNullAndWhitespace_MustFailGate(CoordinationCategory category)
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = -1.600,
            BottomElevation = category == CoordinationCategory.Footing ? -2.500 : null,
            Width = 300,
            Height = 500,
            Diameter = 400,
            Supports = new string[] { null!, "", "   ", "\t", "\r\n" },
            CrossCheckPassed = true
        };

        var result = _gate.EvaluateCadSource(spec, category, "ELEM-WS-SUPPORTS");

        Assert.False(result.IsPassed);
        Assert.Contains(result.Violations, v => v.Contains("FF-5"));
    }

    [Fact]
    public void AssertValidCadSource_PathologicalSpec_ThrowsFailFastCoordinationException()
    {
        var spec = new CadSourceSpec
        {
            Handle = "VALID_HANDLE",
            TopElevation = double.NaN,
            Width = double.NaN,
            Height = double.NaN,
            Supports = null!,
            CrossCheckPassed = false
        };

        var ex = Assert.Throws<FailFastCoordinationException>(() =>
            _gate.AssertValidCadSource(spec, CoordinationCategory.GradeBeam, "GB-PATHOLOGICAL"));

        Assert.Equal("GB-PATHOLOGICAL", ex.ElementId);
        Assert.True(ex.Violations.Count >= 4);
    }
}
