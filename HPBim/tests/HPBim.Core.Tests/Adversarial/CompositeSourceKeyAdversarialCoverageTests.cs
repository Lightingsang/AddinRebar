namespace HPBim.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using Xunit;

public sealed class CompositeSourceKeyAdversarialCoverageTests
{
    [Fact]
    public void Parse_FilenameWithEmbeddedHash_ParsesCorrectlyInBracketedFormat()
    {
        string raw = "[Master#Model#2026.dwg]#[Column]:Col-101";
        var key = CompositeSourceKey.Parse(raw);

        Assert.Equal("Master#Model#2026.dwg", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, key.Category);
        Assert.Equal("Col-101", key.PrimaryHandle);
    }

    [Fact]
    public void Parse_MultipleColonsInHandle_PreservesAllColons()
    {
        string raw = "[Site.dwg]#[Beam]:GridA:Level1:Span2:Rebar-01";
        var key = CompositeSourceKey.Parse(raw);

        Assert.Equal("Site.dwg", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Beam, key.Category);
        Assert.Equal("GridA:Level1:Span2:Rebar-01", key.PrimaryHandle);
    }

    [Fact]
    public void Parse_ExoticWhitespaceAndTabsAroundDelimiters_TrimsProperly()
    {
        string raw = " \t[BuildingA.dwg]\t # \t[Wall]\t : \tW-01\r\n ";
        var key = CompositeSourceKey.Parse(raw);

        Assert.Equal("BuildingA.dwg", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Wall, key.Category);
        Assert.Equal("W-01", key.PrimaryHandle);
    }

    [Fact]
    public void Parse_NumericCategory_ParsesIfDefined_OrRejectsIfNotDefined()
    {
        // 4 corresponds to CoordinationCategory.Column
        int colVal = (int)CoordinationCategory.Column;
        string validNumeric = $"[Doc.dwg]#[{colVal}]:Col-1";
        var key = CompositeSourceKey.Parse(validNumeric);
        Assert.Equal(CoordinationCategory.Column, key.Category);

        // 9999 is undefined in CoordinationCategory
        string invalidNumeric = "[Doc.dwg]#[9999]:Col-1";
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse(invalidNumeric));
    }

    [Theory]
    [InlineData("[Doc]#[Column:Handle")] // Missing category closing bracket
    [InlineData("[Doc]#[Column]Handle")]  // Missing delimiter colon
    [InlineData("[Doc]:[Column]:Handle")]  // Colon instead of hash
    [InlineData("Doc#[Column]")]           // Missing handle and colon
    public void Parse_MalformedDelimiters_ThrowsFormatException(string malformed)
    {
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse(malformed));
    }

    [Fact]
    public void CompareTo_ConsistentWithEqualsContract()
    {
        var k1 = new CompositeSourceKey("DocA", CoordinationCategory.Column, "H1");
        var k2 = new CompositeSourceKey("doca", CoordinationCategory.Column, "h1");
        var k3 = new CompositeSourceKey("DocB", CoordinationCategory.Column, "H1");

        Assert.True(k1.Equals(k2));
        Assert.Equal(0, k1.CompareTo(k2));
        Assert.Equal(0, k2.CompareTo(k1));

        Assert.True(k1.CompareTo(k3) < 0);
        Assert.True(k3.CompareTo(k1) > 0);
    }

    [Fact]
    public void Stress_MassiveKeyLookupAndCollisionResistance()
    {
        var dict = new Dictionary<CompositeSourceKey, string>();
        int count = 500;

        for (int i = 0; i < count; i++)
        {
            var key = new CompositeSourceKey(
                $"SubDoc_{i:D4}",
                CoordinationCategory.Column,
                $"Col_{i:X4}",
                new[] { $"Prov_{i}_A", $"Prov_{i}_B" });
            dict[key] = $"UniqueId_{i}";
        }

        Assert.Equal(count, dict.Count);

        for (int i = 0; i < count; i++)
        {
            // Lookup with inverted casing and completely different provenance
            var lookupKey = new CompositeSourceKey(
                $"subdoc_{i:D4}",
                CoordinationCategory.Column,
                $"col_{i:x4}",
                new[] { "DifferentProv" });

            Assert.True(dict.TryGetValue(lookupKey, out string? uid));
            Assert.Equal($"UniqueId_{i}", uid);
        }
    }

    [Fact]
    public void Equals_And_HashCode_EmptyKeyContract()
    {
        var defaultKey = new CompositeSourceKey();
        var emptyStatic = CompositeSourceKey.Empty;

        Assert.Equal(emptyStatic, defaultKey);
        Assert.Equal(emptyStatic.GetHashCode(), defaultKey.GetHashCode());
        Assert.Equal(string.Empty, emptyStatic.DocIdentifier);
        Assert.Equal(string.Empty, emptyStatic.PrimaryHandle);
        Assert.Equal((CoordinationCategory)0, emptyStatic.Category);
    }
}
