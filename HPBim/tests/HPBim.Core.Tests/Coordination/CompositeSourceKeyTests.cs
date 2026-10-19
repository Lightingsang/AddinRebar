namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using Xunit;

public sealed class CompositeSourceKeyTests
{
    [Fact]
    public void Parse_CanonicalBracketedFormat_Success()
    {
        var key = CompositeSourceKey.Parse("[Drawing1.dwg]#[Column]:2B4F");

        Assert.Equal("Drawing1.dwg", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, key.Category);
        Assert.Equal("2B4F", key.PrimaryHandle);
    }

    [Fact]
    public void Parse_PlainDelimitedFormat_Success()
    {
        var key = CompositeSourceKey.Parse("Drawing1.dwg#Column:2B4F");

        Assert.Equal("Drawing1.dwg", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, key.Category);
        Assert.Equal("2B4F", key.PrimaryHandle);
    }

    [Fact]
    public void Parse_BracketedHandle_StripsOuterBrackets()
    {
        var key = CompositeSourceKey.Parse("[Doc]#[Beam]:[B-101]");

        Assert.Equal("Doc", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Beam, key.Category);
        Assert.Equal("B-101", key.PrimaryHandle);
    }

    [Fact]
    public void Parse_CaseInsensitiveCategory_Success()
    {
        var key = CompositeSourceKey.Parse("[Doc]#[column]:C1");

        Assert.Equal(CoordinationCategory.Column, key.Category);
    }

    [Fact]
    public void Parse_WhitespaceAroundDelimiters_TrimsProperly()
    {
        var key = CompositeSourceKey.Parse(" [Doc] # [Wall] : W1 ");

        Assert.Equal("Doc", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Wall, key.Category);
        Assert.Equal("W1", key.PrimaryHandle);
    }

    [Fact]
    public void ToString_RoundTrip_ProducesCanonical()
    {
        var key = new CompositeSourceKey("D", CoordinationCategory.Slab, "S1");
        string formatted = key.ToString();

        Assert.Equal("[D]#[Slab]:S1", formatted);
        var parsed = CompositeSourceKey.Parse(formatted);
        Assert.Equal(key, parsed);
    }

    [Fact]
    public void PlainKey_RoundTrip_MatchesKey()
    {
        var key = new CompositeSourceKey("D", CoordinationCategory.Beam, "B1");
        string plain = key.PlainKey;

        Assert.Equal("D#Beam:B1", plain);
        var parsed = CompositeSourceKey.Parse(plain);
        Assert.Equal(key, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrEmptyString_ThrowsFormatException(string invalidInput)
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse(invalidInput));
    }

    [Fact]
    public void Parse_MissingCategory_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[Doc]#:H1"));
    }

    [Fact]
    public void Parse_InvalidCategoryEnum_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[Doc]#[InvalidCat]:H1"));
    }

    [Fact]
    public void Parse_MissingHandle_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[Doc]#[Column]:"));
    }

    [Fact]
    public void Parse_WhitespaceHandle_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[Doc]#[Column]:   "));
    }

    [Fact]
    public void Parse_MissingDocIdentifier_Strict_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[]#[Column]:H1", allowEmptyDoc: false));
    }

    [Fact]
    public void Equals_SameKeysDifferentDocCase_ReturnsTrue()
    {
        var k1 = CompositeSourceKey.Parse("[DOC]#[Column]:H1");
        var k2 = CompositeSourceKey.Parse("[doc]#[Column]:H1");

        Assert.True(k1.Equals(k2));
        Assert.True(k1 == k2);
        Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
    }

    [Fact]
    public void Equals_SameKeysDifferentHandleCase_ReturnsTrue()
    {
        var k1 = CompositeSourceKey.Parse("[Doc]#[Column]:3f2a");
        var k2 = CompositeSourceKey.Parse("[Doc]#[Column]:3F2A");

        Assert.True(k1.Equals(k2));
        Assert.True(k1 == k2);
        Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentCategory_ReturnsFalse()
    {
        var k1 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "H1");
        var k2 = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "H1");

        Assert.False(k1.Equals(k2));
        Assert.True(k1 != k2);
    }

    [Fact]
    public void Equals_DifferentProvenanceHandles_StillEqual()
    {
        var k1 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "H1", new[] { "P1" });
        var k2 = new CompositeSourceKey("Doc", CoordinationCategory.Column, "H1", new[] { "P2" });

        Assert.Equal(k1, k2);
        Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
    }

    [Fact]
    public void Dictionary_KeyLookup_FindsIgnoringCase()
    {
        var dict = new Dictionary<CompositeSourceKey, string>();
        var k1 = new CompositeSourceKey("DocA", CoordinationCategory.Column, "3f2a");
        dict[k1] = "UniqueId-123";

        var kQuery = new CompositeSourceKey("doca", CoordinationCategory.Column, "3F2A");
        Assert.True(dict.TryGetValue(kQuery, out string? value));
        Assert.Equal("UniqueId-123", value);
    }

    [Fact]
    public void CompareTo_OrdersDeterministically()
    {
        var k1 = new CompositeSourceKey("A", CoordinationCategory.Column, "1");
        var k2 = new CompositeSourceKey("B", CoordinationCategory.Column, "1");
        var k3 = new CompositeSourceKey("A", CoordinationCategory.Beam, "1");
        var k4 = new CompositeSourceKey("A", CoordinationCategory.Column, "2");

        var list = new List<CompositeSourceKey> { k2, k4, k1, k3 };
        list.Sort();

        Assert.Equal(new[] { k1, k4, k3, k2 }, list);
    }
}
