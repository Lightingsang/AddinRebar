using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HPNavis.BIMCoordinator.Rules;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

/// <summary>The embedded matrix must be exactly the company workbook: counts, priorities, LODs, tolerances, provenance.</summary>
public sealed class ClashMatrixTests
{
    private static readonly ClashMatrix Matrix = ClashMatrix.Default;

    [Fact]
    public void Matrix_has_184_unique_rules_over_21_groups()
    {
        Assert.Equal(21, Matrix.Groups.Count);
        Assert.Equal(184, Matrix.Rules.Count);
        Assert.Equal(184, Matrix.Rules.Select(r => r.Id).Distinct().Count());
        Assert.Equal(184, Matrix.Rules.Select(r => string.CompareOrdinal(r.Left, r.Right) <= 0 ? r.Left + r.Right : r.Right + r.Left).Distinct().Count());
    }

    [Fact]
    public void Priorities_are_40_high_99_medium_45_low()
    {
        Assert.Equal(new[] { 40, 99, 45 }, new[] { 1, 2, 3 }.Select(p => Matrix.Rules.Count(r => r.Priority == p)));
    }

    [Theory]
    [InlineData("200", 27)]
    [InlineData("300", 154)]
    [InlineData("350", 184)]
    public void Rules_per_lod_follow_the_group_flags(string lod, int expected)
    {
        Assert.Equal(expected, Matrix.RulesFor(lod).Count);
    }

    [Theory]
    [InlineData("200", 50)]
    [InlineData("300", 30)]
    [InlineData("350", 10)]
    public void Tolerances_come_from_the_matrix_note(string lod, double expectedMm)
    {
        Assert.Equal(expectedMm, Matrix.ToleranceMmFor(lod));
    }

    [Fact]
    public void Lod_400_is_refused_until_a_tolerance_is_approved()
    {
        var error = Assert.Throws<ArgumentException>(() => Matrix.RulesFor("400"));
        Assert.Contains("no approved tolerance", error.Message);
    }

    [Theory]
    [InlineData("250")]
    [InlineData("")]
    [InlineData("LOD350")]
    public void Unknown_lod_is_the_callers_error(string lod)
    {
        Assert.Throws<ArgumentException>(() => Matrix.ToleranceMmFor(lod));
    }

    [Theory]
    [InlineData("HP_A1_A8", 1, "ARC-ARC")]   // row A1, column A8 (AD5)
    [InlineData("HP_M2_S2", 1, "MEP-STR")]   // row S2, column M2 (P16)
    [InlineData("HP_A8_S2", 3, "ARC-STR")]   // row S2, column A8 (AD16)
    [InlineData("HP_M6_S5", 1, "MEP-STR")]   // row S5, column M6 (T19)
    [InlineData("HP_S1_S1", 3, "STR-STR")]   // self pair (I15)
    [InlineData("HP_A2_M2", 1, "ARC-MEP")]   // row M2, column A2 (X22) — P6 beside it is the legend swatch
    public void Spot_checks_against_the_workbook(string id, int priority, string pair)
    {
        var rule = Matrix.Rules.Single(r => r.Id == id);
        Assert.Equal(priority, rule.Priority);
        Assert.Equal(pair, rule.DisciplinePair);
    }

    [Fact]
    public void Legend_cells_are_not_read_as_matrix_pairs()
    {
        Assert.All(Matrix.Rules, r => Assert.Single(r.Cells));
    }

    [Fact]
    public void Filters_narrow_by_priority_and_rule_id()
    {
        Assert.Equal(40, Matrix.RulesFor("350", priorities: new[] { 1 }).Count);
        Assert.Single(Matrix.RulesFor("350", ruleIds: new[] { "HP_A1_A8" }));
        Assert.Throws<ArgumentException>(() => Matrix.RulesFor("350", ruleIds: new[] { "HP_X1_A1" }));
    }

    [Fact]
    public void Embedded_json_was_generated_from_the_committed_workbook()
    {
        var workbook = Path.Combine(HPNavisRoot(), "tools", "bim-coordinator", Matrix.Document.Source.Workbook);
        using var sha = SHA256.Create();
        var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(workbook))).Replace("-", "").ToLowerInvariant();
        Assert.Equal(hash, Matrix.Document.Source.Sha256);
    }

    [Fact]
    public void A_duplicate_pair_or_unknown_group_fails_the_load()
    {
        var json = ClashMatrix.ReadResource(ClashMatrix.ResourceName);
        var duplicated = json.Replace("\"id\": \"HP_A1_A2\"", "\"id\": \"HP_A1_A1x\"").Replace("\"right\": \"A2\"", "\"right\": \"A1\"");
        Assert.Throws<InvalidOperationException>(() => ClashMatrix.Parse(duplicated));
        Assert.Throws<InvalidOperationException>(() => ClashMatrix.Parse(json.Replace("\"left\": \"S1\"", "\"left\": \"Z9\"")));
    }

    internal static string HPNavisRoot([CallerFilePath] string here = "") => Path.GetDirectoryName(Path.GetDirectoryName(here))!;
}
