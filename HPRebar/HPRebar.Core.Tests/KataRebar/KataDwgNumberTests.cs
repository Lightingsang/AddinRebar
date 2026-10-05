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
/// Each beam's bar numbers against Kata's (<see cref="KataDwgFixture"/>): every tag of Kata's sections — its number and
/// what it says — is a tag of the canvas section at the same cut with the same number.
/// </summary>
public sealed class KataDwgNumberTests
{
    public static IEnumerable<object[]> Beams() => KataDwgBeam.Names();

    public static IEnumerable<object[]> Sections() => KataDwgBeam.Sections();

    [Theory]
    [MemberData(nameof(Beams))]
    public void Every_elevation_tag_carries_Katas_number_but_those_of_the_loads(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var spec = KataDamSheetParser.Parse(beam.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var canvas = KataBarTagBuilder.Build(spec, layout, 10.0).SelectMany(t => t.Numbers).ToHashSet();

        // The hanger bars (2Ø16) and the joint stirrups (2x5Ø10) belong to the loads the Revit model brings.
        var kata = KataDwgFixture.View(beam, "elevation")
            .Where(e => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh" && e.Attributes.Count > 2
                && !e.Attributes.Any(a => a == "2Ø16" || a.Contains("5Ø10")))
            .SelectMany(e => e.Attributes.Take(2).Where(a => Regex.IsMatch(a, @"^\d+$")).Select(int.Parse))
            .Distinct().OrderBy(v => v).ToList();

        var missing = kata.Where(v => !canvas.Contains(v)).ToList();
        Assert.True(missing.Count == 0, $"{name}: Kata {string.Join(",", kata)}; canvas {string.Join(",", canvas.OrderBy(v => v))}");
    }

    [Theory]
    [MemberData(nameof(Sections))]
    public void Every_section_tag_carries_Katas_number(string name, int n)
    {
        var beam = KataDwgBeam.Named(name);
        var spec = KataDamSheetParser.Parse(beam.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var st = KataBeamStations.From(spec);
        double x = beam.Cuts[n - 1];
        int span = Enumerable.Range(0, st.SpanCount).First(s => x >= st.SpanStart[s] - 1.0 && x <= st.SpanEnd[s] + 1.0);
        var canvas = KataSectionDrawingBuilder.Build(spec, layout, KataDetailingRuleBuilder.Build(spec), new KataSectionCut(n, x, span));
        var canvasNumbers = canvas.Tags.SelectMany(t => t.Numbers).ToHashSet();

        var kata = KataDwgFixture.View(beam, $"section-{n}")
            .Where(e => e.Type == "AcDbBlockReference" && e.Layer == "kata_net manh" && e.Attributes.Count > 2)
            .SelectMany(e => e.Attributes.Take(2).Where(a => Regex.IsMatch(a, @"^\d+$")).Select(int.Parse))
            .Distinct().OrderBy(v => v).ToList();

        var missing = kata.Where(v => !canvasNumbers.Contains(v)).ToList();
        Assert.True(missing.Count == 0, $"{name} {n}-{n}: Kata {string.Join(",", kata)}; canvas {string.Join(",", canvasNumbers.OrderBy(v => v))}");
    }
}
