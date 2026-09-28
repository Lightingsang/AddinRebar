using System.Collections.Generic;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public class KataCellTableTests
{
    [Fact]
    public void AddressParsing_StandardCellReferences_ParsesCorrectRowAndCol()
    {
        Assert.True(KataDamCellAccessorExtensions.TryParseAddress("A1", out int r1, out int c1));
        Assert.Equal(1, r1);
        Assert.Equal(1, c1);

        Assert.True(KataDamCellAccessorExtensions.TryParseAddress("B3", out int r2, out int c2));
        Assert.Equal(3, r2);
        Assert.Equal(2, c2);

        Assert.True(KataDamCellAccessorExtensions.TryParseAddress("Z10", out int r3, out int c3));
        Assert.Equal(10, r3);
        Assert.Equal(26, c3);

        Assert.True(KataDamCellAccessorExtensions.TryParseAddress("AA1", out int r4, out int c4));
        Assert.Equal(1, r4);
        Assert.Equal(27, c4);

        Assert.True(KataDamCellAccessorExtensions.TryParseAddress("BZ30", out int r5, out int c5));
        Assert.Equal(30, r5);
        Assert.Equal(78, c5);
    }

    [Fact]
    public void KataCellTable_SetAndGetByRowCol_ReturnsCorrectValues()
    {
        var table = new KataCellTable();
        table.Set(3, 2, "B01");
        table.Set(5, 2, 1100.0);
        table.Set(4, 2, 1);

        Assert.Equal("B01", table.GetText(3, 2));
        Assert.Equal(1100.0, table.GetDouble(5, 2));
        Assert.Equal(1, table.GetInt(4, 2));
    }

    [Fact]
    public void KataCellTable_SetAndGetByA1Address_ReturnsCorrectValues()
    {
        var table = new KataCellTable();
        table.Set("B3", "B01");
        table.Set("B5", 1100.0);
        table.Set("B4", 1);

        Assert.Equal("B01", table.GetText("B3"));
        Assert.Equal(1100.0, table.GetDouble("B5"));
        Assert.Equal(1, table.GetInt("B4"));
    }

    [Fact]
    public void KataCellTable_Raw2DArray1Based_WrapsCorrectly()
    {
        // Simulate Excel COM Value2 array (1-based indexed)
        var raw = (object[,])System.Array.CreateInstance(typeof(object), new[] { 30, 78 }, new[] { 1, 1 });
        raw[3, 2] = "B01";
        raw[5, 2] = 1100.0;
        raw[11, 3] = 400.0;
        raw[11, 4] = 10400.0;

        var table = new KataCellTable(raw);

        Assert.Equal("B01", table.GetText(3, 2));
        Assert.Equal(1100.0, table.GetDouble(5, 2));
        Assert.Equal(400.0, table.GetDouble(11, 3));
        Assert.Equal(10400.0, table.GetDouble(11, 4));
        Assert.Equal("10400", table.GetText(11, 4)); // Formatted whole number
    }

    [Fact]
    public void KataCellTable_FromDictionary_WrapsCorrectly()
    {
        var dict = new Dictionary<string, object?>
        {
            ["B3"] = "B01",
            ["B5"] = "1100",
            ["B6"] = "500"
        };

        var table = new KataCellTable(dict);

        Assert.Equal("B01", table.GetText("B3"));
        Assert.Equal(1100.0, table.GetDouble("B5"));
        Assert.Equal(500.0, table.GetDouble("B6"));
    }
}
