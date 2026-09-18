using HPCivil3d.McpBridge.Service;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPCivil3d.McpBridge.Tests;

/// <summary>The unit rule of the Civil bridge: the Civil drawing unit wins, INSUNITS is the fallback, disagreement is flagged once.</summary>
public sealed class Civil3dUnitTableTests
{
    private const int InsunitsMillimeters = 4;
    private const int InsunitsMeters = 6;
    private const int InsunitsInches = 1;
    private const int InsunitsFeet = 2;
    private const int InsunitsUsSurveyFeet = 21;

    [Theory]
    [InlineData("Meters", 1000)]
    [InlineData("Feet", 304.8)]
    public void The_two_Civil_drawing_units_map_to_millimetres(string drawingUnit, double mmPerUnit)
    {
        var units = Civil3dUnitTable.ForDrawingUnit(drawingUnit);

        Assert.Equal(drawingUnit, units.Label);
        Assert.Equal(mmPerUnit, units.MmPerUnit);
        Assert.Null(units.Note);
        Assert.Equal(1, units.ToDrawing(mmPerUnit));
        Assert.Equal(mmPerUnit, units.ToMm(1));
    }

    [Fact]
    public void An_unknown_drawing_unit_is_treated_as_millimetres_and_says_so()
    {
        var units = Civil3dUnitTable.ForDrawingUnit("Furlongs");

        Assert.Equal("Furlongs", units.Label);
        Assert.Equal(1, units.MmPerUnit);
        Assert.Contains("Furlongs", units.Note);
    }

    [Fact]
    public void Without_a_Civil_unit_INSUNITS_decides_and_nothing_is_flagged()
    {
        var units = Civil3dUnitTable.Resolve(null, InsunitsInches, out var mismatch);

        Assert.Equal(AutocadInsunits.For(InsunitsInches).Label, units.Label);
        Assert.Equal(25.4, units.MmPerUnit);
        Assert.False(mismatch);
    }

    [Theory]
    [InlineData("Meters", InsunitsMeters)]
    [InlineData("Feet", InsunitsFeet)]
    public void Agreeing_units_are_the_Civil_units_without_a_note(string drawingUnit, int insunits)
    {
        var units = Civil3dUnitTable.Resolve(drawingUnit, insunits, out var mismatch);

        Assert.False(mismatch);
        Assert.Equal(drawingUnit, units.Label);
        Assert.Null(units.Note);
    }

    [Fact]
    public void A_millimetre_drawing_with_Feet_settings_follows_Feet_and_is_flagged()
    {
        // the case the spike found: acad.dwt opened in Civil 3D reports DrawingUnits = Feet whatever INSUNITS says
        var units = Civil3dUnitTable.Resolve("Feet", InsunitsMillimeters, out var mismatch);

        Assert.True(mismatch);
        Assert.Equal("Feet", units.Label);
        Assert.Equal(304.8, units.MmPerUnit);
        Assert.Contains("INSUNITS says Millimeters", units.Note);
        Assert.Contains("Civil drawing unit is Feet", units.Note);
    }

    [Fact]
    public void US_survey_feet_and_Feet_count_as_the_same_unit()
    {
        var units = Civil3dUnitTable.Resolve("Feet", InsunitsUsSurveyFeet, out var mismatch);

        Assert.False(mismatch);
        Assert.Equal(304.8, units.MmPerUnit);
    }

    [Fact]
    public void Meters_settings_over_a_Feet_INSUNITS_are_flagged()
    {
        var units = Civil3dUnitTable.Resolve("Meters", InsunitsFeet, out var mismatch);

        Assert.True(mismatch);
        Assert.Equal(1000, units.MmPerUnit);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(".", null)]
    [InlineData("NH83F", "NH83F")]
    public void No_zone_is_reported_as_null_whether_Civil_says_empty_or_dot(string? code, string? expected)
    {
        Assert.Equal(expected, Civil3dUnitTable.NormalizeCoordinateSystemCode(code));
    }
}
