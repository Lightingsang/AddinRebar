using HPNavis.BIMCoordinator.Reports;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

/// <summary>The facts a clash description quotes: an element's system and the grid system its location is read against.</summary>
public sealed class ClashElementFactsTests
{
    private static readonly string[] Systems =
    {
        "THCSLT-HPC-BNN_MB_MPD-ZZ-M3-AA-0001_sangtq6ANLE",
        "THCSLT-HPC-LH_HB_HC-ZZ-M3-AA-0001_sangtq6ANLE",
        "THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_sangtq6ANLE",
        "THCSLT-HPC-TT-ZZ-M3-EP-0001_sangtq6ANLE",
    };

    [Theory]
    [InlineData("THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_sangtq6ANLE.rvt", "THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_sangtq6ANLE")]
    [InlineData("THCSLT-HPC-LH_HB_HC-ZZ-M3-AA-0001_otheruser.rvt", "THCSLT-HPC-LH_HB_HC-ZZ-M3-AA-0001_sangtq6ANLE")]
    [InlineData("THCSLT-HPC-TT-ZZ-M3-EP-0001.nwc", "THCSLT-HPC-TT-ZZ-M3-EP-0001_sangtq6ANLE")]
    public void The_grid_is_the_one_of_the_elements_own_file_not_the_active_building(string sourceFile, string expected)
    {
        Assert.Equal(expected, ClashElementFacts.GridSystemFor(sourceFile, Systems));
    }

    [Theory]
    [InlineData("THCSLT-HPC-NDN-ZZ-M3-AA-0001_user.rvt")]
    [InlineData("")]
    [InlineData(null)]
    public void A_file_without_its_own_grid_system_gets_none_rather_than_another_buildings(string? sourceFile)
    {
        Assert.Null(ClashElementFacts.GridSystemFor(sourceFile, Systems));
    }

    [Fact]
    public void Two_grid_systems_for_one_file_are_ambiguous_and_none_is_quoted()
    {
        var twice = Systems.Concat(new[] { "THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_sangtq6ANLE" });
        Assert.Null(ClashElementFacts.GridSystemFor("THCSLT-HPC-LH_HB_HC-ZZ-M3-ES-0001_user.rvt", twice));
    }

    [Fact]
    public void A_numeric_volume_field_does_not_cut_the_file_name_short()
    {
        var systems = new[] { "P-HPC-0100-ZZ-M3-AA-0001_user", "P-HPC-0100-ZZ-M3-ES-0001_user" };
        Assert.Equal("P-HPC-0100-ZZ-M3-ES-0001_user", ClashElementFacts.GridSystemFor("P-HPC-0100-ZZ-M3-ES-0001_other.rvt", systems));
    }

    [Fact]
    public void The_system_lists_each_value_once_in_order_and_is_empty_without_any()
    {
        Assert.Equal("Sanitary / TP 76", ClashElementFacts.SystemText("Sanitary", "TP 76", ""));
        Assert.Equal("3P-380V", ClashElementFacts.SystemText(null, " ", "3P-380V"));
        Assert.Equal("Sanitary", ClashElementFacts.SystemText("Sanitary", "sanitary"));
        Assert.Equal("", ClashElementFacts.SystemText(null, ""));
    }
}
