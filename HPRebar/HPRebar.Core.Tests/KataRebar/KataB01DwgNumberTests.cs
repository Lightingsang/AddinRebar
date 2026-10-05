using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// B01's bar numbers against Kata's (<see cref="KataB01DwgFixture"/>): every tag of Kata's 14 sections — its number and
/// what it says — is a tag of the canvas section at the same cut with the same number.
/// </summary>
public sealed class KataB01DwgNumberTests
{
    private static readonly double[] KataCuts = { 1250, 5450, 9950, 11850, 14300, 17025, 18750, 20250, 22650, 23950, 25600, 27950, 30600, 32266.7 };

    public static IEnumerable<object[]> Numbers() => Enumerable.Range(1, 14).Select(n => new object[] { n });

    [Fact]
    public void Every_elevation_tag_carries_Katas_number_but_those_of_the_loads()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var canvas = KataBarTagBuilder.Build(spec, layout, 10.0).SelectMany(t => t.Numbers).ToHashSet();

        // The hanger bars (2Ø16) and the joint stirrups (2x5Ø10) belong to the loads the Revit model brings.
        var kata = KataB01DwgFixture.View("elevation")
            .Where(e => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh" && e.Attributes.Count > 2
                && !e.Attributes.Any(a => a == "2Ø16" || a.Contains("5Ø10")))
            .SelectMany(e => e.Attributes.Take(2).Where(a => Regex.IsMatch(a, @"^\d+$")).Select(int.Parse))
            .Distinct().OrderBy(v => v).ToList();

        var missing = kata.Where(v => !canvas.Contains(v)).ToList();
        Assert.True(missing.Count == 0, $"Kata {string.Join(",", kata)}; canvas {string.Join(",", canvas.OrderBy(v => v))}");
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Every_section_tag_carries_Katas_number(int n)
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var st = KataBeamStations.From(spec);
        double x = KataCuts[n - 1];
        int span = Enumerable.Range(0, st.SpanCount).First(s => x >= st.SpanStart[s] - 1.0 && x <= st.SpanEnd[s] + 1.0);
        var canvas = KataSectionDrawingBuilder.Build(spec, layout, KataDetailingRuleBuilder.Build(spec), new KataSectionCut(n, x, span));
        var canvasNumbers = canvas.Tags.SelectMany(t => t.Numbers).ToHashSet();

        var kata = KataB01DwgFixture.View($"section-{n}")
            .Where(e => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh" && e.Attributes.Count > 2)
            .SelectMany(e => e.Attributes.Take(2).Where(a => Regex.IsMatch(a, @"^\d+$")).Select(int.Parse))
            .Distinct().OrderBy(v => v).ToList();

        var missing = kata.Where(v => !canvasNumbers.Contains(v)).ToList();
        Assert.True(missing.Count == 0, $"{n}-{n}: Kata {string.Join(",", kata)}; canvas {string.Join(",", canvasNumbers.OrderBy(v => v))}");
    }
}
