namespace HPBim.Core.Tests.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using Xunit;

/// <summary>
/// Adversarial empirical tests challenging Feature F8 Part 1 (CompositeSourceKey):
/// Verifies case-insensitivity, provenance isolation, dictionary/hashset semantics,
/// round-trip formatting, legacy compatibility syntax, and stress lookups.
/// </summary>
public sealed class EmpiricalChallengerM3CompositeSourceKeyTests
{
    [Theory]
    [InlineData("doc1#Column:3f2a", "DOC1#Column:3F2A")]
    [InlineData("[drawing.dwg]#[beam]:0xAA11", "[DRAWING.DWG]#[BEAM]:0xaa11")]
    [InlineData("SiteA#Slab:Slab-101", "sitea#slab:SLAB-101")]
    public void Challenge_CaseInsensitiveEquality_MatchesOrdinalIgnoreCase(string raw1, string raw2)
    {
        var k1 = CompositeSourceKey.Parse(raw1);
        var k2 = CompositeSourceKey.Parse(raw2);

        Assert.True(k1.Equals(k2));
        Assert.True(k1.Equals((object)k2));
        Assert.True(k1 == k2);
        Assert.False(k1 != k2);
        Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
        Assert.Equal(0, k1.CompareTo(k2));
    }

    [Fact]
    public void Challenge_ProvenanceIsolation_DoesNotAffectEqualityOrHashCode()
    {
        var k1 = new CompositeSourceKey("Master.dwg", CoordinationCategory.Column, "C-101", new[] { "P1", "P2" });
        var k2 = new CompositeSourceKey("master.dwg", CoordinationCategory.Column, "c-101", new[] { "P3", "P4", "P5" });
        var k3 = new CompositeSourceKey("MASTER.DWG", CoordinationCategory.Column, "C-101", Array.Empty<string>());

        Assert.Equal(k1, k2);
        Assert.Equal(k2, k3);
        Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
        Assert.Equal(k2.GetHashCode(), k3.GetHashCode());
        Assert.Equal(0, k1.CompareTo(k2));

        // Provenance handles remain accessible and distinct
        Assert.Equal(2, k1.ProvenanceHandles.Count);
        Assert.Equal(3, k2.ProvenanceHandles.Count);
        Assert.Empty(k3.ProvenanceHandles);
    }

    [Fact]
    public void Challenge_DictionaryLookup_CaseInsensitiveAndProvenanceAgnostic()
    {
        var dict = new Dictionary<CompositeSourceKey, string>();
        var insertKey = new CompositeSourceKey("CadModel.dwg", CoordinationCategory.Wall, "3F2A", new[] { "LineHandle1" });
        dict[insertKey] = "RevitUniqueId-998877";

        var lookupKey = new CompositeSourceKey("cadmodel.dwg", CoordinationCategory.Wall, "3f2a", new[] { "DifferentLineHandle" });

        Assert.True(dict.ContainsKey(lookupKey));
        Assert.True(dict.TryGetValue(lookupKey, out string? value));
        Assert.Equal("RevitUniqueId-998877", value);
    }

    [Fact]
    public void Challenge_HashSetDeduplication_CollapsesCasingAndProvenanceVariants()
    {
        var set = new HashSet<CompositeSourceKey>
        {
            new("Doc", CoordinationCategory.GradeBeam, "GB1", new[] { "A" }),
            new("doc", CoordinationCategory.GradeBeam, "gb1", new[] { "B", "C" }),
            new("DOC", CoordinationCategory.GradeBeam, "GB1", Array.Empty<string>()),
            CompositeSourceKey.Parse("[doc]#[GradeBeam]:gb1"),
            CompositeSourceKey.Parse("DOC#GRADEBEAM:GB1")
        };

        Assert.Single(set);
    }

    [Fact]
    public void Challenge_RoundTripCanonical_ProducesConsistentKey()
    {
        var original = new CompositeSourceKey("Tower1.dwg", CoordinationCategory.Column, "Col-50", new[] { "P1" });
        string canonical = original.ToString();
        Assert.Equal("[Tower1.dwg]#[Column]:Col-50", canonical);

        var parsedCanonical = CompositeSourceKey.Parse(canonical);
        Assert.Equal(original, parsedCanonical);

        string plain = original.PlainKey;
        Assert.Equal("Tower1.dwg#Column:Col-50", plain);

        var parsedPlain = CompositeSourceKey.Parse(plain);
        Assert.Equal(original, parsedPlain);
    }

    [Fact]
    public void Challenge_LegacySyntax_BackwardCompatibility()
    {
        Assert.True(CompositeSourceKey.TryParse("[]#[Column]:C1", out var k1));
        Assert.NotNull(k1);
        Assert.Equal(string.Empty, k1.DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, k1.Category);
        Assert.Equal("C1", k1.PrimaryHandle);

        Assert.True(CompositeSourceKey.TryParse("#Column:C1", out var k2));
        Assert.NotNull(k2);
        Assert.Equal(string.Empty, k2.DocIdentifier);
        Assert.Equal("C1", k2.PrimaryHandle);

        var parsedLoose = CompositeSourceKey.Parse("[]#[Column]:C1", allowEmptyDoc: true);
        Assert.Equal(k1, parsedLoose);

        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[]#[Column]:C1", allowEmptyDoc: false));
        Assert.Throws<FormatException>(() => CompositeSourceKey.Parse("[]#[Column]:C1"));
    }

    [Fact]
    public void Challenge_SpecialHandleCharacters_PreservesEmbeddedColonsAndDashes()
    {
        var key = CompositeSourceKey.Parse("[Doc.dwg]#[Beam]:GridA:Level1-Span2");
        Assert.Equal("Doc.dwg", key.DocIdentifier);
        Assert.Equal(CoordinationCategory.Beam, key.Category);
        Assert.Equal("GridA:Level1-Span2", key.PrimaryHandle);
    }

    [Fact]
    public void Challenge_DeterministicOrdering_CompareToContract()
    {
        var keys = new List<CompositeSourceKey>
        {
            new("DocB", CoordinationCategory.Column, "1"),
            new("docA", CoordinationCategory.Column, "2"),
            new("DOCA", CoordinationCategory.Beam, "1"),
            new("DocA", CoordinationCategory.Column, "1")
        };

        keys.Sort();

        // Column (4) < Beam (7), so Column keys come before Beam keys
        Assert.Equal("DocA", keys[0].DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, keys[0].Category);
        Assert.Equal("1", keys[0].PrimaryHandle);

        Assert.Equal("docA", keys[1].DocIdentifier);
        Assert.Equal(CoordinationCategory.Column, keys[1].Category);
        Assert.Equal("2", keys[1].PrimaryHandle);

        Assert.Equal("DOCA", keys[2].DocIdentifier);
        Assert.Equal(CoordinationCategory.Beam, keys[2].Category);
        Assert.Equal("1", keys[2].PrimaryHandle);

        Assert.Equal("DocB", keys[3].DocIdentifier);
    }

    [Fact]
    public void Challenge_StressDictionaryLookups_ThousandsOfKeysWithRandomCasing()
    {
        var dict = new Dictionary<CompositeSourceKey, int>();
        int count = 1000;

        for (int i = 0; i < count; i++)
        {
            var key = new CompositeSourceKey($"Doc_{i}", CoordinationCategory.Column, $"Handle_{i:X4}");
            dict[key] = i;
        }

        for (int i = 0; i < count; i++)
        {
            // Mutate casing randomly
            string lookupDoc = (i % 2 == 0) ? $"doc_{i}" : $"DOC_{i}";
            string lookupHandle = (i % 3 == 0) ? $"handle_{i:x4}" : $"HANDLE_{i:X4}";
            var lookupKey = new CompositeSourceKey(lookupDoc, CoordinationCategory.Column, lookupHandle);

            Assert.True(dict.TryGetValue(lookupKey, out int val));
            Assert.Equal(i, val);
        }
    }
}
