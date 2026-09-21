using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using HPAutoCad.Tests.HPGeoLink.Fixtures;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// The port must reproduce the reference tool: forward (VN-2000 → WGS84) against the oracle's own output,
/// reverse against proj4, and round trips that close.
/// </summary>
public sealed class ProjectionGoldenTests
{
    private const double ForwardToleranceDeg = 1e-9;   // same series, same constants: agreement to the last digits
    private const double ReverseToleranceM = 0.01;     // proj4 uses a different TM series; cm-level agreement expected
    private const double RoundTripToleranceM = 1e-6;   // the reverse is the exact inverse of the forward chain

    private static readonly Vn2000Wgs84Transform Transform = Vn2000Wgs84Transform.Default;

    [Fact]
    public void Golden_fixture_is_the_oracle_output()
    {
        var g = GoldenFixtures.Forward;
        Assert.Contains("ToolVN2000ConvertGoogleEarth", g.Source);
        Assert.True(g.SampleCount >= 10, "the oracle's sample block should carry at least 10 points");
        Assert.True(g.Cases.Count >= g.SampleCount + 17 * 12, "sample points plus a 3×4 grid per central meridian");
        Assert.Equal(TmParameters.Tm3ScaleFactor, g.Tm.K0);
        Assert.Equal(TmParameters.DefaultFalseEasting, g.Tm.FalseEasting);
    }

    [Fact]
    public void Helmert_parameters_match_the_oracle()
    {
        var h = GoldenFixtures.Forward.HelmertParameters;
        var p = Helmert7Parameters.Vn2000ToWgs84;
        Assert.Equal(h.Dx, p.DxM);
        Assert.Equal(h.Dy, p.DyM);
        Assert.Equal(h.Dz, p.DzM);
        Assert.Equal(h.Rx, p.RxArcSec);
        Assert.Equal(h.Ry, p.RyArcSec);
        Assert.Equal(h.Rz, p.RzArcSec);
        Assert.Equal(h.S, p.ScalePpm);
    }

    [Fact]
    public void Forward_matches_every_golden_case()
    {
        var worst = 0.0;
        foreach (var c in GoldenFixtures.Forward.Cases)
        {
            var wgs = Transform.ToWgs84(new PlanePoint(c.E, c.N), TmParameters.Tm3(c.Cm));
            var dLat = Math.Abs(wgs.LatDeg - c.Lat);
            var dLon = Math.Abs(wgs.LonDeg - c.Lon);
            worst = Math.Max(worst, Math.Max(dLat, dLon));
            Assert.True(dLat <= ForwardToleranceDeg && dLon <= ForwardToleranceDeg,
                $"{c.Id}: Δlat {dLat:E2}°, Δlon {dLon:E2}° exceeds {ForwardToleranceDeg:E0}°");
        }
        Assert.True(worst <= ForwardToleranceDeg, $"worst deviation {worst:E2}°");
    }

    [Fact]
    public void Helmert_step_alone_matches_the_oracle()
    {
        foreach (var c in GoldenFixtures.Forward.HelmertOnly)
        {
            var (lat, lon, h) = Transform.Vn2000GeodeticToWgs84(Deg(c.Vn2000Lat), Deg(c.Vn2000Lon));
            Assert.InRange(lat * 180 / Math.PI, c.Wgs84Lat - 1e-10, c.Wgs84Lat + 1e-10);
            Assert.InRange(lon * 180 / Math.PI, c.Wgs84Lon - 1e-10, c.Wgs84Lon + 1e-10);
            Assert.InRange(h, c.Wgs84Height - 1e-6, c.Wgs84Height + 1e-6);
        }
    }

    [Fact]
    public void Reverse_matches_proj4_within_a_centimetre()
    {
        var g = GoldenFixtures.Reverse;
        Assert.True(g.Proj4ForwardMaxDeviationFromOracleM < 0.001, "the reverse golden was generated with the wrong rotation sign");
        var worst = 0.0;
        foreach (var c in g.Cases)
        {
            var grid = Transform.ToVn2000(new GeoPoint(c.Lat, c.Lon), TmParameters.Tm3(c.Cm));
            var d = Math.Sqrt((grid.Easting - c.E) * (grid.Easting - c.E) + (grid.Northing - c.N) * (grid.Northing - c.N));
            worst = Math.Max(worst, d);
            Assert.True(d <= ReverseToleranceM, $"{c.Id}: {d:F4} m from proj4");
        }
        Assert.True(worst <= ReverseToleranceM, $"worst {worst:F4} m");
    }

    [Fact]
    public void Round_trip_closes_to_a_micrometre()
    {
        foreach (var c in GoldenFixtures.Forward.Cases)
        {
            var tm = TmParameters.Tm3(c.Cm);
            var back = Transform.ToVn2000(Transform.ToWgs84(new PlanePoint(c.E, c.N), tm), tm);
            var d = Math.Sqrt((back.Easting - c.E) * (back.Easting - c.E) + (back.Northing - c.N) * (back.Northing - c.N));
            Assert.True(d <= RoundTripToleranceM, $"{c.Id}: round trip off by {d:E2} m");
        }
    }

    [Fact]
    public void Helmert_inverse_is_exact()
    {
        var p = Helmert7Parameters.Vn2000ToWgs84;
        var v = (X: -1855000.0, Y: 5900000.0, Z: 1200000.0);
        var back = Helmert7.Inverse(Helmert7.Forward(v, p), p);
        Assert.InRange(back.X, v.X - 1e-7, v.X + 1e-7);
        Assert.InRange(back.Y, v.Y - 1e-7, v.Y + 1e-7);
        Assert.InRange(back.Z, v.Z - 1e-7, v.Z + 1e-7);
    }

    [Fact]
    public void Sample_point_lands_north_of_Ho_Chi_Minh_City()
    {
        // First sample of the oracle's textarea: E 600125.887, N 1231608.428 on 105°45'.
        var wgs = Transform.ToWgs84(new PlanePoint(600125.887, 1231608.428), TmParameters.Tm3(105.75));
        Assert.InRange(wgs.LatDeg, 11.13, 11.14);
        Assert.InRange(wgs.LonDeg, 106.66, 106.67);
    }

    private static double Deg(double d) => d * Math.PI / 180.0;
}
