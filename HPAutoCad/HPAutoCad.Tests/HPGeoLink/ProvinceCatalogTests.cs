using HPAutoCad.Core.HPGeoLink.Catalog;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>The embedded province table is the oracle's, complete and internally consistent.</summary>
public sealed class ProvinceCatalogTests
{
    private static readonly ProvinceCatalog Catalog = ProvinceCatalog.Default;

    [Fact]
    public void Counts_match_the_reference_tool()
    {
        Assert.Equal(34, Catalog.Current.Count);
        Assert.Equal(63, Catalog.Legacy.Count);
        Assert.Equal(17, Catalog.CentralMeridians.Count);
        Assert.Equal(103.0, Catalog.CentralMeridians[0]);
        Assert.Equal(108.5, Catalog.CentralMeridians[^1]);
        Assert.Contains("ToolVN2000ConvertGoogleEarth", Catalog.Source);
    }

    [Fact]
    public void Every_meridian_is_a_quarter_degree_between_103_and_108_30()
    {
        foreach (var cm in Catalog.CentralMeridians)
        {
            Assert.InRange(cm, 103.0, 108.5);
            Assert.Equal(0.0, Math.Abs(cm * 4 - Math.Round(cm * 4)), 9);
        }
    }

    [Fact]
    public void Ho_Chi_Minh_City_has_two_meridians_with_former_province_labels()
    {
        var hcm = Catalog.Find(ProvinceCatalogKind.Current, "TP. Hồ Chí Minh");
        Assert.NotNull(hcm);
        Assert.Equal(new[] { 105.75, 107.75 }, hcm!.CentralMeridians);
        Assert.False(hcm.HasSingleMeridian);
        Assert.Equal("TP. Hồ Chí Minh cũ + Bình Dương cũ", Catalog.FormerProvinceLabel(hcm.Name, 105.75));
        Assert.Equal("Bà Rịa - Vũng Tàu cũ", Catalog.FormerProvinceLabel(hcm.Name, 107.75));
    }

    [Fact]
    public void Single_meridian_provinces_have_no_former_label()
    {
        var hanoi = Catalog.Find(ProvinceCatalogKind.Current, "TP. Hà Nội");
        Assert.NotNull(hanoi);
        Assert.True(hanoi!.HasSingleMeridian);
        Assert.Equal(105.0, hanoi.CentralMeridians[0]);
        Assert.Equal("", Catalog.FormerProvinceLabel(hanoi.Name, 105.0));
    }

    [Fact]
    public void Every_origin_meridian_belongs_to_its_province()
    {
        foreach (var p in Catalog.Current)
        {
            foreach (var cm in p.CentralMeridians)
            {
                // Either no note (single-zone province) or a non-empty label — never a label for a foreign meridian.
                var label = Catalog.FormerProvinceLabel(p.Name, cm);
                Assert.True(label.Length == 0 || p.CentralMeridians.Count > 1, $"{p.Name} {cm}: '{label}'");
            }
            Assert.Equal("", Catalog.FormerProvinceLabel(p.Name, 99.0));
        }
    }

    [Fact]
    public void Legacy_catalogue_carries_the_pre_merger_provinces()
    {
        Assert.NotNull(Catalog.Find(ProvinceCatalogKind.Legacy, "Bình Dương"));
        Assert.NotNull(Catalog.Find(ProvinceCatalogKind.Legacy, "Bà Rịa - Vũng Tàu"));
        Assert.Null(Catalog.Find(ProvinceCatalogKind.Current, "Bình Dương"));
    }

    [Theory]
    [InlineData(105.75, "105°45′")]
    [InlineData(105.0, "105°00′")]
    [InlineData(104.5, "104°30′")]
    [InlineData(108.25, "108°15′")]
    [InlineData(double.NaN, "—")]
    public void Central_meridian_formats_like_the_reference_tool(double degrees, string expected) =>
        Assert.Equal(expected, CentralMeridian.Format(degrees));

    [Theory]
    [InlineData("105.75", 105.75)]
    [InlineData("105,75", 105.75)]
    [InlineData("105°45′", 105.75)]
    [InlineData("105-45", 105.75)]
    [InlineData("105 30", 105.5)]
    [InlineData("105", 105.0)]
    public void Central_meridian_parses_common_spellings(string text, double expected) =>
        Assert.Equal(expected, CentralMeridian.Parse(text));

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("105-75")]
    public void Central_meridian_rejects_garbage(string text) => Assert.Null(CentralMeridian.Parse(text));

    [Fact]
    public void Epsg_code_is_not_assigned_until_verified() => Assert.Null(new CentralMeridian(105.75).EpsgCode);
}
