using HPGeo.Core.Imagery;
using HPGeo.Core.Settings;
using HPGeo.Core.Units;
using Xunit;

namespace HPGeo.Tests;

/// <summary>The -HPGEOIMAGE argument line and the zone/unit precedence chain (argument → drawing record → user settings → defaults, value by value).</summary>
public sealed class ImageArgumentsTests
{
    [Fact]
    public void Defaults_leave_everything_to_the_resolver_except_margin_and_provider()
    {
        var a = ImageArguments.Parse("");
        Assert.Null(a.CentralMeridianDeg);
        Assert.Null(a.ScaleFactor);
        Assert.Null(a.FalseEasting);
        Assert.Null(a.FalseNorthing);
        Assert.Null(a.Handle);
        Assert.Null(a.LayerFilter);
        Assert.Null(a.ResolutionMPerPx);
        Assert.Null(a.Zoom);
        Assert.Null(a.MarginM);
        Assert.Null(a.AreaRatio); // the command then covers DefaultAreaRatio × the boundary's box
        Assert.Null(a.UnitOverride);
        Assert.Null(a.OutputPath);
        Assert.Equal(EsriWorldImageryProvider.ProviderId, a.ProviderId);
    }

    [Fact]
    public void Every_key_is_parsed_and_the_png_extension_is_added()
    {
        var a = ImageArguments.Parse("cm=105-45 layer=RANH* res=0,5 margin=12.5 unit=mm out=\"C:\\out dir\\site\" provider=ESRI k0=0.9996 fe=500000 fn=0");
        Assert.Equal(105.75, a.CentralMeridianDeg);
        Assert.Equal("RANH*", a.LayerFilter);
        Assert.Equal(0.5, a.ResolutionMPerPx);
        Assert.Equal(12.5, a.MarginM);
        Assert.Equal(DrawingUnit.Millimeters, a.UnitOverride);
        Assert.Equal("C:\\out dir\\site.png", a.OutputPath);
        Assert.Equal("esri", a.ProviderId);
        Assert.Equal(0.9996, a.ScaleFactor);
        Assert.Equal(19, ImageArguments.Parse("handle=2A zoom=19").Zoom);
        Assert.Equal(4.0, ImageArguments.Parse("area=4").AreaRatio);
        Assert.Equal("2A", ImageArguments.Parse("handle=2A zoom=19").Handle);
    }

    [Theory]
    [InlineData("foo=1", "'foo'")]
    [InlineData("handle=2A layer=0", "handle=")]
    [InlineData("res=0.3 zoom=18", "res=")]
    [InlineData("res=0", "res=")]
    [InlineData("res=abc", "res=")]
    [InlineData("zoom=20", "zoom=")]          // above the provider's maximum, never silently clamped
    [InlineData("margin=-1", "margin=")]
    [InlineData("area=0.5", "area=")]
    [InlineData("margin=30 area=10", "margin=")]
    [InlineData("margin=1e400", "margin=")]   // ∞ would reach the projection as NaN
    [InlineData("k0=0", "k0=")]
    [InlineData("cm=abc", "cm=")]
    [InlineData("provider=google", "google")]
    [InlineData("unit=furlong", "furlong")]
    public void Bad_lines_are_refused_with_the_offending_key_named(string line, string expectedInMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => ImageArguments.Parse(line));
        Assert.Contains(expectedInMessage, exception.Message);
    }

    private static readonly GeoSettings Record = new() { CentralMeridianDeg = 107.75, ScaleFactor = 0.9996, FalseEasting = 500000, FalseNorthing = 0, Unit = DrawingUnit.Meters };
    private static readonly GeoSettings User = new() { CentralMeridianDeg = 105.75, ScaleFactor = 0.9999 };

    [Fact]
    public void Zone_comes_from_the_argument_then_the_record_then_the_user_then_the_defaults_value_by_value()
    {
        // Nothing given: the record's whole zone, k0 included — the value that moves a raster by hundreds of metres.
        var fromRecord = ImageZoneResolver.Resolve(ImageArguments.Parse(""), Record, User, DrawingUnit.Millimeters);
        Assert.Equal((107.75, 0.9996), (fromRecord.Tm.CentralMeridianDeg, fromRecord.Tm.ScaleFactor));
        Assert.Equal(DrawingUnit.Meters, fromRecord.Unit); // the record's unit beats INSUNITS

        // cm= alone: the argument's cm, the record's k0 (still that record's zone).
        var cmOnly = ImageZoneResolver.Resolve(ImageArguments.Parse("cm=105.75"), Record, User, DrawingUnit.Millimeters);
        Assert.Equal((105.75, 0.9996), (cmOnly.Tm.CentralMeridianDeg, cmOnly.Tm.ScaleFactor));

        // No record: the user's zone; no record unit: INSUNITS.
        var fromUser = ImageZoneResolver.Resolve(ImageArguments.Parse(""), null, User, DrawingUnit.Millimeters);
        Assert.Equal((105.75, 0.9999, 500000.0), (fromUser.Tm.CentralMeridianDeg, fromUser.Tm.ScaleFactor, fromUser.Tm.FalseEasting));
        Assert.Equal(DrawingUnit.Millimeters, fromUser.Unit);

        // Explicit arguments beat everything; unit= beats the record.
        var explicitAll = ImageZoneResolver.Resolve(ImageArguments.Parse("cm=104.5 k0=1 fe=0 fn=10 unit=mm"), Record, User, DrawingUnit.Meters);
        Assert.Equal((104.5, 1.0, 0.0, 10.0), (explicitAll.Tm.CentralMeridianDeg, explicitAll.Tm.ScaleFactor, explicitAll.Tm.FalseEasting, explicitAll.Tm.FalseNorthing));
        Assert.Equal(DrawingUnit.Millimeters, explicitAll.Unit);
    }

    [Fact]
    public void A_record_without_a_zone_lends_neither_its_k0_nor_a_missing_meridian()
    {
        var noZone = new GeoSettings { ScaleFactor = 0.5, Unit = DrawingUnit.Meters };
        var resolved = ImageZoneResolver.Resolve(ImageArguments.Parse("cm=105.75"), noZone, null, DrawingUnit.Meters);
        Assert.Equal(0.9999, resolved.Tm.ScaleFactor);

        var refused = Assert.Throws<ImageZoneException>(() => ImageZoneResolver.Resolve(ImageArguments.Parse(""), noZone, null, DrawingUnit.Meters));
        Assert.Equal(ImageZoneResolver.NoCentralMeridianCode, refused.Code);
    }

    [Fact]
    public void An_unknown_unit_is_refused_unless_an_argument_or_the_record_names_one()
    {
        var refused = Assert.Throws<ImageZoneException>(() => ImageZoneResolver.Resolve(ImageArguments.Parse("cm=105.75"), null, null, DrawingUnit.Unknown));
        Assert.Equal(ImageZoneResolver.UnknownUnitCode, refused.Code);
        Assert.Equal(DrawingUnit.Meters, ImageZoneResolver.Resolve(ImageArguments.Parse("cm=105.75"), new GeoSettings { Unit = DrawingUnit.Meters }, null, DrawingUnit.Unknown).Unit);
        Assert.Equal(DrawingUnit.Feet, ImageZoneResolver.Resolve(ImageArguments.Parse("cm=105.75 unit=ft"), null, null, DrawingUnit.Unknown).Unit);
    }
}
