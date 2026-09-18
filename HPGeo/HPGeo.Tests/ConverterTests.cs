using HPGeo.Core.Conversion;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;
using HPGeo.Core.Units;
using HPGeo.Core.Validation;
using Xunit;

namespace HPGeo.Tests;

/// <summary>Validation order and messages of the reference tool, plus the unit and plausibility rules.</summary>
public sealed class ConverterTests
{
    private static readonly Vn2000Converter Converter = new();
    private static readonly TmParameters Hcm = TmParameters.Tm3(105.75);

    private static SurveyPoint Pt(int i, double e, double n) => new(i, i.ToString(), new PlanePoint(e, n));

    private static readonly SurveyPoint[] Samples =
    {
        Pt(1, 600125.887, 1231608.428),
        Pt(2, 600124.894, 1231587.765),
        Pt(3, 600130.542, 1231543.79),
    };

    [Fact]
    public void Converts_points_in_metres()
    {
        var r = Converter.Convert(Samples, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 1.0));
        Assert.True(r.Success, string.Join(" | ", r.Issues.Select(i => i.Message)));
        Assert.Equal(3, r.Points.Count);
        Assert.InRange(r.Points[0].Wgs84.LatDeg, 11.13, 11.14);
        Assert.Empty(r.Issues);
        Assert.NotNull(r.Center);
    }

    [Fact]
    public void Millimetre_drawing_gives_the_same_result_as_metres()
    {
        var mm = Samples.Select(p => p with { DrawingXY = new PlanePoint(p.DrawingXY.Easting * 1000, p.DrawingXY.Northing * 1000) }).ToList();
        var a = Converter.Convert(Samples, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 1.0));
        var b = Converter.Convert(mm, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, DrawingUnitFactor.MetersPerUnit(DrawingUnit.Millimeters)!.Value));
        Assert.True(b.Success);
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(a.Points[i].Wgs84.LatDeg, b.Points[i].Wgs84.LatDeg, 9);
            Assert.Equal(a.Points[i].Wgs84.LonDeg, b.Points[i].Wgs84.LonDeg, 9);
        }
    }

    [Fact]
    public void Missing_central_meridian_is_the_first_error()
    {
        var r = Converter.Convert(Samples, Array.Empty<BoundaryPolyline>(), new ConversionOptions(TmParameters.Tm3(double.NaN), 1.0));
        Assert.False(r.Success);
        var e = Assert.Single(r.Issues);
        Assert.Equal("NO_CENTRAL_MERIDIAN", e.Code);
        Assert.Contains("KINH TUYẾN TRỤC", e.Message);
    }

    [Fact]
    public void Invalid_projection_parameters_are_refused()
    {
        var r = Converter.Convert(Samples, Array.Empty<BoundaryPolyline>(), new ConversionOptions(new TmParameters(105.75, 0, 500000, 0), 1.0));
        Assert.Equal("INVALID_PROJECTION", Assert.Single(r.Issues).Code);
    }

    [Fact]
    public void Unknown_unit_is_refused_not_assumed()
    {
        var r = Converter.Convert(Samples, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 0));
        Assert.Equal("UNKNOWN_UNIT", Assert.Single(r.Issues).Code);
        Assert.Null(DrawingUnitFactor.MetersPerUnit(DrawingUnit.Unknown));
        Assert.Equal(DrawingUnit.Unknown, DrawingUnitFactor.FromInsUnits(0));
        Assert.Equal(DrawingUnit.Meters, DrawingUnitFactor.FromInsUnits(6));
        Assert.Equal(DrawingUnit.Millimeters, DrawingUnitFactor.FromInsUnits(4));
    }

    [Fact]
    public void Nothing_selected_is_an_error()
    {
        var r = Converter.Convert(Array.Empty<SurveyPoint>(), Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 1.0));
        Assert.Equal("NO_INPUT", Assert.Single(r.Issues).Code);
    }

    [Fact]
    public void Result_outside_Vietnam_fails_with_the_diagnostic_message()
    {
        // Northing 100 km puts the point near the equator — a wrong-zone/wrong-order mistake.
        var r = Converter.Convert(new[] { Pt(7, 600125.887, 100000) }, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 1.0));
        Assert.False(r.Success);
        var err = Assert.Single(r.Errors);
        Assert.Equal("OUTSIDE_VIETNAM", err.Code);
        Assert.Equal(7, err.PointIndex);
        Assert.Contains("E=600125.887", err.Message);
        Assert.Contains("105°45′", err.Message);
        Assert.Contains("E/N ↔ X/Y", err.Message);
    }

    [Fact]
    public void Swapped_axes_produce_a_hint_but_do_not_block_when_inside_Vietnam()
    {
        var swapped = new PlanePoint(1231608.428, 600125.887);
        Assert.Contains("đảo", PlausibilityCheck.Diagnose(swapped));
        Assert.Contains("mm", PlausibilityCheck.Diagnose(new PlanePoint(600125887, 1231608428)));
        Assert.Null(PlausibilityCheck.Diagnose(new PlanePoint(600125.887, 1231608.428)));
    }

    [Fact]
    public void Closed_boundary_needs_three_vertices()
    {
        var ring = new BoundaryPolyline("ranh", new[] { new PlanePoint(600000, 1231000), new PlanePoint(600100, 1231000) }, Closed: true);
        var r = Converter.Convert(Array.Empty<SurveyPoint>(), new[] { ring }, new ConversionOptions(Hcm, 1.0));
        Assert.Equal("RING_TOO_SHORT", Assert.Single(r.Issues).Code);
        Assert.Empty(r.Boundaries);
    }

    [Fact]
    public void Open_boundary_with_two_vertices_converts()
    {
        var line = new BoundaryPolyline("tim đường", new[] { new PlanePoint(600000, 1231000), new PlanePoint(600100, 1231000) }, Closed: false);
        var r = Converter.Convert(Array.Empty<SurveyPoint>(), new[] { line }, new ConversionOptions(Hcm, 1.0));
        Assert.True(r.Success);
        Assert.Equal(2, Assert.Single(r.Boundaries).Wgs84.Count);
    }

    [Fact]
    public void Many_points_only_warn()
    {
        var many = Enumerable.Range(1, ConversionOptions.PointCountWarningThreshold + 1)
            .Select(i => Pt(i, 600000 + i * 0.01, 1231000)).ToList();
        var r = Converter.Convert(many, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 1.0));
        Assert.True(r.Success);
        Assert.Contains(r.Warnings, w => w.Code == "MANY_POINTS");
        Assert.Equal(many.Count, r.Points.Count);
    }

    [Fact]
    public void Inputs_are_never_mutated()
    {
        var before = Samples.Select(p => p.DrawingXY).ToArray();
        Converter.Convert(Samples, Array.Empty<BoundaryPolyline>(), new ConversionOptions(Hcm, 0.001));
        Assert.Equal(before, Samples.Select(p => p.DrawingXY).ToArray());
    }
}
